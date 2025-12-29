# Contributing to ClipGallery

Thank you for considering contributing to ClipGallery! This document provides guidelines and information for contributors.

## Code of Conduct

- Be respectful and inclusive
- Focus on constructive feedback
- Help create a welcoming environment for all contributors

## How to Contribute

### Reporting Bugs

When reporting bugs, please include:
- ClipGallery version
- Operating system and version
- Steps to reproduce the issue
- Expected vs actual behavior
- Screenshots or error messages if applicable
- Sample clips or folder structures (if relevant)

### Suggesting Features

Feature requests are welcome! Please include:
- Clear description of the feature
- Use case or problem it solves
- Potential implementation approach (if you have ideas)
- Examples from other applications (if applicable)

### Pull Requests

1. **Fork the Repository**
   ```bash
   git clone https://github.com/Xenthio/ClipGallery.git
   cd ClipGallery
   ```

2. **Create a Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make Your Changes**
   - Follow the existing code style
   - Add comments for complex logic
   - Test your changes thoroughly

4. **Test Your Changes**
   ```bash
   npm start
   ```
   - Verify the application starts
   - Test affected features
   - Check for console errors

5. **Commit Your Changes**
   ```bash
   git add .
   git commit -m "Add feature: description"
   ```

6. **Push to Your Fork**
   ```bash
   git push origin feature/your-feature-name
   ```

7. **Create a Pull Request**
   - Go to the original repository
   - Click "New Pull Request"
   - Select your feature branch
   - Provide a clear description

## Development Setup

### Prerequisites
- Node.js v16 or higher
- npm
- Git

### Installation
```bash
git clone https://github.com/Xenthio/ClipGallery.git
cd ClipGallery
npm install
```

### Running in Development
```bash
# Start with developer tools
NODE_ENV=development npm start
```

### Project Structure
```
ClipGallery/
├── main.js           # Electron main process
│                     # - Database initialization
│                     # - IPC handlers
│                     # - File system operations
│                     # - FFmpeg integration
│
├── renderer.js       # Renderer process
│                     # - UI event handlers
│                     # - Data loading and display
│                     # - Player controls
│
├── index.html        # Application UI
│                     # - HTML structure
│                     # - Embedded CSS
│
├── package.json      # Dependencies and scripts
├── assets/           # Icons and static files
└── README.md         # Main documentation
```

## Coding Guidelines

### JavaScript Style

**General**
- Use modern JavaScript (ES6+)
- Use `const` and `let`, avoid `var`
- Use arrow functions where appropriate
- Add semicolons consistently

**Naming Conventions**
- Use camelCase for variables and functions: `loadClips()`, `currentClipId`
- Use PascalCase for classes: `Database`, `VideoPlayer`
- Use UPPER_SNAKE_CASE for constants: `MAX_FILE_SIZE`

**Comments**
- Add comments for complex logic
- Use JSDoc for function documentation when helpful
- Explain "why" not "what" in comments

**Example:**
```javascript
// Good
async function scanDirectory(dir, clips, extensions, depth = 0) {
  // Prevent infinite recursion in circular symlinks
  if (depth > 10) return;
  
  // ... implementation
}

// Bad
// scans directory
function scanDirectory(dir, clips, extensions, depth = 0) {
  if (depth > 10) return; // return if depth is more than 10
  
  // ... implementation
}
```

### Database

**Schema Changes**
- Add migrations for schema changes
- Keep backward compatibility when possible
- Update database version

**Queries**
- Use prepared statements
- Avoid SQL injection vulnerabilities
- Add indexes for frequently queried columns

### UI/UX

**Consistency**
- Follow existing design patterns
- Use the established color scheme
- Maintain consistent spacing and sizing

**Accessibility**
- Use semantic HTML
- Add keyboard navigation
- Include ARIA labels where needed

**Responsiveness**
- Test at different window sizes
- Ensure UI elements scale appropriately

## Feature Development

### Adding New Features

1. **Plan the Feature**
   - Outline requirements
   - Consider edge cases
   - Think about backward compatibility

2. **Database Changes (if needed)**
   - Update schema in `initDatabase()`
   - Add new IPC handlers in main.js
   - Test with existing data

3. **Backend Implementation**
   - Add IPC handlers in main.js
   - Implement business logic
   - Add error handling

4. **Frontend Implementation**
   - Update UI in index.html
   - Add event handlers in renderer.js
   - Style new components

5. **Testing**
   - Test happy path
   - Test edge cases
   - Test error conditions

### Example: Adding a New Filter

1. **Database** (main.js)
```javascript
ipcMain.handle('get-clips-by-duration', (event, minDuration, maxDuration) => {
  return db.prepare(`
    SELECT * FROM clips 
    WHERE duration BETWEEN ? AND ?
    ORDER BY added_at DESC
  `).all(minDuration, maxDuration);
});
```

2. **UI** (index.html)
```html
<select id="duration-filter">
  <option value="">All Durations</option>
  <option value="0-30">Under 30s</option>
  <option value="30-60">30s - 1min</option>
  <option value="60-300">1-5 min</option>
</select>
```

3. **Event Handler** (renderer.js)
```javascript
document.getElementById('duration-filter').addEventListener('change', async (e) => {
  const value = e.target.value;
  if (value) {
    const [min, max] = value.split('-').map(Number);
    const clips = await ipcRenderer.invoke('get-clips-by-duration', min, max);
    displayClips(clips);
  } else {
    loadClips();
  }
});
```

## Common Tasks

### Adding a New Video Format

Edit `main.js`:
```javascript
const videoExtensions = [
  '.mp4', '.mkv', '.avi', '.mov', 
  '.webm', '.m4v', '.flv',
  '.your-new-extension'  // Add here
];
```

### Adding a New Metadata Field

1. Update database schema:
```javascript
db.exec(`
  ALTER TABLE clips ADD COLUMN your_field TEXT;
`);
```

2. Update scan function to collect data
3. Update display to show data

### Modifying the Theme

Edit CSS in `index.html`:
```css
body {
  background: #1a1a1a;  /* Dark background */
  color: #e0e0e0;        /* Light text */
}

.btn-primary {
  background: #0066cc;   /* Primary color */
}
```

## Testing

### Manual Testing Checklist

Before submitting a PR:
- [ ] Application starts without errors
- [ ] Can add source directories
- [ ] Scan finds clips correctly
- [ ] Clips display in gallery
- [ ] Video playback works
- [ ] Ratings can be set
- [ ] Tags can be added/removed
- [ ] Filters work correctly
- [ ] Trim function works
- [ ] Export function works
- [ ] Settings save properly

### Testing with Sample Data

Create test folder structure:
```
test-clips/
├── Game1/
│   ├── clip1.mp4
│   └── clip2.mp4
└── Game2/
    └── clip3.mp4
```

## Release Process

1. Update version in package.json
2. Update CHANGELOG.md
3. Test thoroughly
4. Create release tag
5. Build distributables
6. Publish release

## Questions?

- Open an issue for questions
- Check existing issues and PRs
- Read the documentation

## License

By contributing, you agree that your contributions will be licensed under the ISC License.
