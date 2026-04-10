using UnityEngine;

[CreateAssetMenu(fileName = "DefaultWaveBalanceProfile", menuName = "Waves/Wave Balance Profile")]
public class WaveBalanceProfile : ScriptableObject
{
    [Header("Wave Setup")]
    [SerializeField] private float initialWaveDelay = 2f;
    [SerializeField] private float intermissionDuration = 180f;
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private float waveAnnouncementDuration = 2.5f;
    [SerializeField] private int baseEnemyCount = 10;
    [SerializeField] private int additionalEnemiesPerWave = 2;

    [Header("Enemy Round Scaling")]
    [SerializeField] private float healthMultiplierPerRound = 1.03f;
    [SerializeField] private float damageMultiplierPerRound = 1.02f;
    [SerializeField] private float maxHealthMultiplier = 1.75f;
    [SerializeField] private float maxDamageMultiplier = 1.35f;

    public float InitialWaveDelay => initialWaveDelay;
    public float IntermissionDuration => intermissionDuration;
    public float SpawnInterval => spawnInterval;
    public float WaveAnnouncementDuration => waveAnnouncementDuration;
    public int BaseEnemyCount => baseEnemyCount;
    public int AdditionalEnemiesPerWave => additionalEnemiesPerWave;
    public float HealthMultiplierPerRound => healthMultiplierPerRound;
    public float DamageMultiplierPerRound => damageMultiplierPerRound;
    public float MaxHealthMultiplier => maxHealthMultiplier;
    public float MaxDamageMultiplier => maxDamageMultiplier;
}
