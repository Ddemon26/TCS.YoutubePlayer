using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TCS.YoutubePlayer.Exceptions;

namespace TCS.YoutubePlayer.ToolManagement {
    public enum LibraryType {
        YtDlp,
        FFmpeg,
    }
    
    public class ToolDownloadManager : IDisposable {
        readonly HttpClient m_httpClient;
        readonly string m_toolsDirectory;

        const string YT_DLP_WINDOWS_URL = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
        const string FFMPEG_WINDOWS_URL = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
        const string TOOL_VERSION_FILE = "tool_versions.json";

        public ToolDownloadManager() {
            m_httpClient = new HttpClient();
            m_httpClient.DefaultRequestHeaders.Add( "User-Agent", "TCS.YoutubePlayer/1.0" );
            m_toolsDirectory = Application.streamingAssetsPath;
            Directory.CreateDirectory( m_toolsDirectory );
        }
        
        public string LibraryDirectory(LibraryType libraryType) {
            return libraryType switch {
                LibraryType.YtDlp => Path.Combine(m_toolsDirectory, "yt-dlp"),
                LibraryType.FFmpeg => Path.Combine(m_toolsDirectory, "ffmpeg"),
                _ => throw new NotSupportedException($"Library type {libraryType} is not supported."),
            };
        }

        public bool ShouldUpdateTool(string toolName, string currentVersion = null) {
            string versionFilePath = Path.Combine( m_toolsDirectory, TOOL_VERSION_FILE );

            if ( !File.Exists( versionFilePath ) ) {
                return true; // No version info, assume update needed
            }

            try {
                string json = File.ReadAllText( versionFilePath );
                Dictionary<string, ToolVersionInfo> versionInfo = JsonConvert.DeserializeObject<Dictionary<string, ToolVersionInfo>>( json );

                if ( !versionInfo.TryGetValue( toolName, out var toolInfo ) ) {
                    return true; // Tool is not tracked, update needed
                }

                // Check if tool was downloaded more than 7 days ago
                if ( DateTime.UtcNow - toolInfo.DownloadedAt > TimeSpan.FromDays( 7 ) ) {
                    Logger.Log( $"{toolName} is older than 7 days, considering update" );
                    return true;
                }

                return false;
            }
            catch (Exception ex) {
                Logger.LogWarning( $"Failed to read version info: {ex.Message}" );
                return true;
            }
        }

        void UpdateToolVersion(string toolName, string version, string filePath) {
            string versionFilePath = Path.Combine( m_toolsDirectory, TOOL_VERSION_FILE );

            try {
                Dictionary<string, ToolVersionInfo> versionInfo;

                if ( File.Exists( versionFilePath ) ) {
                    string json = File.ReadAllText( versionFilePath );
                    versionInfo = JsonConvert.DeserializeObject<Dictionary<string, ToolVersionInfo>>( json ) ?? new Dictionary<string, ToolVersionInfo>();
                }
                else {
                    versionInfo = new Dictionary<string, ToolVersionInfo>();
                }

                versionInfo[toolName] = new ToolVersionInfo {
                    Version = version ?? "unknown",
                    FilePath = filePath,
                    DownloadedAt = DateTime.UtcNow,
                };

                string updatedJson = JsonConvert.SerializeObject( versionInfo, Formatting.Indented );
                File.WriteAllText( versionFilePath, updatedJson );

                Logger.Log( $"Updated version info for {toolName}: {version}" );
            }
            catch (Exception ex) {
                Logger.LogWarning( $"Failed to update version info: {ex.Message}" );
            }
        }

        public async Task<string> EnsureYtDlpAsync(CancellationToken cancellationToken = default) {
            string ytDlpPath = GetYtDlpPath();

            if ( File.Exists( ytDlpPath ) ) {
                Logger.Log( $"yt-dlp already exists at: {ytDlpPath}" );
                return ytDlpPath;
            }

            Logger.Log( "Downloading yt-dlp..." );

            // Only download for Windows platforms for now
            if ( Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor ) {
                await DownloadFileAsync( YT_DLP_WINDOWS_URL, ytDlpPath, cancellationToken );
                UpdateToolVersion( "yt-dlp", "latest", ytDlpPath );
                Logger.Log( $"yt-dlp downloaded successfully to: {ytDlpPath}" );
            }
            else {
                throw new NotSupportedException( $"Automatic yt-dlp download not supported for platform {Application.platform}" );
            }

            return ytDlpPath;
        }

        public async Task<string> EnsureFFmpegAsync(CancellationToken cancellationToken = default) {
            string ffmpegPath = GetFFmpegPath();

            if ( File.Exists( ffmpegPath ) ) {
                Logger.Log( $"FFmpeg already exists at: {ffmpegPath}" );
                return ffmpegPath;
            }

            // Only download for the Windows platform for now
            if ( Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.WindowsEditor ) {
                throw new NotSupportedException( $"Automatic FFmpeg download not supported for platform {Application.platform}" );
            }

            Logger.Log( "Downloading FFmpeg..." );
            string archivePath = Path.Combine( Path.GetTempPath(), "ffmpeg.zip" );

            try {
                await DownloadFileAsync( FFMPEG_WINDOWS_URL, archivePath, cancellationToken );
                await ExtractFFmpegArchiveAsync( archivePath, cancellationToken );

                if ( File.Exists( archivePath ) ) {
                    File.Delete( archivePath );
                }

                UpdateToolVersion( "ffmpeg", "essentials-latest", ffmpegPath );
                Logger.Log( $"FFmpeg downloaded and extracted successfully to: {ffmpegPath}" );
                return ffmpegPath;
            }
            catch (Exception ex) {
                if ( File.Exists( archivePath ) ) {
                    try { File.Delete( archivePath ); }
                    catch {
                        // ignored
                    }
                }

                throw new YtDlpException( $"Failed to download and extract FFmpeg: {ex.Message}", ex );
            }
        }

