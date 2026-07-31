import axios from "axios";
import { setupAuthInterceptor } from "./interceptors/authInterceptor";
import { setupRefreshTokenInterceptor } from "./interceptors/refreshTokenInterceptor";
import { setupErrorInterceptor } from "./interceptors/errorInterceptor";

export const axiosClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || "http://localhost:5207/api/v1",
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 10000,
});

setupAuthInterceptor(axiosClient);
setupRefreshTokenInterceptor(axiosClient);
setupErrorInterceptor(axiosClient);
