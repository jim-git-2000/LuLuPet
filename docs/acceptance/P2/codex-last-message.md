# P2 Execution Report

## Changed Files

- `src/LuluPet.Core/Config/AppSettings.cs`
- `src/LuluPet.Core/Config/JsonSettingsStore.cs`
- `src/LuluPet.App/MainWindow.xaml`
- `src/LuluPet.App/MainWindow.xaml.cs`
- `tests/LuluPet.Core.Tests/ProjectInfoTests.cs`
- `settings.json`
- `docs/acceptance/P2/static-check.md`
- `docs/acceptance/P2/local-validation.log`

## Completed

- Added Core settings model and JSON settings store.
- Added missing-file and invalid-JSON fallback to default window coordinates.
- Restored main window position from `settings.json` on startup.
- Saved `Left` and `Top` after successful drag and before close.
- Added unit tests for default load, save/load roundtrip, and invalid JSON fallback.

## Local Validation

- Static P2 inspection passed.
- `dotnet build src/LuluPet.Win32/LuluPet.Win32.csproj --no-restore` passed.
- Full `dotnet test` remains delegated to GitHub Actions because local NuGet access is unavailable.

## Suggested Commit Message

`feat(p2): persist desktop pet window position`
