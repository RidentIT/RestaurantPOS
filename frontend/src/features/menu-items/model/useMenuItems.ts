import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { CreateMenuItemPayload, MenuItem, MenuItemFilters, UpdateMenuItemPayload } from "@/entities/menu-item";
import { menuItemsApi } from "../api/menuItemsApi";

const MENU_ITEMS_KEY = "menu-items";
const MENU_CATEGORIES_KEY = "menu-categories";

export function useMenuItems(filters: MenuItemFilters = {}) {
  return useQuery({
    queryKey: [MENU_ITEMS_KEY, filters],
    queryFn: () => menuItemsApi.list(filters),
    placeholderData: (previous) => previous,
  });
}

/** One menu item, for the detail page — reachable directly by URL, not just from the list. */
export function useMenuItem(id: string | undefined) {
  return useQuery({
    queryKey: [MENU_ITEMS_KEY, id],
    queryFn: () => menuItemsApi.byId(id!),
    enabled: !!id,
  });
}

/** Every category name worth offering in the Add/Edit menu item picker. */
export function useMenuCategories() {
  return useQuery({
    queryKey: [MENU_CATEGORIES_KEY],
    queryFn: menuItemsApi.listCategories,
  });
}

export function useCreateMenuCategory() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (name: string) => menuItemsApi.createCategory(name),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [MENU_CATEGORIES_KEY] }),
  });
}

export function useDeleteMenuCategory() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (name: string) => menuItemsApi.deleteCategory(name),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [MENU_CATEGORIES_KEY] }),
  });
}

export function useMenuItemMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: [MENU_ITEMS_KEY] });

  const create = useMutation<MenuItem, Error, CreateMenuItemPayload>({
    mutationFn: menuItemsApi.create,
    onSuccess: invalidate,
  });

  const update = useMutation<MenuItem, Error, { id: string; payload: UpdateMenuItemPayload }>({
    mutationFn: ({ id, payload }) => menuItemsApi.update(id, payload),
    onSuccess: invalidate,
  });

  const setActive = useMutation<MenuItem, Error, { id: string; isActive: boolean }>({
    mutationFn: ({ id, isActive }) => menuItemsApi.setActive(id, isActive),
    onSuccess: invalidate,
  });

  return { create, update, setActive };
}
