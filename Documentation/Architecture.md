# Architecture and boundaries

- `CombatRules.cs`: pure C# balance rules, rune definitions and progression thresholds. No Unity dependency.
- `UmbraPrototype.cs`: prototype orchestration, seeded world generation, movement, enemy state, projectiles, loot and local persistence.
- `UmbraHud.cs`: self-contained scaled HUD, minimap, rune selection, help and pause.
- `ForestVertex.shader`: URP vertex-color shader for batched grass and ground details.
- `UmbraProjectSetup.cs`: editor configuration, URP resource creation, scene bootstrap, smoke checks, screenshots and Web build menu.

All game state currently lives locally. Client-held health, XP, shards and damage are not authoritative and cannot be trusted in a multiplayer release. Do not connect trading or a real economy to this persistence.

## Prototype rendering

Orthographic 3D camera, transparent pixel character sprites, shared environment materials, statically combined environment meshes, one directional sun, a small point light, modest post-processing, and a single grass mesh. The original HDRP package remains only to preserve template compatibility; the actual prototype uses URP.

## Known limits

- Fixed camera orientation; a single character facing sprite with mirrored directions and two step poses.
- Placeholder faceted foliage and simple original pixel sprites, not production HD-2D art.
- No sound yet.
- Right-click movement steers directly and stops at obstacles; it does not route around them.
- The river is blocked except at the bridge; simple cylindrical trunk avoidance replaces full world physics.
- Minimap is schematic; it is not a full cartographic rendering.
- Rune/help panels suppress mouse gameplay input; keyboard movement remains live. Escape pauses the simulation.
- Immediate-mode UI is for prototyping, not the intended final production UI.
- Mobile/touch and low-end browser performance are not validated.
- The editor command file is a local developer convenience supporting only a small fixed list of actions, never arbitrary code or shell execution. `.umbra-*` files are ignored by Git.

## Next network milestone

Extract the world simulation behind a fixed tick. Keep presentation in the client; validate movement, hit tests, cooldowns and rewards on the server. Implement a browser-compatible transport, prediction/reconciliation, nearby-entity subscriptions, server-owned inventory, idempotent rewards and transactional persistence before adding trade. Test two players first, then load-test a single zone. No player-capacity claims are made for a free-tier VM.
