using System.Threading.Tasks;
using UnityEngine.Video;
namespace TCS.YoutubePlayer {
    [RequireComponent( typeof(VideoPlayer) )]
    [DisallowMultipleComponent]
    public class VideoPlayerController : MonoBehaviour {
        [Tooltip( "Automatically start playing once the VideoPlayer is prepared." )]
        public bool m_autoPlayOnPrepare = true;

        VideoPlayer m_player;
        AudioSource m_audioSrc; // only present if you add one yourself

        void Awake() {
            m_player = GetComponent<VideoPlayer>();
            m_audioSrc = GetComponent<AudioSource>(); // null unless you routed audio to a source
            m_player.loopPointReached += _ => OnLoopReached();
        }

        void Start() {
            if ( m_player.isPrepared ) {
                OnPrepared();
            }
            else {
                m_player.prepareCompleted += _ => OnPrepared();
            }
            
            SetVolume( 0.5f ); // Set default volume to 50%
        }

        /// <summary>
        /// Toggles between play and pause states
        /// </summary>
        public void TogglePlayPause() {
            if ( m_player == null || !m_player.isPrepared ) {
                return;
            }

            if ( m_player.isPlaying ) {
                m_player.Pause();
            }
            else {
                m_player.Play();
            }
        }

        /// <summary>
        /// Stops video playback completely
        /// </summary>
        public void StopPlayback() {
            if ( m_player == null || !m_player.isPrepared ) {
                return;
            }

            m_player.Stop();
        }

        /// <summary>
        /// Seeks to an absolute time position in the video
        /// </summary>
        /// <param name="seconds">Time position in seconds</param>
        public async void SeekAbsolute(float seconds) {
            if ( m_player == null || !m_player.canSetTime ) {
                return;
            }

            await SeekAsync( seconds );
        }

        public void SeekRelative(float deltaSeconds) {
            if ( m_player == null ) {
                return;
            }

            SeekAbsolute( (float)m_player.time + deltaSeconds );
        }

        public void SkipForward(float seconds = 10f) => SeekRelative( +seconds );
        public void SkipBackward(float seconds = 10f) => SeekRelative( -seconds );

        /// <summary>
        /// Sets the playback speed multiplier
        /// </summary>
        /// <param name="speed">Speed multiplier (1.0 = normal speed)</param>
        public void SetPlaybackSpeed(float speed = 1f) => m_player.playbackSpeed = speed;

        public void HalfSpeed() => SetPlaybackSpeed( 0.5f );
        public void DoubleSpeed() => SetPlaybackSpeed( 2f );

        public void MuteToggle() {
            if ( m_audioSrc ) {
                m_audioSrc.mute = !m_audioSrc.mute;
            }
            else {
                m_player.SetDirectAudioMute( 0, !m_player.GetDirectAudioMute( 0 ) );
            }
        }

        /// <summary>
        /// Set volume (0–1). If you've routed audio to an AudioSource,
        /// this adjusts that; otherwise, it changes the first direct-audio track.
        /// </summary>
        /// <param name="value">Volume level from 0.0 to 1.0</param>
        public void SetVolume(float value) {
            value = Mathf.Clamp01( value );
            if ( m_audioSrc ) {
                m_audioSrc.volume = value;
            }
            else {
                m_player.SetDirectAudioVolume( 0, value );
            }
        }

        /* ───────────────── INTERNAL ───────────────── */

        async Task SeekAsync(float toTime) {
            if ( !m_player.isPrepared ) {
                return;
            }

            m_player.time = Mathf.Clamp( toTime, 0f, (float)m_player.length );

            // For streamed videos, we pause and prepare, so Unity buffers before replaying.
            m_player.Pause();
            m_player.Prepare();

            while (!m_player.isPrepared) {
                await Task.Yield();
            }

            m_player.Play();
        }

        void OnPrepared() {
            if ( m_autoPlayOnPrepare ) {
                m_player.Play();
            }
        }

        void OnLoopReached() {
            // Default: stop at end. Uncomment to loop:
            // player.time = 0;
            // player.Play();
        }
    }
}