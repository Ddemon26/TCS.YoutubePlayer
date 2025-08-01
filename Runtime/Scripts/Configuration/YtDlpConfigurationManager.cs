using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TCS.YoutubePlayer.Exceptions;
using TCS.YoutubePlayer.ToolManagement;

namespace TCS.YoutubePlayer.Configuration {
    public class YtDlpConfigurationManager : IDisposable {
        readonly YtDlpConfig m_config;
        readonly ToolDownloadManager m_toolDownloadManager;

        public YtDlpConfigurationManager() {
            m_config = LoadConfiguration();
            m_toolDownloadManager = new ToolDownloadManager(m_config.GetNameVersion());
        }

        public string GetYtDlpPath() {
            return m_toolDownloadManager.GetYtDlpPath();
        }

        public async Task<string> EnsureYtDlpAsync(CancellationToken cancellationToken = default) {
            return await m_toolDownloadManager.EnsureYtDlpAsync(cancellationToken);
        }

        public string GetFFmpegPath() {
            return m_toolDownloadManager.GetFFmpegPath();
        }

        public async Task<string> EnsureFFmpegAsync(CancellationToken cancellationToken = default) {
            return await m_toolDownloadManager.EnsureFFmpegAsync(cancellationToken);
        }

        YtDlpConfig LoadConfiguration() {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot)) {
                throw new YtDlpException(
                    $"Could not determine project root from Application.dataPath: {Application.dataPath}"
                );
            }

            string assetsConfigPath = Path.Combine(
                Application.dataPath,
                "TCS.YoutubePlayer",
                "config.json"
            );
            string packagesConfigPath = Path.Combine(
                projectRoot,
                "Packages",
                "TCS.YoutubePlayer",
                "config.json"
            );

            string configPath = null;
            if (File.Exists(assetsConfigPath)) {
                configPath = assetsConfigPath;
            }
            else if (File.Exists(packagesConfigPath)) {
                configPath = packagesConfigPath;
            }

            if (configPath == null) {
                throw new ConfigurationException(
                    $"Configuration file not found at expected locations:\n" +
                    $"  * {assetsConfigPath}\n" +
                    $"  * {packagesConfigPath}",
                    "config.json"
                );
            }

            try {
                string json = File.ReadAllText(configPath);
                var loadedConfig = JsonConvert.DeserializeObject<YtDlpConfig>(json);
                return loadedConfig ?? throw new YtDlpException("Configuration file is empty or invalid JSON.");
            }
            catch (JsonException jsonEx) {
                throw new ConfigurationException($"Failed to parse JSON in `{configPath}`.", configPath, jsonEx);
            }
            catch (IOException ioEx) {
                throw new ConfigurationException($"Failed to read configuration from `{configPath}`.", configPath, ioEx);
            }
        }

        public void Dispose() {
            m_toolDownloadManager?.Dispose();
        }
    }
}