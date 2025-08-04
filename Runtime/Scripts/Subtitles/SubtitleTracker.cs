using System.Linq;
using UnityEngine.Video;
using UnityEngine.Events;

namespace TCS.YoutubePlayer.Subtitles {
    /// <summary>
    /// Unity component for tracking and displaying subtitles synchronized with video playback.
    /// Subscribe to subtitle events to display them in your custom GUI.
    /// </summary>
    public class SubtitleTracker : MonoBehaviour {
        [Header("Subtitle Settings")]
        [SerializeField] bool m_autoSync = true;
        [SerializeField] float m_timeOffset = 0f;
        [SerializeField] string m_preferredLanguage = "en";
        
        [Header("Debug")]
        [SerializeField] bool m_debugMode = false;

        public UnityEvent<SubtitleEntry> OnSubtitleChanged;
        public UnityEvent OnSubtitleCleared;
        public UnityEvent<SubtitleTrack> OnTrackLoaded;

        readonly List<SubtitleTrack> m_loadedTracks = new();
        SubtitleTrack m_activeTrack;
        SubtitleEntry? m_currentEntry;
        VideoPlayer m_videoPlayer;
        
        bool m_isPlaying = false;
        float m_lastUpdateTime = -1f;

        public bool IsTrackLoaded => m_activeTrack != null;
        public int LoadedTrackCount => m_loadedTracks.Count;
        public SubtitleTrack ActiveTrack => m_activeTrack;
        public SubtitleEntry? CurrentEntry => m_currentEntry;
        public float TimeOffset { get => m_timeOffset; set => m_timeOffset = value; }
        public string PreferredLanguage { get => m_preferredLanguage; set => m_preferredLanguage = value; }

        void Awake() {
            // Try to find VideoPlayer component
            m_videoPlayer = GetComponent<VideoPlayer>() ?? GetComponentInParent<VideoPlayer>();
            
            if (m_videoPlayer == null) {
                Logger.LogWarning("SubtitleTracker: No VideoPlayer found. Manual time sync required.");
            }
        }

        void Update() {
            if (!m_autoSync || !IsTrackLoaded) return;

            float currentTime = GetCurrentPlaybackTime();
            if (Mathf.Abs(currentTime - m_lastUpdateTime) < 0.1f) return; // Update max 10 times per second
            
            m_lastUpdateTime = currentTime;
            UpdateSubtitle(currentTime);
        }

        async Task<bool> LoadSubtitleFileAsync(string filePath, string language = null) {
            try {
                string lang = language ?? ExtractLanguageFromFilename(filePath) ?? m_preferredLanguage;
                var track = await SubtitleParser.ParseFileAsync(filePath, lang);
                
                AddTrack(track);
                Logger.Log($"SubtitleTracker: Loaded subtitle track: {track}");
                return true;
            }
            catch (Exception ex) {
                Logger.LogError($"SubtitleTracker: Failed to load subtitle file '{filePath}': {ex.Message}");
                return false;
            }
        }

        public async Task<int> LoadSubtitleFilesAsync(IEnumerable<string> filePaths) {
            var loadedCount = 0;
            IEnumerable<Task> tasks = filePaths.Select(async filePath => {
                bool success = await LoadSubtitleFileAsync(filePath);
                if (success) loadedCount++;
            });
            
            await Task.WhenAll(tasks);
            return loadedCount;
        }

        public void AddTrack(SubtitleTrack track) {
            if (track == null) return;
            
            m_loadedTracks.Add(track);
            OnTrackLoaded?.Invoke(track);
            
            // Auto-select first track or preferred language
            if (m_activeTrack == null || track.Language == m_preferredLanguage) {
                SetActiveTrack(track);
            }
        }

        public void SetActiveTrack(SubtitleTrack track) {
            if (track == null || !m_loadedTracks.Contains(track)) return;
            
            m_activeTrack = track;
            ClearCurrentSubtitle();
            
            Logger.Log($"SubtitleTracker: Active track set to {track.Language} ({track.Format})");
        }

