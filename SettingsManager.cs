using System;
using System.IO;
using System.Text.Json;

namespace YoutubeDownloader;

public class AppSettings
{
    public string DefaultDownloadFolder { get; set; } = string.Empty;
    public bool ShowNotifications { get; set; } = true;
}

public static class SettingsManager
{
    public static string UserDataFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YoutubeDownloader");
    private static readonly string SettingsFile = Path.Combine(UserDataFolder, "settings.json");

    public static AppSettings Settings { get; private set; } = new AppSettings();

    public static void Load()
    {
        if (!Directory.Exists(UserDataFolder)) Directory.CreateDirectory(UserDataFolder);
        
        if (File.Exists(SettingsFile))
        {
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
