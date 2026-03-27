# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Hotel Property Management System (PMS) MVP with LG Pro:Centric IPTV integration. Node.js/Express backend, PostgreSQL database, API-first architecture.

## Commands

- `npm start` — run the server (src/server.js)
- `npm run dev` — run with auto-reload (Node --watch)
- `psql -U postgres -d pms_hotel -f src/config/schema.sql` — initialize/reset database schema and seed data

## Architecture

MVC pattern: `src/models/` → `src/controllers/` → `src/routes/` → `src/app.js`.

- **Models** use raw SQL via `pg` pool (no ORM). Each model exports an object with async query methods.
- **Controllers** handle request/response logic and call models + services.
- **Routes** are thin wrappers that map HTTP methods to controller functions.
- **src/integrations/procentric/** — LG Pro:Centric IPTV middleware integration. Sends guest data on check-in via JSON/REST (primary) with XML/HTNG fallback.
- **src/config/db.js** — PostgreSQL connection pool shared across all models.

## Key flow: Check-in

`POST /api/checkin/:reservationId` triggers: reservation status → checked_in, room status → occupied, Pro:Centric IPTV notification (non-blocking), audit log entries. IPTV failures are logged but don't block check-in.

## Database

PostgreSQL with enums for `room_status` (available/occupied/dirty), `room_type`, `reservation_status` (confirmed/checked_in/checked_out/cancelled). Schema in `src/config/schema.sql`.

## Conventions

- CommonJS modules (`require`/`module.exports`)
- All DB queries use parameterized `$1, $2` placeholders (SQL injection safe)
- Config via environment variables (dotenv), see `.env.example`
