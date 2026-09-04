using System.Windows.Input;
using NexTap.Helpers;
using NexTap.Models;
using NexTap.Services;

namespace NexTap.ViewModels;

[QueryProperty(nameof(CardId), "CardId")]
public class ApproachViewModel : BaseViewModel
{
	private readonly ICardStoreService _cardStore;
	private readonly INfcService _nfcService;

	public ApproachViewModel(ICardStoreService cardStore, INfcService nfcService)
	{
		_cardStore = cardStore;
		_nfcService = nfcService;

		CancelCommand = new AsyncRelayCommand(async () =>
		{
			_nfcService.DisarmEmulation();
			await Shell.Current.GoToAsync("..");
		});
	}

	public string CardId { get; set; } = string.Empty;

	private NfcCardModel? _card;
	public NfcCardModel? Card
	{
		get => _card;
		set => SetProperty(ref _card, value);
	}

	private string _statusText = "Hold your phone near the reader";
	public string StatusText
	{
		get => _statusText;
		set => SetProperty(ref _statusText, value);
	}

	public ICommand CancelCommand { get; }

	/// <summary>Called by ApproachPage once it can show the animation and navigate onward.</summary>
	public event Action? Presented;

	public async Task ArmAsync()
	{
		Card = await _cardStore.GetCardAsync(CardId);
		if (Card is null)
			return;

		if (!_nfcService.IsNfcAvailable || !_nfcService.IsNfcEnabled)
		{
			// No working NFC (emulator, disabled radio, etc.) - still let the
			// person see the intended flow rather than dead-ending here.
			StatusText = "NFC unavailable - showing preview";
			return;
		}

		_nfcService.ArmEmulation(Card.Uid, Card.NdefContent, () =>
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				StatusText = "Reader detected NexTap";
				await _cardStore.TouchLastUsedAsync(Card.Id);
				Presented?.Invoke();
			});
		});
	}

	public void Disarm() => _nfcService.DisarmEmulation();
}
