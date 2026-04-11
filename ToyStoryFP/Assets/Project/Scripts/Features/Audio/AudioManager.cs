using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string GamePlaySceneName = "GamePlay";
    private const string EndMenuSceneName = "EndMenu";
    private const int EndMenuMusicLegacyIndex = 3;

    private static AudioManager instance;

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
    private bool hasLoggedMissingMusicSource;
    private bool hasLoggedMissingSfxSource;
    private readonly HashSet<string> warnedKnownScenesWithoutMusic = new HashSet<string>();

    public static AudioManager Instance
    {
        get { return instance; }
    }

    public ProjectAudioCatalog Catalog => ResolveCatalog();
    public AudioClip[] MusicList => musicList;
    public AudioClip[] SfxList => sfxList;
    public AudioSource SharedSfxSource => ResolveSfxSource();

    // Inicializa referencias antes de usar el componente.
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // DontDestroyOnLoad solo funciona en objetos raiz.
        // Si el gestor esta anidado en la jerarquia, lo desacoplamos primero.
        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }

        DontDestroyOnLoad(gameObject);
        ResolveCatalog();
        ResolveMusicSource();
        ResolveSfxSource();

        // Campo legacy conservado por compatibilidad de inspector; ya no gobierna el cambio de musica.
        _ = keepMainMenuMusicInAllScenes;
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

    // Arranca la configuracion inicial del componente.
    private void Start()
    {
        SyncMusicForScene(SceneManager.GetActiveScene().name);
    }

    // Gestiona el evento de escena loaded.
    private void OnSceneLoaded(Scene scene, LoadSceneMode __)
    {
        SyncMusicForScene(scene.name);
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

        if (musicList == null || musicList.Length == 0)
        {
            GameDebug.Advertencia("Audio", "No hay pistas en musicList para reproducir.", this);
            return;
        }

        if (musicIndex < 0 || musicIndex >= musicList.Length)
        {
            GameDebug.Advertencia("Audio", $"Indice de musica fuera de rango: {musicIndex}", this);
            return;
        }

        if (ResolveMusicSource() == null)
        {
            return;
        }

        PlayMusicClip(musicList[musicIndex]);
    }

    // Reproduce SFX.
    public void PlaySFX(int sfxIndex)
    {
        if (sfxList == null || sfxList.Length == 0 || sfxIndex < 0 || sfxIndex >= sfxList.Length)
        {
            return;
        }

        AudioSource resolvedSfxSource = ResolveSfxSource();

        if (resolvedSfxSource == null)
        {
            return;
        }

        resolvedSfxSource.PlayOneShot(sfxList[sfxIndex]);
    }

    public void PlayMusicClip(AudioClip clip, bool loop = true)
    {
        AudioSource resolvedMusicSource = ResolveMusicSource();

        if (resolvedMusicSource == null || clip == null)
        {
            return;
        }

        resolvedMusicSource.clip = clip;
        resolvedMusicSource.loop = loop;
        resolvedMusicSource.volume = musicVolume;
        resolvedMusicSource.Play();
    }

    public AudioClip GetMainMenuMusicClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        AudioClip clip = resolvedCatalog != null ? resolvedCatalog.Music.mainMenu : null;
        return clip != null ? clip : GetLegacyMusicClip(mainMenuMusicIndex);
    }

    public AudioClip GetGameplayMusicClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Music.gameplay : null;
    }

    public AudioClip GetShopMusicClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Music.shop : null;
    }

    public AudioClip GetEndMenuMusicClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        AudioClip clip = resolvedCatalog != null ? resolvedCatalog.Music.endMenu : null;
        return clip != null ? clip : GetLegacyMusicClip(EndMenuMusicLegacyIndex);
    }

    public AudioClip GetWaveAnnouncementClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Waves.announcement : null;
    }

    public AudioClip GetPlayerJumpClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Player.jump : null;
    }

    public AudioClip[] GetPlayerFootstepClips()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Player.footsteps : System.Array.Empty<AudioClip>();
    }

    public AudioClip GetPlayerWeaponSwitchClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Player.weaponSwitch : null;
    }

    public AudioClip GetPlayerCoinPickupClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Player.coinPickup : null;
    }

    public AudioClip GetPlayerKillConfirmClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Player.killConfirm : null;
    }

    public AudioClip GetPlayerHurtClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Player.hurt : null;
    }

    public AudioClip GetEnemyAlertClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Enemy.alert : null;
    }

    public AudioClip GetDefaultWeaponFireClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Weapons.defaultFire : null;
    }

    public AudioClip GetDefaultWeaponDryFireClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Weapons.defaultDryFire : null;
    }

    public AudioClip GetDefaultWeaponReloadClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Weapons.defaultReload : null;
    }

    public AudioClip GetUiClickClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Ui.click : null;
    }

    public AudioClip GetUiHoverClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Ui.hover : null;
    }

    public AudioClip GetUiPanelOpenClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Ui.panelOpen : null;
    }

    public AudioClip GetUiPanelCloseClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Ui.panelClose : null;
    }

    public AudioClip GetCreditsIntroWhooshClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Credits.introWhoosh : null;
    }

    public AudioClip GetCreditsNameHitClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Credits.nameHit : null;
    }

    public AudioClip GetCreditsNameTickClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Credits.nameTick : null;
    }

    public AudioClip GetCreditsFinalStingClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Credits.finalSting : null;
    }

    public AudioClip GetCreditsOutroSwishClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Credits.outroSwish : null;
    }

    public AudioClip GetSceneMusicClip(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return null;
        }

        return GetMusicClipForScene(sceneName.Trim());
    }

    // Sincroniza la musica segun la escena activa sin pisar escenas no configuradas.
    private void SyncMusicForScene(string sceneName)
    {
        AudioSource resolvedMusicSource = ResolveMusicSource();

        if (resolvedMusicSource == null)
        {
            return;
        }

        if (!IsKnownMusicScene(sceneName))
        {
            return;
        }

        AudioClip targetClip = GetMusicClipForScene(sceneName);
        if (targetClip == null)
        {
            StopMusicForKnownSceneWithoutClip(sceneName, resolvedMusicSource);
            return;
        }

        resolvedMusicSource.volume = musicVolume;
        resolvedMusicSource.loop = true;

        bool wrongClip = resolvedMusicSource.clip != targetClip;
        bool stopped = !resolvedMusicSource.isPlaying;

        if (wrongClip || stopped)
        {
            resolvedMusicSource.clip = targetClip;
            resolvedMusicSource.Play();
        }
    }

    private AudioSource ResolveMusicSource()
    {
        if (musicSource != null)
        {
            return musicSource;
        }

        if (!hasLoggedMissingMusicSource)
        {
            hasLoggedMissingMusicSource = true;
            GameDebug.Advertencia("Audio", "No hay AudioSource de musica asignado en el inspector.", this);
        }

        return null;
    }

    private AudioSource ResolveSfxSource()
    {
        if (sfxSource != null)
        {
            return sfxSource;
        }

        if (!hasLoggedMissingSfxSource)
        {
            hasLoggedMissingSfxSource = true;
            GameDebug.Advertencia("Audio", "No hay AudioSource de SFX asignado en el inspector.", this);
        }

        return null;
    }

    // Devuelve la pista asociada a una escena conocida.
    private AudioClip GetMusicClipForScene(string sceneName)
    {
        switch (sceneName)
        {
            case MainMenuSceneName:
                return GetMainMenuMusicClip();
            case GamePlaySceneName:
                return GetGameplayMusicClip();
            case EndMenuSceneName:
                return GetEndMenuMusicClip();
            default:
                return null;
        }
    }

    private bool IsKnownMusicScene(string sceneName)
    {
        switch (sceneName)
        {
            case MainMenuSceneName:
            case GamePlaySceneName:
            case EndMenuSceneName:
                return true;
            default:
                return false;
        }
    }

    private void StopMusicForKnownSceneWithoutClip(string sceneName, AudioSource resolvedMusicSource)
    {
        if (resolvedMusicSource.isPlaying)
        {
            resolvedMusicSource.Stop();
        }

        resolvedMusicSource.clip = null;

        if (warnedKnownScenesWithoutMusic.Contains(sceneName))
        {
            return;
        }

        warnedKnownScenesWithoutMusic.Add(sceneName);
        GameDebug.Advertencia(
            "Audio",
            $"La escena musical conocida '{sceneName}' no tiene clip configurado. Se detiene la musica actual para evitar arrastrar la pista anterior.",
            this);
    }

    private ProjectAudioCatalog ResolveCatalog()
    {
        if (catalog != null)
        {
            return catalog;
        }

        if (!hasLoggedMissingCatalog)
        {
            hasLoggedMissingCatalog = true;
            GameDebug.Advertencia(
                "Audio",
                "No hay ProjectAudioCatalog asignado en el inspector. Se usaran los arrays legacy si existen.",
                this);
        }

        return null;
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
                return GetEndMenuMusicClip();
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
