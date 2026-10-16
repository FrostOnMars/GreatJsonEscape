using GreatJsonEscape.Utilities;
using System.Text;
using System.Text.Json;

namespace GreatJsonEscape.Processing;

internal static class JsonWriterFacade
{
    private const int MaxExpansionDepth = 32;

    public static string Serialize(
        JsonElement root,
        EmbeddedJsonExpander expander,
        bool compact)
    {
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(
                   buffer,
                   new JsonWriterOptions { Indented = !compact }))
        {
            WriteValue(root, writer, expander, 0);
            writer.Flush();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteValue(
        JsonElement element,
        Utf8JsonWriter writer,
        EmbeddedJsonExpander expander,
        int depth)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var prop in element.EnumerateObject())
                {
                    writer.WritePropertyName(prop.Name);
                    WriteValue(prop.Value, writer, expander, depth + 1);
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteValue(item, writer, expander, depth + 1);
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                var s = element.GetString();
                if (depth < MaxExpansionDepth && expander.TryExpand(s, out var embedded))
                    using (embedded)
                    {
                        if (embedded != null)
                        {
                            WriteValue(embedded.RootElement, writer, expander, depth + 1);
                            break;
                        }
                    }

                writer.WriteStringValue(s);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;

            default:
                writer.WriteRawValue(element.GetRawText());
                break;
        }
    }
}