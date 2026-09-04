using NexTap.ViewModels;

namespace NexTap.Views;

public partial class SuccessPage : ContentPage
{
	public SuccessPage(SuccessViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await Checkmark.PlayAsync();
	}
}
