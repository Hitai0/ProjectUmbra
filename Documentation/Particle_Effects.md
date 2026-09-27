# Spirit particle effects

Implemented 2026-09-27. The visual language uses small luminous diamond cores with feathered halos: antique gold for rewards/critical hits, sage for wind/healing, amber for Ember, ice blue for lightning, and restrained coral for player damage.

## Gameplay hooks

- Spirit arrows leave short, rune-colored wakes, sampled at most once every 0.04 simulation seconds per arrow.
- Sword attacks sweep particles across the slash arc; lightning throws sparks at its target.
- Hits and critical hits have different burst strengths; defeated enemies release fading golden motes.
- Quickstep emits an initial burst plus a short wake while dashing.
- Nova throws a radial burst; Mend raises a slower column of sage motes.
- Amber pickup emits a small rising glint; level-up emits a larger golden celebration.
- Sparse woodland fireflies spawn around the moving player instead of remaining at the original map center.

## Runtime and limits

`UmbraParticles.cs` creates three reusable ParticleSystems with one shared material: combat (640 particles), celebration (96), ambient (80). Regular bursts share a 96-particle emission budget per rendered frame; celebrations have their own bounded pool. Individual hits never allocate particle GameObjects or materials. The analytic `Resources/Umbra/SpiritParticles.shader` needs no texture and is retained for builds through a Resources reference.

Combat and ambient effects use scaled simulation time, following pause and game-speed settings. Only celebrations use unscaled time so their animation can finish while drafting. All pools clear at reward settlement, combat reset and teardown. Cosmetic randomness uses a separate System.Random and does not consume the gameplay RNG. Particle systems have fixed internal seeds.

## Verification

Unity compilation and the fresh Play Mode suite passed **338 checks**. Nine particle checks cover pool initialization, capacity, clock configuration, shader support, gameplay RNG isolation, per-frame burst limiting, shared hit budget, independent celebration capacity, and combat cleanup. Captured and inspected `Recordings/Particles-Skills.png` in-game: healing, nova, lightning impact and reward glints render without opaque particle quads. The level-up UI capture confirms the normal draft overlay; it obscures most of the world celebration.

The editor `particles-preview` command runs a save-suppressed live preview and captures skill/draft frames. No WebGL rebuild, mobile performance profiling or full 15-minute stress test was performed; the capacity limit is a bound, not a measured frame-rate guarantee.
