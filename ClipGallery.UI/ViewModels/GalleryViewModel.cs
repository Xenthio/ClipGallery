using CommunityToolkit.Mvvm.ComponentModel;
using ClipGallery.Core.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace ClipGallery.UI.ViewModels;

public partial class GalleryViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ClipViewModel> _clips = new();

    // Cache of all clips for filtering
    private List<ClipViewModel> _allClips = new();

    [ObservableProperty]
    private ObservableCollection<string> _games = new();

    [ObservableProperty]
    private ObservableCollection<string> _tags = new();

    private string? _selectedGame;
    private string? _selectedTag;

    [ObservableProperty]
    private ClipViewModel? _selectedClip;

    public void LoadClips(IEnumerable<ClipViewModel> clips)
    {
        _allClips = clips.ToList();
        RefreshGames();
        RefreshTags();
        ApplyFilter();
    }

    public void RefreshGames()
    {
        // Use DisplayGameName for sidebar to support merged games
        var games = _allClips.Select(c => c.DisplayGameName).Distinct().OrderBy(g => g);
        Games = new ObservableCollection<string>(games);
    }

    public void RefreshTags()
    {
        var tags = _allClips.SelectMany(c => c.Model.Tags).Distinct().OrderBy(t => t);
        Tags = new ObservableCollection<string>(tags);
    }

    public void FilterByGame(string? gameName)
    {
        _selectedGame = gameName;
        ApplyFilter();
    }

    public void FilterByTag(string? tagName)
    {
        _selectedTag = tagName;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = _allClips.AsEnumerable();

        if (!string.IsNullOrEmpty(_selectedGame))
        {
            // Filter by DisplayGameName to support merged games
            filtered = filtered.Where(c => c.DisplayGameName == _selectedGame);
        }

        if (!string.IsNullOrEmpty(_selectedTag))
        {
            filtered = filtered.Where(c => c.Model.Tags.Contains(_selectedTag));
        }

        Clips = new ObservableCollection<ClipViewModel>(filtered);
    }
}
