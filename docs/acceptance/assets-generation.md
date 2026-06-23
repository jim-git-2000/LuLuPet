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

