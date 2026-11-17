using Godot;

namespace RacePoker.Godot;

/// <summary>Presentation-only animations. Game rules remain in RacePoker.Core.</summary>
public partial class ArcadeEffects : Control
{
    private readonly List<Tween> _tweens = new();
    private readonly Dictionary<Control, Tween> _hover = new();
    private CpuParticles2D? _confetti;
    private bool _paused;
    public int CardsFlown { get; private set; }
    public int RewardsShown { get; private set; }
    public int CelebrationsShown { get; private set; }
    public int ComputerCardsRevealed { get; private set; }
    public float CardFlightProgress { get; private set; }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 30;
    }

    private Tween Animate()
    {
        _tweens.RemoveAll(t => !t.IsValid());
        var tween = CreateTween();
        _tweens.Add(tween);
        if (_paused) tween.Pause();
        return tween;
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
        foreach (var tween in _tweens.Where(t => t.IsValid()))
            if (paused) tween.Pause(); else tween.Play();
        if (_confetti != null) _confetti.SpeedScale = paused ? 0 : 1;
    }

    public void Clear()
    {
        foreach (var tween in _tweens.Where(t => t.IsValid())) tween.Kill();
        _tweens.Clear();
        _hover.Clear();
        foreach (var child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        _confetti = null;
        _paused = false;
        CardsFlown = RewardsShown = CelebrationsShown = 0;
        ComputerCardsRevealed = 0;
        CardFlightProgress = 0;
    }

    public void Hover(Control card, bool active)
    {
        if (_paused) return;
        if (_hover.Remove(card, out var old) && old.IsValid()) old.Kill();
        card.PivotOffset = card.Size / 2;
        var tween = Animate().SetParallel().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        _hover[card] = tween;
        tween.TweenProperty(card, "scale", Vector2.One * (active ? 1.06f : 1), 0.16);
        tween.TweenProperty(card, "rotation", active ? -0.035f : 0f, 0.16);
    }

    public void Deal(IEnumerable<TextureRect> cards, Action finished)
    {
        int i = 0;
        foreach (var card in cards)
        {
            card.PivotOffset = card.Size / 2;
            card.Scale = new Vector2(0.08f, 0.8f);
            card.Modulate = new Color(1, 1, 1, 0);
            var tween = Animate().SetParallel().SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(card, "scale", Vector2.One, 0.24).SetDelay(i * 0.065);
            tween.TweenProperty(card, "modulate:a", 1f, 0.18).SetDelay(i++ * 0.065);
        }
        Animate().TweenCallback(Callable.From(finished)).SetDelay(0.52);
    }

    public void RevealComputerCard(TextureRect card)
    {
        ComputerCardsRevealed++;
        card.PivotOffset = card.Size / 2;
        card.Scale = new Vector2(0.05f, 1);
        Animate().TweenProperty(card, "scale", Vector2.One, 0.22)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    public void FlyCard(Texture2D texture, Rect2 source, TextureRect target, Action finished)
    {
        CardsFlown++;
        CardFlightProgress = 0;
        var inverse = GetGlobalTransform().AffineInverse();
        Vector2 start = inverse * source.Position;
        Vector2 end = inverse * target.GlobalPosition;
        var flying = new TextureRect
        {
            Texture = texture, Position = start, Size = source.Size,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(flying);
        target.Modulate = new Color(1, 1, 1, 0);
        Vector2 bend = (start + end) / 2 + new Vector2(0, -100);
        var flight = Animate().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        flight.TweenMethod(Callable.From<float>(t =>
        {
            CardFlightProgress = t;
            flying.Position = (1 - t) * (1 - t) * start + 2 * (1 - t) * t * bend + t * t * end;
            flying.Size = source.Size.Lerp(target.Size, t);
            flying.PivotOffset = flying.Size / 2;
            flying.Rotation = MathF.Sin(t * MathF.PI) * -0.15f;
        }), 0f, 1f, 0.3);
        flight.TweenCallback(Callable.From(() =>
        {
            flying.QueueFree();
            target.Modulate = Colors.White;
            target.PivotOffset = target.Size / 2;
            target.Scale = Vector2.One * 1.09f;
            Animate().TweenProperty(target, "scale", Vector2.One, 0.15)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            finished();
        }));
    }

    public void Reward(string hand, int points, Rect2 track)
    {
        RewardsShown++;
        var label = new Label
        {
            Text = $"{hand.ToUpperInvariant()}   +{points:N0}",
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = GetGlobalTransform().AffineInverse() * (track.Position + new Vector2(track.Size.X * 0.25f, 40)),
            Size = new Vector2(track.Size.X * 0.5f, 56), MouseFilter = MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", points >= 9000 ? 34 : 28);
        label.AddThemeColorOverride("font_color", Color.FromHtml("#FFE49B"));
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.9f));
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 3);
        AddChild(label);
        label.PivotOffset = label.Size / 2;
        label.Scale = Vector2.One * 0.6f;
        var tween = Animate();
        tween.TweenProperty(label, "scale", Vector2.One, 0.22).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.TweenInterval(0.55);
        tween.SetParallel();
        tween.TweenProperty(label, "position:y", label.Position.Y - 32, 0.6);
        tween.TweenProperty(label, "modulate:a", 0f, 0.6);
        tween.Chain().TweenCallback(Callable.From(label.QueueFree));
    }

    public void Celebrate()
    {
        CelebrationsShown++;
        _confetti = new CpuParticles2D
        {
            Position = new Vector2(Size.X / 2, -15), Emitting = true, OneShot = true,
            Amount = 180, Lifetime = 3.5, Explosiveness = 0.9f,
            Direction = Vector2.Down, Spread = 75, Gravity = new Vector2(0, 200),
            InitialVelocityMin = 90, InitialVelocityMax = 260,
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
            EmissionRectExtents = new Vector2(Size.X * 0.45f, 10),
            ScaleAmountMin = 3, ScaleAmountMax = 7,
            AngularVelocityMin = -240, AngularVelocityMax = 240,
            ColorInitialRamp = new Gradient
            {
                Colors = new[] { Color.FromHtml("#F4CF75"), Color.FromHtml("#77E4BD"), Color.FromHtml("#F78575"), Color.FromHtml("#75BBF7") },
                Offsets = new[] { 0f, 0.33f, 0.66f, 1f }
            }
        };
        AddChild(_confetti);
    }
}
