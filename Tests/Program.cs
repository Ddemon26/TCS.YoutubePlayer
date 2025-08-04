using System;

public class Program {
    public static int Main(string[] args) {
        // Example usage of the methods from the snippets
        string url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        
        // Trim YouTube URL
        string trimmedUrl = TCS.YoutubePlayer.UrlProcessing.YouTubeUrlProcessor.TrimYouTubeUrl(url);
        System.Console.WriteLine($"Trimmed URL: {trimmedUrl}");
        
        // // Extract Video ID
        // string videoId = TCS.YoutubePlayer.UrlProcessing.YouTubeUrlProcessor.TryExtractVideoId(url);
        // System.Console.WriteLine($"Video ID: {videoId}");
        
        // Parse expiry from URL
        DateTime? expiryDate = TCS.YoutubePlayer.UrlProcessing.YouTubeUrlProcessor.ParseExpiryFromUrl(url);
        System.Console.WriteLine($"Expiry Date: {expiryDate}");
        
        // Sanitize for shell
        string sanitizedInput = TCS.YoutubePlayer.UrlProcessing.YouTubeUrlProcessor.SanitizeForShell("example's input; with & special $ characters");
        System.Console.WriteLine($"Sanitized Input: {sanitizedInput}");
        
        return 0; // Indicate successful execution
    }
}