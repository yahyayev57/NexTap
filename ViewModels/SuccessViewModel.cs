using System.Windows.Input;
using NexTap.Helpers;

namespace NexTap.ViewModels;

[QueryProperty(nameof(CardName), "CardName")]
public class SuccessViewModel : BaseViewModel
{
	public SuccessViewModel()
	{
		BackCommand = new AsyncRelayCommand(async () =>
			// Pop everything back to the wallet root, not just one step,
			// since Approach shouldn't be sitting in the back stack.
			await Shell.Current.GoToAsync("//main"));
	}

	private string _cardName = string.Empty;
	public string CardName
	{
		get => _cardName;
		set => SetProperty(ref _cardName, value);
	}

	public ICommand BackCommand { get; }
}
