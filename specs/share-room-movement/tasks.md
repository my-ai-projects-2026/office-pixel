# Shared Room Movement — Tasks

Plan: ./plan.md   ·   Spec: ./spec.md   ·   Last updated: 2026-11-16

Tags: `[BE]` backend (Dyg), `[UI]` frontend (teammate), `[TEST]`, `[INFRA]`; `[P]` = can run in parallel with the previous task. Task IDs are never renumbered.

## Foundations

- [x] **T01** [INFRA] Server hygiene: warnings as errors, remove the weather template, add `PixelOffice.API.Tests` (xUnit) to the solution
      Done when: `dotnet build && dotnet test` pass with one smoke test proving the host boots.
      Note (2026-11-16): `Directory.Build.props` holds net10.0 / nullable / warnings-as-errors for all projects; `Program.cs` and `.http` stripped to OpenAPI only; `PixelOffice.API.Tests` (xunit 2.9.3, Mvc.Testing 10.0.14, template's coverlet dropped) with `HostSmokeTests`. `global.json` now rolls forward to the installed SDK 11.0 RC 3 (no SDK 10 on the dev machine; see plan.md). Build 0 warnings, 1/1 test passed.
- [x] **T38** [INFRA] Aspire `PixelOffice.ServiceDefaults` project; API calls `AddServiceDefaults()` and `MapDefaultEndpoints()` — depends on T01
      Done when: `/health` and `/alive` return 200 in Development; the smoke test from T01 uses `/health`.
      Note (2026-11-16): scaffolded with `dotnet new aspire-servicedefaults` (Extensions.cs unchanged; package versions from the template: Http.Resilience/ServiceDiscovery 10.6.0, OpenTelemetry 1.15.x); csproj relies on `Directory.Build.props`; added to the solution and referenced by the API. `HostSmokeTests` now checks `/health` and `/alive` return 200 "Healthy". Build 0 warnings, 2/2 tests passed.
- [x] **T02** [INFRA] Aspire `PixelOffice.AppHost`: PostgreSQL container with data volume, database `pixeloffice`, API project referenced and waiting on the database; empty `AppDbContext` registered with `AddNpgsqlDbContext` — depends on T38
      Done when: running the AppHost starts PostgreSQL and the API, both show healthy in the Aspire dashboard, and no connection string is in source; CLAUDE.md stack table and "Commands" updated.
      Note (2026-11-16): Aspire pinned to **13.6.5** (AppHost SDK, `Aspire.Hosting.PostgreSQL`, `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`) — the installed template's 13.4.6 has published high/critical advisories that fail the build. `AspireUseCliBundle=false` + `ASPIRE010` suppressed so `dotnet run` needs no Aspire CLI. API registers `Data/AppDbContext` (no entities) and its `/health` checks the database. Tests use `Fixtures/ApiFactory` (placeholder connection string, DB health check off). Verified: `dotnet run --project PixelOffice.AppHost` (https profile) started postgres:18.3 with volume `…-postgres-data`, API `/health` Healthy in ~27 s. Build 0 warnings, 2/2 tests passed. CLAUDE.md layout, stack table and Commands updated.
- [x] **T39** [INFRA] AppHost starts the Next.js client (`app/`) with the API address injected as an environment variable; API CORS origin comes from the AppHost — depends on T02
      Done when: one AppHost run serves the client, and the client reaches the API with no hard-coded URL on either side.
      Note (2026-11-16): `Aspire.Hosting.JavaScript` 13.6.5; `AddNextJsApp("web", "../../../app")` (experimental, `ASPIREJAVASCRIPT001` suppressed locally) gets `NEXT_PUBLIC_API_URL` = API https endpoint and waits for the API; API gets `Cors__AllowedOrigins__0` = the web endpoint. API default CORS policy reads `Cors:AllowedOrigins` (none configured → no cross-origin access), allows credentials for SignalR later. `CorsTests` (2) added; `ApiFactory` unsealed. Aspire runs `npm install` in `app/` first (`web-installer`); no `app/` files changed. Verified: AppHost run shows postgres, api, web Running; a browser on the client's proxied address (http://localhost:50284) fetched `https://localhost:7009/health` → 200 Healthy. Build 0 warnings, 4/4 tests passed.
- [ ] **T03** [INFRA] [P] Client test tooling: Vitest and Playwright with `npm run test` and `npm run e2e`
      Done when: one trivial Vitest test and one Playwright test that loads `/` pass; CLAUDE.md "Commands" updated.

## Domain & persistence

- [x] **T04** [BE] `Player` entity, `AvatarCatalog` (6 presets), configuration — covers BR-16, BR-17, BR-19, BR-20, BR-22 — depends on T02
      Done when: unit tests show catalogue membership check works, display name validation (1–24 chars, trimmed) and email normalisation (trim, lower-case) pass, and a new player defaults to avatar 1.
      Note (2026-11-16): `Features/Players/` — `AvatarCatalog` (ids 1–6, keys `avatar-1`…`avatar-6`, default 1), `EmailAddress.Normalize`, `DisplayName.TryNormalize`, `Player` (`Create` → v7 Guid, normalised email, default avatar; `TryChangeAvatar` keeps current look on an unknown id). `Data/Configurations/PlayerConfiguration.cs`: table `Players`, unique index on `Email`, max lengths 254/24, check constraint `AvatarId BETWEEN 1 AND 6`, no index on `DisplayName`; `AppDbContext.Players` + `ApplyConfigurationsFromAssembly`. Email format and password rules are left to T08. Tests: `Players/*Tests` (unit) and `Persistence/PlayerModelTests` (design-time model, no DB). Build 0 warnings, 40/40 tests passed. No migration yet (T07).
- [x] **T05** [BE] `Room` and `Map` entities, `Assets/maps/office.tmj` (30 × 20, layers floor / walls / furniture / spawn / zones), idempotent `DbSeeder` — depends on T02
      Done when: seeding twice leaves exactly one `default` room and one map.
      Note (2026-11-16): `Features/Rooms/` (not `Features/Room`: a `Room` class in a `Room` namespace clashes) — `Map` (Name unique, Version, **ContentHash** SHA-256 hex, TiledJson jsonb; `ReplaceContent` bumps Version only on a different hash) and `Room` (Slug unique `default`, Name, MaxPlayers 10 with check > 0, MapId FK restrict). `Assets/maps/office.tmj` generated: 30 × 20, walls border, six 4 × 2 desk clusters, corner plants, spawn tile (15, 16), empty zones; tileset is a placeholder `office-placeholder.png` (4 tiles) until T23. `Data/DbSeeder` (scoped; `TimeProvider.System` registered) — not yet called at startup: T07 runs migrations then the seeder in Development. Tests: `Fixtures/PostgresFixture` (Testcontainers 4.16.0, postgres:18.3, fresh database per test via EnsureCreated until T07), `Persistence/DbSeederTests` (3), `Persistence/OfficeMapFileTests` (3); `Microsoft.Extensions.TimeProvider.Testing` 10.11.0. Build 0 warnings, 46/46 tests passed.
- [x] **T06** [BE] [P] `DiagnosticRecord` entity and configuration with indexes — covers BR-18 — depends on T02
      Done when: solution builds; model snapshot shows the `OccurredAt` index.
      Note (2026-11-16): `Features/Diagnostics/DiagnosticRecord.cs` — `DiagnosticRecordType` (Joined, Left, GraceExpired, SessionReplaced, MoveRejected) stored as text; long identity Id; PlayerId, RoomId (no foreign keys, deliberate: append-only log, purged by age); OccurredAt normalised to UTC; Detail ≤ 256 chars (cut), blank → null. `DiagnosticRecordConfiguration`: index on `OccurredAt` and on (`PlayerId`, `OccurredAt`); `AppDbContext.DiagnosticRecords`. No migration snapshot exists until T07, so the index is checked on the design-time model instead (`Persistence/DiagnosticRecordModelTests`), plus a real-PostgreSQL round trip and unit tests. Build 0 warnings, 56/56 tests passed.
- [x] **T07** [BE] Migration `AddAccountsRoomsAndDiagnostics` — depends on T04, T05, T06
      Done when: migration generated and reviewed; applies cleanly to an empty local database; Testcontainers fixture applies it in tests (replacing EnsureCreated); in Development the API applies migrations and runs `DbSeeder` at startup.
      Note (2026-11-16): `Data/Migrations/20261116081722_AddAccountsRoomsAndDiagnostics` — reviewed: creates Players, Maps, Rooms, DiagnosticRecords with the planned keys, unique indexes, check constraints and FK Rooms→Maps (restrict); no drops or renames. `Microsoft.EntityFrameworkCore.Design` 10.0.14; global `dotnet-ef` is 10.0.10 (works, prints an "older tools" notice). `AppDbContextDesignTimeFactory` so `migrations add` needs no database or AppHost. `Data/DatabaseStartup.MigrateAndSeedAsync` runs before serving when `Database:ApplyMigrationsOnStartup` is true (set only in appsettings.Development.json; `ApiFactory` turns it off). `PostgresFixture` now migrates instead of EnsureCreated; new `DatabaseApiFactory` + `Persistence/MigrationTests` (no pending model changes, applies to empty DB, startup migrates + seeds + /health Healthy with the DB check). Verified on the local AppHost volume: migration applied, room `default`/Office/10 and map `office` v1 present. Build 0 warnings, 59/59 tests passed.

## Accounts & API

- [x] **T08** [BE] JWT options + token service; `POST /api/auth/register` and `POST /api/auth/login` — covers BR-01, BR-19–BR-23; AC-25, AC-26, AC-27, AC-28, AC-29, AC-30 — depends on T07
      Done when: `AC_25_…` to `AC_30_…` server tests pass, including two concurrent registrations with the same email and a token aged past 12 h with `FakeTimeProvider`.
      Note (2026-11-16): `Features/Auth/` — `JwtOptions` (issuer/audience `pixeloffice`, SigningKey ≥ 32 chars validated at startup; 12 h lifetime fixed per BR-23), `JwtTokenService` (HS256, only `sub` = player id; times from TimeProvider), `AuthSetup` (JwtBearer 10.0.14, MapInboundClaims off, lifetime checked against TimeProvider, 30 s skew), `PasswordHashing` (ASP.NET Core PasswordHasher, dummy-hash check for unknown emails, rehash on login when needed), `PasswordRules` (8–128), `AuthEndpoints` (POST /api/auth/register → 201/400 per-field/409; POST /api/auth/login → 200/401 identical for unknown email and wrong password; concurrent duplicate caught via unique-violation). `EmailAddress.IsValid` added. AppHost generates `jwt-signing-key` (persisted in its user secrets) → `Jwt__SigningKey`. Tests: `Auth/AuthTests` (AC_25–AC_30 incl. concurrent registration and FakeTimeProvider expiry) + `Auth/RegistrationRulesTests`; test factories set a test key. AC-30 hub part is left to T15 (no hub yet); token refusal checked with the API's own validation parameters. Verified via AppHost over HTTPS (201, 409, 200, 401); test account removed afterwards. Build 0 warnings, 95/95 tests passed. Branch `t08-auth-register-login`.
- [x] **T09** [BE] `GET /api/me`, `GET /api/avatars`, `PUT /api/me/avatar` (persistence only) — covers BR-17 — depends on T08
      Done when: tests show a valid avatar is saved and returned by `/api/me`; an id outside the set returns 400 and the look is unchanged.
      Note (2026-11-16): `Features/Players/PlayerEndpoints.cs` — `GET /api/avatars` (anonymous, the 6 presets), `GET /api/me` (200 profile; 401 without a token or when the account no longer exists), `PUT /api/me/avatar` (204; 400 `avatarId` outside the set, look kept; saved with `ExecuteUpdateAsync`). `Features/Auth/PlayerIdentity.TryGetPlayerId` reads the player id from `sub` only (reused by the hub in T15). AC-30 test now also checks `/api/me` returns 401 for a 12-hour-old token through the real pipeline. Test clients now start on `https://localhost`: over HTTP, `UseHttpsRedirection` redirected and HttpClient dropped the `Authorization` header (test-host issue only). Tests: `Players/PlayerEndpointsTests` (11). Build 0 warnings, 106/106 tests passed. Broadcasting the change (AC-22) is T18.
- [ ] **T10** [BE] [P] `GET /api/rooms/default` returning room definition and Tiled JSON — depends on T08
      Done when: authenticated call returns the seeded map; anonymous call returns 401.

## Room core (pure, no I/O)

- [ ] **T11** [BE] `TiledMapParser` → `CollisionGrid` (blocked tiles, bounds, spawn, default facing) — covers BR-04, BR-06 — depends on T05
      Done when: unit tests parse `office.tmj` and a small fixture map; malformed maps fail with a clear error at startup.
- [ ] **T12** [BE] `PlayerMovementState` + `MovementRules.TryEnqueue` (adjacency, bounds, blocked, queue limit, stale epoch) — covers BR-06, BR-07, BR-14; AC-06, AC-07, AC-14 — depends on T11
      Done when: `AC_06_…`, `AC_07_…`, `AC_14_…` unit tests pass.
- [ ] **T13** [BE] `MovementRules.Drain` with schedule-based step pacing on `TimeProvider` — covers BR-05, BR-11; AC-04, AC-13, AC-15 — depends on T12
      Done when: `AC_04_…`, `AC_13_…` (unit part) and `AC_15_…` (unit part) pass with `FakeTimeProvider`.
- [ ] **T14** [BE] `RoomState`: join, capacity, single presence / replacement, leave, disconnect, grace, rejoin, event outbox, snapshot — covers BR-02, BR-03, BR-04, BR-10; AC-03, AC-10, AC-11 — depends on T13
      Done when: unit tests for `AC_03_…`, `AC_10_…`, `AC_11_…` pass; a grace-period presence counts toward capacity.

## Hub & realtime

- [ ] **T15** [BE] `RoomContracts.cs` (MessagePack shapes from the plan's hub table), SignalR with MessagePack + WebSockets only, JWT on the hub, `RoomHub.Join` / `Leave`, session replacement — covers AC-01, AC-02, AC-03, AC-16 — depends on T08, T14
      Done when: `AC_01_…`, `AC_02_…`, `AC_03_…` (integration), `AC_16_…` pass using real SignalR test clients.
- [ ] **T16** [BE] `RoomHub.Move` + `Corrected` to caller; binding-failure filter for malformed input — covers AC-13, AC-14 — depends on T15
      Done when: `AC_14_…` (integration) passes; a malformed payload produces a correction and leaves other clients untouched.
- [ ] **T17** [BE] `RoomTicker` background service (15 Hz): drain queues, flush `PlayerJoined` / `PlayerLeft` / `PlayerUpdated`, send `Snapshot`, expire grace, expire tokens (`SessionExpired`) — covers BR-08, BR-09, BR-10; AC-08, AC-09, AC-11, AC-12, AC-13 — depends on T16
      Done when: two-client integration tests `AC_08_…`, `AC_09_…`, `AC_11_…`, `AC_12_…`, `AC_13_…` pass with a controlled clock.
- [ ] **T18** [BE] In-room avatar change: `PUT /api/me/avatar` updates the live presence and queues `PlayerUpdated` — covers AC-22 — depends on T09, T17
      Done when: `AC_22_Avatar_change_is_broadcast_and_persisted` passes.
- [ ] **T19** [BE] `DiagnosticsWriter` (bounded channel, batched inserts, rejected-move coalescing) wired to join, leave, grace expiry, replacement, rejection — covers BR-18 — depends on T17
      Done when: integration test shows one record per event type and a 100-move flood produces at most one `MoveRejected` record per second.
- [ ] **T20** [BE] [P] `DiagnosticsRetentionService` (startup + every 24 h, `ExecuteDeleteAsync`) — covers AC-24 — depends on T07
      Done when: `AC_24_Retention_deletes_records_older_than_30_days_and_keeps_newer` passes against PostgreSQL.
- [ ] **T21** [TEST] Test-only inbound delay option on the hub; two-client sync tests — covers AC-05, AC-10, AC-15, AC-17, AC-23 — depends on T17
      Done when: `AC_05_…`, `AC_10_…` (integration), `AC_15_…` (integration), `AC_17_…`, `AC_23_…` pass; the delay option is ignored outside the `Testing` environment.
- [ ] **T22** [BE] CORS policy for the hub (origin supplied by configuration, see T39), options validation at startup, structured logging pass (one event per hub outcome, no positions above Debug) — depends on T17
      Done when: log assertions in one integration test pass; invalid configuration fails fast at startup.

## Client

- [ ] **T23** [UI] Art: tileset and 6 avatar sprite sheets (placeholders allowed) in `public/assets`; `docs/assets.md` with licences — covers BR-17
      Done when: assets load at 32 px with nearest-neighbour scaling; every pack has a licence entry.
- [ ] **T24** [UI] `contracts.ts` mirroring `RoomContracts.cs` and API DTOs; `api.ts`; `session.ts` (token storage) — depends on T15 (contract frozen)
      Done when: type-checks with no `any`; Vitest covers session expiry handling.
- [ ] **T25** [UI] `/sign-in` (sign in + register) and `/room` route guard — covers AC-01, AC-27, AC-30 — depends on T24
      Done when: `AC-01 visitor is redirected to sign-in`, `AC-27 form shows the message for the wrong field` and `AC-30 expired session redirects to sign-in` pass; the 401 and 409 messages are shown.
- [ ] **T26** [UI] `roomConnection.ts` singleton: WebSockets only, MessagePack, token factory, reconnect, `Join` after every connect — depends on T24
      Done when: Vitest with a fake connection shows `Join` is re-sent after reconnect and `SessionReplaced` disables auto-reconnect.
- [ ] **T27** [UI] `gameStore.ts` (plain store) + `useMembers` subscription; handlers for `JoinResult`, `PlayerJoined`, `PlayerLeft`, `PlayerUpdated`, `Snapshot` — depends on T26
      Done when: Vitest covers each handler; React re-renders only on member/UI changes, not on snapshots.
- [ ] **T28** [UI] `PixiRoom` client-only component: load room definition, render tile layers, camera, nearest scaling — depends on T23, T24
      Done when: the office renders from the API's map JSON; canvas is loaded with `dynamic(..., { ssr: false })`.
- [ ] **T29** [UI] Avatar sprites, name labels (plain text), disconnected marking — covers AC-02, AC-12, AC-23 — depends on T27, T28
      Done when: `AC-12 disconnected avatar is marked` passes; two same-named players render as separate avatars.
- [ ] **T30** [UI] [P] Keyboard input: four directions, most-recent-key-wins, release fallback, hidden-tab stop — covers AC-18 — depends on T27
      Done when: `AC-18 most recently pressed key wins and movement is never diagonal` passes.
- [ ] **T31** [UI] Local player step loop, prediction against `collision.ts`, `Move` sending, `Corrected` reconciliation — covers AC-04, AC-06, AC-07, AC-13 — depends on T29, T30
      Done when: client `AC-04`, `AC-06`, `AC-07`, `AC-13` unit tests pass.
- [ ] **T32** [UI] [P] Remote avatar path-following between snapshot tiles with catch-up — covers AC-05 — depends on T29
      Done when: `AC-05 remote avatar walks continuously between snapshot tiles` passes.
- [ ] **T33** [UI] A* pathfinding, click-to-move, key cancel, "can't go there" indication — covers AC-19, AC-20, AC-21 — depends on T31
      Done when: client `AC-19`, `AC-20`, `AC-21` unit tests pass.
- [ ] **T34** [UI] Member list and room states: loading, room full, reconnecting, session replaced, session expired — covers AC-03, AC-08, AC-09, AC-16 — depends on T27
      Done when: `AC-03 shows room full message` passes; each state in the plan's table is reachable.
- [ ] **T35** [UI] Avatar picker panel calling `PUT /api/me/avatar` — covers AC-22 — depends on T29
      Done when: choosing a look updates the local avatar and the choice survives a reload.

## End-to-end

- [ ] **T36** [TEST] Playwright two-browser suite against the real server — covers AC-01, AC-02, AC-05, AC-08, AC-09, AC-16, AC-19, AC-22, AC-25, AC-28 — depends on T21, T31–T35
      Done when: all listed `AC-xx` e2e tests pass locally from a single command against a stack started by the AppHost.
- [ ] **T37** [TEST] Traceability check: every AC-01…AC-30 has at least one passing automated test named for it — depends on T36
      Done when: a script (or test) listing AC ids against test names reports no gaps.
