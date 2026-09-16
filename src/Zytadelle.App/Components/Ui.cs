using Zytadelle.App.Game;

using Zytadelle.Core.Upgrades;

namespace Zytadelle.App.Components;

/// <summary>Names and labels the markup needs. The sprite ids match the gene ids one to one.</summary>
public static class Ui
{
    public static string IconFor(GeneId id)
    {
        var s = id.ToString();
        return char.ToLowerInvariant(s[0]) + s[1..];
    }

    public static string TabIcon(GeneTab tab) => tab switch
    {
        GeneTab.Attack => "offense",
        GeneTab.Defense => "barrier",
        _ => "metabolism",
    };

    public static string TabLabel(GeneTab tab) => tab switch
    {
        GeneTab.Attack => "Offense",
        GeneTab.Defense => "Barrier",
        _ => "Metabolism",
    };

    public static readonly GeneTab[] Tabs = [GeneTab.Attack, GeneTab.Defense, GeneTab.Utility];

    public static readonly (HubView View, string Label, string Icon)[] HubNav =
    [
        (HubView.Cell, "Cell", "cell"),
        (HubView.Genome, "Genome", "dna"),
        (HubView.Missions, "Missions", "missions"),
        (HubView.Records, "Records", "records"),
    ];

    public static string KindLabel(EnemyKind kind) => kind switch
    {
        EnemyKind.Basic => "bacilli",
        EnemyKind.Fast => "spirochetes",
        EnemyKind.Tank => "staphylococci",
        EnemyKind.Ranged => "phages",
        _ => "paramecia",
    };
}
