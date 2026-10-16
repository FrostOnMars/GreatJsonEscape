namespace GreatJsonEscape.Options;

internal sealed record AppOptions(
    string? Input,
    string? InputPath,
    string? OutputPath,
    bool Compact
)
{
    public static AppOptions Parse(string[] args)
    {
        string? input = null;
        string? inputPath = null;
        string? output = null;
        var compact = false;

        for (var i = 0; i < args.Length; i++)
            switch (args[i])
            {
                case "--file":
                case "-f":
                    inputPath = NextValue(args, ref i, args[i]);
                    break;

                case "--out":
                case "-o":
                    output = NextValue(args, ref i, args[i]);
                    break;

                case "--compact":
                    compact = true;
                    break;

                default:
                    if (args[i].StartsWith('-'))
                        throw new ArgumentException($"Unknown option: {args[i]}");

                    if (input != null)
                        throw new ArgumentException("Only one input value may be supplied.");

                    input = args[i];
                    break;
            }

        return new AppOptions(input, inputPath, output, compact);
    }

    private static string NextValue(string[] args, ref int i, string optionName)
    {
        if (i + 1 >= args.Length)
            throw new ArgumentException($"Option '{optionName}' requires a value.");

        return args[++i];
    }
}