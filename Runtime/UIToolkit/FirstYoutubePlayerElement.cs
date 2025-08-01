/*using TCS.UiToolkitUtils.Attributes;
using UnityEngine.UIElements;

namespace TCS.YoutubePlayer {
    [UxmlElement] public partial class FirstYoutubePlayerElement : VisualElement {
        [USSName] readonly VisualElement m_rootContainer = new() { name = "root-container" };
        [USSName] readonly VisualElement m_playerWindow = new() { name = "player-window" };

        [USSName] readonly VisualElement m_headerContainer = new() { name = "header-container" };
        [USSName] readonly Label m_titleLabel = new() { name = "title-label" };

        [USSName] readonly VisualElement m_searchContainer = new() { name = "search-container" };
        [USSName] readonly TextField m_urlTextField = new() { name = "url-text-field" };
        [USSName] readonly Button m_searchButton = new() { name = "search-button" };

        [USSName] readonly VisualElement m_currentlyPlayingContainer = new() { name = "currently-playing-container" };
        [USSName] readonly Label m_currentlyPlayingTitleLabel = new() { name = "currently-playing-title-label" };
        [USSName] readonly Label m_currentlyPlayingValueLabel = new() { name = "currently-playing-value-label" };

        [USSName] readonly VisualElement m_queueContainer = new() { name = "queue-container" };
        [USSName] readonly Label m_queueTitleLabel = new() { name = "queue-title-label" };
        [USSName] readonly ScrollView m_queueScrollView = new() { name = "queue-scroll-view" };

        [USSName] readonly VisualElement m_controlsContainer = new() { name = "controls-container" };
        [USSName] readonly Button m_previousButton = new() { name = "previous-button" };
        [USSName] readonly Button m_playPauseButton = new() { name = "play-pause-button" };
        [USSName] readonly Button m_nextButton = new() { name = "next-button" };
        [USSName] readonly Button m_skipButton = new() { name = "skip-button" };

        [USSName] readonly VisualElement m_progressContainer = new() { name = "progress-container" };
        [USSName] readonly Slider m_progressSlider = new() { name = "progress-slider" };

        public FirstYoutubePlayerElement() {
            SetElementClassNames();

            // Header
            m_titleLabel.text = "Youtube Player";
            
            m_searchButton.text = "Search";
            
            m_currentlyPlayingTitleLabel.text = "Currently Playing:";
            m_currentlyPlayingValueLabel.text = "Nothing";


            m_queueTitleLabel.text = "Player Queue";
            
            m_previousButton.text = "◀◀";
            m_playPauseButton.text = "▶";
            m_skipButton.text = "||";
            m_nextButton.text = "▶▶";
            
        
            m_headerContainer.Add( m_titleLabel );

            m_searchContainer.Add( m_urlTextField );
            m_searchContainer.Add( m_searchButton );

            m_currentlyPlayingContainer.Add( m_currentlyPlayingTitleLabel );
            m_currentlyPlayingContainer.Add( m_currentlyPlayingValueLabel );

            m_queueContainer.Add( m_queueTitleLabel );
            m_queueContainer.Add( m_queueScrollView );

            m_controlsContainer.Add( m_previousButton );
            m_controlsContainer.Add( m_playPauseButton );
            m_controlsContainer.Add( m_nextButton );
            m_controlsContainer.Add( m_skipButton );

            m_progressContainer.Add( m_progressSlider );

            m_playerWindow.Add( m_headerContainer );
            m_playerWindow.Add( m_searchContainer );
            m_playerWindow.Add( m_currentlyPlayingContainer );
            m_playerWindow.Add( m_queueContainer );
            m_playerWindow.Add( m_controlsContainer );
            m_playerWindow.Add( m_progressContainer );

            m_rootContainer.Add( m_playerWindow );
            hierarchy.Add( m_rootContainer );
        }
    }
}*/