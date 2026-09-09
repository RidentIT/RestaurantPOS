import QRCode from "qrcode";
import type { KotDocument, ReceiptDocument } from "@/entities/order";
import { renderKotHtml, renderReceiptHtml } from "../lib/thermalTemplates";

export interface PrintOutcome {
  success: boolean;
  message: string;
  /** True when the document went to a preview instead of straight to a printer. */
  previewed?: boolean;
}

/** True when running inside the desktop shell, where silent printing is available. */
export const canPrintSilently = (): boolean => typeof window !== "undefined" && !!window.electronAPI?.printHtml;

async function toQrDataUri(payload: string): Promise<string | null> {
  try {
    return await QRCode.toDataURL(payload, { margin: 0, width: 256, errorCorrectionLevel: "M" });
  } catch {
    // A receipt without its QR code is still a valid receipt, so a failure here must not stop
    // the customer being handed one.
    return null;
  }
}

/**
 * Prints a finished HTML document.
 *
 * In the desktop shell this goes straight to the printer with no dialog, because a cashier
 * confirming an order should not have to dismiss a print prompt every time. In a plain browser
 * — development, or someone opening the till in Chrome — it falls back to an offscreen iframe
 * and the browser's own print dialog. An iframe rather than a popup window, since popups are
 * blocked by default and would silently swallow the receipt.
 */
async function printHtml(html: string, title: string): Promise<PrintOutcome> {
  if (canPrintSilently()) {
    const result = await window.electronAPI!.printHtml(html, { silent: true, widthMm: 80 });

    return { success: result.success, message: result.message };
  }

  return printViaIframe(html, title);
}

function printViaIframe(html: string, title: string): Promise<PrintOutcome> {
  return new Promise((resolve) => {
    const frame = document.createElement("iframe");
    frame.setAttribute("aria-hidden", "true");
    frame.style.position = "fixed";
    frame.style.right = "0";
    frame.style.bottom = "0";
    frame.style.width = "0";
    frame.style.height = "0";
    frame.style.border = "0";
    frame.title = title;

    const cleanUp = () => {
      // Deferred: removing the frame while the print dialog is still reading it cancels the job
      // in some browsers.
      window.setTimeout(() => frame.remove(), 1000);
    };

    frame.onload = () => {
      try {
        frame.contentWindow?.focus();
        frame.contentWindow?.print();
        resolve({ success: true, message: "Sent to the print dialog", previewed: true });
      } catch (error) {
        resolve({
          success: false,
          message: error instanceof Error ? error.message : "The browser refused to print",
        });
      } finally {
        cleanUp();
      }
    };

    document.body.appendChild(frame);
    frame.srcdoc = html;
  });
}

/** Prints a kitchen slip (POS-010, POS-014, POS-020). */
export async function printKot(kot: KotDocument): Promise<PrintOutcome> {
  return printHtml(renderKotHtml(kot), `KOT ${kot.tableNumber ?? "Takeaway"}-${kot.ticketNumber}`);
}

/** Prints a customer receipt (POS-026). */
export async function printReceipt(receipt: ReceiptDocument): Promise<PrintOutcome> {
  const qr = await toQrDataUri(receipt.qrPayload);

  return printHtml(renderReceiptHtml(receipt, qr), `Receipt ${receipt.receiptNumber}`);
}
