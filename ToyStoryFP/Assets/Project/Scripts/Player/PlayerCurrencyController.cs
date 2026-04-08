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
