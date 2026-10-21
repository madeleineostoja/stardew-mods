using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace BetterJournal;

internal sealed class EventsPage : IClickableMenu
{
    private const int RowId = 1000;
    private const int PreviousId = 2002;
    private const int NextId = 2003;
    private readonly Func<IReadOnlyList<EventRow>> findEvents;
    private readonly List<ClickableComponent> tabs;
    private readonly ClickableComponent close;
    private readonly ListPosition position = new();
    private readonly List<ClickableComponent> rows = new();
    private readonly ClickableTextureComponent previous = new(Rectangle.Empty, Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f);
    private readonly ClickableTextureComponent next = new(Rectangle.Empty, Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f);
    private IReadOnlyList<EventRow> results = Array.Empty<EventRow>();
    private string hoverText = "";

    private int TabId => tabs.First(tab => tab.name == "events").myID;
    private int RowHeight => Math.Max(64, SpriteText.getHeightOfString("Ag") + 16);
    private int Capacity => Math.Max(1, (height - 40) / RowHeight);
    public int DefaultId => results.Count > 0 ? RowId + position.Selected : TabId;

    public EventsPage(Func<IReadOnlyList<EventRow>> findEvents, List<ClickableComponent> tabs, ClickableComponent close)
    {
        this.findEvents = findEvents;
        this.tabs = tabs;
        this.close = close;
    }

    public void Enter()
    {
        // Eligibility and localized titles are a snapshot for this visit, never a layout/tick concern.
        results = findEvents();
        hoverText = "";
        position.SetItems(results.Count, Capacity, 0);
        Rebuild();
    }

    public void Layout(Rectangle bounds)
    {
        xPositionOnScreen = bounds.X;
        yPositionOnScreen = bounds.Y;
        width = bounds.Width;
        height = bounds.Height;
        hoverText = "";
        position.SetItems(results.Count, Capacity);
        Rebuild();
    }

    private void Rebuild()
    {
        int oldId = currentlySnappedComponent?.myID ?? DefaultId;
        rows.Clear();
        for (int i = 0; i < position.VisibleCount; i++)
        {
            int index = position.FirstVisible + i;
            rows.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + 24,
                yPositionOnScreen + 16 + i * RowHeight, width - 48, RowHeight - 4), index.ToString())
            {
                myID = RowId + index,
                upNeighborID = i == 0 ? TabId : ClickableComponent.CUSTOM_SNAP_BEHAVIOR,
                downNeighborID = i + 1 < position.VisibleCount ? ClickableComponent.CUSTOM_SNAP_BEHAVIOR
                    : position.HasNext ? NextId : position.HasPrevious ? PreviousId : -1,
                leftNeighborID = position.HasPrevious ? PreviousId : -1,
                rightNeighborID = position.HasNext ? NextId : -1,
                fullyImmutable = true
            });
        }

        previous.bounds = new Rectangle(xPositionOnScreen - 64, yPositionOnScreen + height - 48, 48, 44);
        previous.myID = PreviousId;
        previous.upNeighborID = previous.rightNeighborID = DefaultId;
        next.bounds = new Rectangle(xPositionOnScreen + width + 16, previous.bounds.Y, 48, 44);
        next.myID = NextId;
        next.upNeighborID = next.leftNeighborID = DefaultId;

        allClickableComponents = new List<ClickableComponent>(rows);
        allClickableComponents.AddRange(tabs);
        allClickableComponents.Add(close);
        if (position.HasPrevious)
            allClickableComponents.Add(previous);
        if (position.HasNext)
            allClickableComponents.Add(next);
        currentlySnappedComponent = getComponentWithID(oldId) ?? getComponentWithID(DefaultId);
    }

    public override void populateClickableComponentList() => Rebuild();

    public override void snapToDefaultClickableComponent()
    {
        currentlySnappedComponent = getComponentWithID(DefaultId);
        if (Game1.options.SnappyMenus)
            snapCursorToCurrentSnappedComponent();
    }

    public override void applyMovementKey(int direction)
    {
        int index = (currentlySnappedComponent?.myID ?? -1) - RowId;
        if (index >= 0 && index < results.Count && (direction == 0 || direction == 2))
        {
            int target = index + (direction == 0 ? -1 : 1);
            if (target >= position.FirstVisible && target < position.FirstVisible + position.VisibleCount)
            {
                position.Select(target);
                Rebuild();
                currentlySnappedComponent = getComponentWithID(RowId + target);
                snapCursorToCurrentSnappedComponent();
                return;
            }
        }
        base.applyMovementKey(direction);
        int selected = (currentlySnappedComponent?.myID ?? -1) - RowId;
        if (selected >= 0 && selected < results.Count)
            position.Select(selected);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (position.HasPrevious && previous.bounds.Contains(x, y))
            GoToPage(position.Page - 1);
        else if (position.HasNext && next.bounds.Contains(x, y))
            GoToPage(position.Page + 1);
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

    public override void performHoverAction(int x, int y)
    {
        hoverText = "";
        if (position.HasPrevious)
            previous.tryHover(x, y, 0.2f);
        if (position.HasNext)
            next.tryHover(x, y, 0.2f);
        var row = rows.Find(row => row.bounds.Contains(x, y));
        if (row is not null)
        {
            string title = results[row.myID - RowId].Title;
            if (MenuDrawing.QuestText(title, row.bounds.Width - 48) != title)
                hoverText = title;
        }
    }

    public override void draw(SpriteBatch b)
    {
        drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
            xPositionOnScreen, yPositionOnScreen, width, height, Color.White, 4f);
        foreach (var row in rows)
        {
            string text = MenuDrawing.QuestText(results[row.myID - RowId].Title, row.bounds.Width - 48);
            int textY = row.bounds.Y + (row.bounds.Height - SpriteText.getHeightOfString(text)) / 2;
            SpriteText.drawString(b, text, row.bounds.X + 24, textY);
        }
        previous.texture = next.texture = Game1.mouseCursors;
        if (position.HasPrevious)
            previous.draw(b);
        if (position.HasNext)
            next.draw(b);
        if (hoverText.Length > 0)
            drawHoverText(b, hoverText, Game1.smallFont);
    }
}
