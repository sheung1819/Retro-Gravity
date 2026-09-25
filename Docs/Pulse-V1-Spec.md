# Pulse — V1 Spec

## Concept
A retro arcade survival game. The player controls a catcher that slides along
the current "floor" edge of the screen, catching falling objects while
avoiding hazards. At random intervals, gravity flips — the floor relocates to
a different edge, and the player must immediately reorient.

## Platform / Tooling
- Unity (C#)
- Targets: iOS and Android (portrait, phones and tablets)

## Core Loop
1. Catcher sits on the current floor edge (bottom, top, left, or right).
2. Player swipes/drags to move the catcher along that edge.
3. Objects fall from the opposite edge:
   - Good objects → catch for points.
   - Bad objects → avoid; contact ends the run (or costs a life — TBD).
4. At a random interval, gravity flips:
   - A new floor edge is chosen (different from the current one).
   - The catcher smoothly transitions to the new floor position.
   - Swipe-to-movement mapping updates automatically to match the new axis.
   - Fall direction for spawned objects updates to match.
5. Difficulty ramps over time: fall speed and spawn frequency increase.
6. Win/lose condition: endless survival — score = time survived (or objects
   caught, TBD which feels better in testing).

## Visual Style
Retro vector/wireframe glow — flat shapes, glowing outlines/particle trails,
no photographic textures. Dark background. Reference concept mockup:
glowing catcher shape, simple geometric falling objects, particle trail on
motion, monospace HUD text for score.

## Audio
Chiptune loop for background music. Distinct SFX for: catch (good), miss/hit
(bad), and gravity flip (a "whoosh" or similar transition sound — this
matters a lot for making the flip moment read clearly to the player).

## Build Status

### Done (V1 implemented; see README for how to run and build)
- **Catcher + gravity flip** (`Gameplay/CatcherController.cs`). The original
  controller, extended with:
  - a flip telegraph: the upcoming floor edge pulses amber about 1s before the
    flip, with a two-tick warning sound,
  - catcher rotation during the flip so the cup always faces into the playfield,
  - safe-area-aware bounds (notches, home indicator),
  - flip interval driven by the difficulty ramp,
  - events for flip start and end (`GameEvents`).
- **Falling object spawner** (`Gameplay/ObjectSpawner.cs`, `FallingObject.cs`)
  - Good = green diamond, bad = red/magenta star. They differ by shape as well
    as colour, so colour-blind players can tell them apart.
  - Objects spawn just off the edge opposite the floor, at positions the
    catcher can reach.
  - Crossing time is the same for every floor, so the short sideways axis is as
    fair as the long vertical one.
  - On a flip, objects already in the air curve onto the new gravity.
  - Objects are pooled.
- **Collision / scoring / lives** (`Core/GameManager.cs`)
- **Gravity-flip transition** (`Visual/FlipEffects.cs`, `CameraRig.cs`): screen
  flash, damped camera roll, shake, spark bursts, floor-line relocation, and a
  background grid that scrolls in the direction of gravity.
- **Vector-glow style**: one additive shader, a procedural glow-line texture,
  and particle sparks. Palette in `GlowKit`.
- **Audio** (`Audio/ChiptuneSynth.cs`): an 8-bar A-minor chiptune loop at
  150 BPM, plus catch (pitch rises with combo), hit, miss, flip whoosh,
  warning tick, start and game-over sounds. All synthesized at startup.
  Authored clips can override any of them.
- **Menu / UI**: title, HUD (score, hearts, combo multiplier), pause (automatic
  when the app is backgrounded), game over with best score saved in PlayerPrefs.
  Everything uses a 5×7 glowing pixel font.
- **Haptics**: vibration on hit (iOS/Android).
- **Mobile builds**: `Pulse ▸ Build` menu and CLI entry points for APK/AAB/Xcode.

### Remaining
- Playtest tuning of the numbers in `PulseConfig`.
- App icon, splash screen and store listing art.
- Optional: lighter haptics via native plugins, settings toggle for
  sound/haptics, leaderboards (Game Center / Play Games).

## Decisions (resolved open questions; all values are in `PulseConfig`)
- **Lives**: 3 lives. Touching a bad object costs one and gives 1.2s of
  invulnerability. Missing a good object costs no life but resets the combo.
- **Scoring**: both. 10 points per catch × combo multiplier (+1 per 5
  consecutive catches, max ×5), plus 1 point per second survived.
- **Flip invulnerability**: yes. The player can't be hit during the 0.4s
  transition plus a 0.5s grace period after it (the catcher blinks). Good
  objects left behind the new floor by a flip don't count as misses.
- **Difficulty ramp** over 150s: spawn interval 1.0s → 0.38s, crossing time
  3.0s → 1.4s, hazard chance 25% → 45%, flip interval 8–16s → 5–9s.
