using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace BetterJournal;

internal sealed class JournalListPage : IClickableMenu
{
    private const int RowId = 1000;
    private const int AddId = 2000;
    private const int PreviousId = 2002;
    private const int NextId = 2003;
    private const int TasksFooterHeight = 104;
    private readonly bool tasksPage;
    private readonly TaskRepository repository;
    private readonly ITranslationHelper translations;
    private readonly List<ClickableComponent> tabs;
    private readonly ClickableComponent close;
    private readonly Action<string?> edit;
    private readonly ListPosition position = new();
    private readonly List<ClickableComponent> rows = new();
    private readonly ClickableComponent add = new(Rectangle.Empty, "add");
    private readonly ClickableTextureComponent previous = new(Rectangle.Empty, Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f);
    private readonly ClickableTextureComponent next = new(Rectangle.Empty, Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f);
    private List<GuildGoal> goals = new();
    private double nextRefresh;
    private string hoverText = "";
    private int? editingRow;
    private string? editingTaskId;

    private int TaskCount => repository.Tasks?.Entries.Count ?? 0;
    private int Count => tasksPage ? TaskCount + (editingRow == TaskCount ? 1 : 0) : goals.Count;
    private int TabId => tabs[tasksPage ? 1 : 2].myID;
    private int RowTop => tasksPage ? 16 : SpriteText.getHeightOfString("Ag") + 48;
    private int RowHeight => tasksPage
        ? Math.Max((height - 32) / 6, SpriteText.getHeightOfString("Ag") + 24)
        : Math.Max(64, SpriteText.getHeightOfString("Ag") + 16);
    private int Capacity => Math.Max(1, (height - RowTop - (tasksPage ? TasksFooterHeight : 24)) / RowHeight);
    public bool IsEditing => editingRow.HasValue;
    public Rectangle EditingBounds => rows.First(row => row.myID == RowId + editingRow).bounds;
    public string? SelectedTaskId => tasksPage && position.Selected >= 0 && position.Selected < TaskCount
        ? repository.Tasks!.Entries[position.Selected].Id : null;
    public int DefaultId => Count > 0 ? RowId + position.Selected
        : tasksPage && repository.Tasks is not null ? AddId : TabId;

    public JournalListPage(bool tasksPage, TaskRepository repository, ITranslationHelper translations,
        List<ClickableComponent> tabs, ClickableComponent close, Action<string?> edit)
    {
        this.tasksPage = tasksPage;
        this.repository = repository;
        this.translations = translations;
        this.tabs = tabs;
        this.close = close;
        this.edit = edit;
        add.label = T("tasks.add");
    }

    public void Layout(Rectangle bounds)
    {
        xPositionOnScreen = bounds.X;
        yPositionOnScreen = bounds.Y;
        width = bounds.Width;
        height = bounds.Height;
        Refresh();
    }

    public void BeginEdit(string? taskId)
    {
        editingTaskId = taskId;
        editingRow = taskId is null ? TaskCount
            : repository.Tasks!.Entries.ToList().FindIndex(task => task.Id == taskId);
        position.SetItems(Count, Capacity, editingRow);
        hoverText = "";
        Rebuild();
    }

    public void EndEdit(string? preferredTask)
    {
        editingRow = null;
        editingTaskId = null;
        Refresh(preferredTask);
    }

    public void Refresh(string? preferredTask = null)
    {
        if (!tasksPage)
            goals = GuildGoal.Read();
        bool focusedRow = rows.Contains(currentlySnappedComponent);
        hoverText = "";
        int? selected = null;
        if (IsEditing)
        {
            editingRow = editingTaskId is null ? TaskCount
                : repository.Tasks!.Entries.ToList().FindIndex(task => task.Id == editingTaskId);
            selected = editingRow;
        }
        else if (preferredTask is not null && repository.Tasks is not null)
        {
            int found = repository.Tasks.Entries.ToList().FindIndex(task => task.Id == preferredTask);
            if (found >= 0)
                selected = found;
        }
        position.SetItems(Count, Capacity, selected);
        Rebuild();
        if (focusedRow)
            currentlySnappedComponent = getComponentWithID(DefaultId);
    }

