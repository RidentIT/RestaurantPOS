/** Matches the backend `UnitOfMeasurement` enum, serialised by name. */
export type UnitOfMeasurement = "Kilogram" | "Gram" | "Liter" | "Milliliter" | "Piece" | "Bottle" | "Packet";

export const UNITS_OF_MEASUREMENT: UnitOfMeasurement[] = [
  "Kilogram",
  "Gram",
  "Liter",
  "Milliliter",
  "Piece",
  "Bottle",
  "Packet",
];

/** Short display form, e.g. for "0.25 kg" next to a quantity. */
export const UNIT_ABBREVIATIONS: Record<UnitOfMeasurement, string> = {
  Kilogram: "kg",
  Gram: "g",
  Liter: "L",
  Milliliter: "ml",
  Piece: "pcs",
  Bottle: "bottles",
  Packet: "packets",
};

/** An ingredient tracked in stock. Its unit is fixed for its whole lifetime. */
export interface RawMaterial {
  id: string;
  name: string;
  unitOfMeasurement: UnitOfMeasurement;
  /** Main Store is flagged low once its stock reaches this. Null means no threshold is set. */
  mainStoreReorderLevel: number | null;
  /** Kitchen is flagged low once its stock reaches this. Null means no threshold is set. */
  kitchenParLevel: number | null;
  isActive: boolean;
}

export interface CreateRawMaterialPayload {
  name: string;
  unitOfMeasurement: UnitOfMeasurement;
  mainStoreReorderLevel: number | null;
  kitchenParLevel: number | null;
}

export type UpdateRawMaterialPayload = CreateRawMaterialPayload;

export interface RawMaterialFilters {
  search?: string;
  isActive?: boolean;
}
