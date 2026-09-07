import type { Approval, ModuleDescriptor, Session, User } from "@/entities/user";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

/** Every call the authentication and approval-PIN flows make. */
export const authApi = {
  login: (username: string, password: string) =>
    apiService.post<Session>(API_ENDPOINTS.AUTH.LOGIN, { username, password }),

  logout: (refreshToken: string | null) =>
    apiService.post<void>(API_ENDPOINTS.AUTH.LOGOUT, { refreshToken }),

  /** Re-reads the signed-in user so permissions are never trusted from cached client state. */
  me: () => apiService.get<User>(API_ENDPOINTS.AUTH.ME),

  /** Updates the signed-in user's own display name and email. */
  updateProfile: (fullName: string, email: string | null) =>
    apiService.put<User>(API_ENDPOINTS.AUTH.PROFILE, { fullName, email }),

  changePassword: (currentPassword: string, newPassword: string) =>
    apiService.post<Session>(API_ENDPOINTS.AUTH.CHANGE_PASSWORD, {
      currentPassword,
      newPassword,
    }),

  modules: () => apiService.get<ModuleDescriptor[]>(API_ENDPOINTS.MODULES),

  /** Sets the administrator's approval PIN. Pass `null` to have the server generate one. */
  setApprovalPin: (currentPassword: string, pin: string | null) =>
    apiService.post<{ pin: string }>(API_ENDPOINTS.AUTH.PIN, { currentPassword, pin }),

  clearApprovalPin: () => apiService.delete<void>(API_ENDPOINTS.AUTH.PIN),

  /** Authorises a privileged action with an administrator's PIN. */
  verifyApprovalPin: (pin: string, reason?: string) =>
    apiService.post<Approval>(API_ENDPOINTS.AUTH.VERIFY_PIN, { pin, reason }),
};
