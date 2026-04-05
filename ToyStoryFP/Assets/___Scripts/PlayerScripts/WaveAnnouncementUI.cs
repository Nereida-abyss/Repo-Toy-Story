using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WaveAnnouncementUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text announcementText;

    private Graphic[] graphics;
    private bool hasLoggedMissingReferences;

    void Awake()
    {
        ResolveReferences();
        HideWave();
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

        announcementText.text = $"OLEADA {waveNumber}";
        SetVisible(true);
    }

    public void HideWave()
    {
        SetVisible(false);
    }

    private void ResolveReferences()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (announcementText == null)
        {
            announcementText = FindTextByExactName("WaveAnnouncementText");
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
        Debug.LogWarning("WaveAnnouncementUI is missing its panel or text reference.", this);
    }
}
