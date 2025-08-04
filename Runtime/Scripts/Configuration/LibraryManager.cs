using System.IO;
using System.Threading;
using TCS.YoutubePlayer.ToolManagement;
using TCS.YoutubePlayer.Utils;
namespace TCS.YoutubePlayer.Configuration {
    public class LibraryManager : IDisposable {
        readonly ToolDownloadManager m_toolDownloadManager;

        public LibraryManager() {
            m_toolDownloadManager = new ToolDownloadManager();
        }

        public static string GetLibraryPath(LibraryType libraryType) => PlatformPathResolver.GetLibraryPath( libraryType );
        public static string GetYtDlpPath() => PlatformPathResolver.GetYtDlpPath();
        public static string GetFFmpegPath() => PlatformPathResolver.GetFFmpegPath();

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