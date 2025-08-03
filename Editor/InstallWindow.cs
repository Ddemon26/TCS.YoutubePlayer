namespace TCS.YoutubePlayer {
    public class InstallWindow : EditorWindow {
        [SerializeField] VisualTreeAsset m_visualTreeAsset;

        [MenuItem( "Tools/Install/Youtube Dependencies" )]
        public static void ShowExample() {
            InstallWindow wnd = GetWindow<InstallWindow>();
            wnd.titleContent = new GUIContent( "InstallWindow" );
        }

        public void CreateGUI() {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // VisualElements objects can contain other VisualElement following a tree hierarchy.
            // VisualElement label = new Label( "Hello World! From C#" );
            // root.Add( label );

            // Instantiate UXML
            VisualElement labelFromUXML = m_visualTreeAsset.Instantiate();
            root.Add( labelFromUXML );
        }
    }
}