using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class WaveAnnouncementUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text announcementText;

    private bool hasLoggedMissingReferences;

    void Awake()
    {
        ResolveReferences();
    }

    void OnValidate()
    {
        ResolveReferences();
    }

    public void ShowWave(int waveNumber)
    {
        ResolveReferences();

        if (panelRoot == null || announcementText == null)
        {
            LogMissingReferences();
            return;
        }

        announcementText.text = $"WAVE {waveNumber}";
        SetVisible(true);
    }

    public void HideWave()
    {
        SetVisible(false);
    }

    private void ResolveReferences()
    {
        panelRoot ??= gameObject;

        if (announcementText == null)
        {
            announcementText = FindTextByExactName("WaveAnnouncementText");
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
        Debug.LogWarning("WaveAnnouncementUI is missing its panel or text reference.", this);
    }
}
