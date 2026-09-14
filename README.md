# Zytadelle

Idle-Cell-Defense-Spiel: eine Kultur (Run) läuft in 35-Sekunden-Zyklen, endet beim Tod der
Zelle, bankt DNA, die zwischen Runs im Gene Lab in permanente Level fließt. Gebaut mit
.NET MAUI Blazor Hybrid.

## Voraussetzungen

- .NET 10 SDK
- Windows (WebView2 Runtime — auf aktuellem Windows 10/11 in der Regel schon vorhanden)

## Starten

```
dotnet run --project src/Zytadelle.App    # Spiel
dotnet run --project src/Zytadelle.Lab    # Balance Lab
dotnet build                              # alles bauen
```

## Projektlayout

| Projekt | Zweck |
|---|---|
| `Zytadelle.App` | Das Spiel selbst (MAUI Blazor Hybrid, UI) |
| `Zytadelle.Core` | Spiel-Engine: Simulation, Persistenz, kein Balancing-Wissen |
| `Zytadelle.Balancing` | Alle Balancing-Zahlen (Konstanten, Kurven, Modifier) — die einzige Quelle für Zahlen mit Balancing-Bedeutung |
| `Zytadelle.Lab.Engine` | Tuning, Kampagnen-Simulation und Suche fürs Balancing; plain C#, kein UI |
| `Zytadelle.Lab` | Balance Lab: MAUI-Blazor-Oberfläche über `Zytadelle.Lab.Engine` |

## Das Balance Lab

Werkzeug fürs Balancing: jede Zahl aus `Zytadelle.Balancing` ist im Formular einstellbar
(per Reflection erzeugt, keine manuell gepflegte Liste). Es simuliert Kampagnen mit
unterschiedlichen Gene-Lab-/In-Run-Prioritäten — entweder eine Prioritäts-Suche
(Cross-Entropy-Methode) oder ein exhaustives Raster über eine gewählte Gen-Teilmenge —
und liefert die fünf besten Builds zurück. Für jeden Build zeigt eine Leiter-Tabelle, wie
viele Runs und wie viel Zeit (simuliert und in Echtzeit bei wählbarem Speed) bis zum
jeweils nächsten Bestcycle nötig waren. Geänderte Werte lassen sich als Diff plus
C#-Schnipsel exportieren, um sie manuell in `Zytadelle.Balancing` zu übernehmen.
