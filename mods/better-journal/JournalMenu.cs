using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace BetterJournal;

internal sealed class JournalMenu : IClickableMenu, IDisposable
{
    private const int TabId = 5000;
    private const int HeaderHeight = 128;
    private static readonly RasterizerState HeaderClip = new() { ScissorTestEnable = true };
    private static readonly FieldInfo QuestPage = typeof(QuestLog).GetField("questPage", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private readonly QuestLog quests;
    private readonly TaskRepository repository;
    private readonly ITranslationHelper translations;
    private readonly List<ClickableComponent> tabs = new();
    private readonly JournalListPage tasks;
    private readonly JournalListPage? guild;
    private readonly EventsPage? events;
    private readonly int nativeWidth;
    private readonly int nativeHeight;
    private int selectedTab;
    private bool closing;
    private bool nativeClosed;
    private string hoverText = "";
    private Point viewport;
    private ClickableComponent? questFocus;

    // QuestLog has no public detail-state accessor; tab navigation must target visible controls.
    private bool ShowingQuestDetails => (int)QuestPage.GetValue(quests)! >= 0;
    private int QuestDefaultId => ShowingQuestDetails ? QuestLog.region_backButton : 0;

    private string JournalTitle => Game1.content.LoadString("Strings\\StringsFromCSFiles:QuestLog.cs.11373");
    private IClickableMenu Page => tabs[selectedTab].name switch
    {
        "tasks" => tasks,
        "guild" => guild!,
        "events" => events!,
        _ => quests
    };
    private IClickableMenu FocusMenu
    {
        get
        {
            IClickableMenu menu = this;
            while (menu.GetChildMenu() is { } child)
                menu = child;
            return ReferenceEquals(menu, this) ? Page : menu;
        }
    }

    public JournalMenu(QuestLog quests, TaskRepository repository, ITranslationHelper translations,
        bool showMonsterGoals, Func<IReadOnlyList<EventRow>>? findEvents)
    {
        this.quests = quests;
        this.repository = repository;
        this.translations = translations;
        nativeWidth = quests.width;
        nativeHeight = quests.height;
        upperRightCloseButton = quests.upperRightCloseButton;

        bool showGuild = showMonsterGoals && GuildGoal.HasVisitedGuild;
        var names = new List<string> { "quests", "tasks" };
        if (findEvents is not null)
            names.Add("events");
        if (showGuild)
            names.Add("guild");
        for (int i = 0; i < names.Count; i++)
        {
            tabs.Add(new ClickableComponent(Rectangle.Empty, names[i], translations.Get("tab." + names[i]))
            {
                myID = TabId + i,
                leftNeighborID = i > 0 ? TabId + i - 1 : -1,
                rightNeighborID = i < names.Count - 1 ? TabId + i + 1 : upperRightCloseButton_ID,
                fullyImmutable = true,
                tryDefaultIfNoDownNeighborExists = true
            });
        }

        tasks = new JournalListPage(true, repository, translations, tabs, upperRightCloseButton, OpenEditor);
        if (showGuild)
            guild = new JournalListPage(false, repository, translations, tabs, upperRightCloseButton, OpenEditor);
        if (findEvents is not null)
        {
            events = new EventsPage(findEvents, tabs, upperRightCloseButton);
            events.exitFunction = () =>
            {
                if (!closing && ReferenceEquals(Game1.activeClickableMenu, this))
                    exitThisMenu(false);
            };
        }

        // QuestLog closes itself, but it isn't the active menu when hosted here.
        var nativeExit = quests.exitFunction;
        quests.exitFunction = () =>
        {
            nativeClosed = true;
            nativeExit?.Invoke();
            if (!closing && ReferenceEquals(Game1.activeClickableMenu, this))
                exitThisMenu(false);
        };
        Layout();
        AttachTabs();
        if (Game1.options.SnappyMenus)
            quests.snapToDefaultClickableComponent();
    }

    private void Layout()
    {
        viewport = new Point(Game1.uiViewport.Width, Game1.uiViewport.Height);
        width = Math.Min(nativeWidth, Math.Max(576, viewport.X - 128));
        height = Math.Min(nativeHeight, Math.Max(320, viewport.Y - 160));
        xPositionOnScreen = (viewport.X - width) / 2;
        yPositionOnScreen = HeaderHeight + (viewport.Y - HeaderHeight - height) / 2;
        var bounds = new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height);

        // Both vanilla quests and event hints use the game's list/detail layout.
        foreach (var page in new QuestLog?[] { quests, events }.OfType<QuestLog>())
        {
            page.xPositionOnScreen = bounds.X;
            page.yPositionOnScreen = bounds.Y;
            page.width = width;
            page.height = height;
            for (int i = 0; i < page.questLogButtons.Count; i++)
                page.questLogButtons[i].bounds = new Rectangle(bounds.X + 16,
                    bounds.Y + 16 + i * ((height - 32) / 6), width - 32, (height - 32) / 6 + 4);
            page.backButton.bounds = new Rectangle(bounds.X - 64, bounds.Y + 8, 48, 44);
            page.forwardButton.bounds = new Rectangle(bounds.Right + 16, bounds.Bottom - 48, 48, 44);
            page.rewardBox.bounds = new Rectangle(bounds.Center.X - 80, bounds.Bottom - 128, 96, 96);
            page.cancelQuestButton.bounds = new Rectangle(bounds.X + 4, bounds.Bottom + 4, 48, 48);
            page.upArrow.bounds = new Rectangle(bounds.Right + 16, bounds.Y + 96, 44, 48);
            page.downArrow.bounds = new Rectangle(bounds.Right + 16, bounds.Bottom - 64, 44, 48);
            page.scrollBarBounds = new Rectangle(page.upArrow.bounds.X + 12, page.upArrow.bounds.Bottom + 4,
                24, Math.Max(40, page.downArrow.bounds.Top - page.upArrow.bounds.Bottom - 8));
            page.scrollBar.bounds = new Rectangle(page.scrollBarBounds.X, page.scrollBarBounds.Y, 24, 40);
        }
        upperRightCloseButton.bounds = new Rectangle(bounds.Right - 20, bounds.Y - 48, 48, 48);
        upperRightCloseButton.myID = upperRightCloseButton_ID;
        upperRightCloseButton.leftNeighborID = tabs[^1].myID;
        upperRightCloseButton.downNeighborID = 0;

        for (int i = 0; i < tabs.Count; i++)
            tabs[i].bounds = new Rectangle(bounds.X + 64 + i * 64, bounds.Y - 48, 64, 64);
        tasks.Layout(bounds);
        guild?.Layout(bounds);
        events?.Layout();
        quests.questLogButtons[0].upNeighborID = TabId;
        quests.backButton.upNeighborID = TabId;
        quests.forwardButton.upNeighborID = TabId;
        AttachTabs();
    }

