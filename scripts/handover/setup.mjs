#!/usr/bin/env node
/*
 * One-shot setup for a fresh Restaurant POS install: business profile, tax/footer, tables,
 * stewards and the full menu, all read from restaurant.json and menu.json in this folder.
 *
 * Usage (with the Restaurant POS app open and signed in at least once):
 *     node scripts/handover/setup.mjs
 *
 * Safe to run more than once — it looks at what already exists and only adds what is missing.
 * It never touches sales, payments, expenses or stock, so a database it has run against carries
 * only the menu and configuration, ready to hand to a customer.
 */

import { readFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const here = dirname(fileURLToPath(import.meta.url));

// restaurant.local.json holds the real, filled-in values (admin password, phone, …) and is
// git-ignored so those never land in the repo. restaurant.json stays as the committed template.
async function loadConfig() {
  for (const name of ["restaurant.local.json", "restaurant.json"]) {
    try {
      return JSON.parse(await readFile(join(here, name), "utf8"));
    } catch (e) {
      if (e.code !== "ENOENT") throw e;
    }
  }
  throw new Error("No restaurant.json (or restaurant.local.json) found next to setup.mjs.");
}

const config = await loadConfig();
const menu = JSON.parse(await readFile(join(here, "menu.json"), "utf8"));

const BASE = config.api.replace(/\/+$/, "");
let token = "";

const c = {
  reset: "\x1b[0m", green: "\x1b[32m", yellow: "\x1b[33m", red: "\x1b[31m", dim: "\x1b[2m",
};
const ok = (m) => console.log(`${c.green}  ✓${c.reset} ${m}`);
const skip = (m) => console.log(`${c.dim}  – ${m} (already there)${c.reset}`);
const info = (m) => console.log(`\n${c.yellow}${m}${c.reset}`);
const fail = (m) => { console.error(`${c.red}\n✗ ${m}${c.reset}`); process.exit(1); };

async function api(method, path, body) {
  const res = await fetch(`${BASE}${path}`, {
    method,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  const text = await res.text();
  const data = text ? JSON.parse(text) : null;

  if (!res.ok) {
    const detail = data?.detail || data?.title || text || res.statusText;
    const err = new Error(`${method} ${path} → ${res.status}: ${detail}`);
    err.status = res.status;
    throw err;
  }
  return data;
}

// ---- 1. Sign in -----------------------------------------------------------

async function signIn() {
  info("Signing in");
  let session;
  try {
    session = await api("POST", "/auth/login", {
      username: config.admin.username,
      password: config.admin.password,
    });
  } catch (e) {
    if (e.status === 401) {
      fail(
        "The username or password in restaurant.json is wrong. If you have already changed the " +
          "admin password in the app, put that password in admin.password.",
      );
    }
    if (String(e).includes("fetch failed") || e.code === "ECONNREFUSED") {
      fail(`Could not reach the app at ${BASE}. Open Restaurant POS and sign in once, then run this again.`);
    }
    throw e;
  }

  token = session.accessToken;

  if (session.user.mustChangePassword) {
    if (!config.admin.newPassword) {
      fail(
        "This admin account must set a new password. Put one in admin.newPassword in restaurant.json " +
          "(at least 8 characters, with a letter, a number and a symbol) and run this again.",
      );
    }
    const changed = await api("POST", "/auth/change-password", {
      currentPassword: config.admin.password,
      newPassword: config.admin.newPassword,
    });
    token = changed.accessToken;
    ok(`Admin password changed. From now on sign in with the password in admin.newPassword.`);
  } else {
    ok(`Signed in as ${session.user.username}`);
  }
}

// ---- 2. Restaurant profile ---------------------------------------------------

async function setProfile() {
  if (!config.profile) return;
  info("Business profile");

  const current = await api("GET", "/settings");
  await api("PUT", "/settings/profile", {
    name: config.profile.name ?? current.name,
    addressLine1: config.profile.addressLine1 ?? current.addressLine1,
    addressLine2: config.profile.addressLine2 ?? null,
    city: config.profile.city ?? null,
    phone: config.profile.phone ?? null,
    logoPath: current.logoPath ?? null,
    vatRegistrationNumber: config.profile.vatRegistrationNumber ?? null,
  });
  ok(`Name, address and phone set for "${config.profile.name}"`);

  if (config.billCharges) {
    await api("PUT", "/settings/bill-charges", {
      taxRatePercent: config.billCharges.taxRatePercent ?? 0,
      serviceChargeRatePercent: config.billCharges.serviceChargeRatePercent ?? 0,
    });
    ok(`Tax ${config.billCharges.taxRatePercent ?? 0}% · service charge ${config.billCharges.serviceChargeRatePercent ?? 0}%`);
  }

  if (config.receiptFooter) {
    await api("PUT", "/settings/receipt-footer", { message: config.receiptFooter });
    ok(`Receipt footer: "${config.receiptFooter}"`);
  }
}

// ---- 3. Tables -------------------------------------------------------------

async function setTables() {
  const spec = config.tables;
  if (!spec) return;
  info("Tables");

  const wanted = Array.isArray(spec)
    ? spec.map(String)
    : spec.count
      ? Array.from({ length: spec.count }, (_, i) => String(i + 1))
      : [];
  if (wanted.length === 0) return;

  const existing = new Set((await api("GET", "/tables")).map((t) => t.number));

  for (const number of wanted) {
    if (existing.has(number)) { skip(`Table ${number}`); continue; }
    await api("POST", "/tables", { number, seats: 4, notes: null });
    ok(`Table ${number}`);
  }
}

// ---- 4. Stewards ----------------------------------------------------------

async function setStewards() {
  const names = config.stewards ?? [];
  if (names.length === 0) return;
  info("Stewards");

  let existing;
  try {
    existing = new Set((await api("GET", "/stewards")).map((s) => s.name.toLowerCase()));
  } catch (e) {
    if (e.status === 404) {
      console.log(`${c.yellow}  Stewards aren't in this version of the app — skipping. Add them by hand later.${c.reset}`);
      return;
    }
    throw e;
  }

  for (const name of names) {
    if (existing.has(String(name).toLowerCase())) { skip(name); continue; }
    await api("POST", "/stewards", { name });
    ok(name);
  }
}

// ---- 5. Menu -------------------------------------------------------------

async function setMenu() {
  info("Menu categories");
  const categories = new Set((await api("GET", "/menu-items/categories")).map((n) => n.toLowerCase()));
  for (const name of menu.categories ?? []) {
    if (categories.has(name.toLowerCase())) { skip(name); continue; }
    await api("POST", "/menu-items/categories", { name });
    ok(name);
  }

  info("Menu items");
  const existing = new Set((await api("GET", "/menu-items")).map((i) => i.name.toLowerCase()));
  let added = 0;
  for (const item of menu.items ?? []) {
    if (existing.has(item.name.toLowerCase())) { skip(item.name); continue; }
    await api("POST", "/menu-items", {
      name: item.name,
      category: item.category,
      variants: item.variants.map((v) => ({ name: v.name ?? null, price: v.price })),
    });
    const prices = item.variants
      .map((v) => (v.name ? `${v.name} ${v.price}` : String(v.price)))
      .join(" · ");
    ok(`${item.name}  ${c.dim}(${prices})${c.reset}`);
    added++;
  }
  console.log(`\n${c.green}Menu: ${added} item(s) added, ${(menu.items?.length ?? 0) - added} already present.${c.reset}`);
}

// ---- run -----------------------------------------------------------------

try {
  console.log(`Restaurant POS — handover setup  ${c.dim}(${BASE})${c.reset}`);
  await signIn();
  await setProfile();
  await setTables();
  await setStewards();
  await setMenu();
  console.log(`\n${c.green}Done.${c.reset} Recipes, raw materials and suppliers are not seeded — add those in the app or send the data to have them scripted too.\n`);
} catch (e) {
  fail(e.message ?? String(e));
}
