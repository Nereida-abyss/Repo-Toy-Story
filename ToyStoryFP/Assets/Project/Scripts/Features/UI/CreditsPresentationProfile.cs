using UnityEngine;

public enum CreditsEaseType
{
    OutCubic,
    OutSine,
    OutBack
}

[CreateAssetMenu(fileName = "DefaultCreditsPresentationProfile", menuName = "UI/Credits Presentation Profile")]
public class CreditsPresentationProfile : ScriptableObject
{
    [Header("Sequence Timing")]
    [SerializeField] private float gameOverDuration = 3f;
    [SerializeField] private float introSkipGracePeriod = 1.5f;
    [SerializeField] private float skipInputGracePeriod = 0.15f;
    [SerializeField] private float creditsFadeOutDuration = 0.6f;
    [SerializeField] private float skippedFadeOutDuration = 0.2f;

    [Header("Credits V2 Timing")]
    [SerializeField] private float titleInDuration = 2.2f;
    [SerializeField] private float sectionInDuration = 1.2f;
    [SerializeField] private float sectionHoldDuration = 5.5f;
    [SerializeField] private float sectionOutDuration = 0.9f;
    [SerializeField] private float finalHoldDuration = 6f;
    [SerializeField] private float globalFadeOutDuration = 1.2f;
    [SerializeField] private float sectionGap = 0.6f;

    [Header("Credits V2 Visual")]
    [SerializeField] private float titleStartScale = 0.88f;
    [SerializeField] private float titleStartYOffset = 45f;
    [SerializeField] private float sectionStartYOffset = 95f;
    [SerializeField] private bool sequentialNameReveal = true;
    [SerializeField] private float nameRevealDuration = 0.26f;
    [SerializeField] private float nameRevealGap = 0.08f;
    [SerializeField] private float nameStartYOffset = 28f;
    [SerializeField] private float sectionDimAlpha = 0.3f;
    [SerializeField] private float sectionFocusAlpha = 1f;
    [SerializeField] private float lineStagger = 0.2f;
    [SerializeField] private CreditsEaseType easeType = CreditsEaseType.OutCubic;

    [Header("Credits Hype Reel")]
    [SerializeField] private float targetCreditsDurationMin = 7f;
    [SerializeField] private float targetCreditsDurationMax = 9f;
    [SerializeField] private float introBeatDuration = 0.75f;
    [SerializeField] private float perNameRevealDuration = 0.55f;
    [SerializeField] private float perNameGap = 0.14f;
    [SerializeField] private float comboHoldDuration = 0.65f;
    [SerializeField] private float finalStingerDuration = 1.45f;
    [SerializeField] private float outroFadeDuration = 0.65f;
    [SerializeField] private float introStartScale = 0.92f;
    [SerializeField] private float namePunchScale = 0.10f;
    [SerializeField] private float panelPulseAmount = 0.03f;
    [SerializeField] private float previousNameAlpha = 0.78f;
    [SerializeField] private float microShakeAmount = 4f;
    [SerializeField] private float microShakeDuration = 0.08f;

    [Header("Credits Audio")]
    [SerializeField] private AudioClip nameHitClip;
    [SerializeField] private AudioClip nameTickClip;
    [SerializeField] private AudioClip introWhooshClip;
    [SerializeField] private AudioClip finalStingClip;
    [SerializeField] private AudioClip outroSwishClip;
    [SerializeField] private bool useAudioManagerFallback = true;

    [Header("Fallback")]
    [SerializeField] private float fallbackCreditsDuration = 3f;
    [SerializeField] private float textStagger = 0.18f;
    [SerializeField] private float textFadeInDuration = 0.45f;
    [SerializeField] private float textMoveDuration = 0.7f;
    [SerializeField] private float textStartYOffset = 70f;

    public float GameOverDuration => gameOverDuration;
    public float IntroSkipGracePeriod => introSkipGracePeriod;
    public float SkipInputGracePeriod => skipInputGracePeriod;
    public float CreditsFadeOutDuration => creditsFadeOutDuration;
    public float SkippedFadeOutDuration => skippedFadeOutDuration;
    public float TitleInDuration => titleInDuration;
    public float SectionInDuration => sectionInDuration;
    public float SectionHoldDuration => sectionHoldDuration;
    public float SectionOutDuration => sectionOutDuration;
    public float FinalHoldDuration => finalHoldDuration;
    public float GlobalFadeOutDuration => globalFadeOutDuration;
    public float SectionGap => sectionGap;
    public float TitleStartScale => titleStartScale;
    public float TitleStartYOffset => titleStartYOffset;
    public float SectionStartYOffset => sectionStartYOffset;
    public bool SequentialNameReveal => sequentialNameReveal;
    public float NameRevealDuration => nameRevealDuration;
    public float NameRevealGap => nameRevealGap;
    public float NameStartYOffset => nameStartYOffset;
    public float SectionDimAlpha => sectionDimAlpha;
    public float SectionFocusAlpha => sectionFocusAlpha;
    public float LineStagger => lineStagger;
    public CreditsEaseType EaseType => easeType;
    public float TargetCreditsDurationMin => targetCreditsDurationMin;
    public float TargetCreditsDurationMax => targetCreditsDurationMax;
    public float IntroBeatDuration => introBeatDuration;
    public float PerNameRevealDuration => perNameRevealDuration;
    public float PerNameGap => perNameGap;
    public float ComboHoldDuration => comboHoldDuration;
    public float FinalStingerDuration => finalStingerDuration;
    public float OutroFadeDuration => outroFadeDuration;
    public float IntroStartScale => introStartScale;
    public float NamePunchScale => namePunchScale;
    public float PanelPulseAmount => panelPulseAmount;
    public float PreviousNameAlpha => previousNameAlpha;
    public float MicroShakeAmount => microShakeAmount;
    public float MicroShakeDuration => microShakeDuration;
    public AudioClip NameHitClip => nameHitClip;
    public AudioClip NameTickClip => nameTickClip;
    public AudioClip IntroWhooshClip => introWhooshClip;
    public AudioClip FinalStingClip => finalStingClip;
    public AudioClip OutroSwishClip => outroSwishClip;
    public bool UseAudioManagerFallback => useAudioManagerFallback;
    public float FallbackCreditsDuration => fallbackCreditsDuration;
    public float TextStagger => textStagger;
    public float TextFadeInDuration => textFadeInDuration;
    public float TextMoveDuration => textMoveDuration;
    public float TextStartYOffset => textStartYOffset;
}
