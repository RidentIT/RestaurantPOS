# RestaurantPOS Frontend

Enterprise-grade Next.js 15 (App Router, React 19, TypeScript, pnpm) architecture tailored for high-reliability POS applications.

## Architecture Overview

This project strictly follows **Feature-Sliced Design (FSD)** principles to enforce domain decoupling and layer isolation:

```
src/
├── app/          # App Router routes, layouts, providers
├── widgets/      # Composition layer (complex UI blocks combining features/entities)
├── features/     # User actions/interactions (auth, orders, kitchen, inventory, etc.)
├── entities/     # Domain business models (user, order, product, supplier)
└── shared/       # Cross-cutting infrastructure (api, store, hooks, lib, ui, utils, websocket)
```

### Architectural Layer Import Rules
Rules are statically enforced via `eslint-plugin-boundaries`:
1. `shared` can only import `shared`.
2. `entities` can only import `shared`.
3. `features` can import `entities` and `shared`.
4. `widgets` can import `features`, `entities`, and `shared`.
5. `app` can import `widgets`, `features`, `entities`, and `shared`.

**Crucial Rule**: All API calls must reside exclusively inside `@/shared/api/endpoints`. Features and entities MUST NOT invoke direct raw HTTP calls.

## State Management & Real-time Stack

- **Redux Toolkit**: Client UI state, active session, authentication context.
- **TanStack Query (React Query)**: Server state fetching, caching, optimistic updates, and cache invalidation.
- **Axios Interceptors**: JWT bearer token injection, automated silent token refresh, and standardized error mapping.
- **Socket.io Client**: Real-time order status, kitchen order tickets (KOT), and table status updates.

## Testing Infrastructure

- **Vitest & React Testing Library**: Fast unit, component, and architecture boundary testing.
- **MSW (Mock Service Worker)**: API mocking layer for both browser development and Vitest integration testing.
- **Playwright**: End-to-end user journey automation testing across Chromium, Firefox, and WebKit.

## Getting Started

```bash
# Install dependencies
pnpm install

# Start development server
pnpm dev

# Run type check
pnpm typecheck

# Run unit & component tests
pnpm test

# Run E2E tests
pnpm test:e2e
```
