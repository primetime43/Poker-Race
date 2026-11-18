using Godot;

namespace RacePoker.Godot;

public partial class Main
{
    private PlayerPreferences _preferences = new();
    private string _preferencesPath = "user://preferences.cfg";
    private Control _settingsLayer = null!;
    private SettingSwitch _soundToggle = null!, _hintsToggle = null!;
    private Label _settingsNotice = null!;
    private bool _settingsOpen;

    private void SavePreferences()
    {
        var error = _preferences.Save(_preferencesPath);
        _settingsNotice.Text = error == Error.Ok ? "Changes save automatically."
            : "Applied for this session. Your preferences could not be saved.";
    }

    private void SetSoundEnabled(bool enabled)
    {
        if (_audio.Muted == enabled) _audio.ToggleMute();
        _preferences.SoundEnabled = enabled;
        _soundToggle.SyncState(enabled);
        SavePreferences();
    }

    private void SetLastCardHints(bool enabled)
    {
        _preferences.ShowLastCardHints = enabled;
        _hintsToggle.SyncState(enabled);
        Refresh(); // Clears both the hand note and card tooltips immediately when disabled.
        SavePreferences();
    }

    private void ToggleSettings() => ToggleDialog(_settingsLayer, true);

    private void ToggleDialog(Control layer, bool settings)
    {
        bool alreadyOpen = settings ? _settingsOpen : _guideOpen;
        if (alreadyOpen)
        {
            layer.Visible = false;
            if (settings) _settingsOpen = false; else _guideOpen = false;
            if (_resumeAfterGuide && !_focusLostInGuide) TogglePause();
            return;
        }
        if (_settingsOpen || _guideOpen) return;
        _resumeAfterGuide = !_paused;
        _focusLostInGuide = false;
        if (_resumeAfterGuide) TogglePause();
        if (settings) _settingsOpen = true; else _guideOpen = true;
        layer.Visible = true;
    }

    private void BuildSettings()
    {
        _settingsLayer = new Control { Visible = false, ZIndex = 60 };
        _settingsLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_settingsLayer);
        var shade = new ColorRect { Color = new Color(0, 0, 0, 0.78f), MouseFilter = MouseFilterEnum.Stop };
        shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _settingsLayer.AddChild(shade);
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _settingsLayer.AddChild(center);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(720, 0) };
        var style = Panel("#0B1D23", "#34545C");
        style.BorderWidthLeft = style.BorderWidthRight = style.BorderWidthTop = style.BorderWidthBottom = 1;
        style.CornerRadiusTopLeft = style.CornerRadiusTopRight = style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 18;
        style.ShadowColor = new Color(0, 0, 0, 0.45f);
        style.ShadowSize = 24;
        style.ContentMarginLeft = style.ContentMarginRight = 28;
        style.ContentMarginTop = style.ContentMarginBottom = 28;
        panel.AddThemeStyleboxOverride("panel", style);
        center.AddChild(panel);
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 22);
        panel.AddChild(content);
        var header = new HBoxContainer();
        var heading = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        heading.AddChild(Text("Settings", 36, "#F1F8F5"));
        heading.AddChild(Text("Sound and gameplay assistance", 17, "#A8BEC5"));
        header.AddChild(heading);
        var close = ActionButton("×", ToggleSettings);
        close.CustomMinimumSize = new Vector2(44, 44);
        close.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        close.TooltipText = "Close settings · Esc";
        close.FocusMode = FocusModeEnum.All;
        close.AddThemeFontSizeOverride("font_size", 26);
        header.AddChild(close);
        content.AddChild(header);
        _soundToggle = SettingToggle("Sound effects", _preferences.SoundEnabled, SetSoundEnabled);
        content.AddChild(SettingSection("AUDIO", "Sound effects", "Card picks, payouts, and the finish fanfare.\nPress M to toggle sound anytime.", _soundToggle));
        _hintsToggle = SettingToggle("Last-card hints", _preferences.ShowLastCardHints, SetLastCardHints);
        content.AddChild(SettingSection("GAMEPLAY ASSISTANCE", "Last-card hints", "See the resulting hand and payout when you hover over your final card choice.", _hintsToggle));
        content.AddChild(new HSeparator());
        var footer = new HBoxContainer();
        _settingsNotice = Text("Changes save automatically.", 15, "#A8BEC5");
        _settingsNotice.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _settingsNotice.CustomMinimumSize = new Vector2(280, 0);
        _settingsNotice.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        footer.AddChild(_settingsNotice);
        var done = ActionButton("Done", ToggleSettings);
        done.CustomMinimumSize = new Vector2(140, 48);
        done.FocusMode = FocusModeEnum.All;
        done.AddThemeStyleboxOverride("normal", Panel("#78E0B7", "#78E0B7"));
        done.AddThemeStyleboxOverride("hover", Panel("#A0F0D0", "#A0F0D0"));
        done.AddThemeStyleboxOverride("pressed", Panel("#53BB93", "#53BB93"));
        foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color" })
            done.AddThemeColorOverride(state, Color.FromHtml("#092B24"));
        done.AddThemeFontSizeOverride("font_size", 18);
        done.TooltipText = "Back to race · S / Esc";
        footer.AddChild(done);
        content.AddChild(footer);
    }

    private static VBoxContainer SettingSection(string category, string title, string description, SettingSwitch toggle)
    {
        var section = new VBoxContainer();
        section.AddThemeConstantOverride("separation", 10);
        section.AddChild(Text(category, 13, "#77D8B5"));
        var card = new PanelContainer();
        var style = Panel("#102B32", "#25444D");
        style.BorderWidthLeft = style.BorderWidthTop = style.BorderWidthRight = style.BorderWidthBottom = 1;
        style.ContentMarginLeft = style.ContentMarginRight = 20;
        style.ContentMarginTop = style.ContentMarginBottom = 18;
        card.AddThemeStyleboxOverride("panel", style);
        section.AddChild(card);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 24);
        card.AddChild(row);
        var copy = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        copy.AddThemeConstantOverride("separation", 8);
        copy.AddChild(Text(title, 23, "#F1F8F5"));
        var detail = Text(description, 16, "#A8BEC5");
        detail.CustomMinimumSize = new Vector2(380, 0);
        detail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        copy.AddChild(detail);
        row.AddChild(copy);
        toggle.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        row.AddChild(toggle);
        return section;
    }

    private static SettingSwitch SettingToggle(string text, bool enabled, Action<bool> changed)
    {
        var toggle = new SettingSwitch
        {
            ToggleMode = true, ButtonPressed = enabled,
            TooltipText = text, AccessibilityName = text
        };
        toggle.Toggled += enabledValue => changed(enabledValue);
        return toggle;
    }
}
