using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class RoundDialogueController : MonoBehaviour
{
    private const string DialogueInputGateOwner = "RoundDialogueController.Dialogue";

    [Header("UI References")]
    public TMP_Text npcNameText;
    public TMP_Text sentenceText;
    public GameObject dialoguePanel;

    [Header("Dialogue Settings")]
    public float typingSpeed = 0.05f;
    public bool pauseGameDuringDialogue = true;

    [Header("Dependencies")]
    [SerializeField] private RoundDialogueManager dialogueManager;
    private bool hasLoggedMissingManager;
    private bool hasAcquiredInputGate;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ResolveDialogueManager();

        SetDialoguePanelVisible(false);
    }

    void OnDisable()
    {
        EndDialogueFlow();
    }

    void OnDestroy()
    {
        EndDialogueFlow();
    }

    public IEnumerator ShowDialogueAndWait()
    {
        if (!ResolveDialogueManager())
        {
            yield break;
        }

        if (!HasRequiredUiReferences())
        {
            GameDebug.Error("Dialogo", "RoundDialogueController necesita panel y textos asignados para mostrar dialogos.", this);
            yield break;
        }

        Dialogue dialogue = dialogueManager.GetDialogueForCurrentRound();
        if (dialogue == null)
        {
            Debug.LogWarning("No dialogue found for the current round.");
            yield break;
        }

        BeginDialogueFlow();

        try
        {
            SetDialoguePanelVisible(true);
            npcNameText.text = dialogue.npcName;

            for (int i = 0; i < dialogue.sentences.Length; i++)
            {
                yield return StartCoroutine(TypeSentence(dialogue.sentences[i]));

                if (i < dialogue.sentences.Length - 1)
                {
                    yield return new WaitForSecondsRealtime(GetSentencePauseDuration());
                }
            }

            yield return WaitForDialogueAdvanceInput();
        }
        finally
        {
            EndDialogueFlow();
        }
    }

    private void BeginDialogueFlow()
    {
        AcquireInputGateIfNeeded();
        PauseDialogueFlow();
    }

    private void EndDialogueFlow()
    {
        SetDialoguePanelVisible(false);
        ReleaseInputGateIfNeeded();
        ResumeDialogueFlow();
    }

    private void AcquireInputGateIfNeeded()
    {
        if (hasAcquiredInputGate)
        {
            return;
        }

        GameplayInputGate.Acquire(DialogueInputGateOwner);
        hasAcquiredInputGate = true;
    }

    private void ReleaseInputGateIfNeeded()
    {
        if (!hasAcquiredInputGate)
        {
            return;
        }

        GameplayInputGate.Release(DialogueInputGateOwner);
        hasAcquiredInputGate = false;
    }

    private IEnumerator TypeSentence(string sentence)
    {
        sentenceText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            sentenceText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
    }

    private IEnumerator WaitForDialogueAdvanceInput()
    {
        while (!ProjectInput.WasDialogueAdvancePressed())
        {
            yield return null;
        }

        ProjectInput.ConsumePrimaryFireUntilRelease();
    }

    private bool HasRequiredUiReferences()
    {
        return npcNameText != null && sentenceText != null && dialoguePanel != null;
    }

    private bool ResolveDialogueManager()
    {
        if (dialogueManager != null)
        {
            hasLoggedMissingManager = false;
            return true;
        }

        dialogueManager = RoundDialogueManager.Instance;

        if (dialogueManager != null)
        {
            hasLoggedMissingManager = false;
            return true;
        }

        if (!hasLoggedMissingManager)
        {
            hasLoggedMissingManager = true;
            GameDebug.Error("Dialogo", "RoundDialogueController necesita una referencia a RoundDialogueManager.", this);
        }

        return false;
    }

    private float GetSentencePauseDuration()
    {
        return ResolveDialogueManager() ? dialogueManager.GetSentencePauseDuration() : 1.5f;
    }

    private void SetDialoguePanelVisible(bool isVisible)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(isVisible);
        }
    }

    private void PauseDialogueFlow()
    {
        if (pauseGameDuringDialogue)
        {
            Time.timeScale = 0f;
        }
    }

    private void ResumeDialogueFlow()
    {
        if (pauseGameDuringDialogue)
        {
            Time.timeScale = 1f;
        }
    }
}
