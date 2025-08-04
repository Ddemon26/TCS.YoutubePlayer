using System.Threading;
using System.Threading.Tasks;
using TCS.YoutubePlayer.Caching;
using TCS.YoutubePlayer.Configuration;
using TCS.YoutubePlayer.Exceptions;
using TCS.YoutubePlayer.ProcessExecution;
using TCS.YoutubePlayer.UrlProcessing;
using TCS.YoutubePlayer.VideoConversion;

namespace TCS.YoutubePlayer {
    public enum YtDlpUpdateResult {
        Updated,
        AlreadyUpToDate,
        Failed,
    }

    public class YtDlpService : IDisposable {
        const string YT_DLP_TITLE_ARGS_FORMAT = "--get-title --get-url -f \"best[ext=mp4]/best\" --no-warnings \"{0}\"";
        const string BROWSER_FOR_COOKIES = "firefox";

        readonly YtDlpConfigurationManager m_configManager;
        readonly YtDlpUrlCache m_urlCache;
        readonly ProcessExecutor m_processExecutor;
        readonly YouTubeUrlProcessor m_urlProcessor;
        readonly Mp4Converter m_mp4Converter;

        public YtDlpService() {
            m_configManager = new YtDlpConfigurationManager();
            m_urlProcessor = new YouTubeUrlProcessor();
            m_urlCache = new YtDlpUrlCache( m_urlProcessor );
            m_processExecutor = new ProcessExecutor( YtDlpConfigurationManager.GetFFmpegPath() );
            m_mp4Converter = new Mp4Converter( m_processExecutor, m_urlProcessor );
        }

        public async Task InitializeToolsAsync(CancellationToken cancellationToken = default) {
            Logger.Log( "Initializing external tools..." );

            try {
                Task<string> ytDlpTask = m_configManager.EnsureYtDlpAsync( cancellationToken );
                Task<string> ffmpegTask = m_configManager.EnsureFFmpegAsync( cancellationToken );

                await Task.WhenAll( ytDlpTask, ffmpegTask );

                string ytDlpPath = await ytDlpTask;
                string ffmpegPath = await ffmpegTask;

                Logger.Log( "Tools initialized successfully:" );
                Logger.Log( $"yt-dlp: {ytDlpPath}" );
                Logger.Log( $"ffmpeg: {ffmpegPath}" );
            }
            catch (Exception ex) {
                Logger.LogError( $"Failed to initialize external tools: {ex.Message}" );
                throw;
            }
        }

        public string GetCacheTitle(string videoUrl) => m_urlCache.GetCacheTitle( videoUrl );

