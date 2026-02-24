using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GreatJsonEscape
{
    class Program
    {
        private const string Terminator = "__END__";

        static int Main(string[] args)
        {
            string? input = null;
            string? outPath = null;
            bool compact = false;

            // Simple args parse: --file <path>  --out <path>  --compact
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--file":
                    case "-f":
                        if (i + 1 >= args.Length)
                        {
                            Console.Error.WriteLine("Missing value for --file");
                            return 1;
                        }
                        input = File.ReadAllText(args[++i]);
                        break;

                    case "--out":
                    case "-o":
                        if (i + 1 >= args.Length)
                        {
                            Console.Error.WriteLine("Missing value for --out");
                            return 1;
                        }
                        outPath = args[++i];
                        break;

                    case "--compact":
                        compact = true;
                        break;

                    default:
                        // If user passed raw text as args, collect it
                        input = (input == null) ? args[i] : input + " " + args[i];
                        break;
                }
            }

            if (input == null)
            {
                if (Console.IsInputRedirected)
                {
                    using var reader = new StreamReader(Console.OpenStandardInput());
                    input = reader.ReadToEnd();
                }
                else
                {
                    input = ReadInteractiveMultiline();
                }
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.Error.WriteLine("No input provided.");
                return 1;
            }

            try
            {
                string output;

                // Path A: Input is a JSON document (object/array)
                if (LooksLikeJsonDocument(input))
                {
                    using var doc = JsonDocument.Parse(input);
                    output = TransformAndSerialize(doc.RootElement, compact);
                }
                else
                {
                    // Path B: Input is a JSON string literal or raw escaped string
                    // Normalize to a JSON string literal and decode it.
                    string jsonStringLiteral = EnsureQuoted(input);
                    string decoded = JsonSerializer.Deserialize<string>(jsonStringLiteral)!;

                    // If the decoded content is JSON, parse & transform, else output the decoded text as is.
                    if (LooksLikeJsonDocument(decoded))
                    {
                        using var innerDoc = JsonDocument.Parse(decoded);
                        output = TransformAndSerialize(innerDoc.RootElement, compact);
                    }
                    else
                    {
                        output = decoded;
                    }
                }

                if (!string.IsNullOrEmpty(outPath))
                {
                    File.WriteAllText(outPath, output);
                }
                else
                {
                    Console.WriteLine(output);
                }

                return 0;
            }
            catch (JsonException jx)
            {
                Console.Error.WriteLine("JSON parsing error: " + jx.Message);
                return 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Unexpected error: " + ex);
                return 3;
            }
        }

        // ===== Helpers =====

        private static string ReadInteractiveMultiline()
        {
            Console.WriteLine("Paste your JSON (supports multiple lines).");
            Console.WriteLine($"If your JSON contains embedded JSON *as strings*, I will decode them recursively.");
            Console.WriteLine($"When finished, type a line with only {Terminator} and press Enter.\n");

            var sb = new StringBuilder();
            string? line;
            while ((line = Console.ReadLine()) != null)
            {
                if (line == Terminator)
                    break;

                sb.AppendLine(line);
            }

            return sb.ToString();
        }

        private static bool LooksLikeJsonDocument(string s)
        {
            // Fast heuristic: trims outer whitespace and checks for starting brace/bracket.
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.TrimStart();
            return s.StartsWith("{") || s.StartsWith("[");
        }

        private static string EnsureQuoted(string input)
        {
            string candidate = input.Trim();
            if (candidate.Length >= 2 && candidate[0] == '"' && candidate[^1] == '"')
                return candidate;
            return $"\"{candidate}\"";
        }

        private static string TransformAndSerialize(JsonElement root, bool compact)
        {
            var sb = new ArrayBufferWriter<byte>();
            var options = new JsonWriterOptions
            {
                Indented = !compact,
                SkipValidation = false
            };
            using var writer = new Utf8JsonWriter(sb, options);
            WriteTransformed(root, writer);
            writer.Flush();
            return Encoding.UTF8.GetString(sb.WrittenSpan);
        }

        /// <summary>
        /// Recursively writes a JsonElement, but whenever it encounters a string that is itself valid JSON,
        /// it parses that string and writes the parsed JSON inline (recursively).
        /// </summary>
        private static void WriteTransformed(JsonElement element, Utf8JsonWriter writer)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (var prop in element.EnumerateObject())
                    {
                        writer.WritePropertyName(prop.Name);
                        if (prop.Value.ValueKind == JsonValueKind.String)
                        {
                            if (TryParseEmbeddedJson(prop.Value.GetString(), out var embedded))
                            {
                                using (embedded)
                                {
                                    WriteTransformed(embedded.RootElement, writer);
                                }
                            }
                            else
                            {
                                writer.WriteStringValue(prop.Value.GetString());
                            }
                        }
                        else
                        {
                            WriteTransformed(prop.Value, writer);
                        }
                    }
                    writer.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in element.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            if (TryParseEmbeddedJson(item.GetString(), out var embedded))
                            {
                                using (embedded)
                                {
                                    WriteTransformed(embedded.RootElement, writer);
                                }
                            }
                            else
                            {
                                writer.WriteStringValue(item.GetString());
                            }
                        }
                        else
                        {
                            WriteTransformed(item, writer);
                        }
                    }
                    writer.WriteEndArray();
                    break;

                case JsonValueKind.String:
                    {
                        var s = element.GetString();
                        if (TryParseEmbeddedJson(s, out var embedded))
                        {
                            using (embedded)
                            {
                                WriteTransformed(embedded.RootElement, writer);
                            }
                        }
                        else
                        {
                            writer.WriteStringValue(s);
                        }
                        break;
                    }

                case JsonValueKind.Number:
                    writer.WriteRawValue(element.GetRawText());
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    writer.WriteRawValue(element.GetRawText());
                    break;

                case JsonValueKind.Null:
                    writer.WriteNullValue();
                    break;

                default:
                    // Fallback to raw (covers Undefined)
                    writer.WriteRawValue(element.GetRawText());
                    break;
            }
        }

        /// <summary>
        /// Attempts to parse a string as JSON (object or array).
        /// Returns true if successful and outputs a JsonDocument.
        /// </summary>
        private static bool TryParseEmbeddedJson(string? s, out JsonDocument? doc)
        {
            doc = null;
            if (string.IsNullOrWhiteSpace(s)) return false;

            // Quick check to avoid throwing for obvious non-JSON strings
            var trimmed = s.TrimStart();
            if (!(trimmed.StartsWith("{") || trimmed.StartsWith("[")))
                return false;

            try
            {
                doc = JsonDocument.Parse(s);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    // Minimal ArrayBufferWriter used for buffering Utf8JsonWriter output without external packages
    internal sealed class ArrayBufferWriter<T> : IBufferWriter<T>
    {
        private T[] _buffer;
        private int _index;

        public ArrayBufferWriter(int initialCapacity = 256)
        {
            _buffer = new T[initialCapacity];
            _index = 0;
        }

        public ReadOnlyMemory<T> WrittenMemory => new ReadOnlyMemory<T>(_buffer, 0, _index);
        public ReadOnlySpan<T> WrittenSpan => new ReadOnlySpan<T>(_buffer, 0, _index);

        public void Advance(int count) => _index += count;

        public Memory<T> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return new Memory<T>(_buffer, _index, _buffer.Length - _index);
        }

        public Span<T> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return new Span<T>(_buffer, _index, _buffer.Length - _index);
        }

        private void EnsureCapacity(int sizeHint)
        {
            if (sizeHint == 0) sizeHint = 256;
            if (_buffer.Length - _index >= sizeHint) return;

            int newSize = Math.Max(_buffer.Length * 2, _index + sizeHint);
            Array.Resize(ref _buffer, newSize);
        }
    }
}