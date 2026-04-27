namespace GreatJsonEscape;

internal class Program
{
    static int Main(string[] args)
    {
        try
        {
            return AppRunner.Run(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Unexpected error:");
            Console.Error.WriteLine(ex);
            return 3;
        }
    }
}