        public async Task<string> GetDirectUrlAsync(string videoUrl, CancellationToken cancellationToken) {
            await m_configManager.EnsureYtDlpAsync( cancellationToken );
            YouTubeUrlProcessor.ValidateUrl( videoUrl );

            string trimUrl = YouTubeUrlProcessor.TrimYouTubeUrl( videoUrl );
            string cacheKey = m_urlProcessor.TryExtractVideoId( videoUrl ) ?? videoUrl;

            if ( m_urlCache.TryGetCachedEntry( cacheKey, out var existingEntry ) ) {
                return existingEntry.DirectUrl;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if ( string.IsNullOrEmpty( BROWSER_FOR_COOKIES ) ) {
                throw new YtDlpException(
                    "BrowserForCookies is not set. Please assign a valid browser name."
                );
            }

            var cookieArg = $" --cookies-from-browser \"{YouTubeUrlProcessor.SanitizeForShell( BROWSER_FOR_COOKIES )}\"";
            string arguments = string.Format( YT_DLP_TITLE_ARGS_FORMAT, YouTubeUrlProcessor.SanitizeForShell( trimUrl ) ) + cookieArg;

            var result = await m_processExecutor.RunProcessAsync(
                YtDlpConfigurationManager.GetYtDlpPath(),
                arguments,
                cancellationToken
            );

            if ( !result.IsSuccess ) {
                throw new ProcessExecutionException(
                    $"yt-dlp failed with exit code {result.ExitCode} for URL '{videoUrl}'.",
                    result.ExitCode, result.StandardOutput, result.StandardError
                );
            }

            string[] lines = result.StandardOutput.Split( new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries );
            if ( lines.Length < 2 ) {
                throw new YtDlpException(
                    $"yt-dlp failed to return both title and URL for '{videoUrl}'.\nStdout: {result.StandardOutput}\nStderr: {result.StandardError}"
                );
            }

            string title = lines[0].Trim();
            string directUrl = lines[1].Trim();

            if ( string.IsNullOrWhiteSpace( directUrl ) || !Uri.TryCreate( directUrl, UriKind.Absolute, out _ ) ) {
                throw new YtDlpException( $"yt-dlp returned an invalid direct URL: {directUrl}" );
            }

            DateTime? expiresAt = YouTubeUrlProcessor.ParseExpiryFromUrl( directUrl );
            m_urlCache.AddToCache( videoUrl, directUrl, title, expiresAt );

            return directUrl;
        }

        public Task<string> ConvertToMp4Async(string hlsUrl, CancellationToken cancellationToken) =>
            m_mp4Converter.ConvertToMp4Async( hlsUrl, cancellationToken );

        public async Task<string> GetCurrentYtDlpVersionAsync(CancellationToken cancellationToken) {
            Logger.Log( "Checking yt-dlp version..." );

            string ytDlpExecutablePath = await m_configManager.EnsureYtDlpAsync( cancellationToken );
            var result = await m_processExecutor.RunProcessAsync(
                ytDlpExecutablePath,
                "--version",
                cancellationToken
            );

            if ( !result.IsSuccess || string.IsNullOrWhiteSpace( result.StandardOutput ) ) {
                throw new YtDlpException(
                    $"yt-dlp --version failed with exit code {result.ExitCode}.\n" +
                    $"Stdout: {result.StandardOutput}\nStderr: {result.StandardError}"
                );
            }

            string version = result.StandardOutput
                .Split( new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries )[0]
                .Trim();
            Logger.Log( $"Current yt-dlp version: {version}" );
            return version;
        }

        public async Task<YtDlpUpdateResult> UpdateYtDlpAsync(CancellationToken cancellationToken) {
            Logger.Log( "Attempting to update yt-dlp..." );

            string ytDlpExecutablePath = await m_configManager.EnsureYtDlpAsync( cancellationToken );
            var result = await m_processExecutor.RunProcessAsync(
                ytDlpExecutablePath,
                "--update",
                cancellationToken
            );

            string stdout = result.StandardOutput.Replace( "\r\n", "\n" ).Trim();
            string stderr = result.StandardError.Replace( "\r\n", "\n" ).Trim();

            if ( !result.IsSuccess ) {
                if ( ContainsUpToDateMessage( stdout ) || ContainsUpToDateMessage( stderr ) ) {
                    Logger.Log( "yt-dlp is already up to date." );
                    return YtDlpUpdateResult.AlreadyUpToDate;
                }

                Logger.LogError(
                    $"yt-dlp --update failed (exit code {result.ExitCode}).\n" +
                    $"Stdout: {stdout}\nStderr: {stderr}"
                );
                return YtDlpUpdateResult.Failed;
            }

            if ( stdout.Contains( "Updated yt-dlp to" ) || stdout.Contains( "Successfully updated" ) ) {
                Logger.Log( $"yt-dlp updated successfully: {stdout}" );
                return YtDlpUpdateResult.Updated;
            }

            if ( ContainsUpToDateMessage( stdout ) || ContainsUpToDateMessage( stderr ) ) {
                Logger.Log( "yt-dlp is already up to date." );
                return YtDlpUpdateResult.AlreadyUpToDate;
            }

            if ( !string.IsNullOrWhiteSpace( stderr ) ) {
                Logger.LogWarning( $"yt-dlp --update exited 0 but had stderr:\n{stderr}" );
            }

            Logger.LogWarning(
                "yt-dlp --update finished with exit code 0 but did not explicitly report an update. " +
                "Assuming it is already up to date.\n" +
                $"Stdout: {stdout}"
            );
            return YtDlpUpdateResult.AlreadyUpToDate;
        }

        public async Task PerformYtDlpUpdateCheckAsync(CancellationToken cancellationToken) {
            var oldVersion = "unknown";
            try {
                oldVersion = await GetCurrentYtDlpVersionAsync( cancellationToken );
                Logger.Log( $"Current yt-dlp version (before update): {oldVersion}" );
            }
            catch (YtDlpException ex) {
                Logger.LogWarning( $"Could not determine current yt-dlp version: {ex.Message}" );
            }
            catch (OperationCanceledException) {
                Logger.Log( "Version check before update was canceled." );
                throw;
            }

            cancellationToken.ThrowIfCancellationRequested();

            YtDlpUpdateResult updateResult;
            try {
                updateResult = await UpdateYtDlpAsync( cancellationToken );
            }
            catch (YtDlpException ex) {
                Logger.LogError( $"yt-dlp update threw an exception: {ex.Message}" );
                updateResult = YtDlpUpdateResult.Failed;
            }
            catch (OperationCanceledException) {
                Logger.Log( "Update process was canceled." );
                throw;
            }

            switch (updateResult) {
                case YtDlpUpdateResult.Updated:
                    Logger.Log( "yt-dlp was updated." );
                    try {
                        string newVersion = await GetCurrentYtDlpVersionAsync( cancellationToken );
                        Logger.Log( $"New yt-dlp version (after update): {newVersion}" );
                        if ( oldVersion != "unknown" && oldVersion == newVersion ) {
                            Logger.LogWarning(
                                $"yt-dlp reported an update, but version remains {newVersion} (same as {oldVersion})."
                            );
                        }
                    }
                    catch (YtDlpException ex) {
                        Logger.LogWarning( $"Could not get version after update: {ex.Message}" );
                    }
                    catch (OperationCanceledException) {
                        Logger.Log( "Version check after update was canceled." );
                    }

                    break;

                case YtDlpUpdateResult.AlreadyUpToDate:
                    Logger.Log( $"yt-dlp is already at the latest version (was {oldVersion})." );
                    break;

                case YtDlpUpdateResult.Failed:
                    Logger.LogError( $"yt-dlp update failed. Version likely remains: {oldVersion}" );
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static bool ContainsUpToDateMessage(string text) {
            if ( string.IsNullOrEmpty( text ) ) {
                return false;
            }

            return text.Contains( "is already the newest version", StringComparison.OrdinalIgnoreCase )
                   || text.Contains( "is up to date", StringComparison.OrdinalIgnoreCase );
        }

        public void Dispose() {
            m_mp4Converter?.Dispose();
            m_processExecutor?.Dispose();
            m_urlCache?.Dispose();
            m_configManager?.Dispose();
        }
    }
}