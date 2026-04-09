using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private const string DefaultCatalogResourcePath = "Audio/ProjectAudioCatalog";

    private static AudioManager instance;

    public static AudioManager Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<AudioManager>();

            if (instance == null && Application.isPlaying)
            {
                GameObject audioManagerObject = new GameObject(nameof(AudioManager));
                instance = audioManagerObject.AddComponent<AudioManager>();
            }

            return instance;
        }
    }

    [Header("Audio Catalog")]
    [SerializeField] private ProjectAudioCatalog catalog;

    [Header("Legacy Audio Clip Arrays")]
    [SerializeField] private AudioClip[] musicList;
    [SerializeField] private AudioClip[] sfxList;

    [Header("Audio Source References")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Settings")]
    [SerializeField] private bool keepMainMenuMusicInAllScenes = true;
    [SerializeField] private int mainMenuMusicIndex = 0;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.05f;

    private bool hasLoggedMissingCatalog;

    public ProjectAudioCatalog Catalog => ResolveCatalog();
    public AudioClip[] MusicList => musicList;
    public AudioClip[] SfxList => sfxList;

    // Inicializa referencias antes de usar el componente.
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // DontDestroyOnLoad solo funciona en objetos raíz.
        // Si el gestor esta anidado en la jerarquía, lo desacoplamos primero.
        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }

        DontDestroyOnLoad(gameObject);
        EnsureAudioSources();
        ResolveCatalog();
    }

    // Activa listeners y estado al habilitar el objeto.
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Libera listeners y estado al deshabilitar el objeto.
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // Arranca la configuración inicial del componente.
    private void Start()
    {
        EnsureMainMenuMusic();
    }

    // Gestiona el evento de escena loaded.
    private void OnSceneLoaded(Scene _, LoadSceneMode __)
    {
        if (!keepMainMenuMusicInAllScenes)
        {
            return;
        }

        EnsureMainMenuMusic();
    }

    // Reproduce music.
    public void PlayMusic(int musicIndex)
    {
        AudioClip clipFromCatalog = GetMusicClipFromLegacyIndex(musicIndex);

        if (clipFromCatalog != null)
        {
            PlayMusicClip(clipFromCatalog);
            return;
        }

        EnsureAudioSources();

        if (musicList == null || musicList.Length == 0)
        {
            GameDebug.Advertencia("Audio", "No hay pistas en musicList para reproducir.", this);
            return;
        }

        if (musicIndex < 0 || musicIndex >= musicList.Length)
        {
            GameDebug.Advertencia("Audio", $"Indice de música fuera de rango: {musicIndex}", this);
            return;
        }

        if (musicSource == null)
        {
            GameDebug.Advertencia("Audio", "No hay AudioSource de música asignado.", this);
            return;
        }

        PlayMusicClip(musicList[musicIndex]);
    }

    // Reproduce SFX.
    public void PlaySFX(int sfxIndex)
    {
        EnsureAudioSources();

        if (sfxList == null || sfxList.Length == 0 || sfxIndex < 0 || sfxIndex >= sfxList.Length)
        {
            return;
        }

        if (sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(sfxList[sfxIndex]);
    }

    public void PlayMusicClip(AudioClip clip, bool loop = true)
    {
        EnsureAudioSources();

        if (musicSource == null || clip == null)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public AudioClip GetMainMenuMusicClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        AudioClip clip = resolvedCatalog != null ? resolvedCatalog.Music.mainMenu : null;
        return clip != null ? clip : GetLegacyMusicClip(mainMenuMusicIndex);
    }

    public AudioClip GetGameplayMusicClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Music.gameplay : null;
    }

    public AudioClip GetShopMusicClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Music.shop : null;
    }

    public AudioClip GetDefeatMusicClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Music.defeat : null;
    }

    public AudioClip GetVictoryMusicClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Music.victory : null;
    }

    public AudioClip GetPlayerJumpClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Player.jump : null;
    }

    public AudioClip[] GetPlayerFootstepClips()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Player.footsteps : System.Array.Empty<AudioClip>();
    }

    public AudioClip GetPlayerWeaponSwitchClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Player.weaponSwitch : null;
    }

    public AudioClip GetPlayerCoinPickupClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Player.coinPickup : null;
    }

    public AudioClip GetPlayerKillConfirmClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Player.killConfirm : null;
    }

    public AudioClip GetPlayerHurtClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Player.hurt : null;
    }

    public AudioClip GetEnemyAlertClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Enemy.alert : null;
    }

    public AudioClip GetDefaultWeaponFireClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Weapons.defaultFire : null;
    }

    public AudioClip GetDefaultWeaponDryFireClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Weapons.defaultDryFire : null;
    }

    public AudioClip GetDefaultWeaponReloadClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Weapons.defaultReload : null;
    }

    public AudioClip GetUiClickClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Ui.click : null;
    }

    public AudioClip GetUiHoverClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Ui.hover : null;
    }

    public AudioClip GetUiPanelOpenClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Ui.panelOpen : null;
    }

    public AudioClip GetUiPanelCloseClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Ui.panelClose : null;
    }

    public AudioClip GetCreditsIntroWhooshClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Credits.introWhoosh : null;
    }

    public AudioClip GetCreditsNameHitClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Credits.nameHit : null;
    }

    public AudioClip GetCreditsNameTickClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Credits.nameTick : null;
    }

    public AudioClip GetCreditsFinalStingClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Credits.finalSting : null;
    }

    public AudioClip GetCreditsOutroSwishClip()
    {
        return ResolveCatalog() != null ? ResolveCatalog().Credits.outroSwish : null;
    }

    // Asegura main menu music.
    private void EnsureMainMenuMusic()
    {
        EnsureAudioSources();

        if (!keepMainMenuMusicInAllScenes || musicSource == null)
        {
            return;
        }

        AudioClip targetClip = GetMainMenuMusicClip();
        musicSource.volume = musicVolume;
        musicSource.loop = true;

        if (targetClip == null)
        {
            return;
        }

        bool wrongClip = musicSource.clip != targetClip;
        bool stopped = !musicSource.isPlaying;

        if (wrongClip || stopped)
        {
            musicSource.clip = targetClip;
            musicSource.Play();
        }
    }

    // Asegura audio sources.
    private void EnsureAudioSources()
    {
        if (musicSource != null && sfxSource != null)
        {
            return;
        }

        AudioSource[] sources = GetComponents<AudioSource>();

        if (musicSource == null && sources.Length > 0)
        {
            musicSource = sources[0];
        }

        if (sfxSource == null && sources.Length > 1)
        {
            sfxSource = sources[1];
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
    }

    private ProjectAudioCatalog ResolveCatalog()
    {
        if (catalog != null)
        {
            return catalog;
        }

        catalog = Resources.Load<ProjectAudioCatalog>(DefaultCatalogResourcePath);

        if (catalog == null && !hasLoggedMissingCatalog)
        {
            hasLoggedMissingCatalog = true;
            GameDebug.Advertencia(
                "Audio",
                $"No se encontro un ProjectAudioCatalog en Resources/{DefaultCatalogResourcePath}. Se usaran los arrays legacy si existen.",
                this);
        }

        return catalog;
    }

    private AudioClip GetMusicClipFromLegacyIndex(int musicIndex)
    {
        switch (musicIndex)
        {
            case 0:
                return GetMainMenuMusicClip();
            case 1:
                return GetGameplayMusicClip();
            case 2:
                return GetShopMusicClip();
            case 3:
                return GetDefeatMusicClip();
            case 4:
                return GetVictoryMusicClip();
            default:
                return null;
        }
    }

    private AudioClip GetLegacyMusicClip(int musicIndex)
    {
        if (musicList == null || musicList.Length == 0)
        {
            return null;
        }

        if (musicIndex < 0 || musicIndex >= musicList.Length)
        {
            GameDebug.Advertencia("Audio", $"Indice de musica fuera de rango: {musicIndex}", this);
            return null;
        }

        return musicList[musicIndex];
    }
}
