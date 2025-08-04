using System.Linq;
using System.Text.RegularExpressions;

namespace TCS.YoutubePlayer.Subtitles {
    public static class SubtitleParser {
        static readonly Regex SrtTimeRegex = new(@"(\d{2}):(\d{2}):(\d{2}),(\d{3}) --> (\d{2}):(\d{2}):(\d{2}),(\d{3})", RegexOptions.Compiled);
        static readonly Regex VttTimeRegex = new(@"(\d{2}):(\d{2}):(\d{2})\.(\d{3}) --> (\d{2}):(\d{2}):(\d{2})\.(\d{3})", RegexOptions.Compiled);
        static readonly Regex HtmlTagRegex = new(@"<[^>]*>", RegexOptions.Compiled);

        public static async Task<SubtitleTrack> ParseFileAsync(string filePath, string language = "unknown") {
            if (!File.Exists(filePath)) {
                throw new FileNotFoundException($"Subtitle file not found: {filePath}");
            }

            string extension = Path.GetExtension(filePath).ToLower();
            string format = extension switch {
                ".srt" => "srt",
                ".vtt" => "vtt",
                ".ass" => "ass",
                _ => "unknown",
            };

            var track = new SubtitleTrack(language, format, filePath);
            string content = await File.ReadAllTextAsync(filePath);

            switch (format) {
                case "srt":
                    ParseSrt(content, track);
                    break;
                case "vtt":
                    ParseVtt(content, track);
                    break;
                case "ass":
                    ParseAss(content, track);
                    break;
                default:
                    Logger.LogWarning($"Unsupported subtitle format: {format}");
                    break;
            }

            track.SortEntries();
            Logger.Log($"Parsed subtitle file: {track}");
            return track;
        }

        static void ParseSrt(string content, SubtitleTrack track) {
            string[] blocks = content.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string block in blocks) {
                string[] lines = block.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 3) continue;

                // Parse index
                if (!int.TryParse(lines[0].Trim(), out int index)) continue;

                // Parse timing
                var timeMatch = SrtTimeRegex.Match(lines[1]);
                if (!timeMatch.Success) continue;

                float startTime = ParseSrtTime(timeMatch, 1);
                float endTime = ParseSrtTime(timeMatch, 5);

                // Parse text (lines 2+)
                string text = string.Join("\n", lines.Skip(2))
                    .Replace("\\N", "\n")  // Handle line breaks
                    .Trim();

                // Remove basic HTML tags
                text = HtmlTagRegex.Replace(text, "");

