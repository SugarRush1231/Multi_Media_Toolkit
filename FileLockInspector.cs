using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace YoutubeDownloader;

internal static class FileLockInspector
{
    private const int ErrorMoreData = 234;
    private const int MaxPath = 260;
    private const int MaxAppName = 255;
    private const int MaxServiceName = 63;

    public static string GetUsageHint(string? filePath)
    {
        const string fallback = "다른 프로그램에서 파일을 사용 중입니다. 해당 프로그램을 닫고 다시 시도해 주세요.";
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return fallback;

        IReadOnlyList<string> processes = GetLockingProcesses(filePath);
        if (processes.Count == 0) return fallback;

        string fileName = Path.GetFileName(filePath);
        return $"{string.Join(", ", processes)}에서 '{fileName}' 파일을 사용 중입니다. 해당 프로그램을 닫고 다시 시도해 주세요.";
    }

    private static IReadOnlyList<string> GetLockingProcesses(string filePath)
    {
        uint sessionHandle = 0;
        var sessionKey = new StringBuilder(MaxPath);
        if (RmStartSession(out sessionHandle, 0, sessionKey) != 0) return Array.Empty<string>();

        try
        {
            if (RmRegisterResources(sessionHandle, 1, new[] { filePath }, 0, null, 0, null) != 0)
                return Array.Empty<string>();

            uint processInfoNeeded = 0;
            uint processInfoCount = 0;
            uint rebootReasons = 0;
            int result = RmGetList(sessionHandle, out processInfoNeeded, ref processInfoCount, null, ref rebootReasons);
            if (result != ErrorMoreData || processInfoNeeded == 0) return Array.Empty<string>();

            var processInfo = new RmProcessInfo[processInfoNeeded];
            processInfoCount = processInfoNeeded;
            result = RmGetList(sessionHandle, out processInfoNeeded, ref processInfoCount, processInfo, ref rebootReasons);
            if (result != 0) return Array.Empty<string>();

            var names = new List<string>();
            for (int i = 0; i < processInfoCount; i++)
            {
                string name = processInfo[i].ApplicationName?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name)) continue;

                string displayName = $"{name} (PID {processInfo[i].Process.ProcessId})";
                if (!names.Contains(displayName, StringComparer.OrdinalIgnoreCase))
                    names.Add(displayName);
            }

            return names;
        }
        catch
        {
            return Array.Empty<string>();
        }
        finally
        {
            RmEndSession(sessionHandle);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RmUniqueProcess
    {
        public int ProcessId;
        public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RmProcessInfo
    {
        public RmUniqueProcess Process;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxAppName + 1)]
        public string ApplicationName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxServiceName + 1)]
        public string ServiceShortName;

        public uint ApplicationType;
        public uint ApplicationStatus;
        public uint TerminalServicesSessionId;

        [MarshalAs(UnmanagedType.Bool)]
        public bool Restartable;
    }

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmStartSession(out uint sessionHandle, int sessionFlags, StringBuilder sessionKey);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmRegisterResources(
        uint sessionHandle,
        uint fileCount,
        string[] fileNames,
        uint applicationCount,
        RmUniqueProcess[]? applications,
        uint serviceCount,
        string[]? serviceNames);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmGetList(
        uint sessionHandle,
        out uint processInfoNeeded,
        ref uint processInfoCount,
        [In, Out] RmProcessInfo[]? processInfo,
        ref uint rebootReasons);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmEndSession(uint sessionHandle);
}
