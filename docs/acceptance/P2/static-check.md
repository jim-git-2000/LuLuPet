# P2 Static Check

## Configuration

- `AppSettings` stores `window.left` and `window.top`.
- `JsonSettingsStore` loads defaults when `settings.json` is missing, unreadable, or invalid.
- `JsonSettingsStore` writes indented camelCase JSON.
- Default position is `Left=1200`, `Top=650`.
- `LuluPet.App.csproj` copies the default root `settings.json` into the app output.

## Window Behavior

- `MainWindow` reads `settings.json` from `AppContext.BaseDirectory`.
- Startup restores `Left` and `Top` before the window is shown.
- Left mouse drag uses `DragMove()`.
- Position is saved after a successful drag and before explicit close.
- Clicking the close button is excluded from drag handling.
- Save failures from IO or permission errors are swallowed so closing the app does not crash.

## Tests Added

- `JsonSettingsStore_LoadsDefaults_WhenFileDoesNotExist`
- `JsonSettingsStore_SavesAndLoadsWindowPosition`
- `JsonSettingsStore_LoadsDefaults_WhenJsonIsInvalid`
