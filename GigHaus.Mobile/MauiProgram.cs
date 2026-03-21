using Microsoft.Extensions.Logging;

namespace GigHaus.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register HTTP client for API calls
        builder.Services.AddHttpClient("GigHausApi", client =>
        {
            // Base URL will be configured per-environment
            client.BaseAddress = new Uri("https://localhost:5001");
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

