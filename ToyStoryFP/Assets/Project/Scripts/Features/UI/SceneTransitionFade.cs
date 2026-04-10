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
    [SerializeField] private SceneTransitionProfile sceneTransitionProfile;
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
    private bool hasLoggedMissingProfile;

    void Awake()
    {
        ApplyProfile();
    }

    // Punto de entrada para salir con fade y cargar una escena nueva sin cortes bruscos.
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
            return false;
        }

        return fade.BeginFade(sceneName);
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

    // Decide qué cámara debe recibir el efecto de fade.
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

    // Arranca la transición si no hay otra corriendo ya.
    private bool BeginFade(string sceneName)
    {
        if (fadeCoroutine != null)
        {
            return true;
        }

        if (!EnsurePostProcessingVolume())
        {
            return false;
        }

        CacheUiFadeTargets();
        ApplyFadeState(0f);
        ApplyUiFadeState(0f);
        isTransitioning = true;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        fadeCoroutine = StartCoroutine(FadeOutAndLoadSceneRoutine(sceneName));
        return true;
    }

    private void ApplyProfile()
    {
        if (sceneTransitionProfile == null)
        {
            WarnIfMissingProfile();
            return;
        }

        hasLoggedMissingProfile = false;
        fadeDuration = sceneTransitionProfile.FadeDuration;
        startVignetteIntensity = sceneTransitionProfile.StartVignetteIntensity;
        endVignetteIntensity = sceneTransitionProfile.EndVignetteIntensity;
        startPostExposure = sceneTransitionProfile.StartPostExposure;
        endPostExposure = sceneTransitionProfile.EndPostExposure;
        startVignetteSmoothness = sceneTransitionProfile.StartVignetteSmoothness;
        endVignetteSmoothness = sceneTransitionProfile.EndVignetteSmoothness;
    }

    private void WarnIfMissingProfile()
    {
        if (hasLoggedMissingProfile)
        {
            return;
        }

        hasLoggedMissingProfile = true;
        GameDebug.Advertencia("Escenas", "SceneTransitionFade no tiene SceneTransitionProfile asignado. Se usaran los valores locales del componente.", this);
    }

    // Secuencia completa del cambio de escena:
    // prepara objetivos, aplica fade, carga la escena y después restaura el estado.
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

    // Garantiza el volumen de postproceso que usa el fade antes de empezar a animar.
    private bool EnsurePostProcessingVolume()
    {
        cameraData = GetComponent<UniversalAdditionalCameraData>();

        if (cameraData != null)
        {
            originalPostProcessingState = cameraData.renderPostProcessing;
            cameraData.renderPostProcessing = true;
        }

        if (transitionVolume == null)
        {
            transitionVolume = GetComponent<Volume>();
        }

        if (transitionVolume == null)
        {
            GameDebug.Advertencia(
                "Escenas",
                "SceneTransitionFade necesita un Volume explicito en la camara para aplicar el fade.",
                this);
            return false;
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
        return true;
    }

    // Aplica el valor de fade a cámara y postproceso en un único punto.
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

    // Cuando la escena termina de cargar, restauramos el flag global y soltamos el evento.
    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isTransitioning = false;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // Devuelve el postproceso a su configuración original después del fade.
    private void RestoreOriginalPostProcessingState()
    {
        if (cameraData != null)
        {
            cameraData.renderPostProcessing = originalPostProcessingState;
        }
    }

    // Guarda las piezas de UI que también deben desvanecerse durante la transición.
    private void CacheUiFadeTargets()
    {
        uiFadeTargets = UIFadeUtility.ResolveActiveCanvasTargets(gameObject.scene);
    }

    // Aplica el mismo fade a la UI cacheada para que la escena se vaya de forma uniforme.
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

    // Devuelve la UI al alpha normal cuando la transición ha terminado.
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

    // Limpia referencias de fade UI que ya no existen o han cambiado de escena.
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
