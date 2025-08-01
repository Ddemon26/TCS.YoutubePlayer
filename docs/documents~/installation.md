# Installation Guide

Learn how to install TCS.YoutubePlayer in your Unity project using different methods.

## System Requirements

Before installing, ensure your system meets these requirements:

| Requirement | Specification |
|-------------|---------------|
| **Unity Version** | 2020.3 or newer |
| **Platform** | Windows (full support), macOS/Linux (planned) |
| **Internet Connection** | Required for initial tool downloads |
| **Storage Space** | ~50MB for external tools |
| **.NET Version** | .NET Standard 2.1 compatible |

## Installation Methods

### Method 1: Unity Package Manager (Recommended)

The easiest way to install TCS.YoutubePlayer is through Unity's Package Manager:

1. **Open Package Manager**
   - In Unity, go to `Window > Package Manager`

2. **Add Package from Git URL**
   - Click the `+` button in the top-left corner
   - Select `Add package from git URL...`

3. **Enter Repository URL**
   ```
   https://github.com/Ddemon26/TCS-YoutubePlayer.git
   ```

4. **Install Package**
   - Click `Add` and wait for Unity to download and import the package
   - The package will appear in your Package Manager under "In Project"

### Method 2: Manual Installation

For more control or offline installation:

1. **Download the Package**
   - Go to the [GitHub Releases](https://github.com/Ddemon26/TCS-YoutubePlayer/releases)
   - Download the latest `.unitypackage` file

2. **Import to Unity**
   - In Unity, go to `Assets > Import Package > Custom Package...`
   - Select the downloaded `.unitypackage` file
   - Click `Import` to add all files to your project

### Method 3: Clone Repository

For development or customization:

1. **Clone the Repository**
   ```bash
   git clone https://github.com/Ddemon26/TCS-YoutubePlayer.git
   ```

2. **Copy to Unity Project**
   - Copy the entire folder to your Unity project's `Assets` directory
   - Unity will automatically detect and import the package

## Post-Installation Setup

### Verify Installation

After installation, verify the package is working:

1. **Check Package Files**
   - Look for `TCS.YoutubePlayer` folder in your Project window
   - Verify these key folders exist:
     ```
     TCS.YoutubePlayer/
     ├── Runtime/
     │   ├── Scripts/
     │   ├── Prefabs/
     │   └── Materials/
     └── Editor/
     ```

2. **Test Assembly References**
   - Create a test script with this code:
   ```csharp
   using TCS.YoutubePlayer;
   
   public class InstallationTest : MonoBehaviour 
   {
       void Start() 
       {
           Debug.Log("TCS.YoutubePlayer installed successfully!");
       }
   }
   ```

### Initial Configuration

1. **StreamingAssets Folder**
   - The package will create `Assets/StreamingAssets/` if it doesn't exist
   - This folder stores external tools (yt-dlp, FFmpeg)

2. **First Run Setup**
   - On first use, the system will automatically download required tools
   - This requires an internet connection and may take a few minutes

## Package Structure

After installation, your project will include:

```
Assets/TCS.YoutubePlayer/
├── Runtime/
│   ├── Scripts/
│   │   ├── Caching/           # URL cache management
│   │   ├── Configuration/     # Settings and config
│   │   ├── Exceptions/        # Custom exception types
│   │   ├── ProcessExecution/  # External process handling
│   │   ├── ToolManagement/    # Automatic tool downloads
│   │   ├── UrlProcessing/     # YouTube URL parsing
│   │   ├── VideoConversion/   # MP4 conversion
│   │   ├── Utils/             # Logging and utilities
│   │   └── UIToolkit/         # UI components
│   ├── Materials/             # Video materials
│   ├── Prefabs/              # Ready-to-use prefabs
│   ├── Textures/             # Render textures
│   └── TCS.YoutubePlayer.asmdef
├── Editor/
│   ├── StreamingAssetsImporter.cs
│   └── TCS.YoutubePlayer.Editor.asmdef
└── StreamingAssets~/          # External tools (auto-downloaded)
```

## Troubleshooting Installation

### Common Issues

**Package Manager Shows "Error"**
- Ensure you have internet access
- Check if the Git URL is correct
- Try refreshing the Package Manager

**Assembly Reference Errors**
- Restart Unity after installation
- Check that all `.asmdef` files are properly imported
- Verify Unity version compatibility

**Missing Dependencies**
- The package is self-contained with no external Unity dependencies
- External tools (yt-dlp, FFmpeg) are downloaded automatically

### Manual Tool Installation

If automatic tool downloads fail, you can install them manually:

1. **Create Directory Structure**
   ```
   Assets/StreamingAssets/
   ├── yt-dlp/Windows/yt-dlp.exe
   └── ffmpeg/Windows/bin/ffmpeg.exe
   ```

2. **Download Tools**
   - **yt-dlp**: Download from [GitHub releases](https://github.com/yt-dlp/yt-dlp/releases)
   - **FFmpeg**: Download from [gyan.dev](https://www.gyan.dev/ffmpeg/builds/)

3. **Create Version File**
   Create `Assets/StreamingAssets/tool_versions.json`:
   ```json
   {
     "yt-dlp": "2023.12.30",
     "ffmpeg": "6.1"
   }
   ```

## Updating the Package

### Via Package Manager
1. Open Package Manager
2. Find TCS.YoutubePlayer in "In Project"
3. Click "Update" if available

### Manual Update
1. Remove the old package folder
2. Follow installation steps with the new version
3. Reimport any custom configurations

## Uninstallation

To remove TCS.YoutubePlayer:

1. **Via Package Manager**
   - Select the package and click "Remove"

2. **Manual Removal**
   - Delete the `TCS.YoutubePlayer` folder from Assets
   - Delete `StreamingAssets/yt-dlp` and `StreamingAssets/ffmpeg` folders
   - Remove any custom scripts using the package

## Next Steps

After successful installation:

1. **[Quick Start Guide](quick-start.md)**: Play your first YouTube video
2. **[System Overview](core/overview.md)**: Understand the architecture
3. **[Configuration](config/player-settings.md)**: Customize the player

The installation is complete! You're ready to start integrating YouTube video playback into your Unity project.