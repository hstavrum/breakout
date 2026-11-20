using System.Numerics;
using Raylib_cs;

namespace Breakout;

internal enum GameState
{
    Title,
    Playing,
    ReadyToServe,
    Won,
    Lost
}

[Flags]
internal enum Impact
{
    None = 0,
    Wall = 1,
    Paddle = 2,
    Brick = 4
}

internal sealed class Brick(Rectangle bounds, int row, int column)
{
    public Rectangle Bounds { get; } = bounds;
    public int Row { get; } = row;
    public int Column { get; } = column;
    public bool IsBroken { get; set; }
}

/// <summary>
/// Owns the rules and positions of a round. The renderer and audio player read
/// its output, so a simulation step can also be run without opening a window.
/// </summary>
internal sealed class BreakoutGame
{
    public const int Width = 1100;
    public const int Height = 800;
    public const int HudHeight = 64;
    public const int Rows = 5;
    public const int Columns = 9;
    public const float BallRadius = 10f;
    public const float StepSeconds = 1f / 240f;

    private const int StartingLives = 3;
    private const int PointsPerBrick = 100;
    private const int BrickSideMargin = 54;
    private const int BrickTop = 101;
    private const int BrickHeight = 35;
    private const int BrickGapX = 10;
    private const int BrickGapY = 12;
    private const int PaddleWidth = 204;
    private const int PaddleHeight = 20;
    private const int PaddleY = 757;
    private const float PaddleSpeed = 760f;
    private const float StartBallSpeed = 465f;
    private const float SpeedGainPerBrick = 9f;
    private const float MaxPaddleAngle = MathF.PI * 0.35f;
    private const float MinimumVerticalFraction = 0.22f;
    private const float ContactSeparation = 0.02f;

    private readonly Brick[] bricks = CreateBricks();
    private Vector2 ballVelocity;
    private float paddleX = (Width - PaddleWidth) / 2f;
    private int bricksRemaining = Rows * Columns;

    public BreakoutGame()
    {
        PlaceBallAbovePaddle();
    }

    public GameState State { get; private set; } = GameState.Title;
    public int Lives { get; private set; } = StartingLives;
    public int Score { get; private set; }
    public Vector2 BallPosition { get; private set; }
    public Rectangle Paddle => new(paddleX, PaddleY, PaddleWidth, PaddleHeight);
    public IReadOnlyList<Brick> Bricks => bricks;

    // The number of broken bricks survives a missed ball, so later serves
    // start at the speed the player has reached in this round.
    private float CurrentBallSpeed => StartBallSpeed + (bricks.Length - bricksRemaining) * SpeedGainPerBrick;

    /// <summary>Starts a round, serves a waiting ball, or restarts a finished round.</summary>
    public void PressEnter()
    {
        switch (State)
        {
            case GameState.Won:
            case GameState.Lost:
                ResetRound();
                Serve();
                break;
            case GameState.Title:
            case GameState.ReadyToServe:
                Serve();
                break;
        }
    }

    /// <summary>
    /// Advances the game by StepSeconds. Movement is -1 for left, 0 for still,
    /// and 1 for right. Returns the surfaces hit during this step.
    /// </summary>
    public Impact Step(int movement)
    {
        if (State is not (GameState.Playing or GameState.ReadyToServe))
        {
            return Impact.None;
        }

        float previousPaddleX = paddleX;
        paddleX = Math.Clamp(
            paddleX + movement * PaddleSpeed * StepSeconds,
            0f,
            Width - PaddleWidth);

        if (State == GameState.ReadyToServe)
        {
            PlaceBallAbovePaddle();
            return Impact.None;
        }

        // Positions are measured from the top left. Positive Y moves downward.
        // Velocity is in pixels per second, so multiply by the step length in seconds.
        Vector2 previousBall = BallPosition;
        BallPosition += ballVelocity * StepSeconds;

        Impact impacts = BounceOffWalls();
        if (BounceOffPaddle(previousBall, previousPaddleX))
        {
            impacts |= Impact.Paddle;
        }

        if (BounceOffBrick())
        {
            impacts |= Impact.Brick;
        }

        if (BallPosition.Y - BallRadius > Height)
        {
            LoseLife();
        }

        return impacts;
    }

