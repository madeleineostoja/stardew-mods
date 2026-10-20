# Configurable Rumble

Choose which vanilla actions vibrate your controller. Fishing reeling and horse riding are off by default; all other effects are enabled.

## Requirements and installation

Requires Stardew Valley 1.6.15 and SMAPI 4.0 or later. Download [Configurable Rumble from Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/54193) and extract the `ConfigurableRumble` folder into `Stardew Valley/Mods`. Generic Mod Config Menu is optional.

The game's own **Controller Rumble** setting must be enabled. This mod only filters existing vibration; it does not add rumble to actions that don't already have it.

## Configuration

Use Generic Mod Config Menu, or edit the generated `config.json` while the game is closed:

```json
{
  "FishingBites": true,
  "FishingCasts": true,
  "FishingReeling": false,
  "ToolUse": true,
  "Combat": true,
  "Explosions": true,
  "HorseRiding": false,
  "DoorsAndTransitions": true
}
```

These defaults apply to new configs and Generic Mod Config Menu resets; existing saved settings are preserved. Set a toggle to `false` to silence that activity:

| Toggle | Vanilla effects |
| --- | --- |
| Fishing bites | Bite alerts, including Auto-Hook |
| Fishing casts | Casting the rod |
| Fishing reeling | Ongoing vibration while the fish is inside the catching bar |
| Tool use | Heavy-hitting tools, excluding melee weapons |
| Combat | Hitting monsters, taking damage, parrying, and health-based knockouts |
| Explosions | Bomb blasts in the current location, including multiplayer explosions |
| Horse riding | Hoofbeats on every surface |
| Doors & transitions | Building entrances, NPC room doors, theater entry, and movie screenings |

Fishing settings are independent: you can keep bite alerts while silencing casts and reeling. Changes in Generic Mod Config Menu affect subsequent rumble requests; an existing pulse may finish.

Only the vibration calls are filtered. Damage, tool actions, fishing mechanics, sounds, animations, and location changes are unchanged. Vanilla vibration updates and stop requests are never blocked.

## Compatibility

Settings apply locally; other players do not need this mod. Direct vibration requests from other mods are not filtered, though modded content using these vanilla actions follows the same toggles. The game's controller support, global rumble setting, and split-screen controller routing remain in control.

The patches check the expected vanilla call counts. If a patch cannot be applied, the mod removes its patches and logs an error instead of leaving a partially configured set of effects. Mods replacing these same methods may conflict.

## Build and verify

From the repository root:

```sh
python3 -m unittest discover -s tests -v
python3 scripts/mods.py validate
python3 scripts/mods.py build configurable-rumble --game-path "/path/to/Stardew Valley"
```

For in-game testing, run `./build-local.sh` and fully restart the game. Check that each toggle mutes only its activity, especially keeping fishing bites while disabling reeling. Check normal and Auto-Hook bites, casting, tool use, monster hits and player damage, a parry and knockout, explosions, horse footsteps, and door/movie transitions. Confirm the global rumble setting still silences everything, and repeat explosion and local-controller checks in multiplayer or split-screen.
