using System.IO;

namespace TCS.YoutubePlayer.Configuration {
    [Serializable] public class YtDlpProfile {
        public string Name { get; set; }
        public string Description { get; set; }
        public YtDlpSettings Settings { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }

        public YtDlpProfile() {
            CreatedAt = DateTime.Now;
            ModifiedAt = DateTime.Now;
        }

        public YtDlpProfile(string name, string description, YtDlpSettings settings) : this() {
            Name = name;
            Description = description;
            Settings = settings?.Clone();
        }

        public void UpdateSettings(YtDlpSettings newSettings) {
            Settings = newSettings?.Clone();
            ModifiedAt = DateTime.Now;
        }
    }

    public interface IYtDlpProfileManager {
        IReadOnlyDictionary<string, YtDlpProfile> Profiles { get; }
        YtDlpProfile GetProfile(string name);
        void SaveProfile(string name, YtDlpSettings settings, string description = "");
        void SaveProfile(YtDlpProfile profile);
        bool DeleteProfile(string name);
        void LoadProfiles();
        void SaveProfiles();
        YtDlpProfile GetDefaultProfile();
    }

    public class YtDlpProfileManager : IYtDlpProfileManager {
        const string PROFILES_DIRECTORY = "YtDlpProfiles";
        const string PROFILES_FILE = "profiles.json";

        readonly Dictionary<string, YtDlpProfile> m_profiles = new();
        readonly string m_profilesPath;

        public IReadOnlyDictionary<string, YtDlpProfile> Profiles => m_profiles;

        public YtDlpProfileManager() {
            m_profilesPath = Path.Combine( Application.persistentDataPath, PROFILES_DIRECTORY );
            EnsureDirectoryExists();
            LoadBuiltInProfiles();
            LoadProfiles();
        }

        public YtDlpProfile GetProfile(string name) {
            if ( string.IsNullOrEmpty( name ) ) return GetDefaultProfile();

            m_profiles.TryGetValue( name, out var profile );
            return profile ?? GetDefaultProfile();
        }

        public void SaveProfile(string name, YtDlpSettings settings, string description = "") {
            if ( string.IsNullOrEmpty( name ) || settings == null ) return;

            var profile = new YtDlpProfile( name, description, settings );
            SaveProfile( profile );
        }

        public void SaveProfile(YtDlpProfile profile) {
            if ( profile == null || string.IsNullOrEmpty( profile.Name ) ) return;

            if ( m_profiles.ContainsKey( profile.Name ) ) {
                profile.ModifiedAt = DateTime.Now;
            }

            m_profiles[profile.Name] = profile;
            SaveProfiles();
        }

        public bool DeleteProfile(string name) {
            if ( string.IsNullOrEmpty( name ) || IsBuiltInProfile( name ) ) return false;

            bool removed = m_profiles.Remove( name );
            if ( removed ) {
                SaveProfiles();
            }

            return removed;
        }

        public void LoadProfiles() {
            try {
                string filePath = Path.Combine( m_profilesPath, PROFILES_FILE );
                if ( !File.Exists( filePath ) ) return;

                string json = File.ReadAllText( filePath );
                var profileData = JsonUtility.FromJson<SerializableProfileCollection>( json );

                if ( profileData?.m_profiles != null ) {
                    foreach (var profile in profileData.m_profiles) {
                        if ( !string.IsNullOrEmpty( profile.Name ) ) {
                            m_profiles[profile.Name] = profile;
                        }
                    }
                }
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to load YtDlp profiles: {e.Message}" );
            }
        }

        public void SaveProfiles() {
            try {
                List<YtDlpProfile> userProfiles = new();
                foreach (var profile in m_profiles.Values) {
                    if ( !IsBuiltInProfile( profile.Name ) ) {
                        userProfiles.Add( profile );
                    }
                }

                var profileData = new SerializableProfileCollection { m_profiles = userProfiles };
                string json = JsonUtility.ToJson( profileData, true );
                string filePath = Path.Combine( m_profilesPath, PROFILES_FILE );
                File.WriteAllText( filePath, json );
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to save YtDlp profiles: {e.Message}" );
            }
        }

        public YtDlpProfile GetDefaultProfile() {
            return GetProfile( "Default" ) ?? CreateDefaultProfile();
        }

        void LoadBuiltInProfiles() {
            var defaultProfile = new YtDlpProfile(
                "Default",
                "Default settings for general use",
                new YtDlpSettings()
            );

            var streamingProfile = new YtDlpProfile(
                "Streaming",
                "Optimized for fast streaming playback",
                YtDlpSettings.CreateStreamingPreset()
            );

            var downloadProfile = new YtDlpProfile(
                "Download",
                "Best quality for local downloads",
                YtDlpSettings.CreateDownloadPreset()
            );

            var audioOnlyProfile = new YtDlpProfile(
                "AudioOnly",
                "Extract audio only",
                YtDlpSettings.CreateAudioOnlyPreset()
            );

            var highQualityProfile = new YtDlpProfile(
                "HighQuality",
                "High quality with subtitles",
                YtDlpSettings.CreateHighQualityPreset()
            );

            var quickStreamProfile = new YtDlpProfile(
                "QuickStream",
                "Fast streaming with medium quality",
                new YtDlpSettings()
                    .WithVideoQuality( VideoQuality.Medium )
                    .WithTimeout( TimeSpan.FromMinutes( 1 ) )
                    .WithNetworkSettings( concurrentFragments: 2 )
            );

            var cookieStreamProfile = new YtDlpProfile(
                "WithCookies",
                "Streaming with browser cookies for private videos",
                YtDlpSettings.CreateStreamingPreset()
                    .WithBrowser( BrowserType.Firefox )
            );

            m_profiles["Default"] = defaultProfile;
            m_profiles["Streaming"] = streamingProfile;
            m_profiles["Download"] = downloadProfile;
            m_profiles["AudioOnly"] = audioOnlyProfile;
            m_profiles["HighQuality"] = highQualityProfile;
            m_profiles["QuickStream"] = quickStreamProfile;
            m_profiles["WithCookies"] = cookieStreamProfile;
        }

        YtDlpProfile CreateDefaultProfile() {
            return new YtDlpProfile( "Default", "Default settings", new YtDlpSettings() );
        }

        void EnsureDirectoryExists() {
            if ( !Directory.Exists( m_profilesPath ) ) {
                Directory.CreateDirectory( m_profilesPath );
            }
        }

        bool IsBuiltInProfile(string name) {
            return name switch {
                "Default" or "Streaming" or "Download" or "AudioOnly" or "HighQuality"
                    or "QuickStream" or "WithCookies" => true,
                _ => false,
            };
        }

        [Serializable]
        class SerializableProfileCollection {
            public List<YtDlpProfile> m_profiles;
        }
    }

    public static class YtDlpProfileExtensions {
        public static YtDlpSettings GetSettingsFromProfile(this IYtDlpProfileManager manager, string profileName) {
            var profile = manager.GetProfile( profileName );
            return profile?.Settings?.Clone() ?? new YtDlpSettings();
        }

        public static YtDlpProfile CreateProfile(this YtDlpSettings settings, string name, string description = "") {
            return new YtDlpProfile( name, description, settings );
        }
    }
}