# Handover setup

Loads a fresh Restaurant POS database with the restaurant's **menu and configuration** — and
nothing else. Use it to prepare a clean database to hand to a customer without any of the test
sales / expenses you rang up while demoing.

## What it sets

- Business profile (name, address, phone, VAT number)
- Tax and service-charge rates, receipt footer
- Tables
- Stewards
- Menu categories and every menu item with its sizes and prices (`menu.json`)

It does **not** touch sales, payments, expenses, stock, recipes, raw materials or suppliers.

## How to use it

1. Install the app on a machine with an empty database (fresh install, or after wiping
   `%APPDATA%\Restaurant POS\`).
2. Open Restaurant POS and sign in once (set the new admin password when prompted).
3. Edit **`restaurant.json`** — put in the real business details, the admin password you just set,
   the number of tables, and the steward names. Check **`menu.json`** and fix any price or name.
4. From the project root, run:

   ```
   node scripts/handover/setup.mjs
   ```

5. Open the app — the menu, tables and settings are all there.

Re-running is safe: it only adds what's missing.

## Making the handover database

Run this on your laptop against a **fresh** install, then copy
`%APPDATA%\Restaurant POS\restaurantpos.db` (+ `-wal`, `-shm`) and the `attachments\` folder onto
the customer's machine. That database has the real menu and no demo transactions.

## Notes

- `menu.json` uses **"Vegi"** where the printed cards wrote "Uegi" (assumed a typo) — change it if
  the cards are correct. "Seafood" is spelled consistently.
- Cheese Kottu / Cheese Pasta / Pasta items are single-price; everything else has Normal / Full.
- Recipes (ingredient tracking), raw materials and suppliers still need to be added in the app, or
  sent over to be scripted the same way.
