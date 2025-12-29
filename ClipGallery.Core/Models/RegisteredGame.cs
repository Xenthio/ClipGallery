using System.Collections.Generic;

namespace ClipGallery.Core.Models;

/// <summary>
/// Represents a registered game with display information and folder mappings.
/// Multiple folder names can be associated with a single game to merge clips from different sources.
/// </summary>
public class RegisteredGame
{
    /// <summary>
    /// Unique identifier for this game entry
    /// </summary>
    public string Id { get; set; } = "";
    
    /// <summary>
    /// Display name shown in the UI
    /// </summary>
    public string DisplayName { get; set; } = "";
    
    /// <summary>
    /// Path to small icon image (shown in sidebar)
    /// </summary>
    public string? IconPath { get; set; }
    
    /// <summary>
    /// Path to box art image (shown in game grid view)
    /// </summary>
    public string? BoxArtPath { get; set; }
    
    /// <summary>
    /// List of folder names that map to this game.
    /// These are the folder names detected in the library paths.
    /// </summary>
    public List<string> FolderNames { get; set; } = new();
    
    public RegisteredGame()
    {
        Id = Guid.NewGuid().ToString();
    }
}
