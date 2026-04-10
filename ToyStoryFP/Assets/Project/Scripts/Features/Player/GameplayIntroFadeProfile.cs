using UnityEngine;

[CreateAssetMenu(fileName = "DefaultGameplayIntroFadeProfile", menuName = "FX/Gameplay Intro Fade Profile")]
public class GameplayIntroFadeProfile : ScriptableObject
{
    [SerializeField] [Min(0.01f)] private float fadeDuration = 1.35f;
    [SerializeField] [Range(0f, 1f)] private float startVignetteIntensity = 1f;
    [SerializeField] [Range(0f, 1f)] private float endVignetteIntensity = 0f;
    [SerializeField] private float startPostExposure = -10f;
    [SerializeField] private float endPostExposure = 0f;
    [SerializeField] [Range(0f, 1f)] private float startVignetteSmoothness = 1f;
    [SerializeField] [Range(0f, 1f)] private float endVignetteSmoothness = 0.2f;

    public float FadeDuration => fadeDuration;
    public float StartVignetteIntensity => startVignetteIntensity;
    public float EndVignetteIntensity => endVignetteIntensity;
    public float StartPostExposure => startPostExposure;
    public float EndPostExposure => endPostExposure;
    public float StartVignetteSmoothness => startVignetteSmoothness;
    public float EndVignetteSmoothness => endVignetteSmoothness;
}
