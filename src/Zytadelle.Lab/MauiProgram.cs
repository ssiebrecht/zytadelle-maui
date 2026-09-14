using Microsoft.Extensions.Logging;
using Zytadelle.Core.Persistence;

namespace Zytadelle.Lab;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();

        // The Lab keeps its own file next to the game's save, and reuses the game's storage seam so
        // a blocked or missing file costs the form, never the process.
        builder.Services.AddSingleton<ISaveStorage>(_ =>
            new FileSaveStorage(Path.Combine(FileSystem.AppDataDirectory, "zytadelle.lab.json")));
        builder.Services.AddSingleton<LabHost>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
