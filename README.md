# ClipGallery

ClipGallery is a powerful desktop application for organizing, watching, and managing game clips from various capture software including NVIDIA ShadowPlay, SteelSeries GameSense, OBS, and Steam.

## Features

### Video Support
- **Multi-audio track support** - View and play clips with multiple audio tracks
- **Modern codecs** - Full support for AV1, H.265/HEVC, H.264, VP9, and VP8
- **Video processing** - Trim clips and re-export as H.264 for compatibility

### Organization
- **Multiple source directories** - Add clips from different capture software locations
- **Automatic game detection** - Categorizes clips based on folder structure
- **Smart tagging system** - Add custom tags to organize your clips
- **Rating system** - Rate clips from 1-5 stars to quickly find your favorites

### Library Management
- **Search and filter** - Find clips by name, game, rating, or tags
- **Statistics dashboard** - View total clips, games, and tags at a glance
- **Gallery view** - Browse clips with thumbnail previews

## Installation

### Prerequisites
- Node.js (v16 or higher)
- npm

### Setup
1. Clone the repository:
```bash
git clone https://github.com/Xenthio/ClipGallery.git
cd ClipGallery
```

2. Install dependencies:
```bash
npm install
```

3. Run the application:
```bash
npm start
```

## Usage

### First Time Setup
1. Open the application
2. Click **Settings** in the sidebar
3. Click **Add Directory** to add folders containing your game clips
4. Click **Scan Clips** to index your video files

### Managing Clips
- **Browse**: View all clips in the gallery view
- **Search**: Use the search box to find specific clips
- **Filter**: Filter by game or rating using the dropdown menus
- **Open**: Click any clip to open the player

### Video Player
- **Play**: Watch clips with full multi-audio track support
- **Rate**: Click stars to rate clips (1-5 stars)
- **Tag**: Add custom tags to organize clips
- **Trim**: Extract portions of clips with the trim feature
- **Export**: Convert clips to H.264 format for better compatibility

### Organizing with Tags
- Tags can be added to any clip from the player view
- Click a tag in the sidebar to filter clips by that tag
- Remove tags by clicking them in the player view

### Categories
- Clips are automatically categorized by game based on their folder path
- View clips by game using the sidebar
- Common capture software folder names (clips, recordings, etc.) are automatically skipped

## Supported Video Formats

ClipGallery supports video files with the following extensions:
- `.mp4` - MPEG-4 container
- `.mkv` - Matroska container
- `.avi` - Audio Video Interleave
- `.mov` - QuickTime Movie
- `.webm` - WebM container
- `.m4v` - MPEG-4 Video
- `.flv` - Flash Video

## Supported Codecs

### Video Codecs
- AV1 (`av1`)
- H.265/HEVC (`hevc`, `h265`)
- H.264/AVC (`h264`, `avc`)
- VP9 (`vp9`)
- VP8 (`vp8`)

### Audio
- Multiple audio tracks are automatically detected
- Supports all common audio codecs (AAC, MP3, Opus, Vorbis, etc.)

## Technical Details

### Built With
- **Electron** - Cross-platform desktop framework
- **SQLite** - Local database for metadata
- **FFmpeg** - Video processing and metadata extraction
- **electron-store** - Persistent settings storage

### Database Schema
Clip metadata is stored in a local SQLite database including:
- File path and name
- Game category
- Duration, resolution, FPS
- Video codec
- Audio track count
- Rating (0-5)
- Tags (many-to-many relationship)
- Creation and addition timestamps

## Development

### Project Structure
```
ClipGallery/
├── main.js           # Electron main process
├── renderer.js       # UI logic and event handlers
├── index.html        # Application UI
├── package.json      # Dependencies and scripts
├── assets/           # Icons and static resources
└── README.md         # Documentation
```

### Running in Development Mode
```bash
# Enable developer tools
NODE_ENV=development npm start
```

## License

ISC

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

For issues and feature requests, please use the GitHub issue tracker.
