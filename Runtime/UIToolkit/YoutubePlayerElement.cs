[UxmlElement] public partial class YoutubePlayerElement : VisualElement, IDisposable {
    #region USS Class Names
    public static readonly string ClassNameUSS = "youtube-player-element";
    public static readonly string RootUSS = ClassNameUSS + "_root"; // youtube-player-element_root 
    public static readonly string PanelUSS = ClassNameUSS + "_panel"; // youtube-player-element_panel 
    public static readonly string HeaderContainerUSS = ClassNameUSS + "_header-container"; // youtube-player-element_header-container 
    public static readonly string HeaderUSS = ClassNameUSS + "_header"; // youtube-player-element_header 
    public static readonly string SearchContainerUSS = ClassNameUSS + "_search-container"; // youtube-player-element_search-container 
    public static readonly string UrlFieldUSS = ClassNameUSS + "_url-field"; // youtube-player-element_url-field 
    public static readonly string SearchButtonUSS = ClassNameUSS + "_search-button"; // youtube-player-element_search-button 
    public static readonly string CurrentTitleContainerUSS = ClassNameUSS + "_current-title-container"; // youtube-player-element_current-title-container 
    public static readonly string CurrentPlayingUSS = ClassNameUSS + "_current-playing"; // youtube-player-element_current-playing 
    public static readonly string ContextUSS = ClassNameUSS + "_context"; // youtube-player-element_context 
    public static readonly string ButtonStripContainerUSS = ClassNameUSS + "_button-strip-container"; // youtube-player-element_button-strip-container 
    public static readonly string PlayButtonUSS = ClassNameUSS + "_play-button"; // youtube-player-element_play-button 
    public static readonly string PauseButtonUSS = ClassNameUSS + "_pause-button"; // youtube-player-element_pause-button 
    public static readonly string PrevButtonUSS = ClassNameUSS + "_prev-button"; // youtube-player-element_prev-button 
    public static readonly string NextButtonUSS = ClassNameUSS + "_next-button"; // youtube-player-element_next-button 
    public static readonly string ProgressContainerUSS = ClassNameUSS + "_progress-container"; // youtube-player-element_progress-container 
    public static readonly string VideoProgressUSS = ClassNameUSS + "_video-progress"; // youtube-player-element_video-progress 
    #endregion

    #region UI Elements
    readonly VisualElement m_root = new() { name = "Root" };
    readonly VisualElement m_panel = new() { name = "Panel" };
    readonly VisualElement m_headerContainer = new() { name = "HeaderContainer" };
    readonly Label m_header = new() { name = "Header" };
    readonly VisualElement m_searchContainer = new() { name = "SearchContainer" };
    readonly TextField m_urlField = new() { name = "UrlField" };
    readonly Button m_searchButton = new() { name = "SearchButton" };
    readonly VisualElement m_currentTitleContainer = new() { name = "CurrentTitleContainer" };
    readonly Label m_currentPlaying = new() { name = "CurrentPlaying" };
    readonly Label m_context = new() { name = "Context" };
    readonly VisualElement m_buttonStripContainer = new() { name = "ButtonStripContainer" };
    readonly Button m_playButton = new() { name = "PlayButton" };
    readonly Button m_pauseButton = new() { name = "PauseButton" };
    readonly Button m_prevButton = new() { name = "PrevButton" };
    readonly Button m_nextButton = new() { name = "NextButton" };
    readonly VisualElement m_progressContainer = new() { name = "ProgressContainer" };
    readonly ProgressBar m_videoProgress = new() { name = "VideoProgress" };
    #endregion
    
    #region Variables
    float m_progressValue; // Default progress value
    float m_progressMaxValue = 100f; // Default max value for progress
    float m_progressMinValue; // Default min value for progress
    
    public float ProgressValue {
        get => m_progressValue;
        set {
            if ( value < m_progressMinValue || value > m_progressMaxValue ) {
                throw new ArgumentOutOfRangeException(
                    nameof( value ),
                    $"Progress value must be between {m_progressMinValue} and {m_progressMaxValue}."
                );
            }
            m_progressValue = value;
            m_videoProgress.value = value;
        }
    }
    
    public float ProgressMaxValue {
        get => m_progressMaxValue;
        set {
            if ( value <= m_progressMinValue ) {
                throw new ArgumentOutOfRangeException(
                    nameof( value ),
                    "Max value must be greater than min value."
                );
            }
            m_progressMaxValue = value;
            m_videoProgress.highValue = value;
        }
    }
    
    public float ProgressMinValue {
        get => m_progressMinValue;
        set {
            if ( value >= m_progressMaxValue ) {
                throw new ArgumentOutOfRangeException(
                    nameof( value ),
                    "Min value must be less than max value."
                );
            }
            m_progressMinValue = value;
            m_videoProgress.lowValue = value;
        }
    }
    #endregion
    
    #region Actions
    public Action<string> OnSearchButtonClicked;
    public Action OnPlayButtonClicked;
    public Action OnPauseButtonClicked;
    public Action OnPrevButtonClicked;
    public Action OnNextButtonClicked;
    #endregion

    public YoutubePlayerElement() {
        SetElementClassNames();

        // Set Text Fields
        m_header.text = "Youtube Player";
        m_searchButton.text = "Search";
        m_currentPlaying.text = "Currently Playing:";
        m_context.text = "Nothing";
        m_playButton.text = ">";
        m_pauseButton.text = "||";
        m_prevButton.text = "<<";
        m_nextButton.text = ">>";
        m_urlField.value = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"; // Placeholder URL

        // Build Hierarchy
        hierarchy.Add( m_root );
        m_root.Add( m_panel );
        m_panel.Add( m_headerContainer );
        m_headerContainer.Add( m_header );
        m_panel.Add( m_searchContainer );
        m_searchContainer.Add( m_urlField );
        m_searchContainer.Add( m_searchButton );
        m_panel.Add( m_currentTitleContainer );
        m_currentTitleContainer.Add( m_currentPlaying );
        m_currentTitleContainer.Add( m_context );
        m_panel.Add( m_buttonStripContainer );
        m_buttonStripContainer.Add( m_playButton );
        m_buttonStripContainer.Add( m_pauseButton );
        m_buttonStripContainer.Add( m_prevButton );
        m_buttonStripContainer.Add( m_nextButton );
        m_panel.Add( m_progressContainer );
        m_progressContainer.Add( m_videoProgress );
    }
    void SetElementClassNames() {
        AddToClassList( ClassNameUSS );
        m_root.AddToClassList( RootUSS );
        m_panel.AddToClassList( PanelUSS );
        m_headerContainer.AddToClassList( HeaderContainerUSS );
        m_header.AddToClassList( HeaderUSS );
        m_searchContainer.AddToClassList( SearchContainerUSS );
        m_urlField.AddToClassList( UrlFieldUSS );
        m_searchButton.AddToClassList( SearchButtonUSS );
        m_currentTitleContainer.AddToClassList( CurrentTitleContainerUSS );
        m_currentPlaying.AddToClassList( CurrentPlayingUSS );
        m_context.AddToClassList( ContextUSS );
        m_buttonStripContainer.AddToClassList( ButtonStripContainerUSS );
        m_playButton.AddToClassList( PlayButtonUSS );
        m_pauseButton.AddToClassList( PauseButtonUSS );
        m_prevButton.AddToClassList( PrevButtonUSS );
        m_nextButton.AddToClassList( NextButtonUSS );
        m_progressContainer.AddToClassList( ProgressContainerUSS );
        m_videoProgress.AddToClassList( VideoProgressUSS );
    }
    
    // Register callbacks for button clicks
    public void RegisterCallbacks() {
        m_searchButton.clicked += SearchPressed;
        m_playButton.clicked += PlayPressed;
        m_pauseButton.clicked += PausePressed;
        m_prevButton.clicked += PrevPressed;
        m_nextButton.clicked += NextPressed;
    }
    
    public void SetProgress(float value) {
        m_videoProgress.value = value;
    }
    
    void SearchPressed() {
        OnSearchButtonClicked?.Invoke( m_urlField.value );
        m_urlField.value = string.Empty; // Clear the URL field after search
    }
    
    void PlayPressed() => OnPlayButtonClicked?.Invoke();
    void PausePressed() => OnPauseButtonClicked?.Invoke();
    void PrevPressed() => OnPrevButtonClicked?.Invoke();
    void NextPressed() => OnNextButtonClicked?.Invoke();
    
    public void SetCurrentPlaying(string title) {
        m_context.text = $"{title}";
    }

    public void Dispose() {
        // Unregister callbacks to prevent memory leaks
        m_searchButton.clicked -= SearchPressed;
        m_playButton.clicked -= PlayPressed;
        m_pauseButton.clicked -= PausePressed;
        m_prevButton.clicked -= PrevPressed;
        m_nextButton.clicked -= NextPressed;

        // Clear the hierarchy
        hierarchy.Clear();
    }
}