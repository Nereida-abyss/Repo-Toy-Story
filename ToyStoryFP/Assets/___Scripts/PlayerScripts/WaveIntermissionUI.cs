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

    public void HidePrompt()
    {
        SetVisible(false);
    }

    private void ResolveReferences()
    {
        panelRoot ??= gameObject;

        if (promptLabel == null)
        {
            promptLabel = FindTextByExactName("NextWavePromptText");
        }
    }

    private void SetVisible(bool isVisible)
    {
        ResolveReferences();

        if (panelRoot == null)
        {
            return;
        }

        UIFxUtility.SetPanelActive(panelRoot, isVisible);
    }

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

    private void LogMissingReferences()
    {
        if (hasLoggedMissingReferences)
        {
            return;
        }

        hasLoggedMissingReferences = true;
        Debug.LogWarning("WaveIntermissionUI is missing its text reference.", this);
    }
}
