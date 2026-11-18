using Godot;
using RacePoker.Core.Models;

namespace RacePoker.Godot;

public partial class RaceTrack : Control
{
    private RaceLineup _lineup = new(1, true);
    private readonly AnimatedSprite2D[] _horses = new AnimatedSprite2D[4];
    private readonly CpuParticles2D[] _dust = new CpuParticles2D[4];
    private readonly ShaderMaterial[] _glow = new ShaderMaterial[4];
    private readonly Tween?[] _movement = new Tween?[4];
    private readonly float[] _scores = new float[4], _boost = new float[4];
    private readonly int[] _frames = new int[4];
    private float _finish = 150000, _sceneryOffset;
    private bool _paused, _racing;
    private int _activeSeat;
    public int LaneCount => _lineup.RacerCount;
    public float DisplayScore => Score(_lineup.FirstHumanSeatId);
    public float RivalDisplayScore => _lineup.HasComputer ? Score(0) : 0;
    public int AnimationFrameChanges => _frames[_lineup.FirstHumanSeatId];
    public int RivalAnimationFrameChanges => _lineup.HasComputer ? _frames[0] : 0;
    public float Score(int seatId) => _scores[seatId];

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ClipContents = true;
        var sheet = GD.Load<Texture2D>("res://Assets/horse-gallop.png");
        var cell = new Vector2(sheet.GetWidth() / 4f, sheet.GetHeight() / 2f);
        var frames = new SpriteFrames();
        frames.RemoveAnimation("default");
        frames.AddAnimation("gallop");
        frames.SetAnimationSpeed("gallop", 12);
        for (int i = 0; i < 8; i++) frames.AddFrame("gallop", new AtlasTexture
        {
            Atlas = sheet, Region = new Rect2(new Vector2(i % 4, i / 4) * cell, cell), FilterClip = true
        });
        frames.AddAnimation("idle");
        frames.AddFrame("idle", frames.GetFrameTexture("gallop", 4));
        for (int i = 0; i < 4; i++)
        {
            int slot = i;
            var fade = new Gradient
            {
                Colors = new[] { new Color(0.82f, 0.69f, 0.43f, 0.5f), new Color(0.82f, 0.69f, 0.43f, 0) },
                Offsets = new[] { 0f, 1f }
            };
            _dust[i] = new CpuParticles2D
            {
                Emitting = false, Amount = 16, Lifetime = 0.65, LocalCoords = false,
                Direction = new Vector2(-1, -0.35f), Spread = 25,
                InitialVelocityMin = 30, InitialVelocityMax = 70, Gravity = new Vector2(0, -12),
                ScaleAmountMin = 0.25f, ScaleAmountMax = 0.65f,
                Texture = new GradientTexture2D
                {
                    Gradient = fade, Width = 24, Height = 24, Fill = GradientTexture2D.FillEnum.Radial,
                    FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1, 0.5f)
                }
            };
            AddChild(_dust[i]);
            _glow[i] = new ShaderMaterial
            {
                Shader = new Shader { Code = "shader_type canvas_item; uniform float flash = 0.0; void fragment() { vec4 tex = texture(TEXTURE, UV); COLOR = vec4(mix(tex.rgb, vec3(1.0, 0.88, 0.48), flash * 0.45), tex.a); }" }
            };
            _horses[i] = new AnimatedSprite2D { SpriteFrames = frames, Animation = "idle", TextureFilter = TextureFilterEnum.Linear, Material = _glow[i] };
            _horses[i].FrameChanged += () => _frames[slot]++;
            AddChild(_horses[i]);
        }
        Configure(_lineup);
    }

    public void Configure(RaceLineup lineup)
    {
        _lineup = lineup;
        ResetRace();
        for (int i = 0; i < 4; i++)
        {
            _horses[i].Visible = _dust[i].Visible = i < LaneCount;
            // Separate materials preserve each racer's flash and colour independently.
            _glow[i].Shader.Code = "shader_type canvas_item; uniform float flash = 0.0; uniform vec4 tint : source_color = vec4(1.0); void fragment() { vec4 tex = texture(TEXTURE, UV); COLOR = vec4(mix(tex.rgb * tint.rgb, vec3(1.0, 0.88, 0.48), flash * 0.45), tex.a); }";
            _glow[i].SetShaderParameter("tint", LaneColor(i));
        }
    }

    private Color LaneColor(int seat)
    {
        if (_lineup.IsComputer(seat)) return Color.FromHtml("#8FC5FF");
        int human = seat - _lineup.FirstHumanSeatId;
        return Color.FromHtml(new[] { "#FFFFFF", "#FFC2AD", "#FFE5A1", "#B8E8FF" }[Math.Clamp(human, 0, 3)]);
    }

    public void ResetRace()
    {
        for (int i = 0; i < 4; i++)
        {
            _movement[i]?.Kill();
            _scores[i] = _boost[i] = 0;
            _frames[i] = 0;
            _horses[i]?.Stop();
            if (_dust[i] != null) { _dust[i].Emitting = false; _dust[i].SpeedScale = 1; _dust[i].Restart(); }
        }
        _paused = _racing = false;
        _sceneryOffset = 0;
        QueueRedraw();
    }

    public void MoveSeatToScore(int seat, int score, int finish)
    {
        _finish = finish;
        _boost[seat] = 1;
        _movement[seat]?.Kill();
        _movement[seat] = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _movement[seat]!.TweenMethod(Callable.From<float>(v => _scores[seat] = v), _scores[seat], (float)score, 1.4);
    }

    public void SetActiveSeat(int seat) => _activeSeat = seat;
    public void SetRacing(bool racing) => _racing = racing;
    public void SetPaused(bool paused)
    {
        _paused = paused;
        foreach (var tween in _movement)
            if (tween?.IsValid() == true) { if (paused) tween.Pause(); else tween.Play(); }
        UpdateAnimations();
    }

    private void UpdateAnimations()
    {
        for (int i = 0; i < 4; i++)
        {
            bool moving = _movement[i]?.IsRunning() == true;
            bool running = i < LaneCount && (_racing || moving);
            _horses[i].SpeedScale = moving ? 1.3f : 0.8f;
            if (_paused) _horses[i].Pause();
            else if (running) _horses[i].Play("gallop");
            else { _horses[i].Animation = "idle"; _horses[i].Stop(); }
            _dust[i].Emitting = running && !_paused;
            _dust[i].SpeedScale = _paused ? 0 : 1;
        }
    }

    public override void _Process(double delta)
    {
        float width = HorseWidth();
        if (!_paused)
        {
            for (int i = 0; i < 4; i++) _boost[i] = Math.Max(0, _boost[i] - (float)delta / 1.4f);
            if (_racing) _sceneryOffset = (_sceneryOffset + (float)delta * (85 + _boost.Max() * 240)) % 64;
        }
        for (int i = 0; i < LaneCount; i++)
        {
            _glow[i].SetShaderParameter("flash", _boost[i]);
            _horses[i].Scale = Vector2.One * (width / _horses[i].SpriteFrames.GetFrameTexture("gallop", 0).GetWidth());
            _horses[i].Position = new Vector2(HorseX(_scores[i]), GroundY(i) - width * 0.3f);
            _dust[i].Position = new Vector2(HorseX(_scores[i]) - width * 0.23f, GroundY(i));
        }
        UpdateAnimations();
        QueueRedraw();
    }

    public override void _Draw()
    {
        float w = Size.X, h = Size.Y, laneHeight = (h - 48) / LaneCount;
        DrawRect(new Rect2(0, 0, w, h), Color.FromHtml("#123F34"));
        for (float x = 150 - _sceneryOffset; x < w; x += 64)
            DrawRect(new Rect2(x, 24, 32, h - 48), Color.FromHtml("#174A3B"));
        for (int lane = 0; lane < LaneCount; lane++)
        {
            float top = 24 + lane * laneHeight;
            DrawRect(new Rect2(0, top, 150, laneHeight), Color.FromHtml("#102B2D"));
            if (_racing && lane == _activeSeat)
                DrawRect(new Rect2(0, top, w, laneHeight), new Color(1, 1, 1, 0.055f));
            DrawRect(new Rect2(150, GroundY(lane) - 3, Math.Max(0, w - 150), 10), Color.FromHtml("#766644"));
            DrawLine(new Vector2(0, top + laneHeight), new Vector2(w, top + laneHeight), Color.FromHtml("#A8C8BA"), 1);
            var color = _lineup.IsComputer(lane) ? Color.FromHtml("#8FC5FF") : LaneColor(lane);
            DrawString(ThemeDB.FallbackFont, new Vector2(12, top + laneHeight / 2 - 3), _lineup.Name(lane), fontSize: 12, modulate: color);
            DrawString(ThemeDB.FallbackFont, new Vector2(12, top + laneHeight / 2 + 16), $"{_scores[lane]:N0} PTS", fontSize: 14, modulate: Colors.White);
            if (_boost[lane] > 0)
                for (int streak = 0; streak < 3; streak++)
                {
                    float x = 160 + (streak * 137 + _sceneryOffset * 8) % Math.Max(1, w - 220);
                    float y = top + laneHeight * (0.2f + streak * 0.22f);
                    DrawLine(new Vector2(x, y), new Vector2(x + 35, y), new Color(0.96f, 0.86f, 0.58f, _boost[lane] * 0.22f), 2, true);
                }
            DrawSetTransform(new Vector2(HorseX(_scores[lane]), GroundY(lane)), 0, new Vector2(1, 0.15f));
            DrawCircle(Vector2.Zero, HorseWidth() * 0.37f, new Color(0, 0, 0, 0.25f));
            DrawSetTransform(Vector2.Zero);
        }
        DrawLine(new Vector2(150, 24), new Vector2(w, 24), Color.FromHtml("#A8C8BA"), 2);
        for (int row = 0; row < (h - 48) / 16; row++)
            for (int col = 0; col < 2; col++)
                DrawRect(new Rect2(w - 38 + col * 10, 24 + row * 16, 10, Math.Max(0, Math.Min(16, h - 24 - (24 + row * 16)))),
                    (row + col) % 2 == 0 ? Colors.White : Color.FromHtml("#132B2B"));
        DrawString(ThemeDB.FallbackFont, new Vector2(150, h - 5), "START", fontSize: 12, modulate: Color.FromHtml("#ADD0C7"));
        DrawString(ThemeDB.FallbackFont, new Vector2(w - 63, h - 5), "FINISH", fontSize: 12, modulate: Color.FromHtml("#F4CF75"));
    }

    private float HorseWidth() => Math.Min(150, (Size.Y - 48) / LaneCount * 1.5f);
    private float GroundY(int lane) => 24 + (Size.Y - 48) / LaneCount * (lane + 1) - 8;
    private float HorseX(float score)
    {
        float start = 160 + HorseWidth() / 2;
        return start + Math.Clamp(score / _finish, 0, 1) * Math.Max(0, Size.X - start - HorseWidth() / 2 - 42);
    }
}
