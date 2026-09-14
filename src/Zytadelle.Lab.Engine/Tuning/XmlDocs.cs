using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Zytadelle.Lab.Engine.Tuning;

/// <summary>
/// The <c>///</c> lines of the balance project, read back at runtime from the XML the compiler
/// emits next to the assembly. Every balancing constant in this codebase carries a one-line
/// summary saying what it is; showing that as the hint on its input is free documentation, and it
/// cannot go stale the way a second list of labels would.
///
/// Positional records keep their prose in a <c>param</c> on the type rather than on the property,
/// so a property with no entry of its own falls back to its declaring type's parameter of the same
/// name. That is what keeps the enemy table and the infection tiers annotated.
/// </summary>
public sealed partial class XmlDocs
{
    private static readonly XmlDocs None = new(new Dictionary<string, string>());

    private readonly Dictionary<string, string> _text;

    private XmlDocs(Dictionary<string, string> text) => _text = text;

    public static XmlDocs Load(Assembly assembly)
    {
        var path = Path.ChangeExtension(assembly.Location, ".xml");
        if (string.IsNullOrEmpty(assembly.Location) || !File.Exists(path)) return None;

        try
        {
            var text = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var m in XDocument.Load(path).Descendants("member"))
            {
                if (m.Attribute("name")?.Value is not { Length: > 0 } name) continue;
                if (Flatten(m.Element("summary")) is { Length: > 0 } summary) text[name] = summary;
                foreach (var p in m.Elements("param"))
                    if (p.Attribute("name")?.Value is { Length: > 0 } pn && Flatten(p) is { Length: > 0 } pt)
                        text[$"{name}/{pn}"] = pt;
            }

            return new XmlDocs(text);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            // A missing or malformed doc file costs hints, nothing else. Never worth failing over.
            return None;
        }
    }

    /// <summary>The one-line summary for a member, or null when it carries none.</summary>
    public string? Summary(MemberInfo member)
    {
        var kind = member is FieldInfo ? 'F' : 'P';
        var declaring = member.DeclaringType;
        if (declaring is null) return null;

        var typeKey = $"T:{Name(declaring)}";
        return _text.GetValueOrDefault($"{kind}:{Name(declaring)}.{member.Name}")
               ?? _text.GetValueOrDefault($"{typeKey}/{member.Name}");
    }

    /// <summary>The name the doc file uses: nested types are separated by a plus sign there too.</summary>
    private static string Name(Type t) => t.FullName?.Replace('+', '.') ?? t.Name;

    /// <summary>
    /// Doc XML is indented prose with inline tags. Strip the tags to their text, collapse the
    /// whitespace the indentation left behind, and the result fits on one line of the form.
    /// </summary>
    private static string? Flatten(XElement? element)
    {
        if (element is null) return null;
        var raw = string.Concat(element.Nodes().Select(n => n switch
        {
            XText t => t.Value,
            XElement e when e.Name == "see" || e.Name == "paramref" || e.Name == "typeparamref" =>
                Last(e.Attribute("cref")?.Value ?? e.Attribute("name")?.Value ?? e.Value),
            XElement e => e.Value,
            _ => string.Empty,
        }));
        return Whitespace().Replace(raw, " ").Trim();
    }

    /// <summary>A cref reads <c>P:Zytadelle.Balancing.CycleBalance.Cooldown</c>; only the last part reads.</summary>
    private static string Last(string cref) => cref[(cref.LastIndexOf('.') + 1)..];

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
