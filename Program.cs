using Raylib_cs;

namespace Breakout;

internal static class Program
{
    private const int FramesPerSecond = 60;
    private const float MaxFrameTime = 0.1f;

    public static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(990, 720, "Breakout");

        try
        {
            Raylib.SetWindowMinSize(550, 400);
            Raylib.SetTargetFPS(FramesPerSecond);

            // These objects release native resources before the window closes.
            using var renderer = new GameRenderer();
            using var audio = new GameAudio();
            RunGame(new BreakoutGame(), renderer, audio);
        }
        finally
        {
            Raylib.CloseWindow();
        }
    }

    private static void RunGame(BreakoutGame game, GameRenderer renderer, GameAudio audio)
    {
        double pendingTime = 0;

        while (!Raylib.WindowShouldClose())
        {
            int move = ReadInput(game);

            // The render rate can vary. Accumulate elapsed seconds and run as many
            // equal-length simulation steps as fit before drawing the next frame.
            // Cap catch-up time so dragging the window cannot skip far ahead.
            pendingTime += Math.Min(Raylib.GetFrameTime(), MaxFrameTime);
            while (pendingTime >= BreakoutGame.StepSeconds)
            {
                audio.Play(game.Step(move));
                pendingTime -= BreakoutGame.StepSeconds;
            }

            renderer.Draw(game);
        }
    }

    private static int ReadInput(BreakoutGame game)
    {
        bool left = Raylib.IsKeyDown(KeyboardKey.Left) || Raylib.IsKeyDown(KeyboardKey.A);
        bool right = Raylib.IsKeyDown(KeyboardKey.Right) || Raylib.IsKeyDown(KeyboardKey.D);

        // The queue catches taps that start and end between rendered frames.
        // Held movement keys are also read above so the paddle moves continuously.
        KeyboardKey key;
        while ((key = (KeyboardKey)Raylib.GetKeyPressed()) != KeyboardKey.Null)
        {
            switch (key)
            {
                case KeyboardKey.F:
                    Raylib.ToggleBorderlessWindowed();
                    break;
                case KeyboardKey.Enter:
                    game.PressEnter();
                    break;
                case KeyboardKey.Left:
                case KeyboardKey.A:
                    left = true;
                    break;
                case KeyboardKey.Right:
                case KeyboardKey.D:
                    right = true;
                    break;
            }
        }

        return (right ? 1 : 0) - (left ? 1 : 0);
    }
}
