using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TCS.YoutubePlayer.Configuration;
using TCS.YoutubePlayer.ToolManagement;

namespace TCS.YoutubePlayer {
    public class InstallWindow : EditorWindow {
        [SerializeField] VisualTreeAsset m_visualTreeAsset;

        DependencyContainer m_ytlDipDependencyContainer;
        DependencyContainer m_ffmpegDependencyContainer;

        ToolDownloadManager m_toolDownloadManager;
        CancellationTokenSource m_cancellationTokenSource;
        bool m_isOperationInProgress;

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

                m_ytlDipDependencyContainer = root.Q<DependencyContainer>( "Ytldip" );
                m_ytlDipDependencyContainer.RegisterCallbacks();
                m_ytlDipDependencyContainer.OnInstallButtonClicked += YtldipInstallPressed;
                m_ytlDipDependencyContainer.OnUpdateButtonClicked += YtldipUpdatePressed;
                m_ytlDipDependencyContainer.OnUninstallButtonClicked += YtldipUninstallPressed;
                m_ytlDipDependencyContainer.SetHeaderText( "Yt-dlp" );
                m_ytlDipDependencyContainer.SetInformationText(
                    "Yt-dlp is a fork of youtube-dl with enhanced features for converting YouTube URLs into Unity playable videos. " +
                    "Additional features include automatic conflict resolution and improved error handling." +
                    "\nDownload Link: https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"
                );

                m_ffmpegDependencyContainer = root.Q<DependencyContainer>( "Ffmpeg" );
                m_ffmpegDependencyContainer.RegisterCallbacks();
                m_ffmpegDependencyContainer.OnInstallButtonClicked += FfmpegInstallPressed;
                m_ffmpegDependencyContainer.OnUpdateButtonClicked += FfmpegUpdatePressed;
                m_ffmpegDependencyContainer.OnUninstallButtonClicked += FfmpegUninstallPressed;
                m_ffmpegDependencyContainer.SetHeaderText( "Ffmpeg" );
                m_ffmpegDependencyContainer.SetInformationText(
                    "FFmpeg is a complete, cross-platform solution to record, convert and stream audio and video. " +
                    "It is used by this tool for video processing tasks. " +
                    "Ffmpeg provides robust support for various media formats and high performance transcoding. " +
                    "\nDownload Link: https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
                );
                m_ffmpegDependencyContainer.ToggleCurrentVersionVisibility( false );

