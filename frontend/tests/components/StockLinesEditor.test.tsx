import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { StockLinesEditor } from "@/features/inventory";
import type { RawMaterial } from "@/entities/raw-material";

const rawMaterials: RawMaterial[] = [
  { id: "rice", name: "Rice", unitOfMeasurement: "Kilogram", mainStoreReorderLevel: null, kitchenParLevel: null, isActive: true },
  { id: "chicken", name: "Chicken", unitOfMeasurement: "Kilogram", mainStoreReorderLevel: null, kitchenParLevel: null, isActive: true },
];

describe("StockLinesEditor", () => {
  it("shows an empty state with no lines", () => {
    render(<StockLinesEditor rawMaterials={rawMaterials} lines={[]} onChange={vi.fn()} />);

    expect(screen.getByText("No raw materials added yet")).toBeInTheDocument();
  });

  it("adds a line with the chosen raw material and quantity", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<StockLinesEditor rawMaterials={rawMaterials} lines={[]} onChange={onChange} />);

    await user.click(screen.getByRole("combobox"));
    await user.click(screen.getByRole("option", { name: "Rice" }));
    await user.type(screen.getByLabelText("Quantity"), "5");
    await user.click(screen.getByRole("button", { name: /add/i }));

    expect(onChange).toHaveBeenCalledWith([{ rawMaterialId: "rice", quantity: 5 }]);
  });

  it("refuses to add without a quantity greater than zero", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<StockLinesEditor rawMaterials={rawMaterials} lines={[]} onChange={onChange} />);

    await user.click(screen.getByRole("combobox"));
    await user.click(screen.getByRole("option", { name: "Rice" }));
    await user.click(screen.getByRole("button", { name: /add/i }));

    expect(onChange).not.toHaveBeenCalled();
  });

  it("excludes a raw material already on the list from the picker", async () => {
    const user = userEvent.setup();
    render(
      <StockLinesEditor rawMaterials={rawMaterials} lines={[{ rawMaterialId: "rice", quantity: 5 }]} onChange={vi.fn()} />,
    );

    expect(screen.getByText("Rice")).toBeInTheDocument();

    await user.click(screen.getByRole("combobox"));

    // Only Chicken remains selectable — Rice is already a line, not an option.
    expect(screen.getByRole("option", { name: "Chicken" })).toBeInTheDocument();
    expect(screen.queryByRole("option", { name: "Rice" })).not.toBeInTheDocument();
  });

  it("removes a line", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <StockLinesEditor rawMaterials={rawMaterials} lines={[{ rawMaterialId: "rice", quantity: 5 }]} onChange={onChange} />,
    );

    await user.click(screen.getByRole("button", { name: /remove rice/i }));

    expect(onChange).toHaveBeenCalledWith([]);
  });
});
