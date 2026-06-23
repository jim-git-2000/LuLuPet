# P5 Static Check

## Scope

Phase P5 adds automatic pet behavior on top of the P4 frame animation player.

Implemented states:

- `Idle`
- `Walk`
- `Sleep`

## Core Design

- `src/LuluPet.Core/Behavior/PetStateMachine.cs`
  - Owns state transitions and elapsed-time accounting.
  - Exposes `CurrentState`, `StateElapsed`, `StateChanged`, `Tick`, `Wake`, and `ForceState`.
- `src/LuluPet.Core/Behavior/PetStateMachineOptions.cs`
  - Defines minimum idle time, decision interval, long-idle sleep threshold, probabilities, and walk/sleep duration ranges.
  - Validates duration and probability inputs.
- `src/LuluPet.Core/Behavior/IPetRandom.cs`
  - Allows deterministic tests by replacing random number generation.
- `src/LuluPet.Core/Behavior/SystemPetRandom.cs`
  - Production adapter around `System.Random`.

Default behavior:

- Idle waits at least 8 seconds before random decisions.
- Idle makes a decision every 5 seconds.
- Idle can enter Walk with probability `0.45`.
- Idle can enter Sleep after 45 seconds of cumulative awake time with probability `0.35`.
- Walk lasts 5-12 seconds, then returns to Idle.
- Sleep lasts 15-30 seconds, then returns to Idle.

## App Integration

- `src/LuluPet.App/MainWindow.xaml.cs`
  - Adds a `_stateTimer` ticking every 100 ms.
  - Calls `_stateMachine.Tick(elapsed)` independently from the frame animation timer.
  - Maps state changes to animation actions using the existing `TryPlayAction`.
  - Logs transitions with `Debug.WriteLine`.
  - Moves the window horizontally during `Walk` at `36` pixels per second.
  - Reverses walk direction when reaching the current WPF `SystemParameters.WorkArea` horizontal bounds.
  - Pauses the state timer when the tray hides the window and restarts it when shown.

Manual keyboard controls remain available:

- `I`: force Idle
- `W`: force Walk
- `S`: force Sleep

## Tests

- `tests/LuluPet.Core.Tests/PetStateMachineTests.cs`
  - `Tick_TransitionsFromIdleToWalk_WhenWalkRollSucceeds`
  - `Tick_ReturnsFromWalkToIdle_WhenWalkDurationEnds`
  - `Tick_TransitionsFromLongIdleToSleep_ThenWakesToIdle`
  - `Tick_CanSleepAfterWalking_WhenAwakeTimeExceedsSleepThreshold`
  - `StateChanged_RaisesTransitionSequence`

The test file uses a deterministic `SequencePetRandom` fake and short state durations so transitions are stable and fast.

## Local Validation

- `dotnet build src/LuluPet.Core/LuluPet.Core.csproj --no-restore`: passed.
- Package-free `/tmp/lulupet-p5-smoke` state-machine smoke test: passed.
- `dotnet build src/LuluPet.Win32/LuluPet.Win32.csproj --no-restore`: passed.
- `dotnet build tests/LuluPet.Core.Tests/LuluPet.Core.Tests.csproj --no-restore`: blocked locally by missing xUnit NuGet packages.
- `dotnet build src/LuluPet.App/LuluPet.App.csproj --no-restore`: blocked locally by missing Ubuntu WindowsDesktop SDK targets.

## Manual Acceptance

Upload to GitHub and download the Windows artifact. Run `LuluPet.exe` for 5 minutes without interaction and observe:

- Idle animation appears initially.
- Walk appears at least once and the pet moves horizontally.
- Sleep appears after longer idle periods.
- Walk and Sleep both return to Idle automatically.
