# Troubleshooting Guide

## Common Issues and Solutions

### Installation Issues

#### Error: "Module not found"
**Solution:**
```bash
# Delete node_modules and reinstall
rm -rf node_modules package-lock.json
npm install
```

#### Error: "EACCES: permission denied"
**Solution (Linux/macOS):**
```bash
# Fix npm permissions
sudo chown -R $USER:$USER ~/.npm
sudo chown -R $USER:$USER .
npm install
```

**Solution (Windows):**
Run Command Prompt as Administrator, then:
```bash
npm install
```

### Runtime Errors

#### Error: "Store is not a constructor"
**Status:** ✅ Fixed in latest version
**Cause:** electron-store v11+ uses ES modules
**Solution:** Downgraded to electron-store v8.1.0 (already done)

If you still see this error:
```bash
npm install electron-store@8.1.0
```

#### Error: "Cannot find module 'electron'"
**Solution:**
```bash
npm install electron
```

#### Error: "SUID sandbox helper binary"
**Context:** This appears in some Linux environments
**Solution:** Run with sandbox disabled:
```bash
npm start -- --no-sandbox
```

Or permanently in package.json:
```json
"scripts": {
  "start": "electron . --no-sandbox"
}
```

#### Error: "ffprobe/ffmpeg not found"
**Status:** Should not occur (ffmpeg is bundled)
**Solution:** If it does occur:
```bash
npm install @ffmpeg-installer/ffmpeg
```

#### Database locked error
**Cause:** Multiple instances running or improper shutdown
**Solution:**
1. Close all ClipGallery windows
2. Delete database lock files:
   - Windows: `%APPDATA%\clipgallery\clipgallery.db-wal` and `.db-shm`
   - Linux: `~/.config/clipgallery/clipgallery.db-wal` and `.db-shm`
   - macOS: `~/Library/Application Support/clipgallery/clipgallery.db-wal` and `.db-shm`
3. Restart the application

### Application Issues

#### "Scan Clips" finds no clips
**Possible causes:**
1. **Wrong directory selected**
   - Verify you selected the correct folder
   - Check that folder contains video files

2. **Unsupported file format**
   - Supported: `.mp4`, `.mkv`, `.avi`, `.mov`, `.webm`, `.m4v`, `.flv`
   - Check your file extensions

3. **Nested too deep**
   - Max depth is 10 folders
   - Move clips to shallower structure

**Solution:**
```bash
# Check if files exist
ls /path/to/clips
# or on Windows
dir C:\path\to\clips
```

#### Video won't play
**Possible causes:**
1. **Codec not supported by browser**
   - Try exporting as H.264
   
2. **File corrupted**
   - Try playing in VLC or another player
   
3. **File moved/deleted**
   - Re-scan to update database

**Solution:**
1. Open clip details to see codec
2. If codec is rare, export as H.264:
   - Open clip
   - Click "Export as H.264"
   - Use exported version

#### Trim/Export not working
**Possible causes:**
1. **FFmpeg error**
   - Check console for errors (Ctrl+Shift+I / Cmd+Option+I)
   
2. **Disk space**
   - Ensure enough free space for output
   
3. **File permissions**
   - Ensure write access to clip directory

**Solution:**
1. Check available disk space
2. Try trimming to a different location
3. Check Developer Console for detailed errors

#### Tags or ratings not saving
**Possible causes:**
1. **Database locked**
   - See "Database locked error" above
   
2. **Database corrupted**
   - May need to reset database

**Solution (Reset Database):**
⚠️ **Warning:** This will delete all ratings and tags
1. Close ClipGallery
2. Delete database file:
   - Windows: `%APPDATA%\clipgallery\clipgallery.db`
   - Linux: `~/.config/clipgallery/clipgallery.db`
   - macOS: `~/Library/Application Support/clipgallery/clipgallery.db`
3. Restart and re-scan

#### UI not updating after scan
**Solution:**
1. Click different sidebar item and back
2. Use search box to force refresh
3. Restart application if needed

### Performance Issues

#### Slow scanning
**Causes:**
- Many files to scan
- Slow disk (network drive, external HDD)
- Large video files

