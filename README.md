# Project Umbra

**Amberfall Grove** — a playable Unity prototype exploring pixel characters in a warm 3D forest, with action combat and interchangeable support runes.

Unity **6000.3.24f1** · C# · Universal Render Pipeline · New Input System

## Play

1. Open this repository in Unity Hub using the version above.
2. Allow package import and script compilation to finish.
3. Open **Assets/Umbra/Scenes/Amberfall.unity**, or use **Umbra → Open Amberfall**.
4. Press **Play**. Double-click the Game tab to maximize the view.

The scene is intentionally a small bootstrap: its environment and actors are generated when Play starts. The original HDRP template scene and settings are preserved, but the prototype and active quality settings use URP. The original scene may need its original pipeline restored if used independently.

## Controls

| Input | Action |
|---|---|
| WASD / arrows | Move |
| Right click | Travel toward a ground point (simple steering, no pathfinding) |
| Hold left mouse | Aim and fire |
| Q | Wind Nova: radial arrows |
| Space | Quickstep with brief invulnerability |
| E | Mend health |
| R or 1 / 2 / 3 | Switch support rune |
| Tab | Rune collection |
| H | Controls |
| Escape | Pause |
| Mouse wheel | Zoom |

## Playable loop

Defeat grove creatures, dodge the red attack wind-up, collect amber, level up, and restore the grove by defeating six creatures. Enemies respawn after 18 seconds. The elder has more health and deals more damage. Player defeat returns you to the trail without losing shards.

Three freely available runes change the same arrow skill:

- **Piercing Wind**: an arrow can hit three different enemies.
- **Split Shot**: three lower-damage arrows in a fan.
- **Ember Seed**: splash damage on impact.

Only **amber shards and selected rune** are saved locally with PlayerPrefs. Level, combat, enemies, and quest progress reset each session. Browser storage is device/origin-specific and can be cleared by the browser. This is not secure MMO persistence.

## Web build

Install the **Web Build Support** module for this Unity version. Stop Play Mode and choose **Umbra → Build Web prototype**. Output: `Builds/Web` (ignored by Git).

For local testing, run `python Tools/serve_web.py` from the repository and visit **http://127.0.0.1:8080**. Do not open the generated HTML via `file://`.

The local server presents a full-window branded launcher from `Tools/web_shell.html`; click **Enter the Grove** to load the game. Unity's generated `Builds/Web/index.html` remains available for ordinary static deployment. The launcher assumes the uncompressed filenames produced by this project's build configuration.

The prototype uses uncompressed Web build files for uncomplicated local serving. Before hosting at scale, configure compression headers, inspect file-size limits, and test target browsers. A Web build is not automatically a multiplayer game server.

## Validation

In Play Mode use **Umbra → Run gameplay smoke tests**. The check covers movement, river collision, rune definitions, enemy damage/death, XP/drop creation, pickup, healing, cooldown enforcement, and player respawn. It temporarily changes the live game; restart Play afterward. The result is written to the ignored `.umbra-test` file and the Console.

## Scope and next steps

This is an **offline visual/gameplay prototype**, not a running MMO. It contains no login, remote server, other players, trading, or anti-cheat. The visual style uses original placeholder pixel sprites and procedural environment art; it does not yet reproduce the detail of the reference concept image.

Next milestone: a two-player authoritative server slice (movement, combat, loot ownership, reconnect), then zone transfer and database persistence. Web clients require a browser-compatible transport such as WSS.

See [Architecture](Documentation/Architecture.md) and [Validation](Documentation/Validation.md).

## Assets

Prototype sprites and ground texture were generated specifically for this project. No Ragnarok, Path of Exile, or Octopath assets are included. Unity's existing template content remains under its respective terms. See `Tools/generate_art.py` to regenerate the original art (requires Pillow).
