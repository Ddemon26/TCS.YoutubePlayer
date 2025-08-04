namespace TCS.YoutubePlayer.Configuration {
    [Serializable] public class YtDlpSettingsData {
        [Header( "Video Quality" )]
        public VideoQuality m_videoQuality = VideoQuality.Best;
        public string m_customVideoQuality = "best";

        [Header( "Audio Quality" )]
        public AudioQuality m_audioQuality = AudioQuality.Best;
        public string m_customAudioQuality = "bestaudio";

        [Header( "Browser Settings" )]
        public BrowserType m_browser = BrowserType.None;
        public string m_customBrowserPath = "";
        public string m_browserProfile = "";

        [Header( "Output Format" )]
        public OutputFormat m_outputFormat = OutputFormat.Default;
        public string m_customOutputFormat = "";

        [Header( "Download Settings" )]
        public bool m_extractAudioOnly = false;
        public bool m_ignoreErrors = false;
        public bool m_writeInfoJson = false;

        [Header( "Playlist Settings" )]
        public bool m_usePlaylistIndex = true;
        public PlaylistHandling m_playlistHandling = PlaylistHandling.StripPlaylistParams;
        public int m_maxPlaylistItems = 50;

        [Header( "Network Settings" )]
        public bool m_useHlsNative = true;
        public int m_concurrentFragments = 1;
        public string m_rateLimit = "";
        public int m_retries = 10;

        [Header( "Timeouts" )]
        public float m_timeoutMinutes = 5f;

        [Header( "Time Range" )]
        public bool m_useTimeRange = false;
        public float m_startTimeSeconds = 0f;
        public float m_endTimeSeconds = 0f;

        [Header( "Custom Arguments" )]
        [TextArea( 3, 5 )]
        public string m_customArguments = "";

        public VideoQuality VideoQuality => m_videoQuality;
        public string CustomVideoQuality => m_customVideoQuality;
        public AudioQuality AudioQuality => m_audioQuality;
        public string CustomAudioQuality => m_customAudioQuality;
        public BrowserType Browser => m_browser;
        public string CustomBrowserPath => m_customBrowserPath;
        public string BrowserProfile => m_browserProfile;
        public OutputFormat OutputFormat => m_outputFormat;
        public string CustomOutputFormat => m_customOutputFormat;
        public bool ExtractAudioOnly => m_extractAudioOnly;
        public bool UsePlaylistIndex => m_usePlaylistIndex;
        public PlaylistHandling PlaylistHandling => m_playlistHandling;
        public int MaxPlaylistItems => m_maxPlaylistItems;
        public bool IgnoreErrors => m_ignoreErrors;
        public bool WriteInfoJson => m_writeInfoJson;
        public bool UseHlsNative => m_useHlsNative;
        public int ConcurrentFragments => m_concurrentFragments;
        public string RateLimit => m_rateLimit;
        public int Retries => m_retries;
        public float TimeoutMinutes => m_timeoutMinutes;
        public bool UseTimeRange => m_useTimeRange;
        public float StartTimeSeconds => m_startTimeSeconds;
        public float EndTimeSeconds => m_endTimeSeconds;
        public string CustomArguments => m_customArguments;

        public YtDlpSettingsData() { }

        public YtDlpSettingsData(YtDlpSettingsData other) {
            if ( other == null ) return;

            m_videoQuality = other.m_videoQuality;
            m_customVideoQuality = other.m_customVideoQuality;
            m_audioQuality = other.m_audioQuality;
            m_customAudioQuality = other.m_customAudioQuality;
            m_browser = other.m_browser;
            m_customBrowserPath = other.m_customBrowserPath;
            m_browserProfile = other.m_browserProfile;
            m_outputFormat = other.m_outputFormat;
            m_customOutputFormat = other.m_customOutputFormat;
            m_extractAudioOnly = other.m_extractAudioOnly;
            m_usePlaylistIndex = other.m_usePlaylistIndex;
            m_playlistHandling = other.m_playlistHandling;
            m_maxPlaylistItems = other.m_maxPlaylistItems;
            m_ignoreErrors = other.m_ignoreErrors;
            m_writeInfoJson = other.m_writeInfoJson;
            m_useHlsNative = other.m_useHlsNative;
            m_concurrentFragments = other.m_concurrentFragments;
            m_rateLimit = other.m_rateLimit;
            m_retries = other.m_retries;
            m_timeoutMinutes = other.m_timeoutMinutes;
            m_useTimeRange = other.m_useTimeRange;
            m_startTimeSeconds = other.m_startTimeSeconds;
            m_endTimeSeconds = other.m_endTimeSeconds;
            m_customArguments = other.m_customArguments;
        }

        public static YtDlpSettingsData CreateDefault() => new();

        public static YtDlpSettingsData CreateStreaming() => new() {
            m_videoQuality = VideoQuality.High,
            m_useHlsNative = true,
            m_timeoutMinutes = 2f,
            m_concurrentFragments = 4,
        };

        public static YtDlpSettingsData CreateDownload() => new() {
            m_videoQuality = VideoQuality.Best,
            m_outputFormat = OutputFormat.Mp4,
            m_timeoutMinutes = 10f,
            m_writeInfoJson = true,
        };

        public static YtDlpSettingsData CreateAudioOnly() => new() {
            m_extractAudioOnly = true,
            m_audioQuality = AudioQuality.Best,
            m_timeoutMinutes = 5f,
        };

        public static YtDlpSettingsData CreateHighQuality() => new() {
            m_videoQuality = VideoQuality.Custom,
            m_customVideoQuality = "bestvideo[height<=1080]+bestaudio/best[height<=1080]",
            m_outputFormat = OutputFormat.Mp4,
            m_timeoutMinutes = 10f,
        };

        void OnValidate() {
            if ( m_concurrentFragments < 1 ) m_concurrentFragments = 1;
            if ( m_retries < 1 ) m_retries = 1;
            if ( m_timeoutMinutes < 0.1f ) m_timeoutMinutes = 0.1f;
            if ( m_startTimeSeconds < 0f ) m_startTimeSeconds = 0f;
            if ( m_endTimeSeconds < 0f ) m_endTimeSeconds = 0f;
        }
        
        public string GetSettingsSummary() {
            var summaryBuilder = new System.Text.StringBuilder();

            summaryBuilder.Append( $"Video: {VideoQuality}" );
            if ( VideoQuality == VideoQuality.Custom ) summaryBuilder.Append( $" ({CustomVideoQuality})" );

            summaryBuilder.Append( $", Audio: {AudioQuality}" );
            if ( AudioQuality == AudioQuality.Custom ) summaryBuilder.Append( $" ({CustomAudioQuality})" );

            summaryBuilder.Append( $"\nBrowser: {Browser}" );
            if ( Browser != BrowserType.None ) summaryBuilder.Append( $" ({CustomBrowserPath} - {BrowserProfile})" );

            summaryBuilder.Append( $"\nOutput: {OutputFormat}" );
            if ( OutputFormat == OutputFormat.Custom ) summaryBuilder.Append( $" ({CustomOutputFormat})" );

            summaryBuilder.Append( $"\nPlaylist: {PlaylistHandling}, Max Items: {MaxPlaylistItems}, Use Index: {UsePlaylistIndex}" );
            summaryBuilder.Append( $"\nNetwork: {ConcurrentFragments} fragments, Rate Limit: {RateLimit}, Retries: {Retries}, HLS Native: {UseHlsNative}" );
            summaryBuilder.Append( $"\nTime: Timeout {TimeoutMinutes} min, Range: {(UseTimeRange ? $"{StartTimeSeconds}s to {EndTimeSeconds}s" : "N/A")}" );
            summaryBuilder.Append( $"\nFlags: Extract Audio: {ExtractAudioOnly}, Write Info JSON: {WriteInfoJson}, Ignore Errors: {IgnoreErrors}" );

            if ( !string.IsNullOrWhiteSpace( CustomArguments ) ) {
                summaryBuilder.Append( $"\nCustom Args: {CustomArguments}" );
            }

            return summaryBuilder.ToString();
        }
    }
}