using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GreatJsonEscape
{
    class Program
    {
        private const string Terminator = "__END";

        static int Main(string[] args)
        {
            string? input = null;
            string? outPath = null;
            var compact = false;

            // Simple args parse: --file <path>  --out <path>  --compact
            for (var i = 0; i < args.Length; i++)
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
                    var jsonStringLiteral = EnsureQuoted(input);
                    var decoded = JsonSerializer.Deserialize<string>(jsonStringLiteral)!;

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

        // ===== High-level helpers =====
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
            return s.StartsWith($"{{") || s.StartsWith($"[");
        }

        private static string EnsureQuoted(string input)
        {
            var candidate = input.Trim();
            if (candidate is ['"', _, ..] && candidate[^1] == '"')
                return candidate;
            return $"\"{candidate}\"";
        }
        
        // ===== Core transformation =====
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
                        WriteAnyValue(prop.Value, writer);
                    }
                    writer.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in element.EnumerateArray())
                    {
                        WriteAnyValue(item, writer);
                    }
                    writer.WriteEndArray();
                    break;

                case JsonValueKind.String:
                    WriteAnyValue(element, writer);
                    break;

                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    writer.WriteRawValue(element.GetRawText());
                    break;

                case JsonValueKind.Null:
                    writer.WriteNullValue();
                    break;

                default:
                    writer.WriteRawValue(element.GetRawText());
                    break;
            }
        }
        /// <summary>
        /// Centralizes the logic for detecting and expanding embedded JSON strings, keeping that concern out of the structural traversal code.
        ///     If value is a string, parse as embedded JSON object or array.
        ///     If value is not a string, delegate back to WriteTransformed.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="writer"></param>
        private static void WriteAnyValue(JsonElement element, Utf8JsonWriter writer)
        {
            if (element.ValueKind == JsonValueKind.String)
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

                return;
            }

            WriteTransformed(element, writer);
        }

        // ===== Parsing helpers =====
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
            if (!(trimmed.StartsWith($"{{") || trimmed.StartsWith($"[")))
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

    // ===== Utilities =====

    // Minimal ArrayBufferWriter used for buffering Utf8JsonWriter output without external packages
    internal sealed class ArrayBufferWriter<T>(int initialCapacity = 256) : IBufferWriter<T>
    {
        private T[] _buffer = new T[initialCapacity];
        private int _index = 0;

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

            var newSize = Math.Max(_buffer.Length * 2, _index + sizeHint);
            Array.Resize(ref _buffer, newSize);
        }
    }
}