/** A sellable dish or drink. Mirrors `MenuItemDto` in `RestaurantPOS.Application`. */
export interface MenuItem {
  id: string;
  name: string;
  category: string;
  price: number;
  isActive: boolean;
  /** Whether Recipe Management has a bill of materials attached to this item. Not every item needs one. */
  hasRecipe: boolean;
  createdAtUtc: string;
}

export interface CreateMenuItemPayload {
  name: string;
  category: string;
  price: number;
}

export type UpdateMenuItemPayload = CreateMenuItemPayload;

export interface MenuItemFilters {
  search?: string;
  category?: string;
  isActive?: boolean;
}
