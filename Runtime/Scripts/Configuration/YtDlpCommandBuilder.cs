using System.Linq;
using System.Text;

namespace TCS.YoutubePlayer.Configuration {
    public interface IYtDlpCommandBuilder {
        string BuildGetDirectUrlCommand(string videoUrl, YtDlpSettings settings = null);
        string BuildGetTitleCommand(string videoUrl, YtDlpSettings settings = null);
        string BuildConversionCommand(string inputUrl, string outputPath, YtDlpSettings settings = null);
        string BuildVersionCommand();
        string BuildUpdateCommand();
    }

    public class YtDlpCommandBuilder : IYtDlpCommandBuilder {
        readonly YtDlpSettings m_defaultSettings;

        public YtDlpCommandBuilder(YtDlpSettings defaultSettings = null) {
            m_defaultSettings = defaultSettings ?? new YtDlpSettings();
        }

        public string BuildGetDirectUrlCommand(string videoUrl, YtDlpSettings settings = null) {
            var effectiveSettings = settings ?? m_defaultSettings;
            var args = new StringBuilder();

            // Process URL based on playlist handling settings
            string processedUrl = ProcessUrlForPlaylistHandling( videoUrl, effectiveSettings );

            args.Append( "--get-url " );
            args.Append( $"-f \"{BuildFormatSelector( effectiveSettings )}\" " );
            args.Append( "--no-warnings " );

            AppendBrowserSettings( args, effectiveSettings );
            AppendNetworkSettings( args, effectiveSettings );
            AppendTimeRangeSettings( args, effectiveSettings );
            AppendPlaylistSettings( args, effectiveSettings );
            AppendCustomArguments( args, effectiveSettings );

            args.Append( $"\"{processedUrl}\"" );

            return args.ToString();
        }

        public string BuildGetTitleCommand(string videoUrl, YtDlpSettings settings = null) {
            var effectiveSettings = settings ?? m_defaultSettings;
            var args = new StringBuilder();

            args.Append( "--get-title --get-url " );
            args.Append( $"-f \"{BuildFormatSelector( effectiveSettings )}\" " );
            args.Append( "--no-warnings " );

            AppendBrowserSettings( args, effectiveSettings );
            AppendCustomArguments( args, effectiveSettings );

            args.Append( $"\"{videoUrl}\"" );

            return args.ToString();
        }

        public string BuildConversionCommand(string inputUrl, string outputPath, YtDlpSettings settings = null) {
            var effectiveSettings = settings ?? m_defaultSettings;
            var args = new StringBuilder();

            if ( effectiveSettings.ExtractAudioOnly ) {
                args.Append( "-x " );
                AppendAudioSettings( args, effectiveSettings );
            }
            else {
                AppendVideoSettings( args, effectiveSettings );
            }

            args.Append( $"-o \"{outputPath}\" " );

            AppendBrowserSettings( args, effectiveSettings );
            AppendNetworkSettings( args, effectiveSettings );
            AppendTimeRangeSettings( args, effectiveSettings );
            AppendPlaylistSettings( args, effectiveSettings );
            AppendCustomArguments( args, effectiveSettings );

            if ( effectiveSettings.IgnoreErrors ) {
                args.Append( "--ignore-errors " );
            }

            if ( effectiveSettings.WriteInfoJson ) {
                args.Append( "--write-info-json " );
            }

            args.Append( $"\"{inputUrl}\"" );

            return args.ToString();
        }

        public string BuildVersionCommand() => "--version";

        public string BuildUpdateCommand() => "--update";

        static string BuildFormatSelector(YtDlpSettings settings) {
            if ( settings.ExtractAudioOnly ) {
                return BuildAudioFormatSelector( settings );
            }

            string videoFormat = BuildVideoFormatSelector( settings );
            string audioFormat = BuildAudioFormatSelector( settings );

            if ( settings.VideoQuality == VideoQuality.Custom && !string.IsNullOrEmpty( settings.CustomVideoQuality ) ) {
                return settings.CustomVideoQuality;
            }

            return $"{videoFormat}+{audioFormat}/{videoFormat}";
        }

        static string BuildVideoFormatSelector(YtDlpSettings settings) {
            return settings.VideoQuality switch {
                VideoQuality.Worst => "worst[ext=mp4]/worst",
                VideoQuality.Low => "best[height<=480][ext=mp4]/best[height<=480]",
                VideoQuality.Medium => "best[height<=720][ext=mp4]/best[height<=720]",
                VideoQuality.High => "best[height<=1080][ext=mp4]/best[height<=1080]",
                VideoQuality.Best => "best[ext=mp4]/best",
                VideoQuality.Custom => settings.CustomVideoQuality.Value,
                _ => "best[ext=mp4]/best",
            };
        }

        static string BuildAudioFormatSelector(YtDlpSettings settings) {
            return settings.AudioQuality switch {
                AudioQuality.Worst => "worstaudio",
                AudioQuality.Low => "worstaudio[abr<=96]",
                AudioQuality.Medium => "bestaudio[abr<=192]",
                AudioQuality.High => "bestaudio[abr<=320]",
                AudioQuality.Best => "bestaudio",
                AudioQuality.Custom => settings.CustomAudioQuality.Value,
                _ => "bestaudio",
            };
        }

        static void AppendBrowserSettings(StringBuilder args, YtDlpSettings settings) {
            if ( settings.Browser == BrowserType.None ) return;

            string browserName = GetBrowserName( settings.Browser, settings.CustomBrowserPath );
            if ( !string.IsNullOrEmpty( browserName ) ) {
                args.Append( $"--cookies-from-browser \"{browserName}" );
                if ( !string.IsNullOrEmpty( settings.BrowserProfile ) ) {
                    args.Append( $":{settings.BrowserProfile}" );
                }

                args.Append( "\" " );
            }
        }

