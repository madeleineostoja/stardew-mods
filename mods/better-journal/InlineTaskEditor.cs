using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace BetterJournal;

// Native child-menu routing isolates text input; the editor itself is drawn inside its task row.
internal sealed class InlineTaskEditor : IClickableMenu
{
    private const int TextId = 3000;
    private const int SaveId = 3001;
    private const int DeleteId = 3002;
    private readonly TaskRepository repository;
    private readonly ITranslationHelper translations;
    private readonly string? taskId;
    private readonly Func<Rectangle> getRow;
    private readonly TextBox textBox;
    private readonly ClickableComponent textField = new(Rectangle.Empty, "text");
    private readonly ClickableComponent save = new(Rectangle.Empty, "save");
    private readonly ClickableComponent delete = new(Rectangle.Empty, "delete");
    private bool showKeyboard;
    private bool invalid;
    private bool finished;
    private string rejectedText = "";
    private string hoverText = "";
    private Point viewport;

    public string? ResultTaskId { get; private set; }

    public InlineTaskEditor(TaskRepository repository, ITranslationHelper translations, string? taskId, Func<Rectangle> getRow)
    {
        this.repository = repository;
        this.translations = translations;
        this.taskId = taskId;
        this.getRow = getRow;
        ResultTaskId = taskId;
        textBox = new LiveTextBox
        {
            limitWidth = false,
            textLimit = TaskList.TextLimit,
            TitleText = T(taskId is null ? "editor.add" : "editor.edit"),
            Text = repository.Tasks!.Entries.FirstOrDefault(task => task.Id == taskId)?.Text ?? ""
        };
        textBox.OnEnterPressed += _ => Save();
        save.label = T("editor.save");
        delete.label = T(taskId is null ? "editor.discard" : "editor.delete");
        Layout();
        textBox.Selected = true;
        showKeyboard = Game1.options.gamepadControls && !Game1.lastCursorMotionWasMouse;
    }

    private void Layout()
    {
        viewport = new Point(Game1.uiViewport.Width, Game1.uiViewport.Height);
        var row = getRow();
        xPositionOnScreen = row.X;
        yPositionOnScreen = row.Y;
        width = row.Width;
        height = row.Height;

        var buttons = new[] { save, delete };
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            button.bounds = new Rectangle(row.Right - 16 - buttons.Length * 48 + i * 48, row.Center.Y - 24, 48, 48);
            button.myID = ReferenceEquals(button, save) ? SaveId : DeleteId;
            button.leftNeighborID = i > 0 ? SaveId : TextId;
            button.rightNeighborID = i == 0 ? DeleteId : -1;
            button.upNeighborID = TextId;
        }
        textField.bounds = new Rectangle(row.X + 80, row.Center.Y - 32, save.bounds.X - row.X - 88, 64);
        textField.myID = TextId;
        textField.rightNeighborID = SaveId;
        textField.downNeighborID = SaveId;
        textBox.X = textField.bounds.X;
        textBox.Y = textField.bounds.Y + (textField.bounds.Height - textBox.Height) / 2;
        textBox.Width = textField.bounds.Width;

