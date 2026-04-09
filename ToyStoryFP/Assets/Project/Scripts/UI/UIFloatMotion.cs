using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIFloatMotion : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float verticalAmplitude = 8f;
    [SerializeField] private float floatFrequency = 0.18f;
    [SerializeField] private float scaleAmplitude = 0.02f;
    [SerializeField] private float phaseOffset;

    private RectTransform rectTransform;
    private Vector2 baseAnchoredPosition;
    private Vector3 baseLocalScale;
    private bool baseStateCached;

    private void Awake()
    {
        CacheBaseState();
        ApplyMotion(Time.unscaledTime);
    }

    private void OnEnable()
    {
        CacheBaseState(true);
        ApplyMotion(Time.unscaledTime);
    }

    private void OnDisable()
    {
        RestoreBaseState();
    }

    private void Update()
    {
        ApplyMotion(Time.unscaledTime);
    }

    private void OnValidate()
    {
        verticalAmplitude = Mathf.Max(0f, verticalAmplitude);
        floatFrequency = Mathf.Max(0f, floatFrequency);
        scaleAmplitude = Mathf.Max(0f, scaleAmplitude);
    }

    private void CacheBaseState(bool forceRefresh = false)
    {
        rectTransform ??= GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            return;
        }

        if (baseStateCached && !forceRefresh)
        {
            return;
        }

        baseAnchoredPosition = rectTransform.anchoredPosition;
        baseLocalScale = rectTransform.localScale;
        baseStateCached = true;
    }

    private void RestoreBaseState()
    {
        if (rectTransform == null || !baseStateCached)
        {
            return;
        }

        rectTransform.anchoredPosition = baseAnchoredPosition;
        rectTransform.localScale = baseLocalScale;
    }

    private void ApplyMotion(float unscaledTime)
    {
        if (rectTransform == null)
        {
            return;
        }

        CacheBaseState();

        float cycle = (unscaledTime * Mathf.Max(0f, floatFrequency) * Mathf.PI * 2f) + phaseOffset;
        float verticalOffset = Mathf.Sin(cycle) * verticalAmplitude;
        float scaleMultiplier = 1f + (Mathf.Sin(cycle + (Mathf.PI * 0.35f)) * scaleAmplitude);

        rectTransform.anchoredPosition = baseAnchoredPosition + Vector2.up * verticalOffset;
        rectTransform.localScale = baseLocalScale * scaleMultiplier;
    }
}
