# Immersive Dialogue UI

[Download on Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/53678)

Immersive Dialogue UI is a lightweight UI mod for Stardew Valley. It moves the NPC portrait outside the dialogue box, keeps the box shallow, and adds a small vanilla-style nameplate.

The layout uses Stardew's own portrait, dialogue-box, name-scroll, font, and advance-indicator assets. All custom portraits are supported.

## Features

- Large portrait above the left edge of the box
- Wide, shallow, bottom-anchored dialogue box
- Text width independent of portrait width
- Compact name scroll tucked beneath the portrait
- Compact or normal box size, independent from portrait size
- Unchanged dialogue content, expressions, paging, input, and game logic

## Requirements

1. [SMAPI](https://smapi.io/)
2. [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) 2.9.0 or later
3. [Dialogue Display Framework Continued (DDFC)](https://www.nexusmods.com/stardewvalley/mods/22678) 0.7.5 or later

## Installation

1. Install the three requirements above.
2. Remove the original Dialogue Display Framework (`aedenthorn.DialogueDisplayFramework`) if it is installed. Use DDFC instead; do not run both frameworks together.
3. Put this folder in `Stardew Valley/Mods`.
4. Start the game through SMAPI.

Content Patcher creates `config.json` on first launch. If Generic Mod Config Menu is installed, the same settings are available in-game.

Only use one Dialogue Display Framework layout pack at a time unless the packs explicitly document compatibility.

## Configuration

| Setting | Options | Default |
| --- | --- | --- |
| `BoxSize` | Compact (1088×320), Normal (1200×384) | Compact |
| `PortraitSize` | Small (4× vanilla), Medium (5×), Large (6×) | Medium |

Box and portrait sizes are independent, so either can be changed without affecting the other. Normal matches vanilla's full 1200×384 dialogue area. The portrait sits slightly behind the top border, with the nameplate layered just beneath it and slightly inset. Changing portrait size keeps that alignment and never changes the dialogue text width or forces the box to grow.

Dialogue Display Framework Continued itself provides framework-level settings. This pack supplies the actual layout data, so its visual values are deliberately kept here rather than duplicated as low-level configuration fields.

## Performance

The pack applies one data edit and uses DDFC's existing draw path. It performs no per-frame asset scans, render-target work, texture processing, or gameplay polling.
