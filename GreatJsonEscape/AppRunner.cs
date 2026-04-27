using System.Text.Json;
using GreatJsonEscape.Input;
using GreatJsonEscape.Options;
using GreatJsonEscape.Processing;

namespace GreatJsonEscape;

internal sealed class AppRunner
{
    public static int Run(string[] args)
    {
        var options = AppOptions.Parse(args);

        var inputProvider = InputProviderFactory.Create(options);
        var input = inputProvider.Read();

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.Error.WriteLine("No input provided.");
            WaitForKeyIfInteractive();
            return 1;
        }

        try
        {
            var processor = new JsonProcessor(options.Compact);
            var output = processor.Process(input);

            if (!string.IsNullOrEmpty(options.OutputPath))
                File.WriteAllText(options.OutputPath, output);
            else
                Console.WriteLine(output);
            WaitForKeyIfInteractive();
            return 0;
        }
        catch (JsonException jx)
        {
            Console.Error.WriteLine("JSON parsing error: " + jx.Message);
            return 2;
        }
    }
    private static void WaitForKeyIfInteractive()
    {
        // Only wait if not being piped/redirected (interactive mode)
        if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey(true);
        }
    }
}