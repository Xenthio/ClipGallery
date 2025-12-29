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

    public async Task GenerateThumbnailAsync(Clip clip)
    {
        if (File.Exists(clip.ThumbnailPath)) return;

        try
        {
            // Take screenshot at 10% or 5 seconds, whichever is smaller (but at least 1s)
            var time = Math.Max(1, Math.Min(5, clip.DurationSeconds * 0.1));

            await Cli.Wrap(FFmpegPath)
                .WithArguments(args => args
                    .Add("-ss").Add(time.ToString("0.0"))
                    .Add("-i").Add(clip.FilePath)
                    .Add("-vframes").Add("1")
                    .Add("-q:v").Add("2") // High quality jpg
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
