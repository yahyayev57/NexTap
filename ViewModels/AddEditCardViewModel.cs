using System.Windows.Input;
using NexTap.Helpers;
using NexTap.Models;
using NexTap.Services;

namespace NexTap.ViewModels;

[QueryProperty(nameof(CardId), "CardId")]
public class AddEditCardViewModel : BaseViewModel
{
	private readonly ICardStoreService _cardStore;
	private readonly INfcService _nfcService;

	private CancellationTokenSource? _scanCts;
	private string _cardId = string.Empty;
	private string _existingId = string.Empty;
	private NfcCardModel? _editingCard;

	public AddEditCardViewModel(ICardStoreService cardStore, INfcService nfcService)
	{
		_cardStore = cardStore;
		_nfcService = nfcService;

		Covers = Enum.GetValues<CardCover>().ToList();
		SelectedCover = CardCover.Blue;

		ScanCommand = new AsyncRelayCommand(OnScanAsync, () => !IsScanning);
		SaveCommand = new AsyncRelayCommand(OnSaveAsync, () => !string.IsNullOrWhiteSpace(Name));
		CancelCommand = new AsyncRelayCommand(async () => await Shell.Current.GoToAsync(".."));
	}

	public string CardId
	{
		get => _cardId;
		set
		{
			_cardId = value;
			_ = LoadIfEditingAsync(value);
		}
	}

	public bool IsEditing => !string.IsNullOrEmpty(_existingId);

	public List<CardCover> Covers { get; }

	private string _name = string.Empty;
	public string Name
	{
		get => _name;
		set
		{
			if (SetProperty(ref _name, value))
				((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
		}
	}

	private CardCover _selectedCover;
	public CardCover SelectedCover
	{
		get => _selectedCover;
		set => SetProperty(ref _selectedCover, value);
	}

	private string _uid = string.Empty;
	public string Uid
	{
		get => _uid;
		set => SetProperty(ref _uid, value);
	}

	private string _technology = string.Empty;

	private string? _ndefContent;
	public string? NdefContent
	{
		get => _ndefContent;
		set => SetProperty(ref _ndefContent, value);
	}
	public string Technology
	{
		get => _technology;
		set => SetProperty(ref _technology, value);
	}

	private bool _isScanning;
	public bool IsScanning
	{
		get => _isScanning;
		set
		{
			if (SetProperty(ref _isScanning, value))
				((AsyncRelayCommand)ScanCommand).RaiseCanExecuteChanged();
		}
	}

	private string _scanStatus = "Not linked to a physical card yet";
	public string ScanStatus
	{
		get => _scanStatus;
		set => SetProperty(ref _scanStatus, value);
	}

	public ICommand ScanCommand { get; }
	public ICommand SaveCommand { get; }
	public ICommand CancelCommand { get; }

	private async Task LoadIfEditingAsync(string cardId)
	{
		if (string.IsNullOrEmpty(cardId))
			return;

		var card = await _cardStore.GetCardAsync(cardId);
		if (card is null)
			return;

		_existingId = card.Id;
		_editingCard = card;
		Name = card.Name;
		SelectedCover = card.Cover;
		Uid = card.Uid;
		Technology = card.Technology;
		NdefContent = card.NdefContent;
		ScanStatus = string.IsNullOrEmpty(card.Uid)
			? "Not linked to a physical card yet"
			: $"Linked - UID {card.Uid}";
		OnPropertyChanged(nameof(IsEditing));
	}

	private async Task OnScanAsync()
	{
		if (!_nfcService.IsNfcAvailable)
		{
			await Shell.Current.DisplayAlertAsync("NFC unavailable", "This device doesn't support NFC.", "OK");
			return;
		}

		if (!_nfcService.IsNfcEnabled)
		{
			await Shell.Current.DisplayAlertAsync("NFC is off", "Turn on NFC in system settings, then try again.", "OK");
			return;
		}

		IsScanning = true;
		ScanStatus = "Hold the card to the back of your phone...";
		_scanCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

		try
		{
			var result = await _nfcService.ScanCardAsync(_scanCts.Token);
			if (result is null)
			{
				ScanStatus = "No card detected - try again";
				return;
			}

			Uid = result.Uid;
			Technology = result.Technology;
			NdefContent = result.NdefContent;
			ScanStatus = string.IsNullOrWhiteSpace(result.NdefContent)
				? $"Linked - UID {result.Uid}"
				: $"Linked - UID {result.Uid} · NDEF detected";
		}
		finally
		{
			IsScanning = false;
		}
	}

	private async Task OnSaveAsync()
	{
		var card = _editingCard ?? new NfcCardModel();

		card.Name = Name.Trim();
		card.Cover = SelectedCover;
		card.Uid = Uid;
		card.Technology = Technology;
		card.NdefContent = NdefContent;

		await _cardStore.SaveCardAsync(card);
		await Shell.Current.GoToAsync("..");
	}
}
