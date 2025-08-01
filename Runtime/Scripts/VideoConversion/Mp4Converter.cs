using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TCS.YoutubePlayer.Exceptions;
using TCS.YoutubePlayer.ProcessExecution;
using TCS.YoutubePlayer.UrlProcessing;
using Logger = TCS.YoutubePlayer.Utils.Logger;

namespace TCS.YoutubePlayer.VideoConversion {
    public class Mp4Converter {
        readonly ProcessExecutor m_processExecutor;
        readonly YouTubeUrlProcessor m_urlProcessor;
        readonly ConcurrentDictionary<string, Mp4ConversionEntry> m_mp4ConversionCache = new();

        const int MP4_CACHE_LIMIT = 1;

        public Mp4Converter(ProcessExecutor processExecutor, YouTubeUrlProcessor urlProcessor) {
            m_processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
            m_urlProcessor = urlProcessor ?? throw new ArgumentNullException(nameof(urlProcessor));
        }

        public async Task<string> ConvertToMp4Async(string hlsUrl, CancellationToken cancellationToken) {
            if (string.IsNullOrEmpty(hlsUrl)) {
                Logger.LogError("[Mp4Converter] HLS URL is null or empty");
                return null;
            }

            if (m_mp4ConversionCache.TryGetValue(hlsUrl, out var existingEntry)) {
                Logger.Log($"[Mp4Converter] HLS URL found in cache. Returning existing file: {existingEntry.OutputFilePath}");
                return existingEntry.OutputFilePath;
            }

            string outputFilePath = PrepareOutputDirectory(hlsUrl);
            await CleanupOldCacheEntriesAsync();
            
            try {
                await ConvertHlsToMp4Async(hlsUrl, outputFilePath, cancellationToken);
                AddToMp4Cache(hlsUrl, outputFilePath);
                return outputFilePath;
            }
            catch (YtDlpException ex) {
                Logger.LogError($"[Mp4Converter] Error converting HLS URL to MP4: {ex.Message}");
                throw;
            }
        }

        string PrepareOutputDirectory(string hlsUrl) {
            Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Streaming"));
            string uniqueFileName = SanitizeUrlToFileName(hlsUrl) + ".mp4";
            return Path.Combine(Application.persistentDataPath, "Streaming", uniqueFileName);
        }

        Task CleanupOldCacheEntriesAsync() {
            if (m_mp4ConversionCache.Count < MP4_CACHE_LIMIT) return Task.CompletedTask;

            KeyValuePair<string, Mp4ConversionEntry> oldestKeyValue = m_mp4ConversionCache.OrderBy(kvp => kvp.Value.CreatedAtUtc).FirstOrDefault();
            if (string.IsNullOrEmpty(oldestKeyValue.Key)) return Task.CompletedTask;

            if (m_mp4ConversionCache.TryRemove(oldestKeyValue.Key, out var removedEntry)) {
                try {
                    if (File.Exists(removedEntry.OutputFilePath)) {
                        File.Delete(removedEntry.OutputFilePath);
                        Logger.Log($"[Mp4Converter] Cache full. Removed oldest entry: {oldestKeyValue.Key} and its file: {removedEntry.OutputFilePath}");
                    }
                    else {
                        Logger.Log($"[Mp4Converter] Cache full. Removed oldest entry: {oldestKeyValue.Key}. File not found: {removedEntry.OutputFilePath}");
                    }
                }
                catch (IOException ex) {
                    Logger.LogError($"[Mp4Converter] Error deleting cached file {removedEntry.OutputFilePath}: {ex.Message}");
                }
            }

            return Task.CompletedTask;
        }

        async Task ConvertHlsToMp4Async(string hlsUrl, string outputFilePath, CancellationToken cancellationToken) {
            if (File.Exists(outputFilePath)) {
                File.Delete(outputFilePath);
            }

            string sanitizedHlsUrl = m_urlProcessor.SanitizeForShell(hlsUrl);
            string sanitizedOutputPath = m_urlProcessor.SanitizeForShell(outputFilePath);
            
            var result = await m_processExecutor.RunProcessAsync(
                "ffmpeg", 
                $"-i \"{sanitizedHlsUrl}\" -c copy \"{sanitizedOutputPath}\"", 
                cancellationToken
            );

            if (!result.IsSuccess) {
                throw new ProcessExecutionException(
                    $"FFmpeg conversion failed with exit code {result.ExitCode}",
                    result.ExitCode,
                    result.StandardOutput,
                    result.StandardError
                );
            }

            Logger.Log($"[Mp4Converter] HLS URL converted to MP4 at {outputFilePath}");
        }

        void AddToMp4Cache(string hlsUrl, string outputFilePath) {
            m_mp4ConversionCache[hlsUrl] = new Mp4ConversionEntry(outputFilePath, DateTime.UtcNow);
        }

        static string SanitizeUrlToFileName(string url) {
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(url));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}