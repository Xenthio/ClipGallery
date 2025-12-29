using System.Text.Json.Serialization;

namespace ClipGallery.Core.Models;

public class Clip
{
    public required string FilePath { get; set; }
    public required string FileName { get; set; }
    public required string GameName { get; set; } // Derived from parent folder
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
    public double DurationSeconds { get; set; }
    public bool HasMultiTrackAudio { get; set; }

    // Metadata (loaded from sidecar)
    public int? Rating { get; set; } // 1-5
    public List<string> Tags { get; set; } = new();
    public string Description { get; set; } = "";
    
    // Cached thumbnail path (computed once)
    private string? _thumbnailPath;
    
    /// <summary>
    /// Gets the thumbnail path in the central cache folder.
    /// Uses a hash of the file path to create a unique filename.
    /// </summary>
    public string ThumbnailPath
    {
        get
        {
            if (_thumbnailPath == null)
            {
                var cacheFolder = GetThumbnailCacheFolder();
                var hash = GetFilePathHash(FilePath);
                _thumbnailPath = Path.Combine(cacheFolder, $"{hash}.jpg");
            }
            return _thumbnailPath;
        }
    }
    
    public string SidecarPath => FilePath + ".json";
    
    private static string? _thumbnailCacheFolder;
    
    private static string GetThumbnailCacheFolder()
    {
        if (_thumbnailCacheFolder == null)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _thumbnailCacheFolder = Path.Combine(appData, "ClipGallery", "thumbnails");
            Directory.CreateDirectory(_thumbnailCacheFolder);
        }
        return _thumbnailCacheFolder;
    }
    
    private static string GetFilePathHash(string filePath)
    {
        // Use simple hash for filename generation (not cryptographic)
        // Combine string hashcode with length for reasonable uniqueness
        var normalized = filePath.ToLowerInvariant();
        var hash1 = normalized.GetHashCode();
        var hash2 = normalized.Length;
        // Create a longer hash by combining multiple hash computations
        var combined = $"{hash1:x8}{hash2:x4}{normalized.Substring(0, Math.Min(8, normalized.Length)).GetHashCode():x8}";
        return combined;
    }
}

public class ClipSidecarData
{
    public int? Rating { get; set; }
    public List<string> Tags { get; set; } = new();
    public string Description { get; set; } = "";
}
