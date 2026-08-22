import jsPDF from "jspdf";
import autoTable from "jspdf-autotable";
import * as XLSX from "xlsx";
import type {
  DailyExpenseReport,
  ExpenseSummary,
  MonthlyExpenseReport,
} from "@/entities/expense";

/**
 * Turns reports into files the owner can keep or email (EXP-031, EXP-032).
 *
 * Both are produced in the browser and handed straight to the download, so nothing is sent to a
 * printer and no round trip to the server is needed for figures already on screen. The PDF is a
 * real document rather than a print job — it lands in Downloads, opens in any viewer and can be
 * attached to an email as it is.
 */

const money = (value: number) =>
  value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const percent = (value: number | null) => (value === null ? "—" : `${value.toFixed(1)}%`);

/** Triggers a browser download for a blob, cleaning up the object URL afterwards. */
function download(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");

  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();

  // Revoked on the next tick: revoking synchronously can cancel the download in some browsers.
  window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}

/** The letterhead every exported report carries, and the y position content should start at. */
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

/** Writes the revenue/expenses/profit block and returns the y position after it. */
function writeSummary(
  doc: jsPDF,
  startY: number,
  summary: { revenue: number; expenses: number; profit: number; profitMargin: number | null },
): number {
  autoTable(doc, {
    startY,
    theme: "plain",
    styles: { fontSize: 10, cellPadding: 1.5 },
    columnStyles: { 0: { fontStyle: "bold", cellWidth: 45 }, 1: { halign: "right", cellWidth: 40 } },
    body: [
      ["Revenue", money(summary.revenue)],
      ["Expenses", money(summary.expenses)],
      ["Profit", money(summary.profit)],
      ["Profit margin", percent(summary.profitMargin)],
    ],
  });

  return (doc as unknown as { lastAutoTable: { finalY: number } }).lastAutoTable.finalY + 8;
}

export function exportDailyReportPdf(report: DailyExpenseReport): void {
  const doc = new jsPDF();

  let y = writeHeader(doc, "Daily Expense Report", report.date);
  y = writeSummary(doc, y, report.summary);

  autoTable(doc, {
    startY: y,
    head: [["Category", "Expenses", "Amount", "Share"]],
    body: report.categories.map((c) => [
      c.categoryName,
      String(c.expenseCount),
      money(c.total),
      `${c.percentageOfTotal.toFixed(1)}%`,
    ]),
    foot: [["Total", "", money(report.summary.expenses), "100.0%"]],
    styles: { fontSize: 9 },
    headStyles: { fillColor: [15, 82, 66] },
    footStyles: { fillColor: [240, 240, 240], textColor: 20, fontStyle: "bold" },
    columnStyles: { 1: { halign: "right" }, 2: { halign: "right" }, 3: { halign: "right" } },
  });

  y = (doc as unknown as { lastAutoTable: { finalY: number } }).lastAutoTable.finalY + 8;

  doc.setFontSize(10);
  doc.text(
    `Compared with ${report.comparison.previousLabel}: ${money(report.comparison.previousTotal)} → ` +
      `${money(report.comparison.currentTotal)} (${report.comparison.change >= 0 ? "+" : ""}` +
      `${money(report.comparison.change)}${
        report.comparison.changePercentage === null
          ? ""
          : `, ${report.comparison.changePercentage >= 0 ? "+" : ""}${report.comparison.changePercentage.toFixed(1)}%`
      })`,
    14,
    y,
  );

  doc.save(`expense-report-${report.date}.pdf`);
}

export function exportMonthlyReportPdf(report: MonthlyExpenseReport): void {
  const doc = new jsPDF();

  let y = writeHeader(doc, "Monthly Expense Report", report.monthLabel);
  y = writeSummary(doc, y, report.summary);

  autoTable(doc, {
    startY: y,
    head: [["Category", "Amount", "Share", "Budget", "Used"]],
    body: report.categories.map((c) => [
      c.categoryName,
      money(c.total),
      `${c.percentageOfTotal.toFixed(1)}%`,
      c.monthlyBudget === null ? "—" : money(c.monthlyBudget),
      c.budgetUsedPercentage === null ? "—" : `${c.budgetUsedPercentage.toFixed(1)}%`,
    ]),
    foot: [["Total", money(report.summary.expenses), "100.0%", "", ""]],
    styles: { fontSize: 9 },
    headStyles: { fillColor: [15, 82, 66] },
    footStyles: { fillColor: [240, 240, 240], textColor: 20, fontStyle: "bold" },
    columnStyles: { 1: { halign: "right" }, 2: { halign: "right" }, 3: { halign: "right" }, 4: { halign: "right" } },
  });

  y = (doc as unknown as { lastAutoTable: { finalY: number } }).lastAutoTable.finalY + 8;

  autoTable(doc, {
    startY: y,
    head: [["Week", "From", "To", "Revenue", "Expenses", "Profit"]],
    body: report.weeklyFigures.map((w) => [
      `Week ${w.weekNumber}`,
      w.startDate,
      w.endDate,
      money(w.revenue),
      money(w.expenses),
      money(w.profit),
    ]),
    styles: { fontSize: 9 },
    headStyles: { fillColor: [15, 82, 66] },
    columnStyles: { 3: { halign: "right" }, 4: { halign: "right" }, 5: { halign: "right" } },
  });

  y = (doc as unknown as { lastAutoTable: { finalY: number } }).lastAutoTable.finalY + 8;

  doc.setFontSize(10);
  doc.text(
    `Compared with ${report.comparison.previousLabel}: ${money(report.comparison.previousTotal)} → ` +
      `${money(report.comparison.currentTotal)}` +
      `${
        report.comparison.largestIncreaseCategory
          ? `. Largest increase: ${report.comparison.largestIncreaseCategory} ` +
            `(+${money(report.comparison.largestIncreaseAmount ?? 0)})`
          : ""
      }`,
    14,
    y,
  );

  doc.save(`expense-report-${report.year}-${String(report.month).padStart(2, "0")}.pdf`);
}

