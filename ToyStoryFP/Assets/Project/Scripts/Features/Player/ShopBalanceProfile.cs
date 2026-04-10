using UnityEngine;

[CreateAssetMenu(fileName = "DefaultShopBalanceProfile", menuName = "Player/Shop Balance Profile")]
public class ShopBalanceProfile : ScriptableObject
{
    [SerializeField] private int sharedUpgradePrice = 10;
    [SerializeField] private int ammoPurchaseMagazineCount = 1;
    [SerializeField] private float upgradeStepMultiplier = 0.1f;
    [SerializeField] private float healFraction = 0.5f;

    public int SharedUpgradePrice => sharedUpgradePrice;
    public int AmmoPurchaseMagazineCount => ammoPurchaseMagazineCount;
    public float UpgradeStepMultiplier => upgradeStepMultiplier;
    public float HealFraction => healFraction;
}
