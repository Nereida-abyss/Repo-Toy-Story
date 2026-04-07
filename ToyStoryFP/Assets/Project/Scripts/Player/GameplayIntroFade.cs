using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class GameplayIntroFade : MonoBehaviour
{
    [SerializeField] [Min(0.01f)] private float fadeDuration = 1.35f;
    [SerializeField] [Range(0f, 1f)] private float startVignetteIntensity = 1f;
    [SerializeField] [Range(0f, 1f)] private float endVignetteIntensity = 0f;
    [SerializeField] private float startPostExposure = -10f;
    [SerializeField] private float endPostExposure = 0f;
    [SerializeField] [Range(0f, 1f)] private float startVignetteSmoothness = 1f;
    [SerializeField] [Range(0f, 1f)] private float endVignetteSmoothness = 0.2f;

    private Volume introVolume;
    private VolumeProfile runtimeProfile;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    private UniversalAdditionalCameraData cameraData;
    private bool originalPostProcessingState;
    private List<UIFadeUtility.FadeTarget> uiFadeTargets = new List<UIFadeUtility.FadeTarget>();

    void Awake()
    {
        EnsurePostProcessingVolume();
        CacheUiFadeTargets();
        ApplyFadeState(0f);
        ApplyUiFadeState(0f);
    }

    void Start()
    {
        StartCoroutine(FadeInRoutine());
    }

    void OnDestroy()
    {
        RestoreUiFadeTargets();

        if (runtimeProfile != null)
        {
            Destroy(runtimeProfile);
        }
    }

    private IEnumerator FadeInRoutine()
    {
        float duration = Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            ApplyFadeState(normalized);
            ApplyUiFadeState(normalized);
            yield return null;
        }

        ApplyFadeState(1f);
        ApplyUiFadeState(1f);

        if (introVolume != null)
        {
            introVolume.weight = 0f;
            introVolume.enabled = false;
        }

        if (cameraData != null)
        {
            cameraData.renderPostProcessing = originalPostProcessingState;
        }

        CleanupUiFadeTargets();
    }

    private void EnsurePostProcessingVolume()
    {
        cameraData = GetComponent<UniversalAdditionalCameraData>();

        if (cameraData != null)
        {
            originalPostProcessingState = cameraData.renderPostProcessing;
            cameraData.renderPostProcessing = true;
        }

        introVolume = GetComponent<Volume>();

        if (introVolume == null)
        {
            introVolume = gameObject.AddComponent<Volume>();
        }

        introVolume.isGlobal = true;
        introVolume.priority = 100f;
        introVolume.blendDistance = 0f;
        introVolume.weight = 1f;
        introVolume.enabled = true;

        runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        runtimeProfile.hideFlags = HideFlags.DontSave;

        vignette = runtimeProfile.Add<Vignette>(true);
        vignette.active = true;
        vignette.color.Override(Color.black);
        vignette.center.Override(new Vector2(0.5f, 0.5f));
        vignette.rounded.Override(false);

        colorAdjustments = runtimeProfile.Add<ColorAdjustments>(true);
        colorAdjustments.active = true;

        introVolume.sharedProfile = null;
        introVolume.profile = runtimeProfile;
    }

    private void ApplyFadeState(float normalized)
    {
        if (vignette == null || colorAdjustments == null || introVolume == null)
        {
            return;
        }

        float eased = Mathf.SmoothStep(0f, 1f, normalized);

        introVolume.enabled = true;
        introVolume.weight = 1f;
        vignette.intensity.Override(Mathf.Lerp(startVignetteIntensity, endVignetteIntensity, eased));
        vignette.smoothness.Override(Mathf.Lerp(startVignetteSmoothness, endVignetteSmoothness, eased));
        colorAdjustments.postExposure.Override(Mathf.Lerp(startPostExposure, endPostExposure, eased));
    }

    private void CacheUiFadeTargets()
    {
        uiFadeTargets = UIFadeUtility.ResolveActiveCanvasTargets(gameObject.scene);
    }

    private void ApplyUiFadeState(float normalized)
    {
        if (uiFadeTargets == null || uiFadeTargets.Count == 0)
        {
            return;
        }

        float eased = Mathf.SmoothStep(0f, 1f, normalized);

        for (int i = 0; i < uiFadeTargets.Count; i++)
        {
            UIFadeUtility.FadeTarget target = uiFadeTargets[i];

            if (target == null)
            {
                continue;
            }

            target.SetAlpha(Mathf.Lerp(0f, target.OriginalAlpha, eased));
        }
    }

    private void RestoreUiFadeTargets()
    {
        if (uiFadeTargets == null || uiFadeTargets.Count == 0)
        {
            return;
        }

        for (int i = 0; i < uiFadeTargets.Count; i++)
        {
            UIFadeUtility.FadeTarget target = uiFadeTargets[i];

            if (target == null)
            {
                continue;
            }

            target.Restore();
        }
    }

    private void CleanupUiFadeTargets()
    {
        if (uiFadeTargets == null || uiFadeTargets.Count == 0)
        {
            return;
        }

        RestoreUiFadeTargets();

        for (int i = 0; i < uiFadeTargets.Count; i++)
        {
            UIFadeUtility.FadeTarget target = uiFadeTargets[i];

            if (target == null)
            {
                continue;
            }

            target.Cleanup();
        }

        uiFadeTargets.Clear();
    }
}
