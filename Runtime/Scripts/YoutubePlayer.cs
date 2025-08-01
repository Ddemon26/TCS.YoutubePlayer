using System.Threading;
using UnityEngine.Video;
using Logger = TCS.YoutubePlayer.Utils.Logger;

namespace TCS.YoutubePlayer {
    [RequireComponent( typeof(VideoPlayer) )]
    public class YoutubePlayer : MonoBehaviour {
        public bool m_isAllowedToDownload; // Flag to control download behavior
        
        [SerializeField] string m_title = string.Empty;
        public string Title {
            get => m_title;
            private set => m_title = value;
        }

        public VideoPlayer m_videoPlayer;
        string m_currentVideoUrl; // To store the original URL, we attempted to play
        readonly CancellationTokenSource m_cts = new();

        async void Awake() {
            try {
                if ( !m_videoPlayer ) {
                    m_videoPlayer = GetComponent<VideoPlayer>();
                    if ( !m_videoPlayer ) {
                        m_videoPlayer = gameObject.AddComponent<VideoPlayer>();
                    }
                }

                // Subscribe to the error event
                m_videoPlayer.errorReceived += HandleVideoError;
                m_videoPlayer.prepareCompleted += VideoPlayerOnPrepareCompleted;
                // Optional: Subscribe to other events as needed
        
                try {
                    Logger.Log("[YoutubePlayer] Initializing external tools...");
                    await YtDlpExternalTool.InitializeToolsAsync(m_cts.Token);
                    Logger.Log("[YoutubePlayer] External tools initialized successfully.");
                    
                    await YtDlpExternalTool.PerformYtDlpUpdateCheckAsync(m_cts.Token);
                }
                catch (OperationCanceledException) {
                    Logger.LogWarning("[YoutubePlayer] Tool initialization or update check was cancelled during initialization.");
                }
                catch (Exception e) {
                    Logger.LogError($"[YoutubePlayer] Failed to initialize tools or check for updates: {e.Message}");
                }
            }
            catch (Exception e) {
                Logger.LogError($"[YoutubePlayer] Failed to initialize VideoPlayer: {e.Message}");
            }
        }

        public async void PlayVideo(string url) {
            try {
                if ( string.IsNullOrEmpty( url ) ) {
                    Logger.LogError( "Video URL is null or empty." );
                    return;
                }

                m_currentVideoUrl = url; // Store the URL we are trying to play
                Logger.Log($"[YoutubePlayer] Attempting to play: {url}");

                string directUrlAsync = await YtDlpExternalTool.GetDirectUrlAsync( url, m_cts.Token ); 

                if ( IsMp4Stream( directUrlAsync ) ) {
                    if ( !m_isAllowedToDownload ) {
                        Logger.LogWarning($"[YoutubePlayer] Download is not allowed. Playing the URL directly: {directUrlAsync}");
                        return;
                    }
                
                
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
            if (m_currentVideoUrl != null) {
                Logger.LogError($"[YoutubePlayer] Failed to play video from URL: {m_currentVideoUrl}");
            }
        }

        void OnDestroy() {
            if ( m_videoPlayer ) {
                m_videoPlayer.errorReceived -= HandleVideoError;
                m_videoPlayer.prepareCompleted -= VideoPlayerOnPrepareCompleted;
            }

            m_cts.Cancel();
            m_cts.Dispose();
        }
    }
}