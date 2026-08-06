import { contextBridge, ipcRenderer } from 'electron';

contextBridge.exposeInMainWorld('electronAPI', {
  ping: () => ipcRenderer.invoke('system:ping'),
  printHtml: (html: string, options?: unknown) => ipcRenderer.invoke('printer:printHtml', html, options),
  listPrinters: () => ipcRenderer.invoke('printer:list'),
  onMainProcessMessage: (callback: (message: string) => void) => {
    ipcRenderer.on('main-process-message', (_event, message) => callback(message));
  },
});
