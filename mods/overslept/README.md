# Overslept

Sleep in after a 2am collapse instead of losing energy and money.

## Requirements and installation

Requires Stardew Valley 1.6 and SMAPI 4.0 or later. Download [Overslept from Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/54074) and extract the `Overslept` folder into `Stardew Valley/Mods`. Generic Mod Config Menu is optional.

**Single-player only.** Overslept leaves multiplayer wakeup times, energy recovery, rescue charges, and notices unchanged.

## Behaviour

- Collapse at 2am and wake at 10am the following day by default, with a frustrated emote after sleeping in.
- Gaining a vanilla skill level that day keeps the 6am wakeup and normal level-up menus.
- By default, the 2am collapse has no late-night energy penalty or rescue charge. The associated rescue notice is not generated.
- Normal bedtimes, earlier exhaustion collapses, and combat knockouts are unchanged.
- Exhaustion's separate recovery penalty remains intact. Overslept does not force full stamina.
- Vanilla relocation to bed and other overnight processing are preserved.

The skipped morning uses normal ten-minute clock updates for machines, locations, and other timed systems. Other NPCs are moved along their scheduled routes instead of simulating every walking frame, so their positions are approximate. Spouses (including roommates) are never teleported by catch-up; their normal movement and marriage behaviour remain in control. Routes beginning at the wakeup time are allowed to play normally.

This runs only when an eligible morning starts, not every frame. The default four-hour skip processes 24 clock ticks. Mods which react to time changes will also see those ticks. Compatibility with other sleep or time-changing mods is not guaranteed.

The intended wakeup is stored with the morning save, so reloading that morning applies the same skip. Removing Overslept restores vanilla 6am loading; it does not refund penalties from earlier days.

## Configuration

Edit the generated `config.json`, or use Generic Mod Config Menu:

```json
{
  "WakeUpTime": 1000,
  "DisableEnergyPenalty": true,
  "DisableMoneyLoss": true
}
```

- **WakeUpTime:** HHMM in ten-minute increments, from `600` (6am) to `2550` (1:50am the following night). Invalid values fall back to `1000`. Levelling up always keeps 6am.
- **DisableEnergyPenalty:** remove only the late-night energy penalty from a 2am collapse.
- **DisableMoneyLoss:** prevent both the rescue charge and its associated notice. Set to `false` to retain both.

The penalty toggles are independent of the level-up wakeup exception.

## Build and verify

From the repository root:

```sh
python3 -m unittest discover -s tests -v
python3 scripts/mods.py validate
python3 scripts/mods.py build overslept --game-path "/path/to/Stardew Valley"
```

In-game checks: collapse outdoors at 2am with and without a pending skill level; repeat with each penalty toggle disabled; check an already-exhausted player, a normal bedtime, an earlier collapse, and a combat knockout. Check machine progress, NPC cross-map routes, a spouse/roommate, reload the morning save, and enter multiplayer to confirm vanilla behaviour.
