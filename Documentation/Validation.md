# Validation — v0.2.4

Validated 2026-09-25 on Windows with Unity 6000.3.24f1, URP and the new Input System.

## Runtime integration

141 assertions passed: 41 progression/combat/lifecycle checks plus 100 safe-spawn samples. Coverage includes camp allocation and respec; mobile state initialization and toggle switching; locked in-run stats; opening draft and menu pause; rejecting repeated draft claims; separate permanent/run XP; level up safety radial knockback (enemies within 3.0m cleanly pushed to 3.5m); capped draft fallback; damage, death and pickup (XP granted exclusively on amber pickup, not on kill); heal/nova cooldowns; hit grace; reward settlement exactly once; all three guardian timings and victories; arena boundaries; 15-minute timeout; supply purchases; all four class attacks; crit/CDR caps. Test save writes are suppressed and the original local profile is restored.

Visual, touch, and audio inspection verified:
- Mobile device auto-detection (`Application.isMobilePlatform`, `SystemInfo.deviceType`, touch event detection) with real-time toggle between Touch and Desktop modes.
- Floating virtual analog joystick on the lower-left screen with dynamic thumb origin, clamped radius, deadzone, and procedural antialiased circular rendering.
- Ergonomic thumb-arc circular action buttons on the lower-right: Dodge (Space), Wind Nova (Q), Mend (E), and Auto-Aim toggle, with real-time radial cooldown sweeps and ready highlights.
- Multi-touch responsiveness: independent left-thumb analog movement and right-thumb skill execution without interference.
- Uncluttered mobile combat viewport: bottom desktop bar is hidden when mobile touch UI is active, freeing the center screen.
- Full card tap selection in boon drafts for seamless mobile drafting.
- Ragnarok-style golden pillar of light (alpha 0.28, double-sided unlit, 0.5s fade-out) and warm point light on level up.
- Synthesized 4-note ascending chime arpeggio (C5, E5, G5, C6) with 2nd harmonic bell overtones.
- Fluid draft screen: 0.15s backdrop fade, staggered card entrance (0.05s interval, EaseOutBack curve), hover lift (8px upward), and instant quick-select hotkeys `[1]`, `[2]`, `[3]` and `[R]`.

## Web build and browser

Unity reported Succeeded: 73,019,728 bytes, build time 5m16s. Runtime version and loader identify v0.2.0.

In the Codex Chromium browser on localhost: loaded the WebGL build, entered camp, began a run, selected several boons, observed automatic attacks and run-level progression, used right-click travel, paused, voluntarily ended the expedition, and verified the results screen banked 60 permanent XP from 12 kills. Reloading preserved Base Level 2 and 3 available stat points. Source whitespace validation passed.

## Scope limits

This is a prototype balance pass. Automated guardian/timeout checks advance the clock and invoke real game methods; they are not a claim of a complete 15-minute manual playthrough on every route. Mobile, Safari, low-end hardware, long-session performance and broad player balance testing remain unvalidated. Class sprites/world geometry are reused; cards are draft effects rather than a persistent socket inventory.

## Responsive HUD correction — 2026-09-25

Unity compilation and runtime smoke suite passed (161 assertions). Added 20 layout assertions across 1600x900, 2560x1080, 1024x768, 390x844 and 844x390: fixed edge margins, centered modals, desktop hit areas and mobile coordinate conversion. HUD groups now anchor to the actual viewport; modal backdrops cover it fully. Mobile amber no longer overlaps the XP bar. Draft hover uses the IMGUI-local pointer without applying the matrix inverse twice.

These checks validate geometry and input conversion, not visual or real-device touch testing. Menus retain their proportional reference layout (no portrait column reflow). Existing WebGL build output has not been rebuilt for this correction.

## Horde balance pass - 2026-09-25

