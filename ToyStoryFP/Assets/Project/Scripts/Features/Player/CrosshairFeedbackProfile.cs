using UnityEngine;

[CreateAssetMenu(fileName = "DefaultCrosshairFeedbackProfile", menuName = "UI/Crosshair Feedback Profile")]
public class CrosshairFeedbackProfile : ScriptableObject
{
    [Header("Hit Marker")]
    [SerializeField] private Color hitMarkerColor = Color.white;
    [SerializeField] [Min(0.01f)] private float hitMarkerDuration = 0.12f;
    [SerializeField] [Min(1f)] private float hitMarkerSize = 24f;
    [SerializeField] [Min(1f)] private float hitMarkerThickness = 3f;
    [SerializeField] [Min(1f)] private float hitMarkerScalePunch = 1.16f;

    [Header("Death Marker")]
    [SerializeField] private Color deathMarkerColor = new Color(1f, 0.3f, 0.12f, 1f);
    [SerializeField] [Min(0.01f)] private float deathMarkerDuration = 0.22f;
    [SerializeField] [Min(1f)] private float deathMarkerSize = 34f;
    [SerializeField] [Min(1f)] private float deathMarkerThickness = 4f;
    [SerializeField] [Min(1f)] private float deathMarkerScalePunch = 1.25f;

    public Color HitMarkerColor => hitMarkerColor;
    public float HitMarkerDuration => hitMarkerDuration;
    public float HitMarkerSize => hitMarkerSize;
    public float HitMarkerThickness => hitMarkerThickness;
    public float HitMarkerScalePunch => hitMarkerScalePunch;
    public Color DeathMarkerColor => deathMarkerColor;
    public float DeathMarkerDuration => deathMarkerDuration;
    public float DeathMarkerSize => deathMarkerSize;
    public float DeathMarkerThickness => deathMarkerThickness;
    public float DeathMarkerScalePunch => deathMarkerScalePunch;
}
