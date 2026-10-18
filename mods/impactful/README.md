# Impactful

Subtle camera feedback for the moments that should feel weighty in Stardew Valley. Short, directional kicks ease into a gentle rebound without turning everyday play into constant screen shake.

Only the world view moves—your HUD, menus, cursor, and gameplay controls stay unchanged.

## Effects

- **Large rock breaks:** feedback when a pickaxe destroys a boulder, meteorite, or other large mineral clump. Ordinary rocks and ore nodes do not shake the camera.
- **Tree landings:** a small kick when a mature wild tree you chop down hits the ground.
- **Hardwood breaks:** a smaller kick when your axe destroys a large hardwood stump or log.
- **Parries:** feedback when you successfully block a monster with a defensive sword. Hammer specials keep their vanilla shake.
- **Player damage:** feedback when you actually take damage.
- **Explosions:** distance-based feedback for nearby bombs, with stronger effects for larger blasts.

Bombs produce one explosion cue rather than stacking it with a separate damage shake when you get hurt. If explosion feedback is disabled, bomb damage still uses the player-damage setting. Explosion kicks wait until the vanilla screen flash fades enough to reveal them.

Individual tool swings, ordinary melee hits, fruit trees, ordinary tree-stump removal, grass cutting, and hoeing do not add camera feedback. Impactful does not pause combat or add particles, flashes, or controller rumble.

## Installation

Requires **Stardew Valley 1.6.15+** and **SMAPI 4.4+**.

1. Download Impactful from [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/53679).
2. Extract the `Impactful` folder into your game's `Mods` folder.
3. Launch the game through SMAPI.

[Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) is optional and lets you adjust settings in-game.

## Configuration

Adjust settings through Generic Mod Config Menu, or edit `config.json` after launching the mod once. Restart the game after editing the file directly.

```json
{
  "EnableScreenShake": true,
  "ShakeStrength": 100,
  "Mining": true,
  "Combat": true,
  "PlayerDamage": true,
  "Explosions": true,
  "Trees": true
}
```

- **EnableScreenShake:** turn all Impactful feedback on or off.
- **ShakeStrength:** the in-game slider runs from `0.00` to `2.00` in steps of `0.01`. `1.00` is normal strength, `0.50` is half strength, and `0.00` disables feedback. The config file stores percentages (`100` means `1.00` in the menu).
- **Mining, Combat, PlayerDamage, Explosions, Trees:** toggle large rock breaks, parries, damage, explosions, and tree feedback independently. Trees covers both tree landings and hardwood stump/log breaks.

## Multiplayer and compatibility

Other players' mining, parries, and tree chopping do not shake your camera. In split screen, each local view has its own feedback.

For shared explosion feedback, install Impactful on the host and each client that wants the effect. The host can relay explosions even with its own feedback disabled. Other effects do not require the host to install Impactful.

Menus, dialogue, and minigames suppress camera feedback. Mods that change the camera during world rendering may conflict with Impactful.
