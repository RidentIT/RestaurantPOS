import { describe, expect, it, vi } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { render } from "@testing-library/react";
import { ModulePermissionPicker } from "@/features/users";
import { moduleCatalog } from "@tests/mocks/fixtures";

describe("ModulePermissionPicker", () => {
  it("explains that module selection does not apply to administrators", () => {
    render(
      <ModulePermissionPicker catalog={moduleCatalog} selected={[]} onChange={vi.fn()} isAdmin />,
    );

    expect(screen.getByText(/administrators can open every module/i)).toBeInTheDocument();
    expect(screen.queryByText("POS & Billing")).not.toBeInTheDocument();
  });

  it("never offers an admin-only module to a staff account", () => {
    render(
      <ModulePermissionPicker catalog={moduleCatalog} selected={[]} onChange={vi.fn()} isAdmin={false} />,
    );

    expect(screen.getByText("POS & Billing")).toBeInTheDocument();
    expect(screen.queryByText("User Management & Roles")).not.toBeInTheDocument();
  });

  it("toggles a module on and off", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    render(
      <ModulePermissionPicker catalog={moduleCatalog} selected={[]} onChange={onChange} isAdmin={false} />,
    );

    await user.click(screen.getByRole("checkbox", { name: /pos & billing/i }));

    expect(onChange).toHaveBeenCalledWith(["PosBilling"]);
  });

  it("deselects a currently-granted module", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    render(
      <ModulePermissionPicker
        catalog={moduleCatalog}
        selected={["PosBilling"]}
        onChange={onChange}
        isAdmin={false}
      />,
    );

    await user.click(screen.getByRole("checkbox", { name: /pos & billing/i }));

    expect(onChange).toHaveBeenCalledWith([]);
  });

  it("clears every selection at once", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    render(
      <ModulePermissionPicker
        catalog={moduleCatalog}
        selected={["PosBilling", "ReportsAnalytics"]}
        onChange={onChange}
        isAdmin={false}
      />,
    );

    await user.click(screen.getByRole("button", { name: /clear all/i }));

    expect(onChange).toHaveBeenCalledWith([]);
  });
});
