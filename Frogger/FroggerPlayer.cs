using Godot;
using System;

public partial class FroggerPlayer : CharacterBody2D
{
    [Signal] public delegate void KilledEventHandler();
    [Signal] public delegate void ReachedGoalEventHandler();
    [Signal] public delegate void MovedUpEventHandler();

    public const float Grid = 21f;
    public bool InputEnabled = false;

    private AnimatedSprite2D _anim;
    private const float MinX = 24f, MaxX = 360f, MinY = 58f, MaxY = 618f;
    public float StartX = 192f, StartY = 585f;
    private bool _busy = false;

    public override void _Ready()
    {
        _anim = new AnimatedSprite2D();
        var sheet = GD.Load<Texture2D>("res://课程素材/graphics/player/player_tileset.png");
        var sf = new SpriteFrames();
        // columns: 0 = left, 16 = down, 32 = up  (each 16x16, 4 columns * 3 rows)
        AddAnim(sf, "down", sheet, 16, 0);
        AddAnim(sf, "left", sheet, 0, 0);
        AddAnim(sf, "up", sheet, 32, 0);
        _anim.SpriteFrames = sf;
        _anim.Play("down");
        _anim.AnimationFinished += () => { if (_anim.Animation != "down") _anim.Play("down"); };
        AddChild(_anim);

        var shape = new CollisionShape2D();
        shape.Shape = new CircleShape2D { Radius = 7f };
        AddChild(shape);
        CollisionLayer = 1;
        CollisionMask = 0;
    }

    private void AddAnim(SpriteFrames sf, string name, Texture2D sheet, int colX, int rowY)
    {
        sf.AddAnimation(name);
        sf.SetAnimationSpeed(name, 6f);
        for (int r = 0; r < 3; r++)
        {
            var at = new AtlasTexture
            {
                Atlas = sheet,
                Region = new Rect2(colX, rowY + r * 16, 16, 16)
            };
            sf.AddFrame(name, at);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!InputEnabled || _busy) return;
        if (@event is InputEventKey k && k.Pressed && !k.Echo)
        {
            switch (k.PhysicalKeycode)
            {
                case Key.Up: case Key.W: TryMove(new Vector2(0, -1)); break;
                case Key.Down: case Key.S: TryMove(new Vector2(0, 1)); break;
                case Key.Left: case Key.A: TryMove(new Vector2(-1, 0)); break;
                case Key.Right: case Key.D: TryMove(new Vector2(1, 0)); break;
            }
        }
    }

    private void TryMove(Vector2 dir)
    {
        var target = Position + dir * Grid;
        if (target.X < MinX || target.X > MaxX) return;
        if (target.Y < MinY || target.Y > MaxY)
        {
            // tried to leave the map (bottom/top): just block but allow top into finish zone check
            if (target.Y < MinY && Position.Y > MinY) { /* still allow finishing via area */ }
            else return;
        }
        if (dir.Y < 0) EmitSignal(SignalName.MovedUp);
        if (dir.X > 0) _anim.FlipH = true; else if (dir.X < 0) _anim.FlipH = false;

        _anim.Play(dir.X != 0 ? "left" : (dir.Y < 0 ? "up" : "down"));
        _busy = true;
        var t = CreateTween();
        t.TweenProperty(this, "position", target, 0.11).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        t.Parallel().TweenProperty(this, "scale", new Vector2(1.18f, 0.82f), 0.06)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        t.Chain().TweenProperty(this, "scale", Vector2.One, 0.08).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
        t.TweenCallback(Callable.From(() => { _busy = false; }));
    }

    public void ResetToStart()
    {
        _busy = false;
        Position = new Vector2(StartX, StartY);
        _anim.Play("down");
    }
}
