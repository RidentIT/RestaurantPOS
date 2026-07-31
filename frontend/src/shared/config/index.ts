export const APP_CONFIG = {
  name: "Restaurant POS",
  version: "1.0.0",
  apiBaseUrl: import.meta.env.VITE_API_URL || "http://localhost:5207/api/v1",
  wsUrl: import.meta.env.VITE_WS_URL || "http://localhost:5207",
} as const;
