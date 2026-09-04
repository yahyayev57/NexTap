using Microsoft.Extensions.Logging;
using NexTap.Services;
using NexTap.ViewModels;
using NexTap.Views;

namespace NexTap;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				// No custom fonts bundled yet - falls back to the platform
				// default (Roboto on Android), which already reads soft/modern.
				// Drop .ttf files into Resources/Fonts and register them here
				// if you want a custom typeface later.
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Services (singletons - the wallet and the NFC radio are both
		// "one instance for the whole app" kind of things)
		builder.Services.AddSingleton<ICardStoreService, CardStoreService>();
		builder.Services.AddSingleton<INfcService, NfcService>();

		// ViewModels
		builder.Services.AddSingleton<WalletViewModel>();
		builder.Services.AddTransient<ManageCardsViewModel>();
		builder.Services.AddTransient<AddEditCardViewModel>();
		builder.Services.AddTransient<ApproachViewModel>();
		builder.Services.AddTransient<SuccessViewModel>();

		// Pages
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddTransient<ManageCardsPage>();
		builder.Services.AddTransient<AddEditCardPage>();
		builder.Services.AddTransient<ApproachPage>();
		builder.Services.AddTransient<SuccessPage>();

		return builder.Build();
	}
}
