# ClipGallery Architecture

## Overview
ClipGallery is built using Electron, which provides a cross-platform desktop framework combining Chromium for UI rendering and Node.js for backend operations.

## Application Structure

```
┌─────────────────────────────────────────────────────┐
│                   Electron App                      │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ┌──────────────────┐      ┌──────────────────┐  │
│  │  Main Process    │◄────►│ Renderer Process │  │
│  │  (Node.js)       │ IPC  │  (Chromium)      │  │
│  │                  │      │                  │  │
│  │  main.js         │      │  renderer.js     │  │
│  │  - Database      │      │  - UI Logic      │  │
│  │  - File System   │      │  - Event         │  │
│  │  - FFmpeg        │      │    Handlers      │  │
│  │  - IPC Handlers  │      │  - Data Display  │  │
│  └────────┬─────────┘      └────────┬─────────┘  │
│           │                         │             │
│           ▼                         ▼             │
│  ┌─────────────────┐      ┌──────────────────┐  │
│  │  SQLite DB      │      │  index.html      │  │
│  │  - clips        │      │  - UI Structure  │  │
│  │  - tags         │      │  - CSS Styles    │  │
│  │  - clip_tags    │      │  - Video Player  │  │
│  └─────────────────┘      └──────────────────┘  │
│           │                                       │
│           ▼                                       │
│  ┌─────────────────┐                             │
│  │  FFmpeg         │                             │
│  │  - Metadata     │                             │
│  │  - Trim/Export  │                             │
│  └─────────────────┘                             │
│                                                   │
└───────────────────────────────────────────────────┘
```

## Component Details

### Main Process (main.js)
**Responsibilities:**
- Initialize and manage application lifecycle
- Create browser windows
- Handle file system operations
- Manage SQLite database
- Process FFmpeg operations
- Respond to IPC requests from renderer

**Key Functions:**
- `initDatabase()` - Create/upgrade database schema
- `scanDirectory()` - Recursively find video files
- `getVideoMetadata()` - Extract video information using FFmpeg
- IPC handlers for all backend operations

### Renderer Process (renderer.js)
**Responsibilities:**
- Handle user interactions
- Update UI based on data
- Make IPC calls to main process
- Manage application state

**Key Functions:**
- `loadClips()` - Fetch and display clips
- `openClip()` - Open video player modal
- `loadGames()` / `loadTags()` - Populate filters
- Event handlers for all UI interactions

### User Interface (index.html)
**Components:**
- Sidebar navigation
- Toolbar with search and filters
- Video grid gallery
- Modal video player
- Settings page

**Styling:**
- Dark theme
- Responsive grid layout
- Modern card-based design

## Data Flow

### Scanning Clips
```
User clicks "Scan Clips"
    ↓
renderer.js sends IPC request
    ↓
main.js receives request
    ↓
Scan directories recursively
    ↓
For each video file:
    - Get FFmpeg metadata
    - Extract game from path
    - Insert into database
    ↓
Return count to renderer
    ↓
Refresh UI
```

### Playing a Clip
```
User clicks video card
    ↓
renderer.js opens modal
    ↓
Set video source to file://path
    ↓
Load clip tags and metadata
    ↓
Display in player
    ↓
User can rate, tag, trim, or export
```

### Rating a Clip
```
User clicks star
    ↓
renderer.js sends IPC: set-rating
    ↓
main.js updates database
    ↓
Returns success
    ↓
UI updates star display
```

## Database Schema

### Tables

**clips**
```sql
CREATE TABLE clips (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  filepath TEXT UNIQUE NOT NULL,
  filename TEXT NOT NULL,
  game TEXT,
  rating INTEGER DEFAULT 0,
  duration REAL,
  codec TEXT,
  resolution TEXT,
  fps REAL,
  audio_tracks INTEGER DEFAULT 1,
  created_at INTEGER NOT NULL,
  added_at INTEGER DEFAULT (strftime('%s', 'now'))
);
```

**tags**
```sql
CREATE TABLE tags (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT UNIQUE NOT NULL
);
```

**clip_tags** (Junction Table)
```sql
CREATE TABLE clip_tags (
  clip_id INTEGER NOT NULL,
  tag_id INTEGER NOT NULL,
  PRIMARY KEY (clip_id, tag_id),
  FOREIGN KEY (clip_id) REFERENCES clips(id) ON DELETE CASCADE,
  FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
);
```

### Indexes
- `idx_clips_game` - Fast game filtering
- `idx_clips_rating` - Fast rating filtering
- `idx_clip_tags_clip` - Fast tag lookups
- `idx_clip_tags_tag` - Fast reverse tag lookups

