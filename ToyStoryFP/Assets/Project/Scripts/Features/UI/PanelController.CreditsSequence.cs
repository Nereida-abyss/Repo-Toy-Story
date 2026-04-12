using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PanelController
{
    private IEnumerator AnimateCreditsHypeReel(List<CreditTextEntry> entries, bool allowSkip, float skipAllowedAtTime, System.Action onSkip)
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

        PlayCreditsAudio(creditsProfile != null ? creditsProfile.IntroWhooshClip : null);
        yield return AnimateIntroBeat(animatedRoot, baseRootScale, Mathf.Max(0.01f, introBeatDuration), Mathf.Max(0.01f, introStartScale), clampedPulseAmount, allowSkip, skipAllowedAtTime, skipAction);

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
        ApplyHypeDurationScaling(names.Count, ref scaledNameRevealDuration, ref scaledNameGap, ref scaledComboHold, ref scaledFinalDuration);

        for (int i = 0; i < names.Count; i++)
        {
            CreditTextEntry currentName = names[i];
            SetTextAlpha(currentName, 0f);
            currentName.RectTransform.localScale = Vector3.one;
            currentName.RectTransform.anchoredPosition = currentName.OriginalAnchoredPosition + Vector2.up * Mathf.Max(0f, nameStartYOffset);

            PlayCreditsAudio(creditsProfile != null ? creditsProfile.NameHitClip : null);
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
                Vector2 baseAnchoredPosition = currentName.OriginalAnchoredPosition + Vector2.up * Mathf.Lerp(nameStartYOffset, 0f, eased);

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
                PlayCreditsAudio(creditsProfile != null ? creditsProfile.NameTickClip : null);
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
            finalEntry.RectTransform.anchoredPosition = finalEntry.OriginalAnchoredPosition + Vector2.up * Mathf.Max(nameStartYOffset, sectionStartYOffset * 0.45f);
            SetTextAlpha(finalEntry, 0f);
            PlayCreditsAudio(creditsProfile != null ? creditsProfile.FinalStingClip : null);
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
                Vector2 baseAnchoredPosition = finalEntry.OriginalAnchoredPosition + Vector2.up * Mathf.Lerp(Mathf.Max(nameStartYOffset, sectionStartYOffset * 0.45f), 0f, eased);

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

    private IEnumerator AnimateIntroBeat(Transform targetRoot, Vector3 baseRootScale, float duration, float startScaleMultiplier, float pulseAmount, bool allowSkip, float skipAllowedAtTime, System.Action onSkip)
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

    private IEnumerator HoldDuration(float duration, bool allowSkip, float skipAllowedAtTime, System.Action onSkip)
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
}
