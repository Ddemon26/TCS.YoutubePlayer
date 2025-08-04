namespace TCS.YoutubePlayer.Configuration {
    public static class YtDlpSettingsExtensions {
        // Fluent API extensions for convenience
        public static YtDlpSettings WithVideoQuality(this YtDlpSettings settings, VideoQuality quality)
            => settings.With( data => data.m_videoQuality = quality );

        public static YtDlpSettings WithBrowser(this YtDlpSettings settings, BrowserType browser)
            => settings.With( data => data.m_browser = browser );

        public static YtDlpSettings WithTimeout(this YtDlpSettings settings, TimeSpan timeout)
            => settings.With( data => data.m_timeoutMinutes = (float)timeout.TotalMinutes );

        public static YtDlpSettings WithCustomVideoFormat(this YtDlpSettings settings, string format) {
            return settings.With( data => {
                    data.m_videoQuality = VideoQuality.Custom;
                    data.m_customVideoQuality = format;
                }
            );
        }

        public static YtDlpSettings WithNetworkSettings(this YtDlpSettings settings, int? concurrentFragments = null, string rateLimit = "", int? retries = null) {
            return settings.With( data => {
                    if ( concurrentFragments.HasValue ) data.m_concurrentFragments = concurrentFragments.Value;
                    if ( !string.IsNullOrEmpty( rateLimit ) ) data.m_rateLimit = rateLimit;
                    if ( retries.HasValue ) data.m_retries = retries.Value;
                }
            );
        }

        public static YtDlpSettings WithOutputFormat(this YtDlpSettings settings, OutputFormat format)
            => settings.With( data => data.m_outputFormat = format );

        public static YtDlpSettings WithAudioOnly(this YtDlpSettings settings, bool audioOnly = true)
            => settings.With( data => data.m_extractAudioOnly = audioOnly );

        public static YtDlpSettings WithAudioQuality(this YtDlpSettings settings, AudioQuality quality)
            => settings.With( data => data.m_audioQuality = quality );

        public static YtDlpSettings WithInfoJson(this YtDlpSettings settings, bool writeInfo = true)
            => settings.With( data => data.m_writeInfoJson = writeInfo );

        public static YtDlpSettings WithHlsNative(this YtDlpSettings settings, bool useNative = true)
            => settings.With( data => data.m_useHlsNative = useNative );
    }
}