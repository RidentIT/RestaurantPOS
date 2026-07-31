import { AxiosInstance, InternalAxiosRequestConfig } from "axios";

export function setupAuthInterceptor(axiosInstance: AxiosInstance): void {
  axiosInstance.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
      const token = typeof window !== "undefined" ? localStorage.getItem("access_token") : null;
      if (token && config.headers) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    },
    (error) => Promise.reject(error)
  );
}
