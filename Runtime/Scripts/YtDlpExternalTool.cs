using System.Threading;
using System.Threading.Tasks;
namespace TCS.YoutubePlayer {
    public static class YtDlpExternalTool {
        static readonly YtDlpService Service = new();

        static YtDlpExternalTool()
            => Application.quitting += () => Service?.Dispose();

        public static string GetCacheTitle(string videoUrl) => 
            Service.GetCacheTitle(videoUrl);

        public static Task<string> GetDirectUrlAsync(string videoUrl, CancellationToken cancellationToken) =>
            Service.GetDirectUrlAsync(videoUrl, cancellationToken);

        public static Task<string> ConvertToMp4Async(string hlsUrl, CancellationToken cancellationToken) =>
            Service.ConvertToMp4Async(hlsUrl, cancellationToken);

        public static Task<string> GetCurrentYtDlpVersionAsync(CancellationToken cancellationToken) =>
            Service.GetCurrentYtDlpVersionAsync(cancellationToken);

        public static Task<YtDlpUpdateResult> UpdateYtDlpAsync(CancellationToken cancellationToken) =>
            Service.UpdateYtDlpAsync(cancellationToken);

        public static Task PerformYtDlpUpdateCheckAsync(CancellationToken cancellationToken) =>
            Service.PerformYtDlpUpdateCheckAsync(cancellationToken);

        public static Task InitializeToolsAsync(CancellationToken cancellationToken = default) =>
            Service.InitializeToolsAsync(cancellationToken);
    }
}