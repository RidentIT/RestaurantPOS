import { AxiosError, AxiosInstance, InternalAxiosRequestConfig } from "axios";
import { tokenStorage } from "../tokenStorage";

interface QueuedRequest {
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}

/**
 * Endpoints that must never trigger a refresh attempt.
 *
 * A 401 from sign-in means "wrong password", not "expired session". A 401 from the refresh
 * endpoint itself means the refresh token is dead, and retrying it would recurse forever.
 */
const NON_REFRESHABLE_PATHS = ["/auth/login", "/auth/refresh", "/auth/logout"];

const isNonRefreshable = (url: string | undefined): boolean =>
  !!url && NON_REFRESHABLE_PATHS.some((path) => url.includes(path));

/**
 * Transparently renews an expired access token and replays the request that hit the 401.
 *
 * Concurrent failures queue behind a single refresh: because the server rotates refresh tokens,
 * firing several refreshes at once would consume tokens it has already invalidated and drop the
 * session entirely.
 */
export function setupRefreshTokenInterceptor(
  axiosInstance: AxiosInstance,
  onSessionExpired?: () => void,
): void {
  let isRefreshing = false;
  let queue: QueuedRequest[] = [];

  const flushQueue = (error: unknown, token: string | null): void => {
    for (const pending of queue) {
      if (token) {
        pending.resolve(token);
      } else {
        pending.reject(error);
      }
    }

    queue = [];
  };

  const failSession = (error: unknown): Promise<never> => {
    tokenStorage.clear();
    flushQueue(error, null);
    onSessionExpired?.();

    return Promise.reject(error);
  };

  axiosInstance.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
      const request = error.config as
        | (InternalAxiosRequestConfig & { _retried?: boolean })
        | undefined;

      const shouldAttemptRefresh =
        error.response?.status === 401 &&
        !!request &&
        !request._retried &&
        !isNonRefreshable(request.url);

      if (!shouldAttemptRefresh) {
        return Promise.reject(error);
      }

      request._retried = true;

      // A refresh is already in flight, so wait for it rather than starting another.
      if (isRefreshing) {
        return new Promise<string>((resolve, reject) => {
          queue.push({ resolve, reject });
        }).then((token) => {
          if (request.headers) {
            request.headers.Authorization = `Bearer ${token}`;
          }

          return axiosInstance(request);
        });
      }

      const refreshToken = tokenStorage.getRefreshToken();
      if (!refreshToken) {
        return failSession(error);
      }

      isRefreshing = true;

      try {
        const { data } = await axiosInstance.post("/auth/refresh", { refreshToken });

        tokenStorage.save(data.accessToken, data.refreshToken);
        flushQueue(null, data.accessToken);

        if (request.headers) {
          request.headers.Authorization = `Bearer ${data.accessToken}`;
        }

        return await axiosInstance(request);
      } catch (refreshError) {
        return failSession(refreshError);
      } finally {
        isRefreshing = false;
      }
    },
  );
}
