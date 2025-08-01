using System.Threading;
using System.Threading.Tasks;

namespace TCS.YoutubePlayer {
    public static class YtDlpExternalTool {
        private static readonly YtDlpService _service = new YtDlpService();

        public static string GetCacheTitle(string videoUrl) => 
            _service.GetCacheTitle(videoUrl);

        public static Task<string> GetDirectUrlAsync(string videoUrl, CancellationToken cancellationToken) =>
            _service.GetDirectUrlAsync(videoUrl, cancellationToken);

        public static Task<string> ConvertToMp4Async(string hlsUrl, CancellationToken cancellationToken) =>
            _service.ConvertToMp4Async(hlsUrl, cancellationToken);

        public static Task<string> GetCurrentYtDlpVersionAsync(CancellationToken cancellationToken) =>
            _service.GetCurrentYtDlpVersionAsync(cancellationToken);

        public static Task<YtDlpUpdateResult> UpdateYtDlpAsync(CancellationToken cancellationToken) =>
            _service.UpdateYtDlpAsync(cancellationToken);

        public static Task PerformYtDlpUpdateCheckAsync(CancellationToken cancellationToken) =>
            _service.PerformYtDlpUpdateCheckAsync(cancellationToken);
    }
}