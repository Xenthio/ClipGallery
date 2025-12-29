using ClipGallery.Core.Models;
using CliWrap;
using CliWrap.Buffered;

namespace ClipGallery.Core.Services;

public interface IThumbnailService
{
    Task GenerateThumbnailAsync(Clip clip);
}

public class ThumbnailService : IThumbnailService
{
    private const string FFmpegPath = "ffmpeg";
    
    // Thumbnail settings - smaller size and higher compression
    private const int ThumbnailWidth = 320;  // Scale to 320px width (maintains aspect ratio)
    private const int JpegQuality = 8;       // Higher number = more compression (range 2-31, 8-12 is good balance)

    public async Task GenerateThumbnailAsync(Clip clip)
    {
        if (File.Exists(clip.ThumbnailPath)) return;

        try
        {
            // Ensure the .thumbnails folder exists
            var thumbDir = Path.GetDirectoryName(clip.ThumbnailPath);
            if (!string.IsNullOrEmpty(thumbDir) && !Directory.Exists(thumbDir))
            {
                Directory.CreateDirectory(thumbDir);
            }
            
            // Take screenshot at 10% or 5 seconds, whichever is smaller (but at least 1s)
            var time = Math.Max(1, Math.Min(5, clip.DurationSeconds * 0.1));

            await Cli.Wrap(FFmpegPath)
                .WithArguments(args => args
                    .Add("-ss").Add(time.ToString("0.0"))
                    .Add("-i").Add(clip.FilePath)
                    .Add("-vframes").Add("1")
                    .Add("-vf").Add($"scale={ThumbnailWidth}:-1")  // Scale to width, maintain aspect ratio
                    .Add("-q:v").Add(JpegQuality.ToString())       // More compression (was 2, now 8)
                    .Add("-y") // Overwrite
                    .Add(clip.ThumbnailPath))
                .ExecuteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to thumb {clip.FileName}: {ex.Message}");
        }
    }
}
