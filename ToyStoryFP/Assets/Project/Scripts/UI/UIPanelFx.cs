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
    private static AudioClip cachedFallbackOpenClip;
    private static AudioClip cachedFallbackCloseClip;

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

    // Gestiona show.
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

    // Gestiona hide.
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

    // Reproduce abrir.
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

    // Inicializa si needed.
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

    // Reproduce abrir rutina.
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

    // Reproduce close rutina.
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

    // Aplica abrir estado inmediato.
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

    // Alterna raycast.
    private void ToggleRaycast(bool enabled)
    {
        if (canvasGroup == null || !disableRaycastWhileAnimating)
        {
            return;
        }

        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
    }

    // Detiene activo rutina.
    private void StopActiveRoutine()
    {
        if (activeRoutine == null)
        {
            return;
        }

        StopCoroutine(activeRoutine);
        activeRoutine = null;
    }

    // Gestiona ease salida cubic.
    private float EaseOutCubic(float t)
    {
        float clamped = Mathf.Clamp01(t);
        float inverse = 1f - clamped;
        return 1f - inverse * inverse * inverse;
    }

    // Gestiona ease en cubic.
    private float EaseInCubic(float t)
    {
        float clamped = Mathf.Clamp01(t);
        return clamped * clamped * clamped;
    }

    // Gestiona ease salida back.
    private float EaseOutBack(float t)
    {
        float clamped = Mathf.Clamp01(t);
        const float overshoot = 1.70158f;
        float adjusted = clamped - 1f;
        return 1f + adjusted * adjusted * ((overshoot + 1f) * adjusted + overshoot);
    }

    // Reproduce panel sound.
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

    // Resuelve audio origen.
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

    // Obtiene respaldo abrir clip.
    private AudioClip GetFallbackOpenClip()
    {
        if (!useAudioManagerFallback)
        {
            return null;
        }

        if (cachedFallbackOpenClip != null)
        {
            return cachedFallbackOpenClip;
        }

        cachedFallbackOpenClip = FindAudioClipInManager("switch");

        if (cachedFallbackOpenClip == null)
        {
            cachedFallbackOpenClip = FindAudioClipInManager("click");
        }

        return cachedFallbackOpenClip;
    }

    // Obtiene respaldo close clip.
    private AudioClip GetFallbackCloseClip()
    {
        if (!useAudioManagerFallback)
        {
            return null;
        }

        if (cachedFallbackCloseClip != null)
        {
            return cachedFallbackCloseClip;
        }

        cachedFallbackCloseClip = FindAudioClipInManager("click");
        return cachedFallbackCloseClip;
    }

    // Busca audio clip en gestor.
    private AudioClip FindAudioClipInManager(string token)
    {
        if (AudioManager.Instance == null || AudioManager.Instance.SfxList == null)
        {
            return null;
        }

        AudioClip[] sfx = AudioManager.Instance.SfxList;
        string search = token != null ? token.ToLowerInvariant() : string.Empty;

        for (int i = 0; i < sfx.Length; i++)
        {
            AudioClip clip = sfx[i];

            if (clip == null)
            {
                continue;
            }

            string clipName = clip.name != null ? clip.name.ToLowerInvariant() : string.Empty;

            if (!string.IsNullOrEmpty(search) && clipName.Contains(search))
            {
                return clip;
            }
        }

        return sfx.Length > 0 ? sfx[0] : null;
    }
}
