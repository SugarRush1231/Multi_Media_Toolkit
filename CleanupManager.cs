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
        private static readonly string[] WebViewCacheFolderNames =
        {
            "Cache",
            "Code Cache",
            "GPUCache",
            "Media Cache",
            "DawnCache",
            "DawnWebGPUCache",
            "DawnGraphiteCache",
            "GrShaderCache",
            "ShaderCache",
            "GraphiteDawnCache",
            "component_crx_cache",
            "extensions_crx_cache",
            "VideoDecodeStats"
        };

        private static readonly string[] WebViewHistoryFileNames =
        {
            "History",
            "History-journal",
            "History Provider Cache",
            "History Provider Cache-journal",
            "Visited Links",
            "Top Sites",
            "Top Sites-journal",
            "Favicons",
            "Favicons-journal",
            "Shortcuts",
            "Shortcuts-journal",
            "DownloadMetadata",
            "Network Action Predictor",
            "Network Action Predictor-journal",
            "BrowserMetrics-spare.pma"
        };

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

        public static void DeleteCanceledDownloadArtifacts(string outputPath, bool deleteOutputFile = true)
        {
            if (string.IsNullOrWhiteSpace(outputPath)) return;

            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                outputPath + ".part",
                outputPath + ".ytdl",
                outputPath + ".part.ytdl"
            };
            if (deleteOutputFile) candidates.Add(outputPath);

            try
            {
                string? directory = Path.GetDirectoryName(outputPath);
                string baseName = Path.GetFileNameWithoutExtension(outputPath);
                if (!string.IsNullOrWhiteSpace(directory) &&
                    !string.IsNullOrWhiteSpace(baseName) &&
                    Directory.Exists(directory))
                {
                    foreach (string file in Directory.EnumerateFiles(directory, baseName + "*"))
                    {
                        string fileName = Path.GetFileName(file);
                        if (fileName.Contains(".part", StringComparison.OrdinalIgnoreCase) ||
                            fileName.EndsWith(".ytdl", StringComparison.OrdinalIgnoreCase))
                        {
                            candidates.Add(file);
                        }
                    }
                }
            }
            catch { }

            foreach (string path in candidates)
            {
                DeleteFileOrDirectoryWithRetry(path);
                ActiveFiles.TryRemove(path, out _);
            }
        }

        public static void DeleteTemporaryPath(string path)
        {
            DeleteFileOrDirectoryWithRetry(path);
            ActiveFiles.TryRemove(path, out _);
        }

        public static void DeleteStaleCookieExports()
        {
            DeleteMatchingFiles(SettingsManager.UserDataFolder, "temp*_cookies*.txt");
            DeleteMatchingFiles(Path.GetTempPath(), "mmt_cookies*.txt");
        }

        private static void DeleteMatchingFiles(string directory, string pattern)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return;

            try
            {
                foreach (string file in Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly))
                {
                    DeleteFileOrDirectoryWithRetry(file);
                    ActiveFiles.TryRemove(file, out _);
                }
            }
            catch { }
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

        public static void FullSystemCleanup(bool keepWebViewData = false)
        {
            keepWebViewData = keepWebViewData || SettingsManager.Settings.KeepLoginSession;

            Cleanup();
            if (!keepWebViewData)
            {
                DeleteDirectoryWithRetry(SettingsManager.WebViewDataFolder);
            }
            else
            {
                CleanupWebViewNonLoginData();
            }

            // [속도 최적화] WebView2 캐시 전체 삭제 대신, 민감 데이터(쿠키, 세션 등)만 선택적으로 삭제
            // 이렇게 하면 브라우저 엔진 파일은 유지되어 다음 실행 속도가 획기적으로 빨라집니다.
            string webViewCache = Path.Combine(SettingsManager.UserDataFolder, "WebView2_Cache", "EBWebView");
            if (!keepWebViewData && Directory.Exists(webViewCache))
            {
                string[] sensitiveFolders = { "Default", "Network", "Session Storage" };
                foreach (var folder in sensitiveFolders)
                {
                    string target = Path.Combine(webViewCache, folder);
                    if (Directory.Exists(target))
                    {
                        try { Directory.Delete(target, true); } catch { }
                    }
                }
            }

            // Clear App Temp
            string tempPath = Path.GetTempPath();
            // Optional: Clean only files related to this app if identifiable
            
            ForceMemoryCleanup();
        }

        public static void CleanupWebViewData(bool force = false)
        {
            if (!force && SettingsManager.Settings.KeepLoginSession) return;

            DeleteDirectoryWithRetry(SettingsManager.WebViewDataFolder);
        }

        public static void CleanupWebViewNonLoginData()
        {
            CleanupWebViewNonLoginData(SettingsManager.WebViewDataFolder);
            CleanupWebViewNonLoginData(Path.Combine(SettingsManager.WebViewDataFolder, "EBWebView"));

            string legacyWebViewCache = Path.Combine(SettingsManager.UserDataFolder, "WebView2_Cache", "EBWebView");
            CleanupWebViewNonLoginData(legacyWebViewCache);

            ForceMemoryCleanup();
        }

        private static void CleanupWebViewNonLoginData(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath)) return;

            DeleteCacheFoldersUnder(rootPath);

            foreach (var profilePath in GetWebViewProfileFolders(rootPath))
            {
                DeleteCacheFoldersUnder(profilePath);
                DeleteHistoryFilesUnder(profilePath);
            }
        }

        private static IEnumerable<string> GetWebViewProfileFolders(string rootPath)
        {
            string[] directories;
            try
            {
                directories = Directory.GetDirectories(rootPath);
            }
            catch
            {
                return Array.Empty<string>();
            }

            var profileFolders = new List<string>();
            foreach (string directory in directories)
            {
                string name = Path.GetFileName(directory);
                if (IsWebViewProfileFolder(name))
                {
                    profileFolders.Add(directory);
                }
            }

            return profileFolders;
        }

        private static bool IsWebViewProfileFolder(string name)
        {
            return name.Equals("Default", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Guest Profile", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("System Profile", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase);
        }

        private static void DeleteCacheFoldersUnder(string basePath)
        {
            foreach (string folderName in WebViewCacheFolderNames)
            {
                DeleteDirectoryWithRetry(Path.Combine(basePath, folderName), attempts: 2, delayMs: 50);
            }
        }

        private static void DeleteHistoryFilesUnder(string profilePath)
        {
            foreach (string fileName in WebViewHistoryFileNames)
            {
                DeleteFileOrDirectoryWithRetry(Path.Combine(profilePath, fileName), attempts: 2, delayMs: 50);
            }
        }

        private static void DeleteDirectoryWithRetry(string path, int attempts = 20, int delayMs = 150)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;

            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    Directory.Delete(path, true);
                    return;
                }
                catch
                {
                    if (i + 1 < attempts) System.Threading.Thread.Sleep(delayMs);
                }
            }
        }

        private static void DeleteFileOrDirectoryWithRetry(string path, int attempts = 20, int delayMs = 150)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        return;
                    }

                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                        return;
                    }

                    return;
                }
                catch
                {
                    if (i + 1 < attempts) System.Threading.Thread.Sleep(delayMs);
                }
            }
        }

        public static void ForceMemoryCleanup()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
