using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class WaveIntermissionUI : MonoBehaviour
{
    private const string LegacyStartPrompt = "If you want to start the next round, press T";
    private const string UpdatedStartPrompt = "If you want to start the next round, press Q";

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private string promptText = UpdatedStartPrompt;
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

        UpgradeLegacyPromptIfNeeded();
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
        UpgradeLegacyPromptIfNeeded();

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

    // Reemplaza el texto heredado de la T por el nuevo atajo con Q.
    private void UpgradeLegacyPromptIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(promptText) || promptText == LegacyStartPrompt)
        {
            promptText = UpdatedStartPrompt;
        }
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
