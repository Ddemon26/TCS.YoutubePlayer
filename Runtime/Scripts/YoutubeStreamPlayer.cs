/*using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TestYouTube;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent( typeof(VideoPlayer) )]
public class YoutubeStreamPlayerAsync : MonoBehaviour {
    VideoPlayer m_player;
    
    async void Awake() {
        try {
            m_player = GetComponent<VideoPlayer>();
            m_player.playOnAwake = false;
            
            try {
                await YtDlpExternalTool.PerformYtDlpUpdateCheckAsync();
            }
            catch (Exception e) {
                Debug.LogError( $"YoutubeStreamPlayerAsync: {e.Message}" );
            }
            
        }
        catch (Exception e) {
            Debug.LogError( $"YoutubeStreamPlayerAsync: Failed to initialize VideoPlayer - {e.Message}" );
        }
    }
    
    
    public async void PlayUrl(string youtubeUrl) {
        
        try {
            if ( string.IsNullOrWhiteSpace( youtubeUrl ) ) {
                Debug.LogError( "YoutubeStreamPlayerAsync: youtubeUrl is empty" );
                return;
            }

            string direct;
            try {
                direct = await YtDlpExternalTool.GetDirectUrlAsync( youtubeUrl );
            }
            catch (Exception ex) {
                Debug.LogError( ex.Message );
                return;
            }

            m_player.source = VideoSource.Url;
            m_player.url = direct;
            m_player.Prepare();

            // wait asynchronously until the first frame is ready
            while (!m_player.isPrepared) {
                await Task.Yield();
            }

            m_player.Play();
        }
        catch (Exception e) {
            Debug.LogError( $"YoutubeStreamPlayerAsync: {e.Message}" );
        }
    }
    
    public async void PlayUrlConvertVideoAsync(string youtubeUrl) {
        try {
            if (string.IsNullOrWhiteSpace(youtubeUrl)) {
                Debug.LogError("YoutubeStreamPlayerAsync: youtubeUrl is empty");
                return;
            }
            // Get the direct video URL (might be HLS)  
            string directUrl = await YtDlpExternalTool.GetDirectUrlAsync(youtubeUrl);
            // // Prepare an output file path within persistent data
            // string outputFilePath = Path.Combine(Application.persistentDataPath, "Streaming", "converted_video.mp4");
            //
            // // Delete an existing converted file before starting a new conversion
            // if (File.Exists(outputFilePath)) {
            //     File.Delete(outputFilePath);
            // }

            // Run FFmpeg to convert HLS to MP4
            string finalMp4Path = await YtDlpExternalTool.ConvertToMp4Async(directUrl/*, outputFilePath#1#);
        
            // Set the VideoPlayer to play the converted MP4 file
            m_player.source = VideoSource.Url;
            // Ensure to use a file URI scheme for local files
            m_player.url = $"file://{finalMp4Path}";
            m_player.Prepare();
            // Wait asynchronously until the first frame is ready
            while (!m_player.isPrepared) {
                await Task.Yield();
            }
            m_player.Play();
        }
        catch (Exception e) {
            Debug.LogError($"YoutubeStreamPlayerAsync: {e.Message}");
        }
    }
}*/