    private void AttachTabs()
    {
        if (selectedTab == 0)
        {
            quests.populateClickableComponentList();
            quests.allClickableComponents.AddRange(tabs);
        }
        AttachTabDestinations();
    }

    private void ChangeTab(int index, bool focusTab)
    {
        index = Math.Clamp(index, 0, tabs.Count - 1);
        if (index == selectedTab || !Page.readyToClose())
            return;
        if (selectedTab == 0 && quests.currentlySnappedComponent is { } focus && focus.myID < TabId)
            questFocus = focus;
        selectedTab = index;
        if (Page is JournalListPage list)
            list.Refresh();
        else if (Page is EventsPage eventsPage)
            eventsPage.Enter();
        AttachTabs();
        if (Game1.options.SnappyMenus)
        {
            if (focusTab)
            {
                Page.setCurrentlySnappedComponentTo(TabId + index);
                Page.snapCursorToCurrentSnappedComponent();
            }
            else if (selectedTab == 0)
            {
                bool valid = questFocus is not null && (ShowingQuestDetails
                    ? questFocus.myID is QuestLog.region_backButton or QuestLog.region_rewardBox or QuestLog.region_cancelQuestButton
                    : questFocus.myID is >= 0 and < 6 or QuestLog.region_backButton or QuestLog.region_forwardButton);
                quests.currentlySnappedComponent = valid ? questFocus : quests.getComponentWithID(QuestDefaultId);
                quests.snapCursorToCurrentSnappedComponent();
            }
            else
                Page.snapToDefaultClickableComponent();
        }
        Game1.playSound("smallSelect");
    }

    private void OpenEditor(string? taskId)
    {
        if (repository.Tasks is null || GetChildMenu() is not null)
            return;
        string? previous = tasks.SelectedTaskId;
        tasks.BeginEdit(taskId);
        hoverText = "";
        var editor = new InlineTaskEditor(repository, translations, taskId, () => tasks.EditingBounds);
        editor.exitFunction = () =>
        {
            tasks.EndEdit(editor.ResultTaskId ?? previous);
            AttachTabs();
            if (Game1.options.SnappyMenus)
                tasks.snapToDefaultClickableComponent();
        };
        SetChildMenu(editor);
        if (Game1.options.SnappyMenus)
            editor.snapToDefaultClickableComponent();
        Game1.playSound("smallSelect");
    }

