import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { RecipeLineInput } from "@/entities/recipe";
import { recipesApi } from "../api/recipesApi";

const MENU_ITEMS_KEY = "menu-items";
const recipeKey = (menuItemId: string) => ["recipe", menuItemId];

/** The recipe for one menu item. `data` is `null` (not an error) when it has none. */
export function useRecipe(menuItemId: string | undefined) {
  return useQuery({
    queryKey: recipeKey(menuItemId ?? ""),
    queryFn: () => recipesApi.get(menuItemId!),
    enabled: !!menuItemId,
  });
}

export function useRecipeMutations(menuItemId: string) {
  const queryClient = useQueryClient();

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: recipeKey(menuItemId) });
    // hasRecipe on the menu item list/detail depends on whether a recipe now exists.
    queryClient.invalidateQueries({ queryKey: [MENU_ITEMS_KEY] });
  };

  const upsert = useMutation({
    mutationFn: (lines: RecipeLineInput[]) => recipesApi.upsert(menuItemId, lines),
    onSuccess: invalidate,
  });

  const setEnabled = useMutation({
    mutationFn: (isEnabled: boolean) => recipesApi.setEnabled(menuItemId, isEnabled),
    onSuccess: invalidate,
  });

  const remove = useMutation({
    mutationFn: () => recipesApi.remove(menuItemId),
    onSuccess: invalidate,
  });

  return { upsert, setEnabled, remove };
}
