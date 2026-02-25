using System.Windows.Input;
using RacePoker.Core.Models;

namespace RacePoker.Wpf.ViewModels;

public class CardViewModel : ViewModelBase
{
    private Card? _card;
    private bool _isHeld;
    private bool _canToggle;

    public Card? Card
    {
        get => _card;
        set
        {
            if (SetProperty(ref _card, value))
                OnPropertyChanged(nameof(ImagePath));
        }
    }

    public string ImagePath => Card is not null
        ? $"pack://application:,,,/Assets/PokerCards/{Card.ImageFileName}"
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

    public ICommand ToggleHoldCommand { get; }

    public Action<int>? OnToggleHold { get; set; }

    public CardViewModel(int cardIndex)
    {
        CardIndex = cardIndex;
        ToggleHoldCommand = new RelayCommand(() => OnToggleHold?.Invoke(CardIndex));
    }
}
