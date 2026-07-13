namespace YoutubeDownloader;

static class Program
{
    internal static bool StartedAfterUpdate { get; private set; }

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        StartedAfterUpdate = args.Any(arg =>
            arg.Equals("/updated", StringComparison.OrdinalIgnoreCase));

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }    
}