        public void SetActiveTrack(string language) {
            var track = m_loadedTracks.FirstOrDefault(t => 
                t.Language.Equals(language, StringComparison.OrdinalIgnoreCase));
            
            if (track != null) {
                SetActiveTrack(track);
            } else {
                Logger.LogWarning($"SubtitleTracker: No track found for language '{language}'");
            }
        }

        public void UpdateSubtitle(float currentTime) {
            if (!IsTrackLoaded) return;
            
            float adjustedTime = currentTime + m_timeOffset;
            SubtitleEntry? newEntry = m_activeTrack.GetActiveEntry(adjustedTime);
            
            // Check if subtitle changed
            if (!SubtitleEntriesEqual(newEntry, m_currentEntry)) {
                m_currentEntry = newEntry;
                
                if (newEntry.HasValue) {
                    if (m_debugMode) {
                        Logger.Log($"SubtitleTracker: Showing subtitle at {adjustedTime:F2}s: {newEntry.Value.Text}");
                    }
                    OnSubtitleChanged?.Invoke(newEntry.Value);
                } else {
                    if (m_debugMode && m_currentEntry.HasValue) {
                        Logger.Log($"SubtitleTracker: Cleared subtitle at {adjustedTime:F2}s");
                    }
                    OnSubtitleCleared?.Invoke();
                }
            }
        }

        public void SeekTo(float time) {
            if (!IsTrackLoaded) return;
            
            ClearCurrentSubtitle();
            UpdateSubtitle(time);
        }

        public void ClearCurrentSubtitle() {
            if (m_currentEntry.HasValue) {
                m_currentEntry = null;
                OnSubtitleCleared?.Invoke();
            }
        }

        public void ClearAllTracks() {
            m_loadedTracks.Clear();
            m_activeTrack = null;
            ClearCurrentSubtitle();
            Logger.Log("SubtitleTracker: All tracks cleared");
        }

        public List<string> GetAvailableLanguages() {
            return m_loadedTracks.Select(t => t.Language).Distinct().ToList();
        }

        public SubtitleTrack GetTrackByLanguage(string language) {
            return m_loadedTracks.FirstOrDefault(t => 
                t.Language.Equals(language, StringComparison.OrdinalIgnoreCase));
        }

        float GetCurrentPlaybackTime() {
            if (m_videoPlayer != null && m_videoPlayer.isPlaying) {
                return (float)m_videoPlayer.time;
            }
            
            // Fallback to manual time tracking
            return Time.time;
        }

        static bool SubtitleEntriesEqual(SubtitleEntry? a, SubtitleEntry? b) {
            if (!a.HasValue && !b.HasValue) return true;
            if (!a.HasValue || !b.HasValue) return false;
            return a.Value.Index == b.Value.Index;
        }

        static string ExtractLanguageFromFilename(string filePath) {
            string filename = Path.GetFileNameWithoutExtension(filePath);
            string[] parts = filename.Split('.');
            
            // Look for language codes like "video.en.srt", "video.es.vtt"
            if (parts.Length >= 2) {
                string lastPart = parts[^1];
                if (lastPart.Length == 2) { // Assume 2-letter language codes
                    return lastPart;
                }
            }
            
            return null;
        }

        #if UNITY_EDITOR
        [ContextMenu("Debug Current Subtitle")]
        void DebugCurrentSubtitle() {
            if (m_currentEntry.HasValue) {
                Debug.Log($"Current Subtitle: {m_currentEntry.Value}");
            } else {
                Debug.Log("No current subtitle");
            }
        }

        [ContextMenu("List All Subtitles")]
        void DebugAllSubtitles() {
            if (!IsTrackLoaded) {
                Debug.Log("No subtitle track loaded");
                return;
            }
            
            Debug.Log($"Subtitle Track: {m_activeTrack}");
            foreach (var entry in m_activeTrack.Entries) {
                Debug.Log($"  {entry}");
            }
        }
        #endif
    }
}