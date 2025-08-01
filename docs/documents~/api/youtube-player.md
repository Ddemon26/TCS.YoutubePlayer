# YoutubePlayer API Reference

The `YoutubePlayer` class is the main component for YouTube video playback in Unity. This reference covers all public methods, properties, and events.

## Class Declaration

```csharp
namespace TCS.YoutubePlayer
{
    public class YoutubePlayer : MonoBehaviour
    {
        // Implementation
    }
}
```

## Properties

### Configuration Properties

#### `PreferredQuality`
```csharp
public VideoQuality PreferredQuality { get; set; }
```
Gets or sets the preferred video quality for playback.

**Values:**
- `VideoQuality.Auto` - Automatic quality selection
- `VideoQuality.HD1080` - 1080p resolution
- `VideoQuality.HD720` - 720p resolution  
- `VideoQuality.Medium480` - 480p resolution
- `VideoQuality.Small360` - 360p resolution

#### `AllowDownloads`
```csharp
public bool AllowDownloads { get; set; }
```
Gets or sets whether the player can download videos for offline playback.

#### `AutoPlay`
```csharp
public bool AutoPlay { get; set; }
```
Gets or sets whether videos should start playing automatically after loading.

#### `Loop`
```csharp
public bool Loop { get; set; }
```
Gets or sets whether videos should loop when they reach the end.

### State Properties

#### `IsPlaying`
```csharp
public bool IsPlaying { get; }
```
Gets a value indicating whether a video is currently playing.

#### `IsPaused`
```csharp
public bool IsPaused { get; }
```
Gets a value indicating whether playback is currently paused.

#### `IsLoading`
```csharp
public bool IsLoading { get; }
```
Gets a value indicating whether a video is currently being loaded.

#### `CurrentUrl`
```csharp
public string CurrentUrl { get; }
```
Gets the URL of the currently loaded video.

## Methods

### Playback Control

#### `PlayVideo(string url)`
```csharp
public async Task PlayVideo(string url)
```
Loads and plays a YouTube video from the specified URL.

**Parameters:**
- `url` - The YouTube video URL

**Returns:** Task that completes when video starts playing

**Example:**
```csharp
await youtubePlayer.PlayVideo("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
```

#### `Pause()`
```csharp
public void Pause()
```
Pauses video playback if currently playing.

#### `Resume()`
```csharp
public void Resume()
```
Resumes video playback if currently paused.

#### `Stop()`
```csharp
public void Stop()
```
Stops video playback and resets to the beginning.

#### `SeekTo(float seconds)`
```csharp
public void SeekTo(float seconds)
```
Seeks to a specific time position in the video.

**Parameters:**
- `seconds` - Time position in seconds

### Audio Control

#### `SetVolume(float volume)`
```csharp
public void SetVolume(float volume)
```
Sets the audio volume for video playback.

**Parameters:**
- `volume` - Volume level (0.0 to 1.0)

#### `GetVolume()`
```csharp
public float GetVolume()
```
Gets the current audio volume level.

**Returns:** Current volume (0.0 to 1.0)

#### `SetMuted(bool muted)`
```csharp
public void SetMuted(bool muted)
```
Sets whether audio is muted.

**Parameters:**
- `muted` - True to mute, false to unmute

### Playback Speed

#### `SetPlaybackSpeed(float speed)`
```csharp
public void SetPlaybackSpeed(float speed)
```
Sets the playback speed multiplier.

**Parameters:**
- `speed` - Speed multiplier (0.25 to 2.0)

#### `GetPlaybackSpeed()`
```csharp
public float GetPlaybackSpeed()
```
Gets the current playback speed multiplier.

**Returns:** Current speed multiplier

### Time Information

#### `GetCurrentTime()`
```csharp
public float GetCurrentTime()
```
Gets the current playback position in seconds.

**Returns:** Current time in seconds

#### `GetDuration()`
```csharp
public float GetDuration()
```
Gets the total duration of the current video in seconds.

**Returns:** Total duration in seconds

#### `GetProgress()`
```csharp
public float GetProgress()
```
Gets the current playback progress as a percentage.

**Returns:** Progress value (0.0 to 1.0)

### Video Information

#### `GetVideoInfo()`
```csharp
public VideoInfo GetVideoInfo()
```
Gets information about the currently loaded video.

**Returns:** VideoInfo object containing metadata

#### `GetAvailableQualities()`
```csharp
public VideoQuality[] GetAvailableQualities()
```
Gets the available quality options for the current video.

