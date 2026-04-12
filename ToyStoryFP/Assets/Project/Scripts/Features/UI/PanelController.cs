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

    [Header("CrÃ©ditos (Opcional)")]
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
}
