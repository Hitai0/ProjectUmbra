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
