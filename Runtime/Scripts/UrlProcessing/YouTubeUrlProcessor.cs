using System.Text.RegularExpressions;
using TCS.YoutubePlayer.Exceptions;

namespace TCS.YoutubePlayer.UrlProcessing {
    public class YouTubeUrlProcessor {
        readonly Regex m_youTubeIdRegex = new(
            @"^.*(?:(?:youtu\.be\/|v\/|vi\/|u\/\w\/|embed\/|e\/)|(?:(?:watch)?\?v(?:i)?=|\&v(?:i)?=))([^#\&\?]{11}).*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public void ValidateUrl(string videoUrl) {
            if (string.IsNullOrWhiteSpace(videoUrl)) {
                throw new InvalidYouTubeUrlException(
                    "Video URL cannot be null or empty.",
                    nameof(videoUrl)
                );
            }
        }

        public string TrimYouTubeUrl(string url) {
            if (string.IsNullOrWhiteSpace(url))
                return url;

            var match = Regex.Match(url,
                @"^(https?://(?:www\.)?youtube\.com/watch\?v=[^&]+)",
                RegexOptions.IgnoreCase);

            return match.Success ? match.Groups[1].Value : url;
        }

        public string TryExtractVideoId(string url) {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var match = m_youTubeIdRegex.Match(url);
            return (match.Success && match.Groups[1].Value.Length == 11)
                ? match.Groups[1].Value
                : null;
        }

        public DateTime? ParseExpiryFromUrl(string url) {
            try {
                var match = Regex.Match(url, @"[?&]expire=(\d+)");
                if (match.Success && long.TryParse(match.Groups[1].Value, out long unixSeconds)) {
                    return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
                }
            }
            catch (Exception) {
                // Silently ignore any regex or parse errors
            }

            return null;
        }

        public string SanitizeForShell(string input) {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            
            return input.Replace("\"", "\\\"")
                       .Replace("'", "\\'")
                       .Replace("`", "\\`")
                       .Replace("$", "\\$")
                       .Replace("&", "\\&")
                       .Replace("|", "\\|")
                       .Replace(";", "\\;")
                       .Replace("(", "\\(")
                       .Replace(")", "\\)")
                       .Replace("<", "\\<")
                       .Replace(">", "\\>");
        }
    }
}