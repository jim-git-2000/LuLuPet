# Asset Generation Report

## Reference

- Source folder: `art-ref/`
- Style target: original soft 3D/Q-version desktop pet inspired by the reference set.
- Visual traits: yellow rounded body, orange muzzle and small cap, brown shorts, glossy eyes, soft toy-like shading.

## Generated Runtime Assets

- Pet frames:
  - `idle`: 8 frames, `512x512`, RGBA
  - `walk`: 12 frames, `512x512`, RGBA
  - `sleep`: 8 frames, `512x512`, RGBA
  - `happy`: 8 frames, `512x512`, RGBA
  - `eat`: 10 frames, `512x512`, RGBA
  - `drag`: 4 frames, `512x512`, RGBA
  - `surprised`: 4 frames, `512x512`, RGBA
- UI:
  - `assets/ui/bubble_default.png`, `384x160`, RGBA
  - `assets/ui/mood_happy.png`, `64x64`, RGBA
  - `assets/ui/mood_hungry.png`, `64x64`, RGBA
  - `assets/ui/btn_feed.png`, `64x64`, RGBA
  - `assets/ui/btn_hug.png`, `64x64`, RGBA
  - `assets/ui/splash.png`, `1024x576`, RGBA
- Icons:
  - `assets/icons/app.ico`
  - `assets/icons/tray_light.ico`
  - `assets/icons/tray_dark.ico`

## Validation

- All generated PNG pet frames are `512x512` RGBA and contain transparent pixels.
- File names follow the `lulu_{action}_0001.png` convention.
- `assets/` total size after generation: about `2.2M`.

# Asset Sheet Cutting Report

## Reference

- Source folder: `art-ref/sheets/`
- Runtime output folders: `assets/pet/`, `assets/ui/`, `assets/icons/`
- Style target: original soft 3D/Q-version desktop pet matching the generated sheet set.
- Visual traits: yellow rounded body, orange muzzle and small cap, brown shorts, glossy eyes, soft toy-like shading.

## Source Sheets

- `lulu_idle_sheet_alpha.png`: 4 columns x 2 rows
- `lulu_walk_sheet_alpha.png`: 4 columns x 3 rows
- `lulu_sleep_sheet_alpha.png`: 4 columns x 2 rows
- `lulu_happy_sheet_alpha.png`: 4 columns x 2 rows
- `lulu_eat_sheet_alpha.png`: 5 columns x 2 rows
- `lulu_drag_sheet_alpha.png`: 4 columns x 1 row
- `lulu_surprised_sheet_alpha.png`: 4 columns x 1 row
- `bubble_default_source.png`
- `mood_sheet_source.png`
- `button_sheet_source.png`
- `app_icon_master_source.png`

## Runtime Assets

- Pet frames:
  - `idle`: 8 frames, `512x512`, RGBA
  - `walk`: 12 frames, `512x512`, RGBA
  - `sleep`: 8 frames, `512x512`, RGBA
  - `happy`: 8 frames, `512x512`, RGBA
  - `eat`: 10 frames, `512x512`, RGBA
  - `drag`: 4 frames, `512x512`, RGBA
  - `surprised`: 4 frames, `512x512`, RGBA
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

`assets/ui/splash.png` still exists as a previous runtime asset, but no splash source sheet was provided in `art-ref/sheets/`, so it was not replaced during this sheet cutting pass.

## Validation

- All cut PNG pet frames are `512x512` RGBA and contain transparent pixels.
- File names follow the `lulu_{action}_0001.png` convention.
- UI PNGs listed above are RGBA and contain transparent pixels.
- Icon files open as valid ICO images.
- `assets/` total size after sheet cutting: about `5.4M`.
