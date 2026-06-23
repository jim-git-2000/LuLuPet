# P7 Static Check

## Scope

Phase P7 adds Win32 integration for:

- Click-through window mode.
- Native tool-window and layered-window styles.
- Topmost Z-order maintenance.
- Screen boundary clamping.
- Current-user auto-start via the Windows `Run` registry key.

## Win32 Interop

- `src/LuluPet.Win32/NativeMethods.cs`
  - `GetWindowLongPtr`
  - `SetWindowLongPtr`
  - `SetWindowPos`
  - `WS_EX_LAYERED`
  - `WS_EX_TRANSPARENT`
  - `WS_EX_TOOLWINDOW`
  - `HWND_TOPMOST`
- `src/LuluPet.Win32/WindowStyleService.cs`
  - Reads current extended window styles.
  - Always applies `WS_EX_LAYERED` and `WS_EX_TOOLWINDOW`.
  - Adds/removes `WS_EX_TRANSPARENT` according to the click-through setting.
  - Calls `SetWindowPos(HWND_TOPMOST, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER)`.

## Startup Registry

- `src/LuluPet.Win32/StartupRegistryService.cs`
  - Writes to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
  - Uses `ProjectInfo.ProductName` as the value name.
  - Stores the current process executable path with quotes.
  - Deletes the value when auto-start is disabled.
  - Registry access methods are annotated as Windows-only.

## Settings

- `settings.json`
  - `interaction.clickThrough`
  - `startup.autoStart`
- `src/LuluPet.Core/Config/AppSettings.cs`
  - Adds `InteractionSettings` and `StartupSettings`.
- `src/LuluPet.Core/Config/JsonSettingsStore.cs`
  - Normalizes missing P7 sections to safe defaults.

Default values:

- `clickThrough`: `false`
- `autoStart`: `false`

## App Integration

- `src/LuluPet.App/MainWindow.xaml.cs`
  - Gets the native window handle via `WindowInteropHelper(this).Handle` during `SourceInitialized`.
  - Applies native styles after the handle exists.
  - Adds tray menu toggles:
    - `点击穿透`
    - `开机启动`
  - Saves toggle state back to `settings.json`.
  - Keeps tray toggles available even when click-through is enabled.
  - Clamps window position on restore, drag, walking movement, location changes, and size changes.
  - Uses `SystemParameters.WorkArea` so the full window stays within the current work area.

## Tests

- `tests/LuluPet.Core.Tests/P7SettingsTests.cs`
  - P7 setting defaults.
  - P7 setting persistence.
  - executable path quoting for `Run` registry values.

The local Ubuntu environment cannot run the xUnit project until NuGet packages are restored, but GitHub Actions restores packages before `dotnet test`.

## Local Validation

- `python3 -m json.tool settings.json`: passed.
- `dotnet build src/LuluPet.Core/LuluPet.Core.csproj --no-restore`: passed.
- `dotnet build src/LuluPet.Win32/LuluPet.Win32.csproj --no-restore`: passed with 0 warnings and 0 errors.
- `git diff --check`: passed.
- `dotnet build src/LuluPet.App/LuluPet.App.csproj --no-restore`: blocked locally by missing Ubuntu WindowsDesktop SDK targets.
- `dotnet build tests/LuluPet.Core.Tests/LuluPet.Core.Tests.csproj --no-restore`: blocked locally by missing xUnit NuGet packages.

## Manual Acceptance

Upload to GitHub and download the Windows artifact. Run `LuluPet.exe` and check:

- Tray menu shows `点击穿透` and `开机启动`.
- Enabling `点击穿透` lets clicks pass through the pet window to desktop icons.
- Disabling `点击穿透` allows pet click and drag behavior again.
- Dragging or walking cannot move the pet fully outside the work area.
- Enabling `开机启动` writes `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\LuluPet`.
- Disabling `开机启动` removes the same registry value.
- Both toggles persist in `settings.json`.
