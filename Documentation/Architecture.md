# Architecture and boundaries — v0.2.0

The Unity bootstrap generates the existing forest and runs a local single-player simulation. No game server or network economy is required.

| File | Responsibility |
|---|---|
| CombatRules.cs | Shared formulas, thresholds, rune and boon definitions; uses Unity math |
| UmbraPrototype.cs | World generation, actors, inputs, movement, attacks, loot and profile keys |
| UmbraCampaign.cs | Camp/run/results lifecycle, settlement, class/map progression, weighted drafts, guardians, hazards and short generated audio cues |
| UmbraHud.cs / UmbraCampHud.cs | Scaled IMGUI combat UI, camp, draft, character sheet and results |
| UmbraCampaignTests.cs | Runtime integration checks with save writes suppressed; editor-only guardian preview |
| UmbraProjectSetup.cs | Editor actions, build configuration, smoke-test scheduling and Web shell copy |
| ForestVertex.shader | Vertex-colour grass and telegraph rings |

The runtime is split across partial classes to retain the existing scene/component identity and art generation. A later UI Toolkit migration or ScriptableObject content pipeline can separate content authoring without a save reset.

## Persistence

Existing `Umbra.*` PlayerPrefs keys remain compatible. New keys record cleared-map bits, class, supply rank, mute/focus preferences and save version 2. Loading clamps invalid ranges and refunds allocations above earned stat points. Run-only boons and rewards are not saved as permanent equipment. Changing settings during a run saves the camp rune, not a temporary draft replacement.

Settlement transitions Running → Results before awarding anything, so repeated end-run calls cannot duplicate rewards. A reload during an active expedition loses unsettled rewards; no resumable mid-run checkpoint is implemented. Saves are local to the device/browser origin, separate from Editor saves. Changing production URL would create a new save namespace.

## Rendering and UX

Perspective 3D camera (34° FoV), pixel sprite actors, shared materials, static environment batching, directional light, SSAO and optional bokeh depth of field. Soft focus and audio can be toggled. The original HDRP template is retained for compatibility; this prototype uses URP.

Menus pause simulation and the expedition clock. Focus loss does not pause, supporting background/idle play. `Application.runInBackground = true` is enabled. The command-file bridge supports a fixed list of editor operations and is ignored by Git; it never executes arbitrary shell input. Smoke tests never write player saves.

## Known limits

- Character and guardian art is reused; classes have different mechanics but not full bespoke animations.
- Three route variants share world geometry. Cards are draft effects, not a persistent inventory/socket-board feature.
- Right-click travel uses direct steering. Enemies use collision avoidance, not navigation meshes.
- Immediate-mode UI and generated tones are prototype systems. No music score yet.
- No multiplayer, account/cloud save, mid-run resume, or mobile/touch UI.
- Test coverage validates transitions, formulas and actual combat methods; it does not establish long-term balance or low-end-device performance.

## Static deployment

`Builds/Web` contains the Unity WebGL output and branded loader. Host as static files with correct WASM MIME handling. `Tools/serve_web.py` provides localhost testing. Vercel project metadata and credentials stay outside source control. Build output is uploaded as a release artifact, not committed to Git.

## Pointer layers and UI priority

The runtime UI is IMGUI, not Canvas UI: sorting layers do not determine its input priority. `UmbraHud` uses GUI depth -100, draws the base screen with controls disabled while a modal is open, and draws only the highest-priority modal. The shared priority is **Pause > Draft > Stats > Help > Runes > Camp/Results/combat HUD > world**. Escape follows that same priority; drafting cannot be dismissed. Modal backdrops consume pointer events. A mouse press begun over UI remains captured until both mouse buttons are released, and mobile movement is cleared while a modal is open.

| Physics layer | Purpose |
|---|---|
| 8 / UmbraGround | Retained floor collider; only accepted world-click target, additionally checked against playable bounds |
| 9 / UmbraPointerBlocker | Retained tree-trunk colliders; nearest hit blocks clicks through to ground |
| 0 / Default and other included solid layers | Block clicks when their collider is the nearest hit |
| 2 / Ignore Raycast | Generated decorative primitives/effects; excluded from pointer raycasts |
| 5 / UI | Excluded from physics picking; existing IMGUI hit testing owns UI input |

`pointerMask` is serialized on UmbraPrototype and excludes UI and Ignore Raycast by default. Trigger colliders are ignored. `TryWorldPoint` uses the nearest Physics.Raycast result; it has no infinite-plane or invented-target fallback. These changes affect mouse picking, not projectile collision or world navigation. Additional UI implementations must participate in pointer blocking explicitly.
