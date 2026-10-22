using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewValley.Quests;

namespace BetterJournal;

internal sealed class EventsPage : QuestLog
{
    private readonly Func<IReadOnlyList<EventRow>> findEvents;
    private readonly List<ClickableComponent> tabs;
    private List<IQuest> eventQuests = new();

    private int TabId => tabs.First(tab => tab.name == "events").myID;
    public bool ShowingDetails => questPage >= 0;
    public int DefaultId => ShowingDetails ? region_backButton : pages[currentPage].Count > 0 ? 0 : TabId;

    public EventsPage(Func<IReadOnlyList<EventRow>> findEvents, List<ClickableComponent> tabs, ClickableTextureComponent close)
    {
        this.findEvents = findEvents;
        this.tabs = tabs;
        upperRightCloseButton = close;
    }

    // QuestLog calls this virtually during construction; never enumerate or alter the player's real quests.
    protected override IList<IQuest> GetAllQuests() => eventQuests ?? new List<IQuest>();

    public void Enter()
    {
        eventQuests = findEvents()
            .Select(row => (IQuest)new EventQuest(row, () => width - (ShowingDetails ? 64 : 136)))
            .ToList();
        currentPage = 0;
        paginateQuests();
        scrollAmount = 0;
        Layout();
    }

    public void Layout()
    {
        if (ShowingDetails)
        {
            // QuestLog only measures content inside its objective loop; these read-only entries have none.
            string text = Game1.parseText(_shownQuest.GetDescription(), Game1.dialogueFont, width - 128);
            _contentHeight = Game1.dialogueFont.MeasureString(text).Y + 32;
            _scissorRectHeight = height - 128;
            scrollAmount = Math.Clamp(scrollAmount, 0, Math.Max(0, _contentHeight - _scissorRectHeight));
            scrollBar.bounds.Y = scrollBarBounds.Y + (NeedsScroll()
                ? (int)((scrollBarBounds.Height - scrollBar.bounds.Height) * scrollAmount / (_contentHeight - _scissorRectHeight))
                : 0);
        }
        populateClickableComponentList();
        questLogButtons[0].upNeighborID = TabId;
        backButton.upNeighborID = TabId;
        backButton.rightNeighborID = ShowingDetails ? -1 : 0;
        forwardButton.upNeighborID = TabId;
    }

    public override void populateClickableComponentList()
    {
        // QuestLog initializes controller focus before the derived constructor receives the tabs.
        if (tabs is null)
        {
            base.populateClickableComponentList();
            return;
        }
        allClickableComponents = new List<ClickableComponent>();
        if (ShowingDetails)
        {
            allClickableComponents.Add(backButton);
        }
        else
        {
            allClickableComponents.AddRange(questLogButtons.Take(pages[currentPage].Count));
            // QuestLog resolves Back while opening a detail page, before our next layout pass.
            allClickableComponents.Add(backButton);
            allClickableComponents.Add(forwardButton);
        }
        allClickableComponents.AddRange(tabs);
        allClickableComponents.Add(upperRightCloseButton);
        if (!allClickableComponents.Contains(currentlySnappedComponent))
            currentlySnappedComponent = getComponentWithID(DefaultId);
    }

    public override void snapToDefaultClickableComponent()
    {
        if (tabs is null)
        {
            base.snapToDefaultClickableComponent();
            return;
        }
        currentlySnappedComponent = getComponentWithID(DefaultId);
        if (Game1.options.SnappyMenus)
            snapCursorToCurrentSnappedComponent();
    }

    public void Back()
    {
        if (!ShowingDetails)
            return;
        exitQuestPage();
        scrollAmount = 0;
        Layout();
        if (Game1.options.SnappyMenus)
            snapToDefaultClickableComponent();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (ShowingDetails && backButton.containsPoint(x, y))
        {
            Back();
            return;
        }
        base.receiveLeftClick(x, y, playSound);
        Layout();
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (ShowingDetails)
        {
            base.receiveScrollWheelAction(direction);
            return;
        }

        int page = Math.Clamp(currentPage - Math.Sign(direction), 0, pages.Count - 1);
        if (page == currentPage)
            return;
        currentPage = page;
        Layout();
        Game1.playSound("shwip");
        if (Game1.options.SnappyMenus)
            snapToDefaultClickableComponent();
    }

    public override void draw(SpriteBatch b)
    {
        foreach (var button in new[] { backButton, forwardButton, upArrow, downArrow, scrollBar })
            button.texture = Game1.mouseCursors;
        if (ShowingDetails)
        {
            base.draw(b);
            return;
        }

        // Hearts identify event hints without implying quest status or completion.
        if (!Game1.options.showClearBackgrounds)
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
        drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18),
            xPositionOnScreen, yPositionOnScreen, width, height, Color.White, 4f);
        for (int i = 0; i < pages[currentPage].Count; i++)
        {
            var row = questLogButtons[i];
            drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 396, 15, 15),
                row.bounds.X, row.bounds.Y, row.bounds.Width, row.bounds.Height,
                row.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()) ? Color.Wheat : Color.White, 4f, false);
            b.Draw(Game1.mouseCursors, new Vector2(row.bounds.X + 32, row.bounds.Center.Y - 12),
                new Rectangle(211, 428, 7, 6), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.99f);
            string text = pages[currentPage][i].GetName();
            SpriteText.drawString(b, text, row.bounds.X + 80,
                row.bounds.Y + (row.bounds.Height - SpriteText.getHeightOfString(text)) / 2);
        }
        if (currentPage > 0)
            backButton.draw(b);
        if (currentPage + 1 < pages.Count)
            forwardButton.draw(b);
        upperRightCloseButton.draw(b);
    }

    // Native list/detail rendering without putting synthetic quests into the save or exposing actions.
    private sealed class EventQuest : IQuest
    {
        private readonly EventRow row;
        private readonly Func<int> nameWidth;

        public EventQuest(EventRow row, Func<int> nameWidth)
        {
            this.row = row;
            this.nameWidth = nameWidth;
        }

        public string GetName() => MenuDrawing.QuestText(row.Title, nameWidth());
        public string GetDescription() => string.Join("\n\n", row.Details);
        public List<string> GetObjectiveDescriptions() => new();
        public bool CanBeCancelled() => false;
        public void MarkAsViewed() { }
        public bool ShouldDisplayAsNew() => false;
        public bool ShouldDisplayAsComplete() => false;
        public bool IsTimedQuest() => false;
        public int GetDaysLeft() => 0;
        public bool IsHidden() => false;
        public bool HasReward() => false;
        public bool HasMoneyReward() => false;
        public int GetMoneyReward() => 0;
        public void OnMoneyRewardClaimed() { }
        public bool OnLeaveQuestPage() => false;
    }
}
