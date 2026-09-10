using JobApplyAI.Mobile.Services;
using JobApplyAI.Mobile.ViewModels;
using JobApplyAI.Mobile.Views;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;

namespace JobApplyAI.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseLocalNotification()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddTransient<AuthHeaderHandler>();
		builder.Services
			.AddHttpClient<NextRoleApiClient>()
			.AddHttpMessageHandler<AuthHeaderHandler>();

		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<ResumesViewModel>();
		builder.Services.AddTransient<JobSearchViewModel>();
		builder.Services.AddTransient<ApplicationsViewModel>();
		builder.Services.AddTransient<CoverLettersViewModel>();
		builder.Services.AddTransient<InterviewPrepViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();

		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<ResumesPage>();
		builder.Services.AddTransient<JobSearchPage>();
		builder.Services.AddTransient<ApplicationsPage>();
		builder.Services.AddTransient<CoverLettersPage>();
		builder.Services.AddTransient<InterviewPrepPage>();
		builder.Services.AddTransient<SettingsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

