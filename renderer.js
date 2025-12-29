const { ipcRenderer } = require('electron');

// State
let currentClips = [];
let currentFilters = {};
let currentClipId = null;
let games = [];
let tags = [];

// Initialize app
document.addEventListener('DOMContentLoaded', async () => {
  await loadGames();
  await loadTags();
  await loadClips();
  setupEventListeners();
  updateStats();
});

// Event Listeners
function setupEventListeners() {
  // Sidebar navigation
  document.querySelectorAll('.sidebar-item[data-view]').forEach(item => {
    item.addEventListener('click', (e) => {
      document.querySelectorAll('.sidebar-item').forEach(i => i.classList.remove('active'));
      e.currentTarget.classList.add('active');
      
      const view = e.currentTarget.dataset.view;
      handleViewChange(view);
    });
  });

  // Search
  const searchInput = document.getElementById('search-input');
  let searchTimeout;
  searchInput.addEventListener('input', (e) => {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => {
      currentFilters.search = e.target.value;
      loadClips();
    }, 300);
  });

  // Filters
  document.getElementById('game-filter').addEventListener('change', (e) => {
    currentFilters.game = e.target.value || null;
    loadClips();
  });

  document.getElementById('rating-filter').addEventListener('change', (e) => {
    currentFilters.rating = e.target.value ? parseInt(e.target.value) : null;
    loadClips();
  });

  // Scan button
  document.getElementById('scan-btn').addEventListener('click', async () => {
    const btn = document.getElementById('scan-btn');
    btn.disabled = true;
    btn.textContent = 'Scanning...';
    
    try {
      const count = await ipcRenderer.invoke('scan-clips');
      alert(`Scan complete! Found ${count} clips.`);
      await loadGames();
      await loadClips();
      await updateStats();
    } catch (error) {
      console.error('Error scanning clips:', error);
      alert('Error scanning clips: ' + error.message);
    } finally {
      btn.disabled = false;
      btn.textContent = 'Scan Clips';
    }
  });

  // Modal controls
  document.getElementById('close-modal').addEventListener('click', () => {
    closeModal();
  });

  document.getElementById('player-modal').addEventListener('click', (e) => {
    if (e.target.id === 'player-modal') {
      closeModal();
    }
  });

  // Tag management
  document.getElementById('add-tag-btn').addEventListener('click', async () => {
    const input = document.getElementById('tag-input');
    const tagName = input.value.trim();
    
    if (tagName && currentClipId) {
      const tag = await ipcRenderer.invoke('create-tag', tagName);
      await ipcRenderer.invoke('add-clip-tag', currentClipId, tag.id);
      input.value = '';
      await loadTags();
      await loadClipTags(currentClipId);
    }
  });

  // Trim button
  document.getElementById('trim-btn').addEventListener('click', async () => {
    const player = document.getElementById('video-player');
    const startTime = prompt('Enter start time (seconds):', '0');
    const endTime = prompt('Enter end time (seconds):', player.duration.toString());
    
    if (startTime !== null && endTime !== null) {
      try {
        const outputPath = await ipcRenderer.invoke('trim-clip', currentClipId, 
          parseFloat(startTime), parseFloat(endTime), 'copy');
        alert(`Clip trimmed successfully!\nSaved to: ${outputPath}`);
      } catch (error) {
        alert('Error trimming clip: ' + error.message);
      }
    }
  });

  // Export button
  document.getElementById('export-btn').addEventListener('click', async () => {
    if (!currentClipId) return;
    
    const player = document.getElementById('video-player');
    const clipDuration = player.duration || Number.MAX_SAFE_INTEGER;
    
    try {
      const outputPath = await ipcRenderer.invoke('trim-clip', currentClipId, 0, clipDuration, 'h264');
      alert(`Clip exported as H.264!\nSaved to: ${outputPath}`);
    } catch (error) {
      alert('Error exporting clip: ' + error.message);
    }
  });
}

// View handlers
async function handleViewChange(view) {
  const contentArea = document.getElementById('content-area');
  
  switch (view) {
    case 'all-clips':
      currentFilters = {};
      await loadClips();
      break;
      
    case 'favorites':
      currentFilters = { rating: 4 };
      await loadClips();
      break;
      
    case 'settings':
      await showSettings();
      break;
  }
}

