import { describe, expect, it, afterEach } from "vitest";
import { http, HttpResponse } from "msw";
import { recipesApi } from "@/features/recipes/api/recipesApi";
import { server } from "@tests/mocks/server";

const MENU_ITEM_VARIANT_ID = "11111111-1111-1111-1111-111111111111";

describe("recipesApi.get", () => {
  afterEach(() => server.resetHandlers());

  it("normalises the server's empty body (no recipe yet) to null", async () => {
    // The backend sends a genuinely empty body for "no recipe" rather than the JSON literal
    // `null`, which is exactly the shape that broke the recipe editor before this was fixed:
    // axios turns an empty body into `""`, and `""?.lines` is not short-circuited by optional
    // chaining the way `null?.lines` is.
    server.use(
      http.get(
        `*/api/v1/menu-items/variants/${MENU_ITEM_VARIANT_ID}/recipe`,
        () => new HttpResponse("", { status: 200 }),
      ),
    );

    const result = await recipesApi.get(MENU_ITEM_VARIANT_ID);

    expect(result).toBeNull();
  });

  it("passes a real recipe straight through", async () => {
    const recipe = {
      id: "22222222-2222-2222-2222-222222222222",
      menuItemVariantId: MENU_ITEM_VARIANT_ID,
      isEnabled: true,
      lines: [{ rawMaterialId: "r1", rawMaterialName: "Rice", unitOfMeasurement: "Kilogram", quantity: 0.25 }],
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: null,
    };

    server.use(
      http.get(`*/api/v1/menu-items/variants/${MENU_ITEM_VARIANT_ID}/recipe`, () => HttpResponse.json(recipe)),
    );

    const result = await recipesApi.get(MENU_ITEM_VARIANT_ID);

    expect(result).toEqual(recipe);
  });
});
