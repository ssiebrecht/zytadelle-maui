using Microsoft.AspNetCore.Components;

namespace Zytadelle.Lab.Components;

/// <summary>
/// A component that follows the Lab. <see cref="LabHost.UiChanged"/> fires when something the form
/// did changed, and five times a second while a search runs - never once per campaign.
/// </summary>
public abstract class LabComponent : ComponentBase, IDisposable
{
    [Inject]
    protected LabHost Host { get; set; } = null!;

    protected override void OnInitialized() => Host.UiChanged += OnUiChanged;

    private void OnUiChanged() => InvokeAsync(StateHasChanged);

    public virtual void Dispose() => Host.UiChanged -= OnUiChanged;
}
