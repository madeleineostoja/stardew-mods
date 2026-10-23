# Better Journal

A small, native Stardew Valley journal: vanilla quests, a personal checklist, Adventurer's Guild monster eradication progress, and spoiler-light event hints. Open it with the normal journal button or HUD icon. The centered Journal scroll sits above a separate row of vanilla-sized tabs.

## Requirements

- Stardew Valley 1.6.15 or later with SMAPI 4.1.10 or later.
- No UI framework or Content Patcher dependency.

Extract the `BetterJournal` folder into `Stardew Valley/Mods`. [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/54230).

## Pages

- **Quests:** the existing vanilla quest menu, including details, cancellation, special orders, and reward claiming.
- **Tasks:** add, rename, check/uncheck, and delete personal reminders. Unfinished tasks carry over. Checked tasks show their completed state for about 650 milliseconds, then disappear. Uncheck during that pause to undo.
- **Guild:** read-only monster eradication goals, shown only after this character has entered the Adventurer's Guild and while enabled in config. Counts and goal definitions come from the game, including goals added through its data assets. Monster groups stay unnamed as `????` until the first kill, matching the Guild board. The page uses the game's native journal font and a plain heading, with names on the left and counts on the right. Completed goals dim the whole row and show only the kill total, without checkboxes, progress fractions, or hover outlines. Rewards must still be collected from Gil.
- **Events:** hints for unlocked, unseen location events, behind the filled heart tab after Tasks and before the optional Guild tab. Framed rows use a small heart instead of a quest status icon and say **Visit [location]**, without counts. Click a row to open its full, wrapped trigger instructions and event counts in the native quest detail view, then use the back arrow to return. There are no event-row tooltips, checkboxes, cancellation, rewards, or automatic event launching. No results leaves a blank page; the tab remains available.

Tasks have no automatic completion, recurrence, scheduling, groups, colours, notes, or overlays. Each task is a single line of up to 160 characters. Long titles are shortened in the quest-style list; hover or edit a task to read its full text.

## Event hints and refresh

Entering the Events tab looks up unseen events for the current player. Unlock requirements such as friendship, prerequisite events, mail, items, money, skills, elapsed days/years, the current season, story progress, and known location unlocks must currently pass. Events restricted to another season stay hidden. Visit requirements—time, weather, weekday, day of month, festival timing, and NPC presence/availability—do not hide an unlocked event. They appear as instructions in its expanded description, including negated requirements. An event can remain listed while you wait for the right time, weather, or NPC schedule. Seasonal restrictions still appear in the instructions as context even though the current season must already match.

Each tab visit is a snapshot: drawing, resizing, page turns, ticks, and content invalidation do not update the displayed results. Switch to another tab and back to refresh unlocked events and localized descriptions after friendship, seen status, or other unlock requirements change. Clicking the already-selected Events tab does not refresh. Closing the journal discards the menu's results; only parsed event definitions are cached per screen, with invalidation on event/location data changes, language changes, and returning to title. Disabled Events performs no lookup and constructs no Events page.

List titles contain only the destination. The expanded description starts with a friendly instruction such as “Visit Pierre's General Store any day except Saturday, while Abigail is there, to see an event.” Calendar, positive time, a single weather restriction, and required NPC presence flow into the visit sentence with commas, not a chain of “and” clauses. More complex weather, festival, absence, and other restrictions use descriptive availability sentences such as “You can see this event before noon” or “You can see these events when Leah isn’t there,” without additional commands. Events with identical visit requirements share one instruction ending in “to see 2 events.” Different requirements remain separate paragraphs, introduced as “You can see another event …” without repeating the destination. Alternative headers for the same event remain alternative routes and count only once. Each route has its own schedule and requirement sentences, with “Another option is a visit …” separating routes so their constraints cannot be mistaken for one combined set. Calendar exclusions are simplified to allowed days/seasons when shorter, such as “on Sunday in Winter”. Times use the game's localized formatter, weather and calendar restrictions are described explicitly, and NPC wording distinguishes being at the destination from being available elsewhere. No plot summary or extra description heading is added. Real cutscenes at the player's farm remain listed as **Return to your farm** rather than suggesting that the farm is a separate place to visit. Town hints use **the Town Square**. Destination-specific labels from the vanilla world map take precedence over generic location display names. Locations without a specific map label use localized friendly names for known vanilla IDs, then game-provided display names and a readable fallback for modded IDs. Instructions are generated in the current language, not plot summaries or event-ID descriptions.

Vanilla events and modded events supplied through the game's standard location/event data both participate. This is intentionally not an exhaustive catalogue: every condition in a header must resolve to an allowed, unmodified vanilla handler. Only unlock conditions are evaluated against the current player and destination; visit conditions are parsed into instructions without invoking their handlers. Unknown, replaced, unsafe, or unsupported conditions exclude that header, even when negated. Supported coverage is limited to audited state checks such as friendship, seen events, mail, items, progression, calendar, weather, time, and NPC visibility; script-based prerequisites are not covered. No actors or ownership are inferred from scripts.

Events at the same destination share a row, and different destinations remain separate even if they have the same display name. Event counts appear in the description only. Identical complete sets of alternative visit instructions share a count; different triggers remain separate rather than combining their conditions or implying that they can all happen on one visit. Long descriptions wrap and scroll using the native quest menu.

