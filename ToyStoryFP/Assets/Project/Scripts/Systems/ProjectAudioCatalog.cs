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

    [System.Serializable]
    public sealed class PlayerGroup
    {
        public AudioClip jump;
        public AudioClip[] footsteps = System.Array.Empty<AudioClip>();
        public AudioClip weaponSwitch;
        public AudioClip coinPickup;
        public AudioClip killConfirm;
        public AudioClip hurt;
    }

    [System.Serializable]
    public sealed class EnemyGroup
    {
        public AudioClip alert;
    }

    [System.Serializable]
    public sealed class WeaponGroup
    {
        public AudioClip defaultFire;
        public AudioClip defaultDryFire;
        public AudioClip defaultReload;
    }

    [System.Serializable]
    public sealed class UiGroup
    {
        public AudioClip click;
        public AudioClip hover;
        public AudioClip panelOpen;
        public AudioClip panelClose;
    }

    [System.Serializable]
    public sealed class CreditsGroup
    {
        public AudioClip introWhoosh;
        public AudioClip nameHit;
        public AudioClip nameTick;
        public AudioClip finalSting;
        public AudioClip outroSwish;
    }

    [Header("Music")]
    [SerializeField] private MusicGroup music = new MusicGroup();

    [Header("Player")]
    [SerializeField] private PlayerGroup player = new PlayerGroup();

    [Header("Enemy")]
    [SerializeField] private EnemyGroup enemy = new EnemyGroup();

    [Header("Weapons")]
    [SerializeField] private WeaponGroup weapons = new WeaponGroup();

    [Header("UI")]
    [SerializeField] private UiGroup ui = new UiGroup();

    [Header("Waves")]
    [SerializeField] private WaveGroup waves = new WaveGroup();

    [Header("Credits")]
    [SerializeField] private CreditsGroup credits = new CreditsGroup();

    public MusicGroup Music => music;
    public PlayerGroup Player => player;
    public EnemyGroup Enemy => enemy;
    public WeaponGroup Weapons => weapons;
    public UiGroup Ui => ui;
    public WaveGroup Waves => waves;
    public CreditsGroup Credits => credits;
}
