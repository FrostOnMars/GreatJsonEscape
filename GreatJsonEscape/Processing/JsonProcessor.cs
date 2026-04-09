using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace GreatJsonEscape.Processing;

internal sealed class JsonProcessor(bool compact)
{
    private readonly EmbeddedJsonExpander _expander = new();

    public string Process(string input)
    {
        var current = input;

        // Try for a reasonable input depth limit, like 5 levels
        for (var depth = 0; depth < 5; depth++)
        {
            if (LooksLikeJsonDocument(current))
            {
                using var doc = JsonDocument.Parse(current);
                return JsonWriterFacade.Serialize(doc.RootElement, _expander, compact);
            }
            
            try
            {
                var decoded = DecodeString(current);

                // If decoding didn't change anything, we're done
                if (decoded == current)
                {
                    return current;
                }

                current = decoded;
            }
            catch (JsonException)
            {
                // If we can't decode it, return what we have
                return current;

            }
        }

        //Hit the depth limit, return what we have
        return current;
    }

    private static string DecodeString(string input)
    {
        var quoted = input.Trim().StartsWith('\"') ? input : $"\'{input}'";
        return JsonSerializer.Deserialize<string>(quoted)!;
    }

    private static bool LooksLikeJsonDocument(string s)
    {
        s = s.TrimStart();
        return s.StartsWith('{') || s.StartsWith('[');
    }
}