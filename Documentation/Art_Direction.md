# Project Umbra art direction

Implemented 2026-09-27 from the user's supplied art-direction board: hopeful HD-2D adventure, a warm camp, muted woodland greens, antique gold and parchment, with blue-grey atmospheric depth. This is an art pass on the existing open-meadow roguelite, not a conversion to the board's illustrated village or turn-based combat.

## In-game application

- Camp and results use `Assets/Umbra/Resources/Umbra/camp_vista.png`, cropped to fill the screen behind readable dark panels.
- Shared HUD panels use fine double gold borders, corner ornaments, warm ivory text and sage accents. Buttons have framed selection and hover states; existing Thai typography and hit areas remain.
- The battlefield uses `ground_meadow.png`, repeated every 10 world metres, with mipmaps and bilinear filtering to reduce distant shimmer. Small grass geometry is reduced from 16,000 to 4,500 samples.
- Perimeter trees use `woodland_oak.png` billboards instead of faceted clusters. Trunk blockers stay outside the playable meadow. Route tints also apply to their shared material.
- `sprout_hd.png` and `mushroom_hd.png` replace the original low-resolution enemy art. Visible alpha bounds determine height, preserving the previous 34/24 and 36/24 world-unit sizes and existing elite/boss multipliers. Combat radii are unchanged.
- Cooler ambient shadows, softer warm sunlight, restrained saturation/bloom/grain and gentler depth of field unify the sprites and world.
- Existing four class sheets remain in use. Original replaced textures remain available in the project.

## Asset generation

All five new PNGs were generated with the built-in imagegen tool and copied into `Assets/Umbra/Resources/Umbra/`. Transparent output was requested for the oak and both creatures; the landscape and terrain are opaque. No external image API or Python image editing was used.

### camp_vista.png prompt

Create a production game background, landscape 16:9, for Project Umbra The Wayfarer's Camp. HD-2D intricate pixel art, miniature diorama depth, warm hopeful fantasy. View from a forest campsite on a rocky overlook: canvas tent, lantern, small amber campfire and travel supplies in lower left; lush muted sage and deep pine trees frame the sides; broad misty blue valley, winding river, distant medieval castle in upper right, peach clouds and golden evening sunlight. Rich deliberate pixel clusters and detailed painted pixel textures, not smooth vector, not low-poly. Palette antique gold, parchment ivory, moss green, umber, slate blue; restrained saturation and luminous highlights. Composition must function behind a game menu: broad quiet central valley, detailed scenery at the margins. No text, no logo, no borders, no UI, no characters. Reference mood: classic storybook adventure with Octopath Traveler inspired lighting. Save as a usable background asset.

### ground_meadow.png prompt

Production tileable seamless square ground albedo texture for HD2D fantasy RPG. Perfect overhead orthographic flat forest meadow floor only, fills the entire square edge to edge, seamless all four edges. Fine deliberate pixel-art clusters, moss and short sage olive grass interwoven with weathered warm grey earth and small embedded slate pebbles, sparse tiny ivory wildflowers and dry ochre leaves. Natural irregular patches, subdued woodland palette, readable quiet ground for combat characters over it. Even diffuse light, medium value, no cast shadows, no vignette, no perspective, no large objects, no trees, no text, no border, no grid. Painterly pixel texture reminiscent of detailed classic JRPG environments. Keep high contrast and visual noise restrained.

### woodland_oak.png prompt

Single isolated mature woodland oak tree game sprite, transparent background. Entire tree from roots to crown centered with margin, no cropping. HD-2D fantasy RPG detailed pixel art with intentional visible pixel clusters, three-quarter elevated camera view looking slightly down, matching a storybook JRPG forest. Wide irregular layered canopy, muted deep pine and sage greens, restrained golden sunlit leaf tips, gnarled warm umber trunk, exposed roots and tiny moss at base. Light from upper left. Rich miniature diorama quality, textured pixel edges, no flat polygon facets, no smooth vector look. No ground plane, no sky, no text, no cast shadow beyond roots, no additional objects.

### mushroom_hd.png prompt

One single forest mushroom enemy sprite for Project Umbra HD-2D fantasy RPG. Transparent background, entire creature centered with plenty of margin, no cropping. Charming but mischievous squat mushroom creature, large rust red and ochre cap with ivory flecks, cream stalk body, two small dark expressive eyes, tiny root feet and stubby arms, moss at feet. Three-quarter front view, slightly elevated game camera. Intricate crisp pixel-art clusters, dark umber outlines, muted forest palette, warm upper-left light, dimensional miniature diorama shading, style consistent with classic premium HD-2D JRPG character sprites. Not smooth vector, not emoji, not 3D render. No ground plane, no text, no UI, no separate objects, no other poses.

### sprout_hd.png prompt

One single tiny woodland sprout monster game sprite for Project Umbra HD-2D fantasy RPG, transparent background, whole creature centered with margin. Round pear-shaped moss-green seedling creature with two broad sage leaves growing from its head, little branch arms and root feet, amber eyes and curious mischievous expression. Three-quarter front slightly elevated game view. Intricate crisp pixel art clusters, dark umber outlines, muted sage forest greens, warm upper-left sunlight, dimensional miniature shading. Consistent with premium classic JRPG HD-2D character sprites, charming forest enemy, not emoji, not flat vector, not a smooth 3D render. No ground plane, no extra objects, no text, no UI, no other poses.
