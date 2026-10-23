import type { CheckOutPayload, CurrentShift, Shift, ShiftDetail } from "@/entities/shift";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

export const shiftsApi = {
  checkIn: (openingCash: number) => apiService.post<Shift>(API_ENDPOINTS.SHIFTS.CHECK_IN, { openingCash }),

  checkOut: (payload: CheckOutPayload) => apiService.post<Shift>(API_ENDPOINTS.SHIFTS.CHECK_OUT, payload),

  current: () => apiService.get<CurrentShift>(API_ENDPOINTS.SHIFTS.CURRENT),

  /** The signed-in cashier's own shifts from the last seven days. */
  mine: () => apiService.get<Shift[]>(API_ENDPOINTS.SHIFTS.MINE),

  /** Every cashier's shifts — the owner's view. */
  all: (from?: string, to?: string) => apiService.get<Shift[]>(API_ENDPOINTS.SHIFTS.BASE, { from, to }),

  byId: (id: string) => apiService.get<ShiftDetail>(API_ENDPOINTS.SHIFTS.BY_ID(id)),

  forceClose: (id: string, note: string | null) =>
    apiService.post<Shift>(API_ENDPOINTS.SHIFTS.FORCE_CLOSE(id), { closingCash: null, note }),
};
