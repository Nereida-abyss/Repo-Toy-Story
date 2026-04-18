using UnityEngine;

[CreateAssetMenu(fileName = "SpawnTelegraphProfile", menuName = "Waves/Spawn Telegraph Profile")]
public class SpawnTelegraphProfile : ScriptableObject
{
    [Header("Telegraph")]
    [SerializeField] private GameObject telegraphVfxPrefab;
    [SerializeField] private AudioClip telegraphSfx;
    [SerializeField] [Range(0f, 1f)] private float telegraphVolume = 0.65f;
    [SerializeField] [Min(0.01f)] private float telegraphDuration = 0.25f;
    [SerializeField] [Min(0.01f)] private float telegraphLifetime = 1f;

    public GameObject TelegraphVfxPrefab => telegraphVfxPrefab;
    public AudioClip TelegraphSfx => telegraphSfx;
    public float TelegraphVolume => Mathf.Clamp01(telegraphVolume);
    public float TelegraphDuration => Mathf.Max(0.01f, telegraphDuration);
    public float TelegraphLifetime => Mathf.Max(0.01f, telegraphLifetime);
}
