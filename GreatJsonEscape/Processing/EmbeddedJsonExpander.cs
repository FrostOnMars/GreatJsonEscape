using System.Text.Json;

namespace GreatJsonEscape.Processing;

internal sealed class EmbeddedJsonExpander
{
    public bool TryExpand(string? value, out JsonDocument? document)
    {
        document = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.TrimStart();
        if (!trimmed.StartsWith('{') && !trimmed.StartsWith('['))
            return false;

        try
        {
            document = JsonDocument.Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}