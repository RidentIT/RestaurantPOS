import { app, BrowserWindow, dialog, ipcMain } from 'electron';

export function registerSystemIPC() {
  ipcMain.handle('system:ping', () => 'pong');
  ipcMain.handle('system:version', () => process.versions);

  // Used after a database restore: the API process that was serving this window is about to shut
  // itself down too, so the shell has nothing left to talk to until someone reopens it by hand.
  ipcMain.handle('system:quit', () => {
    app.quit();
  });

  // Backs the backup folder picker in Settings — a native folder dialog rather than a free-typed
  // path, so an admin cannot point backups at a path that does not exist or is misspelled.
  ipcMain.handle('system:selectFolder', async (event) => {
    const window = BrowserWindow.fromWebContents(event.sender) ?? undefined;
    const result = await dialog.showOpenDialog(window, {
      properties: ['openDirectory', 'createDirectory'],
    });

    return result.canceled || result.filePaths.length === 0 ? null : result.filePaths[0];
  });
}
