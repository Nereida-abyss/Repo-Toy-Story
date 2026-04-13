using UnityEngine;

[CreateAssetMenu(fileName = "DefaultPlayerHudProfile", menuName = "UI/Player HUD Profile")]
public class PlayerHudProfile : ScriptableObject
{
    [Header("Health UI")]
    [SerializeField] [Min(0f)] private float healthAnimationSpeed = 2.5f;

    [Header("Damage Feedback")]
    [SerializeField] private Color damageFlashColor = new Color(0.95f, 0.08f, 0.08f, 0.38f);
    [SerializeField] [Min(0f)] private float damageFlashFadeIn = 0.04f;
    [SerializeField] [Min(0f)] private float damageFlashHold = 0.05f;
    [SerializeField] [Min(0.001f)] private float damageFlashFadeOut = 0.2f;
    [SerializeField] [Min(0f)] private float damageFeedbackMinInterval = 0.04f;
    [SerializeField] [Min(1f)] private float healthPulseScale = 1.12f;
    [SerializeField] [Min(0.01f)] private float healthPulseDuration = 0.2f;

    public float HealthAnimationSpeed => healthAnimationSpeed;
    public Color DamageFlashColor => damageFlashColor;
    public float DamageFlashFadeIn => damageFlashFadeIn;
    public float DamageFlashHold => damageFlashHold;
    public float DamageFlashFadeOut => damageFlashFadeOut;
    public float DamageFeedbackMinInterval => damageFeedbackMinInterval;
    public float HealthPulseScale => healthPulseScale;
    public float HealthPulseDuration => healthPulseDuration;
}
