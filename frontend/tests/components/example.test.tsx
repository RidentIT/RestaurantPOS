import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import HomePage from "@/app/page";
import { renderWithProviders } from "../utils/render";

describe("HomePage Component Test", () => {
  it("renders system title heading", () => {
    renderWithProviders(<HomePage />);
    expect(
      screen.getByRole("heading", { name: /Restaurant POS Enterprise System/i })
    ).toBeInTheDocument();
  });
});
