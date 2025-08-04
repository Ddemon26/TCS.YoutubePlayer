using System.IO;
using System.Threading;
using TCS.YoutubePlayer.Configuration;
using TCS.YoutubePlayer.ProcessExecution;
using TCS.YoutubePlayer.Subtitles;
using TCS.YoutubePlayer.UrlProcessing;
using UnityEngine.Video;
namespace TCS.YoutubePlayer {
    [RequireComponent( typeof(VideoPlayer) )]
    public class YoutubePlayer : MonoBehaviour {
        public bool m_isAllowedToDownload; // Flag to control download behavior

        [Header( "Configuration" )]
        [SerializeField] string m_profileName = "Default";
        [SerializeField] YtDlpSettings m_currentSettings = new();
        YtDlpProfileManager m_profileManager;

        [SerializeField] string m_title = string.Empty;
        /// <summary>
        /// The title of the currently loaded video, retrieved from cache
        /// </summary>
        public string Title {
            get => m_title;
            private set => m_title = value;
        }

        /// <summary>
        /// Whether the external tools have been successfully initialized
        /// </summary>
        public bool IsInitialized => m_isInitialized;

        /// <summary>
        /// Whether initialization has failed
        /// </summary>
        public bool InitializationFailed => m_initializationFailed;

        public VideoPlayer m_videoPlayer;
        string m_currentVideoUrl; // To store the original URL, we attempted to play
        readonly CancellationTokenSource m_cts = new();

        // Subtitle components
        SubtitleTracker m_subtitleTracker;
        SubtitleBurner m_subtitleBurner;
        YouTubeUrlProcessor m_urlProcessor;

        bool m_isInitialized;
        bool m_initializationFailed;

        async void Awake() {
            try {
                if ( !m_videoPlayer ) {
                    m_videoPlayer = GetComponent<VideoPlayer>();
                    if ( !m_videoPlayer ) {
                        m_videoPlayer = gameObject.AddComponent<VideoPlayer>();
                    }
                }

                if ( m_videoPlayer != null ) {
                    // Subscribe to the error event
                    m_videoPlayer.errorReceived += HandleVideoError;
                    m_videoPlayer.prepareCompleted += VideoPlayerOnPrepareCompleted;
                }
                else {
                    Logger.LogError( "Failed to get or create VideoPlayer component" );
                    return;
                }

                // Initialize configuration system
                InitializeConfiguration();

                // Initialize subtitle components
                InitializeSubtitleComponents();

                // Initialize URL processor
                m_urlProcessor = new YouTubeUrlProcessor();

                // Start initialization asynchronously but don't await in Awake
                await InitializeAsync();
            }
            catch (Exception e) {
                Logger.LogError( $"Failed to initialize VideoPlayer: {e.Message}" );
            }
        }

        Task InitializeAsync() {
            try {
                Logger.Log( "Checking external tool dependencies..." );

                bool ytDlpExists = CheckYtDlpExists();
                bool ffmpegExists = CheckFfmpegExists();

                if ( !ytDlpExists ) {
                    Logger.LogWarning( "yt-dlp is not installed. Please use the TCS Youtube Player editor window to install required dependencies." );
                    m_initializationFailed = true;
                    return Task.CompletedTask;
                }

                if ( !ffmpegExists ) {
                    Logger.LogWarning( "ffmpeg is not installed. MP4 conversion features will be unavailable. Use the TCS Youtube Player editor window to install ffmpeg if needed." );
                }

                Logger.Log( "Dependency check completed." );
                m_isInitialized = true;
            }
            catch (OperationCanceledException) {
                Logger.LogWarning( "Dependency check was cancelled during initialization." );
                m_initializationFailed = true;
            }
            catch (Exception e) {
                Logger.LogError( $"Failed to check dependencies: {e.Message}" );
                m_initializationFailed = true;
            }

            return Task.CompletedTask;
        }

        void InitializeConfiguration() {
            try {
                // Initialize profile manager
                m_profileManager = new YtDlpProfileManager();

                // If settings were set via Inspector, keep them
                if ( m_currentSettings != null ) {
                    Logger.Log( "Using settings configured in Inspector" );
                    return;
                }

                // Otherwise try to load profile
                LoadProfile( m_profileName );
                Logger.Log( $"Loaded YouTube player profile: {m_profileName}" );
            }
            catch (Exception e) {
                Logger.LogWarning( $"Failed to initialize configuration, using defaults: {e.Message}" );
                if ( m_currentSettings == null ) {
                    m_currentSettings = new YtDlpSettings();
                }
            }
        }

