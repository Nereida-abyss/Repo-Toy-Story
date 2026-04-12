using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "DefaultUiAudioProfile", menuName = "UI/UI Audio Profile")]
public class UiAudioProfile : ScriptableObject, ISerializationCallbackReceiver
{
    [SerializeField] private ConfigurableAudioClip clickAudio = new ConfigurableAudioClip();
    [SerializeField] private ConfigurableAudioClip hoverAudio = new ConfigurableAudioClip();
    [SerializeField] private ConfigurableAudioClip panelOpenAudio = new ConfigurableAudioClip();
    [SerializeField] private ConfigurableAudioClip panelCloseAudio = new ConfigurableAudioClip();

    [FormerlySerializedAs("clickClip")] [SerializeField, HideInInspector] private AudioClip legacyClickClip;
    [FormerlySerializedAs("hoverClip")] [SerializeField, HideInInspector] private AudioClip legacyHoverClip;
    [FormerlySerializedAs("panelOpenClip")] [SerializeField, HideInInspector] private AudioClip legacyPanelOpenClip;
    [FormerlySerializedAs("panelCloseClip")] [SerializeField, HideInInspector] private AudioClip legacyPanelCloseClip;

    public ConfigurableAudioClip ClickAudio => clickAudio;
    public ConfigurableAudioClip HoverAudio => hoverAudio;
    public ConfigurableAudioClip PanelOpenAudio => panelOpenAudio;
    public ConfigurableAudioClip PanelCloseAudio => panelCloseAudio;

    public AudioClip ClickClip => clickAudio != null ? clickAudio.Clip : null;
    public float ClickVolume => clickAudio != null ? clickAudio.Volume : 1f;
    public AudioClip HoverClip => hoverAudio != null ? hoverAudio.Clip : null;
    public float HoverVolume => hoverAudio != null ? hoverAudio.Volume : 1f;
    public AudioClip PanelOpenClip => panelOpenAudio != null ? panelOpenAudio.Clip : null;
    public float PanelOpenVolume => panelOpenAudio != null ? panelOpenAudio.Volume : 1f;
    public AudioClip PanelCloseClip => panelCloseAudio != null ? panelCloseAudio.Clip : null;
    public float PanelCloseVolume => panelCloseAudio != null ? panelCloseAudio.Volume : 1f;

    public void OnAfterDeserialize()
    {
        MigrateLegacyData();
    }

    public void OnBeforeSerialize()
    {
        MigrateLegacyData();
    }

    private void OnValidate()
    {
        MigrateLegacyData();
    }

    private void MigrateLegacyData()
    {
        clickAudio ??= new ConfigurableAudioClip();
        hoverAudio ??= new ConfigurableAudioClip();
        panelOpenAudio ??= new ConfigurableAudioClip();
        panelCloseAudio ??= new ConfigurableAudioClip();

        clickAudio.ApplyLegacyClip(legacyClickClip);
        hoverAudio.ApplyLegacyClip(legacyHoverClip);
        panelOpenAudio.ApplyLegacyClip(legacyPanelOpenClip);
        panelCloseAudio.ApplyLegacyClip(legacyPanelCloseClip);
    }
}
