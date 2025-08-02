using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using TCS.YoutubePlayer.UrlProcessing;
using Logger = TCS.YoutubePlayer.Utils.Logger;

namespace TCS.YoutubePlayer.Caching {
    public class YtDlpUrlCache : IDisposable {
        readonly ConcurrentDictionary<string, CacheEntry> m_cache = new();
        readonly TimeSpan m_defaultCacheExpiration = TimeSpan.FromHours(4);
        readonly string m_cacheFilePath = Path.Combine(Application.persistentDataPath, "yt_dlp_url_cache.json");
        readonly YouTubeUrlProcessor m_urlProcessor;

        public YtDlpUrlCache(YouTubeUrlProcessor urlProcessor) {
            m_urlProcessor = urlProcessor ?? throw new ArgumentNullException(nameof(urlProcessor));
            _ = Task.Run(LoadCacheFromFileAsync);

            #if !UNITY_EDITOR
            Application.quitting += SaveCacheToFile;
            #else
            UnityEditor.EditorApplication.playModeStateChanged += state => {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                    SaveCacheToFile();
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
            if (string.IsNullOrWhiteSpace(videoUrl)) return false;

            string cacheKey = ExtractVideoIdOrUrl(videoUrl);
            
            if (m_cache.TryGetValue(cacheKey, out entry) && DateTime.UtcNow < entry.ExpiresAt) {
                return true;
            }

            return false;
        }

        public void AddToCache(string videoUrl, string directUrl, string title, DateTime? expiresAt = null) {
            if (string.IsNullOrWhiteSpace(videoUrl) || string.IsNullOrWhiteSpace(directUrl)) return;

            string cacheKey = ExtractVideoIdOrUrl(videoUrl);
            var expiry = expiresAt ?? DateTime.UtcNow.Add(m_defaultCacheExpiration);
            
            m_cache[cacheKey] = new CacheEntry(directUrl, title, videoUrl, expiry);
        }

        async Task LoadCacheFromFileAsync() {
            if (File.Exists(m_cacheFilePath)) {
                try {
                    string json = await File.ReadAllTextAsync(m_cacheFilePath);
                    Dictionary<string, CacheEntry> loadedEntries = JsonConvert.DeserializeObject<Dictionary<string, CacheEntry>>(json);

                    if (loadedEntries != null) {
                        int loadedCount = 0;
                        foreach (KeyValuePair<string, CacheEntry> kvp in loadedEntries) {
                            if (DateTime.UtcNow < kvp.Value.ExpiresAt) {
                                if (m_cache.TryAdd(kvp.Key, kvp.Value)) {
                                    loadedCount++;
                                }
                            }
                        }

                        Logger.Log($"[UrlCache] Loaded {loadedCount} non-expired entries from cache file: `{m_cacheFilePath}`");
                    }
                }
                catch (Exception ex) {
                    Logger.LogError($"[UrlCache] Failed to load cache from `{m_cacheFilePath}`: {ex.Message}. Starting with an empty cache.");
                }
            }
            else {
                Logger.Log($"[UrlCache] Cache file not found at `{m_cacheFilePath}`. Starting with an empty cache.");
            }
        }

        void SaveCacheToFile() {
            try {
                Dictionary<string, CacheEntry> entriesToSave = m_cache
                    .Where(kvp => DateTime.UtcNow < kvp.Value.ExpiresAt)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (entriesToSave.Any()) {
                    string json = JsonConvert.SerializeObject(entriesToSave, Formatting.Indented);
                    File.WriteAllText(m_cacheFilePath, json);
                    Logger.Log($"[UrlCache] Saved {entriesToSave.Count} cache entries to: `{m_cacheFilePath}`");
                }
                else {
                    if (File.Exists(m_cacheFilePath)) {
                        File.Delete(m_cacheFilePath);
                        Logger.Log($"[UrlCache] No valid cache entries to save. Deleted existing cache file: `{m_cacheFilePath}`");
                    }
                }
            }
            catch (Exception ex) {
                Logger.LogError($"[UrlCache] Failed to save cache to `{m_cacheFilePath}`: {ex.Message}");
            }
        }

        string ExtractVideoIdOrUrl(string videoUrl) {
            return m_urlProcessor.TryExtractVideoId(videoUrl) ?? videoUrl;
        }

        public void Dispose() {
            SaveCacheToFile();
            m_cache.Clear();
        }
    }
}