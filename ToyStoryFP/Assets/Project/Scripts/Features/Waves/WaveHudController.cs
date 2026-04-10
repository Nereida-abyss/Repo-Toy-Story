using UnityEngine;

[DisallowMultipleComponent]
public class WaveHudController : MonoBehaviour
{
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private WaveAnnouncementUI waveAnnouncementUi;
    [SerializeField] private WaveIntermissionUI waveIntermissionUi;
    [SerializeField] private WaveTimersUI waveTimersUi;

    private float remainingAnnouncementTime;
    private bool hasLoggedMissingReferences;

    void OnEnable()
    {
        Subscribe();
        SyncFromCurrentState();
    }

    void Start()
    {
        SyncFromCurrentState();
    }

    void Update()
    {
        if (waveManager == null)
        {
            LogMissingReferences();
            return;
        }

        UpdateAnnouncementTimer();
        waveTimersUi?.Refresh(waveManager.CurrentState, waveManager.RoundElapsedTime, waveManager.RemainingIntermissionTime);
    }

    void OnDisable()
    {
        Unsubscribe();
        HideTransientUi();
    }

    private void Subscribe()
    {
        if (waveManager == null)
        {
            return;
        }

        waveManager.WaveStarted -= HandleWaveStarted;
        waveManager.WaveStarted += HandleWaveStarted;
        waveManager.IntermissionStarted -= HandleIntermissionStarted;
        waveManager.IntermissionStarted += HandleIntermissionStarted;
    }

    private void Unsubscribe()
    {
        if (waveManager == null)
        {
            return;
        }

        waveManager.WaveStarted -= HandleWaveStarted;
        waveManager.IntermissionStarted -= HandleIntermissionStarted;
    }

    private void HandleWaveStarted(int waveNumber)
    {
        if (!HasUiReferences())
        {
            return;
        }

        waveIntermissionUi.HidePrompt();
        waveAnnouncementUi.ShowWave(waveNumber);
        remainingAnnouncementTime = waveManager != null ? waveManager.WaveAnnouncementDuration : 0f;
    }

    private void HandleIntermissionStarted()
    {
        if (!HasUiReferences())
        {
            return;
        }

        remainingAnnouncementTime = 0f;
        waveAnnouncementUi.HideWave();
        waveIntermissionUi.ShowPrompt();
    }

    private void UpdateAnnouncementTimer()
    {
        if (remainingAnnouncementTime <= 0f || UIManager.IsGamePaused)
        {
            return;
        }

        remainingAnnouncementTime -= Time.deltaTime;

        if (remainingAnnouncementTime <= 0f)
        {
            waveAnnouncementUi?.HideWave();
        }
    }

    private void SyncFromCurrentState()
    {
        if (waveManager == null)
        {
            LogMissingReferences();
            return;
        }

        HideTransientUi();
        waveTimersUi?.Refresh(waveManager.CurrentState, waveManager.RoundElapsedTime, waveManager.RemainingIntermissionTime);

        if (waveManager.CurrentState == WaveManager.WaveRuntimeState.Intermission)
        {
            waveIntermissionUi?.ShowPrompt();
        }
    }

    private void HideTransientUi()
    {
        remainingAnnouncementTime = 0f;
        waveAnnouncementUi?.HideWave();
        waveIntermissionUi?.HidePrompt();
        waveTimersUi?.Refresh(WaveManager.WaveRuntimeState.InitialDelay, 0f, 0f);
    }

    private bool HasUiReferences()
    {
        if (waveAnnouncementUi != null && waveIntermissionUi != null && waveTimersUi != null)
        {
            return true;
        }

        LogMissingReferences();
        return false;
    }

    private void LogMissingReferences()
    {
        if (hasLoggedMissingReferences)
        {
            return;
        }

        hasLoggedMissingReferences = true;
        GameDebug.Advertencia("HUDOleadas", "WaveHudController necesita WaveManager y las tres vistas de oleadas asignadas en inspector.", this);
    }
}
