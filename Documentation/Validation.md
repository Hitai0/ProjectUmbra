# Validation — v0.2.2

Validated 2026-09-24 on Windows with Unity 6000.3.24f1, URP and the new Input System.

## Runtime integration

138 assertions passed: 38 progression/combat/lifecycle checks plus 100 safe-spawn samples. Coverage includes camp allocation and respec; locked in-run stats; opening draft and menu pause; rejecting repeated draft claims; separate permanent/run XP; capped draft fallback; damage, death and pickup (XP granted exclusively on amber pickup, not on kill); heal/nova cooldowns; hit grace; reward settlement exactly once; all three guardian timings and victories; arena boundaries; 15-minute timeout; supply purchases; all four class attacks; crit/CDR caps. Test save writes are suppressed and the original local profile is restored.

Unity visual inspection covered camp, opening draft, ordinary combat and the guardian arena with projectile/ground attacks. A vertex-colour rendering issue discovered during inspection was corrected for telegraph rings.

## Web build and browser

Unity reported Succeeded: 73,019,728 bytes, build time 5m16s. Runtime version and loader identify v0.2.0.

In the Codex Chromium browser on localhost: loaded the WebGL build, entered camp, began a run, selected several boons, observed automatic attacks and run-level progression, used right-click travel, paused, voluntarily ended the expedition, and verified the results screen banked 60 permanent XP from 12 kills. Reloading preserved Base Level 2 and 3 available stat points. Source whitespace validation passed.

## Scope limits

This is a prototype balance pass. Automated guardian/timeout checks advance the clock and invoke real game methods; they are not a claim of a complete 15-minute manual playthrough on every route. Mobile, Safari, low-end hardware, long-session performance and broad player balance testing remain unvalidated. Class sprites/world geometry are reused; cards are draft effects rather than a persistent socket inventory.
