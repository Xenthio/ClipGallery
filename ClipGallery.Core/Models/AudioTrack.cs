namespace ClipGallery.Core.Models;

public class AudioTrack
{
    public int Index { get; set; }
    public required string GlobalIndex { get; set; } // e.g. "0:a:1"
    public required string Language { get; set; }
    public required string Title { get; set; }
    public required string Codec { get; set; }
    public int Channels { get; set; }

    public override string ToString() => $"{Index}: {Title} ({Language}) - {Codec}";
}
