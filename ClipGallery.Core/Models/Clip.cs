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
    
    public string ThumbnailPath => Path.ChangeExtension(FilePath, ".jpg");
    public string SidecarPath => FilePath + ".json";
}

public class ClipSidecarData
{
    public int? Rating { get; set; }
    public List<string> Tags { get; set; } = new();
    public string Description { get; set; } = "";
}
