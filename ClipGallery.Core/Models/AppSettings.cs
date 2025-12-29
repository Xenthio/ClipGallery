using System.Collections.Generic;

namespace ClipGallery.Core.Models;

public class AppSettings
{
    public List<string> LibraryPaths { get; set; } = new();
    
    /// <summary>
    /// Game merging: Maps folder names to a canonical display name.
    /// Key = folder name as detected, Value = display name to show in UI.
    /// Multiple folder names can map to the same display name to "merge" games.
    /// </summary>
    public Dictionary<string, string> GameAliases { get; set; } = new();
}
