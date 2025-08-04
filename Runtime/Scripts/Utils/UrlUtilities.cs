using System.Text.RegularExpressions;

namespace TCS.YoutubePlayer.Utils {
    public static class UrlUtilities {
        public static void ValidateUrl(string videoUrl) {
            if ( string.IsNullOrWhiteSpace( videoUrl ) ) {
                throw new InvalidYouTubeUrlException(
                    "Video URL cannot be null or empty.",
                    nameof(videoUrl)
                );
            }
        }

        public static string TrimYouTubeUrl(string url) {
            if ( string.IsNullOrWhiteSpace( url ) ) {
                return url;
            }

            var match = Regex.Match(
                url,
                @"^(https?://(?:www\.)?youtube\.com/watch\?v=[^&]+)",
                RegexOptions.IgnoreCase
            );

            return match.Success ? match.Groups[1].Value : url;
        }

        public static DateTime? ParseExpiryFromUrl(string url) {
            try {
                var match = Regex.Match( url, @"[?&]expire=(\d+)" );
                if ( match.Success && long.TryParse( match.Groups[1].Value, out long unixSeconds ) ) {
                    return DateTimeOffset.FromUnixTimeSeconds( unixSeconds ).UtcDateTime;
                }
            }
            catch (Exception) {
                // Silently ignore any regex or parse errors
            }

            return null;
        }

        public static string SanitizeForShell(string input) {
            if ( string.IsNullOrEmpty( input ) ) {
                return string.Empty;
            }

            return input.Replace( "\"", "\\\"" )
                .Replace( "'", "\\'" )
                .Replace( "`", "\\`" )
                .Replace( "$", "\\$" )
                .Replace( "&", "\\&" )
                .Replace( "|", "\\|" )
                .Replace( ";", "\\;" )
                .Replace( "(", "\\(" )
                .Replace( ")", "\\)" )
                .Replace( "<", "\\<" )
                .Replace( ">", "\\>" );
        }
    }
}