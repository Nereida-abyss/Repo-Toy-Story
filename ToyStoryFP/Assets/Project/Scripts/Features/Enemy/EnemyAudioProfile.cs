using UnityEngine;

[CreateAssetMenu(fileName = "DefaultEnemyAudioProfile", menuName = "Enemy/Enemy Audio Profile")]
public class EnemyAudioProfile : ScriptableObject
{
    [SerializeField] private AudioClip alertClip;
    [SerializeField] [Range(0f, 1f)] private float alertVolume = 0.08f;
    [SerializeField] private float minDistance = 1.2f;
    [SerializeField] private float maxDistance = 16f;
    [SerializeField] private float firePitchRandomness = 0.02f;

    public AudioClip AlertClip => alertClip;
    public float AlertVolume => alertVolume;
    public float MinDistance => minDistance;
    public float MaxDistance => maxDistance;
    public float FirePitchRandomness => firePitchRandomness;
}
