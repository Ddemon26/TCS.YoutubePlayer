# Quick Start Guide

Get your first YouTube video playing in Unity in under 5 minutes!

## Prerequisites

Before starting, ensure you have:
- Unity 2020.3 or newer
- TCS.YoutubePlayer package installed ([Installation Guide](installation.md))
- An active internet connection

## Step 1: Add the Player to Your Scene

1. **Locate the Prefab**: In your Project window, navigate to:
   ```
   Assets/TCS.YoutubePlayer/Runtime/Prefabs/YoutubePlayer.prefab
   ```

2. **Drag to Scene**: Drag the `YoutubePlayer.prefab` into your scene hierarchy.

3. **Position the Player**: The prefab includes a UI Canvas. Position it where you want the video to appear.

## Step 2: Basic Script Setup

Create a simple script to control the YouTube player:

```csharp
using UnityEngine;
using TCS.YoutubePlayer;

public class VideoController : MonoBehaviour 
{
    [SerializeField] private YoutubePlayer youtubePlayer;
    [SerializeField] private string videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
    
    void Start() 
    {
        // Play video on start
        PlayVideo();
    }
    
    public void PlayVideo()
    {
        if (youtubePlayer != null)
        {
            youtubePlayer.PlayVideo(videoUrl);
        }
    }
    
    public void PauseVideo()
    {
        youtubePlayer?.Pause();
    }
    
    public void StopVideo()
    {
        youtubePlayer?.Stop();
    }
}
```

## Step 3: Configure the Player

1. **Select the YoutubePlayer** in your scene hierarchy
2. **In the Inspector**, configure basic settings:
   - **Allow Downloads**: Enable if you want to cache videos locally
   - **Auto Play**: Enable to start playback automatically
   - **Loop**: Enable to loop the video

## Step 4: Test Your Setup

1. **Enter Play Mode** in Unity
2. **Watch for Console Messages**: The first run will download external tools (yt-dlp and FFmpeg)
3. **Video Should Start Playing**: After tool setup, your video will begin playback

## Common First-Time Setup

### Tool Download Process

On first run, you'll see console messages like:
```
[YoutubePlayer] Downloading yt-dlp...
[YoutubePlayer] Downloading FFmpeg...
[YoutubePlayer] Tools ready, starting playback...
```

This is normal and only happens once. Tools are cached in:
```
Assets/StreamingAssets/yt-dlp/Windows/
Assets/StreamingAssets/ffmpeg/Windows/
```

### Troubleshooting First Run

**Video Not Playing?**
- Check console for error messages
- Ensure internet connection is active
- Verify the YouTube URL is valid and accessible

**Tools Not Downloading?**
- Check firewall/antivirus settings
- Ensure Unity has internet access
- Try running Unity as administrator (Windows)

## Next Steps

Now that you have basic playback working, explore these features:

### Advanced Playback Controls

```csharp
// Seek to specific time (in seconds)
youtubePlayer.SeekTo(30f);

// Change playback speed
youtubePlayer.SetPlaybackSpeed(1.5f);

// Adjust volume
youtubePlayer.SetVolume(0.8f);

// Get video information
var info = youtubePlayer.GetVideoInfo();
Debug.Log($"Title: {info.Title}, Duration: {info.Duration}");
```

### Event Handling

```csharp
void Start()
{
    youtubePlayer.OnVideoStarted += OnVideoStarted;
    youtubePlayer.OnVideoEnded += OnVideoEnded;
    youtubePlayer.OnError += OnError;
}

private void OnVideoStarted()
{
    Debug.Log("Video playback started!");
}

private void OnVideoEnded()
{
    Debug.Log("Video playback finished!");
}

private void OnError(string error)
{
    Debug.LogError($"Playback error: {error}");
}
```

### Quality Selection

```csharp
// Set preferred quality
youtubePlayer.SetPreferredQuality(VideoQuality.HD720);

// Get available qualities
var qualities = youtubePlayer.GetAvailableQualities();
foreach (var quality in qualities)
{
    Debug.Log($"Available: {quality}");
}
```

## What's Next?

- **[System Overview](core/overview.md)**: Learn about the architecture
- **[Configuration](config/player-settings.md)**: Customize player behavior  
- **[Examples](examples/basic-player.md)**: See more implementation examples
- **[API Reference](api/youtube-player.md)**: Complete API documentation

Congratulations! You now have YouTube video playback working in your Unity project. The system will handle tool management, caching, and video processing automatically.