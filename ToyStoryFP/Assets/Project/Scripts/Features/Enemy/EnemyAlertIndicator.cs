using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EnemyAlertIndicator : MonoBehaviour
{
    [SerializeField] private Color alertColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] private float fadeSpeed = 8f;
    [SerializeField] private float pulseDuration = 0.2f;
    [SerializeField] private float pulseScale = 1.25f;
    [SerializeField] private Vector2 canvasSize = new Vector2(50f, 50f);
    [SerializeField] private float worldScale = 0.006f;
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform textRect;
    [SerializeField] private TextMeshProUGUI alertText;

    private Transform followTarget;
    private float heightOffset;
    private Camera cachedCamera;
    private bool isVisible;
    private float pulseTimer;
    private bool hasLoggedMissingReferences;

    // Gestiona configure.
    public void Configure(Transform target, float offset)
    {
        followTarget = target;
        heightOffset = offset;
        if (!ResolveReferences())
        {
            return;
        }

        UpdateTransform();
        canvasGroup.alpha = 0f;
        textRect.localScale = Vector3.one;
    }

    // Actualiza visible.
    public void SetVisible(bool visible, bool playPulse)
    {
        if (!ResolveReferences())
        {
            return;
        }

        isVisible = visible;

        if (playPulse)
        {
            pulseTimer = pulseDuration;
        }

        if (!visible)
        {
            pulseTimer = 0f;
            textRect.localScale = Vector3.one;
        }
    }

    void Awake()
    {
        ResolveReferences();
    }

    void LateUpdate()
    {
        if (!ResolveReferences())
        {
            return;
        }

        UpdateTransform();
        UpdateVisibility();
        UpdatePulse();
    }

    // Asegura referencias serializadas sin crear UI en runtime.
    private bool ResolveReferences()
    {
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (alertText == null)
        {
            alertText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (textRect == null && alertText != null)
        {
            textRect = alertText.rectTransform;
        }

        if (canvas == null || canvasGroup == null || textRect == null || alertText == null)
        {
            if (!hasLoggedMissingReferences)
            {
                hasLoggedMissingReferences = true;
                GameDebug.Advertencia("IA", "EnemyAlertIndicator necesita Canvas, CanvasGroup y TextMeshProUGUI configurados en el prefab enemigo.", this);
            }

            return false;
        }

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 200;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect != null)
        {
            canvasRect.sizeDelta = canvasSize;
            canvasRect.localScale = Vector3.one * worldScale;
        }

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        alertText.alignment = TextAlignmentOptions.Center;
        alertText.text = "!";
        alertText.fontSize = 42f;
        alertText.color = alertColor;
        alertText.raycastTarget = false;
        alertText.textWrappingMode = TextWrappingModes.NoWrap;
        return true;
    }

    // Actualiza transform.
    private void UpdateTransform()
    {
        if (followTarget == null)
        {
            return;
        }

        transform.position = followTarget.position + Vector3.up * heightOffset;

        if (cachedCamera == null || !cachedCamera.isActiveAndEnabled)
        {
            cachedCamera = Camera.main;
        }

        if (cachedCamera == null)
        {
            return;
        }

        canvas.worldCamera = cachedCamera;
        transform.forward = cachedCamera.transform.forward;
    }

    // Actualiza visibility.
    private void UpdateVisibility()
    {
        float targetAlpha = isVisible ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
    }

    // Actualiza pulse.
    private void UpdatePulse()
    {
        if (textRect == null)
        {
            return;
        }

        if (pulseTimer <= 0f)
        {
            textRect.localScale = Vector3.one;
            return;
        }

        pulseTimer = Mathf.Max(0f, pulseTimer - Time.deltaTime);
        float normalized = pulseDuration <= 0f ? 1f : 1f - (pulseTimer / pulseDuration);
        float scale = Mathf.Lerp(pulseScale, 1f, normalized);
        textRect.localScale = Vector3.one * scale;
    }
}
