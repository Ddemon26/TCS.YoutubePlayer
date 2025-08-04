using System.Text.RegularExpressions;
namespace TCS.YoutubePlayer.UrlProcessing {
    public class YouTubeUrlProcessor {
        readonly Regex m_youTubeIdRegex = new(
            @"^.*(?:(?:youtu\.be\/|v\/|vi\/|u\/\w\/|embed\/|e\/)|(?:(?:watch)?\?v(?:i)?=|\&v(?:i)?=))([^#\&\?]{11}).*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );
        
        public string TryExtractVideoId(string url) {
            if ( string.IsNullOrWhiteSpace( url ) ) {
                return null;
            }

            var match = m_youTubeIdRegex.Match( url );
            return (match.Success && match.Groups[1].Value.Length == 11)
                ? match.Groups[1].Value
                : null;
        }

    }
}