using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCurrencyController : MonoBehaviour
{
    [SerializeField] private int startingCoins;

    private int currentCoins;

    public event Action<PlayerCurrencyController> CoinsChanged;

    public int CurrentCoins => currentCoins;

    void Awake()
    {
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
        NotifyCoinsChanged();
    }

    private void NotifyCoinsChanged()
    {
        CoinsChanged?.Invoke(this);
    }
}
