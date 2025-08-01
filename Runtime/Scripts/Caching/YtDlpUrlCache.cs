using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using TCS.YoutubePlayer.UrlProcessing;
using Logger = TCS.YoutubePlayer.Utils.Logger;

namespace TCS.YoutubePlayer.Caching {
    public class YtDlpUrlCache {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        private readonly TimeSpan _defaultCacheExpiration = TimeSpan.FromHours(4);
        private readonly string _cacheFilePath = Path.Combine(Application.persistentDataPath, "yt_dlp_url_cache.json");
        private readonly YouTubeUrlProcessor _urlProcessor;

        public YtDlpUrlCache(YouTubeUrlProcessor urlProcessor) {
            _urlProcessor = urlProcessor ?? throw new ArgumentNullException(nameof(urlProcessor));
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
            return _cache.TryGetValue(cacheKey, out var entry) ? entry.Title : "Not found in cache";
        }

        public bool TryGetCachedEntry(string videoUrl, out CacheEntry entry) {
            entry = null;
            if (string.IsNullOrWhiteSpace(videoUrl)) return false;

            string cacheKey = ExtractVideoIdOrUrl(videoUrl);
            
            if (_cache.TryGetValue(cacheKey, out entry) && DateTime.UtcNow < entry.ExpiresAt) {
                return true;
            }

            return false;
        }

        public void AddToCache(string videoUrl, string directUrl, string title, DateTime? expiresAt = null) {
            if (string.IsNullOrWhiteSpace(videoUrl) || string.IsNullOrWhiteSpace(directUrl)) return;

            string cacheKey = ExtractVideoIdOrUrl(videoUrl);
            var expiry = expiresAt ?? DateTime.UtcNow.Add(_defaultCacheExpiration);
            
            _cache[cacheKey] = new CacheEntry(directUrl, title, videoUrl, expiry);
        }

        private async Task LoadCacheFromFileAsync() {
            if (File.Exists(_cacheFilePath)) {
                try {
                    string json = await File.ReadAllTextAsync(_cacheFilePath);
                    Dictionary<string, CacheEntry> loadedEntries = JsonConvert.DeserializeObject<Dictionary<string, CacheEntry>>(json);

                    if (loadedEntries != null) {
                        int loadedCount = 0;
                        foreach (KeyValuePair<string, CacheEntry> kvp in loadedEntries) {
                            if (DateTime.UtcNow < kvp.Value.ExpiresAt) {
                                if (_cache.TryAdd(kvp.Key, kvp.Value)) {
                                    loadedCount++;
                                }
                            }
                        }

                        Logger.Log($"[UrlCache] Loaded {loadedCount} non-expired entries from cache file: `{_cacheFilePath}`");
                    }
                }
                catch (Exception ex) {
                    Logger.LogError($"[UrlCache] Failed to load cache from `{_cacheFilePath}`: {ex.Message}. Starting with an empty cache.");
                }
            }
            else {
                Logger.Log($"[UrlCache] Cache file not found at `{_cacheFilePath}`. Starting with an empty cache.");
            }
        }

        private void SaveCacheToFile() {
            try {
                Dictionary<string, CacheEntry> entriesToSave = _cache
                    .Where(kvp => DateTime.UtcNow < kvp.Value.ExpiresAt)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (entriesToSave.Any()) {
                    string json = JsonConvert.SerializeObject(entriesToSave, Formatting.Indented);
                    File.WriteAllText(_cacheFilePath, json);
                    Logger.Log($"[UrlCache] Saved {entriesToSave.Count} cache entries to: `{_cacheFilePath}`");
                }
                else {
                    if (File.Exists(_cacheFilePath)) {
                        File.Delete(_cacheFilePath);
                        Logger.Log($"[UrlCache] No valid cache entries to save. Deleted existing cache file: `{_cacheFilePath}`");
                    }
                }
            }
            catch (Exception ex) {
                Logger.LogError($"[UrlCache] Failed to save cache to `{_cacheFilePath}`: {ex.Message}");
            }
        }

        private string ExtractVideoIdOrUrl(string videoUrl) {
            return _urlProcessor.TryExtractVideoId(videoUrl) ?? videoUrl;
        }
    }
}