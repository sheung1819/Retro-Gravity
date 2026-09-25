# Pulse (Retro Gravity)

Retro vector-glow arcade survival for **iOS and Android**, built in Unity.
Slide the catcher along the floor to grab the green diamonds and dodge the red
stars. Every few seconds **gravity flips**: the floor jumps to another edge
and everything falling swings around to follow it.

Everything is generated in code: the glow shapes, the pixel font, the particles,
and the chiptune music and SFX. The repo has no binary assets, and opening it in
Unity gives you a playable game.

## Requirements

| | |
|---|---|
| Unity | **Unity 6 LTS (6000.0.x)** is recommended. 2022.3 LTS also works. |
| Unity modules | **Android Build Support** (with OpenJDK + Android SDK/NDK) and **iOS Build Support** |
| iOS builds | A Mac with Xcode 15+ and an Apple Developer account for device/App Store builds |

## First run

1. Unity Hub ▸ **Add** ▸ select this folder. Open it with Unity 6 LTS. If Hub
   asks about the version, pick any installed 6000.0.x or 2022.3.x.
2. On first import, `Assets/Editor/PulseProjectSetup.cs` automatically:
   - creates `Assets/Scenes/Main.unity` (a camera plus the `GameBootstrap` object),
   - adds it to Build Settings,
   - applies the mobile Player Settings (portrait, IL2CPP, ARM64, bundle id
     `com.retrogravity.pulse`, iOS 13+, Android API 23+).
3. Press **Play**. Click and drag to move. For a phone-shaped view, set the
   Game view to a portrait resolution (e.g. 1170×2532), or use the Device Simulator.
4. Commit the `.meta` files and `ProjectSettings/` that Unity generates.

If Unity asks to enable the new Input System backends, either answer works.
`PulseInput` supports both the new Input System and the legacy Input Manager.

Menu **Pulse ▸ Setup** re-creates the scene or re-applies the Player Settings.

## Building for phones

Use the **Pulse ▸ Build** menu:

| Menu item | Output |
|---|---|
| Android APK (development) | `Builds/Android/Pulse-dev.apk`, sideload with `adb install` |
| Android APK (release) | `Builds/Android/Pulse.apk` |
| Android App Bundle (Google Play) | `Builds/Android/Pulse.aab` |
| iOS Xcode Project | `Builds/iOS/`. Open `Unity-iPhone.xcodeproj` on a Mac, pick your team, then Run or Product ▸ Archive |

From the command line (CI):

```bash
# Android App Bundle
Unity -batchmode -quit -projectPath . -buildTarget Android \
  -executeMethod Pulse.EditorTools.PulseBuild.AndroidAab -logFile -

# iOS Xcode project
Unity -batchmode -quit -projectPath . -buildTarget iOS \
  -executeMethod Pulse.EditorTools.PulseBuild.IOS -logFile -
```

Signing is configured through environment variables, so secrets never go into the repo:

| Variable | Used for |
|---|---|
| `PULSE_KEYSTORE_PATH`, `PULSE_KEYSTORE_PASS`, `PULSE_KEY_ALIAS`, `PULSE_KEY_PASS` | Android release keystore. Without them the build uses Unity's debug key, which Google Play rejects. |
| `PULSE_APPLE_TEAM_ID` | iOS automatic signing team |
| `PULSE_BUILD_NUMBER` | Android `versionCode` / iOS build number |

## Project layout

```
Assets/
  Scripts/
    Core/      GameBootstrap (entry point), GameManager (state/score/lives/difficulty),
               PulseConfig (all tuning), GameEvents (event hub), Playfield (safe-area bounds), Haptics
    Gameplay/  CatcherController (swipe + gravity flip), CatcherVisual, ObjectSpawner, FallingObject
    Visual/    GlowKit (materials/shapes/particles), FlipEffects, CameraRig, BackgroundGrid
    Audio/     ChiptuneSynth (procedural music + SFX), AudioManager
    UI/        Hud, PixelText, PixelFont (5×7 LED-style font)
    Input/     PulseInput (touch/mouse; new or legacy input)
  Resources/Shaders/PulseAdditive.shader   additive glow shader
  Editor/      PulseProjectSetup (scene + player settings), PulseBuild (Android/iOS builds)
Docs/Pulse-V1-Spec.md                       design spec + build status
```

## Tuning

Select the **Pulse** object in `Main.unity`. Every gameplay number is in its
**Config** block: lives, scoring, the difficulty ramp (spawn rate, fall
speed, hazard ratio, flip interval), grace periods and object sizes. The
Catcher, AudioManager and other components add their own serialized fields at
runtime, and you can tweak those in Play mode.

To replace the synthesized audio, drop clips into the override slots on the
`Audio` object's `AudioManager`, or assign them in code.
