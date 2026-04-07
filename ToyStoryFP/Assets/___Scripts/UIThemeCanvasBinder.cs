using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIThemeCanvasBinder : MonoBehaviour
{
    [Header("Theme")]
    [SerializeField] private UIThemeProfile themeProfile;
    [SerializeField] private bool applyAtRuntime;
    [SerializeField] private bool logMissingReferences = true;

    [Header("Override: Fonts")]
    [SerializeField] private bool overrideFonts;
    [SerializeField] private TMP_FontAsset titleFontOverride;
    [SerializeField] private TMP_FontAsset bodyFontOverride;

    [Header("Override: Palette")]
    [SerializeField] private bool overridePalette;
    [SerializeField] private Color panelColorOverride = new Color(0.08f, 0.16f, 0.33f, 0.84f);
    [SerializeField] private Color buttonNormalColorOverride = new Color(1f, 0.95f, 0.78f, 0.96f);
    [SerializeField] private Color buttonHighlightedColorOverride = new Color(1f, 0.98f, 0.9f, 1f);
    [SerializeField] private Color buttonPressedColorOverride = new Color(1f, 0.84f, 0.58f, 1f);
    [SerializeField] private Color buttonSelectedColorOverride = new Color(1f, 0.98f, 0.9f, 1f);
    [SerializeField] private Color buttonDisabledColorOverride = new Color(0.62f, 0.67f, 0.78f, 0.75f);
    [SerializeField] private float buttonFadeDurationOverride = 0.07f;
    [SerializeField] private Color titleTextColorOverride = new Color(1f, 0.97f, 0.79f, 1f);
    [SerializeField] private Color bodyTextColorOverride = new Color(0.93f, 0.97f, 1f, 1f);
    [SerializeField] private Color secondaryTextColorOverride = new Color(0.8f, 0.92f, 1f, 1f);
    [SerializeField] private Color shadowColorOverride = new Color(0.04f, 0.07f, 0.14f, 0.65f);
    [SerializeField] private Vector2 titleShadowDistanceOverride = new Vector2(2.8f, -2.8f);
    [SerializeField] private Vector2 bodyShadowDistanceOverride = new Vector2(2f, -2f);

    [Header("Override: Button Fx")]
    [SerializeField] private bool overrideButtonFx;
    [SerializeField] private float buttonHoverScaleOverride = 1.07f;
    [SerializeField] private float buttonPressedScaleOverride = 0.93f;
    [SerializeField] private float buttonAnimationSpeedOverride = 17f;

    [Header("Override: Button Audio")]
    [SerializeField] private bool overrideButtonAudio;
    [SerializeField] private bool buttonAudioEnabledOverride = true;
    [SerializeField] private float buttonHoverVolumeOverride = 0.35f;
    [SerializeField] private float buttonClickVolumeOverride = 0.6f;
    [SerializeField] private float buttonPitchRandomnessOverride = 0.04f;
    [SerializeField] private float buttonHoverMinIntervalOverride = 0.08f;
    [SerializeField] private bool buttonUseAudioManagerFallbackOverride;
    [SerializeField] private AudioSource buttonAudioSourceOverride;

    [Header("Override: Panel Fx")]
    [SerializeField] private bool overridePanelFx;
    [SerializeField] private bool panelPlayOpenOnEnableOverride = true;
    [SerializeField] private float panelOpenDurationOverride = 0.25f;
    [SerializeField] private float panelCloseDurationOverride = 0.16f;
    [SerializeField] private float panelSlideOffsetOverride = 24f;
    [SerializeField] private float panelStartScaleOverride = 0.94f;
    [SerializeField] private float panelCloseScaleOverride = 0.97f;

    [Header("Override: Panel Audio")]
    [SerializeField] private bool overridePanelAudio;
    [SerializeField] private bool panelAudioEnabledOverride = true;
    [SerializeField] private float panelOpenVolumeOverride = 0.5f;
    [SerializeField] private float panelCloseVolumeOverride = 0.38f;
    [SerializeField] private bool panelUseAudioManagerFallbackOverride;
    [SerializeField] private AudioSource panelAudioSourceOverride;

    [Header("Bindings")]
    [SerializeField] private List<Image> panelImages = new List<Image>();
    [SerializeField] private List<Button> buttons = new List<Button>();
    [SerializeField] private List<TMP_Text> titleTexts = new List<TMP_Text>();
    [SerializeField] private List<TMP_Text> bodyTexts = new List<TMP_Text>();
    [SerializeField] private List<TMP_Text> secondaryTexts = new List<TMP_Text>();
    [SerializeField] private List<UIButtonFx> buttonFxTargets = new List<UIButtonFx>();
    [SerializeField] private List<UIPanelFx> panelFxTargets = new List<UIPanelFx>();

    private bool missingThemeWarningShown;

    private void OnEnable()
    {
        if (!applyAtRuntime || !Application.isPlaying)
        {
            return;
        }

        ApplyThemeNow();
    }

    [ContextMenu("Auto Populate Bindings")]
    public void AutoPopulateBindings()
    {
        panelImages.Clear();
        buttons.Clear();
        titleTexts.Clear();
        bodyTexts.Clear();
        secondaryTexts.Clear();
        buttonFxTargets.Clear();
        panelFxTargets.Clear();

        Button[] sceneButtons = GetComponentsInChildren<Button>(true);
        TMP_Text[] sceneTexts = GetComponentsInChildren<TMP_Text>(true);
        Image[] sceneImages = GetComponentsInChildren<Image>(true);
        UIButtonFx[] sceneButtonFx = GetComponentsInChildren<UIButtonFx>(true);
        UIPanelFx[] scenePanelFx = GetComponentsInChildren<UIPanelFx>(true);

        for (int i = 0; i < sceneButtons.Length; i++)
        {
            if (sceneButtons[i] != null)
            {
                buttons.Add(sceneButtons[i]);
            }
        }

        for (int i = 0; i < sceneImages.Length; i++)
        {
            Image image = sceneImages[i];

            if (image == null)
            {
                continue;
            }

            if (image.GetComponentInParent<Button>(true) != null)
            {
                continue;
            }

            panelImages.Add(image);
        }

        for (int i = 0; i < sceneTexts.Length; i++)
        {
            TMP_Text text = sceneTexts[i];

            if (text == null)
            {
                continue;
            }

            string normalizedName = NormalizeName(text.gameObject.name);

            if (normalizedName.Contains("title")
                || normalizedName.Contains("gameover")
                || normalizedName.Contains("scoretitle")
                || normalizedName.Contains("waveannouncement"))
            {
                titleTexts.Add(text);
            }
            else if (normalizedName.Contains("prompt")
                || normalizedName.Contains("timer")
                || normalizedName.Contains("coins")
                || normalizedName.Contains("wave")
                || normalizedName.Contains("bots")
                || normalizedName.Contains("reload")
                || normalizedName.Contains("health")
                || normalizedName.Contains("ammo"))
            {
                secondaryTexts.Add(text);
            }
            else
            {
                bodyTexts.Add(text);
            }
        }

        for (int i = 0; i < sceneButtonFx.Length; i++)
        {
            if (sceneButtonFx[i] != null)
            {
                buttonFxTargets.Add(sceneButtonFx[i]);
            }
        }

        for (int i = 0; i < scenePanelFx.Length; i++)
        {
            if (scenePanelFx[i] != null)
            {
                panelFxTargets.Add(scenePanelFx[i]);
            }
        }
    }

    [ContextMenu("Apply Theme Now")]
    public void ApplyThemeNow()
    {
        if (themeProfile == null)
        {
            if (!missingThemeWarningShown && logMissingReferences)
            {
                missingThemeWarningShown = true;
                Debug.LogWarning("UIThemeCanvasBinder is missing themeProfile.", this);
            }

            return;
        }

        missingThemeWarningShown = false;
        CleanupNullReferences();
        ApplyPanelImages();
        ApplyButtons();
        ApplyTexts();
        ApplyButtonFx();
        ApplyPanelFx();
    }

    [ContextMenu("Reset To Theme Defaults")]
    public void ResetToThemeDefaults()
    {
        overrideFonts = false;
        overridePalette = false;
        overrideButtonFx = false;
        overrideButtonAudio = false;
        overridePanelFx = false;
        overridePanelAudio = false;
        ApplyThemeNow();
    }

    private void ApplyPanelImages()
    {
        Color panelColor = GetPanelColor();

        for (int i = 0; i < panelImages.Count; i++)
        {
            Image image = panelImages[i];

            if (image == null)
            {
                continue;
            }

            Color color = panelColor;
            color.a = Mathf.Clamp(image.color.a, 0f, 1f);
            image.color = color;
        }
    }

    private void ApplyButtons()
    {
        Color normal = GetButtonNormalColor();
        Color highlighted = GetButtonHighlightedColor();
        Color pressed = GetButtonPressedColor();
        Color selected = GetButtonSelectedColor();
        Color disabled = GetButtonDisabledColor();
        float fadeDuration = Mathf.Max(0f, GetButtonFadeDuration());

        for (int i = 0; i < buttons.Count; i++)
        {
            Button button = buttons[i];

            if (button == null)
            {
                continue;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.selectedColor = selected;
            colors.disabledColor = disabled;
            colors.fadeDuration = fadeDuration;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;
        }
    }

    private void ApplyTexts()
    {
        TMP_FontAsset titleFont = GetTitleFont();
        TMP_FontAsset bodyFont = GetBodyFont();
        Color titleColor = GetTitleTextColor();
        Color bodyColor = GetBodyTextColor();
        Color secondaryColor = GetSecondaryTextColor();
        Color shadowColor = GetShadowColor();
        Vector2 titleShadowDistance = GetTitleShadowDistance();
        Vector2 bodyShadowDistance = GetBodyShadowDistance();

        for (int i = 0; i < titleTexts.Count; i++)
        {
            ApplyTextStyle(titleTexts[i], titleFont, titleColor, shadowColor, titleShadowDistance, FontStyles.Bold);
        }

        for (int i = 0; i < bodyTexts.Count; i++)
        {
            ApplyTextStyle(bodyTexts[i], bodyFont, bodyColor, shadowColor, bodyShadowDistance, FontStyles.Normal);
        }

        for (int i = 0; i < secondaryTexts.Count; i++)
        {
            ApplyTextStyle(secondaryTexts[i], bodyFont, secondaryColor, shadowColor, bodyShadowDistance, FontStyles.Normal);
        }
    }

    private void ApplyTextStyle(
        TMP_Text text,
        TMP_FontAsset font,
        Color textColor,
        Color shadowColor,
        Vector2 shadowDistance,
        FontStyles style)
    {
        if (text == null)
        {
            return;
        }

        if (font != null)
        {
            text.font = font;
        }

        text.color = textColor;
        text.fontStyle = style;

        Shadow shadow = text.GetComponent<Shadow>();

        if (shadow == null)
        {
            return;
        }

        shadow.effectColor = shadowColor;
        shadow.effectDistance = shadowDistance;
        shadow.useGraphicAlpha = true;
    }

    private void ApplyButtonFx()
    {
        HashSet<UIButtonFx> uniqueFx = new HashSet<UIButtonFx>();

        for (int i = 0; i < buttonFxTargets.Count; i++)
        {
            UIButtonFx fx = buttonFxTargets[i];

            if (fx != null)
            {
                uniqueFx.Add(fx);
            }
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            Button button = buttons[i];

            if (button == null)
            {
                continue;
            }

            UIButtonFx fx = button.GetComponent<UIButtonFx>();

            if (fx != null)
            {
                uniqueFx.Add(fx);
            }
            else if (logMissingReferences)
            {
                Debug.LogWarning($"Button '{button.name}' has no UIButtonFx assigned.", button);
            }
        }

        foreach (UIButtonFx fx in uniqueFx)
        {
            fx.ConfigureTheme(
                GetButtonNormalColor(),
                GetButtonHighlightedColor(),
                GetButtonPressedColor(),
                GetButtonHoverScale(),
                GetButtonPressedScale(),
                GetButtonAnimationSpeed(),
                GetButtonAudioEnabled());

            fx.ConfigureAudio(
                GetButtonHoverVolume(),
                GetButtonClickVolume(),
                GetButtonPitchRandomness(),
                GetButtonHoverMinInterval(),
                GetButtonUseAudioManagerFallback(),
                buttonAudioSourceOverride);
        }
    }

    private void ApplyPanelFx()
    {
        for (int i = 0; i < panelFxTargets.Count; i++)
        {
            UIPanelFx panelFx = panelFxTargets[i];

            if (panelFx == null)
            {
                continue;
            }

            panelFx.ConfigureTheme(
                GetPanelPlayOpenOnEnable(),
                GetPanelOpenDuration(),
                GetPanelCloseDuration(),
                GetPanelSlideOffset(),
                GetPanelStartScale(),
                GetPanelCloseScale());

            panelFx.ConfigureAudio(
                GetPanelAudioEnabled(),
                GetPanelOpenVolume(),
                GetPanelCloseVolume(),
                GetPanelUseAudioManagerFallback(),
                panelAudioSourceOverride);
        }
    }

    private void CleanupNullReferences()
    {
        panelImages.RemoveAll(entry => entry == null);
        buttons.RemoveAll(entry => entry == null);
        titleTexts.RemoveAll(entry => entry == null);
        bodyTexts.RemoveAll(entry => entry == null);
        secondaryTexts.RemoveAll(entry => entry == null);
        buttonFxTargets.RemoveAll(entry => entry == null);
        panelFxTargets.RemoveAll(entry => entry == null);
    }

    private TMP_FontAsset GetTitleFont()
    {
        if (overrideFonts && titleFontOverride != null)
        {
            return titleFontOverride;
        }

        return themeProfile != null ? themeProfile.TitleFont : null;
    }

    private TMP_FontAsset GetBodyFont()
    {
        if (overrideFonts && bodyFontOverride != null)
        {
            return bodyFontOverride;
        }

        return themeProfile != null ? themeProfile.BodyFont : null;
    }

    private Color GetPanelColor()
    {
        return overridePalette ? panelColorOverride : themeProfile.PanelColor;
    }

    private Color GetButtonNormalColor()
    {
        return overridePalette ? buttonNormalColorOverride : themeProfile.ButtonNormalColor;
    }

    private Color GetButtonHighlightedColor()
    {
        return overridePalette ? buttonHighlightedColorOverride : themeProfile.ButtonHighlightedColor;
    }

    private Color GetButtonPressedColor()
    {
        return overridePalette ? buttonPressedColorOverride : themeProfile.ButtonPressedColor;
    }

    private Color GetButtonSelectedColor()
    {
        return overridePalette ? buttonSelectedColorOverride : themeProfile.ButtonSelectedColor;
    }

    private Color GetButtonDisabledColor()
    {
        return overridePalette ? buttonDisabledColorOverride : themeProfile.ButtonDisabledColor;
    }

    private float GetButtonFadeDuration()
    {
        return overridePalette ? buttonFadeDurationOverride : themeProfile.ButtonFadeDuration;
    }

    private Color GetTitleTextColor()
    {
        return overridePalette ? titleTextColorOverride : themeProfile.TitleTextColor;
    }

    private Color GetBodyTextColor()
    {
        return overridePalette ? bodyTextColorOverride : themeProfile.BodyTextColor;
    }

    private Color GetSecondaryTextColor()
    {
        return overridePalette ? secondaryTextColorOverride : themeProfile.SecondaryTextColor;
    }

    private Color GetShadowColor()
    {
        return overridePalette ? shadowColorOverride : themeProfile.ShadowColor;
    }

    private Vector2 GetTitleShadowDistance()
    {
        return overridePalette ? titleShadowDistanceOverride : themeProfile.TitleShadowDistance;
    }

    private Vector2 GetBodyShadowDistance()
    {
        return overridePalette ? bodyShadowDistanceOverride : themeProfile.BodyShadowDistance;
    }

    private float GetButtonHoverScale()
    {
        return overrideButtonFx ? buttonHoverScaleOverride : themeProfile.ButtonHoverScale;
    }

    private float GetButtonPressedScale()
    {
        return overrideButtonFx ? buttonPressedScaleOverride : themeProfile.ButtonPressedScale;
    }

    private float GetButtonAnimationSpeed()
    {
        return overrideButtonFx ? buttonAnimationSpeedOverride : themeProfile.ButtonAnimationSpeed;
    }

    private bool GetButtonAudioEnabled()
    {
        return overrideButtonAudio ? buttonAudioEnabledOverride : themeProfile.ButtonAudioEnabled;
    }

    private float GetButtonHoverVolume()
    {
        return overrideButtonAudio ? buttonHoverVolumeOverride : themeProfile.ButtonHoverVolume;
    }

    private float GetButtonClickVolume()
    {
        return overrideButtonAudio ? buttonClickVolumeOverride : themeProfile.ButtonClickVolume;
    }

    private float GetButtonPitchRandomness()
    {
        return overrideButtonAudio ? buttonPitchRandomnessOverride : themeProfile.ButtonPitchRandomness;
    }

    private float GetButtonHoverMinInterval()
    {
        return overrideButtonAudio ? buttonHoverMinIntervalOverride : themeProfile.ButtonHoverMinInterval;
    }

    private bool GetButtonUseAudioManagerFallback()
    {
        return overrideButtonAudio ? buttonUseAudioManagerFallbackOverride : themeProfile.ButtonUseAudioManagerFallback;
    }

    private bool GetPanelPlayOpenOnEnable()
    {
        return overridePanelFx ? panelPlayOpenOnEnableOverride : themeProfile.PanelPlayOpenOnEnable;
    }

    private float GetPanelOpenDuration()
    {
        return overridePanelFx ? panelOpenDurationOverride : themeProfile.PanelOpenDuration;
    }

    private float GetPanelCloseDuration()
    {
        return overridePanelFx ? panelCloseDurationOverride : themeProfile.PanelCloseDuration;
    }

    private float GetPanelSlideOffset()
    {
        return overridePanelFx ? panelSlideOffsetOverride : themeProfile.PanelSlideOffset;
    }

    private float GetPanelStartScale()
    {
        return overridePanelFx ? panelStartScaleOverride : themeProfile.PanelStartScale;
    }

    private float GetPanelCloseScale()
    {
        return overridePanelFx ? panelCloseScaleOverride : themeProfile.PanelCloseScale;
    }

    private bool GetPanelAudioEnabled()
    {
        return overridePanelAudio ? panelAudioEnabledOverride : themeProfile.PanelAudioEnabled;
    }

    private float GetPanelOpenVolume()
    {
        return overridePanelAudio ? panelOpenVolumeOverride : themeProfile.PanelOpenVolume;
    }

    private float GetPanelCloseVolume()
    {
        return overridePanelAudio ? panelCloseVolumeOverride : themeProfile.PanelCloseVolume;
    }

    private bool GetPanelUseAudioManagerFallback()
    {
        return overridePanelAudio ? panelUseAudioManagerFallbackOverride : themeProfile.PanelUseAudioManagerFallback;
    }

    private string NormalizeName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        return rawName.ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty);
    }
}