        allClickableComponents = new List<ClickableComponent> { textField };
        allClickableComponents.AddRange(buttons);
        int oldId = currentlySnappedComponent?.myID ?? TextId;
        currentlySnappedComponent = getComponentWithID(oldId) ?? textField;
    }

    public override void snapToDefaultClickableComponent()
    {
        currentlySnappedComponent = textField;
        if (Game1.options.SnappyMenus)
            snapCursorToCurrentSnappedComponent();
    }

    private void Save()
    {
        if (finished)
            return;
        string text = textBox.Text.Trim();
        if (text.Length == 0 || text.Length > TaskList.TextLimit || text.Contains('\n') || text.Contains('\r'))
        {
            invalid = true;
            rejectedText = textBox.Text;
            Game1.playSound("cancel");
            return;
        }
        if (taskId is null)
            ResultTaskId = repository.Tasks!.Add(text).Id;
        else
            repository.Tasks!.Rename(taskId, text);
        repository.Save();
        exitThisMenu();
    }

    private void ConfirmDelete()
    {
        textBox.Selected = false;
        hoverText = "";
        ConfirmationDialog? confirmation = null;
        confirmation = new ConfirmationDialog(T("editor.delete-confirm"), _ =>
        {
            repository.Tasks!.Remove(taskId!);
            repository.Save();
            ResultTaskId = null;
            confirmation!.exitThisMenu(false);
            exitThisMenu();
        }, _ => confirmation!.exitThisMenu())
        {
            exitFunction = () =>
            {
                currentlySnappedComponent = delete;
                if (Game1.options.SnappyMenus)
                    snapCursorToCurrentSnappedComponent();
            }
        };
        SetChildMenu(confirmation);
        if (Game1.options.SnappyMenus)
            confirmation.snapToDefaultClickableComponent();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (finished || GetChildMenu() is not null || Game1.textEntry is not null)
            return;
        if (!new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height).Contains(x, y))
        {
            ((JournalMenu)GetParentMenu()).CancelInlineAt(this, x, y, playSound);
            return;
        }
        if (textField.containsPoint(x, y))
        {
            currentlySnappedComponent = textField;
            textBox.Update();
            return;
        }
        textBox.Selected = false;
        if (save.containsPoint(x, y))
            Save();
        else if (delete.containsPoint(x, y))
        {
            if (taskId is null)
                exitThisMenu();
            else
                ConfirmDelete();
        }
    }

    public override void receiveKeyPress(Keys key)
    {
        if (finished || GetChildMenu() is not null || Game1.textEntry is not null)
            return;
        if (key == Keys.Escape)
        {
            exitThisMenu();
            return;
        }
        if (!textBox.Selected)
            base.receiveKeyPress(key);
    }

    public override void receiveGamePadButton(Buttons button)
    {
        if (finished || GetChildMenu() is not null || Game1.textEntry is not null)
            return;
        if (button is Buttons.B or Buttons.Back)
        {
            exitThisMenu();
            return;
        }
        if (button is Buttons.LeftTrigger or Buttons.RightTrigger)
        {
            var parent = GetParentMenu();
            exitThisMenu(false);
            parent?.receiveGamePadButton(button);
            return;
        }
        if (button == Buttons.Start)
        {
            Save();
            return;
        }
        if (button is Buttons.DPadUp or Buttons.DPadDown or Buttons.DPadLeft or Buttons.DPadRight
            or Buttons.LeftThumbstickUp or Buttons.LeftThumbstickDown or Buttons.LeftThumbstickLeft or Buttons.LeftThumbstickRight)
            textBox.Selected = false;
    }

    public override void update(GameTime time)
    {
        if (viewport.X != Game1.uiViewport.Width || viewport.Y != Game1.uiViewport.Height)
            GetParentMenu()?.gameWindowSizeChanged(new Rectangle(0, 0, viewport.X, viewport.Y),
                new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height));
        if (invalid && textBox.Text != rejectedText)
            invalid = false;
        if (showKeyboard)
        {
            showKeyboard = false;
            Game1.showTextEntry(textBox);
        }
    }

    internal void Reflow()
    {
        Layout();
        if (Game1.options.SnappyMenus && Game1.textEntry is null && GetChildMenu() is null)
            snapCursorToCurrentSnappedComponent();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        Layout();
        GetChildMenu()?.gameWindowSizeChanged(oldBounds, newBounds);
        if (Game1.options.SnappyMenus)
        {
            if (GetChildMenu() is { } child)
                child.snapCursorToCurrentSnappedComponent();
            else
                snapCursorToCurrentSnappedComponent();
        }
    }

    public override void performHoverAction(int x, int y)
    {
        hoverText = "";
        if (GetChildMenu() is not null || Game1.textEntry is not null)
            return;
        if (save.bounds.Contains(x, y))
            hoverText = save.label;
        else if (delete.bounds.Contains(x, y))
            hoverText = delete.label;
    }

    protected override void cleanupBeforeExit()
    {
        finished = true;
        textBox.Selected = false;
    }

    public override void emergencyShutDown()
    {
        GetChildMenu()?.emergencyShutDown();
        cleanupBeforeExit();
    }

    public override void draw(SpriteBatch b)
    {
        bool complete = taskId is not null && repository.Tasks!.Entries.Any(task => task.Id == taskId && task.CompletedOnDay.HasValue);
        var checkbox = JournalListPage.CheckboxBounds(new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
        MenuDrawing.Checkbox(b, checkbox.X, checkbox.Y, complete);
        textBox.Draw(b, false);
        b.Draw(Game1.mouseCursors, save.bounds, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46), Color.White);
        b.Draw(Game1.mouseCursors, delete.bounds, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 47), Color.White);
        if (GetChildMenu() is null && Game1.textEntry is null)
        {
            if (invalid || hoverText.Length > 0)
                drawHoverText(b, invalid ? T("editor.invalid") : hoverText, Game1.smallFont);
            drawMouse(b, true);
        }
    }

    private sealed class LiveTextBox : TextBox
    {
        public LiveTextBox()
            : base(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor)
        {
        }

        public override void Draw(SpriteBatch b, bool drawShadow = true)
        {
            _textBoxTexture = Game1.content.Load<Texture2D>("LooseSprites\\textBox");
            _font = Game1.smallFont;
            _textColor = Game1.textColor;
            base.Draw(b, drawShadow);
        }
    }

    private string T(string key) => translations.Get(key);
}
