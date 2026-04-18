using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string GamePlaySceneName = "GamePlay";
    private const string EndMenuSceneName = "EndMenu";
    private const float ShopMusicCrossfadeDuration = 0.6f;

    private static AudioManager instance;

    [Header("Audio Catalog")]
    [SerializeField] private ProjectAudioCatalog catalog;
    [SerializeField] private UiAudioProfile uiAudioProfile;

    [Header("Legacy Audio Clip Arrays")]
    [SerializeField] private AudioClip[] musicList;
    [SerializeField] private AudioClip[] sfxList;

    [Header("Audio Source References")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource shopMusicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Settings")]
    [SerializeField] private bool keepMainMenuMusicInAllScenes = true;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.05f;

    private bool hasLoggedMissingCatalog;
    private bool hasLoggedMissingMusicSource;
    private bool hasLoggedMissingShopMusicSource;
    private bool hasLoggedMissingSfxSource;
    private bool hasLoggedMissingUiAudioProfile;
    private readonly HashSet<string> warnedKnownScenesWithoutMusic = new HashSet<string>();
    private Coroutine shopMusicBlendCoroutine;
    private bool isShopMusicActive;
    private bool hasPendingShopMusicBlendTarget;
    private bool pendingShopMusicBlendTarget;

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
        ResolveShopMusicSource(false);
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
        if (TryPlayLegacyMappedMusic(musicIndex))
        {
            return;
        }

        GameDebug.Advertencia("Audio", $"No hay pista de catalogo configurada para el indice de musica {musicIndex}.", this);
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
        PlayMusicClip(clip, 1f, loop);
    }

    public void PlayGameplayMusic()
    {
        PlayConfiguredMusicEntry(GetGameplayMusicEntry(), true);
    }

    public void PlayShopMusic()
    {
        StartShopMusicBlend(true);
    }

    public void RestoreGameplayMusic()
    {
        StartShopMusicBlend(false);
    }

    public void PlayMusicClip(AudioClip clip, float clipVolume, bool loop = true)
    {
        AudioSource resolvedMusicSource = ResolveMusicSource();

        if (resolvedMusicSource == null || clip == null)
        {
            return;
        }

        resolvedMusicSource.clip = clip;
        resolvedMusicSource.loop = loop;
        resolvedMusicSource.volume = Mathf.Clamp01(musicVolume * Mathf.Clamp01(clipVolume));
        resolvedMusicSource.Play();
    }

    public AudioClip GetMainMenuMusicClip()
    {
        return GetAudioClip(GetMainMenuMusicEntry());
    }

    public float GetMainMenuMusicVolume()
    {
        return GetAudioVolume(GetMainMenuMusicEntry());
    }

    public AudioClip GetGameplayMusicClip()
    {
        return GetAudioClip(GetGameplayMusicEntry());
    }

    public float GetGameplayMusicVolume()
    {
        return GetAudioVolume(GetGameplayMusicEntry());
    }

    public AudioClip GetShopMusicClip()
    {
        return GetAudioClip(GetShopMusicEntry());
    }

    public float GetShopMusicVolume()
    {
        return GetAudioVolume(GetShopMusicEntry());
    }

    public AudioClip GetEndMenuMusicClip()
    {
        return GetAudioClip(GetEndMenuMusicEntry());
    }

    public float GetEndMenuMusicVolume()
    {
        return GetAudioVolume(GetEndMenuMusicEntry());
    }

    public AudioClip GetWaveAnnouncementClip()
    {
        return GetAudioClip(GetWaveAnnouncementEntry());
    }

    public float GetWaveAnnouncementVolume()
    {
        return GetAudioVolume(GetWaveAnnouncementEntry());
    }

    public AudioClip GetUiClickClip()
    {
        return GetAudioClip(GetUiClickEntry());
    }

    public float GetUiClickVolume()
    {
        return GetAudioVolume(GetUiClickEntry());
    }

    public AudioClip GetUiHoverClip()
    {
        return GetAudioClip(GetUiHoverEntry());
    }

    public float GetUiHoverVolume()
    {
        return GetAudioVolume(GetUiHoverEntry());
    }

    public AudioClip GetUiPanelOpenClip()
    {
        return GetAudioClip(GetUiPanelOpenEntry());
    }

    public float GetUiPanelOpenVolume()
    {
        return GetAudioVolume(GetUiPanelOpenEntry());
    }

    public AudioClip GetUiPanelCloseClip()
    {
        return GetAudioClip(GetUiPanelCloseEntry());
    }

    public float GetUiPanelCloseVolume()
    {
        return GetAudioVolume(GetUiPanelCloseEntry());
    }

    public AudioClip GetUiShopPurchaseSuccessClip()
    {
        return GetAudioClip(GetUiShopPurchaseSuccessEntry());
    }

    public float GetUiShopPurchaseSuccessVolume()
    {
        return GetAudioVolume(GetUiShopPurchaseSuccessEntry());
    }

    public AudioClip GetUiShopPurchaseFailedClip()
    {
        return GetAudioClip(GetUiShopPurchaseFailedEntry());
    }

    public float GetUiShopPurchaseFailedVolume()
    {
        return GetAudioVolume(GetUiShopPurchaseFailedEntry());
    }

    public AudioClip GetSceneMusicClip(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return null;
        }

        return GetMusicClipForScene(sceneName.Trim());
    }

    public float GetSceneMusicVolume(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return 1f;
        }

        return GetMusicVolumeForScene(sceneName.Trim());
    }

    // Sincroniza la musica segun la escena activa sin pisar escenas no configuradas.
    private void SyncMusicForScene(string sceneName)
    {
        if (!TryResolveKnownSceneMusic(sceneName, out AudioSource resolvedMusicSource, out AudioClip targetClip, out float targetVolume))
        {
            return;
        }

        if (targetClip == null)
        {
            StopShopMusicImmediate();
            StopMusicForKnownSceneWithoutClip(sceneName, resolvedMusicSource);
            return;
        }

        ApplySceneMusic(sceneName, targetClip, targetVolume);
    }

    private bool TryResolveKnownSceneMusic(string sceneName, out AudioSource resolvedMusicSource, out AudioClip targetClip, out float targetVolume)
    {
        resolvedMusicSource = ResolveMusicSource();
        targetClip = null;
        targetVolume = 1f;

        if (resolvedMusicSource == null || !IsKnownMusicScene(sceneName))
        {
            return false;
        }

        targetClip = GetMusicClipForScene(sceneName);
        targetVolume = GetMusicVolumeForScene(sceneName);
        return true;
    }

    private void ApplySceneMusic(string sceneName, AudioClip targetClip, float targetVolume)
    {
        if (sceneName == GamePlaySceneName)
        {
            PrepareBaseMusicClip(targetClip, targetVolume, true);
            PrepareShopMusicClip(ResolveShopMusicSource(false), false);

            return;
        }

        isShopMusicActive = false;
        StopShopMusicImmediate();
        PrepareBaseMusicClip(targetClip, targetVolume, true);
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

    private AudioSource ResolveShopMusicSource(bool logIfMissing = true)
    {
        if (shopMusicSource != null)
        {
            return shopMusicSource;
        }

        if (logIfMissing && !hasLoggedMissingShopMusicSource)
        {
            hasLoggedMissingShopMusicSource = true;
            GameDebug.Advertencia("Audio", "No hay AudioSource de musica de tienda asignado en el inspector.", this);
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

    private float GetMusicVolumeForScene(string sceneName)
    {
        switch (sceneName)
        {
            case MainMenuSceneName:
                return GetMainMenuMusicVolume();
            case GamePlaySceneName:
                return GetGameplayMusicVolume();
            case EndMenuSceneName:
                return GetEndMenuMusicVolume();
            default:
                return 1f;
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

    private void StartShopMusicBlend(bool enteringShop)
    {
        string activeSceneName = GetActiveSceneName();

        if (activeSceneName != GamePlaySceneName)
        {
            if (!enteringShop)
            {
                RestoreSceneMusicOutsideGameplay(activeSceneName);
            }

            return;
        }

        if (isShopMusicActive == enteringShop && shopMusicBlendCoroutine == null)
        {
            return;
        }

        if (shopMusicBlendCoroutine != null
            && hasPendingShopMusicBlendTarget
            && pendingShopMusicBlendTarget == enteringShop)
        {
            return;
        }

        AudioSource resolvedBaseSource = ResolveMusicSource();
        AudioSource resolvedShopSource = ResolveShopMusicSource(enteringShop);

        if (resolvedBaseSource == null)
        {
            return;
        }

        if (resolvedShopSource == null)
        {
            if (enteringShop)
            {
                EnsureGameplayMusicPrepared();
            }

            return;
        }

        EnsureGameplayMusicPrepared();
        PrepareShopMusicClip(resolvedShopSource, enteringShop);
        isShopMusicActive = enteringShop;
        hasPendingShopMusicBlendTarget = true;
        pendingShopMusicBlendTarget = enteringShop;

        if (shopMusicBlendCoroutine != null)
        {
            StopCoroutine(shopMusicBlendCoroutine);
        }

        shopMusicBlendCoroutine = StartCoroutine(BlendShopMusicCoroutine(
            resolvedBaseSource,
            resolvedShopSource,
            enteringShop,
            Mathf.Clamp01(musicVolume * GetGameplayMusicVolume()),
            Mathf.Clamp01(musicVolume * GetShopMusicVolume())));
    }

    private void RestoreSceneMusicOutsideGameplay(string sceneName)
    {
        PlaySceneMusic(sceneName);
    }

    private void PlaySceneMusic(string sceneName)
    {
        PlayMusicClip(GetSceneMusicClip(sceneName), GetSceneMusicVolume(sceneName), true);
    }

    private void PlayConfiguredMusicEntry(ConfigurableAudioClip musicEntry, bool playIfStopped)
    {
        PrepareBaseMusicClip(GetAudioClip(musicEntry), GetAudioVolume(musicEntry), playIfStopped);
    }

    private bool TryPlayLegacyMappedMusic(int musicIndex)
    {
        ConfigurableAudioClip configuredAudio = GetMusicEntryFromLegacyIndex(musicIndex);
        AudioClip clipFromCatalog = GetAudioClip(configuredAudio);

        if (clipFromCatalog == null)
        {
            return false;
        }

        PlayMusicClip(clipFromCatalog, GetAudioVolume(configuredAudio));
        return true;
    }

    private void PrepareBaseMusicClip(AudioClip clip, float clipVolume, bool playIfStopped)
    {
        AudioSource resolvedMusicSource = ResolveMusicSource();

        if (resolvedMusicSource == null || clip == null)
        {
            return;
        }

        float targetVolume = Mathf.Clamp01(musicVolume * Mathf.Clamp01(clipVolume));
        bool wrongClip = resolvedMusicSource.clip != clip;

        if (wrongClip)
        {
            resolvedMusicSource.clip = clip;
        }

        resolvedMusicSource.loop = true;
        resolvedMusicSource.volume = isShopMusicActive ? 0f : targetVolume;

        if ((wrongClip || playIfStopped) && !resolvedMusicSource.isPlaying)
        {
            resolvedMusicSource.Play();
        }

    }

    private void PrepareShopMusicClip(AudioSource resolvedShopSource, bool enteringShop)
    {
        if (resolvedShopSource == null)
        {
            return;
        }

        AudioClip shopClip = GetShopMusicClip();

        if (shopClip == null)
        {
            StopShopMusicImmediate();
            return;
        }

        bool wrongClip = resolvedShopSource.clip != shopClip;

        if (wrongClip)
        {
            resolvedShopSource.clip = shopClip;
        }

        resolvedShopSource.loop = true;
        if (!resolvedShopSource.isPlaying)
        {
            resolvedShopSource.volume = 0f;
            resolvedShopSource.Play();
            return;
        }

        if (!enteringShop && !isShopMusicActive)
        {
            resolvedShopSource.volume = 0f;
        }
    }

    private IEnumerator BlendShopMusicCoroutine(AudioSource baseSource, AudioSource shopSource, bool enteringShop, float gameplayTargetVolume, float shopTargetVolume)
    {
        float duration = Mathf.Max(0.01f, ShopMusicCrossfadeDuration);
        float elapsed = 0f;
        float startBaseVolume = baseSource != null ? baseSource.volume : 0f;
        float startShopVolume = shopSource != null ? shopSource.volume : 0f;
        float endBaseVolume = enteringShop ? 0f : gameplayTargetVolume;
        float endShopVolume = enteringShop ? shopTargetVolume : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (baseSource != null)
            {
                baseSource.volume = Mathf.Lerp(startBaseVolume, endBaseVolume, t);
            }

            if (shopSource != null)
            {
                shopSource.volume = Mathf.Lerp(startShopVolume, endShopVolume, t);
            }

            yield return null;
        }

        if (baseSource != null)
        {
            baseSource.volume = endBaseVolume;
        }

        if (shopSource != null)
        {
            shopSource.volume = endShopVolume;

        }

        shopMusicBlendCoroutine = null;
        hasPendingShopMusicBlendTarget = false;
    }

    private void StopShopMusicImmediate()
    {
        AudioSource resolvedShopSource = ResolveShopMusicSource(false);

        if (shopMusicBlendCoroutine != null)
        {
            StopCoroutine(shopMusicBlendCoroutine);
            shopMusicBlendCoroutine = null;
        }

        hasPendingShopMusicBlendTarget = false;

        if (resolvedShopSource == null)
        {
            return;
        }

        resolvedShopSource.volume = 0f;
        resolvedShopSource.clip = GetShopMusicClip();
        resolvedShopSource.loop = true;

        if (resolvedShopSource.isPlaying)
        {
            resolvedShopSource.Stop();
        }
    }

    private void EnsureGameplayMusicPrepared()
    {
        PlayConfiguredMusicEntry(GetGameplayMusicEntry(), true);
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
                "No hay ProjectAudioCatalog asignado en el inspector. El AudioManager necesita catalogo para resolver musica y anuncios.",
                this);
        }

        return null;
    }

    private UiAudioProfile ResolveUiAudioProfile()
    {
        if (uiAudioProfile != null)
        {
            return uiAudioProfile;
        }

        if (!hasLoggedMissingUiAudioProfile)
        {
            hasLoggedMissingUiAudioProfile = true;
            GameDebug.Advertencia(
                "Audio",
                "No hay UiAudioProfile asignado en el inspector del AudioManager.",
                this);
        }

        return null;
    }

    private string GetActiveSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    private ConfigurableAudioClip GetMainMenuMusicEntry()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Music.MainMenuAudio : null;
    }

    private ConfigurableAudioClip GetGameplayMusicEntry()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Music.GameplayAudio : null;
    }

    private ConfigurableAudioClip GetShopMusicEntry()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Music.ShopAudio : null;
    }

    private ConfigurableAudioClip GetEndMenuMusicEntry()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Music.EndMenuAudio : null;
    }

    private ConfigurableAudioClip GetWaveAnnouncementEntry()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Waves.AnnouncementAudio : null;
    }

    private ConfigurableAudioClip GetUiClickEntry()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.ClickAudio : null;
    }

    private ConfigurableAudioClip GetUiHoverEntry()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.HoverAudio : null;
    }

    private ConfigurableAudioClip GetUiPanelOpenEntry()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.PanelOpenAudio : null;
    }

    private ConfigurableAudioClip GetUiPanelCloseEntry()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.PanelCloseAudio : null;
    }

    private ConfigurableAudioClip GetUiShopPurchaseSuccessEntry()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.ShopPurchaseSuccessAudio : null;
    }

    private ConfigurableAudioClip GetUiShopPurchaseFailedEntry()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.ShopPurchaseFailedAudio : null;
    }

    private ConfigurableAudioClip GetMusicEntryFromLegacyIndex(int musicIndex)
    {
        switch (musicIndex)
        {
            case 0:
                return GetMainMenuMusicEntry();
            case 1:
                return GetGameplayMusicEntry();
            case 2:
                return GetShopMusicEntry();
            case 3:
                return GetEndMenuMusicEntry();
            default:
                return null;
        }
    }

    private static AudioClip GetAudioClip(ConfigurableAudioClip audio)
    {
        return audio != null ? audio.Clip : null;
    }

    private static float GetAudioVolume(ConfigurableAudioClip audio)
    {
        return audio != null ? audio.Volume : 1f;
    }

}
