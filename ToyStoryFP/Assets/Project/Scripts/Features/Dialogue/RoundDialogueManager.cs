using UnityEngine;
using System.Collections.Generic;

public class RoundDialogueManager : MonoBehaviour
{
    public static RoundDialogueManager Instance;

    [Header("Dialogue Catalog")]
    [SerializeField] private WaveDialogueCatalog dialogueCatalog;

    [Header("Legacy Custom Dialogues")]
    public List<Dialogue> customDialogues = new List<Dialogue>();

    [Header("Legacy Auto Dialogues")]
    public string npcName = "Buzz Lightyear";
    public bool useRandomSentences = true;
    public bool repeatSentences = false;

    private int currentRound = 0;

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
        Dialogue catalogDialogue = GetCatalogDialogueForCurrentRound();
        if (catalogDialogue != null)
        {
            return catalogDialogue;
        }

        if (customDialogues != null && customDialogues.Count > 0)
        {
            if (repeatSentences)
            {
                int index = currentRound % customDialogues.Count;
                return customDialogues[index];
            }
            else if (currentRound < customDialogues.Count)
            {
                return customDialogues[currentRound];
            }

        }

        return GenerateAutoDialogue();
    }

    private Dialogue GetCatalogDialogueForCurrentRound()
    {
        if (dialogueCatalog == null)
        {
            return null;
        }

        Dialogue roundDialogue = dialogueCatalog.GetRoundDialogue(currentRound);
        return roundDialogue != null ? roundDialogue : dialogueCatalog.CreateGeneratedDialogue(currentRound);
    }

    private Dialogue GenerateAutoDialogue()
    {
        Dialogue dialogue = ScriptableObject.CreateInstance<Dialogue>();
        dialogue.npcName = npcName;

        int roundNumber = currentRound + 1;

        if (useRandomSentences)
        {
            dialogue.sentences = new string[]
            {
                $"ROUND {roundNumber}: {npcName} says: 'To infinity and beyond!'",
                GetRandomSentence(),
            };
        }
        else
        {
            dialogue.sentences = new string[]
            {
                $"ROUND {roundNumber}: {npcName} says: 'To infinity and beyond!'",
                "Get ready for the next wave, Cowboy!",
                "More tropes are coming! HAHA!",
                "This battle will last TO INFINITY AND BEYOND!"

            };
        }
        return dialogue;
    }

    private string GetRandomSentence()
    {
        string[] randomSentences = new string[]
        {
            "The battle is heating up!",
            "Can you handle the pressure?",
            "Don't let your guard down!",
            "This is just the beginning!",
            "Get ready for the next wave, Cowboy!",
            "More tropes are coming! HAHA!",
            "This battle will last TO INFINITY AND BEYOND!"
        };
        int index = Random.Range(0, randomSentences.Length);
        return randomSentences[index];
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
}
