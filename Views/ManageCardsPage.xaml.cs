using NexTap.ViewModels;

namespace NexTap.Views;

public partial class ManageCardsPage : ContentPage
{
	private readonly ManageCardsViewModel _viewModel;

	public ManageCardsPage(ManageCardsViewModel viewModel)
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
