# Validation

Environment: Windows, Unity 6000.3.24f1, URP 17.3.0, new Input System.

## Observed in the Unity Editor

- C# compiles and Amberfall enters Play Mode.
- Camera, environment, pixel characters, UI, enemy movement and attack telegraphs render.
- Gameplay smoke test passed: movement, river collision, three rune definitions, enemy damage, death/XP/drop, pickup/persistence, healing, cooldown enforcement, and player respawn.
- Source whitespace check passed for C#, shaders, Python tools and Markdown. Unity-generated YAML/meta files retain Unity's own formatting.

Web build and browser verification are being performed separately; Editor success alone does not establish browser compatibility.

## Not yet validated

- Multiplayer, server authority, load/capacity and backend persistence (not implemented).
- Mobile/touch, Safari and low-end hardware.
- Production visual fidelity, long-session performance and exhaustive game balance.
