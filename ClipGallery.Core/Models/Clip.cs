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
    
    /// <summary>
    /// Gets the thumbnail path in a .thumbnails folder next to the video files.
    /// </summary>
    public string ThumbnailPath
    {
        get
        {
            var directory = Path.GetDirectoryName(FilePath) ?? "";
            var thumbFolder = Path.Combine(directory, ".thumbnails");
            return Path.Combine(thumbFolder, FileName + ".thumb.jpg");
        }
    }
    
    public string SidecarPath => FilePath + ".json";
}

public class ClipSidecarData
{
    public int? Rating { get; set; }
    public List<string> Tags { get; set; } = new();
    public string Description { get; set; } = "";
}
