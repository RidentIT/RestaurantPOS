import { AxiosInstance, AxiosError } from "axios";

export function setupErrorInterceptor(axiosInstance: AxiosInstance): void {
  axiosInstance.interceptors.response.use(
    (response) => response,
    (error: AxiosError) => {
      if (!error.response) {
        console.error("Network Error or Server Unreachable");
      } else {
        const { status, data } = error.response;
        console.error(`[API Error ${status}]:`, data);
      }
      return Promise.reject(error);
    }
  );
}
