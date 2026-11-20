using Raylib_cs;

namespace Breakout;

/// <summary>Loads one sound per surface and owns the native audio resources.</summary>
internal sealed class GameAudio : IDisposable
{
    private readonly bool deviceReady;
    private readonly Sound? wall;
    private readonly Sound? paddle;
    private readonly Sound? brick;

    public GameAudio()
    {
        Raylib.InitAudioDevice();
        deviceReady = Raylib.IsAudioDeviceReady();

        // The game rules still run on machines where audio is unavailable.
        if (deviceReady)
        {
            wall = Load("wall.wav");
            paddle = Load("paddle.wav");
            brick = Load("brick.wav");
        }
    }

    public void Play(Impact impacts)
    {
        // A step can contain more than one kind of impact, so check each flag.
        if ((impacts & Impact.Wall) != 0 && wall is Sound wallSound)
        {
            Raylib.PlaySound(wallSound);
        }

        if ((impacts & Impact.Paddle) != 0 && paddle is Sound paddleSound)
        {
            Raylib.PlaySound(paddleSound);
        }

        if ((impacts & Impact.Brick) != 0 && brick is Sound brickSound)
        {
            Raylib.PlaySound(brickSound);
        }
    }

    public void Dispose()
    {
        // Unload the sounds while the device still exists, then close it.
        Unload(brick);
        Unload(paddle);
        Unload(wall);

        if (deviceReady)
        {
            Raylib.CloseAudioDevice();
        }
    }

    private static Sound? Load(string fileName)
    {
        // Use the executable's directory so the files load from any working directory.
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Audio", fileName);
        Sound sound = Raylib.LoadSound(path);
        return Raylib.IsSoundValid(sound) ? sound : null;
    }

    private static void Unload(Sound? sound)
    {
        if (sound is Sound loadedSound)
        {
            Raylib.UnloadSound(loadedSound);
        }
    }
}
