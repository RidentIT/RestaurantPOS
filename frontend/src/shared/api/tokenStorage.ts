const ACCESS_TOKEN_KEY = "access_token";
const REFRESH_TOKEN_KEY = "refresh_token";

const isBrowser = () => typeof window !== "undefined" && !!window.localStorage;

/**
 * The single place tokens are read from and written to.
 *
 * Centralised so the axios interceptors, the auth slice and the sign-out path cannot drift out
 * of step over key names or clean-up.
 */
export const tokenStorage = {
  getAccessToken(): string | null {
    return isBrowser() ? window.localStorage.getItem(ACCESS_TOKEN_KEY) : null;
  },

  getRefreshToken(): string | null {
    return isBrowser() ? window.localStorage.getItem(REFRESH_TOKEN_KEY) : null;
  },

  save(accessToken: string, refreshToken: string): void {
    if (!isBrowser()) return;

    window.localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
    window.localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  },

  clear(): void {
    if (!isBrowser()) return;

    window.localStorage.removeItem(ACCESS_TOKEN_KEY);
    window.localStorage.removeItem(REFRESH_TOKEN_KEY);
  },
};
