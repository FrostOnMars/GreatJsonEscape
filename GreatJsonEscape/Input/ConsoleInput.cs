using System.Text;

namespace GreatJsonEscape.Input;

internal sealed class ConsoleInputProvider : IInputProvider
{
    private const string Terminator = "__END";

    public string Read()
    {
        Console.WriteLine("Paste your JSON. Type __END on its own line to finish.\n");

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
}