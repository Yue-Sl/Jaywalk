using Godot;
using System;
using System.Collections.Generic;

public partial class FroggerMain : Node2D
{
    private enum GameState { Title, Playing, Paused, GameOver }

    // ---- world layout (raw map units, map is 384x640) ----
    private const float MapW = 384f, MapH = 640f;
    private static readonly (float, int, float)[] LaneDefs = {
        (215f,  1, 62f),
        (271f, -1, 74f),
        (292f,  1, 55f),
        (313f, -1, 68f),
        (418f,  1, 80f),
        (439f, -1, 58f),
        (460f,  1, 72f),
        (481f, -1, 64f),
    };

    private GameState _state = GameState.Title;
    private Random _rng = new();
    private Node2D _world;
    private Node2D _carsRoot;
    private FroggerPlayer _player;
    private readonly List<Timer> _laneTimers = new();

    // layout (computed from viewport at runtime)
    private float _vh, _scale, _worldX, _worldW, _panelW, _rightX, _s;

    private int _score;
    private int _lives = 3;
    private int _level = 1;
    private int _highScore;
    private bool _goalLock;

    private AudioStreamPlayer _music;
    private AudioStreamPlayer _hitSfx;
    private AudioStreamPlayer _goalSfx;

    private Label _scoreLabel, _highLabel, _livesLabel, _levelLabel;
    private Label _banner;
    private Control _titleOverlay, _pausedOverlay, _gameOverOverlay;
    private Label _finalScoreLabel;

    public override void _Ready()
    {
        CenterWindow();
        var vp = GetViewportRect().Size;
        _vh = vp.Y;
        _scale = _vh / MapH;                 // map height always fits the window
        _worldW = MapW * _scale;
        _worldX = (vp.X - _worldW) / 2f;
        _panelW = _worldX;
        _rightX = _worldX + _worldW;
        _s = _vh / 1080f;                    // font scale

        _highScore = LoadHighScore();

        _music = MakePlayer("res://\u8bfe\u7a0b\u7d20\u6750/audio/S31-City on Speed.ogg", -14f, loop: true);
        _hitSfx = MakePlayer("res://\u8bfe\u7a0b\u7d20\u6750/audio/car.mp3", -4f, loop: false);
        _goalSfx = MakePlayer("res://\u8bfe\u7a0b\u7d20\u6750/audio/happy.mp3", -10f, loop: false);
        AddChild(_music); AddChild(_hitSfx); AddChild(_goalSfx);
        _music.Play();

        BuildWorld();
        BuildHud();
        ShowTitle();
    }

