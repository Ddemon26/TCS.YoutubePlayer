using System.IO;
using System.Threading;
using TCS.YoutubePlayer.ToolManagement;
namespace TCS.YoutubePlayer.Configuration {
    public class LibraryManager : IDisposable {
        readonly ToolDownloadManager m_toolDownloadManager;

        public LibraryManager() {
            m_toolDownloadManager = new ToolDownloadManager();
        }

        public static string GetLibraryPath(LibraryType libraryType) {
            return libraryType switch {
                LibraryType.YtDlp => GetYtDlpPath(),
                LibraryType.FFmpeg => GetFFmpegPath(),
                _ => throw new NotSupportedException( $"Library type {libraryType} is not supported." ),
            };
        }

        public static string GetYtDlpPath() {
            return Application.platform switch {
                RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor
                    => Path.Combine( Application.streamingAssetsPath, "yt-dlp", "Windows", "yt-dlp.exe" ),
                RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor
                    => Path.Combine( Application.streamingAssetsPath, "yt-dlp", "macOS", "yt-dlp" ),
                RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor
                    => Path.Combine( Application.streamingAssetsPath, "yt-dlp", "Linux", "yt-dlp" ),
                _ => throw new NotSupportedException(
                    $"Platform {Application.platform} is not supported for yt-dlp execution."
                ),
            };
        }

        public static string GetFFmpegPath() {
            return Application.platform switch {
                RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor
                    => Path.Combine( Application.streamingAssetsPath, "ffmpeg", "Windows", "bin", "ffmpeg.exe" ),
                RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor
                    => Path.Combine( Application.streamingAssetsPath, "ffmpeg", "macOS", "bin", "ffmpeg" ),
                RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor
                    => Path.Combine( Application.streamingAssetsPath, "ffmpeg", "Linux", "bin", "ffmpeg" ),
                _ => throw new NotSupportedException(
                    $"Platform {Application.platform} is not supported for FFmpeg."
                ),
            };
        }

        public async Task<string> EnsureLibraryAsync(LibraryType libraryType, CancellationToken cancellationToken = default) {
            return libraryType switch {
                LibraryType.YtDlp => await m_toolDownloadManager.EnsureYtDlpAsync( cancellationToken ),
                LibraryType.FFmpeg => await m_toolDownloadManager.EnsureFFmpegAsync( cancellationToken ),
                _ => throw new NotSupportedException( $"Library type {libraryType} is not supported." ),
            };
        }

        public async Task<string> EnsureYtDlpAsync(CancellationToken cancellationToken = default)
            => await m_toolDownloadManager.EnsureYtDlpAsync( cancellationToken );

        public async Task<string> EnsureFFmpegAsync(CancellationToken cancellationToken = default)
            => await m_toolDownloadManager.EnsureFFmpegAsync( cancellationToken );

        public void Dispose() => m_toolDownloadManager?.Dispose();
    }
}