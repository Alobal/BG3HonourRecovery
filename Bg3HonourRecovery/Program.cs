namespace Bg3HonourRecovery;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--verify-runtime", StringComparer.OrdinalIgnoreCase))
        {
            return RuntimeVerifier.Run();
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(new ProfileRecoveryService()));
        return 0;
    }
}