Unity compilation and a fresh Play Mode smoke run passed (181 assertions). Added coverage: opening batches/cap, elite gate and admission at 45s, reserved champion at 120s, the 14-common-pickup first run level, actual common contact damage, and all guardians' HP, body/aimed telegraphs, delayed volley and enrage cadence. Existing progression, settlement, hit grace and HUD checks still pass.

These checks validate mechanics, not final difficulty. Full-run playtests, guardian time-to-kill, actual draft cadence, permanent-economy impact and performance at 64 enemies remain unmeasured. WebGL was not rebuilt. See GDD.md for reference direction and tuning.

## Open meadow pass - 2026-09-25

Unity compilation and fresh Play Mode smoke tests passed (188 assertions). New checks cover the former river/shrine positions, a traversal grid across the field, all four boundaries, safe spawns at all four corners, and reward-free recycling of distant enemies. Inspected the generated central-field camera capture: the interior is open with low ground decoration and no river, gate or canopy obstruction. This capture does not verify the HUD or perimeter camera views. WebGL was not rebuilt; full-run travel and device performance still need playtesting.

## Camera-aware wave director - 2026-09-25

Unity compilation and a fresh Play Mode suite passed (201 assertions). New coverage includes offscreen placement with player clearance at four aspect ratios and both zoom limits (24/46 degrees), camera-aligned corner spawning, elite budget, live damage/speed stability across wave boundaries, pressure-wave population bounds, relief preserving living actors, and exact wave schedule transitions. Existing guardian, reward settlement, map and HUD tests still pass. Source whitespace checks passed.

The wave values are authored Umbra tuning, not verified Vampire Survivors internals. No full-run playtest, density/frame-time profiling or WebGL rebuild was performed. See Spawn_Design.md for primary references, rationale, wave rows and tradeoffs.

## Pointer layers and modal ownership - 2026-09-25

Unity compilation and fresh Play Mode smoke suite passed (217 assertions). Added coverage validates configured layer names, actual floor hits, nearest solid/default-layer blockers, Ignore Raycast/UI/trigger exclusions, rejection of missing or out-of-bounds targets, and shared overlay priority/Escape behavior. Existing map, spawn, combat and HUD tests pass. Source whitespace checks passed.

Real mouse/touch click-through interaction and visual overlay behavior were not manually exercised; the tests validate physics picking and modal state transitions. WebGL was not rebuilt.

## Game speed - 2026-09-25

Unity compilation and smoke suite passed (235 assertions). Added checks cover each speed, draft/stats/pause overrides, changing speed while paused, and restoring the selected speed on resume. Save writes remain suppressed in tests. UI placement and saved preference reload were not manually exercised. No WebGL rebuild or full-run speed/performance test was performed.

## Monster attack & damage pipeline pass - 2026-09-25

Unity compilation and fresh Play Mode smoke suite passed (264 assertions, +29 from previous 235).

Added test coverage:
1. **Pure formulas**: Verified `DamageRules.Incoming(raw, armor, reduction)` against baseline tests (10, 0, 0 => 10; 10, 3, 0 => 7; 10, 3, .20 => 6; 3, 5, .20 => 1; 0 => 0; negative => 0; negative armor/reduction clamped safely).
2. **Contact geometry**: Exact circle-circle overlap on boundary, within boundary, and strictly outside boundary; verified elevation invariance (XZ plane only).
3. **Protection deadlines**: Verified 3 independent protection deadlines (`hitGraceUntil`, `dashImmuneUntil`, `spawnImmuneUntil`); verified that dashing during spawn immunity extends dash timer via `Mathf.Max` without truncating spawn protection.
4. **Modal & phase damage rejection**: Verified incoming hits are rejected outside `RunPhase.Running` and during active modals (`Drafting`, `Paused`, `RunePanel`, `HelpPanel`, `StatsPanel`).
5. **Frame damage queue & single-hit resolution**: Verified 21 candidate damage requests (20 common hits + 1 boss hit) are collected without frame-time health pileup, and resolved into exactly one strongest mitigated hit (`applied = 40`), leaving the player at 60 HP.
6. **Deterministic winner**: Verified candidate shuffle produces identical results tiebroken by monotonic `SourceId`.
7. **Armor boon (Iron Bark)**: Verified rank cap 5, `PlayerArmor` reading directly from `Rank(IronBark)`, and incoming mitigation reductions.
8. **Swept segment collision**: Verified fast hostile projectile swept segment detection piercing through player in 1 frame, and verified distant non-intersecting trajectory segments do not register false hits.
9. **Backward compatibility**: All legacy assertions (progression gates, camp allocation, safe spawns, opening horde batch, 2-minute champion, amber-only XP, guardian boss schedules, class attacks, stat caps) continue to pass.

