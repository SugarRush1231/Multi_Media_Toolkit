namespace YoutubeDownloader;

static class Program
{
    private const string SingleInstanceMutexName = @"Local\MultiMediaToolkit.SingleInstance";
    private const string ActivateExistingEventName = @"Local\MultiMediaToolkit.ActivateExisting";
    internal static bool StartedAfterUpdate { get; private set; }
    internal static bool StartedAfterFailedUpdate { get; private set; }

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        StartedAfterUpdate = args.Any(arg =>
            arg.Equals("/updated", StringComparison.OrdinalIgnoreCase));
        StartedAfterFailedUpdate = args.Any(arg =>
            arg.Equals("/updatefailed", StringComparison.OrdinalIgnoreCase));

        using var activateExistingEvent = new EventWaitHandle(
            false,
            EventResetMode.AutoReset,
            ActivateExistingEventName);
        using var singleInstanceMutex = new Mutex(false, SingleInstanceMutexName);

        bool ownsSingleInstance;
        try
        {
            int waitMilliseconds = StartedAfterUpdate || StartedAfterFailedUpdate ? 15000 : 0;
            ownsSingleInstance = singleInstanceMutex.WaitOne(waitMilliseconds);
        }
        catch (AbandonedMutexException)
        {
            ownsSingleInstance = true;
        }

        if (!ownsSingleInstance)
        {
            try { activateExistingEvent.Set(); } catch { }
            return;
        }

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        try
        {
            ApplicationConfiguration.Initialize();
            using var mainForm = new Form1();
            RegisteredWaitHandle? activationWait = ThreadPool.RegisterWaitForSingleObject(
                activateExistingEvent,
                (_, timedOut) =>
                {
                    if (timedOut || mainForm.IsDisposed || !mainForm.IsHandleCreated) return;
                    try
                    {
                        mainForm.BeginInvoke(new Action(mainForm.ActivateFromSecondLaunch));
                    }
                    catch { }
                },
                null,
                Timeout.Infinite,
                executeOnlyOnce: false);

            try
            {
                Application.Run(mainForm);
            }
            finally
            {
                activationWait.Unregister(null);
            }
        }
        finally
        {
            try { singleInstanceMutex.ReleaseMutex(); } catch { }
        }
    }    
}
