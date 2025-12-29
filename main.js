const { app, BrowserWindow, ipcMain, dialog } = require('electron');
const path = require('path');
const Database = require('better-sqlite3');
const Store = require('electron-store');
const ffmpeg = require('fluent-ffmpeg');
const ffmpegInstaller = require('@ffmpeg-installer/ffmpeg');
const fs = require('fs').promises;

// Set ffmpeg path
ffmpeg.setFfmpegPath(ffmpegInstaller.path);

// Initialize electron-store for settings
const store = new Store();

// Initialize database
let db;

function initDatabase() {
  const dbPath = path.join(app.getPath('userData'), 'clipgallery.db');
  db = new Database(dbPath);
  
  // Create tables
  db.exec(`
    CREATE TABLE IF NOT EXISTS clips (
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

    CREATE TABLE IF NOT EXISTS tags (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      name TEXT UNIQUE NOT NULL
    );

    CREATE TABLE IF NOT EXISTS clip_tags (
      clip_id INTEGER NOT NULL,
      tag_id INTEGER NOT NULL,
      PRIMARY KEY (clip_id, tag_id),
      FOREIGN KEY (clip_id) REFERENCES clips(id) ON DELETE CASCADE,
      FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
    );

    CREATE INDEX IF NOT EXISTS idx_clips_game ON clips(game);
    CREATE INDEX IF NOT EXISTS idx_clips_rating ON clips(rating);
    CREATE INDEX IF NOT EXISTS idx_clip_tags_clip ON clip_tags(clip_id);
    CREATE INDEX IF NOT EXISTS idx_clip_tags_tag ON clip_tags(tag_id);
  `);
}

let mainWindow;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false,
      enableRemoteModule: false
    },
    icon: path.join(__dirname, 'assets/icon.png')
  });

  mainWindow.loadFile('index.html');
  
  // Open DevTools in development
  if (process.env.NODE_ENV === 'development') {
    mainWindow.webContents.openDevTools();
  }
}

app.whenReady().then(() => {
  initDatabase();
  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    if (db) db.close();
    app.quit();
  }
});

// IPC Handlers

// Settings
ipcMain.handle('get-source-directories', () => {
  return store.get('sourceDirectories', []);
});

ipcMain.handle('set-source-directories', (event, directories) => {
  store.set('sourceDirectories', directories);
  return true;
});

ipcMain.handle('add-source-directory', async () => {
  const result = await dialog.showOpenDialog(mainWindow, {
    properties: ['openDirectory']
  });
  
  if (!result.canceled && result.filePaths.length > 0) {
    const directories = store.get('sourceDirectories', []);
    const newDir = result.filePaths[0];
    if (!directories.includes(newDir)) {
      directories.push(newDir);
      store.set('sourceDirectories', directories);
    }
    return newDir;
  }
  return null;
});

ipcMain.handle('remove-source-directory', (event, directory) => {
  const directories = store.get('sourceDirectories', []);
  const filtered = directories.filter(d => d !== directory);
  store.set('sourceDirectories', filtered);
  return true;
});

// Clip scanning
ipcMain.handle('scan-clips', async () => {
  const directories = store.get('sourceDirectories', []);
  const videoExtensions = ['.mp4', '.mkv', '.avi', '.mov', '.webm', '.m4v', '.flv'];
  const clips = [];

  for (const dir of directories) {
    try {
      await scanDirectory(dir, clips, videoExtensions);
    } catch (error) {
      console.error(`Error scanning directory ${dir}:`, error);
    }
  }

  // Insert or update clips in database
  for (const clip of clips) {
    try {
      const existing = db.prepare('SELECT id FROM clips WHERE filepath = ?').get(clip.filepath);
      if (!existing) {
        db.prepare(`
          INSERT INTO clips (filepath, filename, game, duration, codec, resolution, fps, audio_tracks, created_at)
          VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
        `).run(
          clip.filepath,
          clip.filename,
          clip.game,
          clip.duration,
          clip.codec,
          clip.resolution,
          clip.fps,
          clip.audioTracks,
          clip.created
        );
      }
    } catch (error) {
      console.error(`Error adding clip ${clip.filepath}:`, error);
    }
  }

  return clips.length;
});

