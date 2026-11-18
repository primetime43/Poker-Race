using Godot;

namespace RacePoker.Godot;

/// <summary>A large keyboard-accessible toggle with both a state label and an animated switch.</summary>
public partial class SettingSwitch : Button
{
    private float _position;
    private Tween? _slide;
    private readonly StyleBoxFlat _track = new()
    {
        CornerRadiusTopLeft = 14, CornerRadiusTopRight = 14,
        CornerRadiusBottomLeft = 14, CornerRadiusBottomRight = 14
    };

    public override void _Ready()
    {
        ToggleMode = true;
        CustomMinimumSize = new Vector2(132, 48);
        Alignment = HorizontalAlignment.Left;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        FocusMode = FocusModeEnum.All;
        _position = ButtonPressed ? 1 : 0;
        ApplyAppearance();
        Toggled += enabled =>
        {
            ApplyAppearance();
            _slide?.Kill();
            _slide = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            _slide.TweenMethod(Callable.From<float>(value => { _position = value; QueueRedraw(); }), _position, enabled ? 1f : 0f, 0.16);
        };
    }

    public void SyncState(bool enabled)
    {
        if (ButtonPressed == enabled) return;
        SetPressedNoSignal(enabled);
        _slide?.Kill();
        _position = enabled ? 1 : 0;
        ApplyAppearance();
        QueueRedraw();
    }

    private void ApplyAppearance()
    {
        Text = ButtonPressed ? "ON" : "OFF";
        var fill = Color.FromHtml(ButtonPressed ? "#173E36" : "#172B34");
        var border = Color.FromHtml(ButtonPressed ? "#4DAE8C" : "#46606D");
        AddThemeStyleboxOverride("normal", Box(fill, border));
        AddThemeStyleboxOverride("hover", Box(fill.Lightened(0.12f), border.Lightened(0.2f)));
        AddThemeStyleboxOverride("pressed", Box(fill.Lightened(0.06f), border));
        AddThemeStyleboxOverride("hover_pressed", Box(fill.Lightened(0.12f), border));
        var focus = Box(Colors.Transparent, Color.FromHtml("#F4CF75"));
        focus.BorderWidthLeft = focus.BorderWidthTop = focus.BorderWidthRight = focus.BorderWidthBottom = 2;
        AddThemeStyleboxOverride("focus", focus);
        AddThemeFontSizeOverride("font_size", 14);
        foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_hover_pressed_color" })
            AddThemeColorOverride(state, Color.FromHtml(ButtonPressed ? "#A5F2D1" : "#BACBD2"));
    }

    private static StyleBoxFlat Box(Color fill, Color border) => new()
    {
        BgColor = fill, BorderColor = border,
        BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
        CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
        ContentMarginLeft = 14, ContentMarginRight = 76
    };

    public override void _Draw()
    {
        Vector2 start = new(Size.X - 64, (Size.Y - 28) / 2);
        _track.BgColor = Color.FromHtml("#435964").Lerp(Color.FromHtml("#65DCAB"), _position);
        DrawStyleBox(_track, new Rect2(start, new Vector2(52, 28)));
        DrawCircle(start + new Vector2(14 + _position * 24, 14), 10, Color.FromHtml("#F4FBF7"), true, -1, true);
    }
}
