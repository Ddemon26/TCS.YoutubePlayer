using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Video;

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
                    await YtDlpExternalTool.PerformYtDlpUpdateCheckAsync(m_cts.Token);
                }
                catch (Exception e) {
                    Debug.LogError( $"YoutubeStreamPlayerAsync: {e.Message}" );
                }
            }
            catch (Exception e) {
                Debug.LogError( $"[CustomVideoPlayer] Failed to initialize VideoPlayer: {e.Message}" );
                // Optionally, you can handle specific exceptions or fallback logic here
            }
        }

        public async void PlayVideo(string url) {
            try {
                if ( string.IsNullOrEmpty( url ) ) {
                    Debug.LogError( "Video URL is null or empty." );
                    return;
                }

                m_currentVideoUrl = url; // Store the URL we are trying to play
                Debug.Log( $"[CustomVideoPlayer] Attempting to play: {url}" );

                string directUrlAsync = await YtDlpExternalTool.GetDirectUrlAsync( url, m_cts.Token ); 

                if ( IsMp4Stream( directUrlAsync ) ) {
                    if ( !m_isAllowedToDownload ) {
                        Debug.LogWarning( $"[CustomVideoPlayer] Download is not allowed. Playing the URL directly: {directUrlAsync}" );
                        return;
                    }
                
                
                    Debug.Log( $"[CustomVideoPlayer] Detected Mp4 stream: {directUrlAsync}" );
                    try {
                        string mp4Path = await YtDlpExternalTool.ConvertToMp4Async( directUrlAsync, m_cts.Token );
                        if ( !string.IsNullOrEmpty( mp4Path ) ) {
                            Debug.Log( $"[CustomVideoPlayer] Preemptive conversion successful. Playing local file: {mp4Path}" );
                            m_videoPlayer.source = VideoSource.Url;
                            m_videoPlayer.url = "file://" + mp4Path;
                            m_videoPlayer.Prepare();
                            return;
                        
                        }

                        Debug.LogWarning( $"[CustomVideoPlayer] Preemptive conversion failed or returned an empty path for URL: {directUrlAsync}" );
                    }
                    catch (Exception ex) {
                        Debug.LogError( $"[CustomVideoPlayer] Preemptive conversion failed: {ex.Message}" );
                    }
                }

                // Default: Attempt to play the URL directly
                Title = YtDlpExternalTool.GetCacheTitle( m_currentVideoUrl );
                m_videoPlayer.source = VideoSource.Url;
                m_videoPlayer.url = directUrlAsync;
                m_videoPlayer.Prepare();
            }
            catch (Exception e) {
                Debug.LogError( $"[CustomVideoPlayer] Exception occurred while preparing video: {e.Message}" );
                // Optionally, you can handle specific exceptions or fallback logic here
                if ( e.Message.Contains( "Requested format is not available" ) ) {
                    Debug.LogError( $"[CustomVideoPlayer] The requested video format is not available for URL: {m_currentVideoUrl}. Try a different video or format." );
                    // Optionally, implement fallback logic here, e.g., try fetching a list of available formats
                    // or inform the user more directly.
                }
            }
        }

        static bool IsMp4Stream(string url)
            => !string.IsNullOrEmpty( url )
               && (url.Contains( ".mp4" )
                   || url.Contains( "hls" )
                   || url.Contains( "m3u8" ));

        static void VideoPlayerOnPrepareCompleted(VideoPlayer source) {
            Debug.Log( $"[CustomVideoPlayer] Video prepared. Playing: {source.url}" );
            source.Play();
        }

        void HandleVideoError(VideoPlayer source, string message) {
            Debug.LogError( $"[CustomVideoPlayer] VideoPlayer error: {message}" );
            if ( m_currentVideoUrl != null ) {
                Debug.LogError( $"[CustomVideoPlayer] Failed to play video from URL: {m_currentVideoUrl}" );
            }
            // Optionally, you can implement fallback logic or user notifications here
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