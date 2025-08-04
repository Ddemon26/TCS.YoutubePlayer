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
                await InstallLibrary( LibraryType.YtDlp );
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
            UninstallLibrary( LibraryType.YtDlp );
        }

        async void FfmpegInstallPressed() {
            try {
                if ( m_isOperationInProgress ) return;
                await InstallLibrary( LibraryType.FFmpeg );
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to install ffmpeg: {e.Message}" );
            }
        }

        async void FfmpegUpdatePressed() {
            try {
                if ( m_isOperationInProgress ) return;
                await UpdateLibrary( LibraryType.FFmpeg );
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to update ffmpeg: {e.Message}" );
            }
        }

        void FfmpegUninstallPressed() {
            if ( m_isOperationInProgress ) return;
            UninstallLibrary( LibraryType.FFmpeg );
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

        bool CheckLibraryExists(LibraryType libraryType) {
            try {
                string libraryPath = YtDlpConfigurationManager.GetLibraryPath(libraryType);
                return File.Exists( libraryPath );
            }
            catch {
                return false;
            }
        }

        bool CheckYtDlpExists() {
            return CheckLibraryExists(LibraryType.YtDlp);
        }

        bool CheckFfmpegExists() {
            return CheckLibraryExists(LibraryType.FFmpeg);
        }

        async Task InstallLibrary(LibraryType libraryType) {
            if ( !ValidatePlatformSupport() ) return;

            SetOperationInProgress( true );
            try {
                Debug.Log( $"Installing {libraryType}..." );
                string result = libraryType switch {
                    LibraryType.YtDlp => await m_toolDownloadManager.EnsureYtDlpAsync( m_cancellationTokenSource.Token ),
                    LibraryType.FFmpeg => await m_toolDownloadManager.EnsureFFmpegAsync( m_cancellationTokenSource.Token ),
                    _ => throw new NotSupportedException( $"Installation not supported for library type {libraryType}" )
                };
                Debug.Log( $"{libraryType} installed successfully to: {result}" );
                await RefreshDependencyStatus();
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to install {libraryType}: {e.Message}" );
            }
            finally {
                SetOperationInProgress( false );
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

        async Task UpdateLibrary(LibraryType libraryType) {
            if ( !ValidatePlatformSupport() ) return;

            SetOperationInProgress( true );
            try {
                Debug.Log( $"Updating {libraryType}..." );
                string result = libraryType switch {
                    LibraryType.YtDlp => await HandleYtDlpUpdate(),
                    LibraryType.FFmpeg => await HandleFFmpegUpdate(),
                    _ => throw new NotSupportedException( $"Update not supported for library type {libraryType}" )
                };
                Debug.Log( $"{libraryType} updated successfully to: {result}" );
                await RefreshDependencyStatus();
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to update {libraryType}: {e.Message}" );
            }
            finally {
                SetOperationInProgress( false );
            }
        }

        async Task<string> HandleYtDlpUpdate() {
            bool ytDlpExists = CheckYtDlpExists();
            if ( !ytDlpExists ) {
                return await m_toolDownloadManager.EnsureYtDlpAsync( m_cancellationTokenSource.Token );
            }

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

        async Task<string> HandleFFmpegUpdate() {
            UninstallFfmpeg();
            return await m_toolDownloadManager.EnsureFFmpegAsync( m_cancellationTokenSource.Token );
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

        void UninstallLibrary(LibraryType libraryType) {
            SetOperationInProgress( true );
            try {
                UninstallLibraryImpl(libraryType);
                _ = RefreshDependencyStatus();
            }
            catch (Exception e) {
                Debug.LogError( $"Uninstall operation failed: {e.Message}" );
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
            m_ytlDipDependencyContainer?.ToggleEnabled( inProgress );

            m_ffmpegDependencyContainer?.ToggleEnabled( inProgress );
        }

        static bool ValidatePlatformSupport() {
            if ( Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.WindowsPlayer ) {
                Debug.LogWarning( "Automatic dependency installation is currently only supported on Windows platforms." );
                return false;
            }

            return true;
        }

        static void UninstallLibraryImpl(LibraryType libraryType) {
            try {
                string libraryPath = YtDlpConfigurationManager.GetLibraryPath(libraryType);
                string libraryDir = libraryType switch {
                    LibraryType.YtDlp => Path.GetDirectoryName( libraryPath ),
                    LibraryType.FFmpeg => Path.GetDirectoryName( Path.GetDirectoryName( libraryPath ) ), // Go up two levels from bin/ffmpeg.exe
                    _ => throw new NotSupportedException( $"Uninstall not supported for library type {libraryType}" )
                };

                if ( Directory.Exists( libraryDir ) ) {
                    Directory.Delete( libraryDir, true );
                    Debug.Log( $"{libraryType} uninstalled successfully" );
                }
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to uninstall {libraryType}: {e.Message}" );
            }
        }

        static void UninstallYtDlp() {
            UninstallLibraryImpl(LibraryType.YtDlp);
        }

        static void UninstallFfmpeg() {
            UninstallLibraryImpl(LibraryType.FFmpeg);
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