async function scanDirectory(dir, clips, extensions, depth = 0) {
  if (depth > 10) return; // Prevent infinite recursion

  try {
    const entries = await fs.readdir(dir, { withFileTypes: true });

    for (const entry of entries) {
      const fullPath = path.join(dir, entry.name);

      if (entry.isDirectory()) {
        await scanDirectory(fullPath, clips, extensions, depth + 1);
      } else if (entry.isFile()) {
        const ext = path.extname(entry.name).toLowerCase();
        if (extensions.includes(ext)) {
          try {
            const metadata = await getVideoMetadata(fullPath);
            const stats = await fs.stat(fullPath);
            
            // Extract game name from path
            const pathParts = fullPath.split(path.sep);
            let game = 'Unknown';
            
            // Try to find game name in path (common patterns)
            for (let i = pathParts.length - 2; i >= 0; i--) {
              const part = pathParts[i];
              // Skip common folder names
              if (!['clips', 'videos', 'recordings', 'captures', 'shadowplay', 'obs'].includes(part.toLowerCase())) {
                game = part;
                break;
              }
            }

            clips.push({
              filepath: fullPath,
              filename: entry.name,
              game: game,
              duration: metadata.duration,
              codec: metadata.codec,
              resolution: metadata.resolution,
              fps: metadata.fps,
              audioTracks: metadata.audioTracks,
              created: Math.floor(stats.birthtimeMs / 1000)
            });
          } catch (error) {
            console.error(`Error processing ${fullPath}:`, error);
          }
        }
      }
    }
  } catch (error) {
    console.error(`Error reading directory ${dir}:`, error);
  }
}

function getVideoMetadata(filePath) {
  return new Promise((resolve, reject) => {
    ffmpeg.ffprobe(filePath, (err, metadata) => {
      if (err) {
        reject(err);
        return;
      }

      const videoStream = metadata.streams.find(s => s.codec_type === 'video');
      const audioStreams = metadata.streams.filter(s => s.codec_type === 'audio');

      resolve({
        duration: metadata.format.duration || 0,
        codec: videoStream ? videoStream.codec_name : 'unknown',
        resolution: videoStream ? `${videoStream.width}x${videoStream.height}` : 'unknown',
        fps: videoStream && videoStream.r_frame_rate ? eval(videoStream.r_frame_rate) : 0,
        audioTracks: audioStreams.length
      });
    });
  });
}

// Clip queries
ipcMain.handle('get-clips', (event, filters = {}) => {
  let query = 'SELECT * FROM clips WHERE 1=1';
  const params = [];

  if (filters.game) {
    query += ' AND game = ?';
    params.push(filters.game);
  }

  if (filters.rating) {
    query += ' AND rating >= ?';
    params.push(filters.rating);
  }

  if (filters.search) {
    query += ' AND (filename LIKE ? OR game LIKE ?)';
    params.push(`%${filters.search}%`, `%${filters.search}%`);
  }

  if (filters.tag) {
    query = `SELECT DISTINCT c.* FROM clips c
             INNER JOIN clip_tags ct ON c.id = ct.clip_id
             INNER JOIN tags t ON ct.tag_id = t.id
             WHERE t.name = ?`;
    params.length = 0;
    params.push(filters.tag);
  }

  query += ' ORDER BY added_at DESC';

  if (filters.limit) {
    query += ' LIMIT ?';
    params.push(filters.limit);
  }

  return db.prepare(query).all(...params);
});

ipcMain.handle('get-clip', (event, id) => {
  return db.prepare('SELECT * FROM clips WHERE id = ?').get(id);
});

ipcMain.handle('get-games', () => {
  return db.prepare('SELECT DISTINCT game FROM clips ORDER BY game').all().map(row => row.game);
});

// Ratings
ipcMain.handle('set-rating', (event, clipId, rating) => {
  db.prepare('UPDATE clips SET rating = ? WHERE id = ?').run(rating, clipId);
  return true;
});

