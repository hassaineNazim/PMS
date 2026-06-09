# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Commercial, multi-tenant Hotel Property Management System (PMS) sold to multiple establishments. **.NET 9 / ASP.NET Core** backend, **PostgreSQL** via **EF Core 9**, **React + TypeScript** frontend, LG Pro:Centric / SuperSign IPTV integration behind a hardware-agnostic abstraction.

The active product lives in `backend/` (.NET) and `frontend/` (React). The original Node.js prototype remains in `src/` as **legacy** — do not extend it; build on the .NET solution.

## Commands

All builds/tests run inside Docker (no local .NET/Node required). On Windows, run Docker commands via the **PowerShell tool**, not Bash (Git Bash mangles `/src` mount paths).

- Full stack: `docker compose up --build` → web :8080, api :5080/swagger
- Build backend: `dotnet build backend/Pms.sln -c Release`
- Unit tests: `dotnet test backend/tests/Pms.UnitTests`
- Integration tests (needs Docker for Testcontainers): `dotnet test backend/tests/Pms.IntegrationTests`
- Add migration: `dotnet ef migrations add <Name> --project backend/src/Pms.Infrastructure --startup-project backend/src/Pms.Api -o Persistence/Migrations`
- Frontend dev: `cd frontend && npm install && npm run dev`

## Architecture (Clean Architecture, dependencies point inward)

- **Pms.Domain** — entities, enums, domain exceptions. No framework deps. `TenantEntity` base carries `TenantId`.
- **Pms.Application** — services (one per feature folder under `Features/`), DTOs, FluentValidation validators, and interfaces (`IApplicationDbContext`, `ICurrentTenant`, `IDisplayProvider`, `IInvoiceDocumentGenerator`). Depends only on Domain + EF Core abstractions.
- **Pms.Infrastructure** — `AppDbContext` (implements `IApplicationDbContext`), EF configurations, migrations, `DbInitializer`, JWT/bcrypt, QuestPDF generator, LG display provider, `CurrentTenant`.
- **Pms.Api** — controllers, middleware (`TenantResolutionMiddleware`, `ExceptionHandlingMiddleware`), `ValidationFilter`, JWT auth, Swagger, `Program.cs`.

Services use `IApplicationDbContext` directly (no repository layer). Enums are stored as strings; tables/columns are snake_case (EFCore.NamingConventions).

## Multi-tenancy (critical)

Every `ITenantEntity` gets a global EF query filter `e => e.TenantId == CurrentTenantId`. `CurrentTenant` is scoped and set by `TenantResolutionMiddleware` from the JWT `tenant_id` claim. Always set `TenantId` when creating tenant-scoped entities. Use `IgnoreQueryFilters()` only for cross-tenant lookups (login, tenant/license loading).

## Key flow: Check-in

`POST /api/checkin/{reservationId}` (`CheckInService`): inside a transaction — reservation → CheckedIn, room → Occupied, invoice created, audit logged. After commit, a **best-effort** IPTV welcome push via `IDisplayProvider` (failures are audited, never block check-in).

## No double-booking (three layers)

1. App-level overlap check in `ReservationService`.
2. EF transaction.
3. PostgreSQL `EXCLUDE USING gist (room_id, daterange(check_in, check_out))` added in `DbInitializer`. `AppDbContext.SaveChangesAsync` maps PG `23P01`/`23505` to `ConflictException` (→ 409).

Optimistic concurrency uses the `xmin` system column (Npgsql only; guarded by `Database.IsNpgsql()`).

## Commercial / Algeria modules (Features/)

- **Pricing** (`IPricingService`) resolves nightly rates from `RatePeriod` (seasonal) over the room base price; used by `ReservationService`.
- **Meal plans**: `Reservation.MealPlan` + per-person/night supplement (snapshot from tenant); `MealPlanTotal` flows into the folio/invoice.
- **Billing/Folio** (`FolioService`): single source of money truth = room + meal + extras + tax + cash stamp − payments. `FolioService.Compute` is pure and unit-tested; `RefreshInvoiceAsync` persists onto the `Invoice`.
- **Payments** (`PaymentService`): deposits/balance/refund, methods incl. Cash/CIB/Edahabia; cash receipts accrue the **droit de timbre** (`Tenant.ComputeFiscalStamp`, 1 DA/100 DA slice, min) and attach to the open `CashSession`.
- **CashRegister**: open with float, close with counted cash → expected vs discrepancy.
- **Charges**: mini-POS lines posted to a reservation folio.
- **Settings** (`SettingsService`): tenant identity + DGI fields (NIF/NIS/RC/Article) + stamp + meal supplements.
- **Reports**: fiche de police PDF (`IPoliceFormGenerator`), main courante, CSV exports.
- **Housekeeping**: `Room.HousekeepingStatus` + assignment, separate from commercial `RoomStatus`.
- Invoice PDF (`QuestPdfInvoiceGenerator`) renders DGI mentions, meal plan, extras, stamp, paid/balance.

## Conventions

- C# file-scoped namespaces, primary constructors, nullable enabled.
- Domain exceptions (`NotFoundException`, `ConflictException`, `BusinessRuleException`, `LicenseException`) → HTTP status via `ExceptionHandlingMiddleware`.
- Currency default **DZD**; money formatted client-side. Dates use `DateOnly`/`TimeOnly`.
- Config via environment / `appsettings.json`; secrets (JWT) via env in compose.
