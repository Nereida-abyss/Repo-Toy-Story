using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealthScript))]
public class PlayerDeathFlow : MonoBehaviour
{
    private const string EndMenuSceneName = "EndMenu";

    [SerializeField] private PlayerHealthScript playerHealth;
    [SerializeField] private float destroyDelay = 2f;

    private bool hasHandledDeath;
    private bool hasLoggedMissingHealth;

    void OnEnable()
    {
        hasHandledDeath = false;

        if (playerHealth == null)
        {
            LogMissingHealth();
            return;
        }

        playerHealth.Died -= HandlePlayerDied;
        playerHealth.Died += HandlePlayerDied;
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= HandlePlayerDied;
        }
    }

    private void HandlePlayerDied(PlayerHealthScript deadHealth)
    {
        if (hasHandledDeath || deadHealth != playerHealth)
        {
            return;
        }

        hasHandledDeath = true;
        RunStatsStore.CommitLastRun();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        CambioEscena.LoadSceneSafely(EndMenuSceneName);
        Destroy(gameObject, Mathf.Max(0f, destroyDelay));
    }

    private void LogMissingHealth()
    {
        if (hasLoggedMissingHealth)
        {
            return;
        }

        hasLoggedMissingHealth = true;
        GameDebug.Error("Jugador", "PlayerDeathFlow necesita PlayerHealthScript asignado en inspector.", this);
    }
}
