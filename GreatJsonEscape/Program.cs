using System.Runtime.InteropServices;
using System.Text;
using GreatJsonEscape.UI;

namespace GreatJsonEscape;

internal static class Program
{
    private const int AttachParentProcess = -1;

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachConsole(int processId);

    [STAThread]
    private static int Main(string[] args)
    {
        // This is a WinExe, so it has no console of its own. Try to borrow the
        // launching terminal's console. This also tells us whether we were
        // started from a terminal at all, since it fails when launched from
        // Explorer or from the Visual Studio debugger.
        var hasConsole = AttachConsole(AttachParentProcess);

        if (ShouldRunGui(args, hasConsole))
            return RunGui();

        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            return AppRunner.Run(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Unexpected error:");
            Console.Error.WriteLine(ex);
            return 3;
        }
    }

    private static bool ShouldRunGui(string[] args, bool hasConsole)
    {
        // Any argument means the caller wants the command-line workflow.
        if (args.Length > 0)
            return false;

        // With no arguments, only use the CLI when input is genuinely being
        // piped in from a real console. Checking IsInputRedirected alone is
        // not enough: a WinExe launched from a debugger or by double-click
        // always reports redirected handles even though nothing is piped in.
        return !(hasConsole && Console.IsInputRedirected);
    }

    private static int RunGui()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }
}