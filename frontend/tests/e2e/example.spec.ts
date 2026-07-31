import { test, expect } from "@playwright/test";

test("homepage has title and renders header", async ({ page }) => {
  await page.goto("/");
  await expect(page).toHaveTitle(/Restaurant POS System/i);
  await expect(page.locator("h1")).toContainText("Restaurant POS Enterprise System");
});
