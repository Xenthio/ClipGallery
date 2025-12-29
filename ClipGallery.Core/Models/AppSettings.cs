using System.Collections.Generic;

namespace ClipGallery.Core.Models;

public class AppSettings
{
    public List<string> LibraryPaths { get; set; } = new();
    
    /// <summary>
    /// Legacy game aliases (kept for backward compatibility).
    /// Key = folder name as detected, Value = display name to show in UI.
    /// </summary>
    public Dictionary<string, string> GameAliases { get; set; } = new();
    
    /// <summary>
    /// Registered games with full metadata including icons, box art, and folder mappings.
    /// </summary>
    public List<RegisteredGame> RegisteredGames { get; set; } = new();
}
