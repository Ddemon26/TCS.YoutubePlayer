/*using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TestYouTube;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent( typeof(VideoPlayer) )]
public class YoutubeDownloadPlayerAsync : MonoBehaviour {
    [Tooltip( "Any YouTube link" )]
    public string m_youtubeUrl;

    [Tooltip( "Auto-play after download?" )]
    public bool m_playAfterDownload = true;

    string m_localMp4;
    VideoPlayer m_player;
    
    readonly CancellationTokenSource m_cts = new();

    void Awake() {
#if UNITY_EDITOR
        m_localMp4 = Path.Combine( Application.dataPath, "Downloads", "downloaded.mp4" );
#else
    m_localMp4 = Path.Combine(Application.persistentDataPath, "downloaded.mp4");
#endif
    }
    async void Start() {
        try {
            if ( string.IsNullOrWhiteSpace( m_youtubeUrl ) ) {
                Debug.LogError( "YoutubeDownloadPlayerAsync: youtubeUrl is empty" );
                return;
            }

            m_player = GetComponent<VideoPlayer>();
            m_player.playOnAwake = false;

            string args =
                "--merge-output-format mp4 " +
                "-f \"bv*[height<=1080]+ba[acodec=aac]/best\" " +
                $"-o \"{m_localMp4}\" \"{m_youtubeUrl}\"";

            (int code, _, string err) = await YtDlpExternalTool.RunProcessAsync( args, m_cts.Token );
            if ( code != 0 || !File.Exists( m_localMp4 ) ) {
                Debug.LogError( $"yt-dlp download failed ({code}):\n{err}" );
                return;
            }

            m_player.source = VideoSource.Url;
            m_player.url = "file:///" + m_localMp4.Replace( '\\', '/' );
            
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            #endif

            if ( m_playAfterDownload ) {
                m_player.Prepare();
                while (!m_player.isPrepared) {
                    await Task.Yield();
                }

                m_player.Play();
            }
        }
        catch (Exception e) {
            Debug.LogError( $"YoutubeDownloadPlayerAsync: Exception occurred while downloading or playing video: {e.Message}" );
            Debug.LogError( e.StackTrace );
        }
    }
    
    void OnDestroy() {
        m_cts?.Cancel();
        m_cts?.Dispose();
        //m_cts = null;

        if ( m_player ) {
            m_player.Stop();
            m_player.url = null; // Clear the URL to release resources
        }

        if ( File.Exists( m_localMp4 ) ) {
            try {
                File.Delete( m_localMp4 );
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to delete local video file: {e.Message}" );
            }
        }
    }
}*/