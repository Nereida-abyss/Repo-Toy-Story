using UnityEngine;

[CreateAssetMenu(fileName = "DefaultUIButtonFxProfile", menuName = "FX/UI Button FX Profile")]
public class UIButtonFxProfile : ScriptableObject
{
    [Header("Visual")]
    [SerializeField] private bool affectGraphicColor = true;
    [SerializeField] private Color normalColor = new Color(1f, 0.95f, 0.78f, 1f);
    [SerializeField] private Color hoverColor = new Color(1f, 0.98f, 0.9f, 1f);
    [SerializeField] private Color pressedColor = new Color(1f, 0.85f, 0.62f, 1f);
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float pressedScale = 0.94f;
    [SerializeField] private float animationSpeed = 16f;

    [Header("Audio")]
    [SerializeField] private bool enableAudio = true;
    [SerializeField] private bool useSharedAudioSource = true;
    [SerializeField] private bool useAudioManagerFallback;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private float hoverVolume = 0.35f;
    [SerializeField] private float clickVolume = 0.6f;
    [SerializeField] private float pitchRandomness = 0.04f;
    [SerializeField] private float hoverMinInterval = 0.08f;

    public bool AffectGraphicColor => affectGraphicColor;
    public Color NormalColor => normalColor;
    public Color HoverColor => hoverColor;
    public Color PressedColor => pressedColor;
    public float HoverScale => hoverScale;
    public float PressedScale => pressedScale;
    public float AnimationSpeed => animationSpeed;
    public bool EnableAudio => enableAudio;
    public bool UseSharedAudioSource => useSharedAudioSource;
    public bool UseAudioManagerFallback => useAudioManagerFallback;
    public AudioClip HoverClip => hoverClip;
    public AudioClip ClickClip => clickClip;
    public float HoverVolume => hoverVolume;
    public float ClickVolume => clickVolume;
    public float PitchRandomness => pitchRandomness;
    public float HoverMinInterval => hoverMinInterval;
}
