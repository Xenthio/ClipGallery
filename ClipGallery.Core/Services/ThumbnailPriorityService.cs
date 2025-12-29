using System.Collections.Concurrent;
using ClipGallery.Core.Models;

namespace ClipGallery.Core.Services;

/// <summary>
/// Manages prioritized thumbnail generation.
/// Clips that are visible on screen get priority over background generation.
/// </summary>
public interface IThumbnailPriorityService
{
    /// <summary>
    /// Request thumbnail generation for a clip.
    /// </summary>
    /// <param name="clip">The clip to generate thumbnail for</param>
    /// <param name="highPriority">True if clip is currently visible on screen</param>
    /// <param name="onComplete">Callback when thumbnail is ready</param>
    void RequestThumbnail(Clip clip, bool highPriority, Action? onComplete = null);
    
    /// <summary>
    /// Bump a clip to high priority (e.g., when it becomes visible)
    /// </summary>
    void PrioritizeClip(Clip clip);
    
    /// <summary>
    /// Start processing the thumbnail queue
    /// </summary>
    void Start();
    
    /// <summary>
    /// Stop processing
    /// </summary>
    void Stop();
}

public class ThumbnailPriorityService : IThumbnailPriorityService
{
    private readonly IThumbnailService _thumbnailService;
    private readonly ConcurrentQueue<ThumbnailRequest> _highPriorityQueue = new();
    private readonly ConcurrentQueue<ThumbnailRequest> _lowPriorityQueue = new();
    private readonly ConcurrentDictionary<string, ThumbnailRequest> _pendingRequests = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _processingTask;
    private bool _isRunning;

    public ThumbnailPriorityService(IThumbnailService thumbnailService)
    {
        _thumbnailService = thumbnailService;
    }

    private class ThumbnailRequest
    {
        public Clip Clip { get; set; } = null!;
        public Action? OnComplete { get; set; }
        public bool IsHighPriority { get; set; }
        public bool IsProcessed { get; set; }
    }

    public void RequestThumbnail(Clip clip, bool highPriority, Action? onComplete = null)
    {
        var request = new ThumbnailRequest
        {
            Clip = clip,
            OnComplete = onComplete,
            IsHighPriority = highPriority,
            IsProcessed = false
        };

        // Check if already pending - if so, potentially bump priority
        if (_pendingRequests.TryGetValue(clip.FilePath, out var existing))
        {
            if (highPriority && !existing.IsHighPriority)
            {
                // Mark as high priority - will be picked up by the processor
                existing.IsHighPriority = true;
            }
            // Add new callback
            if (onComplete != null)
            {
                var oldCallback = existing.OnComplete;
                existing.OnComplete = () =>
                {
                    oldCallback?.Invoke();
                    onComplete();
                };
            }
            return;
        }

        // Add new request
        _pendingRequests[clip.FilePath] = request;
        
        if (highPriority)
        {
            _highPriorityQueue.Enqueue(request);
        }
        else
        {
            _lowPriorityQueue.Enqueue(request);
        }
    }

    public void PrioritizeClip(Clip clip)
    {
        if (_pendingRequests.TryGetValue(clip.FilePath, out var request))
        {
            // Just mark as high priority - queue order doesn't matter much
            // since we check IsHighPriority when processing
            request.IsHighPriority = true;
        }
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _processingTask = ProcessQueueAsync();
    }

    public void Stop()
    {
        _isRunning = false;
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task ProcessQueueAsync()
    {
        while (_isRunning && !_cts.Token.IsCancellationRequested)
        {
            ThumbnailRequest? request = null;

            // First try high priority queue
            while (_highPriorityQueue.TryDequeue(out var highReq))
            {
                // Skip already processed requests
                if (!highReq.IsProcessed && _pendingRequests.ContainsKey(highReq.Clip.FilePath))
                {
                    request = highReq;
                    break;
                }
            }
            
            // If no high priority, try low priority but prefer any marked as high priority
            if (request == null)
            {
                var lowPriorityItems = new List<ThumbnailRequest>();
                while (_lowPriorityQueue.TryDequeue(out var lowReq))
                {
                    if (lowReq.IsProcessed || !_pendingRequests.ContainsKey(lowReq.Clip.FilePath))
                    {
                        continue; // Skip processed
                    }
                    
                    // Check if this was bumped to high priority
                    if (lowReq.IsHighPriority)
                    {
                        request = lowReq;
                        // Put remaining back
                        foreach (var item in lowPriorityItems)
                        {
                            _lowPriorityQueue.Enqueue(item);
                        }
                        break;
                    }
                    lowPriorityItems.Add(lowReq);
                }
                
                // If no high priority found, take first from collected items
                if (request == null && lowPriorityItems.Count > 0)
                {
                    request = lowPriorityItems[0];
                    for (int i = 1; i < lowPriorityItems.Count; i++)
                    {
                        _lowPriorityQueue.Enqueue(lowPriorityItems[i]);
                    }
                }
            }

            if (request != null)
            {
                try
                {
                    request.IsProcessed = true;
                    await _thumbnailService.GenerateThumbnailAsync(request.Clip);
                    request.OnComplete?.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Thumbnail generation failed for {request.Clip.FileName}: {ex.Message}");
                }
                finally
                {
                    _pendingRequests.TryRemove(request.Clip.FilePath, out _);
                }
            }
            else
            {
                // No work, wait a bit
                try
                {
                    await Task.Delay(50, _cts.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
    }
}
