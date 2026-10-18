# Overcast

Overcast brings Stardew Valley's summer cloud shadows to spring and fall, with an option for winter too. Adjust their speed and size in every enabled season, including summer. It uses the game's own cloud art and behaviour, without adding a separate cloud layer.

## Requirements

- Stardew Valley 1.6
- SMAPI 4.0 or later

[Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) (GMCM) is optional.

## Install and remove

1. Install SMAPI.
2. Download [Overcast from Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/54073) and unzip it into Stardew Valley's `Mods` directory.
3. Start the game. Configure through GMCM, or edit the generated `config.json` while the game is closed.

To remove Overcast, delete its folder. It does not write save data. Delete `config.json` to reset all settings to their defaults.

## Native behaviour

Cloud shadows are faint, pixel-edged ground silhouettes that drift diagonally up-left. The game varies their appearance by flipping them and choosing sizes from 4 to 6.

The game checks for spawning when entering ordinary mainland outdoor locations and every ten in-game minutes while there. Spawning is random and depends on map size; shadows are not guaranteed to appear immediately. Overcast preserves the native rain checks and stops new spawning one in-game hour before the location's starting-to-get-dark time. Existing clouds drift away naturally.

Only the seasonal restriction in native cloud spawning is extended. Locations that override critter spawning, such as Ginger Island, do not acquire cloud shadows through a separate spawning system. Location-specific seasons are respected.

## Configuration

Unsafe numeric values are clamped to the supported ranges. Non-finite speed values reset to `1`. An inverted size range is repaired by raising the maximum to the minimum.

| Option | Default | Description |
| --- | --- | --- |
| `Enabled` | `true` | Enable the seasonal extension and tuning. Off removes Overcast's changes; existing clouds are not forcibly removed. |
| `EnableInSpring` | `true` | Allow native clouds in spring. |
| `EnableInFall` | `true` | Allow native clouds in fall. |
| `EnableInWinter` | `false` | Allow cloud shadows on clear winter days. |
| `SpeedMultiplier` | `1.0` | Cloud speed, from `0` (still) to `2` (twice as fast). `1` is the usual speed. |
| `MinSize` | `4` | Smallest clouds that can appear. Range: `2`–`8`; lower values make smaller clouds. |
| `MaxSize` | `6` | Largest clouds that can appear. Range: `2`–`8`, and at least `MinSize`. Match the smallest size for equally sized clouds. |

GMCM groups speed and size under **Cloud appearance** and saves through SMAPI. Speed applies to existing clouds immediately; size settings apply when new clouds spawn. Disabling a season stops Overcast's extension and tuning there, but leaves existing clouds and spawning independently enabled by other mods alone. Summer always uses your settings while Overcast is enabled.

## Weather Wonders compatibility

Weather Wonders (`Kana.WeatherWonders`) is optional. Its Cloudy Skies framework's optional API is used to respect explicit weather-specific cloud-spawning settings; neither mod is a required dependency. In every enabled season, including summer:

- **Cloudy:** shadows are half as dark as usual.
- **Heatwave:** its explicit Cloudy Skies cloud-spawning setting is respected; Weather Wonders disables shadows by default.
- **Drizzle, Deluge, Acid Rain, Muddy Rain, Mist, Blizzard, Rain/Snow Mix, Dry Lightning, Hailstorm, and Sandstorm:** no new cloud shadows spawn.
- **Unknown weather:** native weather checks remain in charge, and explicit Cloudy Skies cloud-spawning vetoes are respected.

No weather-dependent speed, density, or wind simulation is added. Existing clouds are not forcibly removed when the weather changes.

## Compatibility and performance

Overcast patches native cloud spawning, movement, and drawing through SMAPI's Harmony support. The seasonal change is limited to cloud spawning; it does not change the game's season or the behaviour of other critters. Native cloud art and its drawing layer remain in use, including texture recolours supplied by other mods.

There are no custom textures, update/render event loops, save data, or additional cloud populations. Other mods that patch the same native cloud methods may conflict. If the expected game hooks cannot be applied when Overcast loads, it logs an error and removes its patches, leaving normal cloud behaviour intact.
