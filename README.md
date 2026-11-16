# Pixel Office — Product Overview

Last updated: 2026-11-16 · Owner: Dyg (Arnold Mercado)

This page is the map of the product. Each feature has its own folder under `specs/` with
`spec.md` (what and why), `plan.md` (how) and `tasks.md` (steps). New specs must reuse the
vocabulary below and name the features they depend on.

## Vision

A Discord-style collaboration space rendered as a 2D pixel-art office. Instead of channels,
people walk up to each other. Who you can hear and talk to depends on where you stand.
Inspired by pixtuoid.dev's look and by Gather-style spatial collaboration; built for small
remote teams first.

## Vocabulary

Use these words in every spec. Do not introduce synonyms.

| Term | Meaning |
|---|---|
| **Player** | A signed-in user while they are inside a room. |
| **Account** | The persistent identity behind a player: display name, avatar choice, memberships. |
| **Avatar** | The player's visible sprite in the room. First slice: chosen from a preset set. |
| **Space** | A team's area: owns one or more rooms, members and invites. (Not in the first slice; there is one implicit space with one default room until `spaces-and-invites`.) |
| **Room** | One map that players can be in at the same time. Has a capacity and a spawn point. |
| **Map** | The tile grid a room uses: walkable and blocked tiles, spawn point, zones. |
| **Tile** | The unit of movement and collision. |
| **Zone** | A named area of a map (meeting room, lounge) that changes chat or voice behaviour. |
| **Presence** | A player's live state: position, facing, connection status. Never persisted. |
| **Proximity** | Being within a set distance (in tiles) of another player. |
| **Grace period** | The 30 seconds a disconnected player's avatar stays before removal. |
| **Diagnostic record** | A stored log of joins, leaves, replacements and rejected actions; kept 30 days. |

## Feature roadmap

Status: Idea → Spec draft → Spec approved → Planned → In progress → Done

| # | Feature (folder) | Status | Depends on | One line |
|---|---|---|---|---|
| 1 | `shared-room-movement` | Spec approved | — | Sign in, enter the default room, walk by keyboard or mouse, see others move, reconnect within the grace period. |
| 2 | `room-text-chat` | Idea | 1 | Room-wide text chat with history; messages persisted. |
| 3 | `proximity-chat` | Idea | 1, 2 | Messages reach only players within proximity; whisper distance. |
| 4 | `zones` | Idea | 1 | Zones on the map; entering one scopes chat (and later voice) to that zone. |
| 5 | `voice-and-video` | Idea | 1, 4 | Voice and camera, audio fades with distance, zones become private calls. |
| 6 | `spaces-and-invites` | Idea | 1 | Multiple spaces and rooms, owner and member roles, invite links, capacity per room. |
| 7 | `avatars-and-profiles` | Idea | 1 | Custom avatars, status text, name changes, profile card on hover. |
| 8 | `presence-and-status` | Idea | 1 | Away/busy/do-not-disturb, idle detection, member list with last seen. |
| 9 | `map-editor` | Idea | 4, 6 | Space owners edit maps and zones in the browser. |

Build order follows the numbers. A feature can start its spec once everything it depends on
is at least **Spec approved**; it can start implementation once its dependencies are **Done**.

## Cross-cutting decisions (apply to every feature)

- The server is authoritative. Clients send intent; the server decides what happened.
- Identity always comes from the sign-in, never from the client.
- Live state is in memory; only accounts, maps, messages and diagnostic records are persisted.
- Timing targets unless a spec says otherwise: other players see an action within 1 second;
  movement within 300 ms at under 100 ms round trip.
- Diagnostic records are kept 30 days.
- Desktop browsers only until a spec says otherwise. English only.

## Out of scope for the product (for now)

- Mobile apps.
- Public or anonymous spaces.
- Screen sharing and whiteboards (revisit after `voice-and-video`).
- Integrations with Slack, Discord or calendars.

## How to add a feature

1. Add a row to the roadmap with status **Idea** and its dependencies.
2. Run `/sdd-spec <feature name>`; it reads this page and the existing specs for vocabulary.
3. Get the spec approved, then `/sdd-plan`, then `/sdd-implement` task by task.
4. Update the status column as it moves.