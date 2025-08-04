using System.Threading;
using TCS.YoutubePlayer.ProcessExecution;
using TCS.YoutubePlayer.Configuration;

namespace TCS.YoutubePlayer.Subtitles {
    /// <summary>
    /// Handles burning (hard-coding) subtitles into video files using FFmpeg.
    /// This creates a new video file with subtitles permanently drawn onto the pixels.
    /// </summary>
    public class SubtitleBurner {
        readonly ProcessExecutor m_processExecutor;
        readonly string m_ffmpegPath;

        public SubtitleBurner(ProcessExecutor processExecutor) {
            m_processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
            m_ffmpegPath = LibraryManager.GetFFmpegPath();
        }

        /// <summary>
        /// Burns subtitles into a video file, creating a new output file.
        /// </summary>
        /// <param name="inputVideoPath">Path to the input video file</param>
        /// <param name="subtitlePath">Path to the subtitle file (.srt, .ass, .vtt)</param>
        /// <param name="outputVideoPath">Path for the output video with burned subtitles</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> BurnSubtitlesAsync(
            string inputVideoPath, 
            string subtitlePath, 
            string outputVideoPath, 
            CancellationToken cancellationToken = default) {
            
            if (!IsFFmpegAvailable()) {
                Logger.LogError("SubtitleBurner: FFmpeg is not available. Cannot burn subtitles.");
                return false;
            }

            if (!ValidateInputs(inputVideoPath, subtitlePath, outputVideoPath)) {
                return false;
            }

            try {
                string arguments = BuildFFmpegCommand(inputVideoPath, subtitlePath, outputVideoPath);
                Logger.Log($"SubtitleBurner: Starting subtitle burning with command: {arguments}");

                var result = await m_processExecutor.RunProcessAsync(
                    m_ffmpegPath,
                    arguments,
                    cancellationToken
                );

                if (result.ExitCode == 0) {
                    Logger.Log($"SubtitleBurner: Successfully burned subtitles to {outputVideoPath}");
                    return true;
                } else {
                    Logger.LogError($"SubtitleBurner: FFmpeg failed with exit code {result.ExitCode}");
                    Logger.LogError($"FFmpeg error output: {result.StandardError}");
                    return false;
                }
            }
            catch (Exception ex) {
                Logger.LogError($"SubtitleBurner: Error burning subtitles: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Burns subtitles with custom FFmpeg options for advanced styling.
        /// </summary>
        /// <param name="inputVideoPath">Path to the input video file</param>  
        /// <param name="subtitlePath">Path to the subtitle file</param>
        /// <param name="outputVideoPath">Path for the output video</param>
        /// <param name="options">Custom subtitle styling options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> BurnSubtitlesWithOptionsAsync(
            string inputVideoPath,
            string subtitlePath, 
            string outputVideoPath,
            SubtitleBurnOptions options,
            CancellationToken cancellationToken = default) {
            
            if (!IsFFmpegAvailable()) {
                Logger.LogError("SubtitleBurner: FFmpeg is not available. Cannot burn subtitles.");
                return false;
            }

            if (!ValidateInputs(inputVideoPath, subtitlePath, outputVideoPath)) {
                return false;
            }

            try {
                string arguments = BuildFFmpegCommandWithOptions(inputVideoPath, subtitlePath, outputVideoPath, options);
                Logger.Log($"SubtitleBurner: Starting subtitle burning with custom options: {arguments}");

                var result = await m_processExecutor.RunProcessAsync(
                    m_ffmpegPath,
                    arguments,
                    cancellationToken
                );

                if (result.ExitCode == 0) {
                    Logger.Log($"SubtitleBurner: Successfully burned subtitles to {outputVideoPath}");
                    return true;
                } else {
                    Logger.LogError($"SubtitleBurner: FFmpeg failed with exit code {result.ExitCode}");
                    Logger.LogError($"FFmpeg error output: {result.StandardError}");
                    return false;
                }
            }
            catch (Exception ex) {
                Logger.LogError($"SubtitleBurner: Error burning subtitles with options: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Estimates the file size of the output video after burning subtitles.
        /// This is a rough estimate based on the original video size.
        /// </summary>
        /// <param name="inputVideoPath">Path to the input video</param>
        /// <returns>Estimated output file size in bytes, or -1 if estimation fails</returns>
        public long EstimateOutputSize(string inputVideoPath) {
            try {
                if (!File.Exists(inputVideoPath)) return -1;
                
                var fileInfo = new FileInfo(inputVideoPath);
                // Burning subtitles typically increases file size by 5-15%
                return (long)(fileInfo.Length * 1.1);
            }
            catch {
                return -1;
            }
        }

        /// <summary>
        /// Checks if the given subtitle file format is supported for burning.
        /// </summary>
        /// <param name="subtitlePath">Path to the subtitle file</param>
        /// <returns>True if the format is supported</returns>
        public static bool IsSupportedSubtitleFormat(string subtitlePath) {
            if (string.IsNullOrEmpty(subtitlePath)) return false;
            
            string extension = Path.GetExtension(subtitlePath).ToLower();
            return extension switch {
                ".srt" => true,
                ".ass" => true,
                ".ssa" => true,
                ".vtt" => true,
                ".sub" => true,
                _ => false,
            };
        }

        bool IsFFmpegAvailable() {
            return !string.IsNullOrEmpty(m_ffmpegPath) && File.Exists(m_ffmpegPath);
        }

        bool ValidateInputs(string inputVideoPath, string subtitlePath, string outputVideoPath) {
            if (string.IsNullOrEmpty(inputVideoPath) || !File.Exists(inputVideoPath)) {
                Logger.LogError($"SubtitleBurner: Input video file not found: {inputVideoPath}");
                return false;
            }

            if (string.IsNullOrEmpty(subtitlePath) || !File.Exists(subtitlePath)) {
                Logger.LogError($"SubtitleBurner: Subtitle file not found: {subtitlePath}");
                return false;
            }

            if (!IsSupportedSubtitleFormat(subtitlePath)) {
                Logger.LogError($"SubtitleBurner: Unsupported subtitle format: {Path.GetExtension(subtitlePath)}");
                return false;
            }

            if (string.IsNullOrEmpty(outputVideoPath)) {
                Logger.LogError("SubtitleBurner: Output video path cannot be empty");
                return false;
            }

            // Ensure output directory exists
            string outputDir = Path.GetDirectoryName(outputVideoPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir)) {
                try {
                    Directory.CreateDirectory(outputDir);
                }
                catch (Exception ex) {
                    Logger.LogError($"SubtitleBurner: Failed to create output directory: {ex.Message}");
                    return false;
                }
            }

            return true;
        }

        string BuildFFmpegCommand(string inputVideoPath, string subtitlePath, string outputVideoPath) {
            // Escape file paths for shell execution
            string escapedInput = EscapeFilePath(inputVideoPath);
            string escapedSubtitle = EscapeFilePath(subtitlePath);
            string escapedOutput = EscapeFilePath(outputVideoPath);

            // Use the subtitles filter for burning
            string subtitleFilter = GetSubtitleFilter(subtitlePath);
            
            return $"-i {escapedInput} -vf \"{subtitleFilter}={escapedSubtitle}\" -c:a copy -y {escapedOutput}";
        }

        string BuildFFmpegCommandWithOptions(string inputVideoPath, string subtitlePath, string outputVideoPath, SubtitleBurnOptions options) {
            string escapedInput = EscapeFilePath(inputVideoPath);
            string escapedSubtitle = EscapeFilePath(subtitlePath);
            string escapedOutput = EscapeFilePath(outputVideoPath);

            string subtitleFilter = GetSubtitleFilter(subtitlePath);
            var filterChain = $"{subtitleFilter}={escapedSubtitle}";

            // Add custom styling options
            if (options != null) {
                List<string> styleOptions = new List<string>();

                if (!string.IsNullOrEmpty(options.FontName)) {
                    styleOptions.Add($"force_style='FontName={options.FontName}'");
                }
                if (options.FontSize > 0) {
                    styleOptions.Add($"force_style='FontSize={options.FontSize}'");
                }
                if (!string.IsNullOrEmpty(options.PrimaryColor)) {
                    styleOptions.Add($"force_style='PrimaryColour={options.PrimaryColor}'");
                }
                if (options.MarginV > 0) {
                    styleOptions.Add($"force_style='MarginV={options.MarginV}'");
                }

                if (styleOptions.Count > 0) {
                    filterChain += ":" + string.Join(":", styleOptions);
                }
            }

            string videoCodec = options?.VideoCodec ?? "libx264";
            string audioCodec = options?.CopyAudio == true ? "copy" : "aac";

            return $"-i {escapedInput} -vf \"{filterChain}\" -c:v {videoCodec} -c:a {audioCodec} -y {escapedOutput}";
        }

        string GetSubtitleFilter(string subtitlePath) {
            string extension = Path.GetExtension(subtitlePath).ToLower();
            return extension switch {
                ".ass" or ".ssa" => "ass",
                ".srt" or ".vtt" or ".sub" => "subtitles",
                _ => "subtitles", // Default to subtitles filter
            };
        }

        static string EscapeFilePath(string filePath) {
            // Basic shell escaping - wrap in quotes and escape internal quotes
            return $"\"{filePath.Replace("\"", "\\\"")}\"";
        }
    }

    /// <summary>
    /// Options for customizing subtitle burning appearance and encoding.
    /// </summary>
    [Serializable]
    public class SubtitleBurnOptions {
        [Header("Subtitle Styling")]
        public string FontName = "Arial";
        public int FontSize = 24;
        public string PrimaryColor = "&Hffffff"; // White in ASS format
        public int MarginV = 50; // Bottom margin in pixels

        [Header("Encoding Options")]
        public string VideoCodec = "libx264";
        public bool CopyAudio = true;
        public string VideoQuality = "23"; // CRF value for x264

        public static SubtitleBurnOptions CreateDefault() => new();

        public static SubtitleBurnOptions CreateHighQuality() => new() {
            FontSize = 28,
            VideoCodec = "libx264",
            VideoQuality = "18", // Higher quality
            CopyAudio = true
        };

        public static SubtitleBurnOptions CreateFastEncode() => new() {
            FontSize = 22,
            VideoCodec = "libx264",
            VideoQuality = "28", // Faster encode, lower quality
            CopyAudio = true
        };
    }
}