import { useQuery } from "@tanstack/react-query";
import { reportsApi } from "../api/reportsApi";

export const SALES_REPORT_KEY = "sales-report";

export function useDailySalesReport(date: string) {
  return useQuery({
    queryKey: [SALES_REPORT_KEY, "daily", date],
    queryFn: () => reportsApi.dailySales(date),
  });
}

export function useMonthlySalesReport(year: number, month: number) {
  return useQuery({
    queryKey: [SALES_REPORT_KEY, "monthly", year, month],
    queryFn: () => reportsApi.monthlySales(year, month),
  });
}
