using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using TCS.YoutubePlayer.Configuration;
using TCS.YoutubePlayer.ToolManagement;

namespace TCS.YoutubePlayer {
    public class InstallWindow : EditorWindow {
        [SerializeField] VisualTreeAsset m_visualTreeAsset;
        
        DependencyContainer m_ytlDipDependencyContainer;
        DependencyContainer m_ffmpegDependencyContainer;
        
        ToolDownloadManager m_toolDownloadManager;
        CancellationTokenSource m_cancellationTokenSource;

        [MenuItem( "Tools/Install/Youtube Dependencies" )]
        public static void OpenWindow() {
            var wnd = GetWindow<InstallWindow>();
            wnd.titleContent = new GUIContent( "InstallWindow" );
            
            wnd.minSize = new Vector2( 400, 300 );
            wnd.maxSize = new Vector2( 400, 300 );
            wnd.Show();
        }

        public async void CreateGUI() {
            try {
                m_toolDownloadManager = new ToolDownloadManager();
                m_cancellationTokenSource = new CancellationTokenSource();
                
                var root = rootVisualElement;
                VisualElement labelFromUxml = m_visualTreeAsset.Instantiate();
                root.Add( labelFromUxml );
            
                m_ytlDipDependencyContainer = root.Q<DependencyContainer>("Ytldip");
                m_ytlDipDependencyContainer.RegisterCallbacks();
                m_ytlDipDependencyContainer.OnInstallButtonClicked += YtldipInstallPressed;
                m_ytlDipDependencyContainer.OnUpdateButtonClicked += YtldipUpdatePressed;
                m_ytlDipDependencyContainer.OnUninstallButtonClicked += YtldipUninstallPressed;
                m_ytlDipDependencyContainer.SetHeaderText( "Yt-dlp" );
                m_ytlDipDependencyContainer.SetInformationText( "Yt-dlp is used for converting YouTube URLs into Unity playable videos." );
            
                m_ffmpegDependencyContainer = root.Q<DependencyContainer>("Ffmpeg");
                m_ffmpegDependencyContainer.RegisterCallbacks();
                m_ffmpegDependencyContainer.OnInstallButtonClicked += FfmpegInstallPressed;
                m_ffmpegDependencyContainer.OnUpdateButtonClicked += FfmpegUpdatePressed;
                m_ffmpegDependencyContainer.OnUninstallButtonClicked += FfmpegUninstallPressed;
                m_ffmpegDependencyContainer.SetHeaderText( "Ffmpeg" );
                m_ffmpegDependencyContainer.SetInformationText( "Ffmpeg is used for converting video formats. Mainly MP4 files, which require downloading and storing the MP4." );
                
                await RefreshDependencyStatus();
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to create InstallWindow: {e.Message}" );
            }
        }
        async void YtldipInstallPressed() {
            try {
                await InstallDependency("yt-dlp", async () => await m_toolDownloadManager.EnsureYtDlpAsync(m_cancellationTokenSource.Token));
            }
            catch (Exception e) {
                Debug.LogError($"Failed to install yt-dlp: {e.Message}");
            }
        }
        
        async void YtldipUpdatePressed() {
            try {
                await UpdateDependency("yt-dlp", async () => {
                    UninstallYtDlp();
                    return await m_toolDownloadManager.EnsureYtDlpAsync(m_cancellationTokenSource.Token);
                });
            }
            catch (Exception e) {
                Debug.LogError($"Failed to update yt-dlp: {e.Message}");
            }
        }
        
        void YtldipUninstallPressed() {
            UninstallYtDlp();
            _ = RefreshDependencyStatus();
        }
        
        async void FfmpegInstallPressed() {
            try {
                await InstallDependency("ffmpeg", async () => await m_toolDownloadManager.EnsureFFmpegAsync(m_cancellationTokenSource.Token));
            }
            catch (Exception e) {
                Debug.LogError($"Failed to install ffmpeg: {e.Message}");
            }
        }
        
        async void FfmpegUpdatePressed() {
            try {
                await UpdateDependency("ffmpeg", async () => {
                    UninstallFfmpeg();
                    return await m_toolDownloadManager.EnsureFFmpegAsync(m_cancellationTokenSource.Token);
                });
            }
            catch (Exception e) {
                Debug.LogError($"Failed to update ffmpeg: {e.Message}");
            }
        }
        
