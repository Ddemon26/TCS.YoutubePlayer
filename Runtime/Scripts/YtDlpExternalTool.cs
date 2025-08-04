using System.Threading;
using TCS.YoutubePlayer.Configuration;
namespace TCS.YoutubePlayer {
    public static class YtDlpExternalTool {
        static readonly YtDlpService Service = new();
        static YtDlpSettings s_globalSettings;

        static YtDlpExternalTool() => Application.quitting += () => Service?.Dispose();

        public static void SetGlobalSettings(YtDlpSettings settings) {
            s_globalSettings = settings;
        }

        public static YtDlpSettings GetGlobalSettings() => s_globalSettings;

        public static string GetCacheTitle(string videoUrl) => Service.GetCacheTitle( videoUrl );

        public static Task<string> GetDirectUrlAsync(string videoUrl, CancellationToken cancellationToken) =>
            Service.GetDirectUrlAsync( videoUrl, s_globalSettings, cancellationToken );

        public static Task<string> GetDirectUrlAsync(string videoUrl, YtDlpSettings settings, CancellationToken cancellationToken) =>
            Service.GetDirectUrlAsync( videoUrl, settings, cancellationToken );

        public static Task<string> ConvertToMp4Async(string hlsUrl, CancellationToken cancellationToken) =>
            Service.ConvertToMp4Async( hlsUrl, s_globalSettings, cancellationToken );

        public static Task<string> ConvertToMp4Async(string hlsUrl, YtDlpSettings settings, CancellationToken cancellationToken) =>
            Service.ConvertToMp4Async( hlsUrl, settings, cancellationToken );

        public static Task<string> GetCurrentYtDlpVersionAsync(CancellationToken cancellationToken) =>
            Service.GetCurrentYtDlpVersionAsync( cancellationToken );

        public static Task<YtDlpUpdateResult> UpdateYtDlpAsync(CancellationToken cancellationToken) =>
            Service.UpdateYtDlpAsync( cancellationToken );

        public static Task PerformYtDlpUpdateCheckAsync(CancellationToken cancellationToken) =>
            Service.PerformYtDlpUpdateCheckAsync( cancellationToken );

        public static Task InitializeToolsAsync(CancellationToken cancellationToken = default) =>
            Service.InitializeToolsAsync( cancellationToken );
    }
}