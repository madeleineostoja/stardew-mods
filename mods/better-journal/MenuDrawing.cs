using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace BetterJournal;

internal static class MenuDrawing
{
    public static void Button(SpriteBatch b, ClickableComponent button)
    {
        bool hovered = button.bounds.Contains(Game1.getMouseX(), Game1.getMouseY());
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9),
            button.bounds.X, button.bounds.Y, button.bounds.Width, button.bounds.Height,
            hovered ? Color.Wheat : Color.White, 4f);
        var size = Game1.dialogueFont.MeasureString(button.label);
        Utility.drawTextWithShadow(b, button.label, Game1.dialogueFont,
            new Vector2(button.bounds.Center.X - size.X / 2, button.bounds.Center.Y + 4 - size.Y / 2),
            Game1.textColor, 1f, -1f, -1, -1, 0f);
    }

    public static void Checkbox(SpriteBatch b, int x, int y, bool complete)
    {
        b.Draw(Game1.mouseCursors, new Vector2(x, y),
            complete ? OptionsCheckbox.sourceRectChecked : OptionsCheckbox.sourceRectUnchecked,
            Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
    }

    public static string QuestText(string text, int width)
    {
        // SpriteText treats carets as line breaks rather than literal task text.
        text = text.Replace('^', ' ');
        if (SpriteText.getWidthOfString(text) <= width)
            return text;
        while (text.Length > 0 && SpriteText.getWidthOfString(text + "...") > width)
            text = text[..^1];
        return text.TrimEnd() + "...";
    }
}