        void FfmpegUninstallPressed() {
            UninstallFfmpeg();
            _ = RefreshDependencyStatus();
        }

        async Task RefreshDependencyStatus() {
            try {
                // Check yt-dlp status
                bool ytDlpExists = CheckYtDlpExists();
                m_ytlDipDependencyContainer.SetInstallTextureResult(ytDlpExists);
                
                if (ytDlpExists) {
                    try {
                        string version = await YtDlpExternalTool.GetCurrentYtDlpVersionAsync(m_cancellationTokenSource.Token);
                        m_ytlDipDependencyContainer.SetVersionValue(version ?? "Unknown");
                    }
                    catch {
                        m_ytlDipDependencyContainer.SetVersionValue("Installed");
                    }
                } else {
                    m_ytlDipDependencyContainer.SetVersionValue("Not Installed");
                }
                
                // Check ffmpeg status
                bool ffmpegExists = CheckFfmpegExists();
                m_ffmpegDependencyContainer.SetInstallTextureResult(ffmpegExists);
                m_ffmpegDependencyContainer.SetVersionValue(ffmpegExists ? "Installed" : "Not Installed");
            }
            catch (Exception e) {
                Debug.LogError($"Failed to refresh dependency status: {e.Message}");
            }
        }
        
        bool CheckYtDlpExists() {
            try {
                string ytDlpPath = YtDlpConfigurationManager.GetYtDlpPath();
                return File.Exists(ytDlpPath);
            }
            catch {
                return false;
            }
        }
        
        bool CheckFfmpegExists() {
            try {
                string ffmpegPath = YtDlpConfigurationManager.GetFFmpegPath();
                return File.Exists(ffmpegPath);
            }
            catch {
                return false;
            }
        }
        
        async Task InstallDependency(string dependencyName, Func<Task<string>> installAction) {
            try {
                Debug.Log($"Installing {dependencyName}...");
                string result = await installAction();
                Debug.Log($"{dependencyName} installed successfully to: {result}");
                await RefreshDependencyStatus();
            }
            catch (Exception e) {
                Debug.LogError($"Failed to install {dependencyName}: {e.Message}");
            }
        }
        
        async Task UpdateDependency(string dependencyName, Func<Task<string>> updateAction) {
            try {
                Debug.Log($"Updating {dependencyName}...");
                string result = await updateAction();
                Debug.Log($"{dependencyName} updated successfully to: {result}");
                await RefreshDependencyStatus();
            }
            catch (Exception e) {
                Debug.LogError($"Failed to update {dependencyName}: {e.Message}");
            }
        }
        
        void UninstallYtDlp() {
            try {
                string ytDlpPath = YtDlpConfigurationManager.GetYtDlpPath();
                string ytDlpDir = Path.GetDirectoryName(ytDlpPath);
                
                if (Directory.Exists(ytDlpDir)) {
                    Directory.Delete(ytDlpDir, true);
                    Debug.Log("yt-dlp uninstalled successfully");
                }
            }
            catch (Exception e) {
                Debug.LogError($"Failed to uninstall yt-dlp: {e.Message}");
            }
        }
        
        void UninstallFfmpeg() {
            try {
                string ffmpegPath = YtDlpConfigurationManager.GetFFmpegPath();
                string ffmpegDir = Path.GetDirectoryName(Path.GetDirectoryName(ffmpegPath)); // Go up two levels from bin/ffmpeg.exe
                
                if (Directory.Exists(ffmpegDir)) {
                    Directory.Delete(ffmpegDir, true);
                    Debug.Log("ffmpeg uninstalled successfully");
                }
            }
            catch (Exception e) {
                Debug.LogError($"Failed to uninstall ffmpeg: {e.Message}");
            }
        }
        
        public void OnDestroy() {
            m_ytlDipDependencyContainer?.UnregisterCallbacks();
            m_ffmpegDependencyContainer?.UnregisterCallbacks();
            
            m_cancellationTokenSource?.Cancel();
            m_cancellationTokenSource?.Dispose();
            m_toolDownloadManager?.Dispose();
        }
    }
}