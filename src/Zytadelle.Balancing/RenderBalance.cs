namespace Zytadelle.Balancing;

/// <summary>
/// The numbers the presentation reads: what counts as low integrity, how long an effect is drawn,
/// how far the camera pulls back. None of them change the simulation, but every one of them used to
/// be a literal written twice - once in C#, once in <c>wwwroot/js</c> - and a pair like that drifts.
///
/// They live here so there is one copy. The host hands the whole set to the renderer at boot
/// (<c>BalanceBridge</c>), which is why the JS side no longer keeps constants of its own.
/// </summary>
public static class RenderBalance
{
    /// <summary>Maximum toxins encoded for rendering; the simulation still steps the rest.</summary>
    public static int MaxProjectiles { get; set; } = 512;

    /// <summary>Seconds a pathogen shows its hit flash.</summary>
    public static double EnemyFlash { get; set; } = 0.12;

    /// <summary>Seconds the cell shows its hit flash.</summary>
    public static double CellFlash { get; set; } = 0.1;

    /// <summary>
    /// Integrity fraction below which the cell reads as in danger: the rim blinks, the plate turns,
    /// and the dish takes on a pulse. One threshold for all three.
    /// </summary>
    public static double LowIntegrityFrac { get; set; } = 0.3;

    /// <summary>
    /// A pathogen only shows a health bar once it is below this much of its own health. Bosses and
    /// tanks always show one; this is the rule for everything else.
    /// </summary>
    public static double HpBarHideAbove { get; set; } = 0.99;

    /// <summary>
    /// Hits a pathogen has to land before its heat-up shows as a glowing core. Counted in hits
    /// rather than written as a multiplier, so it follows <see cref="CombatBalance.HeatupPerHit"/>
    /// instead of silently disagreeing with it.
    /// </summary>
    public static int HeatGlowAfterHits { get; set; } = 5;

    /// <summary>Slack around the dish so its rim is never flush against the canvas edge.</summary>
    public static double CameraPad { get; set; } = 1.04;

    /// <summary>
    /// How much of the visible half-width the cell's Reach is framed to fill. Below 1 the camera
    /// leaves room past the firing line, so approaching pathogens are visible before they are hit.
    /// </summary>
    public static double CameraRangeFrac { get; set; } = 0.83;

    /// <summary>Furthest the camera pulls in on a short-Reach culture.</summary>
    public static double CameraZoomMax { get; set; } = 3.6;

    /// <summary>
    /// Seconds between UI publishes. The sim and the canvas run per frame; Blazor is told about it
    /// at this rate plus once per player action, so a re-render is never on the frame path.
    /// </summary>
    public static double UiPublishInterval { get; set; } = 0.1;

    /// <summary>Metres above the cycle-end position the ATP payout floats up from.</summary>
    public static double CycleAtpPopupY { get; set; } = -10;

    // ------------------------------------------------------------------ effect fades
    // Each of these is how long one kind of effect is actually drawn for. They are all shorter than
    // SimulationBalance.FxLifetime, which is how long the effect stays in the ring - an effect that
    // outlived its fade is simply not painted any more. Raising one past FxLifetime truncates it.

    /// <summary>Seconds the floating crit number is drawn for.</summary>
    public static double CritFxFade { get; set; } = 0.6;

    /// <summary>Seconds an incoming-hit splash is drawn for.</summary>
    public static double HitFxFade { get; set; } = 0.25;

    /// <summary>Seconds the floating cycle-end ATP number is drawn for.</summary>
    public static double AtpFxFade { get; set; } = 1;

    /// <summary>Seconds the white screen flash after a boss kill lasts.</summary>
    public static double BossFlashFade { get; set; } = 0.3;
}
