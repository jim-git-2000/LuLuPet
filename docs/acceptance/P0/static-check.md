# P0 Static Check

## Scope

- Created `LuluPet.sln`.
- Created `src/LuluPet.App`, `src/LuluPet.Core`, `src/LuluPet.Win32`, and `tests/LuluPet.Core.Tests`.
- Added project references:
  - `LuluPet.App` references `LuluPet.Core` and `LuluPet.Win32`.
  - `LuluPet.Win32` references `LuluPet.Core`.
  - `LuluPet.Core.Tests` references `LuluPet.Core`.
- Configured `LuluPet.App` as a WPF Windows executable:
  - `TargetFramework=net8.0-windows`
  - `UseWPF=true`
  - `OutputType=WinExe`
  - `EnableWindowsTargeting=true`
- Configured `LuluPet.Win32` as `net8.0` so platform interop wrappers can be compiled and unit-tested from Ubuntu while the WPF app remains Windows-only.
- Added `.github/workflows/build-windows.yml` for restore, test, publish, artifact verification, SHA256 generation, and artifact upload.

## Local Verification

- `dotnet --version` now returns `8.0.128`.
- `LuluPet.Core` offline restore/build passed.
- `LuluPet.Win32` offline restore/build passed.
- Full solution restore/test is blocked in this environment because NuGet access is denied.
- `LuluPet.App` local restore is blocked without access to the Windows Desktop reference package; GitHub Actions `windows-latest` is expected to provide the correct Windows build environment.

## CI Expectation

- GitHub Actions runner: `windows-latest`.
- Expected artifact name: `LuluPet-win-x64`.
- Expected executable path inside workflow: `publish/win-x64/LuluPet.exe`.
- Expected hash file: `publish/win-x64/LuluPet.exe.sha256.txt`.
