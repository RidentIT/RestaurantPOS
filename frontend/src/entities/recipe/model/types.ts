import type { UnitOfMeasurement } from "@/entities/raw-material";

export interface RecipeLine {
  rawMaterialId: string;
  rawMaterialName: string;
  unitOfMeasurement: UnitOfMeasurement;
  quantity: number;
}

/** The bill of materials for one menu item: what it consumes per unit sold. */
export interface Recipe {
  id: string;
  menuItemId: string;
  isEnabled: boolean;
  lines: RecipeLine[];
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface RecipeLineInput {
  rawMaterialId: string;
  quantity: number;
}
