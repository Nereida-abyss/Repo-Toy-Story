using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class ShopPromptUI : MonoBehaviour
{
    private const string DefaultPrompt = "Para abrir la tienda pulsa T";

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text promptLabel;
    [SerializeField] private string promptText = DefaultPrompt;
    [SerializeField] private bool mutePanelAudio = true;
    [SerializeField] private bool forceLocalPanelAudioSource;
    [SerializeField] private AudioSource localPanelAudioSource;

    private bool hasLoggedMissingReferences;
    private bool isPromptVisibleRequested;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        panelRoot ??= gameObject;
        ApplyPanelAudioConfiguration();
        isPromptVisibleRequested = panelRoot != null && panelRoot.activeSelf;
    }

    private void OnValidate()
    {
        panelRoot ??= gameObject;
        ApplyPanelAudioConfiguration();
    }

    public void ShowPrompt()
    {
        if (isPromptVisibleRequested)
        {
            return;
        }

        if (promptLabel == null)
        {
            LogMissingReferences();
            return;
        }

        promptLabel.text = promptText;
        ApplyPanelAudioConfiguration();
        SetVisible(true);
        isPromptVisibleRequested = true;
    }

    public void HidePrompt()
    {
        if (!isPromptVisibleRequested)
        {
            return;
        }

        ApplyPanelAudioConfiguration();
        SetVisible(false);
        isPromptVisibleRequested = false;
    }

    private void SetVisible(bool isVisible)
    {
        if (panelRoot == null)
        {
            return;
        }

        UIFxUtility.SetPanelActive(panelRoot, isVisible);
    }

    private void LogMissingReferences()
    {
        if (hasLoggedMissingReferences)
        {
            return;
        }

        hasLoggedMissingReferences = true;
        GameDebug.Advertencia("Shop", "ShopPromptUI necesita promptLabel asignado.", this);
    }

    private void ApplyPanelAudioConfiguration()
    {
        if (panelRoot == null)
        {
            return;
        }

        UIPanelFx panelFx = panelRoot.GetComponent<UIPanelFx>();

        if (panelFx == null)
        {
            return;
        }

        bool? overrideAudioEnabled = mutePanelAudio ? false : (bool?)null;
        bool? overrideUseSharedAudioSource = forceLocalPanelAudioSource ? false : (bool?)null;
        AudioSource overrideAudioSource = forceLocalPanelAudioSource ? localPanelAudioSource : null;
        panelFx.ApplyDebugAudioOverrides(overrideAudioEnabled, overrideUseSharedAudioSource, overrideAudioSource);
    }
}
