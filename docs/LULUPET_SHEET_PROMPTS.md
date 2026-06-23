# LuluPet 桌宠图片绘制需求与 Sheet 提示词

## 使用方式

把本文件和同目录下所有 `LULU_ref (*.jpg)` 参考图一起发给网页端 ChatGPT。目标是让它按参考图的整体美术风格，生成原创桌宠「水豚噜噜」的透明 PNG 动画 sheet。

重要：不要直接复制参考图中的具体构图或单张姿势；只参考风格、材质、比例、颜色和角色气质，生成原创角色。

## 总体风格要求

- 角色：原创 Q 版水豚/河马感桌宠，名字叫「噜噜」。
- 风格：高质量 3D 软胶/黏土玩具质感，圆润、柔和、可爱。
- 形象特征：
  - 淡黄色圆润身体。
  - 大面积橙色口鼻区域。
  - 小圆耳朵。
  - 头顶一个小橙色帽子/小果子形装饰。
  - 棕色短裤。
  - 大而亮的黑蓝色椭圆眼睛，有白色高光。
  - 短小手脚，整体比例像参考图中的可爱 3D 桌宠。
- 画面要求：
  - 全身完整。
  - 角色居中。
  - 每一帧脚底基线尽量一致。
  - 不要文字、编号、水印、签名。
  - 不要复杂背景。
  - 不要阴影投在背景上。
  - 不要多余道具，除非动作说明明确要求。

## 输出技术要求

- 最终需要切成单帧 PNG：
  - 宠物帧：`512x512`
  - 透明背景，RGBA。
  - 文件名四位编号，例如 `lulu_idle_0001.png`。
- 如果网页端不能直接输出透明 PNG，请让它先输出「纯色绿幕背景 sheet」：
  - 背景必须是完全统一的 `#00ff00`。
  - 背景无渐变、无纹理、无地面、无阴影。
  - 角色本体不要出现绿色。
  - 后续我会抠绿变透明。
- Sheet 布局：
  - 每个格子大小一致。
  - 角色不能跨格。
  - 每格留足边距。
  - 帧顺序从左到右、从上到下。

## 需要生成的 Sheet 总表

| 动作 | 帧数 | Sheet 布局 | 单帧命名 |
|---|---:|---|---|
| idle | 8 | 4 列 x 2 行 | `lulu_idle_0001.png` ~ `lulu_idle_0008.png` |
| walk | 12 | 4 列 x 3 行 | `lulu_walk_0001.png` ~ `lulu_walk_0012.png` |
| sleep | 8 | 4 列 x 2 行 | `lulu_sleep_0001.png` ~ `lulu_sleep_0008.png` |
| happy | 8 | 4 列 x 2 行 | `lulu_happy_0001.png` ~ `lulu_happy_0008.png` |
| eat | 10 | 5 列 x 2 行 | `lulu_eat_0001.png` ~ `lulu_eat_0010.png` |
| drag | 4 | 4 列 x 1 行 | `lulu_drag_0001.png` ~ `lulu_drag_0004.png` |
| surprised | 4 | 4 列 x 1 行 | `lulu_surprised_0001.png` ~ `lulu_surprised_0004.png` |

---

# 通用负面提示词

每次生成都附加下面这些限制：

```text
No text, no watermark, no signature, no labels, no frame numbers.
No realistic animal anatomy, no scary expression, no extra character.
No background scene, no floor plane, no cast shadow, no gradient background.
Do not change the character identity between frames.
Do not crop the body, ears, feet, or cap.
Do not use green anywhere on the character if using chroma-key background.
```

---

# Sheet 1：Idle 待机

