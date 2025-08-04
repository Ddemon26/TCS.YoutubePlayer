namespace TCS.YoutubePlayer.Configuration {
    [CreateAssetMenu(fileName = "YtDlpSettings", menuName = "TCS/YouTube Player/Settings Asset", order = 1)]
    public class YtDlpSettingsAsset : ScriptableObject {
        [SerializeField] YtDlpSettingsData m_data = new();
        
        public YtDlpSettingsData Data => m_data;
        
        public YtDlpSettings ToYtDlpSettings() => new(m_data);

        public void FromYtDlpSettings(YtDlpSettings settings) {
            if (settings?.Data != null) {
                m_data = new YtDlpSettingsData(settings.Data);
            }
        }
    }
}