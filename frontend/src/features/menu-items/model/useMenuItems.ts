import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { CreateMenuItemPayload, MenuItem, MenuItemFilters, UpdateMenuItemPayload } from "@/entities/menu-item";
import { menuItemsApi } from "../api/menuItemsApi";

const MENU_ITEMS_KEY = "menu-items";

export function useMenuItems(filters: MenuItemFilters = {}) {
  return useQuery({
    queryKey: [MENU_ITEMS_KEY, filters],
    queryFn: () => menuItemsApi.list(filters),
    placeholderData: (previous) => previous,
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
