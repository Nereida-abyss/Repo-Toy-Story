using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class WaveIntermissionUI : MonoBehaviour
{
    private const string DefaultStartPrompt = "If you want to start the next round, press Q";

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private string promptText = DefaultStartPrompt;
    [SerializeField] private TMP_Text promptLabel;

    private bool hasLoggedMissingReferences;

    void Awake()
    {
        panelRoot ??= gameObject;
    }

    void OnValidate()
    {
        panelRoot ??= gameObject;
    }

    // Muestra prompt.
    public void ShowPrompt()
    {
        if (promptLabel == null)
        {
            LogMissingReferences();
            return;
        }

        promptLabel.text = promptText;
        SetVisible(true);
    }

    // Oculta prompt.
    public void HidePrompt()
    {
        SetVisible(false);
    }

    // Actualiza visible.
    private void SetVisible(bool isVisible)
    {
        if (panelRoot == null)
        {
            return;
        }

        UIFxUtility.SetPanelActive(panelRoot, isVisible);
    }

    // Gestiona registro faltante referencias.
    private void LogMissingReferences()
    {
        if (hasLoggedMissingReferences)
        {
            return;
        }

        hasLoggedMissingReferences = true;
        GameDebug.Advertencia("HUDOleadas", "WaveIntermissionUI no tiene texto asignado.", this);
    }
}
