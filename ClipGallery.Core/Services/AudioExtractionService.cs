using ClipGallery.Core.Models;
using CliWrap;

namespace ClipGallery.Core.Services;

public interface IAudioExtractionService
{
    // Returns path to extracted wav file, or null if failed/no track
    Task<string?> ExtractSecondaryAudioAsync(Clip clip);
    Task<List<AudioTrack>> ExtractAudioTracksAsync(string filePath);
}

public class AudioExtractionService : IAudioExtractionService
{
    private const string FFmpegPath = "ffmpeg";
    private const string FFprobePath = "ffprobe";

    public async Task<string?> ExtractSecondaryAudioAsync(Clip clip)
    {
        if (!clip.HasMultiTrackAudio) return null;

        var tempPath = Path.Combine(Path.GetTempPath(), "ClipGallery", $"{clip.FileName}.track2.wav");
        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

        if (File.Exists(tempPath)) return tempPath;

        try
        {
            // Extract 2nd audio stream (index 1) to wav
            // -map 0:a:1 selects the second audio stream from the first input
            await Cli.Wrap(FFmpegPath)
                .WithArguments(args => args
                    .Add("-i").Add(clip.FilePath)
                    .Add("-map").Add("0:a:1")
                    .Add("-vn") // No video
                    .Add("-acodec").Add("pcm_s16le")
                    .Add("-ar").Add("44100")
                    .Add("-y")
                    .Add(tempPath))
                .ExecuteAsync();

            return tempPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Audio extraction failed: {ex.Message}");
            return null;
        }
    }

    public async Task<List<AudioTrack>> ExtractAudioTracksAsync(string filePath)
    {
        var tracks = new List<AudioTrack>();
        try
        {
            // Use ffprobe to get json
            var stdOut = new System.Text.StringBuilder();
            await Cli.Wrap(FFprobePath)
               .WithArguments(args => args
                   .Add("-v").Add("quiet")
                   .Add("-print_format").Add("json")
                   .Add("-show_streams")
                   .Add("-select_streams").Add("a") // audio only
                   .Add(filePath))
               .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
               .ExecuteAsync();

            var json = stdOut.ToString();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var streams = doc.RootElement.GetProperty("streams");

            int index = 0;
            foreach (var stream in streams.EnumerateArray())
            {
                string lang = "und";
                string title = $"Track {index + 1}";

                if (stream.TryGetProperty("tags", out var tags))
                {
                    lang = tags.TryGetProperty("language", out var l) ? l.GetString() ?? "und" : "und";
                    title = tags.TryGetProperty("title", out var t) ? t.GetString() ?? $"Track {index + 1}" : $"Track {index + 1}";
                }

                var track = new AudioTrack
                {
                    Index = index,
                    GlobalIndex = $"0:a:{index}",
                    Codec = stream.TryGetProperty("codec_name", out var c) ? c.GetString() ?? "?" : "?",
                    Channels = stream.TryGetProperty("channels", out var ch) ? ch.GetInt32() : 0,
                    Language = lang,
                    Title = title
                };

                tracks.Add(track);
                index++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FFprobe failed: {ex.Message}");
        }
        return tracks;
    }
}
