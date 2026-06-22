# P1 Static Check

## Window Properties

- `WindowStyle=None`
- `AllowsTransparency=True`
- `Background=Transparent`
- `ResizeMode=NoResize`
- `ShowInTaskbar=False`
- `Topmost=True`
- `WindowStartupLocation=CenterScreen`

## Static Pet Frame

- `LuluPet.App.csproj` sets `AssemblyName=LuluPet`, so `dotnet publish` emits `LuluPet.exe` instead of `LuluPet.App.exe`.
- `MainWindow` tries to load `Assets/pet/idle/lulu_idle_0001.png` from the published output directory.
- `LuluPet.App.csproj` copies `assets/pet/idle/*.png` into the output under `Assets/pet/idle/`.
- If the PNG is missing, startup remains safe and shows a small fallback label instead of throwing.

## Out Of Scope

- Dragging, tray lifecycle, animation playback, and persisted position remain for later phases.