// Load data
async function loadClips() {
  const contentArea = document.getElementById('content-area');
  contentArea.innerHTML = '<div class="loading">Loading clips...</div>';
  
  try {
    const clips = await ipcRenderer.invoke('get-clips', currentFilters);
    currentClips = clips;
    
    if (clips.length === 0) {
      contentArea.innerHTML = `
        <div class="empty-state">
          <h2>No clips found</h2>
          <p>Add source directories in Settings and click "Scan Clips" to get started.</p>
        </div>
      `;
      return;
    }
    
    contentArea.innerHTML = '<div class="video-grid" id="video-grid"></div>';
    const grid = document.getElementById('video-grid');
    
    for (const clip of clips) {
      const card = createVideoCard(clip);
      grid.appendChild(card);
    }
  } catch (error) {
    console.error('Error loading clips:', error);
    contentArea.innerHTML = `<div class="empty-state"><h2>Error loading clips</h2></div>`;
  }
}

function createVideoCard(clip) {
  const card = document.createElement('div');
  card.className = 'video-card';
  card.dataset.clipId = clip.id;
  
  const stars = '★'.repeat(clip.rating) + '☆'.repeat(5 - clip.rating);
  
  card.innerHTML = `
    <div class="video-thumbnail">
      <video src="file://${clip.filepath}" preload="metadata"></video>
    </div>
    <div class="video-info">
      <div class="video-title">${clip.filename}</div>
      <div class="video-meta">
        ${clip.game} • ${clip.codec.toUpperCase()} • ${clip.resolution} • ${formatDuration(clip.duration)}
        ${clip.audio_tracks > 1 ? `• ${clip.audio_tracks} audio tracks` : ''}
      </div>
      <div class="rating">
        ${stars}
      </div>
    </div>
  `;
  
  card.addEventListener('click', () => openClip(clip));
  
  return card;
}

async function openClip(clip) {
  currentClipId = clip.id;
  
  const modal = document.getElementById('player-modal');
  const player = document.getElementById('video-player');
  const title = document.getElementById('modal-title');
  
  title.textContent = clip.filename;
  player.src = `file://${clip.filepath}`;
  
  // Set up rating stars
  const ratingContainer = document.getElementById('modal-rating');
  ratingContainer.innerHTML = '';
  for (let i = 1; i <= 5; i++) {
    const star = document.createElement('span');
    star.className = 'star' + (i <= clip.rating ? ' filled' : '');
    star.textContent = '★';
    star.addEventListener('click', async () => {
      await ipcRenderer.invoke('set-rating', clip.id, i);
      clip.rating = i;
      updateModalRating(i);
      await loadClips(); // Reload to update grid
    });
    ratingContainer.appendChild(star);
  }
  
  // Load tags
  await loadClipTags(clip.id);
  
  // Show details
  const detailsContainer = document.getElementById('clip-details');
  detailsContainer.innerHTML = `
    <div class="detail-row">
      <span class="detail-label">File:</span>
      <span class="detail-value">${clip.filepath}</span>
    </div>
    <div class="detail-row">
      <span class="detail-label">Game:</span>
      <span class="detail-value">${clip.game}</span>
    </div>
    <div class="detail-row">
      <span class="detail-label">Codec:</span>
      <span class="detail-value">${clip.codec.toUpperCase()}</span>
    </div>
    <div class="detail-row">
      <span class="detail-label">Resolution:</span>
      <span class="detail-value">${clip.resolution}</span>
    </div>
    <div class="detail-row">
      <span class="detail-label">FPS:</span>
      <span class="detail-value">${clip.fps ? clip.fps.toFixed(2) : 'N/A'}</span>
    </div>
    <div class="detail-row">
      <span class="detail-label">Duration:</span>
      <span class="detail-value">${formatDuration(clip.duration)}</span>
    </div>
    <div class="detail-row">
      <span class="detail-label">Audio Tracks:</span>
      <span class="detail-value">${clip.audio_tracks}</span>
    </div>
  `;
  
  modal.classList.add('active');
}

function updateModalRating(rating) {
  const stars = document.querySelectorAll('#modal-rating .star');
  stars.forEach((star, index) => {
    if (index < rating) {
      star.classList.add('filled');
    } else {
      star.classList.remove('filled');
    }
  });
}

async function loadClipTags(clipId) {
  const clipTags = await ipcRenderer.invoke('get-clip-tags', clipId);
  const tagsContainer = document.getElementById('modal-tags');
  tagsContainer.innerHTML = '';
  
  for (const tag of clipTags) {
    const tagEl = document.createElement('span');
    tagEl.className = 'tag';
    tagEl.textContent = tag.name;
    tagEl.style.cursor = 'pointer';
    tagEl.title = 'Click to remove';
    tagEl.addEventListener('click', async () => {
      await ipcRenderer.invoke('remove-clip-tag', clipId, tag.id);
      await loadClipTags(clipId);
    });
    tagsContainer.appendChild(tagEl);
  }
}

function closeModal() {
  const modal = document.getElementById('player-modal');
  const player = document.getElementById('video-player');
  
  player.pause();
  player.src = '';
  modal.classList.remove('active');
  currentClipId = null;
}

