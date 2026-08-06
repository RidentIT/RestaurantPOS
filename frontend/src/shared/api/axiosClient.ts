import axios from "axios";
import { APP_CONFIG } from "@/shared/config";
import { setupAuthInterceptor } from "./interceptors/authInterceptor";
import { setupErrorInterceptor } from "./interceptors/errorInterceptor";
import { setupRefreshTokenInterceptor } from "./interceptors/refreshTokenInterceptor";

export const axiosClient = axios.create({
  baseURL: APP_CONFIG.apiBaseUrl,
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 15000,
});

/**
 * Called when a session cannot be renewed. The app layer registers a handler that clears store
 * state and routes to sign-in; keeping it injectable stops this module from having to know
 * about the router or the Redux store.
 */
let sessionExpiredHandler: (() => void) | null = null;

export function onSessionExpired(handler: () => void): void {
  sessionExpiredHandler = handler;
}

// Order matters. Auth stamps the token on the way out; on the way back, refresh gets first
// look at a 401 so it can retry, and the error interceptor normalises whatever is left.
setupAuthInterceptor(axiosClient);
setupRefreshTokenInterceptor(axiosClient, () => sessionExpiredHandler?.());
setupErrorInterceptor(axiosClient);
