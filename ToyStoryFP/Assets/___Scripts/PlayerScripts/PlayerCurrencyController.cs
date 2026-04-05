using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCurrencyController : MonoBehaviour
{
    [SerializeField] private int startingCoins;

    private int currentCoins;
    private PlayerAudioController playerAudio;

    public event Action<PlayerCurrencyController> CoinsChanged;

    public int CurrentCoins => currentCoins;

    void Awake()
    {
        playerAudio = GetComponent<PlayerAudioController>();
        currentCoins = Mathf.Max(0, startingCoins);
        NotifyCoinsChanged();
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentCoins += amount;
        playerAudio?.PlayCoinPickup();
        NotifyCoinsChanged();
    }

    private void NotifyCoinsChanged()
    {
        CoinsChanged?.Invoke(this);
    }
}
