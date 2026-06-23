# P9 Static Check

## Scope

Phase P9 adds a settings center and improves runtime resource fallback.

Implemented settings:

- `Scale`
- `Opacity`
- `Volume`
- `ClickThrough`
- `AutoStart`

## Settings Model

- `src/LuluPet.Core/Config/AppSettings.cs`
  - `AppearanceSettings.Scale`
  - `AppearanceSettings.Opacity`
  - `AudioSettings.Volume`
  - existing `InteractionSettings.ClickThrough`
  - existing `StartupSettings.AutoStart`
- `src/LuluPet.Core/Config/JsonSettingsStore.cs`
  - Normalizes missing settings sections.
  - Clamps:
    - `scale`: `0.75`-`1.5`
    - `opacity`: `0.35`-`1.0`
    - `volume`: `0.0`-`1.0`

Default `settings.json` now includes:

```json
{
  "appearance": {
    "scale": 1.0,
    "opacity": 1.0
  },
  "audio": {
    "volume": 0.6
  },
  "interaction": {
    "clickThrough": false
  },
  "startup": {
    "autoStart": false
  }
}
```

## Settings UI

- `src/LuluPet.App/SettingsWindow.xaml`
- `src/LuluPet.App/SettingsWindow.xaml.cs`

Controls:

- Scale slider.
- Opacity slider.
- Volume slider.
- Click-through checkbox.
- Auto-start checkbox.

The settings window is opened from the tray menu item `设置`.

## Runtime Behavior

- `Scale`
  - Immediately changes main window size, pet image size, and speech bubble size.
  - Saved to `settings.json`.
- `Opacity`
  - Immediately changes WPF window opacity.
  - Saved to `settings.json`.
- `Volume`
  - Saved to `settings.json` for future audio use.
- `ClickThrough`
  - Reuses P7 `WindowStyleService`.
  - Immediately applies or removes `WS_EX_TRANSPARENT`.
  - Saved to `settings.json`.
- `AutoStart`
  - Reuses P7 `StartupRegistryService`.
  - Immediately writes or removes the current-user `Run` value.
  - Saved to `settings.json`.

## Resource Fallback

- Missing animation directories are logged with `Debug.WriteLine`.
- Missing frame files show `MissingAssetFallback`.
- Corrupt or unreadable PNG frames are caught and show `MissingAssetFallback` instead of crashing.
- Speech bubble background already falls back to a WPF border when `assets/ui/bubble_default.png` is missing or invalid.
- Tray icon already falls back to `SystemIcons.Application`.

## Tests

- `tests/LuluPet.Core.Tests/P9SettingsTests.cs`
  - P9 default values.
  - P9 setting save/load.
  - P9 setting range clamping.

## README

- `README.md` documents settings keys and the SQLite data path.

## Manual Acceptance

Upload to GitHub and download the Windows artifact. Run `LuluPet.exe` and check:

- Tray menu opens `设置`.
- Moving Scale changes the pet size immediately and persists after restart.
- Moving Opacity changes transparency immediately and persists after restart.
- Moving Volume updates `settings.json`.
- Click-through checkbox applies immediately and persists after restart.
- Auto-start checkbox writes/removes the current-user `Run` value and persists after restart.
- Temporarily deleting part of `Assets/pet` or `Assets/ui/bubble_default.png` does not crash startup.
