using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private const float DefaultHoverInterval = 0.08f;

    [Header("Visual")]
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private bool affectGraphicColor = true;
    [SerializeField] private Color normalColor = new Color(1f, 0.95f, 0.78f, 1f);
    [SerializeField] private Color hoverColor = new Color(1f, 0.98f, 0.9f, 1f);
    [SerializeField] private Color pressedColor = new Color(1f, 0.85f, 0.62f, 1f);
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float pressedScale = 0.94f;
    [SerializeField] private float animationSpeed = 16f;

    [Header("Audio")]
    [SerializeField] private bool enableAudio = true;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool useSharedAudioSource = true;
    [SerializeField] private bool useAudioManagerFallback;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private float hoverVolume = 0.35f;
    [SerializeField] private float clickVolume = 0.6f;
    [SerializeField] private float pitchRandomness = 0.04f;
    [SerializeField] private float hoverMinInterval = DefaultHoverInterval;

    private static AudioSource sharedAudioSource;
    private Button button;
    private RectTransform rectTransform;
    private Vector3 baseScale = Vector3.one;
    private Color baseColor = Color.white;
    private bool isHovered;
    private bool isPressed;
    private float lastHoverSfxTime = -100f;
    private bool listenerBound;

    // Inicializa referencias antes de usar el componente.
    private void Awake()
    {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        targetGraphic = targetGraphic != null ? targetGraphic : button.targetGraphic;
        baseScale = rectTransform.localScale;

        if (targetGraphic != null)
        {
            baseColor = targetGraphic.color;
        }
    }

    // Activa listeners y estado al habilitar el objeto.
    private void OnEnable()
    {
        BindButtonListener();
        RestoreVisualStateImmediate();
    }

    // Libera listeners y estado al deshabilitar el objeto.
    private void OnDisable()
    {
        UnbindButtonListener();
        RestoreVisualStateImmediate();
    }

    // Mantiene viva la animación visual del botón mientras haya transición en curso.
    private void Update()
    {
        AnimateVisual();
    }

    // Configura theme.
    public void ConfigureTheme(
        Color themedNormalColor,
        Color themedHoverColor,
        Color themedPressedColor,
        float themedHoverScale,
        float themedPressedScale,
        float themedAnimationSpeed,
        bool themedAudioEnabled)
    {
        normalColor = themedNormalColor;
        hoverColor = themedHoverColor;
        pressedColor = themedPressedColor;
        hoverScale = themedHoverScale;
        pressedScale = themedPressedScale;
        animationSpeed = themedAnimationSpeed;
        enableAudio = themedAudioEnabled;
    }

    // Configura audio.
    public void ConfigureAudio(
        float themedHoverVolume,
        float themedClickVolume,
        float themedPitchRandomness,
        float themedHoverMinInterval,
        bool themedUseAudioManagerFallback,
        AudioSource themedAudioSource = null)
    {
        hoverVolume = themedHoverVolume;
        clickVolume = themedClickVolume;
        pitchRandomness = themedPitchRandomness;
        hoverMinInterval = themedHoverMinInterval;
        useAudioManagerFallback = themedUseAudioManagerFallback;

        if (themedAudioSource != null)
        {
            audioSource = themedAudioSource;
        }
    }

    // Completa clips vacíos con opciones de respaldo para no dejar botones mudos.
    public void SetAudioClips(AudioClip hover, AudioClip click)
    {
        hoverClip = hover;
        clickClip = click;
    }

    // Reacción al entrar con el puntero: marca hover y dispara feedback.
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        TryPlayHoverSound();
    }

    // Reacción al salir con el puntero: limpia hover y devuelve el botón a reposo.
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }

    // Reacción al pulsar: registra el botón del ratón y activa la pose de presión.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsLeftMouse(eventData))
        {
            return;
        }

        isPressed = true;
    }

    // Reacción al soltar: solo reproduce click si la liberación corresponde al botón válido.
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsLeftMouse(eventData))
        {
            return;
        }

        isPressed = false;
    }

    // Engancha el onClick del Button una sola vez para no duplicar sonidos.
    private void BindButtonListener()
    {
        if (button == null || listenerBound)
        {
            return;
        }

        button.onClick.RemoveListener(HandleButtonClicked);
        button.onClick.AddListener(HandleButtonClicked);
        listenerBound = true;
    }

    // Desengancha el listener al desactivar el componente.
    private void UnbindButtonListener()
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(HandleButtonClicked);
        listenerBound = false;
    }

    // Feedback centralizado del click, tanto para ratón como para activación por UI.
    private void HandleButtonClicked()
    {
        if (!enableAudio)
        {
            return;
        }

        AudioClip clip = clickClip != null ? clickClip : GetFallbackClickClip();
        PlayOneShot(clip, clickVolume);
    }

    // Interpola escala y color para que el botón no cambie de estado a saltos.
    private void AnimateVisual()
    {
        if (rectTransform == null)
        {
            return;
        }

        float targetScaleMultiplier = 1f;

        if (isPressed)
        {
            targetScaleMultiplier = Mathf.Max(0.6f, pressedScale);
        }
        else if (isHovered)
        {
            targetScaleMultiplier = Mathf.Max(1f, hoverScale);
        }

        float speed = Mathf.Max(1f, animationSpeed);
        Vector3 targetScale = baseScale * targetScaleMultiplier;
        float lerpT = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, lerpT);

        if (!affectGraphicColor || targetGraphic == null)
        {
            return;
        }

        Color targetColor = normalColor;

        if (isPressed)
        {
            targetColor = pressedColor;
        }
        else if (isHovered)
        {
            targetColor = hoverColor;
        }

        targetGraphic.color = Color.Lerp(targetGraphic.color, targetColor, lerpT);
    }

    // Fuerza el estado visual final sin esperar a la animación.
    private void RestoreVisualStateImmediate()
    {
        isHovered = false;
        isPressed = false;

        if (rectTransform != null)
        {
            rectTransform.localScale = baseScale;
        }

        if (targetGraphic != null)
        {
            targetGraphic.color = affectGraphicColor ? normalColor : baseColor;
        }
    }

    // Lanza el sonido de hover solo si toca, evitando repetirlo de forma molesta.
    private void TryPlayHoverSound()
    {
        if (!enableAudio)
        {
            return;
        }

        float minInterval = Mathf.Max(0f, hoverMinInterval);

        if (Time.unscaledTime < lastHoverSfxTime + minInterval)
        {
            return;
        }

        lastHoverSfxTime = Time.unscaledTime;

        AudioClip clip = hoverClip != null ? hoverClip : GetFallbackHoverClip();
        PlayOneShot(clip, hoverVolume);
    }

    // Acepta solo clic izquierdo como pulsación válida de ratón.
    private bool IsLeftMouse(PointerEventData eventData)
    {
        return eventData == null || eventData.button == PointerEventData.InputButton.Left;
    }

    // Reproduce un sonido UI usando la mejor ruta de audio disponible.
    private void PlayOneShot(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource source = ResolveAudioSource();

        if (source == null)
        {
            return;
        }

        float clampedVolume = Mathf.Clamp01(volume);
        float randomPitch = 1f + Random.Range(-Mathf.Abs(pitchRandomness), Mathf.Abs(pitchRandomness));
        source.pitch = Mathf.Clamp(randomPitch, 0.75f, 1.25f);
        source.PlayOneShot(clip, clampedVolume);
        source.pitch = 1f;
    }

    // Busca de dónde sacar el audio: fuente propia, heredada o compartida.
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

        GameObject audioRoot = new GameObject("UIFX_Audio");
        DontDestroyOnLoad(audioRoot);
        sharedAudioSource = audioRoot.AddComponent<AudioSource>();
        sharedAudioSource.playOnAwake = false;
        sharedAudioSource.spatialBlend = 0f;
        sharedAudioSource.loop = false;
        return sharedAudioSource;
    }

    // Intenta encontrar un clip de click de respaldo en el AudioManager.
    private AudioClip GetFallbackClickClip()
    {
        if (!useAudioManagerFallback)
        {
            return null;
        }

        return AudioManager.Instance != null ? AudioManager.Instance.GetUiClickClip() : null;
    }

    // Intenta encontrar un clip de hover de respaldo en el AudioManager.
    private AudioClip GetFallbackHoverClip()
    {
        if (!useAudioManagerFallback)
        {
            return null;
        }

        if (AudioManager.Instance == null)
        {
            return null;
        }

        AudioClip hoverFallback = AudioManager.Instance.GetUiHoverClip();
        return hoverFallback != null ? hoverFallback : AudioManager.Instance.GetUiClickClip();
    }
}
