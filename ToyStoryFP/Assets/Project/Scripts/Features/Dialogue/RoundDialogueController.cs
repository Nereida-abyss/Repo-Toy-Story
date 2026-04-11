using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class RoundDialogueController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text npcNameText;
    public TMP_Text sentenceText;
    public GameObject dialoguePanel;

    [Header("Dialogue Settings")]
    public float typingSpeed = 0.05f;
    public float timeBetweenSentences = 1.5f;
    public bool pauseGameDuringDialogue = true;

    private RoundDialogueManager dialogueManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dialogueManager = RoundDialogueManager.Instance;

        if (dialogueManager == null)
        {
            Debug.LogError("RoundDialogueManager instance not found in the scene.");
            return;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    public IEnumerator ShowDialogueAndWait()
    {
        if (dialogueManager == null)
        {
            dialogueManager = RoundDialogueManager.Instance;
            if (dialogueManager == null)
            {
                Debug.LogError("Cannot show dialogue: RoundDialogueManager not found in the scene.");
                yield break;
            }
        }

        Dialogue dialogue = dialogueManager.GetDialogueForCurrentRound();
        if (dialogue == null)
        {
            Debug.LogWarning("No dialogue found for the current round.");
            yield break;
        }

        if (pauseGameDuringDialogue)
        {
            Time.timeScale = 0f;
        }

        dialoguePanel.SetActive(true);
        npcNameText.text = dialogue.npcName;

        for (int i = 0; i < dialogue.sentences.Length; i++)
        {
            yield return StartCoroutine(TypeSentence(dialogue.sentences[i]));

            if (i < dialogue.sentences.Length - 1)
            {
                yield return new WaitForSecondsRealtime(timeBetweenSentences);
            }
        }

        dialoguePanel.SetActive(false);
        if (pauseGameDuringDialogue)
        {
            Time.timeScale = 1f;
        }
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



    // Update is called once per frame
    void Update()
    {

    }
}