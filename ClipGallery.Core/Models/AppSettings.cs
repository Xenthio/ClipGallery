using System.Collections.Generic;

namespace ClipGallery.Core.Models;

public class AppSettings
{
    public List<string> LibraryPaths { get; set; } = new();
}
