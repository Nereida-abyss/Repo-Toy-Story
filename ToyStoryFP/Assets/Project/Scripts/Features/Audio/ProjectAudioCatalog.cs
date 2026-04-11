using UnityEngine;

[CreateAssetMenu(fileName = "ProjectAudioCatalog", menuName = "Audio/Project Audio Catalog")]
public class ProjectAudioCatalog : ScriptableObject
{
    [System.Serializable]
    public sealed class MusicGroup
    {
        public AudioClip mainMenu;
        public AudioClip gameplay;
        public AudioClip shop;
        public AudioClip endMenu;
    }

    [System.Serializable]
    public sealed class WaveGroup
    {
        public AudioClip announcement;
    }

    [Header("Music")]
    [SerializeField] private MusicGroup music = new MusicGroup();

    [Header("Waves")]
    [SerializeField] private WaveGroup waves = new WaveGroup();

    public MusicGroup Music => music;
    public WaveGroup Waves => waves;
}
