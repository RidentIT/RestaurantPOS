import type { Recipe, RecipeLineInput } from "@/entities/recipe";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

export const recipesApi = {
  /**
   * Null means the menu item has no recipe yet — a normal, expected state, not an error. The
   * server sends a genuinely empty body for that case, which axios surfaces as `""` rather than
   * JSON `null`, so that's normalised here rather than leaking into every consumer of this call.
   */
  get: (menuItemId: string) =>
    apiService
      .get<Recipe | null>(API_ENDPOINTS.MENU_ITEMS.RECIPE(menuItemId))
      .then((data) => data || null),

  /** Creates the recipe if none exists, or replaces its lines if one already does. */
  upsert: (menuItemId: string, lines: RecipeLineInput[]) =>
    apiService.put<Recipe>(API_ENDPOINTS.MENU_ITEMS.RECIPE(menuItemId), { lines }),

  setEnabled: (menuItemId: string, isEnabled: boolean) =>
    apiService.put<Recipe>(API_ENDPOINTS.MENU_ITEMS.RECIPE_STATUS(menuItemId), { isEnabled }),

  remove: (menuItemId: string) => apiService.delete<void>(API_ENDPOINTS.MENU_ITEMS.RECIPE(menuItemId)),
};
