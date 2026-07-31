import { test, expect } from "@playwright/test";

test("homepage has title and renders header", async ({ page }) => {
  await page.goto("/");
  // Title comes from index.html <title> tag
  await expect(page).toHaveTitle(/Restaurant POS Terminal/i);
  // h1 text comes from src/app/App.tsx
  await expect(page.locator("h1")).toContainText("Restaurant POS System");
});
