using System.Numerics;
using Raylib_cs;

namespace Breakout;

/// <summary>Draws the same logical playfield at any window size.</summary>
internal sealed class GameRenderer : IDisposable
{
    private static readonly Color Hud = new(15, 28, 42, 255);
    private static readonly Color BackgroundTop = new(29, 52, 69, 255);
    private static readonly Color BackgroundBottom = new(13, 30, 44, 255);
    private static readonly Color Ivory = new(255, 245, 222, 255);
    private static readonly Color Muted = new(172, 192, 204, 255);
    private static readonly Color Gold = new(252, 208, 126, 255);
    private static readonly Color Panel = new(13, 25, 39, 248);
    private static readonly Color PanelBorder = new(65, 90, 110, 255);
    private static readonly Color Shadow = new(0, 0, 0, 64);

    // Each column has a base color. The rows shade it from light to dark.
    private static readonly Color[] BrickPalette =
    [
        new(230, 101, 98, 255),
        new(235, 137, 91, 255),
        new(235, 179, 89, 255),
        new(211, 194, 100, 255),
        new(126, 186, 126, 255),
        new(81, 185, 167, 255),
        new(88, 158, 210, 255),
        new(126, 140, 220, 255),
        new(170, 129, 213, 255)
    ];

    private readonly RenderTexture2D playfield = Raylib.LoadRenderTexture(
        BreakoutGame.Width, BreakoutGame.Height);

    public GameRenderer()
    {
        Raylib.SetTextureFilter(playfield.Texture, TextureFilter.Bilinear);
    }

    public void Draw(BreakoutGame game)
    {
        Raylib.BeginTextureMode(playfield);
        DrawPlayfield(game);
        Raylib.EndTextureMode();

        // Scale the entire scene uniformly. Extra space becomes black bars so
        // fullscreen and resizing never change the shape of the ball or board.
        float windowWidth = Raylib.GetScreenWidth();
        float windowHeight = Raylib.GetScreenHeight();
        float scale = Math.Min(windowWidth / BreakoutGame.Width, windowHeight / BreakoutGame.Height);
        float width = BreakoutGame.Width * scale;
        float height = BreakoutGame.Height * scale;
        var destination = new Rectangle((windowWidth - width) / 2f, (windowHeight - height) / 2f, width, height);

        // A negative source height flips the render texture upright.
        var source = new Rectangle(0, 0, BreakoutGame.Width, -BreakoutGame.Height);
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        Raylib.DrawTexturePro(playfield.Texture, source, destination, Vector2.Zero, 0f, Color.White);
        Raylib.EndDrawing();
    }

    public void Dispose()
    {
        Raylib.UnloadRenderTexture(playfield);
    }

    private static void DrawPlayfield(BreakoutGame game)
    {
        Raylib.DrawRectangleGradientV(
            0, 0, BreakoutGame.Width, BreakoutGame.Height, BackgroundTop, BackgroundBottom);
        Raylib.DrawRectangle(0, 0, BreakoutGame.Width, BreakoutGame.HudHeight, Hud);
        Raylib.DrawLine(0, BreakoutGame.HudHeight - 1, BreakoutGame.Width, BreakoutGame.HudHeight - 1, PanelBorder);

        string score = $"SCORE  {game.Score}";
        string lives = $"LIVES  {game.Lives}";
        const int hudFontSize = 26;
        Raylib.DrawText(score, 24, 19, hudFontSize, Ivory);
        Raylib.DrawText(lives, BreakoutGame.Width - 24 - Raylib.MeasureText(lives, hudFontSize),
            19, hudFontSize, Ivory);

        foreach (Brick brick in game.Bricks)
        {
            if (!brick.IsBroken)
            {
                DrawBrick(brick);
            }
        }

        Rectangle paddle = game.Paddle;
        Raylib.DrawRectangleRec(paddle with { Y = paddle.Y + 4f }, Shadow);
        Raylib.DrawRectangleRec(paddle, Ivory);
        Raylib.DrawRectangleRec(new Rectangle(paddle.X, paddle.Y + paddle.Height - 4f, paddle.Width, 4f), Gold);
        if (game.State != GameState.Lost)
        {
            Raylib.DrawCircleV(game.BallPosition + new Vector2(0, 4), BreakoutGame.BallRadius, Shadow);
            Raylib.DrawCircleV(game.BallPosition, BreakoutGame.BallRadius, Ivory);
        }

        switch (game.State)
        {
            case GameState.Title:
                DrawOverlay("BREAKOUT", "Clear all the bricks", "ENTER to play", "Arrow keys or A / D to move    |    F for fullscreen");
                break;
            case GameState.ReadyToServe:
                DrawCentered("ENTER to serve", 565, 24, Ivory);
                break;
            case GameState.Won:
                DrawOverlay("BOARD CLEARED", $"Final score  {game.Score}", "ENTER to play again");
                break;
            case GameState.Lost:
                DrawOverlay("GAME OVER", $"Final score  {game.Score}", "ENTER to play again");
                break;
        }
    }

    private static void DrawBrick(Brick brick)
    {
        Rectangle bounds = brick.Bounds;
        Color baseColor = BrickPalette[brick.Column];
        float rowShade = brick.Row / (float)(BreakoutGame.Rows - 1);
        Color face = Raylib.ColorLerp(baseColor, Hud, rowShade * 0.23f);
        Color highlight = Raylib.ColorLerp(face, Ivory, 0.35f);

        Raylib.DrawRectangleRec(bounds with { Y = bounds.Y + 4f }, Shadow);
        Raylib.DrawRectangleRec(bounds, face);
        Raylib.DrawRectangleRec(new Rectangle(bounds.X, bounds.Y, bounds.Width, 3f), highlight);
    }

    private static void DrawOverlay(string title, string subtitle, string action, string? controls = null)
    {
        var panel = new Rectangle(192, 358, 716, 230);
        Raylib.DrawRectangleRec(panel with { Y = panel.Y + 7f }, Shadow);
        Raylib.DrawRectangleRec(panel, Panel);
        Raylib.DrawRectangleLinesEx(panel, 1, PanelBorder);
        DrawCentered(title, 387, 64, Ivory);
        DrawCentered(subtitle, 462, 28, Gold);
        DrawCentered(action, 513, 23, Ivory);

        if (controls is not null)
        {
            DrawCentered(controls, 547, 19, Muted);
        }
    }

    private static void DrawCentered(string text, int y, int size, Color color)
    {
        int x = (BreakoutGame.Width - Raylib.MeasureText(text, size)) / 2;
        Raylib.DrawText(text, x, y, size, color);
    }
}
