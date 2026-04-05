using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class SceneTransitionFade : MonoBehaviour
{
    [SerializeField] [Min(0.01f)] private float fadeDuration = 1.1f;
    [SerializeField] [Range(0f, 1f)] private float startVignetteIntensity = 0f;
    [SerializeField] [Range(0f, 1f)] private float endVignetteIntensity = 1f;
    [SerializeField] private float startPostExposure = 0f;
    [SerializeField] private float endPostExposure = -10f;
    [SerializeField] [Range(0f, 1f)] private float startVignetteSmoothness = 0.2f;
    [SerializeField] [Range(0f, 1f)] private float endVignetteSmoothness = 1f;

    private static bool isTransitioning;

    private Volume transitionVolume;
    private VolumeProfile runtimeProfile;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    private UniversalAdditionalCameraData cameraData;
    private bool originalPostProcessingState;
    private Coroutine fadeCoroutine;
    private bool hasStartedSceneLoad;
    private List<UIFadeUtility.FadeTarget> uiFadeTargets = new List<UIFadeUtility.FadeTarget>();

    public static bool TryFadeOutAndLoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }

        if (isTransitioning)
        {
            return true;
        }

        Camera targetCamera = ResolveTargetCamera();

        if (targetCamera == null)
        {
            return false;
        }

        SceneTransitionFade fade = targetCamera.GetComponent<SceneTransitionFade>();

        if (fade == null)
        {
            fade = targetCamera.gameObject.AddComponent<SceneTransitionFade>();
        }

        fade.BeginFade(sceneName);
        return true;
    }

    void OnDestroy()
    {
        if (!hasStartedSceneLoad)
        {
            RestoreOriginalPostProcessingState();
            RestoreUiFadeTargets();
            CleanupUiFadeTargets();
        }

        if (runtimeProfile != null)
        {
            Destroy(runtimeProfile);
        }
    }

    private static Camera ResolveTargetCamera()
    {
        if (Camera.main != null && Camera.main.isActiveAndEnabled)
        {
            return Camera.main;
        }

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera candidate = cameras[i];

            if (candidate != null && candidate.isActiveAndEnabled)
            {
                return candidate;
            }
        }

        return null;
    }

    private void BeginFade(string sceneName)
    {
        if (fadeCoroutine != null)
        {
            return;
        }

        EnsurePostProcessingVolume();
        CacheUiFadeTargets();
        ApplyFadeState(0f);
        ApplyUiFadeState(0f);
        isTransitioning = true;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        fadeCoroutine = StartCoroutine(FadeOutAndLoadSceneRoutine(sceneName));
    }

    private IEnumerator FadeOutAndLoadSceneRoutine(string sceneName)
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
        hasStartedSceneLoad = true;
        SceneManager.LoadScene(sceneName);
    }

    private void EnsurePostProcessingVolume()
    {
        cameraData = GetComponent<UniversalAdditionalCameraData>();

        if (cameraData != null)
        {
            originalPostProcessingState = cameraData.renderPostProcessing;
            cameraData.renderPostProcessing = true;
        }

        if (transitionVolume == null)
        {
            transitionVolume = gameObject.AddComponent<Volume>();
        }

        transitionVolume.isGlobal = true;
        transitionVolume.priority = 200f;
        transitionVolume.blendDistance = 0f;
        transitionVolume.weight = 1f;
        transitionVolume.enabled = true;

        if (runtimeProfile == null)
        {
            runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            runtimeProfile.hideFlags = HideFlags.DontSave;

            vignette = runtimeProfile.Add<Vignette>(true);
            vignette.active = true;
            vignette.color.Override(Color.black);
            vignette.center.Override(new Vector2(0.5f, 0.5f));
            vignette.rounded.Override(false);

            colorAdjustments = runtimeProfile.Add<ColorAdjustments>(true);
            colorAdjustments.active = true;
        }

        transitionVolume.sharedProfile = null;
        transitionVolume.profile = runtimeProfile;
    }

    private void ApplyFadeState(float normalized)
    {
        if (vignette == null || colorAdjustments == null || transitionVolume == null)
        {
            return;
        }

        float eased = Mathf.SmoothStep(0f, 1f, normalized);

        transitionVolume.enabled = true;
        transitionVolume.weight = 1f;
        vignette.intensity.Override(Mathf.Lerp(startVignetteIntensity, endVignetteIntensity, eased));
        vignette.smoothness.Override(Mathf.Lerp(startVignetteSmoothness, endVignetteSmoothness, eased));
        colorAdjustments.postExposure.Override(Mathf.Lerp(startPostExposure, endPostExposure, eased));
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isTransitioning = false;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void RestoreOriginalPostProcessingState()
    {
        if (cameraData != null)
        {
            cameraData.renderPostProcessing = originalPostProcessingState;
        }
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

            target.SetAlpha(Mathf.Lerp(target.OriginalAlpha, 0f, eased));
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