    internal void CancelInlineAt(InlineTaskEditor editor, int x, int y, bool playSound)
    {
        if (!ReferenceEquals(GetChildMenu(), editor))
            return;
        // Ending a draft can reflow the list; capture the task identity before removing its row.
        var action = tasks.CaptureClick(x, y);
        int tab = tabs.FindIndex(tab => tab.bounds.Contains(x, y));
        bool close = upperRightCloseButton.bounds.Contains(x, y);
        editor.exitThisMenu(false);
        if (close)
            exitThisMenu(playSound);
        else if (tab >= 0)
            ChangeTab(tab, Game1.options.SnappyMenus);
        else
            action?.Invoke();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (GetChildMenu() is not null)
            return;
        if (upperRightCloseButton.containsPoint(x, y) && readyToClose())
        {
            exitThisMenu();
            return;
        }
        for (int i = 0; i < tabs.Count; i++)
        {
            if (!tabs[i].containsPoint(x, y))
                continue;
            ChangeTab(i, Game1.options.SnappyMenus);
            return;
        }
        Page.receiveLeftClick(x, y, playSound);
    }

    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        if (GetChildMenu() is null)
            Page.receiveRightClick(x, y, playSound);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (GetChildMenu() is null)
            Page.receiveScrollWheelAction(direction);
    }

    public override void leftClickHeld(int x, int y)
    {
        if (GetChildMenu() is null)
            Page.leftClickHeld(x, y);
    }

    public override void releaseLeftClick(int x, int y)
    {
        if (GetChildMenu() is null)
            Page.releaseLeftClick(x, y);
    }

    public override void receiveGamePadButton(Buttons button)
    {
        if (GetChildMenu() is not null)
            return;
        if (button == Buttons.LeftTrigger || button == Buttons.RightTrigger)
        {
            ChangeTab(selectedTab + (button == Buttons.LeftTrigger ? -1 : 1), false);
            return;
        }
        Page.receiveGamePadButton(button);
    }

    public override void receiveKeyPress(Keys key)
    {
        // The game can send a mapped key to this menu after the same button opened a child.
        if (GetChildMenu() is not null)
            return;
        if (selectedTab != 0 && (Game1.options.doesInputListContain(Game1.options.menuButton, key)
            || Game1.options.doesInputListContain(Game1.options.journalButton, key)))
        {
            if (Page is EventsPage { ShowingDetails: true } eventsPage)
                eventsPage.Back();
            else
                exitThisMenu();
            return;
        }
        Page.receiveKeyPress(key);
    }

    public override void applyMovementKey(int direction) => Page.applyMovementKey(direction);
    public override void automaticSnapBehavior(int direction, int oldRegion, int oldID) => Page.automaticSnapBehavior(direction, oldRegion, oldID);
    public override void snapToDefaultClickableComponent()
    {
        if (GetChildMenu() is null && selectedTab == 0)
        {
            quests.currentlySnappedComponent = quests.getComponentWithID(QuestDefaultId);
            if (Game1.options.SnappyMenus)
                quests.snapCursorToCurrentSnappedComponent();
        }
        else
            FocusMenu.snapToDefaultClickableComponent();
    }
    public override void snapCursorToCurrentSnappedComponent() => FocusMenu.snapCursorToCurrentSnappedComponent();
    public override ClickableComponent getCurrentlySnappedComponent() => FocusMenu.getCurrentlySnappedComponent();
    public override void setCurrentlySnappedComponentTo(int id) => FocusMenu.setCurrentlySnappedComponentTo(id);
    public override void setUpForGamePadMode()
    {
        FocusMenu.setUpForGamePadMode();
        if (Game1.options.SnappyMenus)
            snapToDefaultClickableComponent();
    }
    public override bool readyToClose() => GetChildMenu() is null && Page.readyToClose();

    internal void AdvanceCompletions(TimeSpan elapsed)
    {
        if (closing)
            return;
        string? selected = tasks.SelectedTaskId;
        if (repository.Tasks?.AdvanceCompletions(elapsed) != true)
            return;
        repository.Save();
        tasks.Refresh(selected);
        AttachTabDestinations();
        if (GetChildMenu() is InlineTaskEditor editor)
            editor.Reflow();
        else if (selectedTab == 1 && Game1.options.SnappyMenus && !Game1.lastCursorMotionWasMouse)
            tasks.snapCursorToCurrentSnappedComponent();
    }

    public override void update(GameTime time)
    {
        if (viewport.X != Game1.uiViewport.Width || viewport.Y != Game1.uiViewport.Height)
        {
            gameWindowSizeChanged(new Rectangle(0, 0, viewport.X, viewport.Y),
                new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height));
        }
        Page.update(time);
        AttachTabDestinations();
    }

    private void AttachTabDestinations()
    {
        int destination = Page switch
        {
            JournalListPage list => list.DefaultId,
            EventsPage eventsPage => eventsPage.DefaultId,
            _ => QuestDefaultId
        };
        foreach (var tab in tabs)
            tab.downNeighborID = destination;
        upperRightCloseButton.downNeighborID = destination;
        tabs[^1].rightNeighborID = upperRightCloseButton_ID;
        upperRightCloseButton.leftNeighborID = tabs[^1].myID;
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        Layout();
        GetChildMenu()?.gameWindowSizeChanged(oldBounds, newBounds);
        if (Game1.options.SnappyMenus)
            snapCursorToCurrentSnappedComponent();
    }

    public override void performHoverAction(int x, int y)
    {
        Page.performHoverAction(x, y);
        upperRightCloseButton.tryHover(x, y);
        hoverText = tabs.Find(tab => tab.bounds.Contains(x, y))?.label ?? "";
    }

    private void DrawPage(SpriteBatch b)
    {
        bool nativePage = Page is QuestLog;
        if (!Game1.options.showClearBackgrounds)
        {
            // Native pages dim the area below the clip themselves; dim the remaining header band once.
            b.Draw(Game1.fadeToBlackRect,
                new Rectangle(0, 0, Game1.uiViewport.Width, nativePage ? yPositionOnScreen : Game1.uiViewport.Height),
                Color.Black * 0.75f);
        }
        if (!nativePage)
        {
            Page.draw(b);
            return;
        }

        // QuestLog draws a fixed-position banner. Clip that out so every page shares our separate header row.
        Rectangle scissor = b.GraphicsDevice.ScissorRectangle;
        b.End();
        b.GraphicsDevice.ScissorRectangle = Utility.ConstrainScissorRectToScreen(
            new Rectangle(0, yPositionOnScreen, Game1.uiViewport.Width, Game1.uiViewport.Height - yPositionOnScreen));
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, HeaderClip);
        Page.draw(b);
        b.End();
        b.GraphicsDevice.ScissorRectangle = scissor;
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
    }

    public override void draw(SpriteBatch b)
    {
        foreach (var button in quests.allClickableComponents.OfType<ClickableTextureComponent>())
            button.texture = Game1.mouseCursors;
        DrawPage(b);
        for (int i = 0; i < tabs.Count; i++)
        {
            var tab = tabs[i];
            float scale = tab.bounds.Width / 16f;
            var origin = new Vector2(tab.bounds.X, tab.bounds.Y + (selectedTab == i ? tab.bounds.Height / 8 : 0));
            // Match the pause menu tile and selection offset, with its stem behind the top border.
            int visibleHeight = (int)((yPositionOnScreen + tab.bounds.Height * 3 / 16 - origin.Y) / scale);
            b.Draw(Game1.mouseCursors, origin, new Rectangle(16, 368, 16, visibleHeight), Color.White,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0.9f);
            Rectangle icon = tab.name switch
            {
                "quests" => new Rectangle(395, 497, 3, 8),
                "tasks" => new Rectangle(165, 443, 10, 10),
                "events" => new Rectangle(211, 428, 7, 6),
                _ => new Rectangle(120, 428, 10, 10)
            };
            var offset = new Vector2(MathF.Round((tab.bounds.Width - icon.Width * scale) / 2),
                MathF.Round((tab.bounds.Height - icon.Height * scale) / 2));
            b.Draw(Game1.mouseCursors, origin + offset, icon, Color.White,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0.91f);
        }
        SpriteText.drawStringWithScrollCenteredAt(b, JournalTitle,
            xPositionOnScreen + width / 2, yPositionOnScreen - HeaderHeight);
        upperRightCloseButton.draw(b);
        if (GetChildMenu() is null && hoverText.Length > 0)
            drawHoverText(b, hoverText, Game1.smallFont);
        if (GetChildMenu() is null)
            drawMouse(b, true);
    }

    protected override void cleanupBeforeExit() => Dispose();

    public override void emergencyShutDown() => Dispose();

    public void Dispose()
    {
        if (closing)
            return;
        closing = true;
        GetChildMenu()?.emergencyShutDown();
        SetChildMenu(null);
        repository.FinishCompleted();
        if (!nativeClosed)
        {
            nativeClosed = true;
            quests.exitThisMenu(false);
        }
    }
}
