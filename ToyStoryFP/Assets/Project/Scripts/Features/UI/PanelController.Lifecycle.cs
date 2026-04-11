using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public partial class PanelController
{
    // Inicializa referencias antes de usar el componente.
    private void Awake()
    {
        ApplyCreditsProfile();
        ValidateReferences();
        ConfigureScorePanelController();
        BindListeners();
    }

    // Activa listeners y estado al habilitar el objeto.
    private void OnEnable()
    {
        ApplyCreditsProfile();
        ValidateReferences();
        ConfigureScorePanelController();
        BindListeners();
    }

    // Libera listeners y estado al deshabilitar el objeto.
    private void OnDisable()
    {
        UnbindListeners();

        if (activeSequence != null)
        {
            StopCoroutine(activeSequence);
            activeSequence = null;
        }

        isSequenceRunning = false;
    }

    private void ApplyCreditsProfile()
    {
        if (creditsProfile == null)
        {
            WarnIfMissingCreditsProfile();
            return;
        }

        hasLoggedMissingCreditsProfile = false;
        gameOverDuration = creditsProfile.GameOverDuration;
        introSkipGracePeriod = creditsProfile.IntroSkipGracePeriod;
        skipInputGracePeriod = creditsProfile.SkipInputGracePeriod;
        creditsFadeOutDuration = creditsProfile.CreditsFadeOutDuration;
        skippedFadeOutDuration = creditsProfile.SkippedFadeOutDuration;
        titleInDuration = creditsProfile.TitleInDuration;
        sectionInDuration = creditsProfile.SectionInDuration;
        sectionHoldDuration = creditsProfile.SectionHoldDuration;
        sectionOutDuration = creditsProfile.SectionOutDuration;
        finalHoldDuration = creditsProfile.FinalHoldDuration;
        globalFadeOutDuration = creditsProfile.GlobalFadeOutDuration;
        sectionGap = creditsProfile.SectionGap;
        titleStartScale = creditsProfile.TitleStartScale;
        titleStartYOffset = creditsProfile.TitleStartYOffset;
        sectionStartYOffset = creditsProfile.SectionStartYOffset;
        sequentialNameReveal = creditsProfile.SequentialNameReveal;
        nameRevealDuration = creditsProfile.NameRevealDuration;
        nameRevealGap = creditsProfile.NameRevealGap;
        nameStartYOffset = creditsProfile.NameStartYOffset;
        sectionDimAlpha = creditsProfile.SectionDimAlpha;
        sectionFocusAlpha = creditsProfile.SectionFocusAlpha;
        lineStagger = creditsProfile.LineStagger;
        easeType = creditsProfile.EaseType;
        targetCreditsDurationMin = creditsProfile.TargetCreditsDurationMin;
        targetCreditsDurationMax = creditsProfile.TargetCreditsDurationMax;
        introBeatDuration = creditsProfile.IntroBeatDuration;
        perNameRevealDuration = creditsProfile.PerNameRevealDuration;
        perNameGap = creditsProfile.PerNameGap;
        comboHoldDuration = creditsProfile.ComboHoldDuration;
        finalStingerDuration = creditsProfile.FinalStingerDuration;
        outroFadeDuration = creditsProfile.OutroFadeDuration;
        introStartScale = creditsProfile.IntroStartScale;
        namePunchScale = creditsProfile.NamePunchScale;
        panelPulseAmount = creditsProfile.PanelPulseAmount;
        previousNameAlpha = creditsProfile.PreviousNameAlpha;
        microShakeAmount = creditsProfile.MicroShakeAmount;
        microShakeDuration = creditsProfile.MicroShakeDuration;
        nameHitClip = creditsProfile.NameHitClip;
        nameTickClip = creditsProfile.NameTickClip;
        introWhooshClip = creditsProfile.IntroWhooshClip;
        finalStingClip = creditsProfile.FinalStingClip;
        outroSwishClip = creditsProfile.OutroSwishClip;
        useAudioManagerFallback = creditsProfile.UseAudioManagerFallback;
        fallbackCreditsDuration = creditsProfile.FallbackCreditsDuration;
        textStagger = creditsProfile.TextStagger;
        textFadeInDuration = creditsProfile.TextFadeInDuration;
        textMoveDuration = creditsProfile.TextMoveDuration;
        textStartYOffset = creditsProfile.TextStartYOffset;
    }

    private void WarnIfMissingCreditsProfile()
    {
        if (hasLoggedMissingCreditsProfile)
        {
            return;
        }

        hasLoggedMissingCreditsProfile = true;
        GameDebug.Advertencia("EndMenu", "PanelController no tiene CreditsPresentationProfile asignado. Se usaran los valores locales del componente.", this);
    }

    // Arranca la configuracion inicial del componente.
    private void Start()
    {
        StartSequence(PlayIntroSequence());
    }

    // Abre creditos desde el boton.
    public void OpenCreditsFromButton()
    {
        if (isSequenceRunning)
        {
            return;
        }

        StartSequence(PlayCreditsSequence(allowSkip: true));
    }

    // Arranca la secuencia solo si el menu esta listo.
    private void StartSequence(IEnumerator routine)
    {
        if (routine == null || isSequenceRunning)
        {
            return;
        }

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            GameDebug.Advertencia(
                "EndMenu",
                "No se pudo iniciar la secuencia porque el objeto Controlador esta inactivo o deshabilitado.",
                this);
            return;
        }

        activeSequence = StartCoroutine(RunManagedSequence(routine));
    }

    // Envuelve cualquier secuencia larga para marcar cuando esta ocupada.
    private IEnumerator RunManagedSequence(IEnumerator routine)
    {
        isSequenceRunning = true;
        yield return routine;
        isSequenceRunning = false;
        activeSequence = null;
    }

    // Activa o desactiva un panel solo si la referencia existe.
    private void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }

    // Resuelve el CanvasGroup requerido sin crear componentes ocultos en runtime.
    private CanvasGroup RequireCanvasGroup(GameObject target, string panelLabel)
    {
        if (target == null)
        {
            return null;
        }

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            return canvasGroup;
        }

        GameDebug.Advertencia(
            "EndMenu",
            $"El panel '{panelLabel}' necesita un CanvasGroup asignado en la escena para animarse correctamente.",
            this);
        return null;
    }

}
