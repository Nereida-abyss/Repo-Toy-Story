using UnityEngine;

[CreateAssetMenu(fileName = "DefaultSettingsDefaultsProfile", menuName = "UI/Settings Defaults Profile")]
public class SettingsDefaultsProfile : ScriptableObject
{
    [SerializeField] [Range(0f, 1f)] private float defaultVolume = 1f;
    [SerializeField] private float defaultLookSensitivity = 2f;
    [SerializeField] private float minLookSensitivity = 0.5f;
    [SerializeField] private float maxLookSensitivity = 5f;
    [SerializeField] private int defaultWindowedWidth = 1024;
    [SerializeField] private int defaultWindowedHeight = 768;
    [SerializeField] private int minimumWindowedDimension = 320;

    public float DefaultVolume => defaultVolume;
    public float DefaultLookSensitivity => defaultLookSensitivity;
    public float MinLookSensitivity => minLookSensitivity;
    public float MaxLookSensitivity => maxLookSensitivity;
    public int DefaultWindowedWidth => defaultWindowedWidth;
    public int DefaultWindowedHeight => defaultWindowedHeight;
    public int MinimumWindowedDimension => minimumWindowedDimension;
}
