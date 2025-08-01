using System;

namespace TCS.YoutubePlayer.Caching {
    public record CacheEntry(string DirectUrl, string Title, string Url, DateTime ExpiresAt);
}