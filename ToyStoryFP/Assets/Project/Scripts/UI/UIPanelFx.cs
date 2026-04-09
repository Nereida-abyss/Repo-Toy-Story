using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIPanelFx : MonoBehaviour
{
    [Header("Open/Close")]
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
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool useSharedAudioSource = true;
    [SerializeField] private bool useAudioManagerFallback;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [SerializeField] private float openVolume = 0.5f;
    [SerializeField] private float closeVolume = 0.38f;

    private static AudioSource sharedAudioSource;
    private RectTransform rectTransform;
    private Coroutine activeRoutine;
    private Vector2 baseAnchoredPosition;
    private Vector3 baseScale;
    private bool initialized;
    private bool suppressOpenAnimation;
    private bool missingCanvasGroupWarningShown;

    // Inicializa referencias antes de usar el componente.
    private void Awake()
    {
        InitializeIfNeeded();
    }

    // Activa listeners y estado al habilitar el objeto.
    private void OnEnable()
    {
        InitializeIfNeeded();

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
        bool themedUseAudioManagerFallback,
        AudioSource themedAudioSource = null)
    {
        enableAudio = themedAudioEnabled;
        openVolume = themedOpenVolume;
        closeVolume = themedCloseVolume;
        useAudioManagerFallback = themedUseAudioManagerFallback;

        if (themedAudioSource != null)
        {
            audioSource = themedAudioSource;
        }
    }

    // Muestra el panel con su animación de entrada y deja la interacción lista al terminar.
    public void Show(bool instant = false)
    {
        InitializeIfNeeded();
        suppressOpenAnimation = instant;
        gameObject.SetActive(true);

        if (instant)
        {
            ApplyOpenStateImmediate();
        }
        else
        {
            PlayPanelSound(openClip != null ? openClip : GetFallbackOpenClip(), openVolume);
        }
    }

    // Oculta el panel con animación o de golpe si no hay condiciones para animar.
    public void Hide(bool instant = false)
    {
        InitializeIfNeeded();

        if (!gameObject.activeSelf)
        {
            return;
        }

        if (instant)
        {
            gameObject.SetActive(false);
            return;
        }

        // Si el objeto no esta activo en jerarquía (por ejemplo, su padre se desactivo),
        // no podemos iniciar corrutinas aquí. Cerramos en modo inmediato.
        if (!gameObject.activeInHierarchy || !isActiveAndEnabled)
        {
            gameObject.SetActive(false);
            return;
        }

        PlayPanelSound(closeClip != null ? closeClip : GetFallbackCloseClip(), closeVolume);
        StopActiveRoutine();
        activeRoutine = StartCoroutine(PlayCloseRoutine());
    }

    // Arranca la apertura y corta cualquier rutina anterior para no mezclar estados.
    public void PlayOpen()
    {
        InitializeIfNeeded();

        if (!gameObject.activeInHierarchy)
        {
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

        if (sharedAudioSource != null)
        {
            return sharedAudioSource;
        }

        GameObject audioRoot = new GameObject("UIPanelFX_Audio");
        DontDestroyOnLoad(audioRoot);
        sharedAudioSource = audioRoot.AddComponent<AudioSource>();
        sharedAudioSource.playOnAwake = false;
        sharedAudioSource.spatialBlend = 0f;
        sharedAudioSource.loop = false;
        return sharedAudioSource;
    }

    // Intenta sacar un clip de apertura de respaldo desde el AudioManager.
    private AudioClip GetFallbackOpenClip()
    {
        if (!useAudioManagerFallback)
        {
            return null;
        }

        if (AudioManager.Instance == null)
        {
            return null;
        }

        AudioClip openFallback = AudioManager.Instance.GetUiPanelOpenClip();

        if (openFallback == null)
        {
            openFallback = AudioManager.Instance.GetUiHoverClip();
        }

        return openFallback != null ? openFallback : AudioManager.Instance.GetUiClickClip();
    }

    // Intenta sacar un clip de cierre de respaldo desde el AudioManager.
    private AudioClip GetFallbackCloseClip()
    {
        if (!useAudioManagerFallback)
        {
            return null;
        }

        if (AudioManager.Instance == null)
        {
            return null;
        }

        AudioClip closeFallback = AudioManager.Instance.GetUiPanelCloseClip();
        return closeFallback != null ? closeFallback : AudioManager.Instance.GetUiClickClip();
    }
}
