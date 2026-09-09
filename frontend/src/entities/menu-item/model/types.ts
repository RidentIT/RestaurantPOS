/** One size a menu item is sold in — always at least one per item. Mirrors `MenuItemVariantDto`. */
export interface MenuItemVariant {
  id: string;
  /** Null for the one variant on a single-size item; every variant is named once there's more than one. */
  name: string | null;
  price: number;
  /** Whether Recipe Management has a bill of materials attached to this size. Not every size needs one. */
  hasRecipe: boolean;
}

/** A sellable dish or drink. Mirrors `MenuItemDto` in `RestaurantPOS.Application`. */
export interface MenuItem {
  id: string;
  name: string;
  category: string;
  isActive: boolean;
  /** Every size this item is sold in, in display order. Always at least one. */
  variants: MenuItemVariant[];
  createdAtUtc: string;
}

export interface CreateMenuItemVariantPayload {
  name: string | null;
  price: number;
}

export interface CreateMenuItemPayload {
  name: string;
  category: string;
  variants: CreateMenuItemVariantPayload[];
}

/** A size after the edit — an existing size to update when `id` is set, a new one when it's null. */
export interface UpdateMenuItemVariantPayload {
  id: string | null;
  name: string | null;
  price: number;
}

export interface UpdateMenuItemPayload {
  name: string;
  category: string;
  variants: UpdateMenuItemVariantPayload[];
}

export interface MenuItemFilters {
  search?: string;
  category?: string;
  isActive?: boolean;
}

/** The lowest and highest price across an item's sizes — equal when it only has one. */
export function getPriceRange(item: Pick<MenuItem, "variants">): { min: number; max: number } {
  const prices = item.variants.map((v) => v.price);

  return { min: Math.min(...prices), max: Math.max(...prices) };
}

/** Whether every size (true), some (undefined) or none (false) has a recipe attached. */
export function getRecipeCoverage(item: Pick<MenuItem, "variants">): boolean | undefined {
  const withRecipe = item.variants.filter((v) => v.hasRecipe).length;

  if (withRecipe === 0) return false;
  if (withRecipe === item.variants.length) return true;

  return undefined;
}

/** "Chicken Fried Rice" for a single, unnamed size — "Chicken Fried Rice (Full)" once sizes are named. */
export function variantDisplayName(item: Pick<MenuItem, "name">, variant: Pick<MenuItemVariant, "name">): string {
  return variant.name ? `${item.name} (${variant.name})` : item.name;
}
