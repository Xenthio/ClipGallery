using ClipGallery.Core.Models;
using CliWrap;
using CliWrap.Buffered;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClipGallery.Core.Services;

public interface IMetadataService
{
    Task EnrichClipAsync(Clip clip);
}

public class MetadataService : IMetadataService
{
    private const string FFProbePath = "ffprobe"; // Assume in PATH

    public async Task EnrichClipAsync(Clip clip)
    {
        try
        {
            var result = await Cli.Wrap(FFProbePath)
                .WithArguments(args => args
                    .Add("-v").Add("quiet")
                    .Add("-print_format").Add("json")
                    .Add("-show_format")
                    .Add("-show_streams")
                    .Add(clip.FilePath))
                .ExecuteBufferedAsync();

            var probeData = JsonSerializer.Deserialize<ProbeResult>(result.StandardOutput);

            if (probeData?.Format != null)
            {
                if (double.TryParse(probeData.Format.Duration, out var d))
                    clip.DurationSeconds = d;
            }

            if (probeData?.Streams != null)
            {
                var audioStreams = probeData.Streams.Count(s => s.CodecType == "audio");
                clip.HasMultiTrackAudio = audioStreams > 1;
            }
        }
        catch (Exception ex)
        {
            // Log error
            Console.WriteLine($"Failed to probe {clip.FileName}: {ex.Message}");
        }
    }

    // Inner classes for JSON mapping
    private class ProbeResult
    {
        [JsonPropertyName("streams")]
        public List<ProbeStream>? Streams { get; set; }

        [JsonPropertyName("format")]
        public ProbeFormat? Format { get; set; }
    }

    private class ProbeStream
    {
        [JsonPropertyName("codec_type")]
        public string? CodecType { get; set; }
    }

    private class ProbeFormat
    {
        [JsonPropertyName("duration")]
        public string? Duration { get; set; }
    }
}
