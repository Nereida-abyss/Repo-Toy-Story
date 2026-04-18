using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIPanelFx : MonoBehaviour
{
    [Header("Open/Close")]
    [SerializeField] private UIPanelFxProfile panelFxProfile;
    [SerializeField] private bool playOpenOnEnable = true;
    [SerializeField] private float openDuration = 0.26f;
    [SerializeField] private float closeDuration = 0.16f;
    [SerializeField] private float slideOffset = 22f;
    [SerializeField] private float startScale = 0.94f;
    [SerializeField] private float closeScale = 0.97f;

    [Header("Canvas Group")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private bool disableRaycastWhileAnimating = true;

    [Header("Audio")]
    [SerializeField] private bool enableAudio = true;
    [SerializeField] private UiAudioProfile uiAudioProfile;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool useSharedAudioSource = true;
    [SerializeField] private float openVolume = 0.5f;
    [SerializeField] private float closeVolume = 0.38f;

    private RectTransform rectTransform;
    private Coroutine activeRoutine;
    private Vector2 baseAnchoredPosition;
    private Vector3 baseScale;
    private bool initialized;
    private bool suppressOpenAnimation;
    private bool missingCanvasGroupWarningShown;
    private bool missingSharedAudioWarningShown;
    private bool hasLoggedMissingProfile;
    private bool targetVisible;
    private bool isTransitioningOpen;
    private bool isTransitioningClose;
    private bool hasAudioEnabledOverride;
    private bool audioEnabledOverrideValue;
    private bool hasUseSharedAudioSourceOverride;
    private bool useSharedAudioSourceOverrideValue;
    private AudioSource audioSourceOverride;

    // Inicializa referencias antes de usar el componente.
    private void Awake()
    {
        ApplyProfile();
        InitializeIfNeeded();
        targetVisible = gameObject.activeSelf;
    }

    // Activa listeners y estado al habilitar el objeto.
    private void OnEnable()
    {
        ApplyProfile();
        InitializeIfNeeded();
        targetVisible = true;

        if (playOpenOnEnable && !suppressOpenAnimation)
        {
            PlayOpen();
        }

        suppressOpenAnimation = false;
    }

    // Libera listeners y estado al deshabilitar el objeto.
    private void OnDisable()
    {
        StopActiveRoutine();
        isTransitioningOpen = false;
        isTransitioningClose = false;
        targetVisible = false;
        suppressOpenAnimation = false;
    }

    // Configura theme.
    public void ConfigureTheme(
        bool themedPlayOpenOnEnable,
        float themedOpenDuration,
        float themedCloseDuration,
        float themedSlideOffset,
        float themedStartScale,
        float themedCloseScale)
    {
        playOpenOnEnable = themedPlayOpenOnEnable;
        openDuration = themedOpenDuration;
        closeDuration = themedCloseDuration;
        slideOffset = themedSlideOffset;
        startScale = themedStartScale;
        closeScale = themedCloseScale;
    }

    // Configura audio.
    public void ConfigureAudio(
        bool themedAudioEnabled,
        float themedOpenVolume,
        float themedCloseVolume,
        AudioSource themedAudioSource = null)
    {
        enableAudio = themedAudioEnabled;
        openVolume = themedOpenVolume;
        closeVolume = themedCloseVolume;

        if (themedAudioSource != null)
        {
            audioSource = themedAudioSource;
        }
    }

    // Permite forzar ajustes temporales de audio del panel sin rehacer el prefab.
    public void ApplyDebugAudioOverrides(bool? overrideAudioEnabled = null, bool? overrideUseSharedAudioSource = null, AudioSource overrideAudioSource = null)
    {
        if (overrideAudioEnabled.HasValue)
        {
            hasAudioEnabledOverride = true;
            audioEnabledOverrideValue = overrideAudioEnabled.Value;
        }

        if (overrideUseSharedAudioSource.HasValue)
        {
            hasUseSharedAudioSourceOverride = true;
            useSharedAudioSourceOverrideValue = overrideUseSharedAudioSource.Value;
        }

        if (overrideAudioSource != null)
        {
            audioSourceOverride = overrideAudioSource;
        }

        ApplyPersistentAudioOverrides();
    }

    private void ApplyProfile()
    {
        if (panelFxProfile == null)
        {
            WarnIfMissingProfile();
            return;
        }

        hasLoggedMissingProfile = false;
        playOpenOnEnable = panelFxProfile.PlayOpenOnEnable;
        openDuration = panelFxProfile.OpenDuration;
        closeDuration = panelFxProfile.CloseDuration;
        slideOffset = panelFxProfile.SlideOffset;
        startScale = panelFxProfile.StartScale;
        closeScale = panelFxProfile.CloseScale;
        disableRaycastWhileAnimating = panelFxProfile.DisableRaycastWhileAnimating;
        enableAudio = panelFxProfile.EnableAudio;
        useSharedAudioSource = panelFxProfile.UseSharedAudioSource;
        openVolume = panelFxProfile.OpenVolume;
        closeVolume = panelFxProfile.CloseVolume;
        ApplyPersistentAudioOverrides();
    }

    private void WarnIfMissingProfile()
    {
        if (hasLoggedMissingProfile)
        {
            return;
        }

        hasLoggedMissingProfile = true;
        GameDebug.Advertencia("UI", $"UIPanelFx en '{name}' no tiene UIPanelFxProfile asignado. Se usaran los valores locales.", this);
    }

    // Muestra el panel con su animación de entrada y deja la interacción lista al terminar.
    public void Show(bool instant = false)
    {
        InitializeIfNeeded();

        if (!instant && targetVisible && (gameObject.activeSelf || isTransitioningOpen))
        {
            return;
        }

        targetVisible = true;
        isTransitioningClose = false;
        suppressOpenAnimation = instant;
        gameObject.SetActive(true);

        if (instant)
        {
            ApplyOpenStateImmediate();
            ToggleRaycast(true);
            isTransitioningOpen = false;
        }
        else
        {
            isTransitioningOpen = true;
            PlayConfiguredPanelSound(true);
        }
    }

    // Oculta el panel con animación o de golpe si no hay condiciones para animar.
    public void Hide(bool instant = false)
    {
        InitializeIfNeeded();

        if (!gameObject.activeSelf && !isTransitioningClose)
        {
            targetVisible = false;
            return;
        }

        if (!instant && !targetVisible && isTransitioningClose)
        {
            return;
        }

        targetVisible = false;
        isTransitioningOpen = false;

        if (instant)
        {
            StopActiveRoutine();
            isTransitioningClose = false;
            gameObject.SetActive(false);
            return;
        }

        // Si el objeto no esta activo en jerarquía (por ejemplo, su padre se desactivo),
        // no podemos iniciar corrutinas aquí. Cerramos en modo inmediato.
        if (!gameObject.activeInHierarchy || !isActiveAndEnabled)
        {
            StopActiveRoutine();
            isTransitioningClose = false;
            gameObject.SetActive(false);
            return;
        }

        isTransitioningClose = true;
        PlayConfiguredPanelSound(false);
        StopActiveRoutine();
        activeRoutine = StartCoroutine(PlayCloseRoutine());
    }

    // Arranca la apertura y corta cualquier rutina anterior para no mezclar estados.
    public void PlayOpen()
    {
        InitializeIfNeeded();
        isTransitioningOpen = true;
        isTransitioningClose = false;

        if (!gameObject.activeInHierarchy)
        {
            isTransitioningOpen = false;
            return;
        }

        StopActiveRoutine();
        activeRoutine = StartCoroutine(PlayOpenRoutine());
    }

    // Prepara referencias y estado solo la primera vez que haga falta.
    private void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null && !missingCanvasGroupWarningShown)
        {
            missingCanvasGroupWarningShown = true;
            GameDebug.Advertencia("UI", $"UIPanelFx en '{name}' no tiene CanvasGroup asignado. Fade/raycast desactivado.", this);
        }

        baseAnchoredPosition = rectTransform.anchoredPosition;
        baseScale = rectTransform.localScale;
        initialized = true;
    }

    // Corrutina de apertura: anima alpha, escala y raycasts hasta dejar el panel listo.
    private IEnumerator PlayOpenRoutine()
    {
        float duration = Mathf.Max(0.01f, openDuration);
        float elapsed = 0f;
        ToggleRaycast(false);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutBack(t);
            float alpha = EaseOutCubic(t);

            rectTransform.anchoredPosition = baseAnchoredPosition + Vector2.up * Mathf.Lerp(slideOffset, 0f, eased);
            float scaleMultiplier = Mathf.Lerp(startScale, 1f, eased);
            rectTransform.localScale = baseScale * scaleMultiplier;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }

            yield return null;
        }

        ApplyOpenStateImmediate();
        ToggleRaycast(true);
        isTransitioningOpen = false;
        activeRoutine = null;
    }

    // Corrutina de cierre: revierte la apertura y decide cuándo bloquear interacción.
    private IEnumerator PlayCloseRoutine()
    {
        float duration = Mathf.Max(0.01f, closeDuration);
        float elapsed = 0f;
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector3 startLocalScale = rectTransform.localScale;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        ToggleRaycast(false);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseInCubic(t);

            rectTransform.anchoredPosition =
                Vector2.LerpUnclamped(startPosition, baseAnchoredPosition + Vector2.down * (slideOffset * 0.45f), eased);
            rectTransform.localScale = Vector3.LerpUnclamped(startLocalScale, baseScale * closeScale, eased);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
            }

            yield return null;
        }

        gameObject.SetActive(false);
        isTransitioningClose = false;
        activeRoutine = null;
    }

    // Fuerza el estado visual final sin animación, útil para sincronizar o reparar el panel.
    private void ApplyOpenStateImmediate()
    {
        if (!initialized)
        {
            return;
        }

        rectTransform.anchoredPosition = baseAnchoredPosition;
        rectTransform.localScale = baseScale;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    // Activa o bloquea interacción del panel según su visibilidad real.
    private void ToggleRaycast(bool enabled)
    {
        if (canvasGroup == null || !disableRaycastWhileAnimating)
        {
            return;
        }

        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
    }

    // Corta la corrutina activa antes de arrancar otra para no mezclar dos animaciones.
    private void StopActiveRoutine()
    {
        if (activeRoutine == null)
        {
            return;
        }

        StopCoroutine(activeRoutine);
        activeRoutine = null;
    }

    // Curva suave que sale rápido y aterriza despacio.
    private float EaseOutCubic(float t)
    {
        float clamped = Mathf.Clamp01(t);
        float inverse = 1f - clamped;
        return 1f - inverse * inverse * inverse;
    }

    // Curva suave que arranca despacio y acelera hacia el final.
    private float EaseInCubic(float t)
    {
        float clamped = Mathf.Clamp01(t);
        return clamped * clamped * clamped;
    }

    // Curva con pequeño rebote para que la entrada se sienta más viva.
    private float EaseOutBack(float t)
    {
        float clamped = Mathf.Clamp01(t);
        const float overshoot = 1.70158f;
        float adjusted = clamped - 1f;
        return 1f + adjusted * adjusted * ((overshoot + 1f) * adjusted + overshoot);
    }

    // Reproduce el sonido del panel usando la mejor fuente disponible.
    private void PlayPanelSound(AudioClip clip, float volume)
    {
        if (!enableAudio || clip == null)
        {
            return;
        }

        AudioSource source = ResolveAudioSource();

        if (source == null)
        {
            return;
        }

        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // Busca un AudioSource local o uno compartido para poder lanzar sonidos UI.
    private AudioSource ResolveAudioSource()
    {
        if (audioSource != null)
        {
            return audioSource;
        }

        if (!useSharedAudioSource)
        {
            return null;
        }

        AudioManager resolvedAudioManager = ResolveAudioManager();

        if (resolvedAudioManager != null && resolvedAudioManager.SharedSfxSource != null)
        {
            return resolvedAudioManager.SharedSfxSource;
        }

        if (!missingSharedAudioWarningShown)
        {
            missingSharedAudioWarningShown = true;
            GameDebug.Advertencia(
                "UI",
                $"UIPanelFx en '{name}' necesita un AudioSource local o un AudioManager con SFX asignado.",
                this);
        }

        return null;
    }

    private AudioClip ResolveOpenClip()
    {
        if (uiAudioProfile != null)
        {
            return ResolveProfileOpenClip();
        }

        AudioManager resolvedAudioManager = ResolveAudioManager();
        if (resolvedAudioManager == null)
        {
            return null;
        }

        return ResolveManagerOpenClip(resolvedAudioManager);
    }

    private AudioClip ResolveCloseClip()
    {
        if (uiAudioProfile != null)
        {
            return ResolveProfileCloseClip();
        }

        AudioManager resolvedAudioManager = ResolveAudioManager();
        if (resolvedAudioManager == null)
        {
            return null;
        }

        return ResolveManagerCloseClip(resolvedAudioManager);
    }

    private float ResolveOpenAssetVolume()
    {
        if (uiAudioProfile != null)
        {
            return ResolveProfileOpenVolume();
        }

        AudioManager resolvedAudioManager = ResolveAudioManager();
        if (resolvedAudioManager == null)
        {
            return 1f;
        }

        return ResolveManagerOpenVolume(resolvedAudioManager);
    }

    private float ResolveCloseAssetVolume()
    {
        if (uiAudioProfile != null)
        {
            return ResolveProfileCloseVolume();
        }

        AudioManager resolvedAudioManager = ResolveAudioManager();
        if (resolvedAudioManager == null)
        {
            return 1f;
        }

        return ResolveManagerCloseVolume(resolvedAudioManager);
    }

    private void PlayConfiguredPanelSound(bool isOpenSound)
    {
        AudioClip clip = isOpenSound ? ResolveOpenClip() : ResolveCloseClip();
        float baseVolume = isOpenSound ? openVolume : closeVolume;
        float assetVolume = isOpenSound ? ResolveOpenAssetVolume() : ResolveCloseAssetVolume();
        PlayPanelSound(clip, baseVolume * assetVolume);
    }

    private AudioClip ResolveProfileOpenClip()
    {
        if (uiAudioProfile.PanelOpenClip != null)
        {
            return uiAudioProfile.PanelOpenClip;
        }

        if (uiAudioProfile.HoverClip != null)
        {
            return uiAudioProfile.HoverClip;
        }

        return uiAudioProfile.ClickClip;
    }

    private AudioClip ResolveProfileCloseClip()
    {
        return uiAudioProfile.PanelCloseClip != null ? uiAudioProfile.PanelCloseClip : uiAudioProfile.ClickClip;
    }

    private float ResolveProfileOpenVolume()
    {
        if (uiAudioProfile.PanelOpenClip != null)
        {
            return uiAudioProfile.PanelOpenVolume;
        }

        return uiAudioProfile.HoverClip != null ? uiAudioProfile.HoverVolume : uiAudioProfile.ClickVolume;
    }

    private float ResolveProfileCloseVolume()
    {
        return uiAudioProfile.PanelCloseClip != null ? uiAudioProfile.PanelCloseVolume : uiAudioProfile.ClickVolume;
    }

    private static AudioClip ResolveManagerOpenClip(AudioManager resolvedAudioManager)
    {
        AudioClip openFallback = resolvedAudioManager.GetUiPanelOpenClip();

        if (openFallback == null)
        {
            openFallback = resolvedAudioManager.GetUiHoverClip();
        }

        return openFallback != null ? openFallback : resolvedAudioManager.GetUiClickClip();
    }

    private static AudioClip ResolveManagerCloseClip(AudioManager resolvedAudioManager)
    {
        AudioClip closeFallback = resolvedAudioManager.GetUiPanelCloseClip();
        return closeFallback != null ? closeFallback : resolvedAudioManager.GetUiClickClip();
    }

    private static float ResolveManagerOpenVolume(AudioManager resolvedAudioManager)
    {
        AudioClip openFallback = resolvedAudioManager.GetUiPanelOpenClip();

        if (openFallback != null)
        {
            return resolvedAudioManager.GetUiPanelOpenVolume();
        }

        openFallback = resolvedAudioManager.GetUiHoverClip();
        return openFallback != null ? resolvedAudioManager.GetUiHoverVolume() : resolvedAudioManager.GetUiClickVolume();
    }

    private static float ResolveManagerCloseVolume(AudioManager resolvedAudioManager)
    {
        AudioClip closeFallback = resolvedAudioManager.GetUiPanelCloseClip();
        return closeFallback != null ? resolvedAudioManager.GetUiPanelCloseVolume() : resolvedAudioManager.GetUiClickVolume();
    }

    private void ApplyPersistentAudioOverrides()
    {
        if (hasAudioEnabledOverride)
        {
            enableAudio = audioEnabledOverrideValue;
        }

        if (hasUseSharedAudioSourceOverride)
        {
            useSharedAudioSource = useSharedAudioSourceOverrideValue;
        }

        if (audioSourceOverride != null)
        {
            audioSource = audioSourceOverride;
        }
    }

    private AudioManager ResolveAudioManager()
    {
        if (audioManager != null)
        {
            return audioManager;
        }

        audioManager = AudioManager.Instance;
        return audioManager;
    }

}
