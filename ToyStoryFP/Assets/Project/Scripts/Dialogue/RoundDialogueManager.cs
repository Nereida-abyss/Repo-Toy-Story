using UnityEngine;
using System.Collections.Generic;

public class RoundDialogueManager : MonoBehaviour
{
    public static RoundDialogueManager Instance;

    [Header("Custom Dialogues (lo programo pero igual ni se usa me la pela la verdad")]
    public List<Dialogue> customDialogues;

    [Header("Auto Dialogues")]
    public string npcName = "Buzz Lightyear";
    public bool useRandomSentences = true;
    public bool repeatSentences = false; //si se cambia a true salen los custom dialogues, asi que se deja en false de momento

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
        //esto por si quereis los custom
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

        //esto para los auto generados que son los que en un principio se van a usar
        return GenerateAutoDialogue();
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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}