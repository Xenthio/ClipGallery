using ClipGallery.Core.Models;

namespace ClipGallery.Core.Services;

public interface IClipScannerService
{
    // Scans directories and returns all found clips
    Task<List<Clip>> BuildLibraryAsync(IEnumerable<string> sourcePaths);

    // Event when filesystem changes detected (optional, for later)
    event EventHandler<Clip> ClipAdded;
    event EventHandler<string> ClipRemoved;

    Task SaveClipAsync(Clip clip); // Persist metadata

    // Enrich with metadata and ensure thumbnail exists
    Task EnrichClipAsync(Clip clip);

    // Start watching a directory for changes
    void StartWatching(string path);
}
