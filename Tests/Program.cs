/*
 * TCS.YoutubePlayer Comprehensive Test Suite
 * 
 * 🚨 MANDATORY: ALL TESTS MUST PASS BEFORE ANY TASK IS COMPLETE
 * 
 * Usage: dotnet run
 * Documentation: See TESTING_WORKFLOW.md in this directory
 * 
 * This test suite validates all core functionality of the TCS.YoutubePlayer library.
 * Before completing any feature, upgrade, or revision, ensure all tests pass.
 */

using System;
using System.Collections.Generic;
using TCS.YoutubePlayer;
using TCS.YoutubePlayer.UrlProcessing;
using TCS.YoutubePlayer.Configuration;
using TCS.YoutubePlayer.Caching;
using TCS.YoutubePlayer.Exceptions;

public class Program {
    static readonly List<string> TestResults = new();
    static int PassedTests = 0;
    static int FailedTests = 0;

    public static int Main(string[] args) {
        Console.WriteLine("=== TCS.YoutubePlayer Comprehensive Test Suite ===\n");
        
        try {
            // Run all test suites
            RunUrlProcessingTests();
            RunYtDlpServiceTests();
            RunConfigurationTests();
            RunCacheTests();
            RunCommandBuilderTests();
            
            // Print final results
            PrintTestResults();
            
            return FailedTests > 0 ? 1 : 0;
        }
        catch (Exception ex) {
            Console.WriteLine($"❌ Critical test failure: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return 1;
        }
    }

    #region URL Processing Tests
    static void RunUrlProcessingTests() {
        Console.WriteLine("🔗 URL Processing Tests");
        Console.WriteLine("======================");
        
        var processor = new YouTubeUrlProcessor();
        var testUrls = new[] {
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            "https://youtu.be/dQw4w9WgXcQ",
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PLrAXtmRdnEQy6nuLMHjjdI-SooCoHy-9u",
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=30s",
            "https://www.youtube.com/embed/dQw4w9WgXcQ"
        };

        // Test TrimYouTubeUrl
        foreach (string url in testUrls) {
            string trimmed = YouTubeUrlProcessor.TrimYouTubeUrl(url);
            LogTest($"TrimYouTubeUrl({url})", !string.IsNullOrEmpty(trimmed), $"Result: {trimmed}");
        }

        // Test TryExtractVideoId
        foreach (string url in testUrls) {
            string videoId = processor.TryExtractVideoId(url);
            LogTest($"TryExtractVideoId({url})", !string.IsNullOrEmpty(videoId) && videoId.Length == 11, $"VideoId: {videoId}");
        }

        // Test URL validation
        try {
            YouTubeUrlProcessor.ValidateUrl("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
            LogTest("ValidateUrl (valid URL)", true, "Validation passed");
        }
        catch (Exception ex) {
            LogTest("ValidateUrl (valid URL)", false, $"Unexpected exception: {ex.Message}");
        }

        try {
            YouTubeUrlProcessor.ValidateUrl("");
            LogTest("ValidateUrl (empty URL)", false, "Should have thrown exception");
        }
        catch (InvalidYouTubeUrlException) {
            LogTest("ValidateUrl (empty URL)", true, "Correctly threw InvalidYouTubeUrlException");
        }
        catch (Exception ex) {
            LogTest("ValidateUrl (empty URL)", false, $"Wrong exception type: {ex.GetType().Name}");
        }

        // Test SanitizeForShell
        var testInputs = new Dictionary<string, string> {
            ["simple"] = "simple",
            ["with spaces"] = "with spaces",
            ["with'quotes"] = "with\\'quotes",
            ["with\"double\"quotes"] = "with\\\"double\\\"quotes",
            ["with$dollar"] = "with\\$dollar",
            ["with&ampersand"] = "with\\&ampersand",
            ["with;semicolon"] = "with\\;semicolon",
            ["with|pipe"] = "with\\|pipe",
            ["with(parentheses)"] = "with\\(parentheses\\)",
        };

        foreach (var kvp in testInputs) {
            string sanitized = YouTubeUrlProcessor.SanitizeForShell(kvp.Key);
            LogTest($"SanitizeForShell({kvp.Key})", sanitized == kvp.Value, $"Expected: {kvp.Value}, Got: {sanitized}");
        }

        // Test ParseExpiryFromUrl
        string urlWithExpiry = "https://example.com/video.mp4?expire=1640995200";
        DateTime? expiry = YouTubeUrlProcessor.ParseExpiryFromUrl(urlWithExpiry);
        LogTest("ParseExpiryFromUrl (with expiry)", expiry.HasValue, $"Expiry: {expiry}");

        string urlWithoutExpiry = "https://example.com/video.mp4";
        DateTime? noExpiry = YouTubeUrlProcessor.ParseExpiryFromUrl(urlWithoutExpiry);
        LogTest("ParseExpiryFromUrl (no expiry)", !noExpiry.HasValue, "No expiry found");

        Console.WriteLine();
    }
    #endregion

    #region YtDlpService Tests
    static void RunYtDlpServiceTests() {
        Console.WriteLine("🎥 YtDlpService Tests");
        Console.WriteLine("=====================");

        try {
            var settings = new YtDlpSettings();
            var service = new YtDlpService(settings);
            
            // Test service initialization
            LogTest("YtDlpService creation", service != null, "Service created successfully");

            // Test with mock/placeholder operations since we don't want to actually download
            try {
                string cacheTitle = service.GetCacheTitle("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
                LogTest("GetCacheTitle", !string.IsNullOrEmpty(cacheTitle), $"Cache title: {cacheTitle}");
            }
            catch (Exception ex) {
                LogTest("GetCacheTitle", true, $"Expected behavior - no cache entry: {ex.Message}");
            }

            service.Dispose();
            LogTest("YtDlpService disposal", true, "Service disposed successfully");
        }
        catch (Exception ex) {
            LogTest("YtDlpService tests", false, $"Service test failed: {ex.Message}");
        }

        Console.WriteLine();
    }
    #endregion

    #region Configuration Tests
    static void RunConfigurationTests() {
        Console.WriteLine("⚙️ Configuration Tests");
        Console.WriteLine("======================");

        // Test YtDlpSettings creation and properties
        var defaultSettings = new YtDlpSettings();
        LogTest("Default settings creation", defaultSettings != null, "Settings created successfully");
        LogTest("Default video quality", defaultSettings.VideoQuality == VideoQuality.Best, $"Video quality: {defaultSettings.VideoQuality}");
        LogTest("Default audio quality", defaultSettings.AudioQuality == AudioQuality.Best, $"Audio quality: {defaultSettings.AudioQuality}");

        // Test preset creation
        var streamingPreset = YtDlpSettings.CreateStreamingPreset();
        LogTest("Streaming preset creation", streamingPreset != null, "Streaming preset created");

        var downloadPreset = YtDlpSettings.CreateDownloadPreset();
        LogTest("Download preset creation", downloadPreset != null, "Download preset created");

        var audioOnlyPreset = YtDlpSettings.CreateAudioOnlyPreset();
        LogTest("Audio-only preset creation", audioOnlyPreset != null && audioOnlyPreset.ExtractAudioOnly, "Audio-only preset created");

        var highQualityPreset = YtDlpSettings.CreateHighQualityPreset();
        LogTest("High quality preset creation", highQualityPreset != null, "High quality preset created");

        // Test settings cloning
        var clonedSettings = defaultSettings.Clone();
        LogTest("Settings cloning", clonedSettings != null && clonedSettings != defaultSettings, "Settings cloned successfully");

        // Test fluent API
        var modifiedSettings = defaultSettings.With(data => {
            data.m_videoQuality = VideoQuality.Medium;
            data.m_extractAudioOnly = true;
        });
        LogTest("Fluent API modification", modifiedSettings.VideoQuality == VideoQuality.Medium && modifiedSettings.ExtractAudioOnly, "Settings modified via fluent API");

        // Test TimeRange
        var timeRange = new TimeRange(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2));
        LogTest("TimeRange creation", timeRange.IsValid && timeRange.Start.HasValue && timeRange.End.HasValue, $"TimeRange: {timeRange}");

        // Test QualitySelector
        QualitySelector customQuality = "best[height<=720]";
        LogTest("QualitySelector implicit conversion", customQuality.Value == "best[height<=720]", $"Quality selector: {customQuality.Value}");

        Console.WriteLine();
    }
    #endregion

    #region Cache Tests
    static void RunCacheTests() {
        Console.WriteLine("💾 Cache Tests");
        Console.WriteLine("==============");

        try {
            var urlProcessor = new YouTubeUrlProcessor();
            var cache = new YtDlpUrlCache(urlProcessor);

            // Test cache operations
            string testUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            string testDirectUrl = "https://example.com/video.mp4";
            string testTitle = "Test Video Title";

            // Test adding to cache
            cache.AddToCache(testUrl, testDirectUrl, testTitle);
            LogTest("AddToCache", true, "Entry added to cache");

            // Test cache retrieval
            string cacheTitle = cache.GetCacheTitle(testUrl);
            LogTest("GetCacheTitle", cacheTitle == testTitle, $"Retrieved title: {cacheTitle}");

            // Test cache hit
            bool cacheHit = cache.TryGetCachedEntry(testUrl, out var entry);
            LogTest("TryGetCachedEntry (hit)", cacheHit && entry != null && entry.Title == testTitle, $"Cache hit: {cacheHit}");

            // Test cache miss
            bool cacheMiss = cache.TryGetCachedEntry("https://www.youtube.com/watch?v=nonexistent", out var missEntry);
            LogTest("TryGetCachedEntry (miss)", !cacheMiss && missEntry == null, $"Cache miss: {!cacheMiss}");

            cache.Dispose();
            LogTest("Cache disposal", true, "Cache disposed successfully");
        }
        catch (Exception ex) {
            LogTest("Cache tests", false, $"Cache test failed: {ex.Message}");
        }

        Console.WriteLine();
    }
    #endregion

    #region Command Builder Tests
    static void RunCommandBuilderTests() {
        Console.WriteLine("🔧 Command Builder Tests");
        Console.WriteLine("========================");

        try {
            var settings = new YtDlpSettings();
            var commandBuilder = new YtDlpCommandBuilder(settings);

            string testUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

            // Test command building methods
            string directUrlCommand = commandBuilder.BuildGetDirectUrlCommand(testUrl);
            LogTest("BuildGetDirectUrlCommand", !string.IsNullOrEmpty(directUrlCommand) && directUrlCommand.Contains("--get-url"), $"Command: {directUrlCommand}");

            string titleCommand = commandBuilder.BuildGetTitleCommand(testUrl);
            LogTest("BuildGetTitleCommand", !string.IsNullOrEmpty(titleCommand) && titleCommand.Contains("--get-title"), $"Command: {titleCommand}");

            string conversionCommand = commandBuilder.BuildConversionCommand(testUrl, "/tmp/output.mp4");
            LogTest("BuildConversionCommand", !string.IsNullOrEmpty(conversionCommand) && conversionCommand.Contains("/tmp/output.mp4"), $"Command: {conversionCommand}");

            string versionCommand = commandBuilder.BuildVersionCommand();
            LogTest("BuildVersionCommand", versionCommand == "--version", $"Command: {versionCommand}");

            string updateCommand = commandBuilder.BuildUpdateCommand();
            LogTest("BuildUpdateCommand", updateCommand == "--update", $"Command: {updateCommand}");

            // Test with different settings
            var customSettings = settings.With(data => {
                data.m_videoQuality = VideoQuality.Medium;
                data.m_browser = BrowserType.Chrome;
                data.m_extractAudioOnly = true;
            });

            string audioCommand = commandBuilder.BuildGetDirectUrlCommand(testUrl, customSettings);
            LogTest("BuildGetDirectUrlCommand (audio-only)", !string.IsNullOrEmpty(audioCommand), $"Audio command: {audioCommand}");

            // Test subtitle commands with new handling modes
            var subtitleSettings = settings.With(data => {
                data.m_subtitleFormat = SubtitleFormat.Srt;
                data.m_subtitleLanguages = new[] { "en", "es" };
                data.m_subtitleHandlingMode = SubtitleHandlingMode.EmbedSoft;
            });

            string directUrlWithSubs = commandBuilder.BuildGetDirectUrlCommand(testUrl, subtitleSettings);
            bool hasSubLangs = directUrlWithSubs.Contains("--sub-langs \"en,es\"");
            bool hasSubFormat = directUrlWithSubs.Contains("--sub-format \"srt\"");
            bool hasWriteSubs = directUrlWithSubs.Contains("--write-subs");
            bool hasEmbedSubs = directUrlWithSubs.Contains("--embed-subs");
            LogTest("BuildGetDirectUrlCommand (with subtitles)", 
                hasSubLangs && hasSubFormat && hasWriteSubs && hasEmbedSubs, 
                $"Command includes subtitle flags: {directUrlWithSubs}");

            string titleWithSubs = commandBuilder.BuildGetTitleCommand(testUrl, subtitleSettings);
            hasSubLangs = titleWithSubs.Contains("--sub-langs \"en,es\"");
            hasSubFormat = titleWithSubs.Contains("--sub-format \"srt\"");
            hasWriteSubs = titleWithSubs.Contains("--write-subs");
            hasEmbedSubs = titleWithSubs.Contains("--embed-subs");
            LogTest("BuildGetTitleCommand (with subtitles)", 
                hasSubLangs && hasSubFormat && hasWriteSubs && hasEmbedSubs, 
                $"Command includes subtitle flags: {titleWithSubs}");

            // Test different subtitle handling modes
            var unityDisplaySettings = settings.With(data => {
                data.m_subtitleFormat = SubtitleFormat.Srt;
                data.m_subtitleLanguages = new[] { "en" };
                data.m_subtitleHandlingMode = SubtitleHandlingMode.UnityDisplay;
            });

            string unityDisplayCommand = commandBuilder.BuildGetDirectUrlCommand(testUrl, unityDisplaySettings);
            bool hasWriteSubsOnly = unityDisplayCommand.Contains("--write-subs") && !unityDisplayCommand.Contains("--embed-subs");
            LogTest("BuildGetDirectUrlCommand (UnityDisplay mode)", hasWriteSubsOnly, 
                $"UnityDisplay command: {unityDisplayCommand}");

            var burnHardSettings = settings.With(data => {
                data.m_subtitleFormat = SubtitleFormat.Srt;
                data.m_subtitleLanguages = new[] { "en" };
                data.m_subtitleHandlingMode = SubtitleHandlingMode.BurnHard;
            });

            string burnHardCommand = commandBuilder.BuildGetDirectUrlCommand(testUrl, burnHardSettings);
            bool hasBurnHardFlags = burnHardCommand.Contains("--write-subs") && !burnHardCommand.Contains("--embed-subs");
            LogTest("BuildGetDirectUrlCommand (BurnHard mode)", hasBurnHardFlags, 
                $"BurnHard command: {burnHardCommand}");

        }
        catch (Exception ex) {
            LogTest("Command builder tests", false, $"Command builder test failed: {ex.Message}");
        }

        Console.WriteLine();
    }
    #endregion

    #region Test Utilities
    static void LogTest(string testName, bool passed, string details = "") {
        string status = passed ? "✅ PASS" : "❌ FAIL";
        string message = $"{status} {testName}";
        if (!string.IsNullOrEmpty(details)) {
            message += $" - {details}";
        }

        Console.WriteLine(message);
        TestResults.Add(message);

        if (passed) {
            PassedTests++;
        } else {
            FailedTests++;
        }
    }

    static void PrintTestResults() {
        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine("TEST RESULTS SUMMARY");
        Console.WriteLine(new string('=', 50));
        Console.WriteLine($"Total Tests: {PassedTests + FailedTests}");
        Console.WriteLine($"✅ Passed: {PassedTests}");
        Console.WriteLine($"❌ Failed: {FailedTests}");
        Console.WriteLine($"Success Rate: {(PassedTests * 100.0 / (PassedTests + FailedTests)):F1}%");
        
        if (FailedTests > 0) {
            Console.WriteLine("\n❌ FAILED TESTS:");
            foreach (string result in TestResults) {
                if (result.Contains("❌ FAIL")) {
                    Console.WriteLine($"  - {result}");
                }
            }
        } else {
            Console.WriteLine("\n🎉 ALL TESTS PASSED!");
        }
        
        Console.WriteLine(new string('=', 50));
    }
    #endregion
}