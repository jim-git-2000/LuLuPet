# Asset Sheet Cutting Report

## Reference

- Source folder: `art-ref/lulu_pet_assets_24f/art-src/sheets_24f/`
- Runtime output folders: `assets/pet/`, `assets/ui/`, `assets/icons/`
- Style target: original soft 3D/Q-version desktop pet matching the generated sheet set.
- Visual traits: yellow rounded body, orange muzzle and small cap, brown shorts, glossy eyes, soft toy-like shading.

## Source Sheet Layout

- `lulu_idle_24f_sheet_alpha.png`: 4 rows x 7 columns, 28 frames
- `lulu_walk_24f_sheet_alpha.png`: 4 rows x 7 columns, 28 frames
- `lulu_happy_24f_sheet_alpha.png`: 4 rows x 6 columns, 24 frames
- `lulu_eat_24f_sheet_alpha.png`: 4 rows x 6 columns, 24 frames
- `lulu_drag_24f_sheet_alpha.png`: 4 rows x 6 columns, 24 frames
- `lulu_surprised_24f_sheet_alpha.png`: 4 rows x 6 columns, 24 frames
- `lulu_angry_24f_sheet_alpha.png`: 4 rows x 6 columns, 24 frames
- `lulu_sleep_24f_sheet_alpha.png`: 4 rows x 6 columns, 24 frames

The sheet images contain sprites wider than their visual grid cells, so runtime
frames were cut by alpha connected-component detection instead of raw grid
cropping. Components are sorted row-major and placed into `512x512` transparent
canvases.

## Runtime Assets

- Pet frames:
  - `idle`: 28 frames, `512x512`, RGBA
  - `walk`: 28 frames, `512x512`, RGBA
  - `happy`: 24 frames, `512x512`, RGBA
  - `eat`: 24 frames, `512x512`, RGBA
  - `drag`: 24 frames, `512x512`, RGBA
  - `surprised`: 24 frames, `512x512`, RGBA
  - `angry`: 24 frames, `512x512`, RGBA
  - `sleep`: 24 frames, `512x512`, RGBA
- UI:
  - `assets/ui/bubble_default.png`, `384x160`, RGBA
  - `assets/ui/mood_happy.png`, `64x64`, RGBA
  - `assets/ui/mood_hungry.png`, `64x64`, RGBA
  - `assets/ui/btn_feed.png`, `64x64`, RGBA
  - `assets/ui/btn_hug.png`, `64x64`, RGBA
- Icons:
  - `assets/icons/app.ico`
  - `assets/icons/tray_light.ico`
  - `assets/icons/tray_dark.ico`

## Alignment

- Standing/action poses (`idle`, `walk`, `happy`, `eat`, `drag`, `surprised`, `angry`) use:
  - body height: `410px`
  - horizontal center: about `256px`
  - bottom anchor: `464px`
- `sleep` uses:
  - body height: `220px`
  - horizontal center: about `256px`
  - bottom anchor: `460px`

## Validation

- All cut PNG pet frames are `512x512` RGBA and contain transparent pixels.
- File names follow the `lulu_{action}_0001.png` convention.
- Runtime pet PNG count: `200`.
- UI PNGs listed above are RGBA and contain transparent pixels.
- Icon files open as valid ICO images.
