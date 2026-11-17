using Godot;

namespace RacePoker.Godot;

public partial class Main
{
    private PlayerPreferences _preferences = new();
    private string _preferencesPath = "user://preferences.cfg";
    private Control _settingsLayer = null!;
    private CheckButton _soundToggle = null!, _hintsToggle = null!;
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
        _soundToggle.SetPressedNoSignal(enabled);
        SavePreferences();
    }

    private void SetLastCardHints(bool enabled)
    {
        _preferences.ShowLastCardHints = enabled;
        _hintsToggle.SetPressedNoSignal(enabled);
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
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(580, 0) };
        var style = Panel("#0B2025", "#77E4BD");
        style.ContentMarginLeft = style.ContentMarginRight = 24;
        style.ContentMarginTop = style.ContentMarginBottom = 20;
        panel.AddThemeStyleboxOverride("panel", style);
        center.AddChild(panel);
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 12);
        panel.AddChild(content);
        content.AddChild(Text("SETTINGS", 28, "#F4CF75"));
        content.AddChild(Text("AUDIO", 14, "#77E4BD"));
        _soundToggle = SettingToggle("Sound effects", _preferences.SoundEnabled, SetSoundEnabled);
        content.AddChild(_soundToggle);
        content.AddChild(Text("Card picks, payouts, and the finish fanfare. Shortcut: M.", 15, "#ADD0C7"));
        content.AddChild(new HSeparator());
        content.AddChild(Text("GAMEPLAY ASSISTANCE", 14, "#77E4BD"));
        _hintsToggle = SettingToggle("Show last-card hints", _preferences.ShowLastCardHints, SetLastCardHints);
        content.AddChild(_hintsToggle);
        var explanation = Text("Preview the resulting hand and points when you hover over your fifth card choice.", 15, "#ADD0C7");
        explanation.CustomMinimumSize = new Vector2(510, 0);
        explanation.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(explanation);
        content.AddChild(new HSeparator());
        _settingsNotice = Text("Changes save automatically.", 14, "#ADD0C7");
        content.AddChild(_settingsNotice);
        content.AddChild(ActionButton("Back to race · S / Esc", ToggleSettings));
    }

    private static CheckButton SettingToggle(string text, bool enabled, Action<bool> changed)
    {
        var toggle = new CheckButton
        {
            Text = text, ButtonPressed = enabled, FocusMode = FocusModeEnum.None,
            MouseDefaultCursorShape = CursorShape.PointingHand
        };
        toggle.AddThemeFontSizeOverride("font_size", 20);
        toggle.Toggled += enabledValue => changed(enabledValue);
        return toggle;
    }
}
