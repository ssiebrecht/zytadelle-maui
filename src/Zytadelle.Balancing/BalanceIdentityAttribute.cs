namespace Zytadelle.Balancing;

/// <summary>
/// Marks a number that names a thing rather than tuning it - an infection's tier number, say.
/// The Balance Lab walks this project by reflection and would otherwise offer such a value as a
/// knob, and turning it would break the lookup that number exists for.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class BalanceIdentityAttribute : Attribute;