Scope limits:
- WebGL was not rebuilt.
- Swarm pressure balance and tactile feel at 2x simulation speed need manual playtesting.

## Game speed expansion (3x, 4x) - 2026-09-25

Unity compilation and smoke suite passed (276 assertions, +12 from previous 264). Added speeds 3x and 4x to `AvailableGameSpeeds` (`1x, 1.5x, 2x, 3x, 4x`). Verified simulation scaling, draft/modal/pause overrides, cycling while paused, and resume restoration across all 5 speed settings.

## Dual-Language Localization (English & Thai) - 2026-09-25

Unity compilation and Play Mode smoke suite passed (280 assertions, +4 new checks).
- **Automated Validation**:
  - `language toggle switches to thai`: Verifies `ToggleLanguage()` alters state to `GameLanguage.Thai` and updates `Loc.Current`.
  - `thai localization mappings`: Validates sample Thai dictionary returns (`Novice -> โนวิซ`, `Pierce -> วายุทะลวง`, `Map 0 -> ป่าแอมเบอร์ฟอล`).
  - `language toggle restores english`: Verifies cycling back restores `GameLanguage.English` and `Loc.Current`.
  - `english localization mappings`: Validates exact English identity preservation (`Novice -> NOVICE`, `Pierce -> PIERCING WIND`, `Map 0 -> AMBERFALL GROVE`).
- **Typography & Font**: Dynamic Windows Tahoma font imported into `Assets/Umbra/Resources/Umbra/Fonts/tahoma.ttf` with runtime fallback to ensure no broken glyphs or tofu when rendering Thai tone marks and consonants in IMGUI.
- **Coverage Verified**:
  - Camp header, class picking, map selection, stats allocation, supplies, and expedition results.
  - In-game combat HUD: Wanderer status bar, minimap region & objectives, desktop & mobile skill buttons, auto-aim toggle, and notifications.
  - Modals: Pause menu (with new Language switch button), Draft boon selection (cards, categories, rarities, titles, descriptions), Support Runes overview, Character Status panel (with 6 attribute formulas), and Help guide.
  - Player preference persistence in `PlayerPrefs` (`"Umbra.Language"`).


## Class sprites and targeted Mage lightning — 2026-09-26

Fresh Play Mode compilation and smoke suite passed **313 checks** (26 additional checks). Coverage includes idle/walk sprite selection for all four classes, all three Mage runes, adjacent enemies receiving no splash damage, distinct victims, range, no arrow projectiles, cooldown, targeted Ember burning, no repeated victim with extra bolts, no-target cooldown preservation, paused casts, Chain remaining single-target, and effect cleanup. Original progression, combat and settlement checks passed.

Inspected generated sheets for transparency and all four in-engine camera captures (`Recordings/Class-*.png`), including the Mage bolt landing on its target. Captures are synchronous within the smoke suite, so effects pending end-of-frame destruction from earlier guardian tests can appear in the background. Two earlier runs in the existing Play Mode session stopped at the pre-existing opening-horde-batch assertion; a fresh stopped/refreshed Play Mode run passed the complete suite. No claim of full-run balance validation or manual input playtesting. WebGL was not rebuilt or deployed.
