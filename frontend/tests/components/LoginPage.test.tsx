import { describe, expect, it } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import LoginPage from "@/pages/login";
import { server } from "@tests/mocks/server";
import { renderWithProviders } from "@tests/utils/render";

describe("LoginPage", () => {
  it("requires both fields before submitting", async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginPage />);

    await user.click(screen.getByRole("button", { name: /sign in/i }));

    expect(await screen.findByText("Username is required.")).toBeInTheDocument();
    expect(screen.getByText("Password is required.")).toBeInTheDocument();
  });

  it("shows the server's message when credentials are rejected", async () => {
    server.use(
      http.post("*/api/v1/auth/login", () =>
        HttpResponse.json(
          { title: "The username or password is incorrect.", code: "Auth.InvalidCredentials" },
          { status: 401 },
        ),
      ),
    );

    const user = userEvent.setup();
    renderWithProviders(<LoginPage />);

    await user.type(screen.getByLabelText(/username/i), "admin");
    await user.type(screen.getByLabelText(/password/i), "wrong-password");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    expect(await screen.findByText("The username or password is incorrect.")).toBeInTheDocument();
  });

  it("establishes a session on success", async () => {
    const user = userEvent.setup();
    const { store } = renderWithProviders(<LoginPage />);

    await user.type(screen.getByLabelText(/username/i), "admin");
    await user.type(screen.getByLabelText(/password/i), "ChangeMe!123");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => expect(store.getState().auth.status).toBe("authenticated"));
    expect(store.getState().auth.user?.username).toBe("admin");
  });
});
