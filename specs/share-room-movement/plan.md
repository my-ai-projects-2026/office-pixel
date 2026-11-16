# Shared Room Movement — Technical Plan

Status: Approved (2026-11-16)   ·   Spec: ./spec.md   ·   Last updated: 2026-11-16

## Approach

One ASP.NET Core process hosts the minimal API (accounts, avatar choice, room/map definition) and one SignalR hub (`/hubs/room`). The room's live state sits in memory behind a single lock; a `BackgroundService` ticks at 15 Hz, applies queued movement, expires grace periods and broadcasts batched events plus one snapshot per room. PostgreSQL stores only accounts, avatar choice, the room/map definition and diagnostic records.

Movement is **tile-stepped and server-paced**. The client sends *intent* as "step to adjacent tile (x, y)". The server validates each step when it arrives (adjacent, in bounds, not blocked) and then applies steps at no more than one per 250 ms per player, using its own clock. Steps that arrive early wait in a short per-player queue (4 steps = 1 s); anything beyond that, or anything invalid, is rejected and the player is corrected. Consequences:

- Speed can never exceed 4 tiles/s as measured by the server (BR-05), with no false rejections from ordinary network jitter.
- A burst after a lag spike is played out at legal speed or dropped, never applied at once (BR-11), so other players never see a jump (AC-13).
- All validation is a pure function over `(player movement state, input, grid, now)` — no I/O, unit-tested in isolation.

The client predicts its own steps immediately and reconciles on a `Corrected` message. Remote avatars walk tile-to-tile toward the positions in each snapshot at the nominal step speed, so motion is continuous on a 60 Hz display even though snapshots arrive at 15 Hz.

Local orchestration uses **Aspire**. An AppHost project provisions PostgreSQL as a container (with a data volume), starts the API with the connection injected, and starts the Next.js client with the API address injected. A ServiceDefaults project gives the API OpenTelemetry, health endpoints and service discovery. Aspire is a development-time orchestrator here; it does not change the runtime design, the hub contract or the database schema.

Latency budget for BR-08 (round trip < 100 ms): 50 ms uplink + ≤ 67 ms tick wait + 50 ms downlink ≈ 170 ms worst case before the remote avatar starts moving, inside the 300 ms limit.

## Codebase state and conventions (detected 2026-11-16)

| Area | What exists | Plan follows |
|---|---|---|
| Server | `server/PixelOffice.API/PixelOffice.API.sln` with one project, `net10.0`, nullable on, template `Program.cs` (weather endpoint), OpenAPI package only. No DbContext, auth, tests, or warnings-as-errors. | Keep the single web project; add vertical slices under `Features/`; add one test project. |
| Client | `app/` Next.js 16.4.6, React 19.3, Tailwind 4, React Compiler; `src/app/` only. No Pixi, SignalR, or test runner. | Add `src/contracts`, `src/net`, `src/store`, `src/game`; add Vitest and Playwright. |
| Docs | `docs/` does not exist. | Create `docs/assets.md` when art lands. |
| SDK | Dev machine has the .NET 10 **runtime** (10.0.14) but only SDKs 9.0.200 and 11.0.100-rc.3; the original `global.json` pinned 10.0.x and could not resolve. | Decided 2026-11-16 (Dyg, no new install): `global.json` uses `rollForward: latestMajor` + `allowPrerelease: true`, so the SDK 11 RC builds. Projects still target `net10.0` with C# 14 and run on the 10.0 runtime. Revisit once SDK 11 is released or SDK 10 is installed. |

## Affected Projects & Files

