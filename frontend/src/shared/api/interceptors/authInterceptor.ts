import { AxiosInstance, InternalAxiosRequestConfig } from "axios";
import { tokenStorage } from "../tokenStorage";

/** Attaches the current access token to every outgoing request. */
export function setupAuthInterceptor(axiosInstance: AxiosInstance): void {
  axiosInstance.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
      const token = tokenStorage.getAccessToken();

      if (token && config.headers) {
        config.headers.Authorization = `Bearer ${token}`;
      }

      return config;
    },
    (error) => Promise.reject(error),
  );
}
