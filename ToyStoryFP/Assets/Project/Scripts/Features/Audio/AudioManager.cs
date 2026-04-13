using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string GamePlaySceneName = "GamePlay";
    private const string EndMenuSceneName = "EndMenu";
    private const int EndMenuMusicLegacyIndex = 3;
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
    [SerializeField] private int mainMenuMusicIndex = 0;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.05f;

    private bool hasLoggedMissingCatalog;
    private bool hasLoggedMissingMusicSource;
    private bool hasLoggedMissingShopMusicSource;
    private bool hasLoggedMissingSfxSource;
    private bool hasLoggedMissingUiAudioProfile;
    private readonly HashSet<string> warnedKnownScenesWithoutMusic = new HashSet<string>();
    private Coroutine shopMusicBlendCoroutine;
    private bool isShopMusicActive;

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
        AudioClip clipFromCatalog = GetMusicClipFromLegacyIndex(musicIndex);
        float volumeFromCatalog = GetMusicVolumeFromLegacyIndex(musicIndex);

        if (clipFromCatalog != null)
        {
            PlayMusicClip(clipFromCatalog, volumeFromCatalog);
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

        PlayMusicClip(musicList[musicIndex], 1f);
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
        PrepareBaseMusicClip(GetGameplayMusicClip(), GetGameplayMusicVolume(), true);
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
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        AudioClip clip = resolvedCatalog != null ? resolvedCatalog.Music.MainMenuAudio.Clip : null;
        return clip != null ? clip : GetLegacyMusicClip(mainMenuMusicIndex);
    }

    public float GetMainMenuMusicVolume()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Music.MainMenuAudio.Volume : 1f;
    }

    public AudioClip GetGameplayMusicClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Music.GameplayAudio.Clip : null;
    }

    public float GetGameplayMusicVolume()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Music.GameplayAudio.Volume : 1f;
    }

    public AudioClip GetShopMusicClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Music.ShopAudio.Clip : null;
    }

    public float GetShopMusicVolume()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Music.ShopAudio.Volume : 1f;
    }

    public AudioClip GetEndMenuMusicClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        AudioClip clip = resolvedCatalog != null ? resolvedCatalog.Music.EndMenuAudio.Clip : null;
        return clip != null ? clip : GetLegacyMusicClip(EndMenuMusicLegacyIndex);
    }

    public float GetEndMenuMusicVolume()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Music.EndMenuAudio.Volume : 1f;
    }

    public AudioClip GetWaveAnnouncementClip()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Waves.AnnouncementAudio.Clip : null;
    }

    public float GetWaveAnnouncementVolume()
    {
        ProjectAudioCatalog resolvedCatalog = ResolveCatalog();
        return resolvedCatalog != null ? resolvedCatalog.Waves.AnnouncementAudio.Volume : 1f;
    }

    public AudioClip GetUiClickClip()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.ClickClip : null;
    }

    public float GetUiClickVolume()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.ClickVolume : 1f;
    }

    public AudioClip GetUiHoverClip()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.HoverClip : null;
    }

    public float GetUiHoverVolume()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.HoverVolume : 1f;
    }

    public AudioClip GetUiPanelOpenClip()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.PanelOpenClip : null;
    }

    public float GetUiPanelOpenVolume()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.PanelOpenVolume : 1f;
    }

    public AudioClip GetUiPanelCloseClip()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.PanelCloseClip : null;
    }

    public float GetUiPanelCloseVolume()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.PanelCloseVolume : 1f;
    }

    public AudioClip GetUiShopPurchaseSuccessClip()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.ShopPurchaseSuccessClip : null;
    }

    public float GetUiShopPurchaseSuccessVolume()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.ShopPurchaseSuccessVolume : 1f;
    }

    public AudioClip GetUiShopPurchaseFailedClip()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.ShopPurchaseFailedClip : null;
    }

    public float GetUiShopPurchaseFailedVolume()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        return profile != null ? profile.ShopPurchaseFailedVolume : 1f;
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
            StopShopMusicImmediate();
            StopMusicForKnownSceneWithoutClip(sceneName, resolvedMusicSource);
            return;
        }

        if (sceneName == GamePlaySceneName)
        {
            PrepareBaseMusicClip(targetClip, GetMusicVolumeForScene(sceneName), true);

            if (!isShopMusicActive)
            {
                StopShopMusicImmediate();
            }

            return;
        }

        isShopMusicActive = false;
        StopShopMusicImmediate();
        PrepareBaseMusicClip(targetClip, GetMusicVolumeForScene(sceneName), true);
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
        if (SceneManager.GetActiveScene().name != GamePlaySceneName)
        {
            if (!enteringShop)
            {
                PrepareBaseMusicClip(GetSceneMusicClip(SceneManager.GetActiveScene().name), GetSceneMusicVolume(SceneManager.GetActiveScene().name), true);
            }

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
                PrepareBaseMusicClip(GetGameplayMusicClip(), GetGameplayMusicVolume(), true);
            }

            return;
        }

        PrepareBaseMusicClip(GetGameplayMusicClip(), GetGameplayMusicVolume(), true);
        PrepareShopMusicClip(resolvedShopSource, enteringShop);
        isShopMusicActive = enteringShop;

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

        if (!resolvedShopSource.isPlaying && enteringShop)
        {
            resolvedShopSource.volume = 0f;
            resolvedShopSource.Play();
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

            if (!enteringShop)
            {
                shopSource.Stop();
            }
        }

        shopMusicBlendCoroutine = null;
    }

    private void StopShopMusicImmediate()
    {
        AudioSource resolvedShopSource = ResolveShopMusicSource(false);

        if (shopMusicBlendCoroutine != null)
        {
            StopCoroutine(shopMusicBlendCoroutine);
            shopMusicBlendCoroutine = null;
        }

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

    private float GetMusicVolumeFromLegacyIndex(int musicIndex)
    {
        switch (musicIndex)
        {
            case 0:
                return GetMainMenuMusicVolume();
            case 1:
                return GetGameplayMusicVolume();
            case 2:
                return GetShopMusicVolume();
            case 3:
                return GetEndMenuMusicVolume();
            default:
                return 1f;
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
