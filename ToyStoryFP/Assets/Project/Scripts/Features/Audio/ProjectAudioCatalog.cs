using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ProjectAudioCatalog", menuName = "Audio/Project Audio Catalog")]
public class ProjectAudioCatalog : ScriptableObject, ISerializationCallbackReceiver
{
    [System.Serializable]
    public sealed class MusicGroup
    {
        [SerializeField] private ConfigurableAudioClip mainMenuAudio = new ConfigurableAudioClip();
        [SerializeField] private ConfigurableAudioClip gameplayAudio = new ConfigurableAudioClip();
        [SerializeField] private ConfigurableAudioClip shopAudio = new ConfigurableAudioClip();
        [SerializeField] private ConfigurableAudioClip endMenuAudio = new ConfigurableAudioClip();

        [FormerlySerializedAs("mainMenu")] [SerializeField, HideInInspector] private AudioClip legacyMainMenu;
        [FormerlySerializedAs("gameplay")] [SerializeField, HideInInspector] private AudioClip legacyGameplay;
        [FormerlySerializedAs("shop")] [SerializeField, HideInInspector] private AudioClip legacyShop;
        [FormerlySerializedAs("endMenu")] [SerializeField, HideInInspector] private AudioClip legacyEndMenu;

        public ConfigurableAudioClip MainMenuAudio => mainMenuAudio;
        public ConfigurableAudioClip GameplayAudio => gameplayAudio;
        public ConfigurableAudioClip ShopAudio => shopAudio;
        public ConfigurableAudioClip EndMenuAudio => endMenuAudio;
        public AudioClip mainMenu => mainMenuAudio != null ? mainMenuAudio.Clip : null;
        public AudioClip gameplay => gameplayAudio != null ? gameplayAudio.Clip : null;
        public AudioClip shop => shopAudio != null ? shopAudio.Clip : null;
        public AudioClip endMenu => endMenuAudio != null ? endMenuAudio.Clip : null;

        public void MigrateLegacyData()
        {
            mainMenuAudio ??= new ConfigurableAudioClip();
            gameplayAudio ??= new ConfigurableAudioClip();
            shopAudio ??= new ConfigurableAudioClip();
            endMenuAudio ??= new ConfigurableAudioClip();

            mainMenuAudio.ApplyLegacyClip(legacyMainMenu);
            gameplayAudio.ApplyLegacyClip(legacyGameplay);
            shopAudio.ApplyLegacyClip(legacyShop);
            endMenuAudio.ApplyLegacyClip(legacyEndMenu);
        }
    }

    [System.Serializable]
    public sealed class WaveGroup
    {
        [SerializeField] private ConfigurableAudioClip announcementAudio = new ConfigurableAudioClip();
        [FormerlySerializedAs("announcement")] [SerializeField, HideInInspector] private AudioClip legacyAnnouncement;

        public ConfigurableAudioClip AnnouncementAudio => announcementAudio;
        public AudioClip announcement => announcementAudio != null ? announcementAudio.Clip : null;

        public void MigrateLegacyData()
        {
            announcementAudio ??= new ConfigurableAudioClip();
            announcementAudio.ApplyLegacyClip(legacyAnnouncement);
        }
    }

    [Header("Music")]
    [SerializeField] private MusicGroup music = new MusicGroup();

    [Header("Waves")]
    [SerializeField] private WaveGroup waves = new WaveGroup();

    public MusicGroup Music => music;
    public WaveGroup Waves => waves;

    public void OnAfterDeserialize()
    {
        MigrateLegacyData();
    }

    public void OnBeforeSerialize()
    {
        MigrateLegacyData();
    }

    private void OnValidate()
    {
        MigrateLegacyData();
    }

    private void MigrateLegacyData()
    {
        music ??= new MusicGroup();
        waves ??= new WaveGroup();
        music.MigrateLegacyData();
        waves.MigrateLegacyData();
    }
}
