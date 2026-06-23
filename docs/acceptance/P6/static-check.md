# P6 Static Check

## Scope

Phase P6 adds speech bubbles and click interaction.

Implemented items:

- `SpeechBubble` WPF control.
- Local dialogue JSON loading.
- Random dialogue selection.
- Pet click feedback.
- Periodic speech.
- Auto-hide speech bubble.
- Broken dialogue file fallback.

## Runtime Assets

- `assets/ui/bubble_default.png`
  - Used as the speech bubble background.
  - If missing or unreadable, `SpeechBubble` falls back to a simple WPF border.
- `assets/dialogues/lines.json`
  - Copied to app output as `Assets/dialogues/lines.json`.
  - `LuluPet.App.csproj` includes `assets/dialogues/*.json`.

## Core Design

- `src/LuluPet.Core/Dialogues/DialogueLineProvider.cs`
  - Supports both JSON array files and `{ "lines": [...] }` object files.
  - Trims empty lines and removes exact duplicates.
  - Falls back to built-in or caller-provided lines when the JSON file is missing, invalid, unreadable, or has no valid text.
  - Uses injectable `IPetRandom` for deterministic tests.

## App Integration

- `src/LuluPet.App/Controls/SpeechBubble.xaml`
  - Renders the bubble in a fixed `230x96` area.
  - Text is centered, wrapped, and trimmed to stay inside the bubble.
  - `IsHitTestVisible` is disabled so the bubble does not intercept pet interaction.
- `src/LuluPet.App/MainWindow.xaml`
  - Window size is `340x390`.
  - Bubble is aligned to the top.
  - Pet image is aligned to the bottom.
  - This keeps the bubble from fully covering the pet body.
- `src/LuluPet.App/MainWindow.xaml.cs`
  - Loads `Happy` animation from `Assets/pet/happy`.
  - Clicks on `PetImage` show a random dialogue line and play `Happy` for 2 seconds.
  - The bubble auto-hides after 4 seconds.
  - Periodic speech runs every 30 seconds while the window is visible.
  - Speech timers stop when the app is hidden to tray.

## Tests

- `tests/LuluPet.Core.Tests/DialogueLineProviderTests.cs`
  - Loads `{ "lines": [...] }`.
  - Loads plain array JSON.
  - Falls back when JSON is invalid.
  - Uses injected random index for deterministic line selection.

## Local Validation

- `python3 -m json.tool assets/dialogues/lines.json`: passed.
- `dotnet build src/LuluPet.Core/LuluPet.Core.csproj --no-restore`: passed.
- Package-free `/tmp/lulupet-p6-smoke` dialogue smoke test: passed.
- `dotnet build src/LuluPet.Win32/LuluPet.Win32.csproj --no-restore`: passed.
- `dotnet build src/LuluPet.App/LuluPet.App.csproj --no-restore`: blocked locally by missing Ubuntu WindowsDesktop SDK targets.
- `dotnet build tests/LuluPet.Core.Tests/LuluPet.Core.Tests.csproj --no-restore`: blocked locally by missing xUnit NuGet packages.

## Manual Acceptance

Upload to GitHub and download the Windows artifact. Run `LuluPet.exe` and check:

- Clicking Lulu shows a speech bubble and plays the Happy animation.
- The bubble disappears automatically after a few seconds.
- Leaving the app visible triggers periodic speech.
- Temporarily corrupting `Assets/dialogues/lines.json` does not crash startup or click interaction.
