using UnityEngine;
using System.Collections.Generic;

public class RoundDialogueManager : MonoBehaviour
{
    public static RoundDialogueManager Instance;

    [Header("Dialogue for each round")]
    public List<Dialogue> dialoguesPerRound;

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
        if (currentRound < dialoguesPerRound.Count)
        {
            return dialoguesPerRound[currentRound];
        }
        else
        {
            return dialoguesPerRound[dialoguesPerRound.Count - 1];
        }
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