**Returns:** Array of available VideoQuality values

### Quality Control

#### `SetPreferredQuality(VideoQuality quality)`
```csharp
public void SetPreferredQuality(VideoQuality quality)
```
Sets the preferred video quality for current and future playback.

**Parameters:**
- `quality` - Desired video quality

## Events

### Playback Events

#### `OnVideoStarted`
```csharp
public event Action OnVideoStarted
```
Fired when video playback begins.

#### `OnVideoEnded`
```csharp
public event Action OnVideoEnded
```
Fired when video playback reaches the end.

#### `OnVideoPaused`
```csharp
public event Action OnVideoPaused
```
Fired when video playback is paused.

#### `OnVideoResumed`
```csharp
public event Action OnVideoResumed
```
Fired when video playback is resumed from pause.

### Progress Events

#### `OnProgressChanged`
```csharp
public event Action<float> OnProgressChanged
```
Fired periodically during playback with current progress.

**Parameters:**
- `progress` - Current progress (0.0 to 1.0)

#### `OnTimeChanged`
```csharp
public event Action<float> OnTimeChanged
```
Fired periodically during playback with current time.

**Parameters:**
- `currentTime` - Current time in seconds

### Loading Events

#### `OnVideoLoading`
```csharp
public event Action<string> OnVideoLoading
```
Fired when a video starts loading.

**Parameters:**
- `url` - URL of the video being loaded

#### `OnVideoLoaded`
```csharp
public event Action<VideoInfo> OnVideoLoaded
```
Fired when video metadata has been loaded.

**Parameters:**
- `videoInfo` - Information about the loaded video

### Error Events

#### `OnError`
```csharp
public event Action<string> OnError
```
Fired when an error occurs during playback.

**Parameters:**
- `errorMessage` - Description of the error

#### `OnNetworkError`
```csharp
public event Action<string> OnNetworkError
```
Fired when a network-related error occurs.

**Parameters:**
- `errorMessage` - Description of the network error

## Supporting Classes

### VideoInfo

```csharp
public class VideoInfo
{
    public string Title { get; set; }
    public string Description { get; set; }
    public float Duration { get; set; }
    public string ThumbnailUrl { get; set; }
    public string Author { get; set; }
    public DateTime UploadDate { get; set; }
    public long ViewCount { get; set; }
    public VideoQuality[] AvailableQualities { get; set; }
}
```

### VideoQuality Enum

```csharp
public enum VideoQuality
{
    Auto,
    Small360,
    Medium480,
    HD720,
    HD1080,
    HD1440,
    HD2160
}
```

## Usage Examples

### Basic Playback
```csharp
// Play a video
await youtubePlayer.PlayVideo("https://www.youtube.com/watch?v=dQw4w9WgXcQ");

// Control playback
youtubePlayer.Pause();
youtubePlayer.Resume();
youtubePlayer.Stop();
```

### Event Handling
```csharp
void Start()
{
    youtubePlayer.OnVideoStarted += () => Debug.Log("Video started!");
    youtubePlayer.OnVideoEnded += () => Debug.Log("Video ended!");
    youtubePlayer.OnError += (error) => Debug.LogError($"Error: {error}");
}
```

### Quality Control
```csharp
// Set preferred quality
youtubePlayer.SetPreferredQuality(VideoQuality.HD720);

// Check available qualities
var qualities = youtubePlayer.GetAvailableQualities();
foreach (var quality in qualities)
{
    Debug.Log($"Available: {quality}");
}
```

### Progress Monitoring
```csharp
void Update()
{
    if (youtubePlayer.IsPlaying)
    {
        float progress = youtubePlayer.GetProgress();
        float currentTime = youtubePlayer.GetCurrentTime();
        float duration = youtubePlayer.GetDuration();
        
        Debug.Log($"Progress: {progress:P0} ({currentTime:F1}s / {duration:F1}s)");
    }
}
```

## Thread Safety

All public methods and properties are thread-safe and can be called from any thread. However, Unity-specific operations will be automatically marshaled to the main thread.

## Performance Notes

- Video loading is asynchronous and won't block the main thread
- Progress events are throttled to avoid excessive updates
- Caching reduces repeated network requests for the same videos
- Memory usage is optimized for mobile and desktop platforms

## See Also

- **[Configuration Classes](configuration.md)**: Player configuration options
- **[Tool Management](tool-management.md)**: External tool handling
- **[Caching System](caching.md)**: URL and video caching
- **[Events & Callbacks](events.md)**: Complete event system reference