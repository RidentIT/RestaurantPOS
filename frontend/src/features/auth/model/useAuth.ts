import { useCallback, useEffect } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { ModuleKey } from "@/entities/user";
import { canAccessModule } from "@/entities/user";
import { tokenStorage } from "@/shared/api/tokenStorage";
import { useAppDispatch, useAppSelector } from "@/shared/store";
import { authApi } from "../api/authApi";
import { authenticating, sessionEnded, sessionEstablished, userLoaded } from "./authSlice";

/**
 * Restores a session from the stored token on start-up.
 *
 * Mounted once by the app shell. The stored profile is never trusted: the server is asked who
 * the token belongs to and what they may open.
 */
export function useRestoreSession(): void {
  const dispatch = useAppDispatch();
  const status = useAppSelector((s) => s.auth.status);

  useEffect(() => {
    if (status !== "idle") return;

    if (!tokenStorage.getAccessToken()) {
      dispatch(sessionEnded());
      return;
    }

    dispatch(authenticating());

    authApi
      .me()
      .then((user) => dispatch(userLoaded(user)))
      .catch(() => dispatch(sessionEnded()));
  }, [dispatch, status]);
}

/** The signed-in user and helpers derived from them. */
export function useAuth() {
  const { user, status } = useAppSelector((s) => s.auth);

  const can = useCallback((module: ModuleKey) => canAccessModule(user, module), [user]);

  return {
    user,
    status,
    isAuthenticated: status === "authenticated",
    /** True while the stored token is still being checked. */
    isResolving: status === "idle" || status === "authenticating",
    isAdmin: user?.role === "Admin",
    mustChangePassword: user?.mustChangePassword ?? false,
    can,
  };
}

/** Signs in with a username and password. */
export function useLogin() {
  const dispatch = useAppDispatch();

  return useMutation({
    mutationFn: ({ username, password }: { username: string; password: string }) =>
      authApi.login(username, password),
    onSuccess: (session) => dispatch(sessionEstablished(session)),
  });
}

/** Updates the signed-in user's own display name and email. */
export function useUpdateProfile() {
  const dispatch = useAppDispatch();

  return useMutation({
    mutationFn: ({ fullName, email }: { fullName: string; email: string | null }) =>
      authApi.updateProfile(fullName, email),
    onSuccess: (user) => dispatch(userLoaded(user)),
  });
}

/** Changes the signed-in user's password and adopts the fresh session it returns. */
export function useChangePassword() {
  const dispatch = useAppDispatch();

  return useMutation({
    mutationFn: ({
      currentPassword,
      newPassword,
    }: {
      currentPassword: string;
      newPassword: string;
    }) => authApi.changePassword(currentPassword, newPassword),
    onSuccess: (session) => dispatch(sessionEstablished(session)),
  });
}

/** Signs out, clearing local state even if the server call fails. */
export function useLogout() {
  const dispatch = useAppDispatch();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => authApi.logout(tokenStorage.getRefreshToken()),
    // Local state is cleared either way: a failed revoke must not strand the user signed in.
    onSettled: () => {
      dispatch(sessionEnded());
      queryClient.clear();
    },
  });
}

/** The module catalog, used for navigation and the permission editor. */
export function useModules() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: ["modules"],
    queryFn: authApi.modules,
    enabled: isAuthenticated,
    // The catalog only changes when the application itself is updated.
    staleTime: Infinity,
  });
}
