namespace GreatJsonEscape.Input;

internal sealed class FixedInputProvider(string input) : IInputProvider
{
    public string Read()
    {
        return input;
    }
}

internal sealed class RedirectedInputProvider : IInputProvider
{
    public string Read()
    {
        using var reader = new StreamReader(Console.OpenStandardInput());
        return reader.ReadToEnd();
    }
}