        void InitializeSubtitleComponents() {
            try {
                // Get or create SubtitleTracker component for Unity display mode
                m_subtitleTracker = GetComponent<SubtitleTracker>();
                if ( m_subtitleTracker == null ) {
                    m_subtitleTracker = gameObject.AddComponent<SubtitleTracker>();
                }

                // Initialize subtitle burner for FFmpeg burning mode  
                var processExecutor = new ProcessExecutor(LibraryManager.GetFFmpegPath());
                m_subtitleBurner = new SubtitleBurner(processExecutor);

                Logger.Log( "Subtitle components initialized successfully" );
            }
            catch (Exception e) {
                Logger.LogWarning( $"Failed to initialize subtitle components: {e.Message}" );
            }
        }

        /// <summary>
        /// Plays a YouTube video from the specified URL. Supports both streaming and download modes.
        /// </summary>
        /// <param name="url">The YouTube video URL to play</param>
        public async void PlayVideo(string url) {
            try {
                if ( string.IsNullOrEmpty( url ) ) {
                    Logger.LogError( "Video URL is null or empty." );
                    return;
                }

                // Wait for initialization if not complete
                if ( !m_isInitialized && !m_initializationFailed ) {
                    Logger.Log( "Waiting for initialization to complete..." );
                    // Give initialization some time to complete
                    int attempts = 0;
                    while (!m_isInitialized && !m_initializationFailed && attempts < 100) {
                        await Task.Delay( 100, m_cts.Token );
                        attempts++;
                    }

                    if ( !m_isInitialized ) {
                        if ( m_initializationFailed ) {
                            Logger.LogError( "Initialization failed. Required dependencies are missing. Please use the TCS Youtube Player editor window to install them." );
                            return;
                        }

                        Logger.LogWarning( "Initialization timeout, proceeding anyway..." );
                    }
                }

                m_currentVideoUrl = url; // Store the URL we are trying to play
                Logger.Log( $"Attempting to play: {url}" );

                string directUrlAsync = await YtDlpExternalTool.GetDirectUrlAsync( url, m_currentSettings, m_cts.Token );

                if ( string.IsNullOrEmpty( directUrlAsync ) ) {
                    Logger.LogError( "Failed to get direct URL from yt-dlp" );
                    return;
                }

                // Handle subtitle processing based on configuration
                await ProcessSubtitlesAsync( url, directUrlAsync );

                if ( IsMp4Stream( directUrlAsync ) && m_isAllowedToDownload ) {
                    Logger.Log( $"Detected Mp4 stream: {directUrlAsync}" );
                    try {
                        string mp4Path = await YtDlpExternalTool.ConvertToMp4Async( directUrlAsync, m_currentSettings, m_cts.Token );
                        if ( !string.IsNullOrEmpty( mp4Path ) ) {
                            Logger.Log( $"Preemptive conversion successful. Playing local file: {mp4Path}" );
                            m_videoPlayer.source = VideoSource.Url;
                            m_videoPlayer.url = "file://" + mp4Path;
                            m_videoPlayer.Prepare();
                            return;

                        }

                        Logger.LogWarning( $"Preemptive conversion failed or returned an empty path for URL: {directUrlAsync}" );
                    }
                    catch (OperationCanceledException) {
                        Logger.LogWarning( $"MP4 conversion was cancelled for URL: {directUrlAsync}" );
                    }
                    catch (Exception ex) {
                        Logger.LogError( $"Preemptive conversion failed: {ex.Message}" );
                    }
                }

                // Default: Attempt to play the URL directly
                if ( m_videoPlayer != null ) {
                    Title = YtDlpExternalTool.GetCacheTitle( m_currentVideoUrl );
                    m_videoPlayer.source = VideoSource.Url;
                    m_videoPlayer.url = directUrlAsync;
                    m_videoPlayer.Prepare();
                }
                else {
                    Logger.LogError( "VideoPlayer component is null, cannot play video" );
                }
            }
            catch (OperationCanceledException) {
                Logger.LogWarning( $"Video playback was cancelled for URL: {m_currentVideoUrl}" );
            }
            catch (Exception e) when (e.Message.Contains( "Requested format is not available" )) {
                Logger.LogError( $"The requested video format is not available for URL: {m_currentVideoUrl}. Try a different video or format." );
            }
            catch (Exception e) {
                Logger.LogError( $"Exception occurred while preparing video: {e.Message}" );
            }
        }

        static bool IsMp4Stream(string url)
            => !string.IsNullOrEmpty( url )
               && (url.Contains( ".mp4" )
                   || url.Contains( "hls" )
                   || url.Contains( "m3u8" ));

        static void VideoPlayerOnPrepareCompleted(VideoPlayer source) {
            Logger.Log( $"Video prepared. Playing: {source.url}" );
            source.Play();
        }

