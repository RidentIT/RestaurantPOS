# Enterprise Desktop POS System — Frontend

Enterprise-grade **Electron 43 + React 19 + Vite 7** desktop application architecture following **Feature-Sliced Design (FSD)** principles for POS terminals.

## Architecture Stack

- **Container**: Electron 43 (2-Process Architecture: Main Node Process & Sandboxed Renderer Process)
- **UI Framework**: React 19 + Vite 7 SPA Engine
- **State Management**: Redux Toolkit + TanStack Query
- **HTTP Client**: Axios with 3 interceptors (Auth, Token Refresh, Global Error Handling)
- **Styling**: TailwindCSS + clsx + tailwind-merge
- **Testing**: Vitest + React Testing Library + MSW + Playwright E2E

---

## Directory Structure (FSD)

```text
frontend/
├── electron/                     # Electron Main Process (Node.js)
│   ├── main.ts                   # Entry point & window initialization
│   ├── preload.ts                # Exposed IPC bridge wall
│   ├── ipc/                      # System & Printer IPC handlers
│   └── services/                 # Native hardware print services
│
├── src/                          # Renderer Process (React 19)
│   ├── app/                      # React root, global providers & CSS
│   ├── pages/                    # Route-level screens (login, dashboard, checkout, reports)
│   ├── widgets/                  # Composite UI blocks (Header, Sidebar, CartSummary)
│   ├── features/                 # User flows & actions (auth, add-to-cart, payment)
│   ├── entities/                 # Business domain models (product, order, customer, ui)
│   └── shared/                   # Generic primitives (ui, api, store, socket, hooks, utils)
│
└── tests/                        # Unit, component, integration, architecture, & E2E tests
```

---

## Getting Started

### Install Dependencies
```bash
npm install
```

### Run Desktop App in Development Mode (Live Reloading)
```bash
npm run electron:dev
```

### Run Test Suite
```bash
npm test
```

### TypeScript Type Check
```bash
npm run typecheck
```

### Build Production Bundle
```bash
npm run build
```

### Package Desktop Executable (.exe Installer)
```bash
npm run electron:build
```