                await RefreshDependencyStatus();
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to create InstallWindow: {e.Message}" );
            }
        }
        async void YtldipInstallPressed() {
            try {
                if ( m_isOperationInProgress ) return;
                await InstallDependency( "yt-dlp", async () => await m_toolDownloadManager.EnsureYtDlpAsync( m_cancellationTokenSource.Token ) );
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to install yt-dlp: {e.Message}" );
            }
        }

        async void YtldipUpdatePressed() {
            try {
                if ( m_isOperationInProgress ) return;

                bool ytDlpExists = CheckYtDlpExists();
                if ( !ytDlpExists ) {
                    await InstallDependency( "yt-dlp", async () => await m_toolDownloadManager.EnsureYtDlpAsync( m_cancellationTokenSource.Token ) );
                    return;
                }

                await UpdateDependency(
                    "yt-dlp", async () => {
                        var updateResult = await YtDlpExternalTool.UpdateYtDlpAsync( m_cancellationTokenSource.Token );
                        switch (updateResult) {
                            case YtDlpUpdateResult.Updated:
                                Debug.Log( "yt-dlp updated successfully" );
                                break;
                            case YtDlpUpdateResult.AlreadyUpToDate:
                                Debug.Log( "yt-dlp is already up to date" );
                                break;
                            case YtDlpUpdateResult.Failed:
                                Debug.LogWarning( "yt-dlp update failed, falling back to re-download" );
                                UninstallYtDlp();
                                return await m_toolDownloadManager.EnsureYtDlpAsync( m_cancellationTokenSource.Token );
                        }

                        return YtDlpConfigurationManager.GetYtDlpPath();
                    }
                );
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to update yt-dlp: {e.Message}" );
            }
        }

        void YtldipUninstallPressed() {
            if ( m_isOperationInProgress ) return;
            ExecuteUninstallOperation( () => {
                    UninstallYtDlp();
                    _ = RefreshDependencyStatus();
                }
            );
        }

        async void FfmpegInstallPressed() {
            try {
                if ( m_isOperationInProgress ) return;
                await InstallDependency( "ffmpeg", async () => await m_toolDownloadManager.EnsureFFmpegAsync( m_cancellationTokenSource.Token ) );
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to install ffmpeg: {e.Message}" );
            }
        }

        async void FfmpegUpdatePressed() {
            try {
                if ( m_isOperationInProgress ) return;
                await UpdateDependency(
                    "ffmpeg", async () => {
                        UninstallFfmpeg();
                        return await m_toolDownloadManager.EnsureFFmpegAsync( m_cancellationTokenSource.Token );
                    }
                );
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to update ffmpeg: {e.Message}" );
            }
        }

        void FfmpegUninstallPressed() {
            if ( m_isOperationInProgress ) return;
            ExecuteUninstallOperation( () => {
                    UninstallFfmpeg();
                    _ = RefreshDependencyStatus();
                }
            );
        }

        async Task RefreshDependencyStatus() {
            try {
                // Check yt-dlp status
                bool ytDlpExists = CheckYtDlpExists();
                m_ytlDipDependencyContainer.SetInstallTextureResult( ytDlpExists );

                if ( ytDlpExists ) {
                    try {
                        string version = await YtDlpExternalTool.GetCurrentYtDlpVersionAsync( m_cancellationTokenSource.Token );
                        m_ytlDipDependencyContainer.SetVersionValue( version ?? "Unknown" );
                    }
                    catch {
                        m_ytlDipDependencyContainer.SetVersionValue( "Installed" );
                    }
                }
                else {
                    m_ytlDipDependencyContainer.SetVersionValue( "Not Installed" );
                }

                // Check ffmpeg status
                bool ffmpegExists = CheckFfmpegExists();
                m_ffmpegDependencyContainer.SetInstallTextureResult( ffmpegExists );
                //m_ffmpegDependencyContainer.SetVersionValue( ffmpegExists ? "Installed" : "Not Installed" );
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to refresh dependency status: {e.Message}" );
            }
        }

        bool CheckYtDlpExists() {
            try {
                string ytDlpPath = YtDlpConfigurationManager.GetYtDlpPath();
                return File.Exists( ytDlpPath );
            }
            catch {
                return false;
            }
        }

        bool CheckFfmpegExists() {
            try {
                string ffmpegPath = YtDlpConfigurationManager.GetFFmpegPath();
                return File.Exists( ffmpegPath );
            }
            catch {
                return false;
            }
        }

        async Task InstallDependency(string dependencyName, Func<Task<string>> installAction) {
            if ( !ValidatePlatformSupport() ) return;

            SetOperationInProgress( true );
            try {
                Debug.Log( $"Installing {dependencyName}..." );
                string result = await installAction();
                Debug.Log( $"{dependencyName} installed successfully to: {result}" );
                await RefreshDependencyStatus();
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to install {dependencyName}: {e.Message}" );
            }
            finally {
                SetOperationInProgress( false );
            }
        }

        async Task UpdateDependency(string dependencyName, Func<Task<string>> updateAction) {
            if ( !ValidatePlatformSupport() ) return;

            SetOperationInProgress( true );
            try {
                Debug.Log( $"Updating {dependencyName}..." );
                string result = await updateAction();
                Debug.Log( $"{dependencyName} updated successfully to: {result}" );
                await RefreshDependencyStatus();
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to update {dependencyName}: {e.Message}" );
            }
            finally {
                SetOperationInProgress( false );
            }
        }

        void ExecuteUninstallOperation(Action uninstallAction) {
            SetOperationInProgress( true );
            try {
                uninstallAction();
            }
            catch (Exception e) {
                Debug.LogError( $"Uninstall operation failed: {e.Message}" );
            }
            finally {
                SetOperationInProgress( false );
            }
        }

        void SetOperationInProgress(bool inProgress) {
            m_isOperationInProgress = inProgress;

            // Update button states
            if ( m_ytlDipDependencyContainer != null ) {
                // Note: DependencyContainer would need button enable/disable methods
                // For now, we prevent multiple operations with the flag
            }

            if ( m_ffmpegDependencyContainer != null ) {
                // Note: DependencyContainer would need button enable/disable methods  
                // For now, we prevent multiple operations with the flag
            }
        }

        static bool ValidatePlatformSupport() {
            if ( Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.WindowsPlayer ) {
                Debug.LogWarning( "Automatic dependency installation is currently only supported on Windows platforms." );
                return false;
            }

            return true;
        }

        static void UninstallYtDlp() {
            try {
                string ytDlpPath = YtDlpConfigurationManager.GetYtDlpPath();
                string ytDlpDir = Path.GetDirectoryName( ytDlpPath );

                if ( Directory.Exists( ytDlpDir ) ) {
                    Directory.Delete( ytDlpDir, true );
                    Debug.Log( "yt-dlp uninstalled successfully" );
                }
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to uninstall yt-dlp: {e.Message}" );
            }
        }

        static void UninstallFfmpeg() {
            try {
                string ffmpegPath = YtDlpConfigurationManager.GetFFmpegPath();
                string ffmpegDir = Path.GetDirectoryName( Path.GetDirectoryName( ffmpegPath ) ); // Go up two levels from bin/ffmpeg.exe

                if ( Directory.Exists( ffmpegDir ) ) {
                    Directory.Delete( ffmpegDir, true );
                    Debug.Log( "ffmpeg uninstalled successfully" );
                }
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to uninstall ffmpeg: {e.Message}" );
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