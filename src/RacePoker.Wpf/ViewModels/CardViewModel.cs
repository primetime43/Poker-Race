using System.Windows.Input;
using RacePoker.Core.Models;

namespace RacePoker.Wpf.ViewModels;

public class CardViewModel : ViewModelBase
{
    private Card? _card;
    private bool _isHeld;
    private bool _canToggle;
    private bool _justAppeared;

    public Card? Card
    {
        get => _card;
        set
        {
            var wasNull = _card is null;
            if (SetProperty(ref _card, value))
            {
                OnPropertyChanged(nameof(ImagePath));
                OnPropertyChanged(nameof(PickLabel));
                if (wasNull && _card is not null)
                {
                    JustAppeared = false;
                    JustAppeared = true;
                }
                else if (_card is null)
                {
                    JustAppeared = false;
                }
            }
        }
    }

    public bool JustAppeared
    {
        get => _justAppeared;
        set => SetProperty(ref _justAppeared, value);
    }

    public string ImagePath => Card is not null
        ? $"pack://application:,,,/RacePoker.Wpf;component/Assets/PokerCards/{Card.ImageFileName}"
        : "";

    public bool IsHeld
    {
        get => _isHeld;
        set => SetProperty(ref _isHeld, value);
    }

    public bool CanToggle
    {
        get => _canToggle;
        set => SetProperty(ref _canToggle, value);
    }

    public int CardIndex { get; }
    public string KeyLabel => $"{CardIndex + 1} / F{CardIndex + 1}";
    public string PickLabel => Card is null ? $"Pick card {CardIndex + 1}" : $"Pick {Card.Rank} of {Card.Suit}";

    public ICommand ToggleHoldCommand { get; }

    public Action<int>? OnToggleHold { get; set; }

    public CardViewModel(int cardIndex)
    {
        CardIndex = cardIndex;
        ToggleHoldCommand = new RelayCommand(() => OnToggleHold?.Invoke(CardIndex));
    }
}