        void HandleVideoError(VideoPlayer source, string message) {
            Logger.LogError( $"VideoPlayer error: {message}" );
            if ( !string.IsNullOrEmpty( m_currentVideoUrl ) ) {
                Logger.LogError( $"Failed to play video from URL: {m_currentVideoUrl}" );
            }
        }

        bool CheckYtDlpExists() {
            try {
                string ytDlpPath = LibraryManager.GetYtDlpPath();
                return File.Exists( ytDlpPath );
            }
            catch (Exception e) {
                Logger.LogError( $"Error checking yt-dlp path: {e.Message}" );
                return false;
            }
        }

        bool CheckFfmpegExists() {
            try {
                string ffmpegPath = LibraryManager.GetFFmpegPath();
                return File.Exists( ffmpegPath );
            }
            catch (Exception e) {
                Logger.LogError( $"Error checking ffmpeg path: {e.Message}" );
                return false;
            }
        }

        void OnDestroy() {
            try {
                if ( m_videoPlayer ) {
                    m_videoPlayer.errorReceived -= HandleVideoError;
                    m_videoPlayer.prepareCompleted -= VideoPlayerOnPrepareCompleted;

                    // Stop video if playing
                    if ( m_videoPlayer.isPlaying ) {
                        m_videoPlayer.Stop();
                    }
                }

                // Clean up subtitle components
                m_subtitleBurner = null;

                if ( !m_cts.IsCancellationRequested ) {
                    m_cts.Cancel();
                }
            }
            catch (Exception e) {
                Logger.LogError( $"Error during cleanup: {e.Message}" );
            }
            finally {
                m_cts?.Dispose();
            }
        }

        public void LoadProfile(string profileName) {
            if ( m_profileManager == null ) {
                Logger.LogWarning( "Profile manager not initialized, using default settings" );
                m_currentSettings = new YtDlpSettings();
                return;
            }

            var profile = m_profileManager.GetProfile( profileName );
            m_currentSettings = profile?.Settings?.Clone() ?? new YtDlpSettings();
            m_profileName = profileName;
            Logger.Log( $"Loaded profile '{profileName}' for YouTube player" );
        }

        public void SetCustomSettings(YtDlpSettings settings) {
            m_currentSettings = settings?.Clone() ?? new YtDlpSettings();
            m_profileName = "Custom";
            Logger.Log( "Applied custom settings to YouTube player" );
        }

        public YtDlpSettings GetCurrentSettings() => m_currentSettings?.Clone();

        public string GetCurrentProfileName() => m_profileName;

        public void SaveCurrentSettingsAsProfile(string profileName, string description = "") {
            if ( m_profileManager == null || m_currentSettings == null ) return;

            m_profileManager.SaveProfile( profileName, m_currentSettings, description );
            Logger.Log( $"Saved current settings as profile '{profileName}'" );
        }

        async Task ProcessSubtitlesAsync(string videoUrl, string directUrl) {
            try {
                if ( m_currentSettings == null || m_currentSettings.SubtitleFormat == SubtitleFormat.None ) {
                    return; // No subtitles requested
                }

                var handlingMode = m_currentSettings.SubtitleHandlingMode;
                
                // Handle backward compatibility
                if ( handlingMode == SubtitleHandlingMode.None && m_currentSettings.SubtitleFormat != SubtitleFormat.None ) {
                    handlingMode = m_currentSettings.EmbedSubtitles ? SubtitleHandlingMode.EmbedSoft : SubtitleHandlingMode.UnityDisplay;
                }

                if ( handlingMode == SubtitleHandlingMode.None ) {
                    return; // No subtitle processing needed
                }

                Logger.Log( $"Processing subtitles in mode: {handlingMode}" );

                switch (handlingMode) {
                    case SubtitleHandlingMode.UnityDisplay:
                        await ProcessUnityDisplaySubtitlesAsync( videoUrl );
                        break;

                    case SubtitleHandlingMode.BurnHard:
                        await ProcessBurnSubtitlesAsync( videoUrl, directUrl );
                        break;

                    case SubtitleHandlingMode.EmbedSoft:
                        Logger.Log( "Soft embedding subtitles (handled by yt-dlp command generation)" );
                        break;

                    default:
                        Logger.LogWarning( $"Unknown subtitle handling mode: {handlingMode}" );
                        break;
                }
            }
            catch (Exception e) {
                Logger.LogError( $"Error processing subtitles: {e.Message}" );
            }
        }

