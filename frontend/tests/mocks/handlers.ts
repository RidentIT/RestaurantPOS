import { http, HttpResponse } from "msw";
import { adminPendingChange, moduleCatalog, sessionFor } from "./fixtures";

const API = "*/api/v1";

/**
 * Default handlers matching the real API's contracts. Individual tests override specific
 * routes with `server.use(...)` for the scenario under test, e.g. a login failure.
 */
export const handlers = [
  http.get("*/health", () => HttpResponse.json({ status: "Healthy" })),

  http.post(`${API}/auth/login`, () => HttpResponse.json(sessionFor(adminPendingChange))),

  http.get(`${API}/auth/me`, () => HttpResponse.json(adminPendingChange)),

  http.get(`${API}/modules`, () => HttpResponse.json(moduleCatalog)),

  http.get(`${API}/users`, () => HttpResponse.json([])),
];
