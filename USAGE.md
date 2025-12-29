# ClipGallery Usage Guide

## Getting Started

### Initial Setup

1. **Launch ClipGallery**
   ```bash
   npm start
   ```

2. **Configure Source Directories**
   - Click on **Settings** in the left sidebar
   - Click **Add Directory** button
   - Navigate to your clips folder (examples below)
   - Repeat for each capture software location

### Common Capture Software Locations

#### Windows
- **NVIDIA ShadowPlay**: `C:\Users\[YourName]\Videos\[GameName]`
- **OBS Studio**: `C:\Users\[YourName]\Videos` or custom location
- **Steam**: `C:\Program Files (x86)\Steam\userdata\[SteamID]\760\remote\[AppID]\screenshots`
- **SteelSeries**: Custom location set in SteelSeries Engine

#### Linux
- **OBS Studio**: `~/Videos` or custom location
- **Steam**: `~/.local/share/Steam/userdata/[SteamID]/760/remote/[AppID]/screenshots`

#### macOS
- **OBS Studio**: `~/Movies` or custom location
- **Steam**: `~/Library/Application Support/Steam/userdata/[SteamID]/760/remote/[AppID]/screenshots`

### First Scan

1. After adding source directories, click **Scan Clips** in the top toolbar
2. Wait for the scan to complete (time depends on number of clips)
3. You'll see a notification with the number of clips found

## Features Guide

### Browsing Clips

**Main Library View**
- All clips are displayed in a grid with thumbnails
- Each card shows: filename, game, codec, resolution, duration, audio track count, and rating
- Hover over thumbnails to see a preview frame

**Filtering**
- **By Game**: Click a game name in the sidebar or use the dropdown filter
- **By Rating**: Use the rating dropdown to show only highly-rated clips (e.g., 4+ stars)
- **By Tag**: Click a tag in the sidebar to filter by that tag
- **Search**: Type in the search box to find clips by filename or game

**Quick Access**
- **All Clips**: Shows every clip in your library
- **Favorites**: Shows only clips rated 4 stars or higher

### Watching Clips

1. **Open a Clip**: Click on any clip card to open the player
2. **Playback Controls**: Use standard HTML5 video controls
   - Play/Pause
   - Seek through video
   - Volume control
   - Fullscreen mode
3. **Multiple Audio Tracks**: Browser will automatically use available audio tracks

### Rating System

**In Gallery View**
- Stars (★) show current rating below each clip
- ☆ indicates unrated stars

**In Player View**
- Click on stars (1-5) to rate the current clip
- Ratings are saved immediately
- Golden stars (★) indicate selected rating

**Filtering by Rating**
- Use the rating dropdown: "4+ Stars" shows clips rated 4 or 5
- Great for finding your best moments quickly

### Tagging Clips

**Adding Tags**
1. Open a clip in the player
2. Type a tag name in the "Add tag..." input field
3. Click **Add Tag** or press Enter
4. Tag appears below the input

**Using Tags**
- Tags are reusable across all clips
- Click a tag in the sidebar to filter by that tag
- Click a tag on a clip to remove it

**Tag Ideas**
- Action types: `headshot`, `clutch`, `ace`, `multikill`
- Emotions: `funny`, `epic`, `fail`, `lucky`
- Players: Player names or `solo`, `squad`
- Events: `tournament`, `ranked`, `casual`

### Game Categories

**Automatic Detection**
- ClipGallery automatically extracts game names from folder paths
- Works best when clips are organized in game-specific folders

**Example Folder Structures**
```
Videos/
├── Counter-Strike 2/
│   ├── clip1.mp4
│   └── clip2.mp4
├── Valorant/
│   ├── ace_2024.mp4
│   └── clutch.mp4
└── Apex Legends/
    └── win.mp4
```

**Viewing by Game**
- Games appear in the sidebar automatically after scanning
- Click a game name to see only clips from that game
- Unknown folder structures get labeled as "Unknown"

### Trimming Clips

**Basic Trim**
1. Open a clip in the player
2. Click **Trim Clip** button
3. Enter start time in seconds (e.g., `5.5` for 5.5 seconds)
4. Enter end time in seconds (e.g., `25` for 25 seconds)
5. Click OK to start trimming

**Output**
- Trimmed clip is saved next to the original
- Filename format: `original_trimmed_[timestamp].ext`
- Original clip is preserved
- Uses codec copy for fast processing (no re-encoding)

**Use Cases**
- Remove unwanted intro/outro
- Extract highlight moments
- Share shorter clips

### Exporting Clips

**Export as H.264**
1. Open a clip in the player
2. Click **Export as H.264** button
3. Wait for conversion to complete
4. Exported file is saved next to the original

**Why Export?**
- H.264 has better compatibility with editing software
- Smaller file sizes than AV1 or H.265 in some cases
- Works on older devices and players

**Export Details**
- Transcodes video to H.264 (libx264)
- Converts audio to AAC
- Preserves original resolution and framerate
- Filename format: `original_export.mp4`

### Statistics

**View Statistics**
1. Go to Settings
2. Check the Statistics section

**Available Stats**
- Total number of clips
- Number of different games
- Total tags created
- Top 5 games by clip count

## Tips and Best Practices

### Organization Tips

1. **Consistent Folder Structure**: Keep clips organized by game in separate folders
2. **Regular Scanning**: Run scans periodically to pick up new clips
3. **Rate Immediately**: Rate clips right after watching for better organization
4. **Tag Strategically**: Use consistent tag names (e.g., always use "clutch" not "clutch-win")

### Performance Tips

1. **Codec Support**: Modern codecs (AV1, H.265) may require more CPU for playback
2. **Large Libraries**: First scan of many clips may take time
3. **Network Drives**: Local drives perform better than network shares

### Workflow Examples

**Finding Your Best Clips**
1. Filter by rating: "4+ Stars"
2. Sort by game
3. Export favorites for sharing

**Creating a Montage**
1. Tag clips with `montage`
2. Filter by that tag
3. Review and trim each clip
4. Export all as H.264 for editing

**Post-Session Review**
1. Scan for new clips
2. Watch recent clips
3. Rate and tag notable moments
4. Delete fails or uninteresting clips manually

## Troubleshooting

### Clips Not Appearing
- Verify directory paths in Settings
- Check file extensions are supported (.mp4, .mkv, .avi, .mov, .webm, .m4v, .flv)
- Run another scan

### Playback Issues
- Check if codec is supported by your browser
- Try exporting to H.264 for compatibility
- Update Electron/Chromium for latest codec support

### Performance Issues
- Close other applications
- Check available disk space
- Consider trimming very long clips

### Database Issues
- Database is stored in Electron's userData folder
- Deleting the database will reset all ratings and tags
- Re-scan to rebuild the library

## Keyboard Shortcuts

While in video player:
- **Space**: Play/Pause
- **F**: Fullscreen
- **M**: Mute/Unmute
- **← →**: Seek backward/forward
- **↑ ↓**: Volume up/down

## Advanced Usage

### Manual Database Location
- **Windows**: `%APPDATA%\clipgallery\clipgallery.db`
- **Linux**: `~/.config/clipgallery/clipgallery.db`
- **macOS**: `~/Library/Application Support/clipgallery/clipgallery.db`

### FFmpeg Options
- Trim uses codec copy by default for speed
- Export uses libx264 with AAC audio
- Quality preset: medium (4000k bitrate)

### Multiple Libraries
- Add multiple source directories for different games or capture software
- All clips are indexed in a single database
- Game categorization helps separate them visually
