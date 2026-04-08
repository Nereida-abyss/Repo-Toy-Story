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

    private Transform followTarget;
    private float heightOffset;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform textRect;
    private Camera cachedCamera;
    private bool isVisible;
    private float pulseTimer;

    // Gestiona configure.
    public void Configure(Transform target, float offset)
    {
        followTarget = target;
        heightOffset = offset;
        EnsureVisuals();
        UpdateTransform();
        canvasGroup.alpha = 0f;
        textRect.localScale = Vector3.one;
    }

    // Actualiza visible.
    public void SetVisible(bool visible, bool playPulse)
    {
        EnsureVisuals();
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
        EnsureVisuals();
    }

    void LateUpdate()
    {
        EnsureVisuals();
        UpdateTransform();
        UpdateVisibility();
        UpdatePulse();
    }

    // Asegura visuals.
    private void EnsureVisuals()
    {
        if (canvas != null && canvasGroup != null && textRect != null)
        {
            return;
        }

        canvas = GetComponent<Canvas>();

        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 200;

        if (GetComponent<CanvasScaler>() == null)
        {
            gameObject.AddComponent<CanvasScaler>();
        }

        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = canvasSize;
        canvasRect.localScale = Vector3.one * worldScale;

        TextMeshProUGUI text = GetComponentInChildren<TextMeshProUGUI>(true);

        if (text == null)
        {
            GameObject textObject = new GameObject("AlertText", typeof(RectTransform));
            textObject.transform.SetParent(transform, false);
            text = textObject.AddComponent<TextMeshProUGUI>();
        }

        textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.alignment = TextAlignmentOptions.Center;
        text.text = "!";
        text.fontSize = 42f;
        text.color = alertColor;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        canvasGroup.alpha = 0f;
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
