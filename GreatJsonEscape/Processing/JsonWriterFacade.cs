using GreatJsonEscape.Utilities;
using System.Text;
using System.Text.Json;

namespace GreatJsonEscape.Processing;

internal static class JsonWriterFacade
{
    public static string Serialize(
        JsonElement root,
        EmbeddedJsonExpander expander,
        bool compact)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8JsonWriter(
            buffer,
            new JsonWriterOptions { Indented = !compact });

        WriteValue(root, writer, expander);
        writer.Flush();

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteValue(
        JsonElement element,
        Utf8JsonWriter writer,
        EmbeddedJsonExpander expander)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var prop in element.EnumerateObject())
                {
                    writer.WritePropertyName(prop.Name);
                    WriteValue(prop.Value, writer, expander);
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteValue(item, writer, expander);
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                var s = element.GetString();
                if (expander.TryExpand(s, out var embedded))
                    using (embedded)
                    {
                        WriteValue(embedded.RootElement, writer, expander);
                    }
                else
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