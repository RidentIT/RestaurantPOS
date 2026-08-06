/** Options for sending a rendered document to a printer. */
export interface PrintHtmlOptions {
  /** Bypasses the print dialog. Off means the operator picks the printer. */
  silent?: boolean;
  /** Target printer name. Omitted uses the system default. */
  deviceName?: string;
  /** Paper width in millimetres. 80 for a standard thermal roll. */
  widthMm?: number;
}

export interface PrintResult {
  success: boolean;
  message: string;
}

export interface IElectronAPI {
  ping: () => Promise<string>;
  /** Renders a standalone HTML document and sends it to a printer. */
  printHtml: (html: string, options?: PrintHtmlOptions) => Promise<PrintResult>;
  /** Names of the printers installed on this machine, for the settings screen. */
  listPrinters: () => Promise<string[]>;
  onMainProcessMessage: (callback: (message: string) => void) => void;
}

declare global {
  interface Window {
    electronAPI?: IElectronAPI;
  }
}