    private void Rebuild()
    {
        int oldId = currentlySnappedComponent?.myID ?? RowId + position.Selected;
        rows.Clear();
        for (int i = 0; i < position.VisibleCount; i++)
        {
            int index = position.FirstVisible + i;
            rows.Add(new ClickableComponent(
                new Rectangle(xPositionOnScreen + (tasksPage ? 16 : 24), yPositionOnScreen + RowTop + i * RowHeight,
                    width - (tasksPage ? 32 : 48), RowHeight + (tasksPage ? 4 : -4)), index.ToString())
            {
                myID = RowId + index,
                upNeighborID = i == 0 ? TabId : ClickableComponent.CUSTOM_SNAP_BEHAVIOR,
                downNeighborID = i + 1 < position.VisibleCount ? ClickableComponent.CUSTOM_SNAP_BEHAVIOR
                    : tasksPage ? AddId : position.HasNext ? NextId : position.HasPrevious ? PreviousId : -1,
                leftNeighborID = position.HasPrevious ? PreviousId : -1,
                rightNeighborID = position.HasNext ? NextId : tasksPage ? AddId : -1,
                fullyImmutable = true
            });
        }

        int buttonWidth = Math.Max(192, (int)Game1.dialogueFont.MeasureString(add.label).X + 64);
        add.bounds = new Rectangle(xPositionOnScreen + (width - buttonWidth) / 2,
            yPositionOnScreen + height - TasksFooterHeight, buttonWidth, 64);
        add.myID = AddId;
        int rowId = position.Selected < 0 ? TabId : RowId + position.Selected;
        add.upNeighborID = rowId;
        add.leftNeighborID = position.HasPrevious ? PreviousId : -1;
        add.rightNeighborID = position.HasNext ? NextId : -1;

        previous.bounds = new Rectangle(xPositionOnScreen - 64, yPositionOnScreen + height - 48, 48, 44);
        previous.myID = PreviousId;
        previous.upNeighborID = rowId;
        previous.rightNeighborID = tasksPage ? AddId : rowId;
        next.bounds = new Rectangle(xPositionOnScreen + width + 16, previous.bounds.Y, 48, 44);
        next.myID = NextId;
        next.upNeighborID = rowId;
        next.leftNeighborID = tasksPage ? AddId : rowId;

        allClickableComponents = new List<ClickableComponent>(rows);
        allClickableComponents.AddRange(tabs);
        allClickableComponents.Add(close);
        if (tasksPage && repository.Tasks is not null && !IsEditing)
            allClickableComponents.Add(add);
        if (position.HasPrevious)
            allClickableComponents.Add(previous);
        if (position.HasNext)
            allClickableComponents.Add(next);
        currentlySnappedComponent = getComponentWithID(oldId)
            ?? getComponentWithID(RowId + position.Selected)
            ?? getComponentWithID(AddId) ?? getComponentWithID(TabId);
    }

    public override void populateClickableComponentList() => Rebuild();

    public override void snapToDefaultClickableComponent()
    {
        currentlySnappedComponent = getComponentWithID(DefaultId);
        if (Game1.options.SnappyMenus)
            snapCursorToCurrentSnappedComponent();
    }

    internal static Rectangle CheckboxBounds(Rectangle row) =>
        new(row.X + 32, row.Center.Y - 18, 36, 36);

    public override void snapCursorToCurrentSnappedComponent()
    {
        int index = (currentlySnappedComponent?.myID ?? -1) - RowId;
        // Keep one focus stop per task, with confirm aimed at its checkbox rather than its text.
        if (tasksPage && index >= 0 && index < Count)
            Game1.setMousePosition(CheckboxBounds(currentlySnappedComponent!.bounds).Center);
        else
            base.snapCursorToCurrentSnappedComponent();
    }

