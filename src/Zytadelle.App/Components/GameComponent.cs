using Microsoft.AspNetCore.Components;
using Zytadelle.App.Game;

namespace Zytadelle.App.Components;

/// <summary>
/// A component that follows the game state. <see cref="GameHost.UiChanged"/> fires at most ten
/// times a second, so subscribing here costs ten re-renders per second - never one per frame. The
/// arena itself never goes through Blazor at all.
/// </summary>
public abstract class GameComponent : ComponentBase, IDisposable
{
    [Inject]
    protected GameHost Host { get; set; } = null!;

    protected override void OnInitialized() => Host.UiChanged += OnUiChanged;

    private void OnUiChanged() => InvokeAsync(StateHasChanged);

    public virtual void Dispose() => Host.UiChanged -= OnUiChanged;
}
