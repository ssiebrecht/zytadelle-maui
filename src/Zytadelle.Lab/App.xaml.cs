namespace Zytadelle.Lab;

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
        var window = new Window(new MainPage()) { Title = "Zytadelle Balance Lab" };

        // Three panes side by side need the width; the stylesheet stacks them below 1100 px.
        window.Width = 1680;
        window.Height = 1000;
        window.MinimumWidth = 520;
        window.MinimumHeight = 620;

        // A search can be hours long and the form is worth keeping either way.
        window.Destroying += (_, _) =>
        {
            var host = _services.GetService<LabHost>();
            host?.Cancel();
            host?.Persist();
        };

        return window;
    }
}