        async Task ProcessUnityDisplaySubtitlesAsync(string videoUrl) {
            try {
                if ( m_subtitleTracker == null ) {
                    Logger.LogWarning( "SubtitleTracker component not available for Unity display mode" );
                    return;
                }

                Logger.Log( "Searching for subtitle files for Unity display..." );

                // Look for subtitle files that may have been downloaded
                string videoId = m_urlProcessor.TryExtractVideoId( videoUrl );
                if ( string.IsNullOrEmpty( videoId ) ) {
                    Logger.LogWarning( "Could not extract video ID from URL for subtitle search" );
                    return;
                }

                // Search for subtitle files in common locations
                List<string> subtitleFiles = await FindSubtitleFilesAsync( videoId );

                if ( subtitleFiles.Count == 0 ) {
                    Logger.LogWarning( "No subtitle files found for Unity display. Ensure yt-dlp downloads subtitle files." );
                    return;
                }

                // Load the first available subtitle file
                string subtitleFile = subtitleFiles[0];
                Logger.Log( $"Loading subtitle file for Unity display: {subtitleFile}" );

                bool loaded = await m_subtitleTracker.LoadSubtitleFileAsync( subtitleFile );
                if ( loaded ) {
                    Logger.Log( "Subtitle file loaded successfully for Unity display" );
                    // The SubtitleTracker automatically connects to VideoPlayer via Awake() method
                }
                else {
                    Logger.LogError( $"Failed to load subtitle file: {subtitleFile}" );
                }
            }
            catch (Exception e) {
                Logger.LogError( $"Error processing Unity display subtitles: {e.Message}" );
            }
        }

        async Task ProcessBurnSubtitlesAsync(string videoUrl, string directUrl) {
            try {
                if ( m_subtitleBurner == null ) {
                    Logger.LogWarning( "SubtitleBurner not available for burning mode" );
                    return;
                }

                // Check if FFmpeg is available
                if ( !CheckFfmpegExists() ) {
                    Logger.LogWarning( "FFmpeg is not installed. Subtitle burning requires FFmpeg. Please install FFmpeg to use this feature." );
                    return;
                }

                Logger.Log( "Processing subtitle burning with FFmpeg..." );

                string videoId = m_urlProcessor.TryExtractVideoId( videoUrl );
                if ( string.IsNullOrEmpty( videoId ) ) {
                    Logger.LogWarning( "Could not extract video ID from URL for subtitle burning" );
                    return;
                }

                // Search for subtitle files
                List<string> subtitleFiles = await FindSubtitleFilesAsync( videoId );

                if ( subtitleFiles.Count == 0 ) {
                    Logger.LogWarning( "No subtitle files found for burning. Ensure yt-dlp downloads subtitle files." );
                    return;
                }

                string subtitleFile = subtitleFiles[0];
                string outputPath = Path.Combine( Path.GetTempPath(), $"{videoId}_with_subtitles.mp4" );

                Logger.Log( $"Burning subtitles from {subtitleFile} into video..." );

                bool success = await m_subtitleBurner.BurnSubtitlesAsync( 
                    directUrl, 
                    subtitleFile, 
                    outputPath, 
                    m_cts.Token 
                );

                if ( success ) {
                    Logger.Log( $"Subtitle burning completed successfully: {outputPath}" );
                    // Note: The burned video could be used here instead of the original
                }
                else {
                    Logger.LogError( "Subtitle burning failed" );
                }
            }
            catch (Exception e) {
                Logger.LogError( $"Error processing subtitle burning: {e.Message}" );
            }
        }


        async Task<List<string>> FindSubtitleFilesAsync(string videoId) {
            // Get Unity paths on main thread before switching to background thread
            string persistentDataPath = Application.persistentDataPath;
            string temporaryCachePath = Application.temporaryCachePath;
            
            return await Task.Run( () => {
                var subtitleFiles = new List<string>();

                try {
                    // Search in common directories where yt-dlp might save subtitle files
                    string[] searchPaths = {
                        Path.GetTempPath(),
                        Environment.GetFolderPath( Environment.SpecialFolder.Desktop ),
                        Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ),
                        persistentDataPath,
                        temporaryCachePath
                    };

                    string[] extensions = { ".srt", ".vtt", ".ass", ".ssa" };

                    foreach (string searchPath in searchPaths) {
                        if ( !Directory.Exists( searchPath ) ) continue;

                        foreach (string extension in extensions) {
                            string pattern = $"*{videoId}*{extension}";
                            try {
                                string[] files = Directory.GetFiles( searchPath, pattern, SearchOption.TopDirectoryOnly );
                                subtitleFiles.AddRange( files );
                            }
                            catch (Exception e) {
                                Logger.LogWarning( $"Error searching for subtitle files in {searchPath}: {e.Message}" );
                            }
                        }
                    }

                    // Also use the SubtitleParser's find method if we have a video file path
                    // This is a placeholder - in a real scenario, we'd need the actual video file path
                }
                catch (Exception e) {
                    Logger.LogError( $"Error finding subtitle files: {e.Message}" );
                }

                return subtitleFiles;
            } );
        }
    }
}