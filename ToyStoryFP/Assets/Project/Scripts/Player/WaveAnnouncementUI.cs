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

    // Muestra oleada.
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

    // Oculta oleada.
    public void HideWave()
    {
        SetVisible(false);
    }

    // Resuelve referencias.
    private void ResolveReferences()
    {
        panelRoot ??= gameObject;

        if (announcementText == null)
        {
            announcementText = FindTextByExactName("WaveAnnouncementText");
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
        GameDebug.Advertencia("HUDOleadas", "WaveAnnouncementUI no tiene panel o texto asignado.", this);
    }
}
