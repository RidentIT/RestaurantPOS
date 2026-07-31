export interface IElectronAPI {
  ping: () => Promise<string>;
  printReceipt: (data: unknown) => Promise<{ success: boolean; message: string }>;
  onMainProcessMessage: (callback: (message: string) => void) => void;
}

declare global {
  interface Window {
    electronAPI?: IElectronAPI;
  }
}
