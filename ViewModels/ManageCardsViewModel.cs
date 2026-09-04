using System.Collections.ObjectModel;
using System.Windows.Input;
using NexTap.Helpers;
using NexTap.Models;
using NexTap.Services;
using NexTap.Views;

namespace NexTap.ViewModels;

public class ManageCardsViewModel : BaseViewModel
{
	private readonly ICardStoreService _cardStore;

	public ManageCardsViewModel(ICardStoreService cardStore)
	{
		_cardStore = cardStore;

		CloseCommand = new AsyncRelayCommand(async () => await Shell.Current.GoToAsync(".."));
		AddCardCommand = new AsyncRelayCommand(async () => await Shell.Current.GoToAsync(nameof(AddEditCardPage)));
		EditCardCommand = new AsyncRelayCommand<NfcCardModel>(OnEditCardAsync);
		DeleteCardCommand = new AsyncRelayCommand<NfcCardModel>(OnDeleteCardAsync);
	}

	public ObservableCollection<NfcCardModel> Cards { get; } = new();

	public ICommand CloseCommand { get; }
	public ICommand AddCardCommand { get; }
	public ICommand EditCardCommand { get; }
	public ICommand DeleteCardCommand { get; }

	public async Task LoadAsync()
	{
		var cards = await _cardStore.GetCardsAsync();
		Cards.Clear();
		foreach (var card in cards)
			Cards.Add(card);
	}

	private async Task OnEditCardAsync(NfcCardModel? card)
	{
		if (card is null)
			return;

		await Shell.Current.GoToAsync(nameof(AddEditCardPage), new Dictionary<string, object>
		{
			["CardId"] = card.Id
		});
	}

	private async Task OnDeleteCardAsync(NfcCardModel? card)
	{
		if (card is null)
			return;

		var confirmed = await Shell.Current.DisplayAlertAsync(
			"Delete card",
			$"Remove \"{card.Name}\" from your wallet? This can't be undone.",
			"Delete", "Cancel");

		if (!confirmed)
			return;

		await _cardStore.DeleteCardAsync(card.Id);
		Cards.Remove(card);
	}
}
