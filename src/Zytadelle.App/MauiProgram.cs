using Microsoft.Extensions.Logging;
using Zytadelle.App.Game;
using Zytadelle.Core.Persistence;

namespace Zytadelle.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddSingleton<ISaveStorage>(_ =>
            new FileSaveStorage(Path.Combine(FileSystem.AppDataDirectory, "zytadelle.save.json")));
        builder.Services.AddSingleton<GameHost>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
