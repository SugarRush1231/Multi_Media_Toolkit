using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace YoutubeDownloader
{
    public static class CleanupManager
    {
        private static readonly ConcurrentDictionary<string, byte> ActiveFiles = new ConcurrentDictionary<string, byte>();
        private static readonly ConcurrentDictionary<Process, byte> ActiveProcesses = new ConcurrentDictionary<Process, byte>();

        public static void RegisterFile(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                ActiveFiles.TryAdd(path, 0);
            }
        }

        public static void UnregisterFile(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                ActiveFiles.TryRemove(path, out _);
            }
        }

        public static void RegisterProcess(Process p)
        {
            if (p != null)
            {
                ActiveProcesses.TryAdd(p, 0);
            }
        }

        public static void UnregisterProcess(Process p)
        {
            if (p != null)
            {
                ActiveProcesses.TryRemove(p, out _);
            }
        }

        public static void Cleanup()
        {
            // 1. Kill active processes
            foreach (var p in ActiveProcesses.Keys)
            {
                try
                {
                    if (!p.HasExited)
                    {
                        p.Kill(true);
                    }
                }
                catch { }
            }

            // 2. Delete partial files (with retry to handle locks)
            foreach (var f in ActiveFiles.Keys)
            {
                for (int i = 0; i < 15; i++) // Try for ~3 seconds
                {
                    try
                    {
                        if (File.Exists(f))
                        {
                            File.Delete(f);
                            break;
                        }
                        else if (Directory.Exists(f))
                        {
                            Directory.Delete(f, true);
                            break;
                        }
                        else break;
                    }
                    catch { System.Threading.Thread.Sleep(200); }
                }
            }
            
            ActiveFiles.Clear();
            ActiveProcesses.Clear();
            
            // 3. Clear WebView2 User Data if possible (Optional, but helps with storage)
            // This is usually in %LOCALAPPDATA%\YoutubeDownloader\EBWebView
        }

        public static void FullSystemCleanup()
        {
            Cleanup();

            // Clear WebView2 Cache (Fixed to use the correct path we now use)
            string webViewCache = Path.Combine(SettingsManager.UserDataFolder, "WebView2_Cache");
            if (Directory.Exists(webViewCache))
            {
                try { Directory.Delete(webViewCache, true); } catch { }
            }

            // Clear App Temp
            string tempPath = Path.GetTempPath();
            // Optional: Clean only files related to this app if identifiable
            
            ForceMemoryCleanup();
        }

        public static void ForceMemoryCleanup()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
