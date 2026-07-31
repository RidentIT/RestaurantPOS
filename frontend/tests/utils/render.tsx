import React, { ReactElement } from "react";
import { render, RenderOptions } from "@testing-library/react";
import { Provider } from "react-redux";
import { QueryClientProvider } from "@tanstack/react-query";
import { createTestStore } from "./testStore";
import { createTestQueryClient } from "./testQueryClient";

interface ExtendedRenderOptions extends Omit<RenderOptions, "queries"> {
  preloadedState?: any;
  store?: ReturnType<typeof createTestStore>;
  queryClient?: ReturnType<typeof createTestQueryClient>;
}

export function renderWithProviders(
  ui: ReactElement,
  {
    preloadedState = {},
    store = createTestStore(preloadedState),
    queryClient = createTestQueryClient(),
    ...renderOptions
  }: ExtendedRenderOptions = {}
) {
  function Wrapper({ children }: { children: React.ReactNode }) {
    return (
      <Provider store={store}>
        <QueryClientProvider client={queryClient}>
          {children}
        </QueryClientProvider>
      </Provider>
    );
  }

  return { store, queryClient, ...render(ui, { wrapper: Wrapper, ...renderOptions }) };
}
