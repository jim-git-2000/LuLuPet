# P3 Static Check

## Tray Lifecycle

- `LuluPet.App.csproj` enables `UseWindowsForms=true`.
- `MainWindow` creates a `System.Windows.Forms.NotifyIcon`.
- The tray icon uses `Assets/icons/app.ico` and falls back to `SystemIcons.Application` if the file is missing or invalid.
- The tray tooltip text is `LuluPet`.

## Tray Menu

- Context menu contains:
  - `显示`
  - `隐藏`
  - separator
  - `退出`
- Double-clicking the tray icon restores and activates the main window.
- `显示` and `隐藏` menu item enabled states follow current window visibility.

## Close And Exit Behavior

- Clicking the window close button calls `Close()`.
- `OnClosing` cancels normal close and hides the window unless `退出` was requested.
- Hiding saves the current window position and stops the animation timer.
- Showing restores focus and restarts the animation timer if an action is playing.
- `退出` sets an exit flag, closes the window, saves position, stops animation, hides the tray icon, and disposes `NotifyIcon`.

## Asset

- `assets/icons/app.ico` exists and is copied to `Assets/icons/app.ico` in the app output.

