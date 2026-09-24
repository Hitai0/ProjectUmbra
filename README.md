# Project Umbra

**v0.2.0 — The Wayfarer's Camp**. A single-player action roguelite mixing Ragnarok-style permanent stats, shared skill supports, and 15-minute expeditions. Unity **6000.3.24f1**, C#, URP, New Input System.

Play online: https://project-umbra-rho.vercel.app

## Play in Unity

Open `Assets/Umbra/Scenes/Amberfall.unity` (or **Umbra → Open Amberfall**) and press Play. The bootstrap scene builds the environment at runtime. Enter camp, choose your starting support, allocate available stat points, and select **Begin Expedition**. Choose one of the opening boons to start the timer.

## Controls

| Input | Action |
|---|---|
| WASD / arrows | Move |
| Right click | Walk toward a point; direct steering, no pathfinding |
| Auto-aim / hold left mouse | Automatic attacks / manually aimed attacks |
| Q | Wind Nova |
| Space | Quickstep with brief invulnerability |
| E | Mend 30% max HP |
| Tab | Inspect build; select starting rune at camp |
| C | Character stats; allocate and freely reset at camp |
| H | Guide |
| Escape | Close a panel or pause |
| Mouse wheel | Zoom |

Menus pause combat. Losing focus pauses a run. Sound and soft focus can be toggled at camp or in the pause menu.

## Expedition loop

- Camp: free stat respec, class switching, three starting runes, and permanent supply upgrades purchased with amber.
- A temporary run level offers capped, weighted boon drafts. Permanent Base Level grants 3 stat points per level after settlement. These progress independently.
- At 5 minutes, enemy pressure increases. At 10 minutes a champion appears. At 14 minutes common enemies clear and a guardian locks the central arena. Defeat it before 15:00.
- Death, timeout, or voluntary return ends the run. Earned XP and collected amber are banked once; victory adds a bounty. Uncollected ground shards are not banked. Closing/reloading the page mid-run abandons unsettled rewards.
- Clear Amberfall to open Twilight Mire, then clear the Mire to open Ashfall Ridge. Classes unlock at Base Level 10 **or** the first guardian victory.
- Novice, Archer, Mage and Swordsman share supports. Character art is reused in this prototype; attacks, modifiers and tint distinguish the classes.

Saves are local to the browser/device and origin (Unity Editor has a separate save). Existing v0.1.4 XP, amber and legal stat allocations migrate automatically. No account or server is required.

## Build and validate

- **Umbra → Run gameplay smoke tests (Play Mode)**: tests the real runtime with persistence writes disabled and restores the original profile afterward.
- Stop Play Mode, then **Umbra → Build Web prototype**. Output: `Builds/Web`. The custom loading page is copied from `Tools/web_shell.html`.
- `python Tools/serve_web.py`, then open http://127.0.0.1:8080. Do not open the generated HTML with `file://`.
- Deploy the contents of `Builds/Web` as a static site. The existing Vercel project link is local and is not committed. Preserve the production origin to preserve browser saves.

See [design and balance](Documentation/GDD.md), [architecture and limits](Documentation/Architecture.md), and [validation](Documentation/Validation.md).
