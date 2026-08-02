# User Management & Roles

The first module of the Sri Lakshmi Family Restaurant POS. It owns sign-in, staff accounts,
per-user module access, and the administrator approval PIN that other modules will call when a
privileged action needs authorising.

## Roles and access

There are two roles:

| Role    | Access                                                                    |
| ------- | ------------------------------------------------------------------------- |
| `Admin` | Every module, implicitly. Can administer staff and hold an approval PIN.   |
| `User`  | Only the modules an administrator has explicitly granted.                 |

Module access is all-or-nothing per module. Grants are stored one row per (user, module) in
`UserModulePermissions`, so action-level flags (`CanCreate`, `CanApprove`, …) can be added later
as columns with defaults rather than as a restructuring migration.

Two modules are marked `AdminOnly` in the catalog and are never offered in the permission
editor, because granting them would be granting administrative authority itself:

- `UserManagement`
- `SystemSettings`

The catalog is served by `GET /api/v1/modules`. The frontend builds both its navigation sidebar
and the permission editor from it, so adding a module to `ModuleCatalog` on the backend is all
that is needed to make it appear and become grantable.

## First run

On start-up the API applies migrations and, if no administrator exists, seeds one from the
`SeedAdmin` configuration section:

```json
"SeedAdmin": {
  "Username": "admin",
  "FullName": "System Administrator",
  "Password": "ChangeMe!123"
}
```

Override per installation with configuration or environment variables
(`SeedAdmin__Username`, `SeedAdmin__Password`).

The seeded account is flagged `MustChangePassword`, so the first thing anyone signing in as it
must do is choose a real password.

### Adding the owner

Sign in as the seeded administrator, then create the owner's account with the `Admin` role and a
temporary password. They will be forced to set their own password at first sign-in — the same
mechanism, no special case.

## Forced password change

Any account with `MustChangePassword` set — the seeded admin, anyone just created, anyone whose
password an admin has reset — receives a normal session, but that session is confined to the
password-change flow.

This is enforced **server-side**, not just in the UI: `PasswordChangeRequiredMiddleware` rejects
every endpoint except those marked `[AllowPendingPasswordChange]` with:

```
403  { "code": "Auth.PasswordChangeRequired" }
```

so a temporary password cannot be used to do real work by calling the API directly. The client
keys on that code to route to the reset screen.

## Approval PIN

An administrator can set or generate a 4-digit PIN (`POST /api/v1/auth/pin`, re-authenticated
with their current password). Only the BCrypt hash is stored — the plaintext is returned exactly
once, at the moment it is set, and cannot be read back.

`POST /api/v1/auth/pin/verify` is the shared approval gate any module can call. It is callable
by **any signed-in user**, which is the point: a cashier attempting to void an order calls it
while an administrator types their PIN, and the response records who authorised it.

```jsonc
// POST /api/v1/auth/pin/verify   { "pin": "4821", "reason": "Cancel order #1042" }
{
  "approvedByUserId": "…",
  "approvedByName": "System Administrator",
  "approvedAtUtc": "2026-08-02T08:17:34Z"
}
```

Because 4 digits is only ten thousand combinations, this endpoint sits behind a fixed-window
rate limiter (10 attempts/minute, partitioned per user), and PINs are hashed with the same cost
factor as passwords.

## Sessions

- **Access token** — JWT, 60 minutes by default, carrying the user's id, role and granted
  modules. Administrators carry no module claims; their role covers everything.
- **Refresh token** — opaque, 14 days, **rotated on every use**. Only a SHA-256 hash is stored.
  Presenting a token that has already been consumed fails, so a stolen copy has a short life.

Sessions are revoked immediately — not left to expire — when a user is deactivated, has their
password reset by an admin, or changes their own password (all sessions but the current one).

### Signing key

If `Jwt:SigningKey` is empty, a 64-byte key is generated on first run and stored at
`Jwt:KeyFilePath` (default `keys/jwt-signing.key`, alongside the application). This keeps a
single-machine install zero-configuration without shipping a weak shared default. The file is
gitignored; deleting it signs everyone out. A multi-machine deployment should set
`Jwt:SigningKey` explicitly instead.

## Accounts are deactivated, never deleted

There is no delete endpoint. Orders, bills and stock movements will reference the staff member
who performed them, so accounts are deactivated to preserve that history. Guards prevent
administering the system into a corner:

| Guard                          | Error code                      |
| ------------------------------ | ------------------------------- |
| Cannot deactivate yourself     | `User.CannotDeactivateSelf`     |
| Cannot change your own role    | `User.CannotDemoteSelf`         |
| Built-in admin is protected    | `User.CannotModifySystemAdmin`  |
| Cannot remove the last admin   | `User.LastAdmin`                |

## API reference

All paths are prefixed `/api/v1`.

| Method   | Path                    | Who                      |
| -------- | ----------------------- | ------------------------ |
| `POST`   | `/auth/login`           | anonymous                |
| `POST`   | `/auth/refresh`         | anonymous                |
| `POST`   | `/auth/logout`          | anonymous                |
| `GET`    | `/auth/me`              | any signed-in user       |
| `POST`   | `/auth/change-password` | any signed-in user       |
| `POST`   | `/auth/pin`             | admin                    |
| `DELETE` | `/auth/pin`             | admin                    |
| `POST`   | `/auth/pin/verify`      | any signed-in user       |
| `GET`    | `/modules`              | any signed-in user       |
| `GET`    | `/users`                | admin                    |
| `GET`    | `/users/{id}`           | admin                    |
| `POST`   | `/users`                | admin                    |
| `PUT`    | `/users/{id}`           | admin                    |
| `PUT`    | `/users/{id}/status`    | admin                    |
| `POST`   | `/users/{id}/password`  | admin                    |

Failures are RFC 7807 problem responses carrying a stable `code` (for example
`Auth.InvalidCredentials`, `User.UsernameTaken`) so clients branch on the code, never on prose.

## Running it

```bash
# Backend — migrates, seeds and serves on http://localhost:5207
cd backend/src/RestaurantPOS.API
dotnet run

# Backend tests (89: 48 unit, 39 integration, 2 architecture)
cd backend
dotnet test
```

Integration tests boot the real API against a throwaway SQLite file and go through the same
migrate-and-seed path a fresh install does, so the bootstrap itself is covered.