```text
请参考我上传的 LULU_ref 图片风格，生成原创角色「水豚噜噜」的 idle 待机动画 sheet。

角色是 Q 版 3D 软胶玩具质感：淡黄色圆润身体，大橙色口鼻，小圆耳朵，头顶小橙色帽子，棕色短裤，大而亮的黑蓝色眼睛。

动作：待机呼吸和眨眼。8 帧。
帧变化：身体轻微上下呼吸，头部轻微浮动，其中一帧眨眼，其余保持温柔微笑。
布局：4 列 x 2 行 sprite sheet。
每格一个完整角色，全身居中，脚底基线一致，留足透明边距。
背景：透明背景；如果不能透明，请用完全统一的 #00ff00 纯绿背景。
风格：高质量 3D clay toy render，柔和光照，和参考图高度接近。
不要文字、编号、水印。
```

---

# Sheet 2：Walk 行走

```text
请参考我上传的 LULU_ref 图片风格，生成原创角色「水豚噜噜」的 walk 行走动画 sheet。

角色外观必须和 idle 完全一致：淡黄色身体、大橙色口鼻、小橙帽、棕色短裤、圆润 3D 软胶玩具质感。

动作：原地小步走。12 帧。
帧变化：左右脚交替，身体轻微左右摇晃，头部轻微上下弹动，手臂自然摆动。不要真的移动出格子。
布局：4 列 x 3 行 sprite sheet。
每格一个完整角色，全身居中，脚底基线尽量一致，留足透明边距。
背景：透明背景；如果不能透明，请用完全统一的 #00ff00 纯绿背景。
风格：高质量 3D clay toy render，柔和光照，和参考图高度接近。
不要文字、编号、水印。
```

---

# Sheet 3：Sleep 睡觉

```text
请参考我上传的 LULU_ref 图片风格，生成原创角色「水豚噜噜」的 sleep 睡觉动画 sheet。

角色外观必须和其他 sheet 一致：淡黄色身体、大橙色口鼻、小橙帽、棕色短裤、圆润 3D 软胶玩具质感。

动作：侧躺或趴睡打呼。8 帧。
帧变化：眼睛闭上，嘴巴微张或轻微打呼，身体轻微起伏。可以有非常小的 Zzz 符号，但如果影响切图请不要加文字。
布局：4 列 x 2 行 sprite sheet。
每格一个完整角色，身体完整不裁切，位置稳定，留足透明边距。
背景：透明背景；如果不能透明，请用完全统一的 #00ff00 纯绿背景。
风格：高质量 3D clay toy render，柔和光照，和参考图高度接近。
不要水印、编号、复杂背景。
```

---

# Sheet 4：Happy 开心

```text
请参考我上传的 LULU_ref 图片风格，生成原创角色「水豚噜噜」的 happy 开心动画 sheet。

角色外观必须和 idle 完全一致：淡黄色身体、大橙色口鼻、小橙帽、棕色短裤、圆润 3D 软胶玩具质感。

动作：被摸头/被点击后开心反馈。8 帧。
帧变化：开心张嘴笑，眼睛变亮或眯眼，双手上举或贴脸，身体轻微弹跳。
布局：4 列 x 2 行 sprite sheet。
每格一个完整角色，全身居中，脚底基线尽量一致，留足透明边距。
背景：透明背景；如果不能透明，请用完全统一的 #00ff00 纯绿背景。
风格：高质量 3D clay toy render，柔和光照，和参考图高度接近。
不要文字、编号、水印。
```

---

# Sheet 5：Eat 吃草

```text
请参考我上传的 LULU_ref 图片风格，生成原创角色「水豚噜噜」的 eat 吃草动画 sheet。

角色外观必须和 idle 完全一致：淡黄色身体、大橙色口鼻、小橙帽、棕色短裤、圆润 3D 软胶玩具质感。

动作：抱着一小束草或叶子吃东西。10 帧。
帧变化：手拿草，嘴巴咀嚼，草叶轻微变化，眼神满足。动作可爱，不要夸张。
布局：5 列 x 2 行 sprite sheet。
每格一个完整角色，全身居中，脚底基线尽量一致，留足透明边距。
背景：透明背景；如果不能透明，请用完全统一的 #00ff00 纯绿背景。
风格：高质量 3D clay toy render，柔和光照，和参考图高度接近。
不要文字、编号、水印。
```