async function loadGames() {
  games = await ipcRenderer.invoke('get-games');
  
  // Update games list in sidebar
  const gamesList = document.getElementById('games-list');
  gamesList.innerHTML = '';
  
  games.forEach(game => {
    const item = document.createElement('div');
    item.className = 'sidebar-item';
    item.textContent = game;
    item.addEventListener('click', () => {
      currentFilters = { game };
      loadClips();
    });
    gamesList.appendChild(item);
  });
  
  // Update filter dropdown
  const gameFilter = document.getElementById('game-filter');
  gameFilter.innerHTML = '<option value="">All Games</option>';
  games.forEach(game => {
    const option = document.createElement('option');
    option.value = game;
    option.textContent = game;
    gameFilter.appendChild(option);
  });
}

async function loadTags() {
  tags = await ipcRenderer.invoke('get-tags');
  
  // Update tags list in sidebar
  const tagsList = document.getElementById('tags-list');
  tagsList.innerHTML = '';
  
  tags.forEach(tag => {
    const item = document.createElement('div');
    item.className = 'sidebar-item';
    item.textContent = tag.name;
    item.addEventListener('click', () => {
      currentFilters = { tag: tag.name };
      loadClips();
    });
    tagsList.appendChild(item);
  });
}

async function updateStats() {
  const stats = await ipcRenderer.invoke('get-stats');
  document.getElementById('total-clips-badge').textContent = stats.totalClips;
}

// Settings view
async function showSettings() {
  const contentArea = document.getElementById('content-area');
  const directories = await ipcRenderer.invoke('get-source-directories');
  const stats = await ipcRenderer.invoke('get-stats');
  
  contentArea.innerHTML = `
    <div class="settings-container">
      <div class="settings-section">
        <h2>Statistics</h2>
        <div class="stats-grid">
          <div class="stat-card">
            <div class="stat-value">${stats.totalClips}</div>
            <div class="stat-label">Total Clips</div>
          </div>
          <div class="stat-card">
            <div class="stat-value">${stats.totalGames}</div>
            <div class="stat-label">Games</div>
          </div>
          <div class="stat-card">
            <div class="stat-value">${stats.totalTags}</div>
            <div class="stat-label">Tags</div>
          </div>
        </div>
      </div>

      <div class="settings-section">
        <h2>Source Directories</h2>
        <p style="color: #888; margin-bottom: 15px;">
          Add directories where your game clips are stored. ClipGallery will scan these 
          folders recursively for video files.
        </p>
        <ul class="directory-list" id="directory-list">
          ${directories.map(dir => `
            <li class="directory-item">
              <span>${dir}</span>
              <button class="btn btn-secondary btn-small remove-dir-btn" data-dir="${dir}">Remove</button>
            </li>
          `).join('')}
        </ul>
        <button class="btn btn-primary" id="add-directory-btn">Add Directory</button>
      </div>

      <div class="settings-section">
        <h2>Supported Formats</h2>
        <p style="color: #888; margin-bottom: 10px;">
          ClipGallery supports the following video codecs:
        </p>
        <ul style="list-style: none; padding-left: 20px; color: #aaa;">
          <li>✓ AV1 (av1)</li>
          <li>✓ H.265/HEVC (hevc, h265)</li>
          <li>✓ H.264/AVC (h264, avc)</li>
          <li>✓ VP9 (vp9)</li>
          <li>✓ VP8 (vp8)</li>
        </ul>
        <p style="color: #888; margin-top: 15px;">
          Multiple audio tracks are automatically detected and preserved.
        </p>
      </div>

      <div class="settings-section">
        <h2>Top Games</h2>
        <div style="margin-top: 10px;">
          ${stats.topGames.map(game => `
            <div class="detail-row">
              <span class="detail-label">${game.game}</span>
              <span class="detail-value">${game.count} clips</span>
            </div>
          `).join('')}
        </div>
      </div>

      <div class="settings-section">
        <h2>About</h2>
        <p style="color: #888;">
          ClipGallery v1.0.0<br>
          A game clip manager for organizing, tagging, and watching your gaming moments.
        </p>
      </div>
    </div>
  `;
  
  // Add directory button
  document.getElementById('add-directory-btn').addEventListener('click', async () => {
    const newDir = await ipcRenderer.invoke('add-source-directory');
    if (newDir) {
      await showSettings(); // Refresh view
    }
  });
  
  // Remove directory buttons
  document.querySelectorAll('.remove-dir-btn').forEach(btn => {
    btn.addEventListener('click', async (e) => {
      const dir = e.target.dataset.dir;
      await ipcRenderer.invoke('remove-source-directory', dir);
      await showSettings(); // Refresh view
    });
  });
}

// Utility functions
function formatDuration(seconds) {
  if (!seconds) return '0:00';
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}
