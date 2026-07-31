import { ipcMain } from 'electron';
import { printerService } from '../services/printer.service';

export function registerPrinterIPC() {
  ipcMain.handle('printer:print', async (_event, data) => {
    return await printerService.printReceipt(data);
  });
}
