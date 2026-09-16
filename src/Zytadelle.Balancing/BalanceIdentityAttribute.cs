namespace Zytadelle.Balancing;

/// <summary>
/// Marks a member the Balance Lab must not offer as a knob. Two kinds of member need it: a number
/// that names a thing rather than tuning it - an infection's tier number, say - and a view over
/// numbers the walk already reaches elsewhere, such as <c>GeneRegistry.All</c>, which would
/// otherwise present every gene curve a second time under a different path. The walk stops at a
/// marked member and does not descend into it.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class BalanceIdentityAttribute : Attribute;
