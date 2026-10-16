using GreatJsonEscape.Options;

namespace GreatJsonEscape.Input;

internal static class InputProviderFactory
{
    public static IInputProvider Create(AppOptions options)
    {
        if (options.InputPath != null)
            return new FileInputProvider(options.InputPath);

        if (options.Input != null)
            return new FixedInputProvider(options.Input);

        if (Console.IsInputRedirected)
            return new RedirectedInputProvider();

        return new ConsoleInputProvider();
    }
}