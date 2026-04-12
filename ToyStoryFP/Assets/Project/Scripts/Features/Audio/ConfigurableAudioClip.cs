using System;
using UnityEngine;

[Serializable]
public sealed class ConfigurableAudioClip
{
    [SerializeField] private AudioClip clip;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;

    public AudioClip Clip => clip;
    public float Volume => Mathf.Clamp01(volume);
    public bool HasClip => clip != null;

    public void ApplyLegacyClip(AudioClip legacyClip)
    {
        if (clip == null && legacyClip != null)
        {
            clip = legacyClip;
        }
    }
}
