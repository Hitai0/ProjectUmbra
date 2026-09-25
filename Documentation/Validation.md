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