        static void AppendVideoSettings(StringBuilder args, YtDlpSettings settings) {
            string format = GetOutputFormatString( settings );
            if ( !string.IsNullOrEmpty( format ) ) {
                args.Append( $"--merge-output-format {format} " );
                args.Append( $"--remux-video {format} " );
            }

            args.Append( $"-f \"{BuildFormatSelector( settings )}\" " );
        }

        static void AppendAudioSettings(StringBuilder args, YtDlpSettings settings) {
            string audioFormat = GetAudioFormatFromQuality( settings.AudioQuality );
            if ( !string.IsNullOrEmpty( audioFormat ) ) {
                args.Append( $"--audio-format {audioFormat} " );
            }
        }

        static void AppendNetworkSettings(StringBuilder args, YtDlpSettings settings) {
            if ( settings.ConcurrentFragments.HasValue ) {
                args.Append( $"--concurrent-fragments {settings.ConcurrentFragments.Value} " );
            }

            if ( !string.IsNullOrEmpty( settings.RateLimit ) ) {
                args.Append( $"--limit-rate {settings.RateLimit} " );
            }

            if ( settings.Retries.HasValue ) {
                args.Append( $"--retries {settings.Retries.Value} " );
            }

            if ( settings.UseHlsNative ) {
                args.Append( "--hls-use-mpegts " );
            }
        }

        void AppendTimeRangeSettings(StringBuilder args, YtDlpSettings settings) {
            if ( !settings.TimeRange.IsValid ) return;

            if ( settings.TimeRange.Start.HasValue ) {
                string startTime = FormatTimeSpan( settings.TimeRange.Start.Value );
                args.Append( $"--download-sections \"*{startTime}" );

                if ( settings.TimeRange.End.HasValue ) {
                    string endTime = FormatTimeSpan( settings.TimeRange.End.Value );
                    args.Append( $"-{endTime}" );
                }

                args.Append( "\" " );
            }
        }

        static void AppendPlaylistSettings(StringBuilder args, YtDlpSettings settings) {
            switch (settings.PlaylistHandling) {
                case PlaylistHandling.StripPlaylistParams:
                    args.Append( "--no-playlist " );
                    break;
                case PlaylistHandling.PlaySingleFromPlaylist:
                    // Keep playlist context but play single video
                    break;
                case PlaylistHandling.PlayEntirePlaylist:
                    if ( settings.MaxPlaylistItems > 0 ) {
                        args.Append( $"--playlist-end {settings.MaxPlaylistItems} " );
                    }

                    break;
            }
        }

        static string ProcessUrlForPlaylistHandling(string videoUrl, YtDlpSettings settings) {
            if ( settings.PlaylistHandling == PlaylistHandling.StripPlaylistParams ) {
                // Strip playlist parameters for single video playback (default behavior)
                if ( Uri.TryCreate( videoUrl, UriKind.Absolute, out var uri ) ) {
                    // Simple regex-based approach to remove playlist parameters
                    string cleanedUrl = videoUrl;
                    cleanedUrl = System.Text.RegularExpressions.Regex.Replace( cleanedUrl, @"[&?]list=[^&]*", "" );
                    cleanedUrl = System.Text.RegularExpressions.Regex.Replace( cleanedUrl, @"[&?]index=[^&]*", "" );
                    cleanedUrl = System.Text.RegularExpressions.Regex.Replace( cleanedUrl, @"[&?]t=[^&]*", "" );

                    // Clean up any remaining and at the start of query params
                    cleanedUrl = System.Text.RegularExpressions.Regex.Replace( cleanedUrl, @"\?&", "?" );

                    return cleanedUrl;
                }
            }

            return videoUrl;
        }

        static void AppendCustomArguments(StringBuilder args, YtDlpSettings settings) {
            foreach (KeyValuePair<string, string> kvp in settings.CustomArguments) {
                if ( string.IsNullOrEmpty( kvp.Value ) ) {
                    args.Append( $"{kvp.Key} " );
                }
                else {
                    args.Append( $"{kvp.Key} \"{kvp.Value}\" " );
                }
            }
        }

        static string GetBrowserName(BrowserType browserType, string customPath = "") {
            return browserType switch {
                BrowserType.Chrome => "chrome",
                BrowserType.Firefox => "firefox",
                BrowserType.Edge => "edge",
                BrowserType.Safari => "safari",
                BrowserType.Brave => "brave",
                BrowserType.Chromium => "chromium",
                BrowserType.Custom when !string.IsNullOrEmpty( customPath ) => customPath,
                _ => "",
            };
        }

        static string GetOutputFormatString(YtDlpSettings settings) {
            return settings.OutputFormat switch {
                OutputFormat.Mp4 => "mp4",
                OutputFormat.Webm => "webm",
                OutputFormat.Mkv => "mkv",
                OutputFormat.Avi => "avi",
                OutputFormat.Flv => "flv",
                OutputFormat.Custom => settings.CustomOutputFormat,
                _ => "",
            };
        }

        static string GetAudioFormatFromQuality(AudioQuality quality) {
            return quality switch {
                AudioQuality.Worst => "mp3",
                AudioQuality.Low => "mp3",
                AudioQuality.Medium => "mp3",
                AudioQuality.High => "mp3",
                AudioQuality.Best => "best",
                _ => "best",
            };
        }

        static string GetSubtitleFormatString(SubtitleFormat format) {
            return format switch {
                SubtitleFormat.Srt => "srt",
                SubtitleFormat.Ass => "ass",
                SubtitleFormat.Vtt => "vtt",
                SubtitleFormat.Best => "best",
                _ => "",
            };
        }

        string FormatTimeSpan(TimeSpan timeSpan) => $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
    }
}