# Basic Video Player Example

This example demonstrates how to create a simple YouTube video player with basic controls using TCS.YoutubePlayer.

## Complete Example

Here's a complete script that creates a functional YouTube video player:

```csharp
using UnityEngine;
using UnityEngine.UI;
using TCS.YoutubePlayer;
using System.Collections;

public class BasicYoutubePlayer : MonoBehaviour
{
    [Header("Player Components")]
    [SerializeField] private YoutubePlayer youtubePlayer;
    
    [Header("UI Controls")]
    [SerializeField] private InputField urlInput;
    [SerializeField] private Button playButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Text statusText;
    [SerializeField] private Text timeText;
    
    [Header("Settings")]
    [SerializeField] private string defaultUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
    
    private bool isUpdatingProgress = false;
    
    void Start()
    {
        SetupUI();
        SetupPlayerEvents();
        
        // Set default URL
        if (urlInput != null)
            urlInput.text = defaultUrl;
    }
    
    void SetupUI()
    {
        // Setup button events
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
            
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseClicked);
            
        if (stopButton != null)
            stopButton.onClick.AddListener(OnStopClicked);
        
        // Setup sliders
        if (progressSlider != null)
        {
            progressSlider.onValueChanged.AddListener(OnProgressChanged);
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
        }
        
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = 1f; // Default volume
        }
        
        UpdateButtonStates(false);
        UpdateStatus("Ready");
    }
    
    void SetupPlayerEvents()
    {
        if (youtubePlayer == null) return;
        
        youtubePlayer.OnVideoStarted += OnVideoStarted;
        youtubePlayer.OnVideoEnded += OnVideoEnded;
        youtubePlayer.OnVideoPaused += OnVideoPaused;
        youtubePlayer.OnVideoResumed += OnVideoResumed;
        youtubePlayer.OnError += OnPlayerError;
        youtubePlayer.OnProgressChanged += OnPlayerProgressChanged;
    }
    
    #region Button Events
    
    public void OnPlayClicked()
    {
        if (youtubePlayer == null || urlInput == null) return;
        
        string url = urlInput.text.Trim();
        if (string.IsNullOrEmpty(url))
        {
            UpdateStatus("Please enter a YouTube URL");
            return;
        }
        
        UpdateStatus("Loading video...");
        youtubePlayer.PlayVideo(url);
    }
    
    public void OnPauseClicked()
    {
        if (youtubePlayer == null) return;
        
        if (youtubePlayer.IsPlaying)
            youtubePlayer.Pause();
        else
            youtubePlayer.Resume();
    }
    
    public void OnStopClicked()
    {
        if (youtubePlayer == null) return;
        
        youtubePlayer.Stop();
    }
    
    #endregion
    
    #region Slider Events
    
    public void OnProgressChanged(float value)
    {
        if (youtubePlayer == null || isUpdatingProgress) return;
        
        float duration = youtubePlayer.GetDuration();
        if (duration > 0)
        {
            float targetTime = value * duration;
            youtubePlayer.SeekTo(targetTime);
        }
    }
    
    public void OnVolumeChanged(float value)
    {
        if (youtubePlayer == null) return;
        
        youtubePlayer.SetVolume(value);
    }
    
    #endregion
    
    #region Player Events
    
    private void OnVideoStarted()
    {
        UpdateStatus("Playing");
        UpdateButtonStates(true);
        
        // Start progress updates
        StartCoroutine(UpdateProgressCoroutine());
    }
    
    private void OnVideoEnded()
    {
        UpdateStatus("Video ended");
        UpdateButtonStates(false);
        
        if (progressSlider != null)
            progressSlider.value = 0f;
    }
    
    private void OnVideoPaused()
    {
        UpdateStatus("Paused");
        
        if (pauseButton != null)
        {
            var buttonText = pauseButton.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = "Resume";
        }
    }
    
    private void OnVideoResumed()
    {
        UpdateStatus("Playing");
        
        if (pauseButton != null)
        {
            var buttonText = pauseButton.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = "Pause";
        }
    }
    
    private void OnPlayerError(string error)
    {
        UpdateStatus($"Error: {error}");
        UpdateButtonStates(false);
        Debug.LogError($"YouTube Player Error: {error}");
    }
    
    private void OnPlayerProgressChanged(float progress)
    {
        // This is handled by the coroutine to avoid conflicts
    }
    
    #endregion
    
    #region UI Updates
    
    private void UpdateButtonStates(bool isPlaying)
    {
        if (playButton != null)
            playButton.interactable = !isPlaying;
            
        if (pauseButton != null)
            pauseButton.interactable = isPlaying;
            
        if (stopButton != null)
            stopButton.interactable = isPlaying;
    }
    
    private void UpdateStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
            
        Debug.Log($"Player Status: {status}");
    }
    
    private void UpdateTimeDisplay(float currentTime, float duration)
    {
        if (timeText == null) return;
        
        string current = FormatTime(currentTime);
        string total = FormatTime(duration);
        timeText.text = $"{current} / {total}";
    }
    
    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }
    
    #endregion
    
    #region Progress Updates
    
    private IEnumerator UpdateProgressCoroutine()
    {
        while (youtubePlayer != null && youtubePlayer.IsPlaying)
        {
            float currentTime = youtubePlayer.GetCurrentTime();
            float duration = youtubePlayer.GetDuration();
            
            if (duration > 0)
            {
                isUpdatingProgress = true;
                
                if (progressSlider != null)
                    progressSlider.value = currentTime / duration;
                
                UpdateTimeDisplay(currentTime, duration);
                
                isUpdatingProgress = false;
            }
            
            yield return new WaitForSeconds(0.1f); // Update 10 times per second
        }
    }
    
    #endregion
    
    void OnDestroy()
    {
        // Clean up events
        if (youtubePlayer != null)
        {
            youtubePlayer.OnVideoStarted -= OnVideoStarted;
            youtubePlayer.OnVideoEnded -= OnVideoEnded;
            youtubePlayer.OnVideoPaused -= OnVideoPaused;
            youtubePlayer.OnVideoResumed -= OnVideoResumed;
            youtubePlayer.OnError -= OnPlayerError;
            youtubePlayer.OnProgressChanged -= OnPlayerProgressChanged;
        }
    }
}
```

