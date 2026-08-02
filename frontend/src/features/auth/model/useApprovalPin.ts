import { useMutation } from "@tanstack/react-query";
import type { Approval } from "@/entities/user";
import { useAppDispatch } from "@/shared/store";
import { authApi } from "../api/authApi";
import { userLoaded } from "./authSlice";

/**
 * Manages the signed-in administrator's own approval PIN.
 *
 * `hasApprovalPin` lives on the cached Redux user, not a react-query cache, so both mutations
 * re-fetch `/auth/me` on success and dispatch the result — otherwise the UI would keep showing
 * the PIN's old presence/absence until the next full page load.
 */
export function useApprovalPin() {
  const dispatch = useAppDispatch();

  const refreshUser = async () => {
    const user = await authApi.me();
    dispatch(userLoaded(user));
  };

  const set = useMutation({
    mutationFn: ({ currentPassword, pin }: { currentPassword: string; pin: string | null }) =>
      authApi.setApprovalPin(currentPassword, pin),
    onSuccess: refreshUser,
  });

  const clear = useMutation({
    mutationFn: () => authApi.clearApprovalPin(),
    onSuccess: refreshUser,
  });

  return { set, clear };
}

/**
 * Requests an administrator's authorisation for a privileged action.
 *
 * This is the hook other modules will reuse: a cashier attempting to void an order calls it,
 * an administrator types their PIN, and the resolved {@link Approval} records who approved it.
 */
export function useRequestApproval() {
  return useMutation<Approval, Error, { pin: string; reason?: string }>({
    mutationFn: ({ pin, reason }) => authApi.verifyApprovalPin(pin, reason),
  });
}
