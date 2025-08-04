using UnityEngine.Serialization;
namespace TCS.YoutubePlayer.Configuration {
    public enum VideoQuality { Worst, Low, Medium, High, Best, Custom, }

    public enum AudioQuality { Worst, Low, Medium, High, Best, Custom, }

    public enum BrowserType { None, Chrome, Firefox, Edge, Safari, Brave, Chromium, Custom, }

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
        [SerializeField] YtDlpSettingsData m_ytDlpSettings = new();
        
        public string GetSettingsSummary() => m_ytDlpSettings.GetSettingsSummary();

        public VideoQuality VideoQuality => m_ytDlpSettings.VideoQuality;
        public AudioQuality AudioQuality => m_ytDlpSettings.AudioQuality;
        public QualitySelector CustomVideoQuality => m_ytDlpSettings.CustomVideoQuality;
        public QualitySelector CustomAudioQuality => m_ytDlpSettings.CustomAudioQuality;

        public BrowserType Browser => m_ytDlpSettings.Browser;
        public string CustomBrowserPath => m_ytDlpSettings.CustomBrowserPath;
        public string BrowserProfile => m_ytDlpSettings.BrowserProfile;

        public OutputFormat OutputFormat => m_ytDlpSettings.OutputFormat;
        public string CustomOutputFormat => m_ytDlpSettings.CustomOutputFormat;

        public TimeSpan? Timeout => TimeSpan.FromMinutes( m_ytDlpSettings.TimeoutMinutes );
        public TimeRange TimeRange => m_ytDlpSettings.UseTimeRange ?
            new TimeRange( TimeSpan.FromSeconds( m_ytDlpSettings.StartTimeSeconds ), TimeSpan.FromSeconds( m_ytDlpSettings.EndTimeSeconds ) ) :
            new TimeRange();

        public bool ExtractAudioOnly => m_ytDlpSettings.ExtractAudioOnly;
        public bool UsePlaylistIndex => m_ytDlpSettings.UsePlaylistIndex;
        public PlaylistHandling PlaylistHandling => m_ytDlpSettings.PlaylistHandling;
        public int MaxPlaylistItems => m_ytDlpSettings.MaxPlaylistItems;
        public bool IgnoreErrors => m_ytDlpSettings.IgnoreErrors;
        public bool UseHlsNative => m_ytDlpSettings.UseHlsNative;
        public bool WriteInfoJson => m_ytDlpSettings.WriteInfoJson;

        public int? ConcurrentFragments => m_ytDlpSettings.ConcurrentFragments > 0 ? m_ytDlpSettings.ConcurrentFragments : null;
        public string RateLimit => m_ytDlpSettings.RateLimit;
        public int? Retries => m_ytDlpSettings.Retries > 0 ? m_ytDlpSettings.Retries : null;

        public Dictionary<string, string> CustomArguments {
            get {
                Dictionary<string, string> dict = new();
                if ( !string.IsNullOrEmpty( m_ytDlpSettings.CustomArguments ) ) {
                    string[] lines = m_ytDlpSettings.CustomArguments.Split( '\n' );
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

        public YtDlpSettings(YtDlpSettingsData settingsData) => m_ytDlpSettings = new YtDlpSettingsData( settingsData );

        public static YtDlpSettings CreateStreamingPreset() => new(YtDlpSettingsData.CreateStreaming());

        public static YtDlpSettings CreateDownloadPreset() => new(YtDlpSettingsData.CreateDownload());

        public static YtDlpSettings CreateAudioOnlyPreset() => new(YtDlpSettingsData.CreateAudioOnly());

        public static YtDlpSettings CreateHighQualityPreset() => new(YtDlpSettingsData.CreateHighQuality());

        public YtDlpSettings Clone() => new(new YtDlpSettingsData( m_ytDlpSettings ));

        public YtDlpSettings With(Action<YtDlpSettingsData> configure) {
            var newData = new YtDlpSettingsData( m_ytDlpSettings );
            configure( newData );
            return new YtDlpSettings( newData );
        }
    }
}