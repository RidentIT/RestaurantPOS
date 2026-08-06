import { ipcMain } from 'electron';
import { printerService, type PrintHtmlOptions } from '../services/printer.service';

export function registerPrinterIPC() {
  ipcMain.handle('printer:printHtml', async (_event, html: string, options?: PrintHtmlOptions) => {
    return await printerService.printHtml(html, options);
  });

  ipcMain.handle('printer:list', async () => {
    return await printerService.listPrinters();
  });
}
