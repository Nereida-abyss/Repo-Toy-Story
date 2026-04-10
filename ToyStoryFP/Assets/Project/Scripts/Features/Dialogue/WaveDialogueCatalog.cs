using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveDialogueCatalog", menuName = "Dialogue/Wave Dialogue Catalog")]
public class WaveDialogueCatalog : ScriptableObject
{
    private static readonly string[] DefaultReusableSentences =
    {
        "The battle is heating up!",
        "Can you handle the pressure?",
        "Don't let your guard down!",
        "This is just the beginning!",
        "Get ready for the next wave, Cowboy!",
        "More tropes are coming! HAHA!",
        "This battle will last TO INFINITY AND BEYOND!"
    };

    private static readonly string[] DefaultOrderedSentences =
    {
        "Get ready for the next wave, Cowboy!",
        "More tropes are coming! HAHA!",
        "This battle will last TO INFINITY AND BEYOND!"
    };

    [Header("Round Sequences")]
    [SerializeField] private Dialogue[] roundDialogues = System.Array.Empty<Dialogue>();
    [SerializeField] private bool repeatRoundDialogues;

    [Header("Generated Dialogue")]
    [SerializeField] private string npcName = "Buzz Lightyear";
    [SerializeField] private bool useRandomGeneratedSentences = true;
    [SerializeField] private string introTemplate = "ROUND {round}: {npc} says: 'To infinity and beyond!'";
    [SerializeField, TextArea(2, 4)] private string[] reusableSentences = (string[])DefaultReusableSentences.Clone();
    [SerializeField, TextArea(2, 4)] private string[] orderedFallbackSentences = (string[])DefaultOrderedSentences.Clone();

    public Dialogue GetRoundDialogue(int roundIndex)
    {
        if (roundDialogues == null || roundDialogues.Length == 0)
        {
            return null;
        }

        if (roundIndex >= 0 && roundIndex < roundDialogues.Length)
        {
            return roundDialogues[roundIndex];
        }

        if (repeatRoundDialogues)
        {
            int wrappedIndex = Mathf.Abs(roundIndex) % roundDialogues.Length;
            return roundDialogues[wrappedIndex];
        }

        return null;
    }

    public Dialogue CreateGeneratedDialogue(int roundIndex)
    {
        Dialogue dialogue = CreateInstance<Dialogue>();
        dialogue.npcName = npcName;
        dialogue.sentences = BuildGeneratedSentences(roundIndex);
        return dialogue;
    }

    private string[] BuildGeneratedSentences(int roundIndex)
    {
        string introLine = BuildIntroLine(roundIndex + 1);

        if (useRandomGeneratedSentences)
        {
            string extraLine = PickRandomSentence(reusableSentences, DefaultReusableSentences);
            return string.IsNullOrWhiteSpace(extraLine) ? new[] { introLine } : new[] { introLine, extraLine };
        }

        List<string> sentences = new List<string> { introLine };
        AppendNonEmptySentences(sentences, orderedFallbackSentences, DefaultOrderedSentences);
        return sentences.ToArray();
    }

    private string BuildIntroLine(int roundNumber)
    {
        string template = string.IsNullOrWhiteSpace(introTemplate)
            ? "ROUND {round}: {npc} says: 'To infinity and beyond!'"
            : introTemplate;

        return template
            .Replace("{round}", roundNumber.ToString())
            .Replace("{npc}", string.IsNullOrWhiteSpace(npcName) ? "Buzz Lightyear" : npcName);
    }

    private static string PickRandomSentence(string[] primaryPool, string[] fallbackPool)
    {
        List<string> pool = new List<string>();
        AddSentences(pool, primaryPool);

        if (pool.Count == 0)
        {
            AddSentences(pool, fallbackPool);
        }

        return pool.Count == 0 ? string.Empty : pool[Random.Range(0, pool.Count)];
    }

    private static void AppendNonEmptySentences(List<string> target, string[] primaryPool, string[] fallbackPool)
    {
        int countBefore = target.Count;
        AddSentences(target, primaryPool);

        if (target.Count == countBefore)
        {
            AddSentences(target, fallbackPool);
        }
    }

    private static void AddSentences(List<string> target, string[] sentences)
    {
        if (sentences == null)
        {
            return;
        }

        for (int i = 0; i < sentences.Length; i++)
        {
            string sentence = sentences[i];
            if (!string.IsNullOrWhiteSpace(sentence))
            {
                target.Add(sentence);
            }
        }
    }
}