**Solutions:**
1. Be patient during first scan
2. Move clips to local SSD if possible
3. Reduce number of source directories

#### Slow playback
**Causes:**
- High resolution video
- CPU-intensive codec (AV1)
- Slow disk

**Solutions:**
1. Export to H.264 for better performance
2. Lower video quality in export settings
3. Move clips to faster storage

#### Application laggy
**Solutions:**
1. Close other applications
2. Reduce window size
3. Filter clips to reduce visible items

### Developer Issues

#### Changes not reflecting
**Solutions:**
- **main.js changes:** Restart application
- **renderer.js changes:** Press Ctrl+R (Cmd+R on macOS)
- **index.html changes:** Press Ctrl+R (Cmd+R on macOS)

#### Cannot open DevTools
**Solution:**
Set environment variable:
```bash
# Linux/macOS
NODE_ENV=development npm start

# Windows (Command Prompt)
set NODE_ENV=development
npm start

# Windows (PowerShell)
$env:NODE_ENV="development"
npm start
```

Or press Ctrl+Shift+I / Cmd+Option+I in the app.

### Platform-Specific Issues

#### Windows: "Windows protected your PC"
**Cause:** Unsigned executable
**Solution:** Click "More info" → "Run anyway"

#### macOS: "App is damaged and can't be opened"
**Cause:** Gatekeeper blocking unsigned app
**Solution:**
```bash
xattr -cr /path/to/ClipGallery.app
```

#### Linux: Missing dependencies
**Solution:**
```bash
# Debian/Ubuntu
sudo apt-get install libgtk-3-0 libnotify4 libnss3 libxss1 libxtst6 xdg-utils libatspi2.0-0 libdrm2 libgbm1 libxcb-dri3-0

# Fedora
sudo dnf install gtk3 libnotify nss libXScrnSaver libXtst xdg-utils at-spi2-atk libdrm mesa-libgbm libxcb
```

## Getting Help

### Gather Information
When reporting issues, include:
1. Operating system and version
2. Node.js version (`node --version`)
3. npm version (`npm --version`)
4. ClipGallery version (from Settings → About)
5. Error messages (full text)
6. Steps to reproduce
7. Screenshots if UI issue

### Check Console
Many errors appear in Developer Console:
1. Open DevTools: Ctrl+Shift+I (Windows/Linux) or Cmd+Option+I (macOS)
2. Go to Console tab
3. Copy error messages

### Debug Mode
Run with extra logging:
```bash
# Enable Electron debug output
DEBUG=* npm start
```

### Reset to Defaults
If all else fails:
```bash
# 1. Backup your database (optional)
# 2. Remove all app data
# Windows:
del %APPDATA%\clipgallery /Q
# Linux:
rm -rf ~/.config/clipgallery
# macOS:
rm -rf ~/Library/Application\ Support/clipgallery

# 3. Reinstall
rm -rf node_modules package-lock.json
npm install
npm start
```

## Reporting Bugs

Please report bugs on GitHub with:
- Clear description
- Steps to reproduce
- Expected vs actual behavior
- System information
- Console output
- Screenshots

GitHub Issues: https://github.com/Xenthio/ClipGallery/issues

## FAQ

**Q: Can I edit the source directories after scanning?**
A: Yes, in Settings. Old clips remain in database until you remove them manually or clear database.

**Q: What happens if I move a video file?**
A: It will show as missing. Re-scan to update paths, or manually remove from database.

**Q: Can I export multiple clips at once?**
A: Not currently. This is a future enhancement.

**Q: Does it modify my original clips?**
A: No. Trim and export create new files. Originals are never modified.

**Q: Can I backup my ratings and tags?**
A: Yes, copy the database file from the userData directory.

**Q: How much disk space does it use?**
A: Minimal. Database is typically <10MB. Exported clips depend on their size.

**Q: Can I run this on a server?**
A: No, it's a desktop app requiring a display. Headless mode is not supported.

---

Still having issues? Check the [GitHub Issues](https://github.com/Xenthio/ClipGallery/issues) or create a new issue.
