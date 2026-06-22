# P4 Static Check

## Core Animation

- `FrameAnimationPlayer` supports named actions, FPS, looping, stopping, and action switching.
- `AnimationFrameLoader` loads `*.png` from one action directory in case-insensitive filename order.
- `AnimationFrameChangedEventArgs` exposes action name, frame index, and frame path.

## App Integration

- `MainWindow` loads Idle, Walk, and Sleep from `Assets/pet/idle`, `Assets/pet/walk`, and `Assets/pet/sleep`.
- `DispatcherTimer` ticks every 33 ms and advances the Core player by real elapsed time.
- `BitmapImage` frames are cached after first decode to reduce UI-thread disk work.
- Keyboard shortcuts for manual acceptance:
  - `I` switches to Idle.
  - `W` switches to Walk.
  - `S` switches to Sleep.
- If no PNG frames are present, startup remains safe and shows the existing missing-asset fallback.

## Resource Layout

```text
assets/pet/idle/lulu_idle_0001.png
assets/pet/walk/lulu_walk_0001.png
assets/pet/sleep/lulu_sleep_0001.png
```

`LuluPet.App.csproj` copies `assets/pet/**/*.png` into the publish output under `Assets/pet/`.

## Tests Added

- `FrameAnimationPlayer_LoopsAtEndOfAction`
- `FrameAnimationPlayer_SwitchesActionFromFirstFrame`
- `FrameAnimationPlayer_StopsAtLastFrame_WhenLoopDisabled`
- `AnimationFrameLoader_LoadsPngFramesInNameOrder`

