using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PanelController
{
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
            GameDebug.Advertencia("EndMenu", "No se encontraron textos TMP en crÃ©ditos. Se usara timing de fallback.", this);
            yield return HoldFallbackCredits(allowSkip, skipAllowedAtTime, () => skipRequested = true);
        }

        float defaultOutro = Mathf.Max(0f, outroFadeDuration > 0f ? outroFadeDuration : globalFadeOutDuration);
        float regularFadeDuration = Mathf.Max(0f, defaultOutro > 0f ? defaultOutro : creditsFadeOutDuration);
        float fadeDuration = skipRequested ? skippedFadeOutDuration : regularFadeDuration;

        if (!skipRequested)
        {
            PlayCreditsAudio(creditsProfile != null ? creditsProfile.OutroSwishClip : null);
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
}
