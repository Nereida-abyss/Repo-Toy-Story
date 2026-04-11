using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class PanelController : MonoBehaviour
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
    [SerializeField] private CreditsPresentationProfile creditsProfile;

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
    private bool hasLoggedMissingCreditsProfile;

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

    // Arranca la configuración inicial del componente.
    // Abre créditos desde el botón.
    // Arranca la secuencia solo si el menu esta listo.
    // Envuelve cualquier secuencia larga para marcar cu?ndo est? ocupada
    // y limpiar bien el estado al terminar.
    // Secuencia inicial del EndMenu: GameOver -> créditos.
    // Esta es la entrada principal del EndMenu.
    // Esconde paneles, ense?a Game Over un momento
    // y luego entrega el control a la secuencia de cr?ditos.
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
    // Orquesta toda la experiencia de cr?ditos de principio a fin.
    // Desactiva la UI que molesta, prepara el panel, deja saltar la secuencia
    // y al final devuelve el control al men? de botones sin dejar restos visuales.
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

        CanvasGroup creditsCanvasGroup = RequireCanvasGroup(panelCredits, nameof(panelCredits));

        if (creditsCanvasGroup != null)
        {
            creditsCanvasGroup.alpha = 1f;
            creditsCanvasGroup.interactable = false;
            creditsCanvasGroup.blocksRaycasts = false;
        }

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
            PlayCreditsAudio(outroSwishClip, "swish");
        }

        yield return FadeCanvasGroupAlpha(creditsCanvasGroup, fadeDuration);

        if (creditsCanvasGroup != null)
        {
            creditsCanvasGroup.alpha = 1f;
        }

        SetPanelActive(panelCredits, false);

        if (creditsPanelFx != null)
        {
            creditsPanelFx.enabled = creditsPanelFxWasEnabled;
        }

        SetPanelActive(panelButtons, true);
    }

    // Animación principal de créditos: intro, nombres y cierre final.
    // Esta es la coreograf?a principal de cr?ditos.
    // Prepara textos, hace la entrada fuerte, ense?a nombres y remata con el cierre final,
    // pero si el jugador pide skip, corta limpio y deja todo en un estado consistente.
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
                PlayCreditsAudio(nameTickClip, "tick");
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
    // Pulso inicial de apertura antes de ense?ar nombres.
    // Es corto, vistoso y adem?s comprueba el skip para no bloquear la salida r?pida.
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

    // Variante por secciones: primero presenta t?tulo, luego grupos intermedios y al final el remate.
    // Sirve para guiar la vista del jugador y que no aparezcan todos los textos peleando por atenci?n.
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

    // Hace entrar un grupo de textos con movimiento, escala y alpha.
    // Piensa en ello como abrir una tarjeta y darle foco antes de pasar a la siguiente.
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

    // Muestra cada l?nea una detr?s de otra para que el bloque se lea mejor
    // cuando interesa dar m?s protagonismo a los nombres.
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

    // Cambia el foco de una secci?n subiendo o bajando su transparencia sin saltos bruscos.
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

    // Espera un rato en tiempo real, pero permite cortar antes si el jugador pide skip.
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

    // Entrada progresiva de textos individuales.
    // Es una versi?n simple del sistema de cr?ditos cuando queremos animar por l?neas y no por secciones.
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

    // Plan B cuando no hay textos preparados para animar.
    // Mantiene el panel visible un tiempo razonable y sigue respetando el skip.
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

    // Conecta botones sin duplicar listeners aunque el objeto se active varias veces.
    private void BindListeners()
    {
        if (listenersBound)
        {
            return;
        }

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

    // Desconecta listeners para no dejar llamadas colgadas al desactivar el panel.
    private void UnbindListeners()
    {
        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveListener(OpenCreditsFromButton);
        }

        listenersBound = false;
    }

    // Asegura el controlador de puntuaci?n y lo deja enlazado con los paneles correctos.
    private void EnsureScorePanelController()
    {
        if (scorePanelController == null)
        {
            ConfigureScorePanelController();
            return;
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
    }

    // Configura el panel de score solo si su referencia esta serializada de forma explicita.
    private void ConfigureScorePanelController()
    {
        if (scorePanelController == null)
        {
            GameDebug.Advertencia(
                "EndMenu",
                "PanelController necesita una referencia explicita a ScorePanelController en el inspector.",
                this);
            return;
        }

        scorePanelController.ConfigureIfNeeded(panelButtons, panelScore);
    }

    // Devuelve la raiz real de textos de creditos sin busquedas por nombre.
    private Transform ResolveCreditsTextRoot()
    {
        if (creditsTextRoot != null)
        {
            return creditsTextRoot;
        }

        if (panelCredits != null)
        {
            GameDebug.Advertencia(
                "EndMenu",
                "PanelController necesita una referencia explicita a creditsTextRoot en el inspector.",
                this);
            return panelCredits.transform;
        }

        return null;
    }

    // Comprueba las dependencias clave del flujo para que EndMenu no dependa de autodeteccion.
    private void ValidateReferences()
    {
        ValidatePanelReference(panelGameOver, "panelGameOver");
        ValidatePanelReference(panelCredits, "panelCredits");
        ValidatePanelReference(panelButtons, "panelButtons");
        ValidatePanelReference(panelSetting, "panelSetting");
        ValidatePanelReference(panelScore, "panelScore");

        if (panelGameOver != null)
        {
            RequireCanvasGroup(panelGameOver, panelGameOver.name);
        }

        if (panelCredits != null)
        {
            RequireCanvasGroup(panelCredits, panelCredits.name);
        }

        if (panelButtons != null)
        {
            RequireCanvasGroup(panelButtons, panelButtons.name);
        }

        if (creditsTextRoot == null)
        {
            GameDebug.Advertencia(
                "EndMenu",
                "PanelController necesita creditsTextRoot asignado desde el inspector.",
                this);
        }

        if (creditsAudioSource == null)
        {
            GameDebug.Advertencia(
                "EndMenu",
                "PanelController necesita creditsAudioSource asignado desde el inspector.",
                this);
        }
    }

    private void ValidatePanelReference(GameObject panel, string fieldName)
    {
        if (panel != null)
        {
            return;
        }

        GameDebug.Advertencia(
            "EndMenu",
            $"PanelController no tiene la referencia '{fieldName}' asignada en el inspector.",
            this);
    }
}
