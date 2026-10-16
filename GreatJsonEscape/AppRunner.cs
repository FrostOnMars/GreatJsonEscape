using System.Text.Json;
using GreatJsonEscape.Input;
using GreatJsonEscape.Options;
using GreatJsonEscape.Processing;

namespace GreatJsonEscape;

internal static class AppRunner
{
    public static int Run(string[] args)
    {
        AppOptions options;
        try
        {
            options = AppOptions.Parse(args);
        }
        catch (ArgumentException ax)
        {
            return Fail(ax.Message, 1);
        }

        string input;
        try
        {
            input = InputProviderFactory.Create(options).Read();
        }
        catch (IOException ioEx)
        {
            return Fail("Could not read input: " + ioEx.Message, 1);
        }
        catch (UnauthorizedAccessException uaEx)
        {
            return Fail("Could not read input: " + uaEx.Message, 1);
        }

        if (string.IsNullOrWhiteSpace(input))
            return Fail("No input provided.", 1);

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
            return Fail("JSON parsing error: " + jx.Message, 2);
        }
        catch (IOException ioEx)
        {
            return Fail("Could not write output: " + ioEx.Message, 2);
        }
    }

    private static int Fail(string message, int exitCode)
    {
        Console.Error.WriteLine(message);
        WaitForKeyIfInteractive();
        return exitCode;
    }

    private static void WaitForKeyIfInteractive()
    {
        // Only wait if not being piped/redirected (interactive mode)
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
            return;

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey(true);
    }
}