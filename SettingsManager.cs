using System;
using System.IO;
using System.Text.Json;

namespace YoutubeDownloader;

public class AppSettings
{
    public string DefaultDownloadFolder { get; set; } = string.Empty;
    public bool ShowNotifications { get; set; } = true;
    public bool AutoOpenFolder { get; set; } = false;
    public bool AutoUpdateCheck { get; set; } = true;
    public string LastHeartbeatDate { get; set; } = string.Empty;
    public System.Collections.Generic.Dictionary<string, int> UsageStats { get; set; } = new System.Collections.Generic.Dictionary<string, int>();
}

public static class SettingsManager
{
    public static string UserDataFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YoutubeDownloader");
    private static readonly string SettingsFile = Path.Combine(UserDataFolder, "settings.json");

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
    }

    public static void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch { }
    }
}