---

# Sheet 6：Drag 被拖拽

```text
请参考我上传的 LULU_ref 图片风格，生成原创角色「水豚噜噜」的 drag 被拖拽动画 sheet。

角色外观必须和 idle 完全一致：淡黄色身体、大橙色口鼻、小橙帽、棕色短裤、圆润 3D 软胶玩具质感。

动作：被鼠标拖拽时的轻微慌张姿态。4 帧。
帧变化：身体轻微倾斜，表情有点困惑或紧张，手脚跟随摆动。不要出现真实鼠标指针，避免 UI 干扰。
布局：4 列 x 1 行 sprite sheet。
每格一个完整角色，全身居中，留足透明边距。
背景：透明背景；如果不能透明，请用完全统一的 #00ff00 纯绿背景。
风格：高质量 3D clay toy render，柔和光照，和参考图高度接近。
不要文字、编号、水印。
```

---

# Sheet 7：Surprised 惊讶

```text
请参考我上传的 LULU_ref 图片风格，生成原创角色「水豚噜噜」的 surprised 惊讶动画 sheet。

角色外观必须和 idle 完全一致：淡黄色身体、大橙色口鼻、小橙帽、棕色短裤、圆润 3D 软胶玩具质感。

动作：突然被点到或听到声音时惊讶。4 帧。
帧变化：眼睛睁大，嘴巴 O 形张开，双手微微抬起，身体轻微后仰。
布局：4 列 x 1 行 sprite sheet。
每格一个完整角色，全身居中，留足透明边距。
背景：透明背景；如果不能透明，请用完全统一的 #00ff00 纯绿背景。
风格：高质量 3D clay toy render，柔和光照，和参考图高度接近。
不要文字、编号、水印。
```

---

# UI 资源提示词

## 气泡底图 bubble_default.png

```text
生成一个桌宠对话气泡 UI 图片，风格匹配 LULU_ref 的柔和 3D 可爱风。
尺寸比例约 384x160。
白色或奶油色圆角气泡，浅橙描边，小尾巴指向左下方。
可以在右上角放一个很小的噜噜头部装饰，但不要放文字。
透明背景。
```

## 心情图标 mood_happy.png / mood_hungry.png

```text
生成 64x64 桌宠心情图标，风格匹配 LULU_ref。
透明背景，圆形小头像。
happy：开心微笑。
hungry：有点饿、期待食物。
不要文字、不要水印。
```

## 设置按钮 btn_feed.png / btn_hug.png

```text
生成 64x64 桌宠按钮图标，风格匹配 LULU_ref。
透明背景，圆角小按钮质感。
feed：草叶/食物图标，暖色按钮。
hug：抱抱/爱心/双手图标，暖色按钮。
不要文字、不要水印。
```

## 图标 app.ico / tray_light.ico / tray_dark.ico

```text
生成应用图标，风格匹配 LULU_ref。
主体是噜噜的圆润头部：淡黄色脸、大橙色口鼻、小橙帽、亮眼睛。
需要适合 16x16、32x32、48x48、64x64 小尺寸识别。
透明背景。
不要文字、不要水印。
```

---

# 切图和落地规范

网页端生成 sheet 后，请保存原始 sheet 到：

```text
art-src/sheets/
```

再把每个 sheet 切成单帧，输出到：

```text
assets/pet/idle/lulu_idle_0001.png
assets/pet/walk/lulu_walk_0001.png
assets/pet/sleep/lulu_sleep_0001.png
assets/pet/happy/lulu_happy_0001.png
assets/pet/eat/lulu_eat_0001.png
assets/pet/drag/lulu_drag_0001.png
assets/pet/surprised/lulu_surprised_0001.png
```

所有单帧必须是：

```text
512x512
PNG
RGBA
透明背景
四位编号
同动作帧尺寸一致
角色脚底或身体基准位置稳定
```

