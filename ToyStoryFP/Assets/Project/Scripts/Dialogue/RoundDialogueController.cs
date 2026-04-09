using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class RoundDialogueController : MonoBehaviour
{
    [Header("UI References")]
    public Text npcNameText;
    public Text sentenceText;
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

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
