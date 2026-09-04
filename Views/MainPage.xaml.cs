using NexTap.ViewModels;

namespace NexTap.Views;

public partial class MainPage : ContentPage
{
	private readonly WalletViewModel _viewModel;

	public MainPage(WalletViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.LoadAsync();
	}
}
