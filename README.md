# Breakout

Klassisk breakout spill med C# and Raylib-cs 8.1.0. 

## Controls

| Key | Action |
| --- | --- |
| Enter | Start, serve after a miss, or restart when the round ends |
| Left / Right or A / D | Move the paddle |
| F | Toggle borderless fullscreen |
| Esc | Quit |

Each brick is worth 100 points and makes the ball a little faster. When the ball drops below the playfield, you lose a life and keep your score, remaining bricks, and current ball speed. Move the paddle between serves to choose where to launch the ball. Starting a fresh round resets the speed.

## Build and run from source

Install the .NET 10 SDK on 64-bit Windows. Open PowerShell in the repository folder containing `Breakout.csproj`. Run these commands to restore the Raylib-cs NuGet package, compile the game, and launch the Debug build.

```powershell
dotnet restore
dotnet build --no-restore
dotnet run --no-build
```

The build puts `Breakout.exe` and its audio assets in `bin/Debug/net10.0`. While editing the code, use `dotnet run` to rebuild and launch in one command.


## Read the code

- [Program.cs](Program.cs) opens the window, reads the keyboard, advances the game, and draws a frame. The simulation moves in fixed steps of 1/240 second, even when the drawing rate varies.
- [BreakoutGame.cs](BreakoutGame.cs) owns the board, positions, score, lives, and round state. Start with `PressEnter` and `Step`, then follow the collision methods. A ball velocity is a vector in pixels per second. Scaling it by the step duration gives the distance the ball moves in that step.
- [GameRenderer.cs](GameRenderer.cs) draws the game to a fixed-size texture and scales it to fit the window. The drawing uses the game's current positions without changing them.
- [GameAudio.cs](GameAudio.cs) loads the three original WAV effects from [Assets/Audio](Assets/Audio). A low paddle thock, bright wall tick, and glass-like brick crack play when the simulation reports the corresponding hit.

To experiment, change `PaddleSpeed` or `PaddleWidth` in `BreakoutGame.cs`. For ball difficulty, adjust `StartBallSpeed` or `SpeedGainPerBrick`, both measured in pixels per second. Change the nine entries in `BrickPalette` in `GameRenderer.cs` to try different colors.

## Publish for Windows x64

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

Open `Breakout.exe` in `bin/Release/net10.0/win-x64/publish`. To share the game, copy the entire publish folder so the executable keeps its native libraries and WAV files. The self-contained folder includes the .NET runtime.