## UI Setup

Create a Canvas with the following UI elements:

### Input Field (URL Input)
- **Component**: InputField
- **Placeholder**: "Enter YouTube URL..."
- **Content Type**: Standard

### Buttons
- **Play Button**: Triggers video playback
- **Pause Button**: Pauses/resumes playback  
- **Stop Button**: Stops playback completely

### Sliders
- **Progress Slider**: Shows and controls playback progress
- **Volume Slider**: Controls audio volume (0-1)

### Text Elements
- **Status Text**: Shows current player status
- **Time Text**: Shows current time / total duration

## Scene Setup

1. **Create Canvas**
   ```
   Canvas
   ├── URL Input (InputField)
   ├── Control Panel
   │   ├── Play Button
   │   ├── Pause Button
   │   └── Stop Button
   ├── Progress Panel
   │   ├── Progress Slider
   │   └── Time Text
   ├── Volume Panel
   │   └── Volume Slider
   ├── Status Text
   └── YoutubePlayer (Prefab)
   ```

2. **Assign References**
   - Drag UI elements to the script's serialized fields
   - Ensure YoutubePlayer prefab is assigned

## Advanced Features

### Quality Selection

Add quality selection dropdown:

```csharp
[SerializeField] private Dropdown qualityDropdown;

void SetupQualityDropdown()
{
    if (qualityDropdown == null) return;
    
    qualityDropdown.options.Clear();
    qualityDropdown.options.Add(new Dropdown.OptionData("Auto"));
    qualityDropdown.options.Add(new Dropdown.OptionData("1080p"));
    qualityDropdown.options.Add(new Dropdown.OptionData("720p"));
    qualityDropdown.options.Add(new Dropdown.OptionData("480p"));
    qualityDropdown.options.Add(new Dropdown.OptionData("360p"));
    
    qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
}

void OnQualityChanged(int index)
{
    VideoQuality quality = VideoQuality.Auto;
    
    switch (index)
    {
        case 1: quality = VideoQuality.HD1080; break;
        case 2: quality = VideoQuality.HD720; break;
        case 3: quality = VideoQuality.Medium480; break;
        case 4: quality = VideoQuality.Small360; break;
    }
    
    youtubePlayer.SetPreferredQuality(quality);
}
```

### Playlist Support

Add simple playlist functionality:

```csharp
[SerializeField] private string[] playlist;
private int currentVideoIndex = 0;

void PlayNextVideo()
{
    if (playlist == null || playlist.Length == 0) return;
    
    currentVideoIndex = (currentVideoIndex + 1) % playlist.Length;
    youtubePlayer.PlayVideo(playlist[currentVideoIndex]);
}

void PlayPreviousVideo()
{
    if (playlist == null || playlist.Length == 0) return;
    
    currentVideoIndex = (currentVideoIndex - 1 + playlist.Length) % playlist.Length;
    youtubePlayer.PlayVideo(playlist[currentVideoIndex]);
}
```

## Troubleshooting

### Common Issues

**Video Not Loading**
- Check console for error messages
- Verify YouTube URL is valid and accessible
- Ensure internet connection is active

**UI Not Responding**
- Verify all UI references are assigned in inspector
- Check that buttons have proper event listeners
- Ensure Canvas is set up correctly

**Progress Slider Jumping**
- The `isUpdatingProgress` flag prevents conflicts
- Make sure coroutine is properly managed

## Next Steps

- **[Custom UI Example](custom-ui.md)**: Create custom player interface
- **[Playlist Player](playlist.md)**: Build a full playlist system
- **[Advanced Integration](advanced.md)**: Complex integration scenarios

This basic player provides a solid foundation for YouTube video playback in Unity. You can extend it with additional features like playlists, custom controls, or integration with your game's UI system.