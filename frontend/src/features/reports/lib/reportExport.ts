import jsPDF from "jspdf";
import autoTable from "jspdf-autotable";
import * as XLSX from "xlsx";
import type { SalesReport } from "@/entities/report";

/**
 * Turns a sales report into files the owner can keep or email — the same PDF/Excel treatment the
 * expense reports get (EXP-031, EXP-032), applied to what sold rather than what was spent.
 */

const money = (value: number) =>
  value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

function download(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");

  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();

  window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}

function lastTableY(doc: jsPDF): number {
  return (doc as unknown as { lastAutoTable: { finalY: number } }).lastAutoTable.finalY + 8;
}

function writeHeader(doc: jsPDF, title: string, subtitle: string): number {
  doc.setFontSize(16);
  doc.setFont("helvetica", "bold");
  doc.text("Sri Lakshmi Family Restaurant", 14, 18);

  doc.setFontSize(12);
  doc.text(title, 14, 27);

  doc.setFontSize(10);
  doc.setFont("helvetica", "normal");
  doc.setTextColor(110);
  doc.text(subtitle, 14, 33);
  doc.setTextColor(0);

  return 40;
}

export function exportSalesReportPdf(report: SalesReport): void {
  const doc = new jsPDF();

  let y = writeHeader(doc, "Sales Report", report.periodLabel);

  autoTable(doc, {
    startY: y,
    theme: "plain",
    styles: { fontSize: 10, cellPadding: 1.5 },
    columnStyles: { 0: { fontStyle: "bold", cellWidth: 45 }, 1: { halign: "right", cellWidth: 40 } },
    body: [
      ["Sales", money(report.summary.revenue)],
      ["Orders", String(report.summary.orderCount)],
      ["Average order", report.summary.averageOrderValue === null ? "—" : money(report.summary.averageOrderValue)],
      ["Expenses", money(report.summary.expenses)],
      ["Profit", money(report.summary.profit)],
    ],
  });
  y = lastTableY(doc);

  autoTable(doc, {
    startY: y,
    head: [["Item", "Category", "Qty sold", "Revenue"]],
    body: report.topItems.map((i) => [i.name, i.category, String(i.quantitySold), money(i.revenue)]),
    styles: { fontSize: 9 },
    headStyles: { fillColor: [15, 82, 66] },
    columnStyles: { 2: { halign: "right" }, 3: { halign: "right" } },
  });
  y = lastTableY(doc);

  autoTable(doc, {
    startY: y,
    head: [["Payment method", "Amount", "Share", "Count"]],
    body: report.paymentMethods.map((p) => [
      p.method,
      money(p.amount),
      `${p.percentageOfTotal.toFixed(1)}%`,
      String(p.count),
    ]),
    styles: { fontSize: 9 },
    headStyles: { fillColor: [15, 82, 66] },
    columnStyles: { 1: { halign: "right" }, 2: { halign: "right" }, 3: { halign: "right" } },
  });
  y = lastTableY(doc);

  doc.setFontSize(10);
  doc.text(
    `Discounts: ${money(report.discounts.totalDiscountGiven)} given across ` +
      `${report.discounts.ordersWithDiscount} of ${report.discounts.totalOrders} orders ` +
      `(${report.discounts.percentageOfOrdersDiscounted.toFixed(1)}%).`,
    14,
    y,
  );

  doc.save(`sales-report-${report.from}${report.from === report.to ? "" : `-to-${report.to}`}.pdf`);
}

function saveWorkbook(sheets: { name: string; rows: unknown[][] }[], fileName: string): void {
  const workbook = XLSX.utils.book_new();

  for (const sheet of sheets) {
    const worksheet = XLSX.utils.aoa_to_sheet(sheet.rows);
    XLSX.utils.book_append_sheet(workbook, worksheet, sheet.name.slice(0, 31));
  }

  const buffer = XLSX.write(workbook, { bookType: "xlsx", type: "array" });

  download(
    new Blob([buffer], { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }),
    fileName,
  );
}

export function exportSalesReportExcel(report: SalesReport): void {
  saveWorkbook(
    [
      {
        name: "Summary",
        rows: [
          ["Sales Report", report.periodLabel],
          [],
          ["Sales", report.summary.revenue],
          ["Orders", report.summary.orderCount],
          ["Average order", report.summary.averageOrderValue],
          ["Expenses", report.summary.expenses],
          ["Profit", report.summary.profit],
          [],
          ["Discount given", report.discounts.totalDiscountGiven],
          ["Orders discounted", report.discounts.ordersWithDiscount],
          ["Total orders", report.discounts.totalOrders],
        ],
      },
      {
        name: "Top items",
        rows: [
          ["Item", "Category", "Qty sold", "Revenue"],
          ...report.topItems.map((i) => [i.name, i.category, i.quantitySold, i.revenue]),
        ],
      },
      {
        name: "Categories",
        rows: [
          ["Category", "Revenue", "Share %", "Qty sold"],
          ...report.categories.map((c) => [c.category, c.revenue, c.percentageOfTotal, c.quantitySold]),
        ],
      },
      {
        name: "Payment methods",
        rows: [
          ["Method", "Amount", "Share %", "Count"],
          ...report.paymentMethods.map((p) => [p.method, p.amount, p.percentageOfTotal, p.count]),
        ],
      },
      {
        name: "Hourly pattern",
        rows: [
          ["Hour", "Revenue", "Orders"],
          ...report.hourlyPattern.map((h) => [h.hour, h.revenue, h.orderCount]),
        ],
      },
      {
        name: "Day of week",
        rows: [
          ["Day", "Revenue", "Orders"],
          ...report.dayOfWeekPattern.map((d) => [d.day, d.revenue, d.orderCount]),
        ],
      },
    ],
    `sales-report-${report.from}${report.from === report.to ? "" : `-to-${report.to}`}.xlsx`,
  );
}
