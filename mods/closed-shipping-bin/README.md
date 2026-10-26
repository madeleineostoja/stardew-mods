# Closed Shipping Bin

Keeps full-size shipping bin lids closed instead of opening when players walk nearby. This also prevents the proximity-triggered opening and closing sounds.

Opening a bin's shipping menu or depositing an item enables its vanilla lid animation and sounds until everyone walks away. Once it closes, walking nearby won't open it again. The shipping menu can pause the lid animation until dismissed.

Shipping items, the shipping menu, and item-toss effects are unchanged. Applies to the starting shipping bin and additional full-size bins; mini-shipping bins are unaffected.

## Requirements

- Stardew Valley 1.6
- SMAPI 4.0 or later

## Install and remove

Unzip the mod into Stardew Valley's `Mods` directory and start the game through SMAPI. There are no settings or additional dependencies.

To remove it, delete the `ClosedShippingBin` folder and restart the game. The mod does not write save data.

## Compatibility

Uses Harmony patches to allow full-size shipping bin lids to open only after interaction, resetting when vanilla requests closing. Native bin art, including texture replacements, remains in use. Mods that replace shipping bin behaviour or patch the same method may conflict.

In multiplayer, install on each player's computer that should have closed lids.