    public override void applyMovementKey(int direction)
    {
        int index = (currentlySnappedComponent?.myID ?? -1) - RowId;
        if (index >= 0 && index < Count && (direction == 0 || direction == 2))
        {
            int next = index + (direction == 0 ? -1 : 1);
            if (next >= position.FirstVisible && next < position.FirstVisible + position.VisibleCount)
            {
                position.Select(next);
                Rebuild();
                currentlySnappedComponent = getComponentWithID(RowId + next);
                snapCursorToCurrentSnappedComponent();
                return;
            }
        }
        base.applyMovementKey(direction);
        int selected = (currentlySnappedComponent?.myID ?? -1) - RowId;
        if (selected >= 0 && selected < Count)
            position.Select(selected);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        // Held confirm must not check the next row after a completed task disappears.
        if (tasksPage && Game1.input.GetGamePadState().IsButtonDown(Buttons.A) && Game1.oldPadState.IsButtonDown(Buttons.A))
            return;
        CaptureClick(x, y)?.Invoke();
    }

    internal Action? CaptureClick(int x, int y)
    {
        var row = rows.Find(row => row.bounds.Contains(x, y));
        if (row is not null)
        {
            int index = row.myID - RowId;
            if (editingRow == index)
                return null;
            if (tasksPage)
            {
                string id = repository.Tasks!.Entries[index].Id;
                bool toggle = x < row.bounds.X + 80;
                return () => ActivateTask(id, toggle);
            }
            return () =>
            {
                position.Select(index);
                Rebuild();
                currentlySnappedComponent = getComponentWithID(RowId + position.Selected);
            };
        }
        if (tasksPage && repository.Tasks is not null && !IsEditing && add.bounds.Contains(x, y))
            return () => edit(null);
        int page = position.Page;
        if (position.HasPrevious && previous.bounds.Contains(x, y))
            return () => GoToPage(page - 1);
        if (position.HasNext && next.bounds.Contains(x, y))
            return () => GoToPage(page + 1);
        return null;
    }

