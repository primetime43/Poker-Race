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

    public SettingsViewModel(GameSettings settings)
    {
        _settings = settings;
        _autoAdvanceEnabled = settings.AutoAdvanceEnabled;
        _autoAdvanceDelaySeconds = settings.AutoAdvanceDelaySeconds;
        _maxRounds = settings.MaxRounds;
        _pickingTimeoutSeconds = settings.PickingTimeoutSeconds;
        _racingDurationSeconds = settings.RacingDurationSeconds;
    }

    public void Apply()
    {
        _settings.AutoAdvanceEnabled = AutoAdvanceEnabled;
        _settings.AutoAdvanceDelaySeconds = AutoAdvanceDelaySeconds;
        _settings.MaxRounds = MaxRounds;
        _settings.PickingTimeoutSeconds = PickingTimeoutSeconds;
        _settings.RacingDurationSeconds = RacingDurationSeconds;
    }
}
