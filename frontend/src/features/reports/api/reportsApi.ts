import type { SalesReport } from "@/entities/report";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

export const reportsApi = {
  dailySales: (date: string) =>
    apiService.get<SalesReport>(API_ENDPOINTS.REPORTS.SALES_DAILY, { date }),

  monthlySales: (year: number, month: number) =>
    apiService.get<SalesReport>(API_ENDPOINTS.REPORTS.SALES_MONTHLY, { year, month }),
};
