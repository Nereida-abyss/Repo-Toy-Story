using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WaveIntermissionUI : MonoBehaviour
{
    [SerializeField] private string promptText = "If you want to start the next round, press T";

    private TMP_Text promptLabel;
    private Graphic[] graphics;

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
            Debug.LogWarning("WaveIntermissionUI is missing its text reference.", this);
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
            promptLabel = GetComponentInChildren<TMP_Text>(true);
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
}
