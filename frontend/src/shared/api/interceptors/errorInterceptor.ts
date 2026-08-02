import { AxiosError, AxiosInstance } from "axios";
import { toApiError } from "../problem";

/**
 * Normalises every failure into an {@link import('../problem').ApiError} so callers handle one
 * error shape rather than picking apart axios internals and RFC 7807 bodies at each call site.
 *
 * Registered last so it sees only failures the refresh interceptor could not recover from.
 */
export function setupErrorInterceptor(axiosInstance: AxiosInstance): void {
  axiosInstance.interceptors.response.use(
    (response) => response,
    (error: AxiosError) => Promise.reject(toApiError(error)),
  );
}
