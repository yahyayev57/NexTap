using NexTap.Views;

namespace NexTap;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(ManageCardsPage), typeof(ManageCardsPage));
		Routing.RegisterRoute(nameof(AddEditCardPage), typeof(AddEditCardPage));
		Routing.RegisterRoute(nameof(ApproachPage), typeof(ApproachPage));
		Routing.RegisterRoute(nameof(SuccessPage), typeof(SuccessPage));
	}
}
