using System.Threading;
using System.Threading.Tasks;
namespace TCS.YoutubePlayer {
    public class InstallWindow : EditorWindow {
        [SerializeField] VisualTreeAsset m_visualTreeAsset;
        
        DependencyContainer m_ytlDipDependencyContainer;
        DependencyContainer m_ffmpegDependencyContainer;

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
                var root = rootVisualElement;
                VisualElement labelFromUxml = m_visualTreeAsset.Instantiate();
                root.Add( labelFromUxml );
            
                m_ytlDipDependencyContainer = root.Q<DependencyContainer>("Ytldip");
                m_ytlDipDependencyContainer.RegisterCallbacks();
                m_ytlDipDependencyContainer.OnInstallButtonClicked += YtldipInstallPressed;
                m_ytlDipDependencyContainer.OnUpdateButtonClicked += YtldipUpdatePressed;
                m_ytlDipDependencyContainer.OnUninstallButtonClicked += YtldipUninstallPressed;
                m_ytlDipDependencyContainer.SetHeaderText( "Yt-dlp" );
                m_ytlDipDependencyContainer.SetInformationText( "Yt-dlp is used for converting url into unity playable videos." );
                //string version = await YtDlpExternalTool.GetCurrentYtDlpVersionAsync( CancellationToken.None );
                //m_ytlDipDependencyContainer.SetVersionValue( version );
            
                m_ffmpegDependencyContainer = root.Q<DependencyContainer>("Ffmpeg");
                m_ffmpegDependencyContainer.RegisterCallbacks();
                m_ffmpegDependencyContainer.OnInstallButtonClicked += FfmpegInstallPressed;
                m_ffmpegDependencyContainer.OnUpdateButtonClicked += FfmpegUpdatePressed;
                m_ffmpegDependencyContainer.OnUninstallButtonClicked += FfmpegUninstallPressed;
                m_ffmpegDependencyContainer.SetHeaderText( "Ffmpeg" );
                m_ffmpegDependencyContainer.SetInformationText( "Ffmpeg is used for converting video formats. Mainly Mp4 files, witch require downloading and storing the Mp4." );
            }
            catch (Exception e) {
                Debug.LogError( $"Failed to create InstallWindow: {e.Message}" );
            }
        }
        void YtldipInstallPressed() {
            throw new NotImplementedException();
        }
        void YtldipUpdatePressed() {
            throw new NotImplementedException();
        }
        void YtldipUninstallPressed() {
            throw new NotImplementedException();
        }
        void FfmpegInstallPressed() {
            throw new NotImplementedException();
        }
        void FfmpegUpdatePressed() {
            throw new NotImplementedException();
        }
        void FfmpegUninstallPressed() {
            throw new NotImplementedException();
        }

        public void OnDestroy() {
            m_ytlDipDependencyContainer.UnregisterCallbacks();
            m_ffmpegDependencyContainer.UnregisterCallbacks();
        }
    }
}