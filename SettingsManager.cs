using System;
using System.IO;
using System.Text.Json;

namespace YoutubeDownloader;

public class LoginBrowserBookmark
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int SortOrder { get; set; } = int.MaxValue;
}

public class LoginBrowserBuiltInAppLayout
{
    public string AppId { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int SortOrder { get; set; } = int.MaxValue;
}

public class AppSettings
{
    public string DefaultDownloadFolder { get; set; } = string.Empty;
    public bool ShowNotifications { get; set; } = true;
    public bool AutoOpenFolder { get; set; } = false;
    public bool AutoUpdateCheck { get; set; } = true;
    public bool KeepLoginSession { get; set; } = false;
    public bool UseSiteFolderRules { get; set; } = false;
    public bool UseCustomSiteFolders { get; set; } = false;
    public System.Collections.Generic.Dictionary<string, string> SiteFolderOverrides { get; set; } = new System.Collections.Generic.Dictionary<string, string>();
    public string FileNamePreset { get; set; } = "Title";
    public string CustomFileNameTemplate { get; set; } = "{title}";
    public string DefaultVideoQuality { get; set; } = "Best";
    public string SubtitleLanguagePreset { get; set; } = "Ko";
    public bool EnableWidgetMode { get; set; } = false;
    public bool EnableCompletedFileQuickUse { get; set; } = false;
    public int WidgetLocationX { get; set; } = int.MinValue;
    public int WidgetLocationY { get; set; } = int.MinValue;
    public string LastSeenVersion { get; set; } = ""; // 기본값은 비워둠 (신규 유저 구분을 위함)
    public string SkippedUpdateVersion { get; set; } = "";
    public string LastHeartbeatDate { get; set; } = "";
    public string InstallId { get; set; } = ""; // 8자리 익명 기기 ID
    public System.Collections.Generic.Dictionary<string, int> UsageStats { get; set; } = new System.Collections.Generic.Dictionary<string, int>();
    public System.Collections.Generic.List<string> DailyDownloadHistory { get; set; } = new System.Collections.Generic.List<string>();
    public System.Collections.Generic.List<LoginBrowserBookmark> LoginBrowserBookmarks { get; set; } = new System.Collections.Generic.List<LoginBrowserBookmark>();
    public System.Collections.Generic.List<LoginBrowserBuiltInAppLayout> LoginBrowserBuiltInAppLayouts { get; set; } = new System.Collections.Generic.List<LoginBrowserBuiltInAppLayout>();
    public int LoginBrowserAppOrderVersion { get; set; } = 0;
}

public static class SettingsManager
{
    public static string UserDataFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YoutubeDownloader");
    public static string WebViewDataFolder { get; } = Path.Combine(UserDataFolder, "BrowserData"); // 보안을 위한 브라우저 데이터 격리 폴더
    private static readonly string SettingsFile = Path.Combine(UserDataFolder, "settings.json");
    private static readonly object SaveLock = new object();

    public static AppSettings Settings { get; private set; } = new AppSettings();
    public static bool IsNewInstall { get; private set; } = false;

    public static void Load()
    {
        if (!Directory.Exists(UserDataFolder)) 
        {
            Directory.CreateDirectory(UserDataFolder);
            IsNewInstall = true;
        }
        
        if (File.Exists(SettingsFile))
        {
            IsNewInstall = false;
            try
            {
                var json = File.ReadAllText(SettingsFile);
                Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch { }
        }

        if (string.IsNullOrWhiteSpace(Settings.InstallId))
        {
            Settings.InstallId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            Save();
        }
    }

    public static string GetToolPath(string toolName)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string baseToolPath = Path.Combine(baseDir, toolName);
        
        // 1. 실행 폴더에 있는지 먼저 확인 (이미 있거나 번들된 경우)
        if (File.Exists(baseToolPath)) return baseToolPath;
        
        // 2. UserDataFolder(AppData)에 있는지 확인
        string userDataToolPath = Path.Combine(UserDataFolder, toolName);
        if (File.Exists(userDataToolPath)) return userDataToolPath;
        
        // 3. 없으면 UserDataFolder에 생길 것으로 예상하고 반환 (여기가 보통 쓰기 권한이 있음)
        return userDataToolPath;
    }

    public static string GetFFmpegPath() => GetToolPath("ffmpeg.exe");
    public static string GetFFprobePath() => GetToolPath("ffprobe.exe");
    public static string GetYtDlpPath() => GetToolPath("yt-dlp.exe");
    public static string GetDenoPath() => GetToolPath("deno.exe");

    public static void Save()
    {
        lock (SaveLock)
        {
            string tempFile = SettingsFile + ".tmp";
            try
            {
                Directory.CreateDirectory(UserDataFolder);
                var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(tempFile, json);
                File.Move(tempFile, SettingsFile, overwrite: true);
            }
            catch
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            }
        }
    }
}
