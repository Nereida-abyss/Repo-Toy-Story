using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "UIThemeProfile_Default", menuName = "ToyStory/UI Theme Profile")]
public class UIThemeProfile : ScriptableObject
{
    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset titleFont;
    [SerializeField] private TMP_FontAsset bodyFont;

    [Header("Panel Colors")]
    [SerializeField] private Color panelColor = new Color(0.08f, 0.16f, 0.33f, 0.84f);

    [Header("Button Colors")]
    [SerializeField] private Color buttonNormalColor = new Color(1f, 0.95f, 0.78f, 0.96f);
    [SerializeField] private Color buttonHighlightedColor = new Color(1f, 0.98f, 0.9f, 1f);
    [SerializeField] private Color buttonPressedColor = new Color(1f, 0.84f, 0.58f, 1f);
    [SerializeField] private Color buttonSelectedColor = new Color(1f, 0.98f, 0.9f, 1f);
    [SerializeField] private Color buttonDisabledColor = new Color(0.62f, 0.67f, 0.78f, 0.75f);
    [SerializeField] private float buttonFadeDuration = 0.07f;

    [Header("Text Colors")]
    [SerializeField] private Color titleTextColor = new Color(1f, 0.97f, 0.79f, 1f);
    [SerializeField] private Color bodyTextColor = new Color(0.93f, 0.97f, 1f, 1f);
    [SerializeField] private Color secondaryTextColor = new Color(0.8f, 0.92f, 1f, 1f);
    [SerializeField] private Color shadowColor = new Color(0.04f, 0.07f, 0.14f, 0.65f);
    [SerializeField] private Vector2 titleShadowDistance = new Vector2(2.8f, -2.8f);
    [SerializeField] private Vector2 bodyShadowDistance = new Vector2(2f, -2f);

    [Header("Button Fx")]
    [SerializeField] private float buttonHoverScale = 1.07f;
    [SerializeField] private float buttonPressedScale = 0.93f;
    [SerializeField] private float buttonAnimationSpeed = 17f;

    [Header("Button Audio")]
    [SerializeField] private bool buttonAudioEnabled = true;
    [SerializeField] private float buttonHoverVolume = 0.35f;
    [SerializeField] private float buttonClickVolume = 0.6f;
    [SerializeField] private float buttonPitchRandomness = 0.04f;
    [SerializeField] private float buttonHoverMinInterval = 0.08f;
    [SerializeField] private bool buttonUseAudioManagerFallback;

    [Header("Panel Fx")]
    [SerializeField] private bool panelPlayOpenOnEnable = true;
    [SerializeField] private float panelOpenDuration = 0.25f;
    [SerializeField] private float panelCloseDuration = 0.16f;
    [SerializeField] private float panelSlideOffset = 24f;
    [SerializeField] private float panelStartScale = 0.94f;
    [SerializeField] private float panelCloseScale = 0.97f;

    [Header("Panel Audio")]
    [SerializeField] private bool panelAudioEnabled = true;
    [SerializeField] private float panelOpenVolume = 0.5f;
    [SerializeField] private float panelCloseVolume = 0.38f;
    [SerializeField] private bool panelUseAudioManagerFallback;

    public TMP_FontAsset TitleFont => titleFont;
    public TMP_FontAsset BodyFont => bodyFont;
    public Color PanelColor => panelColor;
    public Color ButtonNormalColor => buttonNormalColor;
    public Color ButtonHighlightedColor => buttonHighlightedColor;
    public Color ButtonPressedColor => buttonPressedColor;
    public Color ButtonSelectedColor => buttonSelectedColor;
    public Color ButtonDisabledColor => buttonDisabledColor;
    public float ButtonFadeDuration => buttonFadeDuration;
    public Color TitleTextColor => titleTextColor;
    public Color BodyTextColor => bodyTextColor;
    public Color SecondaryTextColor => secondaryTextColor;
    public Color ShadowColor => shadowColor;
    public Vector2 TitleShadowDistance => titleShadowDistance;
    public Vector2 BodyShadowDistance => bodyShadowDistance;
    public float ButtonHoverScale => buttonHoverScale;
    public float ButtonPressedScale => buttonPressedScale;
    public float ButtonAnimationSpeed => buttonAnimationSpeed;
    public bool ButtonAudioEnabled => buttonAudioEnabled;
    public float ButtonHoverVolume => buttonHoverVolume;
    public float ButtonClickVolume => buttonClickVolume;
    public float ButtonPitchRandomness => buttonPitchRandomness;
    public float ButtonHoverMinInterval => buttonHoverMinInterval;
    public bool ButtonUseAudioManagerFallback => buttonUseAudioManagerFallback;
    public bool PanelPlayOpenOnEnable => panelPlayOpenOnEnable;
    public float PanelOpenDuration => panelOpenDuration;
    public float PanelCloseDuration => panelCloseDuration;
    public float PanelSlideOffset => panelSlideOffset;
    public float PanelStartScale => panelStartScale;
    public float PanelCloseScale => panelCloseScale;
    public bool PanelAudioEnabled => panelAudioEnabled;
    public float PanelOpenVolume => panelOpenVolume;
    public float PanelCloseVolume => panelCloseVolume;
    public bool PanelUseAudioManagerFallback => panelUseAudioManagerFallback;
}