    private void ActivateTask(string id, bool toggle)
    {
        int index = repository.Tasks!.Entries.ToList().FindIndex(task => task.Id == id);
        if (index < 0 || (!toggle && repository.Tasks.Entries[index].CompletedOnDay.HasValue))
            return;
        position.SetItems(Count, Capacity, index);
        Rebuild();
        currentlySnappedComponent = getComponentWithID(RowId + index);
        if (toggle)
        {
            repository.Tasks.Toggle(id, Game1.Date.TotalDays);
            repository.Save();
            Game1.playSound("drumkit6");
            if (Game1.options.SnappyMenus && !Game1.lastCursorMotionWasMouse)
                snapCursorToCurrentSnappedComponent();
        }
        else
            edit(id);
    }

    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        if (!tasksPage || repository.Tasks is null)
            return;
        var row = rows.Find(row => row.bounds.Contains(x, y));
        if (row is null)
            return;
        ActivateTask(repository.Tasks.Entries[row.myID - RowId].Id, false);
    }

    public override void receiveGamePadButton(Buttons button)
    {
        if (tasksPage && repository.Tasks is not null && button == Buttons.Y)
            edit(null);
    }

    public override void receiveScrollWheelAction(int direction) => GoToPage(position.Page - Math.Sign(direction));

    private void GoToPage(int page)
    {
        if (!position.GoToPage(page))
            return;
        hoverText = "";
        Rebuild();
        Game1.playSound("shwip");
        if (Game1.options.SnappyMenus && !Game1.lastCursorMotionWasMouse)
            snapCursorToCurrentSnappedComponent();
    }

    public override void update(GameTime time)
    {
        if (tasksPage || time.TotalGameTime.TotalSeconds < nextRefresh)
            return;
        nextRefresh = time.TotalGameTime.TotalSeconds + 1;
        var updated = GuildGoal.Read();
        if (!goals.SequenceEqual(updated))
        {
            goals = updated;
            position.SetItems(Count, Capacity);
            Rebuild();
        }
    }

    public override void performHoverAction(int x, int y)
    {
        hoverText = "";
        if (IsEditing || Game1.activeClickableMenu?.GetChildMenu() is not null)
            return;
        if (position.HasPrevious)
            previous.tryHover(x, y, 0.2f);
        if (position.HasNext)
            next.tryHover(x, y, 0.2f);
        foreach (var row in rows)
        {
            int index = row.myID - RowId;
            if (!row.bounds.Contains(x, y))
                continue;
            string text = tasksPage ? repository.Tasks!.Entries[index].Text : goals[index].Name;
            int textWidth = tasksPage ? row.bounds.Width - 128 : GoalTextWidth(goals[index], row.bounds.Width);
            if (MenuDrawing.QuestText(text, textWidth) != text)
                hoverText = text;
        }
    }

    public override void draw(SpriteBatch b)
    {
        drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
            xPositionOnScreen, yPositionOnScreen, width, height, Color.White, 4f);
        if (!tasksPage)
        {
            string heading = MenuDrawing.QuestText(T("guild.heading"), width - 96);
            SpriteText.drawString(b, heading,
                xPositionOnScreen + (width - SpriteText.getWidthOfString(heading)) / 2, yPositionOnScreen + 24);
        }
        foreach (var row in rows)
        {
            int index = row.myID - RowId;
            if (tasksPage)
                drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 396, 15, 15),
                    row.bounds.X, row.bounds.Y, row.bounds.Width, row.bounds.Height, Color.White, 4f, false);
            if (editingRow == index)
                continue;
            if (tasksPage)
            {
                bool complete = repository.Tasks!.Entries[index].CompletedOnDay.HasValue;
                var checkbox = CheckboxBounds(row.bounds);
                MenuDrawing.Checkbox(b, checkbox.X, checkbox.Y, complete);
                string text = MenuDrawing.QuestText(repository.Tasks.Entries[index].Text, row.bounds.Width - 128);
                int textY = row.bounds.Y + (row.bounds.Height - SpriteText.getHeightOfString(text)) / 2;
                SpriteText.drawString(b, text, row.bounds.X + 96, textY, alpha: complete ? 0.5f : 1f);
            }
            else
            {
                var goal = goals[index];
                string text = MenuDrawing.QuestText(goal.Name, GoalTextWidth(goal, row.bounds.Width));
                string progress = GoalProgress(goal);
                int textY = row.bounds.Y + (row.bounds.Height - SpriteText.getHeightOfString(text)) / 2;
                float alpha = goal.Complete ? 0.5f : 1f;
                SpriteText.drawString(b, text, row.bounds.X + 24, textY, alpha: alpha);
                SpriteText.drawString(b, progress, row.bounds.Right - SpriteText.getWidthOfString(progress) - 24,
                    textY, alpha: alpha);
            }
        }

        if (Count == 0 && (!tasksPage || repository.Tasks is null))
        {
            string key = tasksPage ? "tasks.unreadable" : "guild.empty";
            string message = T(key);
            int availableWidth = width - 128;
            int textWidth = Math.Min(availableWidth, SpriteText.getWidthOfString(message));
            int textHeight = SpriteText.getHeightOfString(message, availableWidth);
            int contentHeight = height - (tasksPage ? TasksFooterHeight : 0);
            SpriteText.drawString(b, message, xPositionOnScreen + (width - textWidth) / 2,
                yPositionOnScreen + (contentHeight - textHeight) / 2, width: availableWidth);
        }

        if (tasksPage && repository.Tasks is not null && !IsEditing)
            MenuDrawing.Button(b, add);
        previous.texture = next.texture = Game1.mouseCursors;
        if (position.HasPrevious)
            previous.draw(b);
        if (position.HasNext)
            next.draw(b);
        if (!IsEditing && Game1.activeClickableMenu?.GetChildMenu() is null && hoverText.Length > 0)
            drawHoverText(b, hoverText, Game1.smallFont);
    }

    private string GoalProgress(GuildGoal goal) => goal.Complete
        ? translations.Get("guild.complete", new { count = goal.Count })
        : translations.Get("guild.progress", new { count = goal.Count, target = goal.Target });

    private int GoalTextWidth(GuildGoal goal, int rowWidth) => rowWidth - 72 - SpriteText.getWidthOfString(GoalProgress(goal));

    private string T(string key) => translations.Get(key);
}
