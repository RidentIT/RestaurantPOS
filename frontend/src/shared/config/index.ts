export const APP_CONFIG = {
  name: "Restaurant POS",
  version: "1.0.0",
  apiBaseUrl: process.env.NEXT_PUBLIC_API_URL || "http://localhost:5207/api/v1",
  wsUrl: process.env.NEXT_PUBLIC_WS_URL || "http://localhost:5207",
} as const;
