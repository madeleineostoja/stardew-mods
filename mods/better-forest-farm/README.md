# Better Forest Farm

An immersive, configurable alteration of the Forest (foraging) farm map for Stardew Valley. Compatible with all recolours.

## Features

- **Forest Lake (optional)** — Replace most of ponds on the farm with a large lake that extends into the bottom right of the farm, with a small rustic jetty attached to it. Overall land cover doesn't change too much so as not to drastically change the vanilla balancing of the map, but it should be much easier to plan around and generally fits the foresty vibe.
- **Secret Woods Shortcut (optional)** — Adds a shortcut straight from the bottom left of the map to the Secret Woods, because we're in a forest after all. The shortcut is behind a large hardwood log, same as the main entrance to the Secret Woods so progression doesn't change.
- **Forest Fish (optional)** — Uses Cindersap Forest fish spawns instead of the standard Forest Farm fishing pool.
- **Remove Shipping Bin Path (optional)** — Removes the raised dirt path from the shipping bin to the farm entrance, preserving the grass/dirt outline and leaving the bin in place.
- **Immersive Boundaries** — A small tweak, removes the fences and remodels the boundaries around the top and right of the map, to make it feel more natural and like you're nestled into an actual forest

## Requirements

- [SMAPI](https://smapi.io/)
- [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) 2.0.0+

## Optional

- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) — configure options in-game from the main menu

## Installation

1. Install SMAPI and Content Patcher if you haven't already
2. Download Better Forest Farm from [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/46969)
3. Unzip into your `Stardew Valley/Mods` folder
4. Launch the game — select the **Forest** farm type when creating a new save

## Configuration

Options can be changed in `config.json` or via Generic Mod Config Menu:

| Option                  | Default | Description                                                                 |
| ----------------------- | ------- | --------------------------------------------------------------------------- |
| `Lake`                  | `true`  | Replaces most ponds with a large lake tucked into the bottom corner          |
| `WoodsShortcut`         | `true`  | Adds a shortcut to the Secret Woods from the bottom of the farm              |
| `ForestFish`            | `false` | Uses Cindersap Forest fish spawns on the Forest Farm                          |
| `RemoveShippingBinPath` | `false` | Removes the raised path between the shipping bin and farm entrance           |

## Compatibility

- Works with all seasonal recolour mods — the map references vanilla tilesheet paths so any replacements apply automatically
- Forest farm type only; has no effect on other farm layouts
