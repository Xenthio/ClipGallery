using ClipGallery.Core.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace ClipGallery.Core.Services;

public class ClipScannerService : IClipScannerService
{
    private static readonly HashSet<string> ValidExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".mov", ".avi", ".webm"
    };

    public event EventHandler<Clip>? ClipAdded;
    public event EventHandler<string>? ClipRemoved;

    private readonly IMetadataService _metadataService;
    private readonly IThumbnailService _thumbnailService;

    public ClipScannerService(IMetadataService metadataService, IThumbnailService thumbnailService)
    {
        _metadataService = metadataService;
        _thumbnailService = thumbnailService;
    }

    public async Task<List<Clip>> BuildLibraryAsync(IEnumerable<string> sourcePaths)
    {
        var allClips = new ConcurrentBag<Clip>();

        await Parallel.ForEachAsync(sourcePaths, async (path, ct) =>
        {
            if (!Directory.Exists(path)) return;

            var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(f => ValidExtensions.Contains(Path.GetExtension(f)));

            foreach (var file in files)
            {
                var clip = await ProcessFileAsync(file);
                allClips.Add(clip);
            }
        });

        return allClips.OrderByDescending(c => c.CreatedAt).ToList();
    }

    private async Task<Clip> ProcessFileAsync(string filePath)
    {
        var info = new FileInfo(filePath);
        var dirName = info.Directory?.Name ?? "Unknown";

        var clip = new Clip
        {
            FilePath = filePath,
            FileName = info.Name,
            GameName = dirName,
            CreatedAt = info.CreationTime,
            SizeBytes = info.Length,
            DurationSeconds = 0, // Need MetadataService for this
            HasMultiTrackAudio = false // Need MetadataService for this
        };

        // Try load sidecar
        var sidecarPath = clip.SidecarPath;
        if (File.Exists(sidecarPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(sidecarPath);
                var data = JsonSerializer.Deserialize<ClipSidecarData>(json);
                if (data != null)
                {
                    clip.Rating = data.Rating;
                    clip.Tags = data.Tags;
                    clip.Description = data.Description ?? "";
                }
            }
            catch
            {
                // Ignore corrupt sidecar
            }
        }

        return clip;
    }

    private readonly List<FileSystemWatcher> _watchers = new();

    public void StartWatching(string path)
    {
        if (!Directory.Exists(path)) return;

        var watcher = new FileSystemWatcher(path);
        watcher.IncludeSubdirectories = true;
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Attributes;

        watcher.Created += async (s, e) =>
        {
            if (Path.GetExtension(e.Name) is string ext && ValidExtensions.Contains(ext))
            {
                // Small delay to ensure lock is released (e.g. OBS finishing write)
                await Task.Delay(1000);
                var clip = await ProcessFileAsync(e.FullPath);
                ClipAdded?.Invoke(this, clip);
            }
        };

        watcher.Deleted += (s, e) =>
        {
            if (Path.GetExtension(e.Name) is string ext && ValidExtensions.Contains(ext))
            {
                ClipRemoved?.Invoke(this, e.FullPath);
            }
        };

        watcher.EnableRaisingEvents = true;
        _watchers.Add(watcher);
    }

    public async Task EnrichClipAsync(Clip clip)
    {
        // Enrich (Duration, Streams)
        await _metadataService.EnrichClipAsync(clip);

        // Generate Thumbnail
        await _thumbnailService.GenerateThumbnailAsync(clip);
    }

    public async Task SaveClipAsync(Clip clip)
    {
        var data = new ClipSidecarData
        {
            Rating = clip.Rating,
            Tags = clip.Tags,
            Description = clip.Description
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(clip.SidecarPath, json);
    }
}
