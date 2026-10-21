# Better Journal

A small, native Stardew Valley journal: vanilla quests, a personal checklist, and Adventurer's Guild monster eradication progress. Open it with the normal journal button or HUD icon.

## Requirements

- Stardew Valley 1.6.15 or later with SMAPI 4.1.10 or later.
- No UI framework or Content Patcher dependency.

Extract the `BetterJournal` folder into `Stardew Valley/Mods`. [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/54230).

## Pages

- **Quests:** the existing vanilla quest menu, including details, cancellation, special orders, and reward claiming.
- **Tasks:** add, rename, check/uncheck, and delete personal reminders. Unfinished tasks carry over. Checked tasks show their completed state for about 650 milliseconds, then disappear. Uncheck during that pause to undo.
- **Guild:** read-only monster eradication goals, shown only after this character has entered the Adventurer's Guild and while enabled in config. Counts and goal definitions come from the game, including goals added through its data assets. Monster groups stay unnamed as `????` until the first kill, matching the Guild board. The page uses the game's native journal font and a plain heading, with names on the left and counts on the right. Completed goals dim the whole row and show only the kill total, without checkboxes, progress fractions, or hover outlines. Rewards must still be collected from Gil.

Tasks have no automatic completion, recurrence, scheduling, groups, colours, notes, or overlays. Each task is a single line of up to 160 characters. Long titles are shortened in the quest-style list; hover or edit a task to read its full text.

## Event hint data

The event-hint lookup generates spoiler-light location-entry instructions for unseen events whose supported conditions currently pass. It does not activate an Events tab. Hints use the destination's live display name, the intersection of positive time windows, and NPC names only when a positive condition requires those NPCs to be visible at that destination. Times use the game's localized formatter; negated time requirements are not worded. Other supported prerequisites still gate eligibility without appearing in the hint. Titles are generated in the current language, not plot summaries or event-ID descriptions.

Vanilla events and modded events supplied through the game's standard location/event data both participate. This is intentionally not an exhaustive catalogue: every condition in a header must resolve to an allowed, unmodified vanilla handler and pass for the current player and destination. Unknown, replaced, unsafe, or unsupported conditions exclude that header, even when negated. Supported coverage is limited to audited state checks such as seen events, mail, items, progression, calendar, weather, time, and NPC visibility; friendship and script-based prerequisites are not covered. No actors or ownership are inferred from scripts.

Identical full single-line hints share a row, with a count of distinct destination/event pairs; alternative matching headers do not inflate counts. Long hints retain their full text for hover disclosure rather than merging by their shortened drawing text. A passing hint does not guarantee that the destination is accessible, that travel will leave its conditions true, or that this event has launch priority over other events. The game remains responsible for deciding what actually plays.

## Controls

Mouse:

- Click an icon tab to change pages; hover to see its name.
- Click the centered **Add Task** button to start an inline draft.
- Click a task's checkbox to check or uncheck it.
- Click task text or right-click a row to edit it in place. Use the green confirm icon to save. The single red button deletes an existing task with confirmation, or discards an unsaved draft.
- Use the bottom previous/next arrows or the mouse wheel to turn pages in long task and monster-goal lists.

Controller:

- **LT / RT:** change tabs.
- **A:** activate the focused control or toggle a task.
- **X:** edit the task under the cursor.
- **Y:** add a task from the Tasks page.
- **B:** go back or cancel; on the quest detail page, this retains vanilla back behaviour.
- The native on-screen keyboard handles task text entry. **Start** also saves an inline edit.

Button names follow Stardew's PC controller mapping. With **Use Controller-Style Menus** enabled, directional navigation snaps to task checkboxes; move left or right to reach the page arrows. With it disabled, the normal controller-driven cursor remains available. Better Journal does not change the game's input settings. Every action also has a clickable control.

## Configuration

SMAPI creates `config.json` in the installed mod folder:

```json
{
  "ShowMonsterGoals": true
}
```

Set `ShowMonsterGoals` to `false` to remove the monster goals tab. When enabled, the tab remains hidden until the current character has entered the Adventurer's Guild. Changes apply when the journal is opened.

The same option is available in Generic Mod Config Menu if installed; GMCM is optional.

## Saves and multiplayer

Inline drafts are not saved until confirmed. Enter or the confirm icon commits the text; Escape or B discards unsaved edits. Clicking outside the editing row or switching tabs cancels the draft before performing that action. Cancelling the on-screen keyboard leaves the draft open; cancelling again discards it.

Closing the journal also clears checked tasks, so they do not reappear if it is closed during the completion pause.

Tasks are stored in the current farmer's `modData` under `heyitsmads.BetterJournal/Tasks` and saved with the game. Restarting an unsaved day rolls back task edits along with the rest of that day. Players and local split-screen characters keep separate lists; no shared checklist or multiplayer messaging is required.

If saved task data cannot be read, editing is disabled and the original data is left untouched. Check the SMAPI log rather than deleting save data.

Removing the mod leaves the game and quests intact. Its task data remains in the save for reinstallation.

## UI recolors and other menus

The interface draws directly from the game's live menu, cursor, checkbox, paper, combat-icon, standard button, and font assets. There is no bundled UI artwork or custom palette. Recolors that replace those assets while preserving their sprite layouts carry through without a Better Journal-specific patch.

Structural menu overhauls and Deluxe Journal-specific skins are not automatically compatible. Better Journal only wraps the standard `QuestLog`; it leaves subclasses and replacement quest menus installed by other mods alone. Avoid installing multiple mods that replace the journal.

## Development

```sh
python3 -m unittest discover -s tests -v
python3 scripts/mods.py validate
python3 scripts/mods.py build better-journal --game-path "/path/to/Stardew Valley"
```

For local deployment, run `./build-local.sh` from the repository root, then fully restart the game.

In-game verification should cover quest reward claiming and returning to an open quest detail page; adding with Y; typing menu-binding letters such as E into a task; cancelling the native keyboard and deletion confirmation; multiple pages, partial last pages, and page-arrow focus; clicking existing tasks or page arrows while cancelling a draft; both controller-style-menu settings; split-screen ownership; delayed completion, undo, and reload; completion while another row is being edited; resizing with a confirmation open; a UI recolor; and the monster tab with membership and config enabled/disabled.

Inspired by [Deluxe Journal](https://github.com/kauf0/DeluxeJournal), with a deliberately smaller manual-checklist scope.
