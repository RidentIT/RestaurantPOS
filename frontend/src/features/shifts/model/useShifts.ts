import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { CheckOutPayload, Shift } from "@/entities/shift";
import { shiftsApi } from "../api/shiftsApi";

export const SHIFTS_KEY = "shifts";

/** The signed-in cashier's open shift with its running totals — kept fresh while the till is open. */
export function useCurrentShift() {
  return useQuery({
    queryKey: [SHIFTS_KEY, "current"],
    queryFn: shiftsApi.current,
    refetchInterval: 30_000,
  });
}

/** The signed-in cashier's shifts from the last seven days. */
export function useMyShifts() {
  return useQuery({ queryKey: [SHIFTS_KEY, "mine"], queryFn: shiftsApi.mine });
}

/** Every cashier's shifts in a date range, for the owner. */
export function useAllShifts(from: string, to: string) {
  return useQuery({
    queryKey: [SHIFTS_KEY, "all", from, to],
    queryFn: () => shiftsApi.all(from, to),
  });
}

export function useShiftDetail(id: string | null) {
  return useQuery({
    queryKey: [SHIFTS_KEY, "detail", id],
    queryFn: () => shiftsApi.byId(id!),
    enabled: !!id,
  });
}

export function useShiftMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: [SHIFTS_KEY] });

  const checkIn = useMutation<Shift, Error, number>({
    mutationFn: shiftsApi.checkIn,
    onSuccess: invalidate,
  });

  const checkOut = useMutation<Shift, Error, CheckOutPayload>({
    mutationFn: shiftsApi.checkOut,
    onSuccess: invalidate,
  });

  const forceClose = useMutation<Shift, Error, { id: string; note: string | null }>({
    mutationFn: ({ id, note }) => shiftsApi.forceClose(id, note),
    onSuccess: invalidate,
  });

  return { checkIn, checkOut, forceClose };
}
