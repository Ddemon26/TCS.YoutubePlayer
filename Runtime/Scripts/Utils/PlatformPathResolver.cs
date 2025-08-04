using System.IO;
using TCS.YoutubePlayer.ToolManagement;

namespace TCS.YoutubePlayer.Utils {
    public static class PlatformPathResolver {
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

        public static string GetLibraryPath(LibraryType libraryType) {
            return libraryType switch {
                LibraryType.YtDlp => GetYtDlpPath(),
                LibraryType.FFmpeg => GetFFmpegPath(),
                _ => throw new NotSupportedException( $"Library type {libraryType} is not supported." ),
            };
        }

        public static bool CheckLibraryExists(LibraryType libraryType) {
            try {
                string libraryPath = GetLibraryPath( libraryType );
                return File.Exists( libraryPath );
            }
            catch (Exception e) {
                Logger.LogError( $"Error checking {libraryType} path: {e.Message}" );
                return false;
            }
        }
    }
}