# P8 Static Check

## Scope

Phase P8 adds SQLite persistence and interaction logging.

Implemented items:

- SQLite package dependency in `src/LuluPet.Core/LuluPet.Core.csproj`.
- User data database path.
- Database initialization on app startup.
- Tables:
  - `pet_profile`
  - `pet_status`
  - `interaction_log`
- Status save/load.
- Click interaction logging.
- Interaction count tracking.
- Core integration tests for repository behavior.

## Dependency

- `Microsoft.Data.Sqlite` version `8.0.10`

GitHub Actions already runs `dotnet restore` before test and publish, so the package should be available on the Windows runner.

## Data Path

Default database path:

```text
%LOCALAPPDATA%\LuluPet\data\lulupet.db
```

Implemented by:

- `src/LuluPet.Core/Storage/LuluPetDataPaths.cs`

If `LocalApplicationData` is unavailable, the code falls back to `AppContext.BaseDirectory`.

## Schema

Schema artifact:

- `docs/acceptance/P8/db-schema.sql`

Runtime schema is embedded in:

- `src/LuluPet.Core/Storage/SqlitePetRepository.cs`

Initialization behavior:

- Creates the parent data directory.
- Opens SQLite database.
- Enables WAL mode with `PRAGMA journal_mode=WAL`.
- Enables foreign keys with `PRAGMA foreign_keys=ON`.
- Creates required tables and interaction index.
- Inserts singleton default rows for `pet_profile` and `pet_status` if missing.

## App Integration

- `src/LuluPet.App/MainWindow.xaml.cs`
  - Creates `SqlitePetRepository` with `LuluPetDataPaths.GetDefaultDatabasePath()`.
  - Calls `InitializeStorage()` before restoring window position.
  - Loads previous `pet_status.window_left`, `pet_status.window_top`, and `pet_status.state` when the database already exists.
  - On first database creation, seeds `pet_status` from the current `settings.json` position.
  - Saves status when position is saved and when the state machine changes state.
  - Records a `click` interaction when the pet is clicked.
  - Logs database path and storage failures with `Debug.WriteLine`.
  - Storage failures do not crash startup or interaction.

## Tests

- `tests/LuluPet.Core.Tests/SqlitePetRepositoryTests.cs`
  - `Initialize_CreatesDatabaseAndDefaultRows`
  - `SaveStatus_CanBeLoadedByNewRepository`
  - `RecordInteraction_WritesLogAndIncrementsProfileCount`

## Local Validation

- `git diff --check`: passed.
- Static search confirms repository, schema, app startup integration, status persistence, and interaction logging are present.
- `dotnet restore src/LuluPet.Core/LuluPet.Core.csproj`: blocked locally by restricted network access to `https://api.nuget.org/v3/index.json`.
- `dotnet build src/LuluPet.Core/LuluPet.Core.csproj --no-restore`: blocked locally because `Microsoft.Data.Sqlite` is not restored.
- `dotnet build src/LuluPet.App/LuluPet.App.csproj --no-restore`: still blocked locally by missing Ubuntu WindowsDesktop SDK targets.

## Manual Acceptance

Upload to GitHub and download the Windows artifact. Run `LuluPet.exe` and check:

- First launch creates `%LOCALAPPDATA%\LuluPet\data\lulupet.db`.
- Closing and relaunching restores the last saved pet position/state.
- Clicking Lulu writes a row to `interaction_log`.
- `pet_profile.interaction_count` increases after click interactions.
