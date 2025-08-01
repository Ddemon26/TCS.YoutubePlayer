using System.IO;
using Newtonsoft.Json;
using TCS.YoutubePlayer.Exceptions;

namespace TCS.YoutubePlayer.Configuration {
    public class YtDlpConfigurationManager {
        readonly YtDlpConfig m_config;

        public YtDlpConfigurationManager() {
            m_config = LoadConfiguration();
        }

        public string GetYtDlpPath() {
            string basePath = Path.Combine(
                Application.streamingAssetsPath,
                m_config.GetNameVersion(),
                "yt-dlp"
            );

            return Application.platform switch {
                RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor
                    => Path.Combine(basePath, "Windows", "yt-dlp.exe"),

                RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor
                    => Path.Combine(basePath, "macOS", "yt-dlp"),

                RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor
                    => Path.Combine(basePath, "Linux", "yt-dlp"),

                _ => throw new NotSupportedException(
                    $"Platform {Application.platform} is not supported for yt-dlp execution."
                ),
            };
        }

        public string GetFFmpegPath() {
            string basePath = Path.Combine(
                Application.streamingAssetsPath,
                m_config.GetNameVersion(),
                "ffmpeg"
            );

            return Application.platform switch {
                RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor
                    => Path.Combine(basePath, "Windows", "bin", "ffmpeg.exe"),

                RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor
                    => Path.Combine(basePath, "macOS", "bin", "ffmpeg"),

                RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor
                    => Path.Combine(basePath, "Linux", "bin", "ffmpeg"),

                _ => throw new NotSupportedException(
                    $"Platform {Application.platform} is not supported for FFmpeg."
                ),
            };
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
    }
}