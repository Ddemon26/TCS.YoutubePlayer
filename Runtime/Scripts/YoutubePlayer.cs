using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;
using Logger = TCS.YoutubePlayer.Utils.Logger;

namespace TCS.YoutubePlayer {
    [RequireComponent( typeof(VideoPlayer) )]
    public class YoutubePlayer : MonoBehaviour {
        public bool m_isAllowedToDownload; // Flag to control download behavior
        
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

        bool m_isInitialized = false;
        bool m_initializationFailed = false;

        void Awake() {
            try {
                if ( !m_videoPlayer ) {
                    m_videoPlayer = GetComponent<VideoPlayer>();
                    if ( !m_videoPlayer ) {
                        m_videoPlayer = gameObject.AddComponent<VideoPlayer>();
                    }
                }

                if (m_videoPlayer != null) {
                    // Subscribe to the error event
                    m_videoPlayer.errorReceived += HandleVideoError;
                    m_videoPlayer.prepareCompleted += VideoPlayerOnPrepareCompleted;
                } else {
                    Logger.LogError("[YoutubePlayer] Failed to get or create VideoPlayer component");
                    return;
                }
                
                // Start initialization asynchronously but don't await in Awake
                _ = InitializeAsync();
            }
            catch (Exception e) {
                Logger.LogError($"[YoutubePlayer] Failed to initialize VideoPlayer: {e.Message}");
            }
        }

        async Task InitializeAsync() {
            try {
                Logger.Log("[YoutubePlayer] Initializing external tools...");
                await YtDlpExternalTool.InitializeToolsAsync(m_cts.Token);
                Logger.Log("[YoutubePlayer] External tools initialized successfully.");
                
                await YtDlpExternalTool.PerformYtDlpUpdateCheckAsync(m_cts.Token);
                m_isInitialized = true;
                Logger.Log("[YoutubePlayer] Initialization completed successfully.");
            }
            catch (OperationCanceledException) {
                Logger.LogWarning("[YoutubePlayer] Tool initialization or update check was cancelled during initialization.");
                m_initializationFailed = true;
            }
            catch (Exception e) {
                Logger.LogError($"[YoutubePlayer] Failed to initialize tools or check for updates: {e.Message}");
                m_initializationFailed = true;
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
                if (!m_isInitialized && !m_initializationFailed) {
                    Logger.Log("[YoutubePlayer] Waiting for initialization to complete...");
                    // Give initialization some time to complete
                    int attempts = 0;
                    while (!m_isInitialized && !m_initializationFailed && attempts < 100) {
                        await Task.Delay(100, m_cts.Token);
                        attempts++;
                    }
                    
                    if (!m_isInitialized) {
                        if (m_initializationFailed) {
                            Logger.LogError("[YoutubePlayer] Initialization failed. Cannot play video.");
                            return;
                        } else {
                            Logger.LogWarning("[YoutubePlayer] Initialization timeout, proceeding anyway...");
                        }
                    }
                }

                m_currentVideoUrl = url; // Store the URL we are trying to play
                Logger.Log($"[YoutubePlayer] Attempting to play: {url}");

                string directUrlAsync = await YtDlpExternalTool.GetDirectUrlAsync( url, m_cts.Token ); 

                if (string.IsNullOrEmpty(directUrlAsync)) {
                    Logger.LogError("[YoutubePlayer] Failed to get direct URL from yt-dlp");
                    return;
                }

                if ( IsMp4Stream( directUrlAsync ) && m_isAllowedToDownload ) {
                    Logger.Log($"[YoutubePlayer] Detected Mp4 stream: {directUrlAsync}");
                    try {
                        string mp4Path = await YtDlpExternalTool.ConvertToMp4Async( directUrlAsync, m_cts.Token );
                        if ( !string.IsNullOrEmpty( mp4Path ) ) {
                            Logger.Log($"[YoutubePlayer] Preemptive conversion successful. Playing local file: {mp4Path}");
                            m_videoPlayer.source = VideoSource.Url;
                            m_videoPlayer.url = "file://" + mp4Path;
                            m_videoPlayer.Prepare();
                            return;
                        
                        }

                        Logger.LogWarning($"[YoutubePlayer] Preemptive conversion failed or returned an empty path for URL: {directUrlAsync}");
                    }
                    catch (OperationCanceledException) {
                        Logger.LogWarning($"[YoutubePlayer] MP4 conversion was cancelled for URL: {directUrlAsync}");
                    }
                    catch (Exception ex) {
                        Logger.LogError($"[YoutubePlayer] Preemptive conversion failed: {ex.Message}");
                    }
                }

                // Default: Attempt to play the URL directly
                Title = YtDlpExternalTool.GetCacheTitle( m_currentVideoUrl );
                m_videoPlayer.source = VideoSource.Url;
                m_videoPlayer.url = directUrlAsync;
                m_videoPlayer.Prepare();
            }
            catch (OperationCanceledException) {
                Logger.LogWarning($"[YoutubePlayer] Video playback was cancelled for URL: {m_currentVideoUrl}");
            }
            catch (Exception e) when (e.Message.Contains("Requested format is not available")) {
                Logger.LogError($"[YoutubePlayer] The requested video format is not available for URL: {m_currentVideoUrl}. Try a different video or format.");
            }
            catch (Exception e) {
                Logger.LogError($"[YoutubePlayer] Exception occurred while preparing video: {e.Message}");
            }
        }

        static bool IsMp4Stream(string url)
            => !string.IsNullOrEmpty( url )
               && (url.Contains( ".mp4" )
                   || url.Contains( "hls" )
                   || url.Contains( "m3u8" ));

        static void VideoPlayerOnPrepareCompleted(VideoPlayer source) {
            Logger.Log($"[YoutubePlayer] Video prepared. Playing: {source.url}");
            source.Play();
        }

        void HandleVideoError(VideoPlayer source, string message) {
            Logger.LogError($"[YoutubePlayer] VideoPlayer error: {message}");
            if (!string.IsNullOrEmpty(m_currentVideoUrl)) {
                Logger.LogError($"[YoutubePlayer] Failed to play video from URL: {m_currentVideoUrl}");
            }
        }

        void OnDestroy() {
            try {
                if ( m_videoPlayer ) {
                    m_videoPlayer.errorReceived -= HandleVideoError;
                    m_videoPlayer.prepareCompleted -= VideoPlayerOnPrepareCompleted;
                    
                    // Stop video if playing
                    if (m_videoPlayer.isPlaying) {
                        m_videoPlayer.Stop();
                    }
                }

                if (!m_cts.IsCancellationRequested) {
                    m_cts.Cancel();
                }
            }
            catch (Exception e) {
                Logger.LogError($"[YoutubePlayer] Error during cleanup: {e.Message}");
            }
            finally {
                m_cts?.Dispose();
            }
        }
    }
}