    private static Brick[] CreateBricks()
    {
        var board = new Brick[Rows * Columns];
        float brickWidth = (Width - 2 * BrickSideMargin - (Columns - 1) * BrickGapX) / (float)Columns;

        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                float left = BrickSideMargin + column * (brickWidth + BrickGapX);
                float top = BrickTop + row * (BrickHeight + BrickGapY);
                board[row * Columns + column] = new Brick(
                    new Rectangle(left, top, brickWidth, BrickHeight), row, column);
            }
        }

        return board;
    }

    private void ResetRound()
    {
        foreach (Brick brick in bricks)
        {
            brick.IsBroken = false;
        }

        bricksRemaining = bricks.Length;
        Score = 0;
        Lives = StartingLives;
        paddleX = (Width - PaddleWidth) / 2f;
        PlaceBallAbovePaddle();
    }

    private void PlaceBallAbovePaddle()
    {
        BallPosition = new Vector2(paddleX + PaddleWidth / 2f, PaddleY - BallRadius - 14f);
    }

    private void Serve()
    {
        // Aim toward the board when serving from an edge.
        float horizontal = BallPosition.X > Width / 2f ? -0.35f : 0.35f;
        ballVelocity = Vector2.Normalize(new Vector2(horizontal, -1f)) * CurrentBallSpeed;
        State = GameState.Playing;
    }

    private void LoseLife()
    {
        Lives--;
        ballVelocity = Vector2.Zero;
        State = Lives == 0 ? GameState.Lost : GameState.ReadyToServe;

        if (State == GameState.ReadyToServe)
        {
            PlaceBallAbovePaddle();
        }
    }

    private Impact BounceOffWalls()
    {
        Vector2 position = BallPosition;
        float left = BallRadius;
        float right = Width - BallRadius;
        float top = HudHeight + BallRadius;
        bool hitSide = (position.X < left && ballVelocity.X < 0f)
            || (position.X > right && ballVelocity.X > 0f);
        bool hitTop = position.Y < top && ballVelocity.Y < 0f;

        // Place the ball back inside the playfield before turning it around.
        // The bottom is open, so crossing it costs a life instead.
        BallPosition = new Vector2(Math.Clamp(position.X, left, right), Math.Max(position.Y, top));
        if (hitSide)
        {
            ballVelocity.X = -ballVelocity.X;
        }

        if (hitTop)
        {
            ballVelocity.Y = -ballVelocity.Y;
        }

        return hitSide || hitTop ? Impact.Wall : Impact.None;
    }

    private bool BounceOffPaddle(Vector2 previousBall, float previousPaddleX)
    {
        float contactY = PaddleY - BallRadius;
        if (ballVelocity.Y <= 0f || previousBall.Y > contactY || BallPosition.Y < contactY)
        {
            return false;
        }

        // Find where the ball crossed the paddle's top during this step. The
        // paddle may also have moved, so interpolate its position at the same time.
        float fraction = (contactY - previousBall.Y) / (BallPosition.Y - previousBall.Y);
        float ballX = float.Lerp(previousBall.X, BallPosition.X, fraction);
        float paddleAtContact = float.Lerp(previousPaddleX, paddleX, fraction);
        float halfWidth = PaddleWidth / 2f;
        float offset = ballX - (paddleAtContact + halfWidth);

        if (Math.Abs(offset) > halfWidth + BallRadius)
        {
            return false;
        }

        // A center hit goes straight up. A hit near an edge aims farther outward.
        float angle = Math.Clamp(offset / halfWidth, -1f, 1f) * MaxPaddleAngle;
        float speed = ballVelocity.Length();
        ballVelocity = new Vector2(MathF.Sin(angle), -MathF.Cos(angle)) * speed;
        BallPosition = new Vector2(ballX, contactY - ContactSeparation);
        return true;
    }

    private bool BounceOffBrick()
    {
        foreach (Brick brick in bricks)
        {
            if (brick.IsBroken || !TryGetBrickContact(brick.Bounds, out Vector2 normal, out float overlap))
            {
                continue;
            }

            // A negative dot product means the ball is moving toward the face.
            // Moving away from a brick that overlaps the ball is not another hit.
            if (Vector2.Dot(ballVelocity, normal) >= 0f)
            {
                continue;
            }

            BallPosition += normal * (overlap + ContactSeparation);
            ballVelocity = Vector2.Reflect(ballVelocity, normal);

            brick.IsBroken = true;
            Score += PointsPerBrick;
            bricksRemaining--;
            AdjustBallAfterBrickHit();

            if (bricksRemaining == 0)
            {
                State = GameState.Won;
            }

            // Resolve one brick per small step. Another brick at the same seam
            // can be hit on the next step if the ball is heading toward it.
            return true;
        }

        return false;
    }

    private bool TryGetBrickContact(Rectangle brick, out Vector2 normal, out float overlap)
    {
        // Clamp the center to the rectangle to find the closest point. Near a
        // face this produces an axis-aligned normal, and at a corner it is diagonal.
        Vector2 nearest = Vector2.Clamp(
            BallPosition,
            new Vector2(brick.X, brick.Y),
            new Vector2(brick.X + brick.Width, brick.Y + brick.Height));
        Vector2 outward = BallPosition - nearest;
        float distanceSquared = outward.LengthSquared();
        normal = Vector2.Zero;
        overlap = 0f;

        if (distanceSquared > BallRadius * BallRadius)
        {
            return false;
        }

        if (distanceSquared > 0f)
        {
            float distance = MathF.Sqrt(distanceSquared);
            normal = outward / distance;
            overlap = BallRadius - distance;
            return true;
        }

        // If the center is inside, the closest point equals the center. Use
        // the nearest face to recover without dividing by a distance of zero.
        float left = BallPosition.X - brick.X;
        float right = brick.X + brick.Width - BallPosition.X;
        float top = BallPosition.Y - brick.Y;
        float bottom = brick.Y + brick.Height - BallPosition.Y;
        float nearestFace = Math.Min(Math.Min(left, right), Math.Min(top, bottom));
        if (nearestFace == left)
        {
            normal = -Vector2.UnitX;
        }
        else if (nearestFace == right)
        {
            normal = Vector2.UnitX;
        }
        else
        {
            normal = nearestFace == top ? -Vector2.UnitY : Vector2.UnitY;
        }

        overlap = BallRadius + nearestFace;
        return true;
    }

    private void AdjustBallAfterBrickHit()
    {
        Vector2 direction = Vector2.Normalize(ballVelocity);

        // Very shallow shots can bounce between walls for too long. Give them a
        // minimum vertical component while keeping the direction a unit vector.
        if (Math.Abs(direction.Y) < MinimumVerticalFraction)
        {
            direction.Y = MathF.CopySign(MinimumVerticalFraction, direction.Y);
            direction.X = MathF.CopySign(MathF.Sqrt(1f - direction.Y * direction.Y), direction.X);
        }

        ballVelocity = direction * CurrentBallSpeed;
    }
}
