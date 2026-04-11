using UnityEngine;

[CreateAssetMenu(fileName = "DefaultUiAudioProfile", menuName = "UI/UI Audio Profile")]
public class UiAudioProfile : ScriptableObject
{
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip panelOpenClip;
    [SerializeField] private AudioClip panelCloseClip;

    public AudioClip ClickClip => clickClip;
    public AudioClip HoverClip => hoverClip;
    public AudioClip PanelOpenClip => panelOpenClip;
    public AudioClip PanelCloseClip => panelCloseClip;
}
