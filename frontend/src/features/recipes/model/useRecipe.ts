import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { RecipeLineInput } from "@/entities/recipe";
import { recipesApi } from "../api/recipesApi";

const MENU_ITEMS_KEY = "menu-items";
const recipeKey = (menuItemVariantId: string) => ["recipe", menuItemVariantId];

/** The recipe for one menu item size. `data` is `null` (not an error) when it has none. */
export function useRecipe(menuItemVariantId: string | undefined) {
  return useQuery({
    queryKey: recipeKey(menuItemVariantId ?? ""),
    queryFn: () => recipesApi.get(menuItemVariantId!),
    enabled: !!menuItemVariantId,
  });
}

export function useRecipeMutations(menuItemVariantId: string) {
  const queryClient = useQueryClient();

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: recipeKey(menuItemVariantId) });
    // hasRecipe on the menu item's variants depends on whether a recipe now exists.
    queryClient.invalidateQueries({ queryKey: [MENU_ITEMS_KEY] });
  };

  const upsert = useMutation({
    mutationFn: (lines: RecipeLineInput[]) => recipesApi.upsert(menuItemVariantId, lines),
    onSuccess: invalidate,
  });

  const setEnabled = useMutation({
    mutationFn: (isEnabled: boolean) => recipesApi.setEnabled(menuItemVariantId, isEnabled),
    onSuccess: invalidate,
  });

  const remove = useMutation({
    mutationFn: () => recipesApi.remove(menuItemVariantId),
    onSuccess: invalidate,
  });

  return { upsert, setEnabled, remove };
}
