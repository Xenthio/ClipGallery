using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClipGallery.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace ClipGallery.UI.ViewModels;

public partial class GalleryViewModel : ObservableObject
{
    private const int PageSize = 100; // Number of clips to load at a time
    
    /// <summary>
    /// Event fired when displayed clips change (for thumbnail prioritization)
    /// </summary>
    public event EventHandler<IReadOnlyList<ClipViewModel>>? DisplayedClipsChanged;
    
    /// <summary>
    /// Total count of clips matching current filter (for display purposes)
    /// </summary>
    [ObservableProperty]
    private int _totalClipsCount;
    
    /// <summary>
    /// The clips currently displayed (paginated)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ClipViewModel> _displayedClips = new();
    
    /// <summary>
    /// Alias for DisplayedClips for backward compatibility
    /// </summary>
    public ObservableCollection<ClipViewModel> Clips => DisplayedClips;

    // Cache of all clips for filtering
    private List<ClipViewModel> _allClips = new();
    
    // Currently filtered clips (before pagination)
    private List<ClipViewModel> _filteredClips = new();
    
    // Current page
    private int _currentDisplayCount = 0;

    [ObservableProperty]
    private ObservableCollection<string> _games = new();

    [ObservableProperty]
    private ObservableCollection<string> _tags = new();

    private string? _selectedGame;
    private string? _selectedTag;
    private string _searchQuery = "";

    [ObservableProperty]
    private ClipViewModel? _selectedClip;
    
    [ObservableProperty]
    private bool _hasMoreClips;
    
    [ObservableProperty]
    private string _loadMoreText = "Load More";
    
    [ObservableProperty]
    private bool _isLoadingMore;

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

    public void SetSearchQuery(string query)
    {
        _searchQuery = query;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        // Unload thumbnails from clips that will no longer be displayed
        foreach (var clip in DisplayedClips)
        {
            clip.UnloadThumbnail();
        }
        
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

        // Apply search query - search in filename, game name, tags, and description
        if (!string.IsNullOrWhiteSpace(_searchQuery))
        {
            var query = _searchQuery.Trim();
            filtered = filtered.Where(c => 
                c.FileName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.DisplayGameName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.Model.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (c.Model.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            );
        }

        _filteredClips = filtered.ToList();
        TotalClipsCount = _filteredClips.Count;
        
        // Reset pagination and load first page
        _currentDisplayCount = 0;
        HasMoreClips = _filteredClips.Count > 0; // Set to true initially so first load works
        DisplayedClips = new ObservableCollection<ClipViewModel>();
        LoadMoreClips();
    }
    
    [RelayCommand]
    public void LoadMore()
    {
        LoadMoreClips();
    }
    
    private void LoadMoreClips()
    {
        // Prevent multiple simultaneous loads or loading when nothing left
        if (IsLoadingMore) return;
        if (_currentDisplayCount >= _filteredClips.Count) 
        {
            HasMoreClips = false;
            return;
        }
        
        IsLoadingMore = true;
        try
        {
            var nextBatch = _filteredClips.Skip(_currentDisplayCount).Take(PageSize).ToList();
            
            // Create new collection with all items for efficient update
            var newCollection = new ObservableCollection<ClipViewModel>(DisplayedClips.Concat(nextBatch));
            DisplayedClips = newCollection;
            
            _currentDisplayCount += nextBatch.Count;
            
            var remaining = _filteredClips.Count - _currentDisplayCount;
            HasMoreClips = remaining > 0;
            
            if (remaining > 0)
            {
                LoadMoreText = $"Scroll for more ({remaining} remaining)";
            }
            
            // Notify that displayed clips changed (for thumbnail prioritization)
            DisplayedClipsChanged?.Invoke(this, nextBatch);
        }
        finally
        {
            IsLoadingMore = false;
        }
    }
}
