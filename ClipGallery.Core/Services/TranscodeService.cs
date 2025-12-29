using ClipGallery.Core.Models;
using CliWrap;

namespace ClipGallery.Core.Services;

public enum ExportPreset
{
    Lossless,
    Discord,     // CRF 23, Fast
    HighQuality, // CRF 18, Slow
    Compressed   // CRF 28, VeryFast
}

public interface ITranscodeService
{
    Task TrimClipAsync(Clip clip, double start, double end, string outputPath, ExportPreset preset);
}

public class TranscodeService : ITranscodeService
{
    private const string FFmpegPath = "ffmpeg";

    public async Task TrimClipAsync(Clip clip, double start, double end, string outputPath, ExportPreset preset)
    {
        var duration = end - start;
        if (duration <= 0) throw new ArgumentException("End time must be greater than start time");

        var p = Cli.Wrap(FFmpegPath);

        // Common args
        var args = new List<string>
        {
            "-ss", start.ToString("0.00"),
            "-i", clip.FilePath,
            "-t", duration.ToString("0.00"),
            "-map", "0", // Keep all tracks
        };

        if (preset == ExportPreset.Lossless)
        {
            args.Add("-c"); args.Add("copy");
            // Note: -ss before -i usually accurate enough, but -c copy starts at nearest keyframe
        }
        else
        {
            // Re-encode
            args.Add("-c:v"); args.Add("libx264");
            args.Add("-c:a"); args.Add("aac");
            args.Add("-b:a"); args.Add("192k"); // Better audio for all encodes

            switch (preset)
            {
                case ExportPreset.Discord:
                    args.Add("-crf"); args.Add("23");
                    args.Add("-preset"); args.Add("fast");
                    break;
                case ExportPreset.HighQuality:
                    args.Add("-crf"); args.Add("18");
                    args.Add("-preset"); args.Add("slow");
                    break;
                case ExportPreset.Compressed:
                    args.Add("-crf"); args.Add("28");
                    args.Add("-preset"); args.Add("veryfast");
                    break;
            }
        }

        args.Add("-y");
        args.Add(outputPath);

        await p.WithArguments(args).ExecuteAsync();
    }
}
