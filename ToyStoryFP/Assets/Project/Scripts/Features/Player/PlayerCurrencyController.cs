using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCurrencyController : MonoBehaviour
{
    [SerializeField] private int startingCoins;
    [SerializeField] private PlayerAudioController playerAudio;

    private int currentCoins;

    public event Action<PlayerCurrencyController> CoinsChanged;

    public int CurrentCoins => currentCoins;

    void Awake()
    {
        currentCoins = Mathf.Max(0, startingCoins);
        RunStatsStore.UpdateCoins(currentCoins);
        NotifyCoinsChanged();
    }

    // Gestiona add monedas.
    public void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentCoins += amount;
        RunStatsStore.UpdateCoins(currentCoins);
        playerAudio?.PlayCoinPickup();
        NotifyCoinsChanged();
    }

    // Intenta spend monedas.
    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (currentCoins < amount)
        {
            return false;
        }

        currentCoins -= amount;
        RunStatsStore.UpdateCoins(currentCoins);
        NotifyCoinsChanged();
        return true;
    }

    // Notifica monedas cambios.
    private void NotifyCoinsChanged()
    {
        CoinsChanged?.Invoke(this);
    }
}
