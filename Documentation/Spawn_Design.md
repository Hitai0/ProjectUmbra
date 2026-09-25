# Spawn director: Vampire Survivors reference and Umbra adaptation

## Evidence and limits

[Luca Galante's Nintendo interview](https://www.nintendo.com/jp/topics/article/3f3d9c44-6cc5-4197-a31b-1397f229c03b) explains the finite encounter arc and boss endpoint; Nintendo also describes intensity increasing over time. [Poncle's playable-demo page](https://poncle.itch.io/vampire-survivors) is the primary reference for the survival game. Neither publishes the current internal spawn algorithm. No proprietary code or exact spawn tables were available for verification. The rules below are Umbra design choices inspired by this encounter style, not claims about the original game's implementation.

## Analysis of Umbra's previous system

A continuously increasing cap/interval produced more enemies but little variation in encounter rhythm. A 10–15m world-space ring could create enemies inside the camera at wide aspect ratios or zoom levels. A fixed 40m recycle radius could also remove visible enemies. Cycling through a fixed actor pool alone gave little control over elite density. Existing enemies gained damage/speed when the clock advanced, making a familiar creature's threat change without a visual cue.

## Applied design

- **Minute-based waves:** fourteen authored rows vary population target, interval, batch size and elite budget. Pressure waves alternate with relief at minutes 4, 8 and 12; relief reduces new spawns without deleting living enemies.
- **Population refill:** below half the target, the next tick comes twice as quickly. Each tick still adds at most its batch size; there is no unlimited catch-up after pauses or slow frames.
- **Composition:** elites become eligible at 0:45 and receive priority when the wave's elite budget is unmet and a population slot is available. Elite limits apply to new spawns; existing elites persist into relief waves.
- **Directional pressure:** minutes 3, 7 and 11 favour one viewport edge. If that edge is outside the finite field, a bounded search falls back to the other edges. Other waves use all four edges. This creates a front to evade without introducing a new enemy asset or attack type.
- **Placement fairness:** raycast from just outside the actual camera viewport onto the ground, require walkability, 8m player clearance, 1.2m spacing from other actors, and an offscreen margin for a 3m-tall sprite. Both aspect ratio and zoom affect placement. If no valid point exists, skip rather than spawn visibly or inside the player.
- **Recycling:** ordinary actors can recycle beyond 40m only when outside an expanded viewport. No kill, XP, amber or death effect is produced. Champion and guardian are exempt.
- **Readable enemy stats:** HP, movement speed and contact damage are set at spawn. The same living enemy does not become stronger at a minute boundary.

Champion still arrives at minute 2 through a reserved actor, guardian at minute 14, and timeout at minute 15. Existing XP, loot, player damage and boss tuning remain unchanged. The ordinary population tops out at 64, plus the reserved champion; the 72-actor pool bounds allocation. This is a resource-conscious adaptation, not an attempt to match the original game's enemy counts.

## Wave tuning (our values)

| Minute | Target | Batch | Interval (s) | Elite budget | Pattern |
|---|---:|---:|---:|---:|---|
| 0 | 18 | 4 | 1.25 | 1 after 0:45 | Surround |
| 1 | 24 | 5 | 1.15 | 2 | Surround |
| 2 | 28 | 5 | 1.10 | 2 | Surround + champion |
| 3 | 34 | 6 | 1.00 | 3 | Left front |
| 4 | 28 | 5 | 1.10 | 1 | Relief |
| 5 | 38 | 6 | 0.95 | 4 | Surround |
| 6 | 44 | 7 | 0.90 | 4 | Surround |
| 7 | 48 | 7 | 0.85 | 5 | Right front |
| 8 | 40 | 6 | 1.00 | 2 | Relief |
| 9 | 52 | 8 | 0.80 | 5 | Surround |
| 10 | 56 | 8 | 0.75 | 6 | Surround |
| 11 | 60 | 8 | 0.70 | 6 | Bottom front |
| 12 | 52 | 7 | 0.85 | 3 | Relief |
| 13 | 64 | 8 | 0.65 | 8 | Final horde |

Tune actual encounter density, XP per minute, deaths and frame time through playtests. A target is a population ceiling/refill goal, not a promise that that many enemies are onscreen. Large views and finite map edges may restrict valid spawn locations. Existing pairwise enemy separation remains a performance limit; increasing the cap substantially would require profiling and likely spatial partitioning.
