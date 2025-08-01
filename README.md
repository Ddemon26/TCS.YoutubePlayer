# TCS\.YoutubePlayer

## Overview

TCS\.YoutubePlayer is a Unity project designed to integrate YouTube video playback with both streaming and download functionalities using yt\-dlp and ffmpeg.

## Features

- Youtube video streaming
- Youtube video download
- Custom editor tools and runtime components

## Directory Structure

- `Editor` – Editor tools and related assembly definitions
- `Runtime` – Core runtime scripts, materials, prefabs, and textures
- `Runtime/Scripts/ToolManagement` – Automatic tool download and management system

## Requirements

- Unity Editor
- Internet connection (for automatic tool downloads on first use)

## External Tools Setup

**No manual setup required!** TCS.YoutubePlayer automatically downloads and manages external tools on first use.

### Automatic Downloads
The system will automatically download:
- **yt-dlp.exe** - Latest version from GitHub releases for Windows
- **ffmpeg-essentials** - Essential build from gyan.dev for Windows

### Download Locations
Tools are downloaded to: `Assets/StreamingAssets/TCS.YoutubePlayer.{version}/`

Structure:
- `yt-dlp/Windows/yt-dlp.exe`
- `ffmpeg/Windows/bin/ffmpeg.exe`

### Supported Platforms
- **Windows**: Full support with automatic downloads
- **macOS/Linux**: Support planned (manual installation required for now)

### Manual Override (Advanced)
If you need to use custom tool versions, you can:
1. Place tools at the expected paths in the StreamingAssets directory structure
2. Update the tool_versions.json file in the StreamingAssets directory to prevent re-downloads
## Usage

Add the YouTube Player prefab from `Runtime/Prefabs` to your scene and configure the settings via the Inspector.

## License

Refer to the `LICENSE` file for details.