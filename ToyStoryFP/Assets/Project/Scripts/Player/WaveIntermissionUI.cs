using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class WaveIntermissionUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private string promptText = "If you want to start the next round, press T";
    [SerializeField] private TMP_Text promptLabel;

    private bool hasLoggedMissingReferences;

    void Awake()
    {
        ResolveReferences();
    }

    void OnValidate()
    {
        ResolveReferences();
    }

    // Muestra prompt.
    public void ShowPrompt()
    {
        ResolveReferences();

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

    // Resuelve referencias.
    private void ResolveReferences()
    {
        panelRoot ??= gameObject;

        if (promptLabel == null)
        {
            promptLabel = FindTextByExactName("NextWavePromptText");
        }
    }

    // Actualiza visible.
    private void SetVisible(bool isVisible)
    {
        ResolveReferences();

        if (panelRoot == null)
        {
            return;
        }

        UIFxUtility.SetPanelActive(panelRoot, isVisible);
    }

    // Busca texto por exact nombre.
    private TMP_Text FindTextByExactName(string targetName)
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].gameObject.name == targetName)
            {
                return texts[i];
            }
        }

        return null;
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
