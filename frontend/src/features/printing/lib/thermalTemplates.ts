import type { KotDocument, ReceiptDocument } from "@/entities/order";

/**
 * Renders print documents as standalone 80mm HTML pages.
 *
 * Thermal roll paper is 80mm wide with no fixed height, so the page is laid out in millimetres
 * against a monospace face and left to run as long as it needs. Everything is inlined into one
 * document because it is handed straight to a printer window that loads nothing else.
 */

const KOT_KIND_HEADINGS: Record<KotDocument["kind"], string> = {
  New: "NEW ORDER",
  Addition: "ADDED ITEMS",
  Modification: "QUANTITY CHANGED",
  Cancellation: "** CANCELLED **",
};

/** Escapes text bound for the print document. Order data is user-entered and untrusted. */
function escapeHtml(value: string): string {
  return value
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

const money = (value: number) => value.toFixed(2);

function formatDateTime(iso: string): string {
  const date = new Date(iso);

  return `${date.toLocaleDateString()} ${date.toLocaleTimeString()}`;
}

const PAGE_STYLES = `
  @page { size: 80mm auto; margin: 0; }
  * { box-sizing: border-box; }
  body {
    width: 80mm;
    margin: 0;
    padding: 4mm 3mm;
    font-family: "Consolas", "Courier New", monospace;
    font-size: 11px;
    line-height: 1.45;
    color: #000;
    background: #fff;
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
  }
  .center { text-align: center; }
  .right { text-align: right; }
  .bold { font-weight: 700; }
  .lg { font-size: 15px; }
  .xl { font-size: 19px; }
  .rule { border-top: 1px dashed #000; margin: 2mm 0; }
  .solid { border-top: 1px solid #000; margin: 2mm 0; }
  table { width: 100%; border-collapse: collapse; }
  td { vertical-align: top; padding: 0.4mm 0; }
  .muted { font-size: 10px; }
  .note { padding-left: 4mm; font-style: italic; }
  .row { display: flex; justify-content: space-between; gap: 2mm; }
`;

function page(title: string, body: string): string {
  return `<!doctype html>
<html>
<head><meta charset="utf-8"><title>${escapeHtml(title)}</title><style>${PAGE_STYLES}</style></head>
<body>${body}</body>
</html>`;
}

/**
 * A kitchen slip. Deliberately sparse and large: it is read at arm's length across a hot pass,
 * so the table number and quantities carry the layout and prices appear nowhere at all — the
 * kitchen has no use for them.
 */
export function renderKotHtml(kot: KotDocument): string {
  const lines = kot.lines
    .map(
      (line) => `
        <tr>
          <td class="bold xl" style="width: 12mm;">${line.quantity}&times;</td>
          <td class="bold lg">${escapeHtml(line.menuItemName)}</td>
        </tr>
        ${
          line.specialInstructions
            ? `<tr><td></td><td class="note">${escapeHtml(line.specialInstructions)}</td></tr>`
            : ""
        }
        ${line.note ? `<tr><td></td><td class="bold">** ${escapeHtml(line.note)} **</td></tr>` : ""}`,
    )
    .join("");

  const body = `
    <div class="center bold lg">${escapeHtml(KOT_KIND_HEADINGS[kot.kind])}</div>
    <div class="solid"></div>
    <div class="center bold xl">TABLE ${escapeHtml(kot.tableNumber)}</div>
    <div class="center">Order #${String(kot.orderNumber ?? 0).padStart(3, "0")} &middot; KOT-${kot.ticketNumber}</div>
    <div class="rule"></div>
    <table>${lines}</table>
    <div class="rule"></div>
    <div class="muted">${escapeHtml(formatDateTime(kot.printedAtUtc))}</div>
    <div class="muted">Cashier: ${escapeHtml(kot.cashierName)}</div>
    ${kot.printCount > 1 ? `<div class="center bold">-- REPRINT #${kot.printCount} --</div>` : ""}
  `;

  return page(`KOT ${kot.tableNumber}-${kot.ticketNumber}`, body);
}

/** A customer receipt, carrying the restaurant's details, the itemised bill and the tenders. */
export function renderReceiptHtml(receipt: ReceiptDocument, qrDataUri: string | null): string {
  const lines = receipt.lines
    .map(
      (line) => `
        <tr>
          <td>${escapeHtml(line.menuItemName)}</td>
          <td class="right" style="width: 8mm;">${line.quantity}</td>
          <td class="right" style="width: 16mm;">${money(line.unitPrice)}</td>
          <td class="right" style="width: 18mm;">${money(line.lineTotal)}</td>
        </tr>`,
    )
    .join("");

  const payments = receipt.payments
    .map(
      (payment) => `
        <div class="row"><span>${escapeHtml(payment.method)}</span><span>${money(payment.amount)}</span></div>`,
    )
    .join("");

  const address = [receipt.addressLine1, receipt.addressLine2, receipt.city]
    .filter(Boolean)
    .map((part) => `<div>${escapeHtml(part as string)}</div>`)
    .join("");

  const body = `
    <div class="center bold lg">${escapeHtml(receipt.restaurantName)}</div>
    <div class="center">${address}</div>
    ${receipt.phone ? `<div class="center">${escapeHtml(receipt.phone)}</div>` : ""}
    <div class="solid"></div>
    <div class="row"><span>Receipt #</span><span class="bold">${escapeHtml(receipt.receiptNumber)}</span></div>
    <div class="row"><span>Date</span><span>${escapeHtml(formatDateTime(receipt.issuedAtUtc))}</span></div>
    <div class="row"><span>Order #</span><span>${String(receipt.orderNumber ?? 0).padStart(3, "0")}</span></div>
    <div class="row"><span>Table</span><span>${escapeHtml(receipt.tableNumber)}</span></div>
    <div class="row"><span>Cashier</span><span>${escapeHtml(receipt.cashierName)}</span></div>
    <div class="rule"></div>
    <table>
      <tr class="bold">
        <td>Item</td><td class="right">Qty</td><td class="right">Price</td><td class="right">Total</td>
      </tr>
      ${lines}
    </table>
    <div class="rule"></div>
    <div class="row"><span>Subtotal</span><span>${money(receipt.subtotal)}</span></div>
    <div class="row"><span>Discount</span><span>${money(receipt.discountAmount)}</span></div>
    <div class="row"><span>Tax / GST</span><span>${money(receipt.taxAmount)}</span></div>
    <div class="solid"></div>
    <div class="row bold lg"><span>TOTAL</span><span>${money(receipt.total)}</span></div>
    <div class="rule"></div>
    <div class="bold">PAYMENT</div>
    ${payments}
    ${receipt.changeGiven > 0 ? `<div class="row bold"><span>Change</span><span>${money(receipt.changeGiven)}</span></div>` : ""}
    <div class="rule"></div>
    ${qrDataUri ? `<div class="center"><img src="${qrDataUri}" alt="" style="width: 28mm; height: 28mm;"></div>` : ""}
    <div class="center bold">Thank You! Come Again!</div>
    ${receipt.printCount > 1 ? `<div class="center muted">-- REPRINT #${receipt.printCount} --</div>` : ""}
  `;

  return page(`Receipt ${receipt.receiptNumber}`, body);
}
