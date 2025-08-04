using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TCS.YoutubePlayer.UrlProcessing;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TCS.YoutubePlayer.Caching {
    public class YtDlpUrlCache : IDisposable {
        readonly ConcurrentDictionary<string, CacheEntry> m_cache = new();
        readonly TimeSpan m_defaultCacheExpiration = TimeSpan.FromHours(4);
        readonly string m_cacheFilePath;

        string GetCacheFilePath() {
#if UNITY_EDITOR
            // In Unity Editor, find the package root dynamically
            string currentFilePath = GetCurrentFilePath();
            if (!string.IsNullOrEmpty(currentFilePath)) {
                // Navigate up from the current file to find package root
                string packageRoot = FindPackageRoot(currentFilePath);
                if (!string.IsNullOrEmpty(packageRoot)) {
                    return Path.Combine(packageRoot, "yt_dlp_url_cache.json");
                }
            }
            
            // Fallback: use persistent data path
            return Path.Combine(Application.persistentDataPath, "yt_dlp_url_cache.json");
#else
            // In builds, use persistent data path
            return Path.Combine(Application.persistentDataPath, "yt_dlp_url_cache.json");
#endif
        }

        string FindPackageRoot(string filePath) {
            string currentDir = Path.GetDirectoryName(filePath);
            
            // Look for package indicators: package.json, TCS.YoutubePlayer.asmdef, or Runtime folder with specific structure
            while (!string.IsNullOrEmpty(currentDir)) {
                // Check for package.json (UPM package)
                if (File.Exists(Path.Combine(currentDir, "package.json"))) {
                    try {
                        string packageJsonContent = File.ReadAllText(Path.Combine(currentDir, "package.json"));
                        if (packageJsonContent.Contains("\"name\"") && packageJsonContent.Contains("TCS.YoutubePlayer")) {
                            return currentDir;
                        }
                    }
                    catch {
                        // Continue searching if we can't read the file
                    }
                }
                
                // Check for TCS.YoutubePlayer.asmdef in Runtime folder
                string runtimePath = Path.Combine(currentDir, "Runtime");
                if (Directory.Exists(runtimePath) && File.Exists(Path.Combine(runtimePath, "TCS.YoutubePlayer.asmdef"))) {
                    return currentDir;
                }
                
                // Check if the current directory contains TCS.YoutubePlayer.asmdef (for direct Runtime placement)
                if (File.Exists(Path.Combine(currentDir, "TCS.YoutubePlayer.asmdef"))) {
                    return currentDir;
                }
                
                // Move up one directory
                string parentDir = Path.GetDirectoryName(currentDir);
                if (parentDir == currentDir) break; // Reached root
                currentDir = parentDir;
            }
            
            return null;
        }

        string GetCurrentFilePath([System.Runtime.CompilerServices.CallerFilePath] string filePath = "") {
            return filePath;
        }
        readonly YouTubeUrlProcessor m_urlProcessor;

        public YtDlpUrlCache(YouTubeUrlProcessor urlProcessor) {
            m_urlProcessor = urlProcessor ?? throw new ArgumentNullException(nameof(urlProcessor));
            m_cacheFilePath = GetCacheFilePath();
            // Load cache synchronously to ensure it's available immediately
            LoadCacheFromFile();

            #if !UNITY_EDITOR
            Application.quitting += SaveCacheToFile;
            #else
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.ExitingPlayMode) {
                    SaveCacheToFile();
                } else if (state == PlayModeStateChange.EnteredPlayMode) {
                    // Handle domain reload disabled case - reload cache when entering play mode
                    ReloadCacheFromFile();
                }
            };
            #endif
        }

        public string GetCacheTitle(string videoUrl) {
            if (string.IsNullOrWhiteSpace(videoUrl)) {
                return "Null or empty video URL";
            }
            
            string cacheKey = ExtractVideoIdOrUrl(videoUrl);
            return m_cache.TryGetValue(cacheKey, out var entry) ? entry.Title : "Not found in cache";
        }

        public bool TryGetCachedEntry(string videoUrl, out CacheEntry entry) {
            entry = null;
            if (string.IsNullOrWhiteSpace(videoUrl)) {
                return false;
            }

            string cacheKey = ExtractVideoIdOrUrl(videoUrl);
            
            if (m_cache.TryGetValue(cacheKey, out entry) && DateTime.UtcNow < entry.ExpiresAt) {
                return true;
            }

            return false;
        }

        public void AddToCache(string videoUrl, string directUrl, string title, DateTime? expiresAt = null) {
            if (string.IsNullOrWhiteSpace(videoUrl) || string.IsNullOrWhiteSpace(directUrl)) {
                return;
            }

            string cacheKey = ExtractVideoIdOrUrl(videoUrl);
            var expiry = expiresAt ?? DateTime.UtcNow.Add(m_defaultCacheExpiration);
            
            m_cache[cacheKey] = new CacheEntry(directUrl, title, videoUrl, expiry);
        }

        void LoadCacheFromFile() => LoadCacheFromFileInternal(false);

        void ReloadCacheFromFile() => LoadCacheFromFileInternal(true);

        void LoadCacheFromFileInternal(bool isReload) {
            if (File.Exists(m_cacheFilePath)) {
                try {
                    string json = File.ReadAllText(m_cacheFilePath);
                    Dictionary<string, CacheEntry> loadedEntries = JsonConvert.DeserializeObject<Dictionary<string, CacheEntry>>(json);

                    if (loadedEntries != null) {
                        var loadedCount = 0;
                        var expiredCount = 0;
                        var updatedCount = 0;
                        var currentTime = DateTime.UtcNow;
                        
                        foreach (KeyValuePair<string, CacheEntry> kvp in loadedEntries) {
                            if (currentTime >= kvp.Value.ExpiresAt) {
                                expiredCount++;
                                continue; // Skip expired entries during loading
                            }

                            if (isReload) {
                                // During reload, update existing entries or add new ones
                                var wasUpdated = m_cache.ContainsKey(kvp.Key);
                                m_cache[kvp.Key] = kvp.Value;
                                if (wasUpdated) {
                                    updatedCount++;
                                } else {
                                    loadedCount++;
                                }
                            } else {
                                // During initial load, only add if not already present
                                if (m_cache.TryAdd(kvp.Key, kvp.Value)) {
                                    loadedCount++;
                                }
                            }
                        }

                        if (isReload) {
                            Logger.Log($"Reloaded cache: {loadedCount} new, {updatedCount} updated (skipped {expiredCount} expired): `{m_cacheFilePath}`");
                        } else {
                            Logger.Log($"Loaded {loadedCount} valid entries from cache file (skipped {expiredCount} expired entries): `{m_cacheFilePath}`");
                        }
                    }
                }
                catch (Exception ex) {
                    Logger.LogError($"Failed to {(isReload ? "reload" : "load")} cache from `{m_cacheFilePath}`: {ex.Message}.");
                }
            }
            else if (!isReload) {
                Logger.Log($"Cache file not found at `{m_cacheFilePath}`. Starting with an empty cache.");
            }
        }

        void SaveCacheToFile() {
            try {
                var currentTime = DateTime.UtcNow;
                Dictionary<string, CacheEntry> entriesToSave = m_cache
                    .Where(kvp => currentTime < kvp.Value.ExpiresAt)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                // Always save the cache file, even if empty, to preserve the file for future sessions
                string json = JsonConvert.SerializeObject(entriesToSave, Formatting.Indented);
                File.WriteAllText(m_cacheFilePath, json);
                
                if (entriesToSave.Any()) {
                    Logger.Log($"Saved {entriesToSave.Count} valid cache entries to: `{m_cacheFilePath}`");
                } else {
                    Logger.Log($"Saved empty cache (all entries expired) to: `{m_cacheFilePath}`");
                }
            }
            catch (Exception ex) {
                Logger.LogError($"Failed to save cache to `{m_cacheFilePath}`: {ex.Message}");
            }
        }

        string ExtractVideoIdOrUrl(string videoUrl)
            => m_urlProcessor.TryExtractVideoId(videoUrl) ?? videoUrl;

        public void Dispose() {
            SaveCacheToFile();
            m_cache.Clear();
        }
    }
}