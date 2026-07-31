import { http, HttpResponse } from "msw";

export const handlers = [
  http.get("*/api/v1/health", () => {
    return HttpResponse.json({ status: "Healthy" });
  }),
  http.post("*/api/v1/auth/login", () => {
    return HttpResponse.json({
      accessToken: "mock_access_token",
      refreshToken: "mock_refresh_token",
      user: { id: "1", name: "Admin User", email: "admin@pos.com", role: "Admin" },
    });
  }),
];
