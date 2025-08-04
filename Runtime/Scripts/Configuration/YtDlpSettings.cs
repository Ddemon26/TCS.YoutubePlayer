namespace TCS.YoutubePlayer.Configuration {
    public enum VideoQuality { Worst, Low, Medium, High, Best, Custom, }

    public enum AudioQuality { Worst, Low, Medium, High, Best, Custom, }

    public enum BrowserType { None, Chrome, Firefox, Edge, Safari, Brave, Chromium, Custom, }

    public enum SubtitleFormat { None, Srt, Ass, Vtt, Best, }

    public enum OutputFormat { Default, Mp4, Webm, Mkv, Avi, Flv, Custom, }

    public enum PlaylistHandling {
        StripPlaylistParams, // Default: Remove playlist params, play single video
        PlaySingleFromPlaylist, // Play single video but keep playlist context
        PlayEntirePlaylist, // Download/play entire playlist
    }

    public struct QualitySelector {
        public string Value { get; }

        public QualitySelector(string value) => Value = value;

        public static implicit operator QualitySelector(string value) => new(value);
        public static implicit operator string(QualitySelector selector) => selector.Value;

        public static QualitySelector Best => new("best");
        public static QualitySelector Worst => new("worst");
        public static QualitySelector BestVideo => new("bestvideo");
        public static QualitySelector BestAudio => new("bestaudio");
    }

    public readonly record struct TimeRange(TimeSpan? Start = null, TimeSpan? End = null) {
        public bool IsValid => Start.HasValue || End.HasValue;
    }

    [Serializable]
    public class YtDlpSettings {
        [SerializeField] YtDlpSettingsData m_data = new();

        public VideoQuality VideoQuality => m_data.VideoQuality;
        public AudioQuality AudioQuality => m_data.AudioQuality;
        public QualitySelector CustomVideoQuality => m_data.CustomVideoQuality;
        public QualitySelector CustomAudioQuality => m_data.CustomAudioQuality;

        public BrowserType Browser => m_data.Browser;
        public string CustomBrowserPath => m_data.CustomBrowserPath;
        public string BrowserProfile => m_data.BrowserProfile;

        public OutputFormat OutputFormat => m_data.OutputFormat;
        public string CustomOutputFormat => m_data.CustomOutputFormat;

        public SubtitleFormat SubtitleFormat => m_data.SubtitleFormat;
        public List<string> SubtitleLanguages => new(m_data.SubtitleLanguages);

        public TimeSpan? Timeout => TimeSpan.FromMinutes( m_data.TimeoutMinutes );
        public TimeRange TimeRange => m_data.UseTimeRange ?
            new TimeRange( TimeSpan.FromSeconds( m_data.StartTimeSeconds ), TimeSpan.FromSeconds( m_data.EndTimeSeconds ) ) :
            new TimeRange();

        public bool ExtractAudioOnly => m_data.ExtractAudioOnly;
        public bool UsePlaylistIndex => m_data.UsePlaylistIndex;
        public PlaylistHandling PlaylistHandling => m_data.PlaylistHandling;
        public int MaxPlaylistItems => m_data.MaxPlaylistItems;
        public bool IgnoreErrors => m_data.IgnoreErrors;
        public bool UseHlsNative => m_data.UseHlsNative;
        public bool EmbedSubtitles => m_data.EmbedSubtitles;
        public bool WriteInfoJson => m_data.WriteInfoJson;

        public int? ConcurrentFragments => m_data.ConcurrentFragments > 0 ? m_data.ConcurrentFragments : null;
        public string RateLimit => m_data.RateLimit;
        public int? Retries => m_data.Retries > 0 ? m_data.Retries : null;

        public Dictionary<string, string> CustomArguments {
            get {
                Dictionary<string, string> dict = new();
                if ( !string.IsNullOrEmpty( m_data.CustomArguments ) ) {
                    string[] lines = m_data.CustomArguments.Split( '\n' );
                    foreach (string line in lines) {
                        string trimmed = line.Trim();
                        if ( string.IsNullOrEmpty( trimmed ) || trimmed.StartsWith( "#" ) ) continue;

                        string[] parts = trimmed.Split( '=', 2 );
                        if ( parts.Length == 2 ) {
                            dict[parts[0].Trim()] = parts[1].Trim();
                        }
                        else {
                            dict[trimmed] = "";
                        }
                    }
                }

                return dict;
            }
        }

        public YtDlpSettings() { }

        public YtDlpSettings(YtDlpSettingsData settingsData) => m_data = new YtDlpSettingsData( settingsData );

        public static YtDlpSettings CreateStreamingPreset() => new(YtDlpSettingsData.CreateStreaming());

        public static YtDlpSettings CreateDownloadPreset() => new(YtDlpSettingsData.CreateDownload());

        public static YtDlpSettings CreateAudioOnlyPreset() => new(YtDlpSettingsData.CreateAudioOnly());

        public static YtDlpSettings CreateHighQualityPreset() => new(YtDlpSettingsData.CreateHighQuality());

        public YtDlpSettings Clone() => new(new YtDlpSettingsData( m_data ));

        // Direct access to data for ScriptableObjects and Inspector
        public YtDlpSettingsData Data => m_data;

        // Simplified fluent API that modifies a copy
        public YtDlpSettings With(Action<YtDlpSettingsData> configure) {
            var newData = new YtDlpSettingsData( m_data );
            configure( newData );
            return new YtDlpSettings( newData );
        }
    }
}