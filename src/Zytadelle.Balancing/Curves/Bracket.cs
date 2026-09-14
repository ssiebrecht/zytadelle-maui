namespace Zytadelle.Balancing.Curves;

/// <summary>
/// A price step no continuous curve reproduces - Rupture Damage jumps on every tenth level past 100.
/// Every bracket whose <see cref="From"/> is reached multiplies in, so they stack.
/// </summary>
/// <param name="From">Step the jump takes effect at, counted the way the price curve counts.</param>
/// <param name="Mult">What the price is multiplied by from there on.</param>
public readonly record struct Bracket(double From, double Mult);