Known vanilla progression locks are filtered, including the early mine landslide, Willy's back-room invitation, Caroline's two-heart sunroom door, Ginger Island access and its locked farmhouse/Walnut Room, the Railroad/Spa, Desert destinations, the Sewers, and the Witch's areas. This is not pathfinding or a universal modded-door check: opening hours, unhandled locks, changing unlock requirements, and competing-event priority can still affect what actually plays. Unknown mod locations are not hidden merely because their access rules are unknown.

## Controls

Mouse:

- Click an icon tab to change pages; hover to see its name.
- Click the centered **Add Task** button to start an inline draft.
- Click a task's checkbox to check or uncheck it.
- Click task text or right-click a row to edit it in place. Use the green confirm icon to save. The single red button deletes an existing task with confirmation, or discards an unsaved draft.
- Use the previous/next arrows or the mouse wheel to turn pages in long Tasks, Guild, and Events lists.
- Click an Events row to read its trigger instructions. Use its back arrow to return; the mouse wheel scrolls long event details without refreshing the snapshot.

Controller:

- **LT / RT:** change tabs.
- **A:** activate the focused control or toggle a task.
- **X:** edit the task under the cursor.
- **Y:** add a task from the Tasks page.
- **B:** go back or cancel; event details return to their list, and quest details retain vanilla back behaviour.
- The native on-screen keyboard handles task text entry. **Start** also saves an inline edit.

Button names follow Stardew's PC controller mapping. With **Use Controller-Style Menus** enabled, directional navigation snaps to task checkboxes or read-only rows; move left or right to reach the page arrows and up from the first row to reach tabs. Activate an Events row to read its full instructions; there are no row tooltips. An empty Events page focuses its tab, with the close button reachable to the right. With it disabled, the normal controller-driven cursor remains available. Better Journal does not change the game's input settings. Every action also has a clickable control.

## Configuration

SMAPI creates `config.json` in the installed mod folder:

```json
{
  "ShowTasks": true,
  "ShowMonsterGoals": true,
  "ShowEvents": true
}
```

Set `ShowTasks` to `false` to hide the Tasks tab without deleting saved tasks. Set `ShowMonsterGoals` to `false` to remove the monster goals tab. When enabled, the tab remains hidden until the current character has entered the Adventurer's Guild. Set `ShowEvents` to `false` to remove the Events tab and disable event lookup. All three default to `true`; changes apply when the journal is reopened.

All three options are available in Generic Mod Config Menu if installed; GMCM is optional.

## Saves and multiplayer

Inline drafts are not saved until confirmed. Enter or the confirm icon commits the text; Escape or B discards unsaved edits. Clicking outside the editing row or switching tabs cancels the draft before performing that action. Cancelling the on-screen keyboard leaves the draft open; cancelling again discards it.

Closing the journal also clears checked tasks, so they do not reappear if it is closed during the completion pause.

Tasks are stored in the current farmer's `modData` under `heyitsmads.BetterJournal/Tasks` and saved with the game. Restarting an unsaved day rolls back task edits along with the rest of that day. Players and local split-screen characters keep separate lists; no shared checklist or multiplayer messaging is required.

If saved task data cannot be read, editing is disabled and the original data is left untouched. Check the SMAPI log rather than deleting save data.

Event hints are not saved and remain separate for each current player, including local split-screen characters. Looking up hints does not launch events, move players, mark events seen, or grant mail or rewards.

Removing the mod leaves the game and quests intact. Its task data remains in the save for reinstallation.

## UI recolors and other menus

The interface draws directly from the game's live menu, cursor, checkbox, paper, combat-icon, filled-heart, standard button, and font assets. There is no bundled UI artwork or custom palette. Recolors that replace those assets while preserving their sprite layouts carry through without a Better Journal-specific patch.

Structural menu overhauls and Deluxe Journal-specific skins are not automatically compatible. Better Journal only wraps the standard `QuestLog`; it leaves subclasses and replacement quest menus installed by other mods alone. Avoid installing multiple mods that replace the journal.

## Development

```sh
python3 -m unittest discover -s tests -v
python3 scripts/mods.py validate
python3 scripts/mods.py build better-journal --game-path "/path/to/Stardew Valley"
```

For local deployment, run `./build-local.sh` from the repository root, then fully restart the game.

In-game Events verification should use a disposable save and temporary, unshipped content fixture: check default/disabled and optional GMCM behaviour; supported versus unsafe/replaced headers without mail, seen, movement, or event side effects; unmet unlock requirements hiding events while unmet visit requirements remain described; a data-added event/custom destination; switching away/back after friendship or seen changes; snapshot stability during content invalidation, resizing, and pagination; count-free destination titles, counts and alternative triggers in the descriptions, native framed rows, wrapped/scrollable details and back navigation without tooltips; known progression locks and friendly location names; blank results; both Guild states and controller settings; page arrows, LT/RT, directional focus, and close; and distinct split-screen player contexts. Test at the supported minimum viewport (704 × 480) as well as a larger window. Do not change a real save to force eligibility.

Existing-page verification should cover quest reward claiming and returning to an open quest detail page; adding with Y; typing menu-binding letters such as E into a task; cancelling the native keyboard and deletion confirmation; multiple pages, partial last pages, and page-arrow focus; clicking existing tasks or page arrows while cancelling a draft; both controller-style-menu settings; split-screen ownership; delayed completion, undo, and reload; completion while another row is being edited; resizing with a confirmation open; a UI recolor; and the monster tab with membership and config enabled/disabled.

Inspired by [Deluxe Journal](https://github.com/kauf0/DeluxeJournal), with a deliberately smaller manual-checklist scope.
