using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealthScript))]
public class PlayerDeathFlow : MonoBehaviour
{
    [SerializeField] private PlayerHealthScript playerHealth;
    [SerializeField] private PlayerAudioController playerAudio;
    [SerializeField] private float destroyDelay = 2f;
    [SerializeField] private float endMenuLoadDelay = 0.2f;

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
        Destroy(gameObject, Mathf.Max(0f, destroyDelay));
        StartCoroutine(LoadEndMenuAfterDeathAudio());
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

    private System.Collections.IEnumerator LoadEndMenuAfterDeathAudio()
    {
        ResolveAudioController();
        playerAudio?.PlayDeath();

        if (endMenuLoadDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(endMenuLoadDelay);
        }

        SceneFlow.LoadEndMenu();
    }

    private void ResolveAudioController()
    {
        playerAudio ??= GetComponent<PlayerAudioController>();
    }
}
