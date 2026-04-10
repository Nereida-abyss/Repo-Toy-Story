using UnityEngine;

[CreateAssetMenu(fileName = "DefaultUIPanelFxProfile", menuName = "FX/UI Panel FX Profile")]
public class UIPanelFxProfile : ScriptableObject
{
    [Header("Open/Close")]
    [SerializeField] private bool playOpenOnEnable = true;
    [SerializeField] private float openDuration = 0.26f;
    [SerializeField] private float closeDuration = 0.16f;
    [SerializeField] private float slideOffset = 22f;
    [SerializeField] private float startScale = 0.94f;
    [SerializeField] private float closeScale = 0.97f;

    [Header("Canvas Group")]
    [SerializeField] private bool disableRaycastWhileAnimating = true;

    [Header("Audio")]
    [SerializeField] private bool enableAudio = true;
    [SerializeField] private bool useSharedAudioSource = true;
    [SerializeField] private bool useAudioManagerFallback;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [SerializeField] private float openVolume = 0.5f;
    [SerializeField] private float closeVolume = 0.38f;

    public bool PlayOpenOnEnable => playOpenOnEnable;
    public float OpenDuration => openDuration;
    public float CloseDuration => closeDuration;
    public float SlideOffset => slideOffset;
    public float StartScale => startScale;
    public float CloseScale => closeScale;
    public bool DisableRaycastWhileAnimating => disableRaycastWhileAnimating;
    public bool EnableAudio => enableAudio;
    public bool UseSharedAudioSource => useSharedAudioSource;
    public bool UseAudioManagerFallback => useAudioManagerFallback;
    public AudioClip OpenClip => openClip;
    public AudioClip CloseClip => closeClip;
    public float OpenVolume => openVolume;
    public float CloseVolume => closeVolume;
}