    private void CenterWindow()
    {
        // place the window nicely in the middle of the usable desktop area
        var win = GetWindow();
        var rect = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen());
        var wsize = win.Size;
        var pos = rect.Position + (rect.Size - wsize) / 2;
        if (pos.X < rect.Position.X) pos.X = rect.Position.X;
        if (pos.Y < rect.Position.Y) pos.Y = rect.Position.Y;
        win.Position = pos;
    }

    private AudioStreamPlayer MakePlayer(string path, float vol, bool loop)
    {
        var p = new AudioStreamPlayer();
        p.Stream = GD.Load<AudioStream>(path);
        p.VolumeDb = vol;
        if (loop) p.Finished += () => p.Play();
        return p;
    }

    private void BuildWorld()
    {
        _world = new Node2D { Position = new Vector2(_worldX, 0), Scale = new Vector2(_scale, _scale) };
        AddChild(_world);

        var bg = new Sprite2D { Texture = GD.Load<Texture2D>("res://\u8bfe\u7a0b\u7d20\u6750/graphics/map.png") };
        _world.AddChild(bg);

        _carsRoot = new Node2D { Name = "Cars" };
        _world.AddChild(_carsRoot);

        _player = new FroggerPlayer { Name = "Player" };
        _world.AddChild(_player);
        _player.ResetToStart();
        _player.Killed += OnPlayerKilled;
        _player.ReachedGoal += OnReachedGoal;
        _player.MovedUp += OnMovedUp;

        var finish = new Area2D { Name = "Finish" };
        finish.CollisionLayer = 4;
        finish.CollisionMask = 1;
        var fs = new CollisionShape2D();
        fs.Shape = new RectangleShape2D { Size = new Vector2(304, 104) };
        finish.AddChild(fs);
        finish.Position = new Vector2(192, 52);
        finish.BodyEntered += body => { if (body == _player) _player.EmitSignal(FroggerPlayer.SignalName.ReachedGoal); };
        _world.AddChild(finish);

        foreach (var (y, _, _) in LaneDefs)
        {
            var t = new Timer { WaitTime = RandSpawnInterval() };
            t.Timeout += () => SpawnCar(y);
            AddChild(t);
            _laneTimers.Add(t);
        }
    }

    private Label L(string text, Font font, float sizeK, Color color, float x, float y, float w = -1, float h = -1)
    {
        var l = new Label { Text = text };
        l.AddThemeFontOverride("font", font);
        l.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(sizeK * _s));
        l.AddThemeColorOverride("font_color", color);
        l.Position = new Vector2(x, y);
        if (w > 0)
        {
            l.Size = new Vector2(w, h > 0 ? h : 0);
            l.HorizontalAlignment = HorizontalAlignment.Center;
        }
        return l;
    }

    private void BuildHud()
    {
        var font = GD.Load<FontFile>("res://\u8bfe\u7a0b\u7d20\u6750/fonts/Better VCR 9.0.1.ttf");
        var fontTitle = GD.Load<FontFile>("res://\u8bfe\u7a0b\u7d20\u6750/fonts/Crackman.otf");
        float pw = _panelW, rxp = _rightX;

        var ui = new CanvasLayer();
        AddChild(ui);

        // ---- left panel ----
        var left = new ColorRect { Color = new Color(0.08f, 0.09f, 0.16f, 0.96f) };
        left.Position = new Vector2(0, 0);
        left.Size = new Vector2(pw, _vh);
        ui.AddChild(left);

        left.AddChild(L("FROGGER", fontTitle, 86, new Color(0.95f, 0.82f, 0.35f), pw * 0.07f, _vh * 0.06f));
        left.AddChild(L("a little whale's road trip", font, 24, new Color(0.6f, 0.9f, 1f), pw * 0.075f, _vh * 0.175f));
        _scoreLabel = L("SCORE  000000", font, 38, Colors.White, pw * 0.07f, _vh * 0.30f);
        left.AddChild(_scoreLabel);
        _highLabel = L("BEST   000000", font, 32, new Color(0.6f, 0.9f, 1f), pw * 0.07f, _vh * 0.375f);
        left.AddChild(_highLabel);
        _livesLabel = L("LIVES  \u2665\u2665\u2665", font, 32, new Color(1f, 0.4f, 0.45f), pw * 0.07f, _vh * 0.45f);
        left.AddChild(_livesLabel);
        _levelLabel = L("LEVEL  1", font, 32, new Color(0.75f, 1f, 0.6f), pw * 0.07f, _vh * 0.525f);
        left.AddChild(_levelLabel);
        left.AddChild(L("ESC = pause", font, 26, new Color(0.8f, 0.8f, 0.9f), pw * 0.07f, _vh * 0.92f));

        // ---- right panel ----
        var right = new ColorRect { Color = new Color(0.08f, 0.09f, 0.16f, 0.96f) };
        right.Position = new Vector2(rxp, 0);
        right.Size = new Vector2(pw, _vh);
        ui.AddChild(right);

        right.AddChild(L("HOW TO PLAY", fontTitle, 58, new Color(0.95f, 0.82f, 0.35f), pw * 0.09f, _vh * 0.065f));

        string[] lines = {
            "ARROWS / WASD  to move",
            "",
            "Cross all 8 lanes of",
            "traffic and reach the",
            "green field up top!",
            "",
            "Every step forward  +10",
            "Reach the top      +500",
            "",
            "Cars get faster every",
            "level. You have 3 lives!",
        };
        for (int i = 0; i < lines.Length; i++)
            right.AddChild(L(lines[i], font, 26, Colors.White, pw * 0.09f, _vh * 0.20f + i * _vh * 0.042f));

        right.AddChild(L("~ a whale easter egg is hidden ~", font, 22, new Color(0.5f, 0.8f, 1f, 0.9f), pw * 0.09f, _vh * 0.90f));

        // ---- banner ----
        _banner = L("", fontTitle, 72, new Color(0.95f, 0.82f, 0.35f), _worldX, _vh * 0.22f, _worldW, _vh * 0.12f);
        _banner.Visible = false;
        ui.AddChild(_banner);

        // ---- title overlay (covers right panel only) ----
        _titleOverlay = new ColorRect { Color = new Color(0.04f, 0.05f, 0.1f, 0.85f) };
        _titleOverlay.Position = new Vector2(rxp, 0);
        _titleOverlay.Size = new Vector2(pw, _vh);
        ui.AddChild(_titleOverlay);
        _titleOverlay.AddChild(L("FROGGER", fontTitle, 84, new Color(0.95f, 0.82f, 0.35f), pw * 0.03f, _vh * 0.28f, pw * 0.94f, _vh * 0.14f));
        _titleOverlay.AddChild(L("Press ENTER to start", font, 34, Colors.White, pw * 0.03f, _vh * 0.46f, pw * 0.94f, _vh * 0.06f));
        _titleOverlay.AddChild(L("help the little whale", font, 26, new Color(0.6f, 0.9f, 1f), pw * 0.03f, _vh * 0.55f, pw * 0.94f, _vh * 0.05f));
        _titleOverlay.AddChild(L("cross the road!", font, 26, new Color(0.6f, 0.9f, 1f), pw * 0.03f, _vh * 0.60f, pw * 0.94f, _vh * 0.05f));

        // ---- paused overlay ----
        _pausedOverlay = new ColorRect { Color = new Color(0.04f, 0.05f, 0.1f, 0.75f) };
        _pausedOverlay.Position = new Vector2(rxp, 0);
        _pausedOverlay.Size = new Vector2(pw, _vh);
        _pausedOverlay.Visible = false;
        ui.AddChild(_pausedOverlay);
        _pausedOverlay.AddChild(L("PAUSED", fontTitle, 84, Colors.White, pw * 0.03f, _vh * 0.44f, pw * 0.94f, _vh * 0.14f));

        // ---- game over overlay ----
        _gameOverOverlay = new ColorRect { Color = new Color(0.08f, 0.02f, 0.04f, 0.92f) };
        _gameOverOverlay.Position = new Vector2(rxp, 0);
        _gameOverOverlay.Size = new Vector2(pw, _vh);
        _gameOverOverlay.Visible = false;
        ui.AddChild(_gameOverOverlay);
        _gameOverOverlay.AddChild(L("GAME", fontTitle, 82, new Color(1f, 0.35f, 0.4f), pw * 0.03f, _vh * 0.26f, pw * 0.94f, _vh * 0.12f));
        _gameOverOverlay.AddChild(L("OVER", fontTitle, 82, new Color(1f, 0.35f, 0.4f), pw * 0.03f, _vh * 0.36f, pw * 0.94f, _vh * 0.12f));
        _finalScoreLabel = L("FINAL SCORE  000000", font, 32, Colors.White, pw * 0.03f, _vh * 0.52f, pw * 0.94f, _vh * 0.06f);
        _gameOverOverlay.AddChild(_finalScoreLabel);
        _gameOverOverlay.AddChild(L("Press ENTER to restart", font, 28, new Color(0.6f, 0.9f, 1f), pw * 0.03f, _vh * 0.63f, pw * 0.94f, _vh * 0.05f));
    }

    // ---------- spawning ----------
    private float RandSpawnInterval()
    {
        double d = _rng.NextDouble();
        float baseInt = Mathf.Lerp(2.2f, 1.1f, (float)d);
        float speedUp = 1.0f + 0.10f * (_level - 1);
        return Mathf.Max(baseInt / speedUp, 0.45f);
    }

    private void SpawnCar(float laneY)
    {
        int idx = -1;
        for (int i = 0; i < LaneDefs.Length; i++) if (LaneDefs[i].Item1 == laneY) { idx = i; break; }
        if (idx < 0) return;
        var (_, dir, speed) = LaneDefs[idx];

        var car = new FroggerCar();
        car.Position = new Vector2(dir > 0 ? -24f : 424f, laneY);
        car.Setup(speed, dir, _level);
        _carsRoot.AddChild(car);
        car.BodyEntered += OnCarHit;
    }

    private void OnCarHit(Node2D body)
    {
        if (body == _player && _state == GameState.Playing)
            _player.EmitSignal(FroggerPlayer.SignalName.Killed);
    }

    // ---------- events ----------
    private void OnMovedUp()
    {
        if (_state != GameState.Playing) return;
        _score += 10;
        RefreshHud();
    }

    private void OnPlayerKilled()
    {
        if (_state != GameState.Playing) return;
        _hitSfx.Play();
        _lives--;
        RefreshHud();
        if (_lives <= 0) GameOver();
        else
        {
            _player.ResetToStart();
            FlashBanner($"OUCH! {_lives} lives left", 0.9f);
        }
    }

    private void OnReachedGoal()
    {
        if (_state != GameState.Playing || _goalLock) return;
        _goalLock = true;
        _goalSfx.Play();
        _score += 500;
        if (_score > _highScore) { _highScore = _score; SaveHighScore(); }
        _level++;
        RefreshHud();
        FlashBanner($"LEVEL {_level}!", 1.4f);
        _player.ResetToStart();
        var t = GetTree().CreateTimer(0.8);
        t.Timeout += () => _goalLock = false;
    }

    private void GameOver()
    {
        _state = GameState.GameOver;
        _player.InputEnabled = false;
        if (_score > _highScore) { _highScore = _score; SaveHighScore(); }
        _finalScoreLabel.Text = $"FINAL SCORE  {_score:000000}";
        _gameOverOverlay.Visible = true;
    }

    private void FlashBanner(string text, float sec)
    {
        _banner.Text = text;
        _banner.Visible = true;
        _banner.Modulate = new Color(1, 1, 1, 1);
        var t = CreateTween();
        t.TweenInterval(sec);
        t.TweenProperty(_banner, "modulate:a", 0f, 0.5);
        t.TweenCallback(Callable.From(() => _banner.Visible = false));
    }

    private void StartGame()
    {
        _score = 0; _lives = 3; _level = 1; _goalLock = false;
        _player.InputEnabled = true;
        _player.ResetToStart();
        foreach (var t in _laneTimers) { t.WaitTime = RandSpawnInterval(); t.Start(); }
        RefreshHud();
        _state = GameState.Playing;
        _titleOverlay.Visible = false;
        _gameOverOverlay.Visible = false;
        _pausedOverlay.Visible = false;
        if (!_music.Playing) _music.Play();
    }

    private void ShowTitle()
    {
        _state = GameState.Title;
        _player.InputEnabled = false;
        _player.ResetToStart();
        _titleOverlay.Visible = true;
        _gameOverOverlay.Visible = false;
        _pausedOverlay.Visible = false;
        RefreshHud();
    }

    private void RefreshHud()
    {
        _scoreLabel.Text = $"SCORE  {_score:000000}";
        _highLabel.Text = $"BEST   {_highScore:000000}";
        _livesLabel.Text = "LIVES  " + new string('\u2665', Math.Max(_lives, 0));
        _levelLabel.Text = $"LEVEL  {_level}";
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed && !k.Echo)
        {
            if (k.PhysicalKeycode == Key.Escape)
            {
                if (_state == GameState.Playing) { _state = GameState.Paused; _pausedOverlay.Visible = true; }
                else if (_state == GameState.Paused) { _state = GameState.Playing; _pausedOverlay.Visible = false; }
                return;
            }
            if (k.PhysicalKeycode == Key.Enter || k.PhysicalKeycode == Key.Space)
            {
                if (_state == GameState.Title || _state == GameState.GameOver) StartGame();
            }
        }
    }

    private int LoadHighScore()
    {
        using var f = FileAccess.Open("user://frogger_highscore.txt", FileAccess.ModeFlags.Read);
        if (f == null) return 0;
        return (int)f.Get32();
    }

    private void SaveHighScore()
    {
        using var f = FileAccess.Open("user://frogger_highscore.txt", FileAccess.ModeFlags.Write);
        if (f == null) return;
        f.Store32((uint)_highScore);
    }
}
