import { describe, it, expect } from "vitest";
import { apiService } from "@/shared/api/endpoints";

describe("MSW API Integration Tests", () => {
  it("fetches mock health status successfully", async () => {
    const response = await apiService.get<{ status: string }>("/health");
    expect(response.status).toBe("Healthy");
  });
});
