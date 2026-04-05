using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class WaveTimersUI : MonoBehaviour
{
    [SerializeField] private string roundPrefix = "Round Time";
    [SerializeField] private string intermissionPrefix = "Intermission";

    private TMP_Text roundTimerText;
    private TMP_Text intermissionTimerText;

    void Awake()
    {
        ResolveReferences();
        HideAll();
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
            Debug.LogWarning("WaveTimersUI is missing one or more timer text references.", this);
            return;
        }

        switch (state)
        {
            case WaveManager.WaveRuntimeState.WaveInProgress:
                roundTimerText.text = $"{roundPrefix} {FormatElapsedTime(roundElapsedTime)}";
                roundTimerText.enabled = true;
                intermissionTimerText.enabled = false;
                break;
            case WaveManager.WaveRuntimeState.Intermission:
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
        if (roundTimerText != null && intermissionTimerText != null)
        {
            return;
        }

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
            {
                continue;
            }

            if (roundTimerText == null && texts[i].gameObject.name.Contains("RoundTimer"))
            {
                roundTimerText = texts[i];
            }
            else if (intermissionTimerText == null && texts[i].gameObject.name.Contains("IntermissionTimer"))
            {
                intermissionTimerText = texts[i];
            }
        }
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
}
