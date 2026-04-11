using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public partial class PanelController
{
    // Desvanece el panel en tiempo real para que el cierre funcione incluso con el juego pausado.
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

    private List<CreditTextEntry> CollectCreditTextEntries()
    {
        List<CreditTextEntry> entries = new List<CreditTextEntry>();
        Transform root = ResolveCreditsTextRoot();

        if (root == null)
        {
            return entries;
        }

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

        entries.Sort((left, right) => right.OriginalAnchoredPosition.y.CompareTo(left.OriginalAnchoredPosition.y));
        return entries;
    }

    private List<CreditSection> CollectCreditSections()
    {
        List<CreditSection> sections = new List<CreditSection>();
        Transform root = ResolveCreditsTextRoot();

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

            sectionEntries.Sort((left, right) => right.OriginalAnchoredPosition.y.CompareTo(left.OriginalAnchoredPosition.y));

            sections.Add(new CreditSection
            {
                Root = sectionRoot,
                OriginalLocalScale = sectionRoot.localScale,
                Entries = sectionEntries
            });
        }

        return sections.Count >= 2 ? sections : new List<CreditSection>();
    }

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

    private void SetSectionScale(CreditSection section, float multiplier)
    {
        if (section == null || section.Root == null)
        {
            return;
        }

        float clampedMultiplier = Mathf.Max(0.01f, multiplier);
        section.Root.localScale = section.OriginalLocalScale * clampedMultiplier;
    }

    private void SetTextAlpha(CreditTextEntry entry, float normalizedAlpha)
    {
        Color color = entry.OriginalColor;
        color.a = Mathf.Clamp01(normalizedAlpha) * entry.OriginalColor.a;
        entry.Text.color = color;
    }

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

    private void PlayCreditsAudio(AudioClip clip, string fallbackToken)
    {
        AudioSource source = creditsAudioSource;

        if (clip != null)
        {
            if (source != null)
            {
                source.PlayOneShot(clip);
                return;
            }
        }

        if (!useAudioManagerFallback || AudioManager.Instance == null)
        {
            return;
        }

        AudioClip fallbackClip = GetCreditsFallbackClip(fallbackToken);

        if (fallbackClip == null && !string.Equals(fallbackToken, "click"))
        {
            fallbackClip = GetCreditsFallbackClip("click");
        }

        if (fallbackClip == null || source == null)
        {
            return;
        }

        source.PlayOneShot(fallbackClip);
    }

    private AudioClip GetCreditsFallbackClip(string token)
    {
        if (AudioManager.Instance == null)
        {
            return null;
        }

        string normalizedToken = token != null ? token.ToLowerInvariant() : string.Empty;

        switch (normalizedToken)
        {
            case "whoosh":
                return AudioManager.Instance.GetCreditsIntroWhooshClip();
            case "hit":
                return AudioManager.Instance.GetCreditsNameHitClip();
            case "tick":
                return AudioManager.Instance.GetCreditsNameTickClip();
            case "sting":
                return AudioManager.Instance.GetCreditsFinalStingClip();
            case "swish":
                return AudioManager.Instance.GetCreditsOutroSwishClip();
            case "click":
                return AudioManager.Instance.GetCreditsNameTickClip() ?? AudioManager.Instance.GetUiClickClip();
            default:
                return AudioManager.Instance.GetUiClickClip();
        }
    }

    private bool ShouldSkipCredits(bool allowSkip, float skipAllowedAtTime)
    {
        if (!allowSkip || Time.unscaledTime < skipAllowedAtTime)
        {
            return false;
        }

        return ProjectInput.WasCreditsSkipRequested();
    }

    private float EaseOutCubic(float t)
    {
        float clamped = Mathf.Clamp01(t);
        float inverse = 1f - clamped;
        return 1f - inverse * inverse * inverse;
    }

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

    private float EaseOutBack(float t)
    {
        float clamped = Mathf.Clamp01(t);
        const float overshoot = 1.70158f;
        float adjusted = clamped - 1f;
        return 1f + adjusted * adjusted * ((overshoot + 1f) * adjusted + overshoot);
    }

    private WaitForSecondsRealtime WaitForSecondsRealtime(float duration)
    {
        return new WaitForSecondsRealtime(Mathf.Max(0f, duration));
    }
}
