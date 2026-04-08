using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelController : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject panelGameOver;
    [SerializeField] private GameObject panelCredits;
    [SerializeField] private GameObject panelButtons;
    [SerializeField] private GameObject panelSetting;
    [SerializeField] private GameObject panelScore;

    [Header("Créditos (Opcional)")]
    [SerializeField] private Button creditsButton;
    [SerializeField] private Transform creditsTextRoot;

    [Header("Score (Opcional)")]
    [SerializeField] private ScorePanelController scorePanelController;

    [Header("Tiempos")]
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

    [Header("Credits Hype Reel (7-9s)")]
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

    [Header("Credits Audio (Optional)")]
    [SerializeField] private AudioSource creditsAudioSource;
    [SerializeField] private AudioClip nameHitClip;
    [SerializeField] private AudioClip nameTickClip;
    [SerializeField] private AudioClip introWhooshClip;
    [SerializeField] private AudioClip finalStingClip;
    [SerializeField] private AudioClip outroSwishClip;
    [SerializeField] private bool useAudioManagerFallback = true;

    [Header("Fallback (Lineal)")]
    [SerializeField] private float fallbackCreditsDuration = 3f;
    [SerializeField] private float textStagger = 0.18f;
    [SerializeField] private float textFadeInDuration = 0.45f;
    [SerializeField] private float textMoveDuration = 0.7f;
    [SerializeField] private float textStartYOffset = 70f;

    private Coroutine activeSequence;
    private bool isSequenceRunning;
    private bool listenersBound;

    private sealed class CreditTextEntry
    {
        public TMP_Text Text;
        public RectTransform RectTransform;
        public Vector2 OriginalAnchoredPosition;
        public Color OriginalColor;
    }

    private sealed class CreditSection
    {
        public Transform Root;
        public Vector3 OriginalLocalScale;
        public List<CreditTextEntry> Entries;
    }

    private enum CreditsEaseType
    {
        OutCubic,
        OutSine,
        OutBack
    }

    // Inicializa referencias antes de usar el componente.
    private void Awake()
    {
        AutoDiscoverReferences();
        EnsureScorePanelController();
        BindListeners();
    }

    // Activa listeners y estado al habilitar el objeto.
    private void OnEnable()
    {
        AutoDiscoverReferences();
        EnsureScorePanelController();
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

    // Arranca la configuración inicial del componente.
    private void Start()
    {
        StartSequence(PlayIntroSequence());
    }

    // Abre créditos desde el botón.
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

    // Marca la secuencia en curso y limpia el estado al terminar.
    private IEnumerator RunManagedSequence(IEnumerator routine)
    {
        isSequenceRunning = true;
        yield return routine;
        isSequenceRunning = false;
        activeSequence = null;
    }

    // Secuencia inicial del EndMenu: GameOver -> créditos.
    // Al cerrar créditos, devuelve el control al panel de botones.
    private IEnumerator PlayIntroSequence()
    {
        SetPanelActive(panelButtons, false);
        SetPanelActive(panelCredits, false);
        SetPanelActive(panelSetting, false);
        SetPanelActive(panelScore, false);

        if (panelGameOver != null)
        {
            SetPanelActive(panelGameOver, true);
            yield return WaitForSecondsRealtime(gameOverDuration);
            SetPanelActive(panelGameOver, false);
        }
        else
        {
            GameDebug.Advertencia("EndMenu", "PanelController no tiene referencia a panelGameOver.", this);
        }

        yield return PlayCreditsSequence(allowSkip: true, introMode: true);
    }

    // Reproduce la secuencia completa de créditos (entrada, animación, skip y salida).
    // Al final restaura el panel de botones.
    private IEnumerator PlayCreditsSequence(bool allowSkip, bool introMode = false)
    {
        if (panelCredits == null)
        {
            GameDebug.Advertencia("EndMenu", "PanelController no tiene referencia a panelCredits. Se omite la secuencia.", this);
            SetPanelActive(panelButtons, true);
            yield break;
        }

        SetPanelActive(panelButtons, false);
        SetPanelActive(panelSetting, false);
        SetPanelActive(panelScore, false);
        UIPanelFx creditsPanelFx = panelCredits.GetComponent<UIPanelFx>();
        bool creditsPanelFxWasEnabled = creditsPanelFx != null && creditsPanelFx.enabled;

        if (creditsPanelFxWasEnabled)
        {
            creditsPanelFx.enabled = false;
        }

        SetPanelActive(panelCredits, true);

        CanvasGroup creditsCanvasGroup = EnsureCanvasGroup(panelCredits);
        creditsCanvasGroup.alpha = 1f;
        creditsCanvasGroup.interactable = false;
        creditsCanvasGroup.blocksRaycasts = false;

        float skipGracePeriod = introMode ? introSkipGracePeriod : skipInputGracePeriod;
        float skipAllowedAtTime = Time.unscaledTime + Mathf.Max(0f, skipGracePeriod);
        bool skipRequested = false;
        List<CreditTextEntry> textEntries = CollectCreditTextEntries();

        if (textEntries.Count > 0)
        {
            yield return AnimateCreditsHypeReel(
                textEntries,
                allowSkip,
                skipAllowedAtTime,
                () => skipRequested = true);
            RestoreCreditTextEntries(textEntries);
        }
        else
        {
            GameDebug.Advertencia("EndMenu", "No se encontraron textos TMP en créditos. Se usara timing de fallback.", this);
            yield return HoldFallbackCredits(allowSkip, skipAllowedAtTime, () => skipRequested = true);
        }

        float defaultOutro = Mathf.Max(0f, outroFadeDuration > 0f ? outroFadeDuration : globalFadeOutDuration);
        float regularFadeDuration = Mathf.Max(0f, defaultOutro > 0f ? defaultOutro : creditsFadeOutDuration);
        float fadeDuration = skipRequested ? skippedFadeOutDuration : regularFadeDuration;

        if (!skipRequested)
        {
            PlayCreditsAudio(outroSwishClip, "click");
        }

        yield return FadeCanvasGroupAlpha(creditsCanvasGroup, fadeDuration);

        creditsCanvasGroup.alpha = 1f;
        SetPanelActive(panelCredits, false);

        if (creditsPanelFx != null)
        {
            creditsPanelFx.enabled = creditsPanelFxWasEnabled;
        }

        SetPanelActive(panelButtons, true);
    }

    // Animación principal de créditos: intro, nombres y cierre final.
    // Si hay skip, corta la animación y deja el texto en estado consistente.
    private IEnumerator AnimateCreditsHypeReel(
        List<CreditTextEntry> entries,
        bool allowSkip,
        float skipAllowedAtTime,
        System.Action onSkip)
    {
        if (entries == null || entries.Count == 0)
        {
            yield break;
        }

        List<CreditTextEntry> names = new List<CreditTextEntry>(entries);
        CreditTextEntry finalEntry = FindFinalStingerEntry(names);

        if (finalEntry != null)
        {
            names.Remove(finalEntry);
        }

        if (finalEntry == null && names.Count > 0)
        {
            finalEntry = names[0];
            names.RemoveAt(0);
        }

        Transform animatedRoot = creditsTextRoot != null ? creditsTextRoot : panelCredits.transform;
        Vector3 baseRootScale = animatedRoot.localScale;
        float clampedPreviousAlpha = Mathf.Clamp01(previousNameAlpha);
        float clampedPulseAmount = Mathf.Max(0f, panelPulseAmount);
        float clampedPunchScale = Mathf.Max(0f, namePunchScale);
        bool localSkipRequested = false;
        System.Action skipAction = () =>
        {
            localSkipRequested = true;
            onSkip?.Invoke();
        };

        for (int i = 0; i < entries.Count; i++)
        {
            CreditTextEntry entry = entries[i];
            entry.Text.enabled = true;
            entry.RectTransform.localScale = Vector3.one;
            SetTextAlpha(entry, 0f);
            entry.RectTransform.anchoredPosition = entry.OriginalAnchoredPosition;
        }

        PlayCreditsAudio(introWhooshClip, "whoosh");
        yield return AnimateIntroBeat(
            animatedRoot,
            baseRootScale,
            Mathf.Max(0.01f, introBeatDuration),
            Mathf.Max(0.01f, introStartScale),
            clampedPulseAmount,
            allowSkip,
            skipAllowedAtTime,
            skipAction);

        if (localSkipRequested)
        {
            RestoreCreditTextEntries(entries);
            if (animatedRoot != null)
            {
                animatedRoot.localScale = baseRootScale;
            }

            yield break;
        }

        float scaledNameRevealDuration = Mathf.Max(0.01f, perNameRevealDuration);
        float scaledNameGap = Mathf.Max(0f, perNameGap);
        float scaledComboHold = Mathf.Max(0f, comboHoldDuration);
        float scaledFinalDuration = Mathf.Max(0.01f, finalStingerDuration);
        ApplyHypeDurationScaling(
            names.Count,
            ref scaledNameRevealDuration,
            ref scaledNameGap,
            ref scaledComboHold,
            ref scaledFinalDuration);

        for (int i = 0; i < names.Count; i++)
        {
            CreditTextEntry currentName = names[i];
            SetTextAlpha(currentName, 0f);
            currentName.RectTransform.localScale = Vector3.one;
            currentName.RectTransform.anchoredPosition =
                currentName.OriginalAnchoredPosition + Vector2.up * Mathf.Max(0f, nameStartYOffset);

            PlayCreditsAudio(nameHitClip, "hit");
            float elapsed = 0f;

            while (elapsed < scaledNameRevealDuration)
            {
                if (ShouldSkipCredits(allowSkip, skipAllowedAtTime))
                {
                    skipAction?.Invoke();
                    break;
                }

                float t = Mathf.Clamp01(elapsed / scaledNameRevealDuration);
                float eased = EvaluateCreditsEase(t);
                float pulse = Mathf.Sin(t * Mathf.PI) * clampedPulseAmount;
                float punch = Mathf.Sin(t * Mathf.PI) * clampedPunchScale;
                float shakeFade = Mathf.Clamp01(1f - (elapsed / Mathf.Max(0.001f, microShakeDuration)));
                Vector2 shakeOffset = EvaluateMicroShakeOffset(elapsed, Mathf.Max(0f, microShakeAmount) * shakeFade);
                Vector2 baseAnchoredPosition =
                    currentName.OriginalAnchoredPosition + Vector2.up * Mathf.Lerp(nameStartYOffset, 0f, eased);

                SetTextAlpha(currentName, Mathf.Lerp(0f, 1f, eased));
                currentName.RectTransform.anchoredPosition = baseAnchoredPosition + shakeOffset;
                currentName.RectTransform.localScale = Vector3.one * (1f + punch);

                for (int j = 0; j < i; j++)
                {
                    SetTextAlpha(names[j], clampedPreviousAlpha);
                }
                animatedRoot.localScale = baseRootScale * (1f + (pulse * 0.3f));

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (localSkipRequested)
            {
                break;
            }

            SetTextAlpha(currentName, 1f);
            currentName.RectTransform.anchoredPosition = currentName.OriginalAnchoredPosition;
            currentName.RectTransform.localScale = Vector3.one;

            for (int j = 0; j < i; j++)
            {
                SetTextAlpha(names[j], clampedPreviousAlpha);
            }

            animatedRoot.localScale = baseRootScale;

            if (scaledNameGap > 0f && i < names.Count - 1)
            {
                PlayCreditsAudio(nameTickClip, "click");
                yield return HoldDuration(scaledNameGap, allowSkip, skipAllowedAtTime, skipAction);
            }

            if (localSkipRequested)
            {
                break;
            }
        }

        if (localSkipRequested)
        {
            RestoreCreditTextEntries(entries);
            if (animatedRoot != null)
            {
                animatedRoot.localScale = baseRootScale;
            }

            yield break;
        }

        for (int i = 0; i < names.Count; i++)
        {
            SetTextAlpha(names[i], 1f);
        }

        if (scaledComboHold > 0f)
        {
            yield return HoldDuration(scaledComboHold, allowSkip, skipAllowedAtTime, skipAction);
        }

        if (localSkipRequested)
        {
            RestoreCreditTextEntries(entries);
            if (animatedRoot != null)
            {
                animatedRoot.localScale = baseRootScale;
            }

            yield break;
        }

        if (finalEntry != null)
        {
            for (int i = 0; i < names.Count; i++)
            {
                SetTextAlpha(names[i], clampedPreviousAlpha);
            }

            finalEntry.Text.enabled = true;
            finalEntry.RectTransform.localScale = Vector3.one;
            finalEntry.RectTransform.anchoredPosition =
                finalEntry.OriginalAnchoredPosition + Vector2.up * Mathf.Max(nameStartYOffset, sectionStartYOffset * 0.45f);
            SetTextAlpha(finalEntry, 0f);
            PlayCreditsAudio(finalStingClip, "sting");
            float elapsed = 0f;

            while (elapsed < scaledFinalDuration)
            {
                if (ShouldSkipCredits(allowSkip, skipAllowedAtTime))
                {
                    skipAction?.Invoke();
                    break;
                }

                float t = Mathf.Clamp01(elapsed / scaledFinalDuration);
                float eased = EvaluateCreditsEase(t);
                float punch = Mathf.Sin(t * Mathf.PI) * clampedPunchScale * 1.35f;
                float pulse = Mathf.Sin(t * Mathf.PI) * clampedPulseAmount * 1.2f;
                float shakeFade = Mathf.Clamp01(1f - (elapsed / Mathf.Max(0.001f, microShakeDuration)));
                Vector2 shakeOffset = EvaluateMicroShakeOffset(elapsed, Mathf.Max(0f, microShakeAmount * 1.1f) * shakeFade);
                Vector2 baseAnchoredPosition =
                    finalEntry.OriginalAnchoredPosition + Vector2.up * Mathf.Lerp(Mathf.Max(nameStartYOffset, sectionStartYOffset * 0.45f), 0f, eased);
                SetTextAlpha(finalEntry, Mathf.Lerp(0f, 1f, eased));
                finalEntry.RectTransform.anchoredPosition = baseAnchoredPosition + shakeOffset;
                finalEntry.RectTransform.localScale = Vector3.one * (1f + punch);
                animatedRoot.localScale = baseRootScale * (1f + (pulse * 0.25f));

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            SetTextAlpha(finalEntry, 1f);
            finalEntry.RectTransform.anchoredPosition = finalEntry.OriginalAnchoredPosition;
            finalEntry.RectTransform.localScale = Vector3.one;
        }

        if (animatedRoot != null)
        {
            animatedRoot.localScale = baseRootScale;
        }
    }

    // Pulso visual de apertura antes de mostrar los nombres.
    // Respeta el skip para no bloquear la transición.
    private IEnumerator AnimateIntroBeat(
        Transform targetRoot,
        Vector3 baseRootScale,
        float duration,
        float startScaleMultiplier,
        float pulseAmount,
        bool allowSkip,
        float skipAllowedAtTime,
        System.Action onSkip)
    {
        if (targetRoot == null)
        {
            yield break;
        }

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            if (ShouldSkipCredits(allowSkip, skipAllowedAtTime))
            {
                onSkip?.Invoke();
                yield break;
            }

            float t = Mathf.Clamp01(elapsed / safeDuration);
            float eased = EvaluateCreditsEase(t);
            float scaleMultiplier = Mathf.Lerp(startScaleMultiplier, 1f, eased);
            float pulse = Mathf.Sin(t * Mathf.PI) * pulseAmount;
            targetRoot.localScale = baseRootScale * (scaleMultiplier + (pulse * 0.2f));

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        targetRoot.localScale = baseRootScale;
    }

    // Anima los textos de créditos por grupos para destacar cada bloque.
    private IEnumerator AnimateCreditsBySections(
        List<CreditSection> sections,
        bool allowSkip,
        float skipAllowedAtTime,
        System.Action onSkip)
    {
        if (sections == null || sections.Count < 2)
        {
            yield break;
        }

        InitializeSectionsHidden(sections);

        CreditSection titleSection = sections[0];
        CreditSection finalSection = sections[sections.Count - 1];
        List<CreditSection> middleSections = new List<CreditSection>();
        bool localSkipRequested = false;
        System.Action skipAction = () =>
        {
            localSkipRequested = true;
            onSkip?.Invoke();
        };

        for (int i = 1; i < sections.Count - 1; i++)
        {
            middleSections.Add(sections[i]);
        }

        yield return AnimateSectionIntro(
            titleSection,
            startYOffset: titleStartYOffset,
            baseDuration: titleInDuration,
            startScale: titleStartScale,
            targetAlpha: sectionFocusAlpha,
            useSequentialNames: false,
            allowSkip: allowSkip,
            skipAllowedAtTime: skipAllowedAtTime,
            onSkip: skipAction);

        if (localSkipRequested)
        {
            yield break;
        }

        if (middleSections.Count > 0)
        {
            yield return AnimateSectionAlpha(
                titleSection,
                fromAlpha: sectionFocusAlpha,
                toAlpha: sectionDimAlpha,
                duration: sectionOutDuration * 0.6f,
                allowSkip: allowSkip,
                skipAllowedAtTime: skipAllowedAtTime,
                onSkip: skipAction);

            if (localSkipRequested)
            {
                yield break;
            }
        }

        for (int i = 0; i < middleSections.Count; i++)
        {
            CreditSection currentSection = middleSections[i];
            ApplyDimAlphaToAll(sections, sectionDimAlpha);

            yield return AnimateSectionIntro(
                currentSection,
                startYOffset: sectionStartYOffset,
                baseDuration: sectionInDuration,
                startScale: 1f,
                targetAlpha: sectionFocusAlpha,
                useSequentialNames: sequentialNameReveal,
                allowSkip: allowSkip,
                skipAllowedAtTime: skipAllowedAtTime,
                onSkip: skipAction);

            if (localSkipRequested)
            {
                yield break;
            }

            yield return HoldDuration(
                sectionHoldDuration,
                allowSkip,
                skipAllowedAtTime,
                skipAction);

            if (localSkipRequested)
            {
                yield break;
            }

            yield return AnimateSectionAlpha(
                currentSection,
                fromAlpha: sectionFocusAlpha,
                toAlpha: sectionDimAlpha,
                duration: sectionOutDuration,
                allowSkip: allowSkip,
                skipAllowedAtTime: skipAllowedAtTime,
                onSkip: skipAction);

            if (localSkipRequested)
            {
                yield break;
            }

            if (sectionGap > 0f)
            {
                yield return HoldDuration(
                    sectionGap,
                    allowSkip,
                    skipAllowedAtTime,
                    skipAction);

                if (localSkipRequested)
                {
                    yield break;
                }
            }
        }

        ApplyDimAlphaToAll(sections, sectionDimAlpha);

        yield return AnimateSectionIntro(
            finalSection,
            startYOffset: sectionStartYOffset * 0.55f,
            baseDuration: sectionInDuration,
            startScale: 1f,
            targetAlpha: sectionFocusAlpha,
            useSequentialNames: false,
            allowSkip: allowSkip,
            skipAllowedAtTime: skipAllowedAtTime,
            onSkip: skipAction);

        if (localSkipRequested)
        {
            yield break;
        }

        yield return HoldDuration(
            finalHoldDuration,
            allowSkip,
            skipAllowedAtTime,
            skipAction);
    }

    // Entrada de un grupo de textos: aparece con movimiento y mas visibilidad.
    private IEnumerator AnimateSectionIntro(
        CreditSection section,
        float startYOffset,
        float baseDuration,
        float startScale,
        float targetAlpha,
        bool useSequentialNames,
        bool allowSkip,
        float skipAllowedAtTime,
        System.Action onSkip)
    {
        if (section == null || section.Entries == null || section.Entries.Count == 0)
        {
            yield break;
        }

        float clampedTargetAlpha = Mathf.Clamp01(targetAlpha);
        float stagger = Mathf.Max(0f, lineStagger);
        float entryDuration = Mathf.Max(0.01f, baseDuration);
        float totalDuration = (section.Entries.Count - 1) * stagger + entryDuration;
        float elapsed = 0f;

        SetSectionScale(section, startScale);

        for (int i = 0; i < section.Entries.Count; i++)
        {
            CreditTextEntry entry = section.Entries[i];
            entry.Text.enabled = true;
            SetTextAlpha(entry, 0f);
            entry.RectTransform.anchoredPosition = entry.OriginalAnchoredPosition + Vector2.up * startYOffset;
        }

        if (useSequentialNames)
        {
            yield return AnimateSectionIntroSequential(
                section,
                clampedTargetAlpha,
                allowSkip,
                skipAllowedAtTime,
                onSkip);
            SetSectionScale(section, 1f);
            yield break;
        }

        while (elapsed < totalDuration)
        {
            if (ShouldSkipCredits(allowSkip, skipAllowedAtTime))
            {
                onSkip?.Invoke();
                yield break;
            }

            for (int i = 0; i < section.Entries.Count; i++)
            {
                CreditTextEntry entry = section.Entries[i];
                float localElapsed = elapsed - i * stagger;

                if (localElapsed <= 0f)
                {
                    continue;
                }

                float t = Mathf.Clamp01(localElapsed / entryDuration);
                float eased = EvaluateCreditsEase(t);
                SetTextAlpha(entry, Mathf.Lerp(0f, clampedTargetAlpha, eased));
                entry.RectTransform.anchoredPosition =
                    entry.OriginalAnchoredPosition + Vector2.up * Mathf.Lerp(startYOffset, 0f, eased);
            }

            float scaleT = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, totalDuration));
            float scaleMultiplier = Mathf.Lerp(startScale, 1f, EvaluateCreditsEase(scaleT));
            SetSectionScale(section, scaleMultiplier);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        SetSectionAlpha(section, clampedTargetAlpha);
        SetSectionVerticalOffset(section, 0f);
        SetSectionScale(section, 1f);
    }

    // Entrada secuencial: muestra los textos del grupo uno por uno.
    private IEnumerator AnimateSectionIntroSequential(
        CreditSection section,
        float targetAlpha,
        bool allowSkip,
        float skipAllowedAtTime,
        System.Action onSkip)
    {
        if (section == null || section.Entries == null || section.Entries.Count == 0)
        {
            yield break;
        }

        float revealDuration = Mathf.Max(0.01f, nameRevealDuration);
        float revealGap = Mathf.Max(0f, nameRevealGap);
        float revealStartYOffset = Mathf.Max(0f, nameStartYOffset);

        for (int i = 0; i < section.Entries.Count; i++)
        {
            CreditTextEntry entry = section.Entries[i];

            if (entry.Text != null)
            {
                entry.Text.enabled = true;
            }

            SetTextAlpha(entry, 0f);
            entry.RectTransform.anchoredPosition = entry.OriginalAnchoredPosition + Vector2.up * revealStartYOffset;
            float elapsed = 0f;

            while (elapsed < revealDuration)
            {
                if (ShouldSkipCredits(allowSkip, skipAllowedAtTime))
                {
                    onSkip?.Invoke();
                    yield break;
                }

                float t = Mathf.Clamp01(elapsed / revealDuration);
                float eased = EvaluateCreditsEase(t);
                SetTextAlpha(entry, Mathf.Lerp(0f, targetAlpha, eased));
                entry.RectTransform.anchoredPosition =
                    entry.OriginalAnchoredPosition + Vector2.up * Mathf.Lerp(revealStartYOffset, 0f, eased);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            SetTextAlpha(entry, targetAlpha);
            entry.RectTransform.anchoredPosition = entry.OriginalAnchoredPosition;

            if (i >= section.Entries.Count - 1 || revealGap <= 0f)
            {
                continue;
            }

            float gapElapsed = 0f;

            while (gapElapsed < revealGap)
            {
                if (ShouldSkipCredits(allowSkip, skipAllowedAtTime))
                {
                    onSkip?.Invoke();
                    yield break;
                }

                gapElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }

    // Ajusta la transparencia de un grupo de textos para dar o quitar foco.
    private IEnumerator AnimateSectionAlpha(
        CreditSection section,
        float fromAlpha,
        float toAlpha,
        float duration,
        bool allowSkip,
        float skipAllowedAtTime,
        System.Action onSkip)
    {
        if (section == null || section.Entries == null || section.Entries.Count == 0)
        {
            yield break;
        }

        float safeDuration = Mathf.Max(0f, duration);

        if (safeDuration <= 0f)
        {
            SetSectionAlpha(section, toAlpha);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            if (ShouldSkipCredits(allowSkip, skipAllowedAtTime))
            {
                onSkip?.Invoke();
                yield break;
            }

            float t = Mathf.Clamp01(elapsed / safeDuration);
            float eased = EvaluateCreditsEase(t);
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
            SetSectionAlpha(section, alpha);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        SetSectionAlpha(section, toAlpha);
    }

    // Pausa controlada: espera el tiempo indicado o sale antes si hay skip.
    private IEnumerator HoldDuration(
        float duration,
        bool allowSkip,
        float skipAllowedAtTime,
        System.Action onSkip)
    {
        float holdElapsed = 0f;
        float holdDuration = Mathf.Max(0f, duration);

        while (holdElapsed < holdDuration)
        {
            if (ShouldSkipCredits(allowSkip, skipAllowedAtTime))
            {
                onSkip?.Invoke();
                yield break;
            }

            holdElapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // Hace aparecer los textos de créditos de forma progresiva.
    private IEnumerator AnimateCreditTextsIn(
        List<CreditTextEntry> entries,
        bool allowSkip,
        float skipAllowedAtTime,
        System.Action onSkip)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            CreditTextEntry entry = entries[i];
            SetTextAlpha(entry, 0f);
            entry.RectTransform.anchoredPosition = entry.OriginalAnchoredPosition + Vector2.up * textStartYOffset;
        }

        float introDuration = (entries.Count - 1) * textStagger + Mathf.Max(textFadeInDuration, textMoveDuration);
        float elapsed = 0f;

        while (elapsed < introDuration)
        {
            if (ShouldSkipCredits(allowSkip, skipAllowedAtTime))
            {
                onSkip?.Invoke();
                yield break;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                CreditTextEntry entry = entries[i];
                float localElapsed = elapsed - i * textStagger;

                if (localElapsed <= 0f)
                {
                    continue;
                }

                float fadeT = Mathf.Clamp01(localElapsed / Mathf.Max(0.0001f, textFadeInDuration));
                float moveT = Mathf.Clamp01(localElapsed / Mathf.Max(0.0001f, textMoveDuration));
                float easedMoveT = EaseOutCubic(moveT);

                SetTextAlpha(entry, fadeT);
                entry.RectTransform.anchoredPosition =
                    entry.OriginalAnchoredPosition + Vector2.up * Mathf.Lerp(textStartYOffset, 0f, easedMoveT);
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            CreditTextEntry entry = entries[i];
            SetTextAlpha(entry, 1f);
            entry.RectTransform.anchoredPosition = entry.OriginalAnchoredPosition;
        }
    }

    // Mantiene los créditos en modo simple cuando no hay textos para animar.
    private IEnumerator HoldFallbackCredits(bool allowSkip, float skipAllowedAtTime, System.Action onSkip)
    {
        float holdElapsed = 0f;

        while (holdElapsed < fallbackCreditsDuration)
        {
            if (ShouldSkipCredits(allowSkip, skipAllowedAtTime))
            {
                onSkip?.Invoke();
                yield break;
            }

            holdElapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // Desvanece el panel hasta ocultarlo (alpha 0) en tiempo real.
    private IEnumerator FadeCanvasGroupAlpha(CanvasGroup canvasGroup, float duration)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            canvasGroup.alpha = 0f;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = 1f - t;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    // Recopila crédito texto entradas.
    private List<CreditTextEntry> CollectCreditTextEntries()
    {
        List<CreditTextEntry> entries = new List<CreditTextEntry>();

        if (creditsTextRoot == null)
        {
            AutoDiscoverCreditsTextRoot();
        }

        Transform root = creditsTextRoot != null ? creditsTextRoot : panelCredits.transform;
        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text tmpText = texts[i];
            RectTransform rectTransform = tmpText.rectTransform;

            entries.Add(new CreditTextEntry
            {
                Text = tmpText,
                RectTransform = rectTransform,
                OriginalAnchoredPosition = rectTransform.anchoredPosition,
                OriginalColor = tmpText.color
            });
        }

        entries.Sort((left, right) =>
            right.OriginalAnchoredPosition.y.CompareTo(left.OriginalAnchoredPosition.y));

        return entries;
    }

    // Recopila crédito secciones.
    private List<CreditSection> CollectCreditSections()
    {
        List<CreditSection> sections = new List<CreditSection>();

        if (creditsTextRoot == null)
        {
            AutoDiscoverCreditsTextRoot();
        }

        Transform root = creditsTextRoot != null ? creditsTextRoot : panelCredits.transform;

        if (root == null || root.childCount <= 1)
        {
            return sections;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform sectionRoot = root.GetChild(i);
            TMP_Text[] texts = sectionRoot.GetComponentsInChildren<TMP_Text>(true);

            if (texts == null || texts.Length == 0)
            {
                continue;
            }

            List<CreditTextEntry> sectionEntries = new List<CreditTextEntry>(texts.Length);

            for (int j = 0; j < texts.Length; j++)
            {
                TMP_Text tmpText = texts[j];
                RectTransform rectTransform = tmpText.rectTransform;
                sectionEntries.Add(new CreditTextEntry
                {
                    Text = tmpText,
                    RectTransform = rectTransform,
                    OriginalAnchoredPosition = rectTransform.anchoredPosition,
                    OriginalColor = tmpText.color
                });
            }

            sectionEntries.Sort((left, right) =>
                right.OriginalAnchoredPosition.y.CompareTo(left.OriginalAnchoredPosition.y));

            sections.Add(new CreditSection
            {
                Root = sectionRoot,
                OriginalLocalScale = sectionRoot.localScale,
                Entries = sectionEntries
            });
        }

        return sections.Count >= 2 ? sections : new List<CreditSection>();
    }

    // Devuelve los textos a su posición y color originales.
    private void RestoreCreditTextEntries(List<CreditTextEntry> entries)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            CreditTextEntry entry = entries[i];
            entry.RectTransform.anchoredPosition = entry.OriginalAnchoredPosition;
            entry.Text.color = entry.OriginalColor;
            entry.Text.enabled = true;
        }
    }

    // Restaura cada grupo de textos al estado inicial.
    private void RestoreCreditSections(List<CreditSection> sections)
    {
        if (sections == null)
        {
            return;
        }

        for (int i = 0; i < sections.Count; i++)
        {
            CreditSection section = sections[i];

            if (section == null)
            {
                continue;
            }

            SetSectionScale(section, 1f);
            SetSectionVerticalOffset(section, 0f);

            if (section.Entries == null)
            {
                continue;
            }

            for (int j = 0; j < section.Entries.Count; j++)
            {
                CreditTextEntry entry = section.Entries[j];
                entry.RectTransform.anchoredPosition = entry.OriginalAnchoredPosition;
                entry.Text.color = entry.OriginalColor;
                entry.Text.enabled = true;
            }
        }
    }

    // Deja todos los grupos de textos ocultos antes de empezar la animación.
    private void InitializeSectionsHidden(List<CreditSection> sections)
    {
        if (sections == null)
        {
            return;
        }

        for (int i = 0; i < sections.Count; i++)
        {
            CreditSection section = sections[i];
            SetSectionScale(section, 1f);
            SetSectionVerticalOffset(section, 0f);
            SetSectionAlpha(section, 0f);
        }
    }

    // Atenúa todos los grupos de textos para mantener foco visual.
    private void ApplyDimAlphaToAll(List<CreditSection> sections, float dimAlpha)
    {
        if (sections == null)
        {
            return;
        }

        float clamped = Mathf.Clamp01(dimAlpha);

        for (int i = 0; i < sections.Count; i++)
        {
            CreditSection section = sections[i];
            SetSectionVerticalOffset(section, 0f);
            SetSectionScale(section, 1f);
            SetSectionAlpha(section, clamped);
        }
    }

    // Aplica una misma transparencia a todos los textos del grupo.
    private void SetSectionAlpha(CreditSection section, float normalizedAlpha)
    {
        if (section == null || section.Entries == null)
        {
            return;
        }

        float clamped = Mathf.Clamp01(normalizedAlpha);

        for (int i = 0; i < section.Entries.Count; i++)
        {
            SetTextAlpha(section.Entries[i], clamped);
        }
    }

    // Desplaza en vertical todos los textos del grupo.
    private void SetSectionVerticalOffset(CreditSection section, float yOffset)
    {
        if (section == null || section.Entries == null)
        {
            return;
        }

        for (int i = 0; i < section.Entries.Count; i++)
        {
            CreditTextEntry entry = section.Entries[i];
            entry.RectTransform.anchoredPosition = entry.OriginalAnchoredPosition + Vector2.up * yOffset;
        }
    }

    // Escala el grupo de textos completo.
    private void SetSectionScale(CreditSection section, float multiplier)
    {
        if (section == null || section.Root == null)
        {
            return;
        }

        float clampedMultiplier = Mathf.Max(0.01f, multiplier);
        section.Root.localScale = section.OriginalLocalScale * clampedMultiplier;
    }

    // Cambia la transparencia de un texto respetando su color base.
    private void SetTextAlpha(CreditTextEntry entry, float normalizedAlpha)
    {
        Color color = entry.OriginalColor;
        color.a = Mathf.Clamp01(normalizedAlpha) * entry.OriginalColor.a;
        entry.Text.color = color;
    }

    // Calcula un micro-temblor para dar impacto visual al texto.
    private Vector2 EvaluateMicroShakeOffset(float elapsed, float amplitude)
    {
        float safeAmplitude = Mathf.Max(0f, amplitude);

        if (safeAmplitude <= 0f)
        {
            return Vector2.zero;
        }

        float x = Mathf.Sin(elapsed * 58f) * safeAmplitude;
        float y = Mathf.Cos(elapsed * 71f) * safeAmplitude * 0.72f;
        return new Vector2(x, y);
    }

    // Busca final stinger entry.
    private CreditTextEntry FindFinalStingerEntry(List<CreditTextEntry> entries)
    {
        if (entries == null || entries.Count == 0)
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            CreditTextEntry entry = entries[i];

            if (entry == null || entry.Text == null)
            {
                continue;
            }

            string content = entry.Text.text != null ? entry.Text.text.ToLowerInvariant() : string.Empty;

            if (content.Contains("thanks") || content.Contains("playing"))
            {
                return entry;
            }
        }

        return entries[0];
    }

    // Ajusta duraciones para que los créditos queden dentro del tiempo objetivo.
    private void ApplyHypeDurationScaling(
        int namesCount,
        ref float revealDuration,
        ref float revealGap,
        ref float comboHold,
        ref float finalDuration)
    {
        float clampedTargetMin = Mathf.Max(0.1f, targetCreditsDurationMin);
        float clampedTargetMax = Mathf.Max(clampedTargetMin, targetCreditsDurationMax);
        float totalDuration =
            Mathf.Max(0f, introBeatDuration) +
            (Mathf.Max(0, namesCount) * revealDuration) +
            (Mathf.Max(0, namesCount - 1) * revealGap) +
            comboHold +
            finalDuration +
            Mathf.Max(0f, outroFadeDuration);

        if (totalDuration <= 0.01f)
        {
            return;
        }

        float scale = 1f;

        if (totalDuration < clampedTargetMin)
        {
            scale = clampedTargetMin / totalDuration;
        }
        else if (totalDuration > clampedTargetMax)
        {
            scale = clampedTargetMax / totalDuration;
        }

        if (Mathf.Approximately(scale, 1f))
        {
            return;
        }

        revealDuration = Mathf.Max(0.01f, revealDuration * scale);
        revealGap = Mathf.Max(0f, revealGap * scale);
        comboHold = Mathf.Max(0f, comboHold * scale);
        finalDuration = Mathf.Max(0.01f, finalDuration * scale);
    }

    // Reproduce audio de créditos con fallback al AudioManager si hace falta.
    private void PlayCreditsAudio(AudioClip clip, string fallbackToken)
    {
        if (clip != null)
        {
            AudioSource source = creditsAudioSource;

            if (source != null)
            {
                source.PlayOneShot(clip);
                return;
            }
        }

        if (!useAudioManagerFallback || AudioManager.Instance == null || AudioManager.Instance.SfxList == null)
        {
            return;
        }

        int fallbackIndex = FindAudioManagerSfxIndexByToken(fallbackToken);

        if (fallbackIndex < 0)
        {
            fallbackIndex = FindAudioManagerSfxIndexByToken("click");
        }

        if (fallbackIndex < 0)
        {
            return;
        }

        AudioManager.Instance.PlaySFX(fallbackIndex);
    }

    // Busca audio gestor SFX indice por token.
    private int FindAudioManagerSfxIndexByToken(string token)
    {
        if (AudioManager.Instance == null || AudioManager.Instance.SfxList == null)
        {
            return -1;
        }

        AudioClip[] sfxList = AudioManager.Instance.SfxList;
        string normalizedToken = token != null ? token.ToLowerInvariant() : string.Empty;

        for (int i = 0; i < sfxList.Length; i++)
        {
            AudioClip clip = sfxList[i];

            if (clip == null)
            {
                continue;
            }

            string clipName = clip.name != null ? clip.name.ToLowerInvariant() : string.Empty;

            if (!string.IsNullOrEmpty(normalizedToken) && clipName.Contains(normalizedToken))
            {
                return i;
            }
        }

        return sfxList.Length > 0 ? 0 : -1;
    }

    // Comprueba si el usuario puede y quiere saltar los créditos.
    private bool ShouldSkipCredits(bool allowSkip, float skipAllowedAtTime)
    {
        if (!allowSkip || Time.unscaledTime < skipAllowedAtTime)
        {
            return false;
        }

        return Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0);
    }

    // Curva de suavizado para que los movimientos de texto se vean naturales.
    private float EaseOutCubic(float t)
    {
        float clamped = Mathf.Clamp01(t);
        float inverse = 1f - clamped;
        return 1f - inverse * inverse * inverse;
    }

    // Elige la curva de suavizado configurada para animar créditos.
    private float EvaluateCreditsEase(float t)
    {
        float clamped = Mathf.Clamp01(t);

        switch (easeType)
        {
            case CreditsEaseType.OutSine:
                return Mathf.Sin((clamped * Mathf.PI) * 0.5f);
            case CreditsEaseType.OutBack:
                return EaseOutBack(clamped);
            default:
                return EaseOutCubic(clamped);
        }
    }

    // Curva con pequeño rebote para dar energía a la animación.
    private float EaseOutBack(float t)
    {
        float clamped = Mathf.Clamp01(t);
        const float overshoot = 1.70158f;
        float adjusted = clamped - 1f;
        return 1f + adjusted * adjusted * ((overshoot + 1f) * adjusted + overshoot);
    }

    // Espera en tiempo real (sin depender de la escala de tiempo del juego).
    private WaitForSecondsRealtime WaitForSecondsRealtime(float duration)
    {
        return new WaitForSecondsRealtime(Mathf.Max(0f, duration));
    }

    // Helper de UI para activar o desactivar paneles.
    private void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }

    // Asegura un CanvasGroup para poder aplicar transiciones al panel.
    private CanvasGroup EnsureCanvasGroup(GameObject target)
    {
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    // Conecta botones y evita listeners duplicados.
    private void BindListeners()
    {
        if (listenersBound)
        {
            return;
        }

        AutoDiscoverReferences();

        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveListener(OpenCreditsFromButton);
            creditsButton.onClick.AddListener(OpenCreditsFromButton);
            listenersBound = true;
            return;
        }

        GameDebug.Advertencia(
            "EndMenu",
            "No se encontró CreditsButton. Asigna la referencia en Inspector o mantén el nombre 'CreditsButton'.",
            this);
    }

    // Desconecta listeners para no dejar suscripciones activas.
    private void UnbindListeners()
    {
        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveListener(OpenCreditsFromButton);
        }

        listenersBound = false;
    }

    // Gestiona auto detectar referencias.
    private void AutoDiscoverReferences()
    {
        AutoDiscoverCreditsButton();
        AutoDiscoverCreditsTextRoot();
        AutoDiscoverScorePanel();
    }

    // Asegura puntuación panel controller.
    private void EnsureScorePanelController()
    {
        if (scorePanelController == null)
        {
            scorePanelController = GetComponent<ScorePanelController>();
        }

        if (scorePanelController == null)
        {
            GameDebug.Advertencia(
                "EndMenu",
                "No se encontró ScorePanelController en el controlador. Añádelo en la escena EndMenu.",
                this);
            return;
        }

        scorePanelController.ConfigureIfNeeded(panelButtons, panelScore);
        AutoDiscoverScorePanel();
    }

    // Gestiona auto detectar créditos botón.
    private void AutoDiscoverCreditsButton()
    {
        if (creditsButton != null || panelButtons == null)
        {
            return;
        }

        Button[] buttons = panelButtons.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button candidate = buttons[i];

            if (candidate != null && candidate.name == "CreditsButton")
            {
                creditsButton = candidate;
                return;
            }
        }
    }

    // Gestiona auto detectar créditos texto raíz.
    private void AutoDiscoverCreditsTextRoot()
    {
        if (creditsTextRoot != null || panelCredits == null)
        {
            return;
        }

        creditsTextRoot = FindChildByName(panelCredits.transform, "Textos") ?? panelCredits.transform;
    }

    // Gestiona auto detectar puntuación panel.
    private void AutoDiscoverScorePanel()
    {
        if (panelScore != null)
        {
            return;
        }

        Transform searchRoot = panelButtons != null && panelButtons.transform.parent != null
            ? panelButtons.transform.parent
            : transform.root;
        Transform scoreTransform = FindChildByName(searchRoot, "PanelScore");
        panelScore = scoreTransform != null ? scoreTransform.gameObject : null;
    }

    // Busca hijo por nombre.
    private Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform candidate = children[i];

            if (candidate != null && candidate.name == childName)
            {
                return candidate;
            }
        }

        return null;
    }
}
