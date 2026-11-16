# Shared Room Movement — Specification

Status: Approved (2026-11-16; accounts update US-09, BR-19–BR-23, AC-25–AC-30 approved 2026-11-16)   ·   Owner: Dyg (Arnold Mercado)   ·   Last updated: 2026-11-16

## Summary
The first slice of a Discord/Gather-style collaboration space shown as a 2D virtual office. Signed-in players enter one shared default room (a single simple office), walk around a tile-based map using keyboard or mouse, and see each other move in real time. Players can pick their avatar from a preset set. The system, not the player's device, decides what movement is allowed. This slice covers presence and movement only. Chat, voice, zones, invites and custom maps come in later specs.

## Problem & Goals
- Problem: remote teams lack a lightweight, spatial way to see who is around and to move toward people. Before adding chat or voice, the shared space itself must feel live, fair and stable.
- Goals:
  - Two or more players in the same room see each other's avatars and movements, smoothly and with low delay.
  - Movement follows the room's rules (walls, speed) and cannot be bypassed by a modified client.
  - Players who briefly lose connection return to where they were.
  - The room works reliably with up to 10 players moving at the same time.

## Non-goals
- Text chat, voice, video, reactions or emotes.
- Multiple rooms, invite links, guest access, room creation or map editing.
- Collision between players (avatars may overlap).
- Diagonal movement.
- Custom or uploaded avatars; players only choose from a fixed set of preset looks.
- Changing the display name inside the room (it comes from the player's account).
- Mobile or touch support.
- Proximity or zone-based features.
- Keeping live positions across a server restart.
- Confirming that an email address belongs to the person registering (no verification message is sent).
- Password reset or recovery, changing email or password, deleting an account.
- Signing in through another service (Google, Microsoft, company sign-in).

## Users & Roles
| Role | What they can do in this feature |
|---|---|
| Player (signed-in user) | Enter the default room, move their avatar by keyboard or mouse, choose a preset avatar, see other players and their names, leave the room, rejoin after a disconnect |
| Unauthenticated visitor | Nothing in the room; is directed to sign in. Can create an account and sign in |

## User Stories
- **US-01** As a player, I want to sign in and enter the shared room so that I can be present with my team.
- **US-02** As a player, I want to move my avatar around the map with the keyboard or by clicking a destination so that I can navigate the space easily.
- **US-03** As a player, I want to see other players' avatars and names moving smoothly so that the room feels alive.
- **US-04** As a player, I want walls and furniture to block movement so that the space behaves like a real office.
- **US-05** As a player, I want to see when others join or leave so that I know who is currently here.
- **US-06** As a player, I want a short connection drop not to throw me out so that a refresh or Wi-Fi blip does not lose my place.
- **US-07** As the room owner, I want movement to be validated by the system so that no player can cheat by teleporting or speeding.
- **US-08** As a player, I want to choose my avatar from a set of looks so that I can be recognised by my teammates.
- **US-09** As a visitor, I want to create an account with my email, a password and a display name so that I can sign in and enter the room.

## Business Rules
- **BR-01** Only signed-in players can enter the room. A player's identity is decided by the system from their sign-in, never from what their device claims.
- **BR-02** The room holds at most 10 players at the same time. An 11th entry attempt is refused with a clear "room is full" message.
- **BR-03** A player can have only one active presence in the room. If the same player connects again, the newer connection takes over and the older one is told it was replaced.
- **BR-04** A player entering for the first time (or after the grace period has expired) appears at the room's designated spawn point, facing a default direction.
- **BR-05** Maximum movement speed is 4 tiles per second, measured by the system. Faster movement is not accepted.
- **BR-06** A player cannot move onto blocked tiles (walls, furniture) or outside the map boundary.
- **BR-07** When a requested movement breaks BR-05 or BR-06, the system rejects it and puts the player back at their last valid position. The player sees the correction within 1 second.
- **BR-08** Under normal network conditions (round-trip delay under 100 ms), another player's movement appears on screen within 300 ms of it happening, and avatars move continuously with no visible teleporting.
- **BR-09** When a player joins or leaves, all other players see the avatar appear or disappear within 1 second.
- **BR-10** A disconnected player's avatar stays in the room, marked as disconnected, for a grace period of 30 seconds. Reconnecting within that time restores their last position and facing. After 30 seconds the avatar is removed and the next entry starts at the spawn point.
- **BR-11** Movement that arrives late in a burst (for example after a lag spike) cannot make a player cover more distance than BR-05 allows for the elapsed time.
- **BR-12** Every avatar shows its owner's display name.
- **BR-13** Live positions are held only while the room is running. After a system restart, all players re-enter at the spawn point. Only accounts, avatar choices and the map definition are stored permanently in this slice (plus diagnostic records under BR-18).
- **BR-14** Movement is in four directions only (up, down, left, right); there are no diagonal moves. If a player holds two direction keys at once, only the most recently pressed one applies.
- **BR-15** Click-to-move: when a player clicks a walkable tile that can be reached, the avatar walks there automatically along a path that goes around blocked tiles, in four-direction steps, at no more than the BR-05 speed. Pressing any movement key cancels the automatic walk and returns control to the keyboard. A new click replaces the current destination. Clicking a blocked or unreachable tile does nothing except a brief "can't go there" indication.
- **BR-16** Display names do not have to be unique. Two players may have the same name.
- **BR-17** A player chooses their avatar from a fixed set of preset looks. The choice is saved with their account and used on every later entry. A change made while in the room becomes visible to everyone in the room within 1 second.
- **BR-18** Diagnostic records (joins, leaves, replaced sessions, rejected movements) are kept for 30 days and then deleted.
- **BR-19** Anyone can create an account by giving an email address, a password and a display name. An email address can belong to only one account; upper and lower case and surrounding spaces are ignored when comparing. A second registration with an email already in use is refused.
- **BR-20** At registration: the email must look like an email address and be at most 254 characters; the password must be 8 to 128 characters; the display name must be 1 to 24 characters after removing surrounding spaces. A registration that breaks any of these is refused with a message saying which field is wrong, and no account is created.
- **BR-21** A player signs in with their email and password. If either is wrong, sign-in is refused with one message ("email or password is incorrect") that does not reveal whether the email has an account.
- **BR-22** A new account starts with the first preset avatar until the player chooses another (BR-17). Creating an account signs the player in immediately.
- **BR-23** A sign-in lasts 12 hours. After that the player must sign in again (see "Sign-in expires while the player is in the room").

## Acceptance Criteria
- **AC-01** (US-01, BR-01) Given a visitor who is not signed in, when they open the room, then they are sent to sign in and no avatar is created.
- **AC-02** (US-01, BR-01, BR-04) Given a signed-in player, when they enter the room for the first time, then their avatar appears at the spawn point with their display name.
- **AC-03** (US-01, BR-02) Given 10 players in the room, when an 11th signed-in player tries to enter, then entry is refused with a "room is full" message and the 10 existing players are unaffected.
- **AC-04** (US-02, BR-05) Given a player holding a movement key, when 1 second passes, then their avatar has moved no more than 4 tiles.
- **AC-05** (US-03, BR-08) Given two players in the room, when player A walks across the map, then player B sees A's avatar move continuously and within 300 ms of A's movement (round-trip delay under 100 ms).
- **AC-06** (US-04, BR-06) Given a player facing a wall, when they keep pressing toward it, then their avatar stays in front of the wall and does not enter it.
- **AC-07** (US-04, BR-06) Given a player at the map edge, when they keep moving outward, then they stay inside the map boundary.
- **AC-08** (US-05, BR-09) Given player A in the room, when player B enters, then A sees B's avatar and name appear within 1 second.
- **AC-09** (US-05, BR-09) Given players A and B in the room, when B leaves deliberately, then A sees B's avatar disappear within 1 second.
- **AC-10** (US-06, BR-10) Given a player who loses connection, when they reconnect within 30 seconds, then their avatar is at the same position and facing as before the drop.
- **AC-11** (US-06, BR-10) Given a player who loses connection, when 30 seconds pass without reconnection, then their avatar is removed for everyone and their next entry starts at the spawn point.
- **AC-12** (US-06, BR-10) Given a player who is disconnected, when other players look at the room during the grace period, then that avatar is visibly marked as disconnected.
- **AC-13** (US-07, BR-05, BR-07) Given a player whose device sends a movement far faster than allowed, when the system receives it, then the move is rejected and the player is returned to their last valid position within 1 second, and other players never see the illegal jump.
- **AC-14** (US-07, BR-06, BR-07) Given a player whose device sends a position inside a wall, when the system receives it, then it is rejected and the player is returned to their last valid position.
- **AC-15** (US-07, BR-11) Given a player who experienced a 3-second lag spike, when their delayed movements arrive together, then their avatar travels at most the distance allowed for the elapsed time under BR-05.
- **AC-16** (BR-03) Given a player already in the room, when they open the room in a second browser tab, then the second tab takes over and the first tab shows a "session replaced" message; only one avatar exists.
- **AC-17** (BR-13) Given players in the room, when the system restarts, then players can re-enter and all appear at the spawn point.
- **AC-18** (US-02, BR-14) Given a player holding two direction keys at once, when they move, then the avatar travels in only one direction (the most recently pressed) and never diagonally.
- **AC-19** (US-02, BR-15, BR-05) Given a walkable tile that can only be reached by going around an obstacle, when the player clicks it, then the avatar walks around the obstacle to that tile at no more than 4 tiles per second and stops there.
- **AC-20** (US-02, BR-15) Given an avatar walking to a clicked destination, when the player presses a movement key, then the automatic walk stops and the keyboard moves the avatar.
- **AC-21** (US-02, BR-15) Given a player who clicks a wall tile or a tile that cannot be reached, when the click happens, then the avatar does not move and a brief "can't go there" indication is shown.
- **AC-22** (US-08, BR-17) Given a player in the room, when they choose a different preset avatar, then other players see the new look within 1 second, and the same look is used the next time the player signs in.
- **AC-23** (BR-16) Given two players with the same display name, when both are in the room, then both avatars appear with that name and each player controls only their own.
- **AC-24** (BR-18) Given a diagnostic record older than 30 days, when the retention check runs, then that record no longer exists, and records newer than 30 days are still available.
- **AC-25** (US-09, BR-19, BR-22) Given a visitor with an email that has no account, when they register with a valid email, password and display name, then an account is created, they are signed in, and on entering the room their avatar shows that display name and the first preset look.
- **AC-26** (US-09, BR-19) Given an account registered as "ana@example.com", when someone registers with " Ana@Example.com ", then registration is refused with an "email already in use" message and still only one account exists for that email.
- **AC-27** (US-09, BR-20) Given a visitor registering, when the email is malformed, the password is shorter than 8 characters, or the display name is empty or longer than 24 characters, then registration is refused, the message says which field is wrong, and no account is created.
- **AC-28** (US-01, BR-21) Given a registered player, when they sign in with the correct email and password, then they are signed in and can enter the room.
- **AC-29** (US-01, BR-21) Given a registered player, when a sign-in is attempted with the wrong password, or with an email that has no account, then both attempts are refused with the same "email or password is incorrect" message.
- **AC-30** (US-01, BR-23) Given a player who signed in more than 12 hours ago, when they open the room, then they are sent to sign in and no avatar is created.

## Edge Cases & Error Handling
- Missing, invalid or expired sign-in → entry refused or session ended with a "please sign in again" message; no avatar is left behind.
- Sign-in expires while the player is in the room → the player is told, their avatar enters the BR-10 grace period, and they must sign in again to return.
- Movement request with invalid values (not a number, extreme values) → rejected as in BR-07; no crash or effect on other players.
- Two players try to occupy the same tile → allowed (no player collision in this slice).
- Rapid direction changes or many key presses → movement still follows BR-05; no extra speed.
- Click on a tile while another player's avatar stands on it → allowed (avatars may overlap).
- Click-to-move destination becomes unreachable while walking (not possible with a static map, but if the map changes) → the avatar stops at the last valid tile.
- Click while the avatar is already walking → the new destination replaces the old one (BR-15).
- Browser tab hidden or in the background → the player's avatar stops moving and stays in place; it is not removed unless the connection drops.
- Very high delay (over 1 second) → other players may see a stationary avatar followed by a catch-up; no teleporting beyond BR-11.
- Player disconnects and the room is then empty → the room remains available; no data loss beyond live positions.
- Room full while a disconnected player's grace period is running → their reserved place counts toward the 10 until the grace period ends, so they can always return (AC-10).
- Server restart → live positions are lost by design (BR-13); players see a "reconnecting" state and then re-enter.
- Avatar choice that is not in the preset set → rejected; the player keeps their current look.
- Time zones and dates → only relevant to the 30-day retention in BR-18, which is counted in whole days from the time the record was created.
- Two registrations with the same email at the same moment → exactly one succeeds; the other gets the "email already in use" message (BR-19).
- Registration or sign-in with empty fields → refused with a message naming the missing field; nothing is created.
- Display name made only of spaces → treated as empty and refused (BR-20).
- Many failed sign-in attempts in a row → each is refused as in BR-21; there is no lockout in this slice (see Open Questions).
- Large volumes → the design target is exactly 10 players moving at once; behaviour above 10 is out of scope (BR-02 refuses entry).

## Data (conceptual)
- **Player account**: unique identity, email (unique, used only to sign in), password (kept only in a protected, unreadable form), display name (not unique), chosen avatar.
- **Avatar options**: the fixed set of preset looks a player can choose from.
- **Room**: name, maximum players (10), the map it uses.
- **Map**: a single simple open-plan office room; grid size, which tiles are walkable or blocked, spawn point, boundary.
- **Presence** (live only): player, current position, facing direction, status (connected / disconnected-in-grace), time of last activity.
- **Diagnostic record**: joins, leaves, replaced sessions and rejected movements, for abuse and troubleshooting; kept 30 days.

## Permissions & Security
- Only signed-in players can enter; identity comes from the sign-in, not from the player's device.
- All players in the room can see every other player's display name, chosen avatar, position and connection status. No other personal data is shown.
- Players can only control their own avatar.
- Avatars come only from the preset set, so no player-supplied images are shown to others.
- Rejected movements and session takeovers are recorded for review and deleted after 30 days.
- Display names must be treated as untrusted text when shown to others.
- A player's email is never shown to other players.
- Passwords are never stored in readable form and never appear in logs or diagnostic records.

## Non-functional Requirements
- Capacity: 10 simultaneous players in one room, all moving.
- Responsiveness: see BR-08 and BR-09.
- Smoothness: avatar movement appears continuous on a normal 60 Hz display.
- Reliability: a single player's bad connection or bad data must not slow down or crash the room for others.
- Browsers: current desktop versions of Chrome, Edge, Firefox and Safari (keyboard and mouse).
- Language: English only.
- Time zone and localisation: not applicable except for BR-18 retention counting.

## Open Questions
- Resolved by approval: first set contains 6 preset avatars.
- Resolved by approval: display name comes from the account and cannot be changed in the room.
- Resolved by approval: single open-plan office with desks and a spawn point; no extra mandatory areas.
- Resolved 2026-11-16: accounts are created by self-service registration with email and password.
- Resolved by approval 2026-11-16: no email verification and no password reset in this slice (non-goals).
- Resolved by approval 2026-11-16: a sign-in lasts 12 hours (BR-23).
- Resolved by approval 2026-11-16: no lockout or slowdown after repeated failed sign-ins in this slice.