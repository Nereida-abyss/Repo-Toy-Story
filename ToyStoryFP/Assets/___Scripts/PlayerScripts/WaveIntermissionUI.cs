using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WaveIntermissionUI : MonoBehaviour
{
    [SerializeField] private string promptText = "If you want to start the next round, press T";
    [SerializeField] private TMP_Text promptLabel;

    private Graphic[] graphics;
    private bool hasLoggedMissingReferences;

    void Awake()
    {
        ResolveReferences();
        HidePrompt();
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
        if (promptLabel == null)
        {
            promptLabel = FindTextByExactName("NextWavePromptText");
        }

        if (graphics == null || graphics.Length == 0)
        {
            graphics = GetComponentsInChildren<Graphic>(true);
        }
    }

    private void SetVisible(bool isVisible)
    {
        if (graphics == null || graphics.Length == 0)
        {
            return;
        }

        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
            {
                graphics[i].enabled = isVisible;
            }
        }
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
