import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import App from "@/app/App";
import { renderWithProviders } from "../utils/render";

describe("App Component Test", () => {
  it("renders system title heading", () => {
    renderWithProviders(<App />);
    expect(
      screen.getByRole("heading", { name: /Restaurant POS System/i })
    ).toBeInTheDocument();
  });
});
