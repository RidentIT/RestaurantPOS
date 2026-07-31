import { ipcMain } from 'electron';

export function registerSystemIPC() {
  ipcMain.handle('system:ping', () => 'pong');
  ipcMain.handle('system:version', () => process.versions);
}
