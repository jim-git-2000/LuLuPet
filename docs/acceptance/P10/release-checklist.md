# P10 Release Checklist

## Release Gate

- Workflow: `.github/workflows/build-windows.yml`
- Triggers:
  - push to `main`
  - pull request to `main`
  - tag push matching `v*`
  - `workflow_dispatch`
- Required CI steps:
  - `dotnet restore`
  - `dotnet test --configuration Release --no-restore`
  - `dotnet publish src/LuluPet.App/LuluPet.App.csproj -c Release -r win-x64 --self-contained true`
  - verify `publish/win-x64/LuluPet.exe` exists
  - reject suspiciously small exe files under `1MB`
  - write `publish/win-x64/LuluPet.exe.sha256.txt`
  - upload artifact `LuluPet-win-x64`

## Tag Release

- Release tag format: `v*`
- First stable release target: `v1.0.0`
- Tag command:

```bash
git tag v1.0.0
git push origin v1.0.0
```

- Tag builds create or update a GitHub Release.
- Release assets:
  - `LuluPet.exe`
  - `LuluPet.exe.sha256.txt`

## Static Checks

- README contains:
  - download and run instructions
  - settings documentation
  - current interaction list
  - no-audio note for reserved volume setting
  - development commands
  - release and rollback instructions
- Project icon is configured through `ApplicationIcon`.
- Runtime icon loading is guarded so missing icon files do not block startup.
- Startup crash logging writes to `%LOCALAPPDATA%\LuluPet\crash.log`.

## Local Validation Notes

- Ubuntu local WPF publish is not expected to pass because the installed SDK lacks `Microsoft.NET.Sdk.WindowsDesktop`.
- Ubuntu `dotnet test` can also fail in the sandbox before build with MSBuild named-pipe permission errors.
- Windows GitHub Actions is the authoritative build/test/publish gate for P10.

## Manual Acceptance

After GitHub Actions produces the artifact or Release asset, download `LuluPet.exe` on Windows and check:

- exe starts by double click
- tray icon is visible
- app icon is embedded in the exe
- Idle/Walk/Sleep/Happy/Drag/Eat/Angry animations work
- settings apply immediately and persist after restart
- click-through and auto-start toggles do not crash
- crash log path exists or is created only when an unhandled startup/runtime exception occurs

## Evidence To Fill After Release

- CI run link:
- artifact name: `LuluPet-win-x64`
- artifact size:
- `LuluPet.exe` SHA256:
- Release page link:
- `v1.0.0` tag commit:
- Windows manual acceptance result:

## Rollback

- Code regression: `git revert <bad-commit>` and wait for `main` workflow to pass.
- Release regression: annotate or remove the bad Release asset and point users to the previous known-good tag.
- Local data issue: close LuluPet, back up `%LOCALAPPDATA%\LuluPet\data\lulupet.db`, restore the previous database if available, then run the previous known-good exe.
