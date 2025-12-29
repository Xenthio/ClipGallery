using ClipGallery.Core.Models;
using System.Text.Json;

namespace ClipGallery.Core.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }
    Task LoadSettingsAsync();
    Task SaveSettingsAsync();
    event EventHandler? SettingsUpdated;
}

public class SettingsService : ISettingsService
{
    public AppSettings Settings { get; private set; } = new();
    public event EventHandler? SettingsUpdated;

    private readonly string _settingsPath;

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "ClipGallery");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");
    }

    public async Task LoadSettingsAsync()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    Settings = loaded;
                    return;
                }
            }
            catch
            {
                // corrupted settings, stick to defaults
            }
        }

        // Default: Add MyVideos if empty
        if (Settings.LibraryPaths.Count == 0)
        {
            Settings.LibraryPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
        }
    }

    public async Task SaveSettingsAsync()
    {
        var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_settingsPath, json);
        SettingsUpdated?.Invoke(this, EventArgs.Empty);
    }
}
