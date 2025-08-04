using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TCS.YoutubePlayer.Exceptions;
using TCS.YoutubePlayer.ToolManagement;
namespace TCS.YoutubePlayer.Configuration {
    public class YtDlpConfigurationManager : IDisposable {
        readonly ToolDownloadManager m_toolDownloadManager;

        public YtDlpConfigurationManager() {
            m_toolDownloadManager = new ToolDownloadManager();
        }

        public static string GetYtDlpPath() {
            return Application.platform switch {
                RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor
                    => Path.Combine(Application.streamingAssetsPath, "yt-dlp", "Windows", "yt-dlp.exe"),
                RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor
                    => Path.Combine(Application.streamingAssetsPath, "yt-dlp", "macOS", "yt-dlp"),
                RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor
                    => Path.Combine(Application.streamingAssetsPath, "yt-dlp", "Linux", "yt-dlp"),
                _ => throw new NotSupportedException(
                    $"Platform {Application.platform} is not supported for yt-dlp execution."
                ),
            };
        }

        public static string GetFFmpegPath() {
            return Application.platform switch {
                RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor
                    => Path.Combine(Application.streamingAssetsPath, "ffmpeg", "Windows", "bin", "ffmpeg.exe"),
                RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor
                    => Path.Combine(Application.streamingAssetsPath, "ffmpeg", "macOS", "bin", "ffmpeg"),
                RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor
                    => Path.Combine(Application.streamingAssetsPath, "ffmpeg", "Linux", "bin", "ffmpeg"),
                _ => throw new NotSupportedException(
                    $"Platform {Application.platform} is not supported for FFmpeg."
                ),
            };
        }
        
        public async Task<string> EnsureYtDlpAsync(CancellationToken cancellationToken = default)
            => await m_toolDownloadManager.EnsureYtDlpAsync(cancellationToken);

        public async Task<string> EnsureFFmpegAsync(CancellationToken cancellationToken = default)
            => await m_toolDownloadManager.EnsureFFmpegAsync(cancellationToken);
        
        public void Dispose() => m_toolDownloadManager?.Dispose();
    }
}