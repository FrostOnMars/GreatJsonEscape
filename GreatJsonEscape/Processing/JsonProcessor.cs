using System.Text.Json;

namespace GreatJsonEscape.Processing;

internal sealed class JsonProcessor(bool compact)
{
    private const int MaxUnescapeDepth = 5;

    private readonly EmbeddedJsonExpander _expander = new();

    public string Process(string input)
    {
        var current = input;

        for (var depth = 0; depth < MaxUnescapeDepth; depth++)
        {
            // Only a successful parse proves we've reached real JSON. A string
            // such as {\"id\":1} still "looks like" a document but is escaped,
            // so a failed parse here must fall through to another decode pass.
            if (TryParseJsonDocument(current, out var doc))
                using (doc)
                {
                    return JsonWriterFacade.Serialize(doc!.RootElement, _expander, compact);
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

        // Hit the depth limit, return what we have
        return current;
    }

    private static string DecodeString(string input)
    {
        var trimmed = input.Trim();

        // If the text is already a complete JSON string literal, unescape it.
        // Otherwise it is the *contents* of one (e.g. {\"id\":1}), so wrap it
        // in literal quotes to let the reader interpret its escape sequences.
        //
        // Do NOT use JsonSerializer.Serialize to build the wrapper: that
        // escapes the backslashes instead of interpreting them, making the
        // round-trip an identity operation that strips nothing.
        var quoted = IsQuotedStringLiteral(trimmed)
            ? trimmed
            : string.Concat("\"", trimmed, "\"");

        return JsonSerializer.Deserialize<string>(quoted)!;
    }

    private static bool IsQuotedStringLiteral(string s)
    {
        if (s.Length < 2 || s[0] != '"' || s[^1] != '"')
            return false;

        // The trailing quote must not itself be escaped, as in "\"foo\"".
        var backslashes = 0;
        for (var i = s.Length - 2; i >= 0 && s[i] == '\\'; i--)
            backslashes++;

        return backslashes % 2 == 0;
    }

    private static bool TryParseJsonDocument(string s, out JsonDocument? document)
    {
        document = null;

        if (!LooksLikeJsonDocument(s))
            return false;

        try
        {
            document = JsonDocument.Parse(s);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool LooksLikeJsonDocument(string s)
    {
        s = s.TrimStart();
        return s.StartsWith('{') || s.StartsWith('[');
    }
}