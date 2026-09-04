using System.Collections.ObjectModel;
using System.Windows.Input;
using NexTap.Helpers;
using NexTap.Models;
using NexTap.Services;
using NexTap.Views;

namespace NexTap.ViewModels;

public class WalletViewModel : BaseViewModel
{
	private readonly ICardStoreService _cardStore;

	public WalletViewModel(ICardStoreService cardStore)
	{
		_cardStore = cardStore;

		ManageCommand = new AsyncRelayCommand(OnManageAsync);
		UseSelectedCardCommand = new AsyncRelayCommand(OnUseSelectedCardAsync, () => SelectedCard is not null);
	}

	public ObservableCollection<NfcCardModel> Cards { get; } = new();

	private NfcCardModel? _selectedCard;
	public NfcCardModel? SelectedCard
	{
		get => _selectedCard;
		set
		{
			if (SetProperty(ref _selectedCard, value))
				((AsyncRelayCommand)UseSelectedCardCommand).RaiseCanExecuteChanged();
		}
	}

	private bool _hasCards;
	public bool HasCards
	{
		get => _hasCards;
		set => SetProperty(ref _hasCards, value);
	}

	public ICommand ManageCommand { get; }
	public ICommand UseSelectedCardCommand { get; }

	public async Task LoadAsync()
	{
		var cards = await _cardStore.GetCardsAsync();

		Cards.Clear();
		foreach (var card in cards)
			Cards.Add(card);

		HasCards = Cards.Count > 0;

		// Keep the previous selection if it still exists, otherwise default
		// to the top (most recently used) card.
		SelectedCard = Cards.FirstOrDefault(c => c.Id == SelectedCard?.Id) ?? Cards.FirstOrDefault();
	}

	private async Task OnManageAsync()
	{
		await Shell.Current.GoToAsync(nameof(ManageCardsPage));
	}

	private async Task OnUseSelectedCardAsync()
	{
		if (SelectedCard is null)
			return;

		await Shell.Current.GoToAsync(nameof(ApproachPage), new Dictionary<string, object>
		{
			["CardId"] = SelectedCard.Id
		});
	}
}
