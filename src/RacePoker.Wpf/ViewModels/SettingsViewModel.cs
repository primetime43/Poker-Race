using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using RacePoker.Core.Models;
using RacePoker.Core.Enums;

namespace RacePoker.Wpf.ViewModels;

public class SettingsViewModel : ViewModelBase, IDataErrorInfo
{
    private readonly GameSettings _settings;
    public bool RulesEditable { get; }
    private int _finishDistance;
    private bool _untimedPicking;
    private bool _firstToFinish;
    private bool _sumHighCards;
    private string _finishDistanceText = "150,000";
    public int FinishDistance
    {
        get => _finishDistance;
        set
        {
            SetProperty(ref _finishDistance, Math.Clamp(value, 1000, 1000000));
            FinishDistanceText = _finishDistance.ToString("N0", CultureInfo.CurrentCulture);
        }
    }
    public string FinishDistanceText
    {
        get => _finishDistanceText;
        set
        {
            if (!SetProperty(ref _finishDistanceText, value)) return;
            if (TryDistance(out int distance)) { _finishDistance = distance; OnPropertyChanged(nameof(FinishDistance)); }
            OnPropertyChanged(nameof(FinishDistanceError));
        }
    }
    public string FinishDistanceError => TryDistance(out _) ? "" : "Enter a whole number from 1,000 to 1,000,000.";
    public string Error => FinishDistanceError;
    public string this[string columnName] => columnName == nameof(FinishDistanceText) ? FinishDistanceError : "";
    private bool TryDistance(out int distance)
        => int.TryParse(FinishDistanceText, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out distance)
           && distance >= 1000 && distance <= 1000000;
    public ICommand IncreaseDistanceCommand { get; }
    public ICommand DecreaseDistanceCommand { get; }
    public bool UntimedPicking { get => _untimedPicking; set => SetProperty(ref _untimedPicking, value); }
    public bool FirstToFinish { get => _firstToFinish; set => SetProperty(ref _firstToFinish, value); }
    public bool SumHighCards { get => _sumHighCards; set => SetProperty(ref _sumHighCards, value); }

    private bool _autoAdvanceEnabled;
    private double _autoAdvanceDelaySeconds;
    private int _maxRounds;
    private double _pickingTimeoutSeconds;
    private double _racingDurationSeconds;
    private string _raceIconStyle = "Symbol";
    private string _handResultStyle = "Marquee";
    private bool _soundEnabled;
    private double _cardSwayDegrees;
    private double _logoGlowBlur;

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

    public double CardSwayDegrees
    {
        get => _cardSwayDegrees;
        set => SetProperty(ref _cardSwayDegrees, value);
    }

    public double LogoGlowBlur
    {
        get => _logoGlowBlur;
        set => SetProperty(ref _logoGlowBlur, value);
    }

    public SettingsViewModel(GameSettings settings, bool rulesEditable = true)
    {
        _settings = settings;
        RulesEditable = rulesEditable;
        _finishDistance = settings.FinishDistance;
        _finishDistanceText = settings.FinishDistance.ToString("N0", CultureInfo.CurrentCulture);
        IncreaseDistanceCommand = new RelayCommand(() => FinishDistance += 1000);
        DecreaseDistanceCommand = new RelayCommand(() => FinishDistance -= 1000);
        _untimedPicking = settings.PickingTimeoutSeconds <= 0;
        _firstToFinish = settings.FinishRule == FinishRule.FirstToFinish;
        _sumHighCards = settings.HighCardRule == HighCardRule.SumOfRanks;
        _autoAdvanceEnabled = settings.AutoAdvanceEnabled;
        _autoAdvanceDelaySeconds = settings.AutoAdvanceDelaySeconds;
        _maxRounds = settings.MaxRounds;
        _pickingTimeoutSeconds = settings.PickingTimeoutSeconds <= 0 ? 30 : settings.PickingTimeoutSeconds;
        _racingDurationSeconds = settings.RacingDurationSeconds;
        _raceIconStyle = settings.RaceIconStyle;
        _handResultStyle = settings.HandResultStyle;
        _soundEnabled = settings.SoundEnabled;
        _cardSwayDegrees = settings.CardSwayDegrees;
        _logoGlowBlur = settings.LogoGlowBlur;
    }

    public bool Apply()
    {
        if (RulesEditable && !TryDistance(out _)) return false;
        if (RulesEditable)
        {
            _settings.AutoAdvanceEnabled = AutoAdvanceEnabled;
            _settings.AutoAdvanceDelaySeconds = AutoAdvanceDelaySeconds;
            _settings.MaxRounds = MaxRounds;
            _settings.FinishDistance = FinishDistance;
            _settings.FinishRule = FirstToFinish ? FinishRule.FirstToFinish : FinishRule.EndOfRound;
            _settings.HighCardRule = SumHighCards ? HighCardRule.SumOfRanks : HighCardRule.HighestRank;
            _settings.PickingTimeoutSeconds = UntimedPicking ? 0 : PickingTimeoutSeconds;
            _settings.RacingDurationSeconds = RacingDurationSeconds;
        }
        _settings.RaceIconStyle = RaceIconStyle;
        _settings.HandResultStyle = HandResultStyle;
        _settings.SoundEnabled = SoundEnabled;
        _settings.CardSwayDegrees = CardSwayDegrees;
        _settings.LogoGlowBlur = LogoGlowBlur;
        _settings.Normalize();
        return true;
    }
}
