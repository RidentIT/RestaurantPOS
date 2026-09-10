import { app, BrowserWindow, dialog } from 'electron';
import { spawn, execFile, ChildProcess } from 'child_process';
import http from 'http';
import fs from 'fs';
import path from 'path';
import { registerSystemIPC } from './ipc/system.ipc';
import { registerPrinterIPC } from './ipc/printer.ipc';

/** The API always listens here, and the renderer's default API URL points at the same port. */
const API_PORT = 5207;
const API_ORIGIN = `http://localhost:${API_PORT}`;

let mainWindow: BrowserWindow | null = null;
let apiProcess: ChildProcess | null = null;
let quitting = false;

// A till is one window. A second launch (double-clicked icon, tapped twice) just focuses the
// running one rather than starting a second copy fighting over the same database and port.
if (!app.requestSingleInstanceLock()) {
  app.quit();
} else {
  app.on('second-instance', () => {
    if (!mainWindow) return;
    if (mainWindow.isMinimized()) mainWindow.restore();
    mainWindow.focus();
  });

  void main();
}

async function main() {
  await app.whenReady();

  registerSystemIPC();
  registerPrinterIPC();

  if (app.isPackaged) {
    // Recover to a working till unattended after a power cut.
    app.setLoginItemSettings({ openAtLogin: true });
    startApi();
  }

  await createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      void createWindow();
    }
  });
}

/**
 * Where the database, logs, JWT key and attachments live — a per-machine folder Windows always
 * lets us write to, unlike the install directory under Program Files. Survives reinstalls and
 * app updates, so the restaurant's data is never bound to a particular installed version.
 */
function resolveDataDir(): string {
  const dir = app.getPath('userData');

  for (const sub of ['', 'logs', 'keys', 'attachments']) {
    fs.mkdirSync(path.join(dir, sub), { recursive: true });
  }

  return dir;
}

/** Launches the bundled .NET API as a child process, pointed at the writable data folder. */
function startApi() {
  const dataDir = resolveDataDir();
  const exe = path.join(process.resourcesPath, 'backend', 'RestaurantPOS.API.exe');

  apiProcess = spawn(exe, [], {
    cwd: dataDir,
    windowsHide: true,
    stdio: 'ignore',
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: 'Production',
      ASPNETCORE_URLS: API_ORIGIN,
      RPOS_DATA_DIR: dataDir,
      ConnectionStrings__DefaultConnection: `Data Source=${path.join(dataDir, 'restaurantpos.db')}`,
      Jwt__KeyFilePath: path.join(dataDir, 'keys', 'jwt-signing.key'),
      Expenses__AttachmentsPath: path.join(dataDir, 'attachments'),
    },
  });

  apiProcess.on('exit', (code) => {
    apiProcess = null;

    // Exit code 0 is a clean shutdown: either the app is closing, or a database restore asked the
    // API to stop (the renderer has already shown the "reopen the app" screen for that). Anything
    // else is a crash the operator needs to know about.
    if (quitting || code === 0) {
      return;
    }

    dialog.showErrorBox(
      'Restaurant POS has stopped',
      'The background service stopped unexpectedly. The application will now close — reopen it to try again. ' +
        'If this keeps happening, restart Windows.',
    );
    app.quit();
  });
}

function stopApi() {
  if (!apiProcess?.pid) {
    return;
  }

  // taskkill /T takes the whole process tree down; a bare kill() can leave the .NET host running
  // and holding the database file, which then blocks the next launch.
  try {
    execFile('taskkill', ['/pid', String(apiProcess.pid), '/t', '/f']);
  } catch {
    apiProcess.kill();
  }

  apiProcess = null;
}

/** Resolves once the API answers its health check, or rejects after the timeout. */
function waitForApi(timeoutMs = 45_000): Promise<void> {
  const deadline = Date.now() + timeoutMs;

  return new Promise((resolve, reject) => {
    const attempt = () => {
      const request = http.get(`${API_ORIGIN}/health`, (response) => {
        response.resume();

        if (response.statusCode === 200) {
          resolve();
        } else {
          retry();
        }
      });

      request.on('error', retry);
      request.setTimeout(2_000, () => request.destroy());
    };

    const retry = () => {
      if (Date.now() > deadline) {
        reject(new Error('The Restaurant POS service did not start in time.'));
      } else {
        setTimeout(attempt, 500);
      }
    };

    attempt();
  });
}

const SPLASH = `data:text/html,${encodeURIComponent(`
  <!doctype html><meta charset="utf-8">
  <style>
    html,body{height:100%;margin:0}
    body{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:18px;
      font:15px system-ui,-apple-system,"Segoe UI",sans-serif;color:#0f5c4a;background:#fbfaf7}
    .ring{width:34px;height:34px;border:3px solid #d7e6df;border-top-color:#0f5c4a;border-radius:50%;
      animation:spin 0.8s linear infinite}
    @keyframes spin{to{transform:rotate(360deg)}}
    @media(prefers-color-scheme:dark){body{background:#11150f;color:#57bc8c}.ring{border-color:#2a3b33;border-top-color:#57bc8c}}
  </style>
  <div class="ring"></div><div>Starting Restaurant POS…</div>
`)}`;

async function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1280,
    height: 800,
    minWidth: 1024,
    minHeight: 768,
    show: false,
    title: 'Restaurant POS Terminal',
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      nodeIntegration: false,
      contextIsolation: true,
      sandbox: false,
    },
  });

  mainWindow.webContents.on('did-finish-load', () => {
    mainWindow?.webContents.send('main-process-message', new Date().toLocaleString());
  });

  mainWindow.on('closed', () => {
    mainWindow = null;
  });

  const devServerUrl = process.env.VITE_DEV_SERVER_URL;

  if (devServerUrl) {
    // Development: Vite and `dotnet run` are already running, started by `npm run electron:dev`.
    await mainWindow.loadURL(devServerUrl);
    mainWindow.webContents.openDevTools();
    mainWindow.show();
    return;
  }

  await mainWindow.loadURL(SPLASH);
  mainWindow.show();

  if (app.isPackaged) {
    try {
      await waitForApi();
    } catch {
      dialog.showErrorBox(
        'Restaurant POS did not start',
        'The background service did not respond. Close the application and open it again. ' +
          'If it still fails, restart Windows.',
      );
      app.quit();
      return;
    }
  }

  await mainWindow.loadFile(path.join(__dirname, '../dist/index.html'));
}

app.on('before-quit', () => {
  quitting = true;
  stopApi();
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});