                track.AddEntry(new SubtitleEntry(index, startTime, endTime, text));
            }
        }

        static void ParseVtt(string content, SubtitleTrack track) {
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var foundHeader = false;
            var currentIndex = 1;

            for (var i = 0; i < lines.Length; i++) {
                string line = lines[i].Trim();

                // Skip until we find WEBVTT header or first timing line
                if (!foundHeader) {
                    if (line.StartsWith("WEBVTT") || VttTimeRegex.IsMatch(line)) {
                        foundHeader = true;
                    }
                    if (!VttTimeRegex.IsMatch(line)) continue;
                }

                // Parse timing line
                var timeMatch = VttTimeRegex.Match(line);
                if (!timeMatch.Success) continue;

                float startTime = ParseVttTime(timeMatch, 1);
                float endTime = ParseVttTime(timeMatch, 5);

                // Collect text lines until next timing or end
                List<string> textLines = new();
                for (int j = i + 1; j < lines.Length; j++) {
                    string textLine = lines[j].Trim();
                    if (string.IsNullOrEmpty(textLine) || VttTimeRegex.IsMatch(textLine)) {
                        i = j - 1; // Set outer loop position
                        break;
                    }
                    textLines.Add(textLine);
                }

                if (textLines.Count > 0) {
                    string text = string.Join("\n", textLines);
                    text = HtmlTagRegex.Replace(text, ""); // Remove HTML tags
                    track.AddEntry(new SubtitleEntry(currentIndex++, startTime, endTime, text));
                }
            }
        }

        static void ParseAss(string content, SubtitleTrack track) {
            // Basic ASS/SSA parsing - this format is more complex
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var inEventsSection = false;
            int textColumnIndex = -1;
            int startColumnIndex = -1;
            int endColumnIndex = -1;
            var currentIndex = 1;

            foreach (string line in lines) {
                string trimmedLine = line.Trim();

                if (trimmedLine == "[Events]") {
                    inEventsSection = true;
                    continue;
                }

                if (trimmedLine.StartsWith("[") && trimmedLine != "[Events]") {
                    inEventsSection = false;
                    continue;
                }

                if (!inEventsSection) continue;

                if (trimmedLine.StartsWith("Format:")) {
                    // Parse column format
                    string[] columns = trimmedLine.Substring(7).Split(',');
                    for (var i = 0; i < columns.Length; i++) {
                        string column = columns[i].Trim().ToLower();
                        switch (column) {
                            case "start": startColumnIndex = i; break;
                            case "end": endColumnIndex = i; break;
                            case "text": textColumnIndex = i; break;
                        }
                    }
                    continue;
                }

                if (trimmedLine.StartsWith("Dialogue:")) {
                    string[] parts = trimmedLine.Substring(9).Split(',');
                    if (parts.Length <= Math.Max(textColumnIndex, Math.Max(startColumnIndex, endColumnIndex))) continue;

                    if (startColumnIndex >= 0 && endColumnIndex >= 0 && textColumnIndex >= 0) {
                        float startTime = ParseAssTime(parts[startColumnIndex]);
                        float endTime = ParseAssTime(parts[endColumnIndex]);
                        
                        // Text might contain commas, so rejoin from textColumnIndex onwards
                        string text = string.Join(",", parts.Skip(textColumnIndex))
                            .Replace("\\N", "\n")  // ASS line break
                            .Replace("\\n", "\n"); // Alternative line break

                        // Remove ASS tags (basic removal)
                        text = Regex.Replace(text, @"\{[^}]*\}", "");

                        track.AddEntry(new SubtitleEntry(currentIndex++, startTime, endTime, text.Trim()));
                    }
                }
            }
        }

        static float ParseSrtTime(Match match, int groupOffset) {
            int hours = int.Parse(match.Groups[groupOffset].Value);
            int minutes = int.Parse(match.Groups[groupOffset + 1].Value);
            int seconds = int.Parse(match.Groups[groupOffset + 2].Value);
            int milliseconds = int.Parse(match.Groups[groupOffset + 3].Value);

            return hours * 3600f + minutes * 60f + seconds + milliseconds / 1000f;
        }

        static float ParseVttTime(Match match, int groupOffset) {
            int hours = int.Parse(match.Groups[groupOffset].Value);
            int minutes = int.Parse(match.Groups[groupOffset + 1].Value);
            int seconds = int.Parse(match.Groups[groupOffset + 2].Value);
            int milliseconds = int.Parse(match.Groups[groupOffset + 3].Value);

            return hours * 3600f + minutes * 60f + seconds + milliseconds / 1000f;
        }

        static float ParseAssTime(string timeString) {
            // ASS format: H:MM:SS.CC (centiseconds)
            string[] parts = timeString.Split(':');
            if (parts.Length != 3) return 0f;

            int hours = int.Parse(parts[0]);
            int minutes = int.Parse(parts[1]);
            string[] secParts = parts[2].Split('.');
            int seconds = int.Parse(secParts[0]);
            int centiseconds = secParts.Length > 1 ? int.Parse(secParts[1]) : 0;

            return hours * 3600f + minutes * 60f + seconds + centiseconds / 100f;
        }

        public static List<string> FindSubtitleFiles(string videoFilePath) {
            if (string.IsNullOrEmpty(videoFilePath)) return new List<string>();

            string directory = Path.GetDirectoryName(videoFilePath);
            string baseName = Path.GetFileNameWithoutExtension(videoFilePath);
            
            if (string.IsNullOrEmpty(directory)) return new List<string>();

            List<string> subtitleFiles = new();
            string[] extensions = { ".srt", ".vtt", ".ass" };

            foreach (string ext in extensions) {
                // Look for files like "video.srt", "video.en.srt", etc.
                string[] patterns = {
                    $"{baseName}{ext}",
                    $"{baseName}.*{ext}",
                };

                foreach (string pattern in patterns) {
                    try {
                        string[] files = Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly);
                        subtitleFiles.AddRange(files);
                    }
                    catch (Exception ex) {
                        Logger.LogWarning($"Error searching for subtitle files: {ex.Message}");
                    }
                }
            }

            return subtitleFiles.Distinct().ToList();
        }
    }
}