import { BrowserWindow } from 'electron';

export interface PrintHtmlOptions {
  silent?: boolean;
  deviceName?: string;
  widthMm?: number;
}

export interface PrintResult {
  success: boolean;
  message: string;
}

/** Microns per millimetre — Electron states page size in microns. */
const MICRONS_PER_MM = 1000;

/**
 * Sends rendered documents to a printer.
 *
 * The renderer hands over a finished HTML document and this loads it offscreen to print. Going
 * through the installed printer driver rather than emitting raw ESC/POS is what lets the same
 * code drive whatever thermal printer the restaurant actually owns, and lets a receipt carry a
 * QR code without encoding one for a specific print head.
 */
export class PrinterService {
  public async printHtml(html: string, options: PrintHtmlOptions = {}): Promise<PrintResult> {
    const { silent = true, deviceName, widthMm = 80 } = options;

    // Hidden, so printing never steals focus from the till in the middle of service.
    const printWindow = new BrowserWindow({
      show: false,
      webPreferences: { javascript: false },
    });

    try {
      await printWindow.loadURL(`data:text/html;charset=utf-8,${encodeURIComponent(html)}`);

      return await new Promise<PrintResult>((resolve) => {
        printWindow.webContents.print(
          {
            silent,
            printBackground: true,
            ...(deviceName ? { deviceName } : {}),
            margins: { marginType: 'none' },
            // Roll paper has no fixed page length, so the height is left generous and the
            // printer cuts at the end of the content.
            pageSize: { width: widthMm * MICRONS_PER_MM, height: 297 * MICRONS_PER_MM },
          },
          (success, failureReason) => {
            resolve(
              success
                ? { success: true, message: 'Printed' }
                : { success: false, message: failureReason || 'Printing was cancelled' },
            );
          },
        );
      });
    } catch (error) {
      return { success: false, message: error instanceof Error ? error.message : 'Printing failed' };
    } finally {
      if (!printWindow.isDestroyed()) {
        printWindow.destroy();
      }
    }
  }

  /** Printers installed on this machine, so the operator can choose one. */
  public async listPrinters(): Promise<string[]> {
    const window = BrowserWindow.getAllWindows()[0];

    if (!window) {
      return [];
    }

    const printers = await window.webContents.getPrintersAsync();

    return printers.map((printer) => printer.name);
  }
}

export const printerService = new PrinterService();
