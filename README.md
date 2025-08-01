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
- `StreamingAssets~` – External tools including ffmpeg and yt\-dlp

## Requirements

- Unity Editor
- ffmpeg
- yt\-dlp

## YT-DLP Setup
File|Description
:---|:---
[yt-dlp](https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp)|Platform-independent [zipimport](https://docs.python.org/3/library/zipimport.html) binary. Needs Python (recommended for **Linux/BSD**)
[yt-dlp.exe](https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe)|Windows (Win8+) standalone x64 binary (recommended for **Windows**)
[yt-dlp_macos](https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_macos)|Universal MacOS (10.15+) standalone executable (recommended for **MacOS**)

## ffmpeg Setup
File|Description
:---|:---
[ffmpeg.exe](https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-full.7z) |Windows (Win8+) standalone x64 binary (recommended for **Windows**)
[ffmpeg](Unkown) |Linux (x64) standalone binary (recommended for **Linux**)
[ffmpeg](Unkown) |MacOS (x64) standalone binary (recommended for **MacOS**)
## Usage

Add the YouTube Player prefab from `Runtime/Prefabs` to your scene and configure the settings via the Inspector.

## License

Refer to the `LICENSE` file for details.