using TCS.YoutubePlayer;

public class YoutubeUI : MonoBehaviour {
    [SerializeField] YoutubePlayer m_youtubePlayer;
    [SerializeField] VideoPlayerController m_videoPlayer;
    [SerializeField] UIDocument m_uiDoc;

    YoutubePlayerElement m_youtubePlayerElement;
    public void Awake() {
        if ( m_uiDoc ) {
            m_youtubePlayerElement = m_uiDoc.rootVisualElement.Q<YoutubePlayerElement>();
        }
    }

    void Update() {
        if ( m_youtubePlayerElement == null || !m_videoPlayer || !m_youtubePlayer.IsInitialized ) return;
        m_youtubePlayerElement.ProgressValue = m_videoPlayer.GetPlaybackTime();
    }

    void OnEnable() {
        m_youtubePlayerElement.RegisterCallbacks();

        m_youtubePlayerElement.OnSearchButtonClicked += SearchURLPressed;
        m_youtubePlayerElement.OnPlayButtonClicked += PlayPressed;
        m_youtubePlayerElement.OnPauseButtonClicked += PausePressed;
        m_youtubePlayerElement.OnPrevButtonClicked += PrevPressed;
        m_youtubePlayerElement.OnNextButtonClicked += NextPressed;

        m_videoPlayer.OnVideoStarted += VideoStarted;
    }

    void VideoStarted() {
        m_youtubePlayerElement.ProgressMaxValue = m_videoPlayer.GetFullLength();
        m_youtubePlayerElement.ProgressMinValue = 0f;
        m_youtubePlayerElement.SetCurrentPlaying( m_youtubePlayer.Title );
    }

    void PlayPressed() {
        if ( m_youtubePlayer.IsInitialized ) {
            m_videoPlayer.PlayPlayback();
        }
        else {
            Debug.LogWarning( "YoutubePlayer is not initialized yet." );
        }
    }

    void PausePressed() {
        if ( m_youtubePlayer.IsInitialized ) {
            m_videoPlayer.TogglePlayPause();
        }
        else {
            Debug.LogWarning( "YoutubePlayer is not initialized yet." );
        }
    }

    void PrevPressed() {
        if ( m_youtubePlayer.IsInitialized ) {
            m_videoPlayer.SkipBackward();
        }
        else {
            Debug.LogWarning( "YoutubePlayer is not initialized yet." );
        }
    }

    void NextPressed() {
        if ( m_youtubePlayer.IsInitialized ) {
            m_videoPlayer.SkipForward();
        }
        else {
            Debug.LogWarning( "YoutubePlayer is not initialized yet." );
        }
    }

    void SearchURLPressed(string url) {
        if ( !m_youtubePlayer.IsInitialized ) {
            Debug.LogWarning( "YoutubePlayer is not initialized yet." );
            return;
        }

        m_youtubePlayer.PlayVideo( url );
    }

    void OnDisable() {
        m_youtubePlayerElement.OnSearchButtonClicked -= SearchURLPressed;

        m_youtubePlayerElement.Dispose();
    }
}