        public string GetLibraryPath(LibraryType libraryType) {
            return libraryType switch {
                LibraryType.YtDlp => GetYtDlpPath(),
                LibraryType.FFmpeg => GetFFmpegPath(),
                _ => throw new NotSupportedException( $"Library type {libraryType} is not supported." ),
            };
        }

        public string GetYtDlpPath() {
            return Application.platform switch {
                RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor
                    => Path.Combine( m_toolsDirectory, "yt-dlp", "Windows", "yt-dlp.exe" ),
                RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor
                    => Path.Combine( m_toolsDirectory, "yt-dlp", "macOS", "yt-dlp" ),
                RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor
                    => Path.Combine( m_toolsDirectory, "yt-dlp", "Linux", "yt-dlp" ),
                _ => throw new NotSupportedException( $"Platform {Application.platform} is not supported for yt-dlp." ),
            };
        }

        public string GetFFmpegPath() {
            return Application.platform switch {
                RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor
                    => Path.Combine( m_toolsDirectory, "ffmpeg", "Windows", "bin", "ffmpeg.exe" ),
                RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor
                    => Path.Combine( m_toolsDirectory, "ffmpeg", "macOS", "bin", "ffmpeg" ),
                RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor
                    => Path.Combine( m_toolsDirectory, "ffmpeg", "Linux", "bin", "ffmpeg" ),
                _ => throw new NotSupportedException( $"Platform {Application.platform} is not supported for FFmpeg." ),
            };
        }

        async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken) {
            string directoryName = Path.GetDirectoryName( destinationPath );
            if ( !string.IsNullOrEmpty( directoryName ) ) {
                Directory.CreateDirectory( directoryName );
            }

            try {
                using var response = await m_httpClient.GetAsync( url, HttpCompletionOption.ResponseHeadersRead, cancellationToken );
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1;
                Logger.Log( $"Starting download: {Path.GetFileName( destinationPath )} ({(totalBytes > 0 ? $"{totalBytes / 1024 / 1024:F1} MB" : "unknown size")})" );

                await using var contentStream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = new FileStream( destinationPath, FileMode.Create, FileAccess.Write, FileShare.None );

                var buffer = new byte[ 8192 ];
                long downloadedBytes = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync( buffer, 0, buffer.Length, cancellationToken )) > 0) {
                    await fileStream.WriteAsync( buffer, 0, bytesRead, cancellationToken );
                    downloadedBytes += bytesRead;

                    if ( totalBytes > 0 ) {
                        double progressPercent = (double)downloadedBytes / totalBytes * 100;
                        if ( downloadedBytes % (1024 * 1024) < 8192 ) {
                            // Log every ~1MB
                            Logger.Log( $"Download progress: {progressPercent:F1}% ({downloadedBytes / 1024 / 1024:F1}/{totalBytes / 1024 / 1024:F1} MB)" );
                        }
                    }
                }

                Logger.Log( $"Download completed: {Path.GetFileName( destinationPath )} ({downloadedBytes / 1024 / 1024:F1} MB)" );
            }
            catch (HttpRequestException ex) {
                throw new YtDlpException( $"Failed to download from {url}: {ex.Message}", ex );
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException) {
                throw new YtDlpException( $"Download from {url} timed out", ex );
            }
            catch (Exception ex) {
                Logger.LogError( $"Unexpected error during download: {ex.Message}" );
                throw;
            }
        }

        async Task ExtractFFmpegArchiveAsync(string archivePath, CancellationToken cancellationToken) {
            string extractPath = Path.Combine( Path.GetTempPath(), "ffmpeg_extract_temp" );

            try {
                if ( Directory.Exists( extractPath ) ) {
                    Directory.Delete( extractPath, true );
                }

                Directory.CreateDirectory( extractPath );

                await Task.Run(
                    () => {
                        ZipFile.ExtractToDirectory( archivePath, extractPath );
                    }, cancellationToken
                );

                // Find the extracted ffmpeg directory (it usually has a version-specific name)
                string[] extractedDirs = Directory.GetDirectories( extractPath );
                if ( extractedDirs.Length == 0 ) {
                    throw new YtDlpException( "No directories found in extracted FFmpeg archive" );
                }

                string ffmpegSourceDir = extractedDirs[0]; // Take the first (and usually only) directory
                string ffmpegTargetDir = Path.Combine( m_toolsDirectory, "ffmpeg", "Windows" );

                if ( Directory.Exists( ffmpegTargetDir ) ) {
                    Directory.Delete( ffmpegTargetDir, true );
                }

                string parentDirectory = Path.GetDirectoryName( ffmpegTargetDir );
                if ( !string.IsNullOrEmpty( parentDirectory ) ) {
                    Directory.CreateDirectory( parentDirectory );
                }

                Directory.Move( ffmpegSourceDir, ffmpegTargetDir );

                // Clean up temp directory
                if ( Directory.Exists( extractPath ) ) {
                    Directory.Delete( extractPath, true );
                }
            }
            catch (Exception ex) {
                if ( Directory.Exists( extractPath ) ) {
                    try { Directory.Delete( extractPath, true ); }
                    catch {
                        // ignored
                    }
                }

                throw new YtDlpException( $"Failed to extract FFmpeg archive: {ex.Message}", ex );
            }
        }

        public void Dispose() {
            m_httpClient?.Dispose();
        }
    }

    public class ToolVersionInfo {
        public string Version { get; set; }
        public string FilePath { get; set; }
        public DateTime DownloadedAt { get; set; }
    }
}