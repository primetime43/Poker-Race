using Godot;

namespace RacePoker.Godot;

public partial class RaceTrack : Control
{
    public float DisplayScore { get; private set; }
    public float RivalDisplayScore { get; private set; }
    public int RivalAnimationFrameChanges { get; private set; }
    private float _finish = 150000;
    public int AnimationFrameChanges { get; private set; }
    private AnimatedSprite2D _horse = null!;
    private AnimatedSprite2D _rival = null!;
    private CpuParticles2D _dust = null!;
    private Tween? _movement;
    private Tween? _rivalMovement;
    private bool _paused;
    private bool _racing;
    private float _sceneryOffset;
    private float _boost;
    private ShaderMaterial _horseGlow = null!;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ClipContents = true;
        var sheet = GD.Load<Texture2D>("res://Assets/horse-gallop.png");
        var frames = new SpriteFrames();
        frames.RemoveAnimation("default");
        frames.AddAnimation("gallop");
        frames.SetAnimationSpeed("gallop", 12);
        frames.SetAnimationLoop("gallop", true);
        var cell = new Vector2(sheet.GetWidth() / 4f, sheet.GetHeight() / 2f);
        for (int i = 0; i < 8; i++)
            frames.AddFrame("gallop", new AtlasTexture
            {
                Atlas = sheet, Region = new Rect2(new Vector2(i % 4, i / 4) * cell, cell),
                FilterClip = true
            });
        frames.AddAnimation("idle");
        frames.AddFrame("idle", frames.GetFrameTexture("gallop", 4));
        var fade = new Gradient
        {
            Colors = new[] { new Color(0.82f, 0.69f, 0.43f, 0.5f), new Color(0.82f, 0.69f, 0.43f, 0) },
            Offsets = new[] { 0f, 1f }
        };
        _dust = new CpuParticles2D
        {
            Emitting = false, Amount = 28, Lifetime = 0.65, LocalCoords = false,
            Direction = new Vector2(-1, -0.35f), Spread = 25,
            InitialVelocityMin = 30, InitialVelocityMax = 70, Gravity = new Vector2(0, -12),
            ScaleAmountMin = 0.3f, ScaleAmountMax = 0.85f,
            Texture = new GradientTexture2D
            {
                Gradient = fade, Width = 24, Height = 24,
                Fill = GradientTexture2D.FillEnum.Radial,
                FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1, 0.5f)
            }
        };
        AddChild(_dust);
        _horse = new AnimatedSprite2D
        {
            SpriteFrames = frames, Animation = "idle",
            Scale = Vector2.One * (150 / cell.X), TextureFilter = TextureFilterEnum.Linear
        };
        _horseGlow = new ShaderMaterial
        {
            Shader = new Shader
            {
                Code = "shader_type canvas_item; uniform float flash = 0.0; void fragment() { vec4 tex = texture(TEXTURE, UV); COLOR = vec4(mix(tex.rgb, vec3(1.0, 0.88, 0.48), flash * 0.45), tex.a); }"
            }
        };
        _horse.Material = _horseGlow;
        _horse.FrameChanged += () => AnimationFrameChanges++;
        _rival = new AnimatedSprite2D
        {
            SpriteFrames = frames, Animation = "idle", Scale = _horse.Scale,
            TextureFilter = TextureFilterEnum.Linear, Modulate = Color.FromHtml("#8FC5FF")
        };
        _rival.FrameChanged += () => RivalAnimationFrameChanges++;
        AddChild(_rival);
        AddChild(_horse);
    }

    public void ResetRace()
    {
        _movement?.Kill();
        _rivalMovement?.Kill();
        DisplayScore = 0;
        RivalDisplayScore = 0;
        _boost = _sceneryOffset = 0;
        _paused = false;
        _racing = false;
        AnimationFrameChanges = 0;
        RivalAnimationFrameChanges = 0;
        _horse?.Stop();
        _rival?.Stop();
        if (_dust != null) { _dust.Emitting = false; _dust.SpeedScale = 1; _dust.Restart(); }
        QueueRedraw();
    }

    public void MoveToScore(int score, int finish)
    {
        _finish = finish;
        _boost = 1;
        _movement?.Kill();
        _movement = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _movement.TweenMethod(Callable.From<float>(value => { DisplayScore = value; QueueRedraw(); }), DisplayScore, (float)score, 1.4);
    }

    public void SetRacing(bool racing)
    {
        _racing = racing;
        UpdateAnimation();
    }

    public void MoveRivalToScore(int score, int finish)
    {
        _finish = finish;
        _rivalMovement?.Kill();
        _rivalMovement = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _rivalMovement.TweenMethod(Callable.From<float>(value => RivalDisplayScore = value), RivalDisplayScore, (float)score, 1.4);
    }

    private void UpdateAnimation()
    {
        if (_horse == null) return;
        bool running = _racing || _movement?.IsRunning() == true;
        _horse.SpeedScale = _movement?.IsRunning() == true ? 1.3f : 0.8f;
        if (_paused) _horse.Pause();
        else if (running) _horse.Play("gallop");
        else { _horse.Animation = "idle"; _horse.Stop(); }
        _rival.SpeedScale = _rivalMovement?.IsRunning() == true ? 1.3f : 0.75f;
        if (_paused) _rival.Pause();
        else if (running || _rivalMovement?.IsRunning() == true) _rival.Play("gallop");
        else { _rival.Animation = "idle"; _rival.Stop(); }
        _dust.Emitting = running && !_paused;
        _dust.SpeedScale = _paused ? 0 : 1;
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
        foreach (var tween in new[] { _movement, _rivalMovement })
            if (tween?.IsValid() == true)
                if (paused) tween.Pause(); else tween.Play();
        UpdateAnimation();
    }

    public override void _Process(double delta)
    {
        if (!_paused)
        {
            _boost = Math.Max(0, _boost - (float)delta / 1.4f);
            if (_racing) _sceneryOffset = (_sceneryOffset + (float)delta * (85 + _boost * 240)) % 64;
        }
        _horseGlow?.SetShaderParameter("flash", _boost);
        UpdateAnimation();
        float position = HorseX(DisplayScore);
        if (_horse != null)
        {
            _horse.Position = new Vector2(position, GroundY(1) - 45);
            _rival.Position = new Vector2(HorseX(RivalDisplayScore), GroundY(0) - 45);
            _dust.Position = new Vector2(position - 35, GroundY(1));
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        float w = Size.X, h = Size.Y;
        DrawRect(new Rect2(0, 0, w, h), Color.FromHtml("#123F34"));
        for (float x = -_sceneryOffset; x < w; x += 64)
            DrawRect(new Rect2(x, 24, 32, h - 48), Color.FromHtml("#174A3B"));
        for (int lane = 0; lane < 2; lane++)
        {
            DrawRect(new Rect2(0, GroundY(lane) - 3, w, 16), Color.FromHtml("#766644"));
            DrawLine(new Vector2(0, GroundY(lane) + 16), new Vector2(w, GroundY(lane) + 16), Color.FromHtml("#A8C8BA"), 2);
        }
        if (_boost > 0)
            for (int i = 0; i < 9; i++)
            {
                float x = ((i * 137 + _sceneryOffset * 9) % (w + 80)) - 40;
                float y = 38 + i % 3 * 28;
                DrawLine(new Vector2(x, y), new Vector2(x + 25 + 45 * _boost, y),
                    new Color(0.96f, 0.86f, 0.58f, _boost * 0.22f), 2, true);
            }
        DrawLine(new Vector2(0, 24), new Vector2(w, 24), Color.FromHtml("#A8C8BA"), 3);
        DrawLine(new Vector2(0, h - 24), new Vector2(w, h - 24), Color.FromHtml("#A8C8BA"), 3);
        for (int y = 0; y < (h - 48) / 20; y++)
            for (int x = 0; x < 2; x++)
            {
                float rowHeight = Math.Max(0, Math.Min(20, h - 24 - (25 + y * 20)));
                DrawRect(new Rect2(w - 44 + x * 12, 25 + y * 20, 12, rowHeight),
                    (x + y) % 2 == 0 ? Colors.White : Color.FromHtml("#132B2B"));
            }
        // The shadow stays on the ground while the sprite's legs cycle above it.
        for (int lane = 0; lane < 2; lane++)
        {
            DrawSetTransform(new Vector2(HorseX(lane == 0 ? RivalDisplayScore : DisplayScore), GroundY(lane)), 0, new Vector2(1, 0.15f));
            DrawCircle(Vector2.Zero, 56, new Color(0, 0, 0, 0.25f));
        }
        DrawSetTransform(Vector2.Zero);
        DrawString(ThemeDB.FallbackFont, new Vector2(12, 18), $"COMPUTER   {RivalDisplayScore:N0} PTS", fontSize: 12, modulate: Color.FromHtml("#8FC5FF"));
        DrawString(ThemeDB.FallbackFont, new Vector2(12, h / 2 + 18), $"YOU   {DisplayScore:N0} PTS", fontSize: 12, modulate: Color.FromHtml("#77E4BD"));
        DrawString(ThemeDB.FallbackFont, new Vector2(12, h - 5), "START", fontSize: 12, modulate: Color.FromHtml("#ADD0C7"));
        DrawString(ThemeDB.FallbackFont, new Vector2(w - 63, h - 5), "FINISH", fontSize: 12, modulate: Color.FromHtml("#F4CF75"));
    }

    private float GroundY(int lane) => 24 + (Size.Y - 48) / 2 * (lane + 1) - 13;
    private float HorseX(float score) => 92 + Math.Clamp(score / _finish, 0, 1) * Math.Max(0, Size.X - 215);
}
