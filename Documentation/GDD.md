# Project Umbra — refined playable design, v0.2.4

This replaces the earlier MMO and open-ended horde assumptions. The current target is a **single-player** 15-minute action roguelite. The supplied refined plan v2 is the reference; the decisions below describe the implemented slice, not future promises.

## Two progression tracks

Permanent Base Level preserves RO-style identity: each level grants 3 points in STR / AGI / VIT / INT / DEX / LUK. Allocation and unlimited free reset happen at camp. There is no Job XP and no Zeny. Amber buys five camp-supply ranks (+5 HP each; 60/120/180/240/300 amber).

Run Level resets each expedition and drives boon drafts, starting with a free opening choice. Separating it from Base Level keeps draft cadence the same for veterans and new characters. Run XP thresholds are 45 + 25 × (run level − 1). Regular enemies drop amber yielding 10 run XP / 5 permanent XP and 3 amber upon collection; elites drop amber yielding 30 / 15 and 9 amber. Killing monsters does not directly grant XP—XP is acquired exclusively by collecting amber shards. XP earned in a run and collected amber settle once on victory, defeat, timeout or voluntary return. Closing the application before settlement forfeits that run's unbanked rewards.

Base XP thresholds remain 60 + 35 × (level − 1) to preserve existing saves. Base Level caps at 99; each stat caps at 99. Invalid legacy allocations exceeding earned points are refunded rather than retained.

## Fifteen-minute rhythm

| Time | Behaviour |
|---|---|
| 0–5 min | Opening draft; gradual batches of two creatures |
| 5–10 min | Three-enemy batches; elites join the pool |
| 10–14 min | Higher density and a large champion |
| 14–15 min | Clear the field, restore 25% HP, move to the bounded central arena, fight the guardian |
| 15:00 | Defeat by timeout unless guardian already died |

Active ordinary enemies cap at 22. Spawns require walkable terrain and at least 8m clearance from the player. Spawn cadence falls from 3.1s toward 1.1s. Pool size stays 24. Common HP: 48 × (1 + map index × .5 + elapsed / 650). Elite base HP: 155 with the same scaling. Enemy bodies separate and slide around simple obstacles.

## Class and support identity

All advanced classes unlock at **Base Level 10 OR the first Amberfall clear**, resolving the two unlock rules in the reference plan. Class changes are free at camp.

| Class | Identity |
|---|---|
| Novice | Balanced spirit-arrow attacks |
| Archer | 20% faster attack cadence |
| Mage | 25% stronger hits plus 1% spell damage per INT above 1 |
| Swordsman | 40% stronger directional cleaves, 20% less incoming damage |

Choose Pierce / Split Shot / Ember as the starting rune. Rune replacements can appear during drafts; changing them freely mid-combat would make draft choices meaningless, so free selection is restricted to camp. Chain (40% bounce), Ignite (30% over three seconds, refresh only), Nova and Multishot work across all classes. Swordsman translates extra projectiles to +20% slash damage each and extra pierce to +0.4m reach. Sprout Card grants that pierce/reach; Mushroom Card grants 1 HP on kill.

Cards are run-draft rewards in this slice, not persistent monster-drop inventory. There is no socket-board editor yet. The common support mechanics are real; the collection UI is intentionally smaller than the aspirational PoE-style system.

## Balance guardrails

- Basic attack .36s; Nova 7s; dodge 1.8s; Mend 22s restoring 30% max HP.
- Damage: rune base 23 (Split Shot 12) + min(30, Base Level − 1), multiplied by 1 + .02 × (STR−1) + .015 × (DEX−1).
- AGI: +1.5% attack rate and +.4% movement per point above 1. VIT: +8 HP and +.08 HP/s. Fractional regeneration accumulates correctly.
- INT: −1.2% cooldown per point above 1, capped at 50%. DEX: +2% projectile speed. LUK: base 5% crit +.5% per point above 1, capped at 65%; crit multiplier 1.75.
- Quickdraw +15% rate, max 4; Stride +8% move, max 3; Twin Flight +1 projectile, max 2; Roots +20 HP/heal 30, max 4; Attraction +2m pickup, max 2. Unique supports/cards cannot be drafted twice.
- Rare/epic draft weight starts at .4 versus common 1, increasing by .01 per LUK above 1, capped at 1. One free reroll per expedition. Battle Focus (+8% run damage) remains as the non-empty fallback when capped upgrades are exhausted.
- .55s grace after a hit prevents stacked enemies or overlapping hazards from deleting all HP in one frame. Dodge grants .28s immunity. No automatic full heal on every level.

## Three routes on one reusable world

| Map | Entry | Identity | Guardian |
|---|---|---|---|
| Amberfall Grove | Open; recommended Lv. 1–10 | Golden woodland | Elder Bloom; 1,800 HP |
| Twilight Mire | Clear Amberfall; recommended Lv. 10–20 | Violet tint; enemies +40% speed; delayed poison pools on death | Venomous Chitin; 2,600 HP |
| Ashfall Ridge | Clear Mire; recommended Lv. 20–30 | Ember tint; death explosions after 1.5s | Infernal Warden; 3,400 HP |

Recommended levels are guidance, not extra locks. The three routes share geography and creature sprites, with material/post-process palettes, HP scaling and distinct hazards. Guardians fire radial projectiles and delayed attacks aimed at the player's last position; attack intervals tighten below half HP. Bosses use enlarged existing sprites. Victory adds 75/150/225 amber and 100/200/300 XP.

## Presentation and follow-up scope

Camp and results screens make objectives and banked rewards explicit. Pause, stat, guide, draft and build menus stop combat. Gold outlines telegraph future damage; red marks active hazards. Boss arena boundaries and timer backplates improve readability. Generated original attack/hurt/reward tones are optional, as is depth of field.

In v0.2.3, Level Up features a dedicated Ragnarok-inspired sequence:
- A translucent golden pillar of light (alpha 0.28, double-sided, 0.5s auto-fade) and warm point light cast over the player.
- Radial safety knockback pushing enemies within 3.0m back to 3.5m to eliminate cheap unpause hits.
- Synthesized 4-note ascending chime arpeggio (C5, E5, G5, C6) with 2nd harmonic bell overtones.
- Polished draft UI: 0.15s backdrop fade, staggered card entrance (0.05s intervals, EaseOutBack curve), hover lift (8px upward), and zero-lock quick-select hotkeys `[1]`, `[2]`, `[3]` and `[R]`.

In v0.2.4, Mobile Device Detection & Touch Input Experience:
- Automatic handheld device detection (`Application.isMobilePlatform`, `SystemInfo.deviceType`, first touch event) with manual runtime toggle (`INPUT: MOBILE TOUCH` / `DESKTOP`).
- Floating virtual analog joystick on the lower-left screen with dynamic thumb origin, clamped radius, deadzone, and procedural antialiased circular rendering.
- Ergonomic thumb-arc circular action buttons on the lower-right: Dodge (Space), Wind Nova (Q), Mend (E), and Auto-Aim toggle, with real-time radial cooldown sweeps and ready highlights.
- Multi-touch support: independent left-thumb analog movement and right-thumb skill execution without interference.
- Uncluttered mobile combat viewport: bottom desktop bar is hidden when mobile touch UI is active, freeing the center screen.
- Full card tap selection in boon drafts for seamless mobile drafting.

Next art/content pass: bespoke class sprites, richer music/SFX, additional enemy attack patterns, persistent card collection/socket UI, stagger, and more differentiated boss movement. These are not represented as implemented features. This release retains the existing procedural forest instead of claiming production Octopath-quality assets.
