using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelController : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelGameOver;
    public GameObject panelCredits;
    public GameObject panelButtons;
    public GameObject panelSetting;
    public GameObject panelScore;

    [Header("Creditos (Opcional)")]
    [SerializeField] private Button creditsButton;
    [SerializeField] private Transform creditsTextRoot;

    [Header("Score (Opcional)")]
    [SerializeField] private ScorePanelController scorePanelController;

    [Header("Tiempos")]
    [SerializeField] private float gameOverDuration = 3f;
    [SerializeField] private float creditsHoldDuration = 1.3f;
    [SerializeField] private float creditsFadeOutDuration = 0.6f;
    [SerializeField] private float skippedFadeOutDuration = 0.2f;
    [SerializeField] private float fallbackCreditsDuration = 3f;
    [SerializeField] private float skipInputGracePeriod = 0.15f;

    [Header("Animacion de Textos")]
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

    private void Awake()
    {
        AutoDiscoverReferences();
        EnsureScorePanelController();
        BindListeners();
    }

    private void OnEnable()
    {
        AutoDiscoverReferences();
        EnsureScorePanelController();
        BindListeners();
    }

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

    private void Start()
    {
        StartSequence(PlayIntroSequence());
    }

    public void OpenCreditsFromButton()
    {
        if (isSequenceRunning)
        {
            return;
        }

        StartSequence(PlayCreditsSequence(allowSkip: true));
    }

    private void StartSequence(IEnumerator routine)
    {
        if (routine == null || isSequenceRunning)
        {
            return;
        }

        activeSequence = StartCoroutine(RunManagedSequence(routine));
    }

    private IEnumerator RunManagedSequence(IEnumerator routine)
    {
        isSequenceRunning = true;
        yield return routine;
        isSequenceRunning = false;
        activeSequence = null;
    }

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
            Debug.LogWarning("PanelController is missing panelGameOver reference.", this);
        }

        yield return PlayCreditsSequence(allowSkip: false);
    }

    private IEnumerator PlayCreditsSequence(bool allowSkip)
    {
        if (panelCredits == null)
        {
            Debug.LogWarning("PanelController is missing panelCredits reference. Skipping credits sequence.", this);
            SetPanelActive(panelButtons, true);
            yield break;
        }

        SetPanelActive(panelButtons, false);
        SetPanelActive(panelSetting, false);
        SetPanelActive(panelScore, false);
        SetPanelActive(panelCredits, true);

        CanvasGroup creditsCanvasGroup = EnsureCanvasGroup(panelCredits);
        creditsCanvasGroup.alpha = 1f;
        creditsCanvasGroup.interactable = false;
        creditsCanvasGroup.blocksRaycasts = false;

        List<CreditTextEntry> textEntries = CollectCreditTextEntries();
        float skipAllowedAtTime = Time.unscaledTime + skipInputGracePeriod;
        bool skipRequested = false;

        if (textEntries.Count > 0)
        {
            yield return AnimateCreditTextsIn(textEntries, allowSkip, skipAllowedAtTime, () => skipRequested = true);

            if (!skipRequested)
            {
                yield return HoldCredits(allowSkip, skipAllowedAtTime, () => skipRequested = true);
            }

            RestoreCreditTextEntries(textEntries);
        }
        else
        {
            Debug.LogWarning("PanelController could not find TMP texts in credits panel. Using fallback timing.", this);
            yield return HoldFallbackCredits(allowSkip, skipAllowedAtTime, () => skipRequested = true);
        }

        float fadeDuration = skipRequested ? skippedFadeOutDuration : creditsFadeOutDuration;
        yield return FadeCanvasGroupAlpha(creditsCanvasGroup, fadeDuration);

        creditsCanvasGroup.alpha = 1f;
        SetPanelActive(panelCredits, false);
        SetPanelActive(panelButtons, true);
    }

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

    private IEnumerator HoldCredits(bool allowSkip, float skipAllowedAtTime, System.Action onSkip)
    {
        float holdElapsed = 0f;

        while (holdElapsed < creditsHoldDuration)
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

    private void RestoreCreditTextEntries(List<CreditTextEntry> entries)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            CreditTextEntry entry = entries[i];
            entry.RectTransform.anchoredPosition = entry.OriginalAnchoredPosition;
            entry.Text.color = entry.OriginalColor;
        }
    }

    private void SetTextAlpha(CreditTextEntry entry, float normalizedAlpha)
    {
        Color color = entry.OriginalColor;
        color.a = Mathf.Clamp01(normalizedAlpha) * entry.OriginalColor.a;
        entry.Text.color = color;
    }

    private bool ShouldSkipCredits(bool allowSkip, float skipAllowedAtTime)
    {
        if (!allowSkip || Time.unscaledTime < skipAllowedAtTime)
        {
            return false;
        }

        return Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0);
    }

    private float EaseOutCubic(float t)
    {
        float clamped = Mathf.Clamp01(t);
        float inverse = 1f - clamped;
        return 1f - inverse * inverse * inverse;
    }

    private WaitForSecondsRealtime WaitForSecondsRealtime(float duration)
    {
        return new WaitForSecondsRealtime(Mathf.Max(0f, duration));
    }

    private void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }

    private CanvasGroup EnsureCanvasGroup(GameObject target)
    {
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

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

        Debug.LogWarning(
            "PanelController could not find CreditsButton. Assign it in the inspector or keep name as 'CreditsButton'.",
            this);
    }

    private void UnbindListeners()
    {
        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveListener(OpenCreditsFromButton);
        }

        listenersBound = false;
    }

    private void AutoDiscoverReferences()
    {
        AutoDiscoverCreditsButton();
        AutoDiscoverCreditsTextRoot();
        AutoDiscoverScorePanel();
    }

    private void EnsureScorePanelController()
    {
        if (scorePanelController == null)
        {
            scorePanelController = GetComponent<ScorePanelController>();
        }

        if (scorePanelController == null)
        {
            Debug.LogWarning(
                "PanelController could not find ScorePanelController on Controlador. Add it in EndMenu scene.",
                this);
            return;
        }

        scorePanelController.ConfigureIfNeeded(panelButtons, panelScore);
        AutoDiscoverScorePanel();
    }

    private void AutoDiscoverCreditsButton()
    {
        if (creditsButton != null || panelButtons == null)
        {
            return;
        }

        Transform directMatch = panelButtons.transform.Find("CreditsButton");

        if (directMatch != null)
        {
            creditsButton = directMatch.GetComponent<Button>();
        }

        if (creditsButton != null)
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

    private void AutoDiscoverCreditsTextRoot()
    {
        if (creditsTextRoot != null || panelCredits == null)
        {
            return;
        }

        Transform directMatch = panelCredits.transform.Find("Textos");
        creditsTextRoot = directMatch != null ? directMatch : panelCredits.transform;
    }

    private void AutoDiscoverScorePanel()
    {
        if (panelScore != null)
        {
            return;
        }

        Transform scoreTransform = transform.Find("PanelScore");

        if (scoreTransform == null && transform.parent != null)
        {
            scoreTransform = transform.parent.Find("PanelScore");
        }

        panelScore = scoreTransform != null ? scoreTransform.gameObject : null;
    }
}
