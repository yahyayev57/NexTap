using NexTap.ViewModels;

namespace NexTap.Views;

public partial class AddEditCardPage : ContentPage
{
	public AddEditCardPage(AddEditCardViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
