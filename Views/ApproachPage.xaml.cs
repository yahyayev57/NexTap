using NexTap.ViewModels;

namespace NexTap.Views;

public partial class ApproachPage : ContentPage
{
	private readonly ApproachViewModel _viewModel;

	public ApproachPage(ApproachViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
		_viewModel.Presented += OnPresented;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		Pulse.Start();
		await _viewModel.ArmAsync();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		Pulse.Stop();
		_viewModel.Disarm();
	}

	private async void OnPresented()
	{
		Pulse.Stop();
		await Shell.Current.GoToAsync(nameof(SuccessPage), new Dictionary<string, object>
		{
			["CardName"] = _viewModel.Card?.Name ?? string.Empty
		});
	}
}