## IPC Communication

### Main → Renderer
None (Main doesn't initiate communication)

### Renderer → Main (via invoke/handle)

**Settings**
- `get-source-directories` - Get configured directories
- `set-source-directories` - Update directories
- `add-source-directory` - Add new directory with dialog
- `remove-source-directory` - Remove a directory

**Clips**
- `scan-clips` - Scan all directories for videos
- `get-clips` - Query clips with filters
- `get-clip` - Get single clip by ID
- `get-games` - Get list of all games
- `get-stats` - Get library statistics

**Ratings**
- `set-rating` - Update clip rating

**Tags**
- `get-tags` - Get all tags
- `create-tag` - Create new tag
- `get-clip-tags` - Get tags for a clip
- `add-clip-tag` - Add tag to clip
- `remove-clip-tag` - Remove tag from clip

**Video Processing**
- `trim-clip` - Trim clip with FFmpeg
- `export-clip` - Export clip with codec conversion

## Storage Locations

### Database
Stored in Electron's userData directory:
- **Windows**: `%APPDATA%\clipgallery\clipgallery.db`
- **Linux**: `~/.config/clipgallery/clipgallery.db`
- **macOS**: `~/Library/Application Support/clipgallery/clipgallery.db`

### Settings
Managed by electron-store:
- Same directory as database
- JSON format
- Includes: source directories

### Video Files
Remain in original locations:
- Read-only access
- Trimmed/exported clips saved adjacent to originals

## FFmpeg Integration

### Metadata Extraction
```javascript
ffmpeg.ffprobe(filePath, (err, metadata) => {
  // Extract:
  // - Duration
  // - Codec
  // - Resolution
  // - FPS
  // - Audio track count
});
```

### Trimming
```javascript
ffmpeg(inputPath)
  .setStartTime(startTime)
  .setDuration(endTime - startTime)
  .videoCodec('copy')  // Fast
  .audioCodec('copy')
  .save(outputPath);
```

### Exporting
```javascript
ffmpeg(inputPath)
  .videoCodec('libx264')  // Re-encode
  .audioCodec('aac')
  .videoBitrate('4000k')
  .save(outputPath);
```

## Security Model

### Electron Security
- `nodeIntegration: true` - Required for IPC
- `contextIsolation: false` - Simplified architecture
- File system access restricted to source directories

### Input Validation
- SQL: Prepared statements prevent injection
- Frame rate: Safe parsing without eval()
- File paths: Validated before processing

### Dependencies
- Regular security audits
- No known vulnerabilities
- Minimal dependency tree

## Performance Optimizations

### Database
- Indexed columns for fast queries
- Prepared statements (reusable)
- Single transaction for bulk inserts

### UI
- Lazy loading of video thumbnails
- Virtual scrolling (future enhancement)
- Debounced search input

### Video Processing
- Codec copy for fast trimming
- Parallel directory scanning
- FFmpeg process pooling (future enhancement)

## Extension Points

### Adding New Features

**New Video Format:**
```javascript
// main.js
const videoExtensions = [..., '.newformat'];
```

**New Metadata Field:**
```javascript
// main.js - Update schema
ALTER TABLE clips ADD COLUMN new_field TEXT;

// Update getVideoMetadata()
// Update display in renderer.js
```

**New Filter:**
```javascript
// main.js - Add IPC handler
ipcMain.handle('get-clips-by-X', ...);

// renderer.js - Add UI control
// index.html - Add filter element
```

## Future Architecture Considerations

### Scalability
- Pre-generated thumbnails for faster loading
- Background scanning service
- Incremental scanning (only new files)

### Features
- Cloud sync capability
- Multi-user support
- Playlist management
- Keyboard shortcuts

### Performance
- Worker threads for FFmpeg
- WebAssembly for video processing
- IndexedDB for larger datasets
- Virtual scrolling for large libraries

## Development Workflow

1. **Main Process Changes** → Restart application
2. **Renderer Changes** → Reload window (Ctrl+R)
3. **HTML/CSS Changes** → Reload window (Ctrl+R)
4. **Database Schema Changes** → Version migration needed

## Testing Strategy

### Manual Testing
- Start application
- Add directories
- Scan clips
- Test each feature
- Verify database updates

### Future Automated Testing
- Unit tests for business logic
- Integration tests for IPC
- E2E tests for user flows
- Performance benchmarks

## Deployment

### Current
```bash
npm install
npm start
```

### Production (Future)
- electron-builder for packaging
- Platform-specific installers
- Auto-update mechanism
- Signed executables

---

This architecture provides a solid foundation for a clip management application with clear separation of concerns and room for future enhancements.