| Project | New / changed files | Purpose |
|---|---|---|
| `server/PixelOffice.API/PixelOffice.API` | `PixelOffice.API.csproj`, `Program.cs` | Warnings as errors, packages, composition root, remove weather template |
| | `Data/AppDbContext.cs`, `Data/Configurations/*.cs`, `Data/Migrations/*`, `Data/DbSeeder.cs` | EF Core model, migration, seed of default room + map |
| | `Features/Auth/` — `AuthEndpoints.cs`, `JwtTokenService.cs`, `JwtOptions.cs` | Register, sign in, token issue |
| | `Features/Players/` — `Player.cs`, `AvatarCatalog.cs`, `PlayerEndpoints.cs` | Account, avatar choice |
| | `Features/Rooms/` (plural: avoids a `Room` class inside a `Room` namespace) — `Room.cs`, `Map.cs`, `RoomEndpoints.cs`, `TiledMapParser.cs`, `CollisionGrid.cs`, `MovementRules.cs`, `PlayerMovementState.cs`, `RoomState.cs`, `RoomRegistry.cs`, `RoomHub.cs`, `RoomTicker.cs`, `RoomContracts.cs`, `RoomOptions.cs` | Map definition, pure movement validation, live state, hub, tick |
| | `Features/Diagnostics/` — `DiagnosticRecord.cs`, `DiagnosticsWriter.cs`, `DiagnosticsRetentionService.cs` | BR-18 records and 30-day purge |
| | `Assets/maps/office.tmj` | Tiled export of the default office, seeded into the database |
| `server/PixelOffice.API/PixelOffice.API.Tests` (new) | `Movement/`, `Room/`, `Hub/`, `Persistence/`, `Fixtures/` | Unit and integration tests |
| `server/PixelOffice.API/PixelOffice.AppHost` (new) | `PixelOffice.AppHost.csproj`, `AppHost.cs` | Aspire AppHost: PostgreSQL container + database `pixeloffice`, the API project, the Next.js client |
| `server/PixelOffice.API/PixelOffice.ServiceDefaults` (new) | `PixelOffice.ServiceDefaults.csproj`, `Extensions.cs` | Shared OpenTelemetry, health checks, service discovery, HTTP resilience (`AddServiceDefaults`, `MapDefaultEndpoints`) |
| `app/` | `src/contracts/contracts.ts` | Hand-maintained mirror of `RoomContracts.cs` and API DTOs |
| | `src/net/api.ts`, `src/net/session.ts`, `src/net/roomConnection.ts` | REST client, token storage, SignalR singleton |
| | `src/store/gameStore.ts`, `src/store/useMembers.ts` | Plain store read by the Pixi ticker; React subscribes to members/UI state only |
| | `src/game/` — `PixiRoom.tsx`, `mapRenderer.ts`, `avatarSprite.ts`, `input.ts`, `localPlayer.ts`, `remotePlayer.ts`, `pathfinding.ts`, `collision.ts` | Rendering, input, prediction, interpolation, A* |
| | `src/app/sign-in/page.tsx`, `src/app/room/page.tsx`, `src/app/room/*` panels | Routes and UI panels |
| | `public/assets/` tileset + 6 avatar sheets | Art |
| | `e2e/*.spec.ts`, `vitest.config.ts`, `playwright.config.ts`, `package.json` scripts | Tests |
| `docs/` | `assets.md` | Art pack licences |

## Dependencies to add (needs approval — see CLAUDE.md)

