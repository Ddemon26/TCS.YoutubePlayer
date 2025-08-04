using System.Linq;
namespace TCS.YoutubePlayer.Subtitles {
    [Serializable]
    public struct SubtitleEntry {
        public float StartTime { get; }
        public float EndTime { get; }
        public string Text { get; }
        public int Index { get; }

        public SubtitleEntry(int index, float startTime, float endTime, string text) {
            Index = index;
            StartTime = startTime;
            EndTime = endTime;
            Text = text?.Trim() ?? string.Empty;
        }

        public bool IsActiveAt(float currentTime) => 
            currentTime >= StartTime && currentTime <= EndTime;

        public float Duration => EndTime - StartTime;

        public override string ToString() => 
            $"[{Index}] {StartTime:F2}s-{EndTime:F2}s: {Text}";
    }

    [Serializable]
    public class SubtitleTrack {
        public string Language { get; }
        public string Format { get; } // "srt", "vtt", etc.
        public string FilePath { get; }
        public List<SubtitleEntry> Entries { get; }

        public SubtitleTrack(string language, string format, string filePath) {
            Language = language ?? "unknown";
            Format = format?.ToLower() ?? "unknown";
            FilePath = filePath ?? string.Empty;
            Entries = new List<SubtitleEntry>();
        }

        public SubtitleEntry? GetActiveEntry(float currentTime) {
            return Entries.FirstOrDefault(entry => entry.IsActiveAt(currentTime));
        }

        public SubtitleEntry? GetEntryByIndex(int index) {
            return Entries.FirstOrDefault(entry => entry.Index == index);
        }

        public void AddEntry(SubtitleEntry entry) {
            Entries.Add(entry);
        }

        public void SortEntries() {
            Entries.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
        }

        public int EntryCount => Entries.Count;
        public float TotalDuration => Entries.Count > 0 ? Entries.Max(e => e.EndTime) : 0f;

        public override string ToString() => 
            $"SubtitleTrack [{Language}] ({Format}) - {EntryCount} entries, {TotalDuration:F1}s";
    }
}