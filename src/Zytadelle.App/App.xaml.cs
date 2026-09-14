namespace Zytadelle.App;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new MainPage()) { Title = "Zytadelle" };

        // Wide enough for the desktop layout, which puts the panel beside the dish at 900 px.
        window.Width = 1280;
        window.Height = 860;
        window.MinimumWidth = 420;
        window.MinimumHeight = 560;

        // The browser build saved on beforeunload; this is the same moment.
        window.Destroying += (_, _) => _services.GetService<Game.GameHost>()?.Persist();

        return window;
    }
}