// Tags
ipcMain.handle('get-tags', () => {
  return db.prepare('SELECT * FROM tags ORDER BY name').all();
});

ipcMain.handle('create-tag', (event, name) => {
  try {
    const result = db.prepare('INSERT INTO tags (name) VALUES (?)').run(name);
    return { id: result.lastInsertRowid, name };
  } catch (error) {
    if (error.message.includes('UNIQUE constraint failed')) {
      return db.prepare('SELECT * FROM tags WHERE name = ?').get(name);
    }
    throw error;
  }
});

ipcMain.handle('get-clip-tags', (event, clipId) => {
  return db.prepare(`
    SELECT t.* FROM tags t
    INNER JOIN clip_tags ct ON t.id = ct.tag_id
    WHERE ct.clip_id = ?
    ORDER BY t.name
  `).all(clipId);
});

ipcMain.handle('add-clip-tag', (event, clipId, tagId) => {
  try {
    db.prepare('INSERT INTO clip_tags (clip_id, tag_id) VALUES (?, ?)').run(clipId, tagId);
    return true;
  } catch (error) {
    console.error('Error adding tag:', error);
    return false;
  }
});

ipcMain.handle('remove-clip-tag', (event, clipId, tagId) => {
  db.prepare('DELETE FROM clip_tags WHERE clip_id = ? AND tag_id = ?').run(clipId, tagId);
  return true;
});

// Trim/Export
ipcMain.handle('trim-clip', async (event, clipId, startTime, endTime, outputFormat = 'h264') => {
  const clip = db.prepare('SELECT * FROM clips WHERE id = ?').get(clipId);
  if (!clip) {
    throw new Error('Clip not found');
  }

  const inputPath = clip.filepath;
  const ext = path.extname(inputPath);
  const outputPath = inputPath.replace(ext, `_trimmed_${Date.now()}${ext}`);

  return new Promise((resolve, reject) => {
    let command = ffmpeg(inputPath)
      .setStartTime(startTime)
      .setDuration(endTime - startTime);

    // Set codec based on output format
    if (outputFormat === 'h264') {
      command = command
        .videoCodec('libx264')
        .audioCodec('aac');
    } else {
      // Copy codecs for faster processing
      command = command
        .videoCodec('copy')
        .audioCodec('copy');
    }

    command
      .on('end', () => resolve(outputPath))
      .on('error', (err) => reject(err))
      .save(outputPath);
  });
});

ipcMain.handle('export-clip', async (event, clipId, options) => {
  const clip = db.prepare('SELECT * FROM clips WHERE id = ?').get(clipId);
  if (!clip) {
    throw new Error('Clip not found');
  }

  const { filepath: inputPath } = clip;
  const { outputDir, format = 'h264', quality = 'medium' } = options;

  const ext = path.extname(inputPath);
  const basename = path.basename(inputPath, ext);
  const outputPath = path.join(outputDir || path.dirname(inputPath), `${basename}_export.mp4`);

  return new Promise((resolve, reject) => {
    let command = ffmpeg(inputPath);

    if (format === 'h264') {
      command = command
        .videoCodec('libx264')
        .audioCodec('aac');

      // Set quality presets
      if (quality === 'high') {
        command = command.videoBitrate('8000k');
      } else if (quality === 'medium') {
        command = command.videoBitrate('4000k');
      } else {
        command = command.videoBitrate('2000k');
      }
    }

    command
      .on('end', () => resolve(outputPath))
      .on('error', (err) => reject(err))
      .save(outputPath);
  });
});

// Statistics
ipcMain.handle('get-stats', () => {
  const totalClips = db.prepare('SELECT COUNT(*) as count FROM clips').get().count;
  const totalGames = db.prepare('SELECT COUNT(DISTINCT game) as count FROM clips').get().count;
  const totalTags = db.prepare('SELECT COUNT(*) as count FROM tags').get().count;
  
  const topGames = db.prepare(`
    SELECT game, COUNT(*) as count
    FROM clips
    GROUP BY game
    ORDER BY count DESC
    LIMIT 5
  `).all();

  return {
    totalClips,
    totalGames,
    totalTags,
    topGames
  };
});
