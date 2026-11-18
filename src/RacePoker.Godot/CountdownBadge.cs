using Godot;

namespace RacePoker.Godot;

/// <summary>A compact timer with a readable number and a smoothly draining progress ring.</summary>
public partial class CountdownBadge : Control
{
    private double _remaining = 30;
    private float _progress = 1;
    private bool _unlimited, _paused;
    private double _pulse;
    private readonly StyleBoxFlat _surface = new()
    {
        BgColor = Color.FromHtml("#102B32"),
        BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
        CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12,
        CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12
    };

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(236, 28);
        TooltipText = "Time left to finish your hand. At zero, the remaining cards are picked automatically.";
    }

    public void UpdateTime(double progress, double duration, bool paused)
    {
        _progress = (float)Math.Clamp(progress, 0, 1);
        _remaining = Math.Max(0, progress * duration);
        _unlimited = duration <= 0;
        _paused = paused;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!_paused) _pulse += delta;
        if (Visible) QueueRedraw();
    }

    public override void _Draw()
    {
        bool urgent = !_unlimited && _remaining <= 5;
        var accent = Color.FromHtml(urgent ? "#FF927F" : !_unlimited && _remaining <= 10 ? "#F4CF75" : "#77E4BD");
        _surface.BgColor = Color.FromHtml(urgent ? "#382326" : "#102B32");
        _surface.BorderColor = accent with { A = urgent ? 0.55f + 0.25f * (float)Math.Sin(_pulse * Math.Tau * 1.5) : 0.3f };
        DrawStyleBox(_surface, new Rect2(0, -5, Size.X, 38));
        var ring = new Vector2(22, 14);
        DrawArc(ring, 12, 0, Mathf.Tau, 64, Color.FromHtml("#30454C"), 3, true);
        if (_progress > 0)
            DrawArc(ring, 12, -Mathf.Pi / 2, -Mathf.Pi / 2 + Mathf.Tau * (_unlimited ? 1 : _progress), 64, accent, 3, true);
        // Clock hands keep the ring recognizable even when nearly empty.
        DrawLine(ring + new Vector2(0, -6), ring, accent, 1.5f, true);
        DrawLine(ring, ring + new Vector2(4, 3), accent, 1.5f, true);
        var font = ThemeDB.FallbackFont;
        DrawString(font, new Vector2(45, 24), _unlimited ? "∞" : Math.Ceiling(_remaining).ToString("00"), fontSize: 28, modulate: accent);
        DrawString(font, new Vector2(97, 10), _unlimited ? "TAKE YOUR TIME" : urgent ? "FINAL SECONDS" : "TIME LEFT", fontSize: 11, modulate: accent);
        DrawString(font, new Vector2(97, 25), _unlimited ? "No countdown" : urgent ? "Make your pick" : "Finish your hand", fontSize: 11, modulate: Color.FromHtml("#A8BEC5"));
    }
}