/** Writes a workbook to a real .xlsx file, so figures land in cells ready to be summed. */
function saveWorkbook(sheets: { name: string; rows: unknown[][] }[], fileName: string): void {
  const workbook = XLSX.utils.book_new();

  for (const sheet of sheets) {
    const worksheet = XLSX.utils.aoa_to_sheet(sheet.rows);
    // Sheet names are capped at 31 characters by the format itself.
    XLSX.utils.book_append_sheet(workbook, worksheet, sheet.name.slice(0, 31));
  }

  const buffer = XLSX.write(workbook, { bookType: "xlsx", type: "array" });

  download(
    new Blob([buffer], { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }),
    fileName,
  );
}

export function exportDailyReportExcel(report: DailyExpenseReport): void {
  saveWorkbook(
    [
      {
        name: "Summary",
        rows: [
          ["Daily Expense Report", report.date],
          [],
          ["Revenue", report.summary.revenue],
          ["Expenses", report.summary.expenses],
          ["Profit", report.summary.profit],
          ["Profit margin %", report.summary.profitMargin],
          [],
          ["Category", "Expenses", "Amount", "Share %"],
          ...report.categories.map((c) => [c.categoryName, c.expenseCount, c.total, c.percentageOfTotal]),
        ],
      },
      {
        name: "Expenses",
        rows: [
          ["Number", "Date", "Category", "Description", "Method", "Reference", "Status", "Paid", "Amount"],
          ...report.expenses.map((e) => [
            e.expenseNumber,
            e.expenseDate,
            e.categoryName,
            e.description ?? "",
            e.paymentMethod,
            e.paymentReference ?? "",
            e.status,
            e.isPaid ? "Yes" : "No",
            e.amount,
          ]),
        ],
      },
    ],
    `expense-report-${report.date}.xlsx`,
  );
}

export function exportMonthlyReportExcel(report: MonthlyExpenseReport): void {
  saveWorkbook(
    [
      {
        name: "Summary",
        rows: [
          ["Monthly Expense Report", report.monthLabel],
          [],
          ["Revenue", report.summary.revenue],
          ["Expenses", report.summary.expenses],
          ["Profit", report.summary.profit],
          ["Profit margin %", report.summary.profitMargin],
          [],
          ["Category", "Amount", "Share %", "Budget", "Used %"],
          ...report.categories.map((c) => [
            c.categoryName,
            c.total,
            c.percentageOfTotal,
            c.monthlyBudget,
            c.budgetUsedPercentage,
          ]),
        ],
      },
      {
        name: "Daily",
        rows: [
          ["Date", "Revenue", "Expenses", "Profit"],
          ...report.dailyFigures.map((d) => [d.date, d.revenue, d.expenses, d.profit]),
        ],
      },
      {
        name: "Weekly",
        rows: [
          ["Week", "From", "To", "Revenue", "Expenses", "Profit"],
          ...report.weeklyFigures.map((w) => [
            w.weekNumber,
            w.startDate,
            w.endDate,
            w.revenue,
            w.expenses,
            w.profit,
          ]),
        ],
      },
    ],
    `expense-report-${report.year}-${String(report.month).padStart(2, "0")}.xlsx`,
  );
}

/** Exports whatever the expense list is currently showing, filters and all. */
export function exportExpenseListExcel(expenses: ExpenseSummary[], label: string): void {
  saveWorkbook(
    [
      {
        name: "Expenses",
        rows: [
          ["Number", "Date", "Category", "Description", "Method", "Reference", "Status", "Paid", "Amount"],
          ...expenses.map((e) => [
            e.expenseNumber,
            e.expenseDate,
            e.categoryName,
            e.description ?? "",
            e.paymentMethod,
            e.paymentReference ?? "",
            e.status,
            e.isPaid ? "Yes" : "No",
            e.amount,
          ]),
          [],
          ["Total", "", "", "", "", "", "", "", expenses.reduce((sum, e) => sum + e.amount, 0)],
        ],
      },
    ],
    `expenses-${label}.xlsx`,
  );
}
