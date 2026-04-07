using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class WaveTimersUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private string roundPrefix = "Round Time";
    [SerializeField] private string intermissionPrefix = "Intermission";
    [SerializeField] private TMP_Text roundTimerText;
    [SerializeField] private TMP_Text intermissionTimerText;

    private bool hasLoggedMissingReferences;

    void Awake()
    {
        ResolveReferences();
    }

    void OnValidate()
    {
        ResolveReferences();
    }

    public void Refresh(WaveManager.WaveRuntimeState state, float roundElapsedTime, float remainingIntermissionTime)
    {
        ResolveReferences();

        if (roundTimerText == null || intermissionTimerText == null)
        {
            LogMissingReferences();
            return;
        }

        switch (state)
        {
            case WaveManager.WaveRuntimeState.WaveInProgress:
                UIFxUtility.SetPanelActive(panelRoot, true);
                roundTimerText.text = $"{roundPrefix} {FormatElapsedTime(roundElapsedTime)}";
                roundTimerText.enabled = true;
                intermissionTimerText.enabled = false;
                break;
            case WaveManager.WaveRuntimeState.Intermission:
                UIFxUtility.SetPanelActive(panelRoot, true);
                intermissionTimerText.text = $"{intermissionPrefix} {FormatRemainingTime(remainingIntermissionTime)}";
                roundTimerText.enabled = false;
                intermissionTimerText.enabled = true;
                break;
            default:
                HideAll();
                break;
        }
    }

    private void HideAll()
    {
        if (panelRoot != null)
        {
            UIFxUtility.SetPanelActive(panelRoot, false);
        }

        if (roundTimerText != null)
        {
            roundTimerText.enabled = false;
        }

        if (intermissionTimerText != null)
        {
            intermissionTimerText.enabled = false;
        }
    }

    private void ResolveReferences()
    {
        panelRoot ??= gameObject;

        if (roundTimerText != null && intermissionTimerText != null)
        {
            return;
        }

        roundTimerText ??= FindTextByExactName("RoundTimerText");
        intermissionTimerText ??= FindTextByExactName("IntermissionTimerText");
    }

    private string FormatElapsedTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return $"{minutes:00}:{remainingSeconds:00}";
    }

    private string FormatRemainingTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return $"{minutes:00}:{remainingSeconds:00}";
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
        GameDebug.Advertencia("HUDOleadas", "WaveTimersUI no tiene una o mas referencias de textos de temporizador.", this);
    }
}
