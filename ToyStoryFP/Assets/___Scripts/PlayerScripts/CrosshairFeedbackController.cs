using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CrosshairFeedbackController : MonoBehaviour
{
    [Header("Hit Marker")]
    [SerializeField] private Color hitMarkerColor = Color.white;
    [SerializeField] private float hitMarkerDuration = 0.12f;
    [SerializeField] private float hitMarkerSize = 24f;
    [SerializeField] private float hitMarkerThickness = 3f;
    [SerializeField] private float hitMarkerScalePunch = 1.16f;

    [Header("Death Marker")]
    [SerializeField] private Color deathMarkerColor = new Color(1f, 0.3f, 0.12f, 1f);
    [SerializeField] private float deathMarkerDuration = 0.22f;
    [SerializeField] private float deathMarkerSize = 34f;
    [SerializeField] private float deathMarkerThickness = 4f;
    [SerializeField] private float deathMarkerScalePunch = 1.25f;

    private static Sprite whiteSprite;

    private RectTransform hitMarkerRoot;
    private RectTransform deathMarkerRoot;
    private CanvasGroup hitMarkerCanvasGroup;
    private CanvasGroup deathMarkerCanvasGroup;
    private float hitMarkerTimer;
    private float deathMarkerTimer;
    private Vector3 hitBaseScale = Vector3.one;
    private Vector3 deathBaseScale = Vector3.one;

    public static CrosshairFeedbackController Instance { get; private set; }

    public static CrosshairFeedbackController EnsureOnCrosshair(Transform crosshairTransform)
    {
        if (crosshairTransform == null)
        {
            return null;
        }

        CrosshairFeedbackController feedback = crosshairTransform.GetComponent<CrosshairFeedbackController>();

        if (feedback == null)
        {
            feedback = crosshairTransform.gameObject.AddComponent<CrosshairFeedbackController>();
        }

        return feedback;
    }

    void Awake()
    {
        Instance = this;
        EnsureMarkers();
        SetMarkerAlpha(hitMarkerCanvasGroup, 0f);
        SetMarkerAlpha(deathMarkerCanvasGroup, 0f);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        UpdateMarker(ref hitMarkerTimer, hitMarkerDuration, hitMarkerCanvasGroup, hitMarkerRoot, hitBaseScale, hitMarkerScalePunch);
        UpdateMarker(ref deathMarkerTimer, deathMarkerDuration, deathMarkerCanvasGroup, deathMarkerRoot, deathBaseScale, deathMarkerScalePunch);
    }

    public void PlayHitMarker()
    {
        EnsureMarkers();
        hitMarkerTimer = hitMarkerDuration;
        SetMarkerAlpha(hitMarkerCanvasGroup, 1f);
        hitMarkerRoot.localScale = hitBaseScale * hitMarkerScalePunch;
    }

    public void PlayDeathMarker()
    {
        EnsureMarkers();
        deathMarkerTimer = deathMarkerDuration;
        SetMarkerAlpha(deathMarkerCanvasGroup, 1f);
        deathMarkerRoot.localScale = deathBaseScale * deathMarkerScalePunch;
    }

    private void EnsureMarkers()
    {
        if (hitMarkerRoot == null)
        {
            hitMarkerRoot = EnsureMarkerRoot("HitMarker", hitMarkerColor, hitMarkerSize, hitMarkerThickness, out hitMarkerCanvasGroup);
            hitBaseScale = hitMarkerRoot.localScale;
        }

        if (deathMarkerRoot == null)
        {
            deathMarkerRoot = EnsureMarkerRoot("DeathMarker", deathMarkerColor, deathMarkerSize, deathMarkerThickness, out deathMarkerCanvasGroup);
            deathBaseScale = deathMarkerRoot.localScale;
        }
    }

    private RectTransform EnsureMarkerRoot(string markerName, Color color, float size, float thickness, out CanvasGroup canvasGroup)
    {
        Transform existingMarker = transform.Find(markerName);
        RectTransform markerRoot = existingMarker as RectTransform;

        if (markerRoot == null)
        {
            GameObject markerObject = new GameObject(markerName, typeof(RectTransform), typeof(CanvasGroup));
            markerRoot = markerObject.GetComponent<RectTransform>();
            markerRoot.SetParent(transform, false);
            markerRoot.anchorMin = new Vector2(0.5f, 0.5f);
            markerRoot.anchorMax = new Vector2(0.5f, 0.5f);
            markerRoot.pivot = new Vector2(0.5f, 0.5f);
            markerRoot.sizeDelta = new Vector2(size, size);
            markerRoot.anchoredPosition = Vector2.zero;

            CreateMarkerStroke(markerRoot, "StrokeA", color, size, thickness, 45f);
            CreateMarkerStroke(markerRoot, "StrokeB", color, size, thickness, -45f);
        }
        else
        {
            markerRoot.sizeDelta = new Vector2(size, size);
        }

        canvasGroup = markerRoot.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = markerRoot.gameObject.AddComponent<CanvasGroup>();
        }

        markerRoot.localScale = Vector3.one;
        return markerRoot;
    }

    private void CreateMarkerStroke(RectTransform parent, string strokeName, Color color, float size, float thickness, float rotation)
    {
        GameObject strokeObject = new GameObject(strokeName, typeof(RectTransform), typeof(Image));
        RectTransform strokeRect = strokeObject.GetComponent<RectTransform>();
        strokeRect.SetParent(parent, false);
        strokeRect.anchorMin = new Vector2(0.5f, 0.5f);
        strokeRect.anchorMax = new Vector2(0.5f, 0.5f);
        strokeRect.pivot = new Vector2(0.5f, 0.5f);
        strokeRect.sizeDelta = new Vector2(size, thickness);
        strokeRect.localEulerAngles = new Vector3(0f, 0f, rotation);

        Image strokeImage = strokeObject.GetComponent<Image>();
        strokeImage.sprite = GetWhiteSprite();
        strokeImage.type = Image.Type.Simple;
        strokeImage.color = color;
    }

    private void UpdateMarker(
        ref float timer,
        float totalDuration,
        CanvasGroup canvasGroup,
        RectTransform markerRoot,
        Vector3 baseScale,
        float scalePunch)
    {
        if (canvasGroup == null || markerRoot == null)
        {
            return;
        }

        if (timer <= 0f)
        {
            SetMarkerAlpha(canvasGroup, 0f);
            markerRoot.localScale = baseScale;
            return;
        }

        timer = Mathf.Max(0f, timer - Time.unscaledDeltaTime);
        float normalizedTime = totalDuration > 0f ? 1f - (timer / totalDuration) : 1f;
        float alpha = 1f - normalizedTime;
        float scale = Mathf.Lerp(scalePunch, 1f, normalizedTime);

        SetMarkerAlpha(canvasGroup, alpha);
        markerRoot.localScale = baseScale * scale;
    }

    private void SetMarkerAlpha(CanvasGroup canvasGroup, float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
        {
            return whiteSprite;
        }

        whiteSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f));

        return whiteSprite;
    }
}
