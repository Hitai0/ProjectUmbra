# Class art and targeted lightning — 2026-09-26

Each class now has its own idle and two walking frames. Archer keeps the original ranger art; Novice, Mage and Swordsman use new transparent sprite sheets. Camp class selection updates the sprite immediately. Runtime slicing trims transparent vertical padding and normalizes the new characters to 1.85 world units tall.

Mage basic attacks call lightning from above instead of firing arrows. Each bolt damages exactly one living target within 12m; there is no impact-radius damage. Auto aim samples distinct nearby enemies; manual aim prioritizes enemies nearest the cursor. A cast never strikes the same enemy twice. No valid victim means no cooldown is consumed.

Initial tuning: 1.1-second cooldown, affected by attack speed, and 2.5 times the existing class attack damage before critical hits and lightning supports. This is provisional tuning, not a claim of completed balance playtesting. Q/Wind Nova remains the existing separate skill.

Mage basic-attack support mappings:

- Pierce: one bolt and +20% damage.
- Split Shot: up to three distinct victims using the existing lower Split Shot base damage.
- Ember: burns the struck target; no explosion.
- Twin Flight: one additional distinct victim per rank.
- Sprout Card: +20% damage instead of piercing.
- Branching Spirit: +40% damage instead of bouncing.
- Kindling: existing targeted burn; Mushroom Card: existing kill healing.

Damage bonuses from Pierce, Sprout and Branching Spirit add together. English and Thai draft/rune descriptions explain these Mage mappings. Other classes retain their original behavior.

## Art provenance

Generated with the built-in imagegen tool, preserved as transparent PNGs in `Assets/Umbra/Resources/Umbra/`. Archer assets are unchanged. No external game artwork was copied.

Prompt set for Novice and Mage:

> Use case: stylized-concept. Asset type: production pixel-art RPG character sprite sheet PNG with genuine transparent alpha background. Original fantasy chibi adventurer matching simple 48-pixel-tall retro sprites: crisp square pixels, dark olive outlines, restrained earthy palette, readable small silhouette, three-quarter front view facing slightly right. Exactly three equal-width cells in ONE horizontal row: idle, walk left foot forward, walk right foot forward. Same character, size and equipment in each cell, feet aligned at same baseline, entire body and equipment within each cell, generous transparent padding. No text, no grid, no ground, no shadow, no scenery.

Novice suffix:

> Character: novice adventurer, short brown hair, cream tunic, brown vest, small red scarf, leather boots, short wooden training wand; no hat, no bow.

Mage suffix:

> Character: lightning mage, oversized pointed deep violet wizard hat, indigo robe with pale gold trim, brown boots, wooden staff topped by cyan crystal; no bow or sword.

Swordsman final prompt:

> Use case: stylized-concept. Production pixel art fantasy RPG swordsman sprite sheet, genuinely transparent PNG background. Three equal-width cells in one horizontal row: idle, left foot forward walking, right foot forward walking. Identical chibi character and equipment, facing three-quarter right. Short auburn hair, silver steel chestplate and shoulder armor over burgundy tunic, dark boots, steel sword and small round shield. Crisp square pixel clusters, dark outlines, earthy palette, cute oversized head, matching retro chibi RPG adventurers. Entire body and gear fully inside each cell, feet aligned, generous transparent padding. No letters, labels, ground, shadows, scenery or grid.