| Side | Package | Why |
|---|---|---|
| Server | `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT bearer for API and hub |
| Server | `Microsoft.AspNetCore.SignalR.Protocols.MessagePack` | MessagePack hub protocol |
| Server | `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design` | EF Core 10 + PostgreSQL through the Aspire client integration (`AddNpgsqlDbContext`; adds health check and telemetry), migrations |
| AppHost | `Aspire.AppHost.Sdk`, `Aspire.Hosting.PostgreSQL`, `Aspire.Hosting.JavaScript` (all Aspire packages pinned to 13.6.5 in T02; 13.4.6 has published advisories) | Orchestration, PostgreSQL container resource, Next.js resource (`AddNextJsApp`, currently experimental: `ASPIREJAVASCRIPT001`) |
| ServiceDefaults | Packages from the Aspire service-defaults template (OpenTelemetry exporters and instrumentation, `Microsoft.Extensions.ServiceDiscovery`, `Microsoft.Extensions.Http.Resilience`) | Telemetry, health, discovery |
| Server tests | `xunit` (+ runner, `Microsoft.NET.Test.Sdk`), `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.AspNetCore.SignalR.Client`, `Microsoft.AspNetCore.SignalR.Protocols.MessagePack`, `Microsoft.Extensions.TimeProvider.Testing`, `Testcontainers.PostgreSql` | Hub integration tests, fake clock, real PostgreSQL for persistence tests |
| Client | `pixi.js` (v8), `@microsoft/signalr`, `@microsoft/signalr-protocol-msgpack` | Rendering, realtime |
| Client dev | `vitest`, `@playwright/test` | Unit and two-browser e2e |

No state library: the store is a plain module with `useSyncExternalStore`. Password hashing uses `PasswordHasher<T>` from the ASP.NET Core shared framework (no extra package). Exact versions are pinned during the task that adds them, after checking current docs (Context7) for PixiJS v8 and SignalR.

## Domain Model

**Persistent**

- `Player` — `Id` (Guid), `Email` (unique sign-in identifier, stored lower-cased), `PasswordHash`, `DisplayName` (1–24 chars, trimmed, **not unique** — BR-16), `AvatarId` (int, must exist in `AvatarCatalog` — BR-17), `CreatedAt`.
- `AvatarCatalog` — static code catalogue of the 6 preset looks (`Id` 1–6, `Key`, sprite sheet name). Not a table: the set is fixed in this slice and must match the client's sprite sheets. Default avatar is `1`.
- `Map` — `Id`, `Name` (unique), `Version` (int), `ContentHash` (SHA-256 of the source file, so the seeder can detect a change — added in T05), `TiledJson` (jsonb). One row: the open-plan office.
- `Room` — `Id`, `Slug` (unique, `"default"`), `Name`, `MaxPlayers` (10 — BR-02), `MapId`.
- `DiagnosticRecord` — `Id` (long), `Type` (`Joined`, `Left`, `GraceExpired`, `SessionReplaced`, `MoveRejected`), `PlayerId`, `RoomId`, `OccurredAt` (UTC), `Detail` (short text, e.g. rejection reason and count) — BR-18.

**Live only (never persisted — BR-13)**

- `CollisionGrid` — width, height, `bool[] blocked`, spawn tile, default facing (`Down`). Built once from the Tiled JSON: any non-empty tile in a layer named `walls` or `furniture` is blocked; the `spawn` object layer holds one point; `floor` is render-only; `zones` is reserved and ignored.
- `Presence` — `PlayerId`, `DisplayName`, `AvatarId`, `ConnectionId`, `Status` (`Connected` | `Disconnected`), `GraceUntil`, `TokenExpiresAt`, `LastActivityAt`, and a `PlayerMovementState`.
- `PlayerMovementState` — authoritative tile `(X, Y)`, `Facing`, `Epoch` (int, bumped on every correction), `LastAppliedSeq`, `Queue` (≤ 4 pending steps), `NextStepAt`.
- `RoomState` — presences keyed by `PlayerId`, the grid, an outbox of pending events, a dirty flag. All access under one lock per room.

**Invariants**

| Rule | Enforced by |
|---|---|
| BR-01 identity from sign-in | `PlayerId` read only from the JWT `sub` claim in the hub; no hub method accepts a player id |
| BR-02 capacity 10 | `RoomState.Join` counts all presences, including those in grace |
| BR-03 single presence | `RoomState.Join` on an existing presence swaps `ConnectionId`, keeps position, returns the old connection id to be told `SessionReplaced` and aborted |
| BR-04 spawn | New presence starts at `grid.Spawn`, facing `Down` |
| BR-05, BR-11 speed | `MovementRules.TryEnqueue` (queue ≤ 4) + `MovementRules.Drain` (one step per 250 ms on the server clock; `NextStepAt` is schedule-based so tick granularity neither adds nor removes speed) |
| BR-06 blocked / boundary | `MovementRules.TryEnqueue` checks the target against the grid, relative to the position after already-queued steps |
| BR-07 correction | Rejection clears the queue, bumps `Epoch`, returns the authoritative tile; hub sends `Corrected` to the caller immediately. Later inputs carrying the old epoch are dropped silently, so one bad input causes one correction, not a storm |
| BR-10 grace | `OnDisconnectedAsync` marks `Disconnected`, `GraceUntil = now + 30 s` (only if the closing connection is still the current one); `Join` within grace restores; the tick removes expired presences |
| BR-14 four directions | A step target must be at Manhattan distance exactly 1 from the projected position |

Time comes from `TimeProvider` everywhere (step pacing, grace, token expiry, retention).

## Persistence (EF Core)

- `AppDbContext` with `DbSet<Player>`, `DbSet<Room>`, `DbSet<Map>`, `DbSet<DiagnosticRecord>`; Fluent `IEntityTypeConfiguration<T>` per entity in `Data/Configurations`; snake_case not required — default naming.
- Keys and indexes: unique index on `Player.Email`; unique index on `Room.Slug`; index on `DiagnosticRecord.OccurredAt` (purge) and on `(PlayerId, OccurredAt)` (review).
- No tracking by default for reads; avatar update uses `ExecuteUpdateAsync`; purge uses `ExecuteDeleteAsync`.
- Connection: the AppHost declares `AddPostgres("postgres").WithDataVolume()` and `AddDatabase("pixeloffice")`; the API registers the context with `builder.AddNpgsqlDbContext<AppDbContext>("pixeloffice")`. No connection string lives in source or `appsettings.json`. The data volume keeps accounts across AppHost restarts (live positions still reset — BR-13).
- Running the API without the AppHost (CI, a bare `dotnet run`) needs `ConnectionStrings__pixeloffice` set; `dotnet ef migrations add` needs no live database.
- Migration name: `AddAccountsRoomsAndDiagnostics` (single initial migration).
- Seeding: `DbSeeder` runs at startup, idempotent — inserts the map from `Assets/maps/office.tmj` and the `default` room if missing; bumps `Map.Version` and replaces the JSON when the file's content hash changes.
- Migrations are applied on startup in Development only; elsewhere via `dotnet ef database update` (deployment is out of scope for this slice).
- Nothing about positions is ever written (BR-13).

## Application Layer

**Auth (`Features/Auth`)** — validation (BR-20): email format and ≤ 254 chars; password 8–128 chars; display name 1–24 chars after trim. Email is trimmed and lower-cased before comparison and storage; the unique index decides concurrent duplicates (BR-19). Errors as RFC 7807 problem details with per-field messages: 400 validation, 409 "email already in use", 401 "email or password is incorrect" (same response for unknown email and wrong password; a dummy hash is verified for unknown emails so timing does not differ — BR-21). Registration returns a token, so the player is signed in at once, with avatar 1 (BR-22). Token: HS256, claims `sub` (player id) and `exp`; lifetime 12 h (BR-23), configurable for tests; no refresh token, no lockout — on expiry the player signs in again. Display name is read from the database at join time, not from the token.

**Players (`Features/Players`)** — `SetAvatar(playerId, avatarId)`: reject ids outside the catalogue (400, look unchanged); persist; if the player has a live presence, update it and queue a `PlayerUpdated` event for the next tick (≤ 67 ms, well inside BR-17's 1 s).

**Room (`Features/Room`)**

- `MovementRules` (pure, static):
  - `TryEnqueue(state, input, grid) → Accepted | Rejected(reason)`; reasons: `NotAdjacent`, `Blocked`, `OutOfBounds`, `QueueFull`. Stale epoch → `Ignored` (no correction, no record).
  - `Drain(state, now)` — applies at most the steps whose scheduled time has passed, sets `Facing`, advances `LastAppliedSeq`.
- `RoomState`: `Join`, `Leave`, `MarkDisconnected`, `EnqueueMove`, `SetAvatar`, `Tick(now) → (events, snapshot?)`. Returns plain results; performs no I/O.
- `RoomHub` (thin): authenticate → call `RoomState` → return / send `Corrected` to the caller. No broadcasting from hub methods.
- `RoomTicker` (`BackgroundService`, 15 Hz via `PeriodicTimer` on `TimeProvider`): for each room — `Tick(now)` under the lock, then outside the lock send events and, if anything moved, one `Snapshot` to the room group. Also sends `SessionExpired` and aborts connections whose token has expired (the presence then enters grace through the normal disconnect path).
- Malformed hub arguments (wrong types, missing fields) fail SignalR binding; a hub filter turns that into a `MoveRejected` diagnostic and a `Corrected`, and never affects other players.
- Send failures and slow clients: sends are not awaited per client inside the lock; SignalR's per-connection buffer limits apply, so one bad connection cannot stall the tick.

**Diagnostics (`Features/Diagnostics`)** — `DiagnosticsWriter` is a bounded `Channel<DiagnosticRecord>` (drop-oldest when full) drained by a background service that batches inserts; hub and tick never wait on the database. Rejected moves are coalesced per player (at most one record per second, carrying a count) so a flooding client cannot fill the table. `DiagnosticsRetentionService` runs at startup and every 24 h and deletes records with `OccurredAt < now − 30 days`.

**Logging** — one structured event per hub call outcome (`Joined`, `RoomFull`, `SessionReplaced`, `MoveRejected`, …) with player id and room id; positions only at Debug. Logs, traces and metrics are exported through ServiceDefaults' OpenTelemetry setup and viewed in the Aspire dashboard; the same "no positions above Debug" rule applies to span attributes and metric tags.

## API

| Method | Route | Request | Response | Auth/Policy | Covers |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | `{ email, password, displayName }` | 201 `AuthResponse` · 400 · 409 | Anonymous | US-09, AC-25–AC-27 |
| POST | `/api/auth/login` | `{ email, password }` | 200 `AuthResponse` · 401 | Anonymous | US-01, BR-01, AC-28, AC-29 |
| GET | `/api/me` | — | 200 `PlayerProfile` · 401 | Bearer | BR-12 |
| PUT | `/api/me/avatar` | `{ avatarId }` | 204 · 400 · 401 | Bearer | BR-17, AC-22 |
| GET | `/api/avatars` | — | 200 `AvatarOption[]` | Anonymous | BR-17 |
| GET | `/api/rooms/default` | — | 200 `RoomDefinition` (name, maxPlayers, mapVersion, tiled map JSON) · 401 | Bearer | US-04 |
| GET | `/health`, `/alive` | — | 200 | Anonymous, Development only (from `MapDefaultEndpoints`) | AppHost and e2e readiness |

`AuthResponse = { accessToken, expiresAt, player: PlayerProfile }` · `PlayerProfile = { id, displayName, avatarId }`.

**Hub `/hubs/room`** — `[Authorize]`, WebSockets only, MessagePack; token passed via `access_token` query string on the WebSocket request.

| Direction | Method | Payload | Covers |
|---|---|---|---|
| C→S invoke | `Join()` | → `JoinResult { status: Joined \| RoomFull, self?: PlayerInfo, players: PlayerInfo[], epoch, serverTimeMs }` | AC-02, AC-03, AC-10, AC-16, AC-17 |
| C→S send | `Move(MoveInput { epoch, seq, x, y })` | — | AC-04, AC-06, AC-07, AC-13–15 |
| C→S invoke | `Leave()` | — | AC-09 |
| S→C | `Snapshot(RoomSnapshot { tick, serverTimeMs, players: PlayerPosition[] })` | `PlayerPosition { id, x, y, facing, seq }` | AC-05 |
| S→C | `PlayerJoined(PlayerInfo)` | `PlayerInfo { id, displayName, avatarId, x, y, facing, status }` | AC-08 |
| S→C | `PlayerLeft(playerId)` | | AC-09, AC-11 |
| S→C | `PlayerUpdated(PlayerInfo)` | status or avatar change | AC-12, AC-22 |
| S→C (caller) | `Corrected(Correction { epoch, x, y, facing, reason })` | | AC-13, AC-14 |
| S→C (one connection) | `SessionReplaced()` then abort | | AC-16 |
| S→C (one connection) | `SessionExpired()` then abort | | Edge case |

This table **is** the hub contract; any later change needs approval and must land on both sides together.

## UI Contract (for the frontend)

- **Types**: `src/contracts/contracts.ts` mirrors every shape above (no `any`); `Facing = 'up' | 'down' | 'left' | 'right'` sent as 0–3; ids are strings.
- **Session**: token and `expiresAt` in `localStorage` (shared across tabs so AC-16 works). `/room` without a valid token redirects to `/sign-in` and never opens a hub connection (AC-01). A 401 from any call clears the session and shows "Please sign in again".
- **Connection** (`roomConnection.ts`, singleton): WebSockets only, MessagePack, `accessTokenFactory`, automatic reconnect with retries spanning at least 35 s. After every (re)connect call `Join()`; the result fully replaces the store.
- **Local player**: one step per 250 ms while a direction is active; on step start, move immediately and send `Move` with the next `seq` and current `epoch`. On `Corrected`: adopt the new `epoch`, drop pending inputs, snap to the given tile, cancel click-to-move.
- **Remote players**: each snapshot's tile is appended to that avatar's path; the avatar walks it at 250 ms per tile (up to 1.5× when more than 2 tiles behind). Never placed directly at a new tile except on `Join`.
- **Input**: four directions, most recently pressed key wins, release falls back to a key still held; any movement key cancels click-to-move; click runs A* on the same collision grid the server uses; hidden tab clears keys and path.
- **States and messages**

| State | Trigger | UI |
|---|---|---|
| Loading | fetching room definition / joining | "Entering the office…" |
| Room full | `JoinResult.status = RoomFull` | "Room is full. Try again in a moment." + Retry |
| Reconnecting | connection `onreconnecting` | Banner "Reconnecting…", input disabled |
| Session replaced | `SessionReplaced` | Blocking message "This session was opened in another tab." No auto-reconnect |
| Session expired | `SessionExpired` or 401 | "Please sign in again" → `/sign-in` |
| Can't go there | click on blocked/unreachable tile | Brief marker on the tile (~600 ms) |
| Disconnected avatar | `status = Disconnected` | Avatar at 50 % opacity with a small "offline" badge |

- **Names**: rendered as Pixi text / React text nodes only (never HTML) — untrusted input.
- **Assets**: 32 px tiles, `scaleMode = 'nearest'`, sheets in `public/assets`, licences in `docs/assets.md`.

## Testing Strategy

Server unit tests use `FakeTimeProvider`. Hub integration tests use `WebApplicationFactory` with real SignalR clients over the test server and a controllable clock; the tick can be advanced manually in tests. Persistence tests use PostgreSQL via Testcontainers. Network conditions are simulated with a test-only inbound delay option on the hub (honoured only in the `Testing` environment).

| AC | Test type | Test name |
|---|---|---|
| AC-01 | Server integration · e2e | `AC_01_Hub_connection_without_token_is_refused` · `AC-01 visitor is redirected to sign-in` |
| AC-02 | Server integration · e2e | `AC_02_First_join_places_player_at_spawn_with_display_name` · `AC-02 avatar appears at spawn with name` |
| AC-03 | Server unit · integration · client | `AC_03_Eleventh_join_is_refused_and_existing_players_unaffected` · `AC-03 shows room full message` |
| AC-04 | Server unit · client unit | `AC_04_Held_direction_moves_at_most_four_tiles_per_second` · `AC-04 local step loop emits one step per 250 ms` |
| AC-05 | Server integration · client unit · e2e | `AC_05_Second_client_receives_movement_within_budget_with_100ms_round_trip` · `AC-05 remote avatar walks continuously between snapshot tiles` · `AC-05 player B sees A move within 300 ms` |
| AC-06 | Server unit · client unit | `AC_06_Step_into_wall_is_rejected_and_position_unchanged` · `AC-06 prediction does not enter blocked tile` |
| AC-07 | Server unit · client unit | `AC_07_Step_outside_boundary_is_rejected` · `AC-07 prediction stays inside the map` |
| AC-08 | Server integration · e2e | `AC_08_Existing_player_is_told_of_join_within_one_second` · `AC-08 B appears for A` |
| AC-09 | Server integration · e2e | `AC_09_Deliberate_leave_removes_avatar_within_one_second` · `AC-09 B disappears for A` |
| AC-10 | Server unit · integration | `AC_10_Reconnect_within_grace_restores_position_and_facing` |
| AC-11 | Server unit · integration | `AC_11_Grace_expiry_removes_avatar_and_next_join_spawns` |
| AC-12 | Server integration · client unit | `AC_12_Disconnected_player_is_reported_as_disconnected_during_grace` · `AC-12 disconnected avatar is marked` |
| AC-13 | Server unit · integration · client unit | `AC_13_Flood_and_teleport_inputs_are_rejected_and_never_broadcast` · `AC-13 correction snaps local player and clears pending inputs` |
| AC-14 | Server unit · integration | `AC_14_Target_inside_wall_is_rejected_with_correction` |
| AC-15 | Server unit · integration | `AC_15_Burst_after_three_second_lag_travels_no_further_than_speed_allows` |
| AC-16 | Server integration · e2e | `AC_16_Second_connection_replaces_first_and_only_one_avatar_exists` · `AC-16 first tab shows session replaced` |
| AC-17 | Server integration | `AC_17_After_restart_all_players_rejoin_at_spawn` (new factory instance, same database) |
| AC-18 | Client unit | `AC-18 most recently pressed key wins and movement is never diagonal` |
| AC-19 | Client unit · e2e | `AC-19 path goes around an obstacle in four-direction steps` · `AC-19 click walks around the desk and stops` |
| AC-20 | Client unit | `AC-20 movement key cancels click-to-move` |
| AC-21 | Client unit | `AC-21 click on blocked or unreachable tile does not move and shows indication` |
| AC-22 | Server integration · e2e | `AC_22_Avatar_change_is_broadcast_and_persisted` · `AC-22 other player sees new look; it survives sign-out` |
| AC-23 | Server integration | `AC_23_Two_players_with_same_display_name_are_independent` |
| AC-24 | Server persistence | `AC_24_Retention_deletes_records_older_than_30_days_and_keeps_newer` |
| AC-25 | Server integration · e2e | `AC_25_Registration_creates_account_signs_in_and_uses_first_avatar` · `AC-25 new player registers and enters with their name` |
| AC-26 | Server persistence | `AC_26_Duplicate_email_ignoring_case_and_spaces_is_refused` (includes two concurrent registrations) |
| AC-27 | Server integration · client unit | `AC_27_Invalid_registration_names_the_field_and_creates_nothing` · `AC-27 form shows the message for the wrong field` |
| AC-28 | Server integration · e2e | `AC_28_Correct_credentials_sign_in` · `AC-28 registered player signs in and enters` |
| AC-29 | Server integration | `AC_29_Wrong_password_and_unknown_email_get_the_same_refusal` |
| AC-30 | Server integration · client unit | `AC_30_Token_older_than_12_hours_is_refused_by_api_and_hub` · `AC-30 expired session redirects to sign-in` |

## Risks, Rollout & Open Points

**Open points — need a decision before or at approval**

1. ~~Account creation is not in the spec.~~ **Resolved 2026-11-16 (Dyg):** self-service email + password registration with server-issued JWTs. Now in the spec as US-09, BR-19–BR-23, AC-25–AC-30.
2. ~~Token lifetime 12 h, no refresh.~~ **Resolved 2026-11-16:** fixed by BR-23.
3. **Token in `localStorage`.** Required for cross-tab sign-in (AC-16) with a bearer-token hub; exposed to script injection. Mitigation in this slice: no player-supplied HTML anywhere. An httpOnly cookie is the alternative if preferred.
4. **Avatar set as a code catalogue, not a table.** Simpler and matches "fixed set"; a table can come with custom avatars later.
5. **Docker is required** on every dev machine and CI: Aspire runs PostgreSQL in a container, and persistence tests use Testcontainers. Tests deliberately do not go through the AppHost (`Aspire.Hosting.Testing`), because hub tests need a controllable clock and manual ticks inside the API process.
8. **Aspire is a new stack entry.** It is an addition, not an alternative to anything in CLAUDE.md's stack table; T02 adds the row and the run command there. Say so if you would rather record it as an ADR in `docs/`.
9. **AppHost also starts the Next.js client** (T39) through `AddNextJsApp`, which is still marked experimental. The teammate can keep using `npm run dev` on its own; drop T39 if you want the AppHost to cover the server side only.
6. **Art pack** (tileset + 6 character sheets, 32 px) is not chosen; its licence must be recorded in `docs/assets.md`. Until then T23 uses flat-colour placeholders.
7. **Map size** assumed 30 × 20 tiles.

**Risks**

- *Tile-stepped interpolation vs. "smooth"*: remote motion begins up to ~170 ms after the real step; acceptable for BR-08, verified by the AC-05 tests.
- *Queue of 4 steps*: a client lagging more than 1 s gets corrected back rather than catching up. This is the intended BR-11 behaviour, visible as a snap for the lagging player only.
- *SignalR over the test server with WebSockets + MessagePack* needs a confirmed setup; if the in-memory server cannot do it, hub tests run against Kestrel on a random port.
- ~~*Two repositories*~~ Resolved 2026-11-16: server, client, specs and docs now share one repository (github.com/my-ai-projects-2026/office-pixel), so a contract change to `RoomContracts.cs` and `contracts.ts` can land in one PR. T15 and T24 are still paired and checked by the e2e suite.
- *Single instance only*: live state is in process memory; a restart resets positions by design (BR-13).

**Rollout**: local development only in this slice. One command starts PostgreSQL, the API and the client: `dotnet run --project server/PixelOffice.API/PixelOffice.AppHost` (or `aspire run`). Deployment through Aspire is out of scope. No feature flag — nothing exists to protect.
