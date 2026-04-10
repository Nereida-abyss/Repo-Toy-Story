using UnityEngine;

[CreateAssetMenu(fileName = "DefaultSceneTransitionProfile", menuName = "FX/Scene Transition Profile")]
public class SceneTransitionProfile : ScriptableObject
{
    [SerializeField] [Min(0.01f)] private float fadeDuration = 1.1f;
    [SerializeField] [Range(0f, 1f)] private float startVignetteIntensity = 0f;
    [SerializeField] [Range(0f, 1f)] private float endVignetteIntensity = 1f;
    [SerializeField] private float startPostExposure = 0f;
    [SerializeField] private float endPostExposure = -10f;
    [SerializeField] [Range(0f, 1f)] private float startVignetteSmoothness = 0.2f;
    [SerializeField] [Range(0f, 1f)] private float endVignetteSmoothness = 1f;

    public float FadeDuration => fadeDuration;
    public float StartVignetteIntensity => startVignetteIntensity;
    public float EndVignetteIntensity => endVignetteIntensity;
    public float StartPostExposure => startPostExposure;
    public float EndPostExposure => endPostExposure;
    public float StartVignetteSmoothness => startVignetteSmoothness;
    public float EndVignetteSmoothness => endVignetteSmoothness;
}
