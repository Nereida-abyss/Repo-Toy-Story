using UnityEngine;

public class RoundDialogueManager : MonoBehaviour
{
    public static RoundDialogueManager Instance { get; private set; }

    [Header("Dialogue Catalog")]
    [SerializeField] private WaveDialogueCatalog dialogueCatalog;

    private int currentRound = 0;
    private bool hasLoggedMissingCatalog;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Dialogue GetDialogueForCurrentRound()
    {
        if (dialogueCatalog == null)
        {
            WarnIfMissingCatalog();
            return null;
        }

        hasLoggedMissingCatalog = false;
        Dialogue roundDialogue = dialogueCatalog.GetRoundDialogue(currentRound);
        return roundDialogue != null ? roundDialogue : dialogueCatalog.CreateGeneratedDialogue(currentRound);
    }

    public void AdvanceToNextRound()
    {
        currentRound++;
        Debug.Log($"Round {currentRound} - Next dialogue ready");
    }

    public int GetCurrentRound()
    {
        return currentRound;
    }

    private void WarnIfMissingCatalog()
    {
        if (hasLoggedMissingCatalog)
        {
            return;
        }

        hasLoggedMissingCatalog = true;
        GameDebug.Advertencia("Dialogo", "RoundDialogueManager necesita WaveDialogueCatalog asignado para generar los dialogos de ronda.", this);
    }
}
