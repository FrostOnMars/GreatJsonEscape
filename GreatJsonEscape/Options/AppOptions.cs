namespace GreatJsonEscape.Options;

internal sealed record AppOptions(
    string? Input,
    string? OutputPath,
    bool Compact
)
{
    public static AppOptions Parse(string[] args)
    {
        string? input = null;
        string? output = null;
        var compact = false;

        for (var i = 0; i < args.Length; i++)
            switch (args[i])
            {
                case "--file":
                case "-f":
                    input = File.ReadAllText(args[++i]);
                    break;

                case "--out":
                case "-o":
                    output = args[++i];
                    break;

                case "--compact":
                    compact = true;
                    break;

                default:
                    input = input == null ? args[i] : input + " " + args[i];
                    break;
            }

        return new AppOptions(input, output, compact);
    }
}