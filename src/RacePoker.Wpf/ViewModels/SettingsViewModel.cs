using RacePoker.Core.Models;

namespace RacePoker.Wpf.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly GameSettings _settings;

    private bool _autoAdvanceEnabled;
    private double _autoAdvanceDelaySeconds;
    private int _maxRounds;
    private double _pickingTimeoutSeconds;
    private double _racingDurationSeconds;
    private string _raceIconStyle = "Symbol";
    private string _handResultStyle = "Marquee";
    private bool _soundEnabled;

    public bool SoundEnabled
    {
        get => _soundEnabled;
        set => SetProperty(ref _soundEnabled, value);
    }

    public bool AutoAdvanceEnabled
    {
        get => _autoAdvanceEnabled;
        set => SetProperty(ref _autoAdvanceEnabled, value);
    }

    public double AutoAdvanceDelaySeconds
    {
        get => _autoAdvanceDelaySeconds;
        set => SetProperty(ref _autoAdvanceDelaySeconds, value);
    }

    public int MaxRounds
    {
        get => _maxRounds;
        set => SetProperty(ref _maxRounds, value);
    }

    public double PickingTimeoutSeconds
    {
        get => _pickingTimeoutSeconds;
        set => SetProperty(ref _pickingTimeoutSeconds, value);
    }

    public double RacingDurationSeconds
    {
        get => _racingDurationSeconds;
        set => SetProperty(ref _racingDurationSeconds, value);
    }

    public string RaceIconStyle
    {
        get => _raceIconStyle;
        set => SetProperty(ref _raceIconStyle, value);
    }

    public bool IsSymbolIcon
    {
        get => RaceIconStyle == "Symbol";
        set { if (value) RaceIconStyle = "Symbol"; }
    }

    public bool IsImageIcon
    {
        get => RaceIconStyle == "Image";
        set { if (value) RaceIconStyle = "Image"; }
    }

    public string HandResultStyle
    {
        get => _handResultStyle;
        set => SetProperty(ref _handResultStyle, value);
    }

    public bool IsHandResultPlain
    {
        get => HandResultStyle == "Plain";
        set { if (value) HandResultStyle = "Plain"; }
    }

    public bool IsHandResultMarquee
    {
        get => HandResultStyle == "Marquee";
        set { if (value) HandResultStyle = "Marquee"; }
    }

    public bool IsHandResultLights
    {
        get => HandResultStyle == "Lights";
        set { if (value) HandResultStyle = "Lights"; }
    }

    public SettingsViewModel(GameSettings settings)
    {
        _settings = settings;
        _autoAdvanceEnabled = settings.AutoAdvanceEnabled;
        _autoAdvanceDelaySeconds = settings.AutoAdvanceDelaySeconds;
        _maxRounds = settings.MaxRounds;
        _pickingTimeoutSeconds = settings.PickingTimeoutSeconds;
        _racingDurationSeconds = settings.RacingDurationSeconds;
        _raceIconStyle = settings.RaceIconStyle;
        _handResultStyle = settings.HandResultStyle;
        _soundEnabled = settings.SoundEnabled;
    }

    public void Apply()
    {
        _settings.AutoAdvanceEnabled = AutoAdvanceEnabled;
        _settings.AutoAdvanceDelaySeconds = AutoAdvanceDelaySeconds;
        _settings.MaxRounds = MaxRounds;
        _settings.PickingTimeoutSeconds = PickingTimeoutSeconds;
        _settings.RacingDurationSeconds = RacingDurationSeconds;
        _settings.RaceIconStyle = RaceIconStyle;
        _settings.HandResultStyle = HandResultStyle;
        _settings.SoundEnabled = SoundEnabled;
    }
}
