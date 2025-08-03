# TCS.YoutubePlayer

[![Unity Version](https://img.shields.io/badge/Unity-2020.3%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey.svg)](https://docs.microsoft.com/en-us/windows/)

A Unity package for YouTube video playback with automatic tool management, supporting both streaming and download functionalities.

## Features

- Direct YouTube video playback from URLs
- Dual playback modes: streaming and download
- Automatic external tool downloading (yt-dlp & FFmpeg)
- Comprehensive playback controls (play, pause, seek, speed, volume)
- Intelligent URL caching with expiration management
- Cross-platform architecture (Windows, macOS, Linux)
- Unity UI Toolkit integration
- Modern async/await programming patterns

## Installation

### Unity Package Manager

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click the `+` button and select `Add package from git URL`
3. Enter: `https://github.com/Ddemon26/TCS.YoutubePlayer.git`

### Manual Installation

1. Download the latest release from [Releases](https://github.com/Ddemon26/TCS.YoutubePlayer/releases)
2. Extract to your Unity project's `Assets` folder
3. Unity will automatically import the package

### Requirements

- Unity Editor 2020.3 or newer
- Internet connection for automatic tool downloads on first use
- Windows (full support), macOS/Linux (planned)

## Quick Start

1. Add the prefab: Drag `Runtime/Prefabs/YoutubePlayer.prefab` to your scene
2. Configure settings: Set download permissions in the Inspector
3. Call `PlayVideo(url)` to play a YouTube video

```csharp
using TCS.YoutubePlayer;

public class VideoController : MonoBehaviour 
{
    [SerializeField] YoutubePlayer youtubePlayer;
    
    void Start() 
    {
        youtubePlayer.PlayVideo("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
    }
}
```

## Configuration

### External Tools Setup

The system automatically downloads and manages external tools:

- **yt-dlp.exe** - Latest version from [GitHub releases](https://github.com/yt-dlp/yt-dlp/releases)
- **FFmpeg Essentials** - From [gyan.dev](https://www.gyan.dev/ffmpeg/builds/)

**Download Location**: `Assets/StreamingAssets/`

```
StreamingAssets/
├── yt-dlp/Windows/yt-dlp.exe
└── ffmpeg/Windows/bin/ffmpeg.exe
```

### Manual Override

For custom tool versions:

1. Place executables at the expected paths in StreamingAssets
2. Create/update `tool_versions.json` to prevent re-downloads

### Platform Support

| Platform | Status | Notes |
|----------|--------|-------|
| Windows  | Full Support | Automatic downloads available |
| macOS    | Planned | Manual installation required |
| Linux    | Planned | Manual installation required |

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Make your changes and commit: `git commit -m 'Add your feature'`
4. Push to the branch: `git push origin feature/your-feature`
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [yt-dlp](https://github.com/yt-dlp/yt-dlp) - YouTube video extraction
- [FFmpeg](https://ffmpeg.org/) - Video processing and conversion