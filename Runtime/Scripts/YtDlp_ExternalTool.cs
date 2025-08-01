using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TCS.YoutubePlayer {
    public sealed class YtDlpException : Exception {
        public YtDlpException(string message) : base( message ) { }
        public YtDlpException(string message, Exception innerException)
            : base( message, innerException ) { }
    }

    public sealed class InvalidYouTubeUrlException : ArgumentException {
        public InvalidYouTubeUrlException(string message, string paramName)
            : base( message, paramName ) { }
    }

    public enum YtDlpUpdateResult {
        Updated,
        AlreadyUpToDate,
        Failed,
    }

    [Serializable] internal class YtDlpConfig {
        [JsonProperty( "name" )] public string Name { get; set; }
        [JsonProperty( "version" )] public string Version { get; set; }
        public string GetNameVersion() => $"{Name}.{Version}";
    }

    public static class YtDlpExternalTool {
        #region Constants & Static Fields
        const string YT_DLP_ARGS_FORMAT = "-f \"best[ext=mp4]/best\" --no-warnings -g \"{0}\"";
        const string YT_DLP_TITLE_ARGS_FORMAT = "--get-title --get-url -f \"best[ext=mp4]/best\" --no-warnings \"{0}\"";
        const string BROWSER_FOR_COOKIES = "firefox";
        static readonly YtDlpConfig CurrentYtDlpConfig;
        record CacheEntry(string DirectUrl, string Title, string Url, DateTime ExpiresAt);
        static readonly ConcurrentDictionary<string, CacheEntry> Cache = new();
        public static string GetCacheTitle(string videoUrl) {
            if ( string.IsNullOrWhiteSpace( videoUrl ) ) {
                return "Null or empty video URL";
            }
            
            string cacheKey = TryExtractVideoId( videoUrl ) ?? videoUrl;
            return Cache.TryGetValue( cacheKey, out var entry ) ? entry.Title : "Not found in cache";
        }
        static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromHours( 4 );
        record Mp4ConversionEntry(string OutputFilePath, DateTime CreatedAtUtc);
        static readonly ConcurrentDictionary<string, Mp4ConversionEntry> Mp4ConversionCache = new();
        const int MP4_CACHE_LIMIT = 1;
        static readonly string CacheFilePath = Path.Combine( Application.persistentDataPath, "yt_dlp_url_cache.json" );
        #endregion
        
        #region Cache Persistence Helpers
        static void LoadCacheFromFile() {
            if ( File.Exists( CacheFilePath ) ) {
                try {
                    string json = File.ReadAllText( CacheFilePath );
                    Dictionary<string, CacheEntry> loadedEntries = JsonConvert.DeserializeObject<Dictionary<string, CacheEntry>>( json );

                    if ( loadedEntries != null ) {
                        int loadedCount = 0;
                        foreach (KeyValuePair<string, CacheEntry> kvp in loadedEntries) {
                            if ( DateTime.UtcNow < kvp.Value.ExpiresAt ) {
                                // Only load non-expired entries
                                if ( Cache.TryAdd( kvp.Key, kvp.Value ) ) {
                                    loadedCount++;
                                }
                            }
                        }

                        Debug.Log( $"[ExternalTools] Loaded {loadedCount} non-expired entries from cache file: `{CacheFilePath}`" );
                    }
                }
                catch (Exception ex) {
                    Debug.LogError( $"[ExternalTools] Failed to load cache from `{CacheFilePath}`: {ex.Message}. Starting with an empty cache." );
                    // Optionally, delete the corrupt cache file to prevent repeated errors:
                    // try { File.Delete(CacheFilePath); } catch { /* Ignore delete error */ }
                }
            }
            else {
                Debug.Log( $"[ExternalTools] Cache file not found at `{CacheFilePath}`. Starting with an empty cache." );
            }
        }

        static void SaveCacheToFile() {
            try {
                // Create a snapshot of the cache, filtering out already expired entries
                Dictionary<string, CacheEntry> entriesToSave = Cache
                    .Where( kvp => DateTime.UtcNow < kvp.Value.ExpiresAt )
                    .ToDictionary( kvp => kvp.Key, kvp => kvp.Value );

                if ( entriesToSave.Any() ) {
                    string json = JsonConvert.SerializeObject( entriesToSave, Formatting.Indented );
                    File.WriteAllText( CacheFilePath, json );
                    Debug.Log( $"[ExternalTools] Saved {entriesToSave.Count} cache entries to: `{CacheFilePath}`" );
                }
                else {
                    // If no valid entries to save, delete the cache file if it exists
                    if ( File.Exists( CacheFilePath ) ) {
                        File.Delete( CacheFilePath );
                        Debug.Log( $"[ExternalTools] No valid cache entries to save. Deleted existing cache file: `{CacheFilePath}`" );
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogError( $"[ExternalTools] Failed to save cache to `{CacheFilePath}`: {ex.Message}" );
            }
        }
        // Regular expression to extract a YouTube video ID from nearly any URL pattern.
        static readonly Regex YouTubeIdRegex = new(
            @"^.*(?:(?:youtu\.be\/|v\/|vi\/|u\/\w\/|embed\/|e\/)|(?:(?:watch)?\?v(?:i)?=|\&v(?:i)?=))([^#\&\?]{11}).*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );
        #endregion

        #region Configuration Loading
        static YtDlpExternalTool() {
            string projectRoot = Path.GetDirectoryName( Application.dataPath );
            if ( string.IsNullOrEmpty( projectRoot ) ) {
                throw new YtDlpException(
                    $"Could not determine project root from Application.dataPath: {Application.dataPath}"
                );
            }

            string assetsConfigPath = Path.Combine(
                Application.dataPath,
                "TCS.YoutubePlayer",
                "config.json"
            );
            string packagesConfigPath = Path.Combine(
                projectRoot,
                "Packages",
                "TCS.YoutubePlayer",
                "config.json"
            );

            string configPath = null;
            if ( File.Exists( assetsConfigPath ) ) {
                configPath = assetsConfigPath;
            }
            else if ( File.Exists( packagesConfigPath ) ) {
                configPath = packagesConfigPath;
            }

            if ( configPath == null ) {
                throw new YtDlpException(
                    $"Configuration file not found at expected locations:\n" +
                    $"  * {assetsConfigPath}\n" +
                    $"  * {packagesConfigPath}"
                );
            }

            try {
                string json = File.ReadAllText( configPath );
                var loadedConfig = JsonConvert.DeserializeObject<YtDlpConfig>( json );
                CurrentYtDlpConfig = loadedConfig
                                     ?? throw new YtDlpException( "Configuration file is empty or invalid JSON." ); // Assign to the readonly static field
            }
            catch (JsonException jsonEx) {
                throw new YtDlpException( $"Failed to parse JSON in `{configPath}`.", jsonEx );
            }
            catch (IOException ioEx) {
                throw new YtDlpException( $"Failed to read configuration from `{configPath}`.", ioEx );
            }

            // Load cache from disk
            LoadCacheFromFile();

            #if !UNITY_EDITOR
            // Subscribe to save cache on quit
            Application.quitting += SaveCacheToFile;
            #else
            UnityEditor.EditorApplication.playModeStateChanged += state => {
                if ( state == UnityEditor.PlayModeStateChange.ExitingPlayMode ) {
                    SaveCacheToFile();
                }
            };
            #endif
        }
        #endregion

        #region Path Helpers
        static string YtDlpPath {
            get {
                string basePath = Path.Combine(
                    Application.streamingAssetsPath,
                    CurrentYtDlpConfig.GetNameVersion(),
                    "yt-dlp"
                );

                return Application.platform switch {
                    RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor
                        => Path.Combine( basePath, "Windows", "yt-dlp.exe" ),

                    RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor
                        => Path.Combine( basePath, "macOS", "yt-dlp" ),

                    RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor
                        => Path.Combine( basePath, "Linux", "yt-dlp" ),

                    _ => throw new NotSupportedException(
                        $"Platform {Application.platform} is not supported for yt-dlp execution."
                    ),
                };
            }
        }

        static string FFmpegPath {
            get {
                string basePath = Path.Combine(
                    Application.streamingAssetsPath,
                    CurrentYtDlpConfig.GetNameVersion(),
                    "ffmpeg"
                );

                return Application.platform switch {
                    RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor
                        => Path.Combine( basePath, "Windows", "bin", "ffmpeg.exe" ),

                    RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor
                        => Path.Combine( basePath, "macOS", "bin", "ffmpeg" ),

                    RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor
                        => Path.Combine( basePath, "Linux", "bin", "ffmpeg" ),

                    _ => throw new NotSupportedException(
                        $"Platform {Application.platform} is not supported for FFmpeg."
                    ),
                };
            }
        }
        #endregion

        #region Public API: Direct URL Retrieval
        public static async Task<string> GetDirectUrlAsync(
            string videoUrl,
            CancellationToken cancellationToken
        ) {
            if ( string.IsNullOrWhiteSpace( videoUrl ) ) {
                throw new InvalidYouTubeUrlException(
                    "Video URL cannot be null or empty.",
                    nameof(videoUrl)
                );
            }

            string trimUrl = TrimYouTubeUrl( videoUrl );
            string cacheKey = TryExtractVideoId( videoUrl ) ?? videoUrl;

            if ( Cache.TryGetValue( cacheKey, out var existingEntry ) &&
                 DateTime.UtcNow < existingEntry.ExpiresAt ) {
                return existingEntry.DirectUrl;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if ( string.IsNullOrEmpty( BROWSER_FOR_COOKIES ) ) {
                throw new YtDlpException(
                    "BrowserForCookies is not set. Please assign a valid browser name to ExternalTools.BrowserForCookies."
                );
            }

            var cookieArg = $" --cookies-from-browser \"{BROWSER_FOR_COOKIES}\"";
            string arguments = string.Format( YT_DLP_TITLE_ARGS_FORMAT, trimUrl ) + cookieArg;

            (int exitCode, string stdout, string stderr) = await RunProcessAsync( YtDlpPath, arguments, cancellationToken )
                .ConfigureAwait( false );

            if ( exitCode != 0 ) {
                 throw new YtDlpException(
                    $"yt-dlp failed with exit code {exitCode} for URL '{videoUrl}'.\nStderr: {stderr}"
                );
            }
            
            string[] lines = stdout.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) {
                throw new YtDlpException(
                    $"yt-dlp failed to return both title and URL for '{videoUrl}'.\nStdout: {stdout}\nStderr: {stderr}"
                );
            }
            string title = lines[0].Trim();
            string directUrl = lines[1].Trim();


            if ( string.IsNullOrWhiteSpace(directUrl) || !Uri.TryCreate( directUrl, UriKind.Absolute, out _ ) ) {
                throw new YtDlpException( $"yt-dlp returned an invalid direct URL: {directUrl}" );
            }
            
            var expiresAt = ParseExpiryFromUrl( directUrl ) ?? DateTime.UtcNow.Add( DefaultCacheExpiration );
            Cache[cacheKey] = new CacheEntry(
                directUrl,
                title,
                videoUrl,
                expiresAt
            );

            return directUrl;
        }

        public static async Task<string> ConvertToMp4Async(string hlsUrl, CancellationToken cancellationToken) {
            if ( string.IsNullOrEmpty( hlsUrl ) ) {
                Debug.LogError( "[ExternalTools] HLS URL is null or empty" );
                return null;
            }

            // Check if the file is already cached
            if ( Mp4ConversionCache.TryGetValue( hlsUrl, out var existingEntry ) ) {
                Debug.Log( $"[ExternalTools] HLS URL found in cache. Returning existing file: {existingEntry.OutputFilePath}" );
                return existingEntry.OutputFilePath;
            }

            // Ensure the directory exists
            Directory.CreateDirectory( Path.Combine( Application.persistentDataPath, "Streaming" ) );
            // Create a unique filename from the HLS URL
            string uniqueFileName = SanitizeUrlToFileName( hlsUrl ) + ".mp4";
            string outputFilePath = Path.Combine( Application.persistentDataPath, "Streaming", uniqueFileName );
            // If the cache is full, remove the oldest entry
            if ( Mp4ConversionCache.Count >= MP4_CACHE_LIMIT ) {
                // Order by creation time to find the oldest
                KeyValuePair<string, Mp4ConversionEntry> oldestKeyValue = Mp4ConversionCache.OrderBy( kvp => kvp.Value.CreatedAtUtc ).FirstOrDefault();
                // Check if the valid oldest entry was found (cache might be empty)
                if ( !string.IsNullOrEmpty( oldestKeyValue.Key ) ) {
                    // Or check if KeyValuePair is not default
                    if ( Mp4ConversionCache.TryRemove( oldestKeyValue.Key, out var removedEntry ) ) {
                        try {
                            if ( File.Exists( removedEntry.OutputFilePath ) ) {
                                File.Delete( removedEntry.OutputFilePath );
                                Debug.Log( $"[ExternalTools] Cache full. Removed oldest entry: {oldestKeyValue.Key} and its file: {removedEntry.OutputFilePath}" );
                            }
                            else {
                                Debug.Log( $"[ExternalTools] Cache full. Removed oldest entry: {oldestKeyValue.Key}. File not found: {removedEntry.OutputFilePath}" );
                            }
                        }
                        catch (IOException ex) {
                            Debug.LogError( $"[ExternalTools] Error deleting cached file {removedEntry.OutputFilePath}: {ex.Message}" );
                            // Decide if you want to re-throw or just log
                        }
                    }
                }
            }

            // Conversion logic
            try {
                // Delete the file if it exists (from a previous failed attempt)
                if ( File.Exists( outputFilePath ) ) {
                    File.Delete( outputFilePath );
                }

                // Run the ffmpeg process
                await RunProcessAsync( $"{FFmpegPath}", $"-i \"{hlsUrl}\" -c copy \"{outputFilePath}\"", cancellationToken );
                Debug.Log( $"[ExternalTools] HLS URL converted to MP4 at {outputFilePath}" );
                // Add the output file to the cache
                Mp4ConversionCache[hlsUrl] = new Mp4ConversionEntry( outputFilePath, DateTime.UtcNow );
                return outputFilePath;
            }
            catch (YtDlpException ex) {
                Debug.LogError( $"[ExternalTools] Error converting HLS URL to MP4: {ex.Message}" );
                throw;
            }
        }
        static string SanitizeUrlToFileName(string url) {
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash( Encoding.UTF8.GetBytes( url ) );
            return BitConverter.ToString( hashBytes ).Replace( "-", "" ).ToLowerInvariant();
        }
        #endregion

        #region Public API: Version Management
        static async Task<string> GetCurrentYtDlpVersionAsync(CancellationToken cancellationToken) {
            Debug.Log( "[ExternalTools] Checking yt-dlp version..." );

            string ytDlpExecutablePath = YtDlpPath;
            if ( string.IsNullOrEmpty( ytDlpExecutablePath ) || !File.Exists( ytDlpExecutablePath ) ) {
                throw new YtDlpException(
                    $"yt-dlp executable not found at expected path: {ytDlpExecutablePath}."
                );
            }

            (int exitCode, string stdout, string stderr) = await RunProcessAsync(
                ytDlpExecutablePath,
                "--version",
                cancellationToken
            ).ConfigureAwait( false );

            if ( exitCode != 0 || string.IsNullOrWhiteSpace( stdout ) ) {
                throw new YtDlpException(
                    $"yt-dlp --version failed with exit code {exitCode}.\n" +
                    $"Stdout: {stdout}\nStderr: {stderr}"
                );
            }

            string version = stdout
                .Split( new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries )[0]
                .Trim();
            Debug.Log( $"[ExternalTools] Current yt-dlp version: {version}" );
            return version;
        }

        static async Task<YtDlpUpdateResult> UpdateYtDlpAsync(CancellationToken cancellationToken) {
            Debug.Log( "[ExternalTools] Attempting to update yt-dlp..." );

            string ytDlpExecutablePath = YtDlpPath;
            if ( string.IsNullOrEmpty( ytDlpExecutablePath ) || !File.Exists( ytDlpExecutablePath ) ) {
                throw new YtDlpException(
                    $"yt-dlp executable not found at expected path: {ytDlpExecutablePath}. Cannot perform update."
                );
            }

            (int exitCode, string rawStdout, string rawStderr) = await RunProcessAsync(
                ytDlpExecutablePath,
                "--update",
                cancellationToken
            ).ConfigureAwait( false );

            // Normalize line endings for consistent parsing
            string stdout = rawStdout.Replace( "\r\n", "\n" ).Trim();
            string stderr = rawStderr.Replace( "\r\n", "\n" ).Trim();

            // If yt-dlp exits non-zero but outputs "up to date", treat as AlreadyUpToDate
            if ( exitCode != 0 ) {
                if ( ContainsUpToDateMessage( stdout ) || ContainsUpToDateMessage( stderr ) ) {
                    Debug.Log( "[ExternalTools] yt-dlp is already up to date." );
                    return YtDlpUpdateResult.AlreadyUpToDate;
                }

                Debug.LogError(
                    $"[ExternalTools] yt-dlp --update failed (exit code {exitCode}).\n" +
                    $"Stdout: {stdout}\nStderr: {stderr}"
                );
                return YtDlpUpdateResult.Failed;
            }

            // If exit code == 0, check if it explicitly reported an update
            if ( stdout.Contains( "Updated yt-dlp to" ) ||
                 stdout.Contains( "Successfully updated" ) ) {
                Debug.Log( $"[ExternalTools] yt-dlp updated successfully: {stdout}" );
                return YtDlpUpdateResult.Updated;
            }

            // If stdout/stderr contains "up to date", treat accordingly
            if ( ContainsUpToDateMessage( stdout ) || ContainsUpToDateMessage( stderr ) ) {
                Debug.Log( "[ExternalTools] yt-dlp is already up to date." );
                return YtDlpUpdateResult.AlreadyUpToDate;
            }

            // Otherwise, assume AlreadyUpToDate if no explicit failure banner
            if ( !string.IsNullOrWhiteSpace( stderr ) ) {
                Debug.LogWarning( $"[ExternalTools] yt-dlp --update exited 0 but had stderr:\n{stderr}" );
            }

            Debug.LogWarning(
                $"[ExternalTools] yt-dlp --update finished with exit code 0 but did not explicitly report an update. " +
                "Assuming it is already up to date.\n" +
                $"Stdout: {stdout}"
            );
            return YtDlpUpdateResult.AlreadyUpToDate;
        }


        public static async Task PerformYtDlpUpdateCheckAsync(CancellationToken cancellationToken) {
            var oldVersion = "unknown";
            try {
                oldVersion = await GetCurrentYtDlpVersionAsync( cancellationToken ).ConfigureAwait( false );
                Debug.Log( $"[ExternalTools] Current yt-dlp version (before update): {oldVersion}" );
            }
            catch (YtDlpException ex) {
                Debug.LogWarning( $"[ExternalTools] Could not determine current yt-dlp version: {ex.Message}" );
            }
            catch (OperationCanceledException) {
                Debug.Log( "[ExternalTools] Version check before update was canceled." );
                throw;
            }

            cancellationToken.ThrowIfCancellationRequested();

            YtDlpUpdateResult updateResult;
            try {
                updateResult = await UpdateYtDlpAsync( cancellationToken ).ConfigureAwait( false );
            }
            catch (YtDlpException ex) {
                Debug.LogError( $"[ExternalTools] yt-dlp update threw an exception: {ex.Message}" );
                updateResult = YtDlpUpdateResult.Failed;
            }
            catch (OperationCanceledException) {
                Debug.Log( "[ExternalTools] Update process was canceled." );
                throw;
            }

            switch (updateResult) {
                case YtDlpUpdateResult.Updated:
                    Debug.Log( "[ExternalTools] yt-dlp was updated." );
                    try {
                        string newVersion = await GetCurrentYtDlpVersionAsync( cancellationToken )
                            .ConfigureAwait( false );
                        Debug.Log( $"[ExternalTools] New yt-dlp version (after update): {newVersion}" );
                        if ( oldVersion != "unknown" && oldVersion == newVersion ) {
                            Debug.LogWarning(
                                $"[ExternalTools] yt-dlp reported an update, but version remains {newVersion} (same as {oldVersion})."
                            );
                        }
                    }
                    catch (YtDlpException ex) {
                        Debug.LogWarning( $"[ExternalTools] Could not get version after update: {ex.Message}" );
                    }
                    catch (OperationCanceledException) {
                        Debug.Log( "[ExternalTools] Version check after update was canceled." );
                    }

                    break;

                case YtDlpUpdateResult.AlreadyUpToDate:
                    Debug.Log( $"[ExternalTools] yt-dlp is already at the latest version (was {oldVersion})." );
                    break;

                case YtDlpUpdateResult.Failed:
                    Debug.LogError( $"[ExternalTools] yt-dlp update failed. Version likely remains: {oldVersion}" );
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion

        #region Public API: Running External Processes
        public static Task<(int code, string stdout, string stderr)> RunProcessAsync(
            string arguments,
            CancellationToken cancellationToken
        ) {
            return RunProcessAsync(
                YtDlpPath,
                arguments,
                cancellationToken
            );
        }

        static Task<(int code, string stdout, string stderr)> RunProcessAsync(
            string fileName,
            string arguments,
            CancellationToken cancellationToken
        ) {
            TaskCompletionSource<(int, string, string)> tcs = new();
            var process = new Process {
                StartInfo = {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                },
                EnableRaisingEvents = true
            };

            #if UNITY_2020_1_OR_NEWER
            process.StartInfo.Environment["FFMPEG_LOCATION"] = FFmpegPath;
            #else
            process.StartInfo.EnvironmentVariables["FFMPEG_LOCATION"] = FFmpegPath;
            #endif

            var stdoutBuilder = new StringBuilder();
            var stderrBuilder = new StringBuilder();
            CancellationTokenRegistration ctr = default;

            if ( cancellationToken.CanBeCanceled ) {
                ctr = cancellationToken.Register( () => {
                        try {
                            if ( !process.HasExited ) {
                                process.Kill();
                                Debug.LogWarning(
                                    $"[ExternalTools] Killed process {Path.GetFileName( fileName )} due to cancellation."
                                );
                            }
                        }
                        catch (InvalidOperationException) {
                            // Already exited
                        }
                        catch (Exception ex) {
                            Debug.LogError( $"[ExternalTools] Exception trying to kill process: {ex.Message}" );
                        }

                        tcs.TrySetCanceled( cancellationToken );
                    }
                );
            }

            process.OutputDataReceived += (_, e) => {
                if ( e.Data != null )
                    stdoutBuilder.AppendLine( e.Data );
            };
            process.ErrorDataReceived += (_, e) => {
                if ( e.Data != null )
                    stderrBuilder.AppendLine( e.Data );
            };

            process.Exited += (_, _) => {
                tcs.TrySetResult(
                    (process.ExitCode, stdoutBuilder.ToString(), stderrBuilder.ToString())
                );
                if ( cancellationToken.CanBeCanceled )
                    ctr.Dispose();
                process.Dispose();
            };

            try {
                if ( !File.Exists( fileName ) )
                    throw new YtDlpException( $"Executable not found: {fileName}" );

                if ( !process.Start() ) {
                    tcs.TrySetException(
                        new YtDlpException( $"Failed to start process: {fileName}" )
                    );
                }
                else {
                    Debug.Log( $"[ExternalTools] Started process: {fileName} {arguments} (PID: {process.Id})" );
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
            }
            catch (Exception ex) {
                Debug.LogError( $"[ExternalTools] Exception launching '{fileName} {arguments}': {ex}" );
                tcs.TrySetException(
                    new YtDlpException(
                        $"Failed to start process '{Path.GetFileName( fileName )}'. Exception: {ex.Message}", ex
                    )
                );
            }

            return tcs.Task;
        }
        #endregion

        #region Private Helpers
        // ======== Private Helpers ========
        static DateTime? ParseExpiryFromUrl(string url) {
            try {
                var match = Regex.Match( url, @"[?&]expire=(\d+)" );
                if ( match.Success && long.TryParse( match.Groups[1].Value, out long unixSeconds ) ) {
                    return DateTimeOffset.FromUnixTimeSeconds( unixSeconds ).UtcDateTime;
                }
            }
            catch (Exception) {
                // Silently ignore any regex or parse errors.
            }

            return null;
        }

        static string TrimYouTubeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return url;

            // keeps everything up to—but not including—the first ‘&’
            var m = Regex.Match(url,
                                @"^(https?://(?:www\.)?youtube\.com/watch\?v=[^&]+)",
                                RegexOptions.IgnoreCase);

            return m.Success ? m.Groups[1].Value : url;
        }

        static string TryExtractVideoId(string url) {
            if ( string.IsNullOrWhiteSpace( url ) )
                return null;

            var match = YouTubeIdRegex.Match( url );
            return (match.Success && match.Groups[1].Value.Length == 11)
                ? match.Groups[1].Value
                : null;
        }

        static bool ContainsUpToDateMessage(string text) {
            if ( string.IsNullOrEmpty( text ) ) return false;
            return text.Contains( "is already the newest version", StringComparison.OrdinalIgnoreCase )
                   || text.Contains( "is up to date", StringComparison.OrdinalIgnoreCase );
        }
        #endregion
    }
}