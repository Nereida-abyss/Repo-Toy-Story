using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DefaultWeaponCatalog", menuName = "Player/Weapon Catalog")]
public class WeaponCatalog : ScriptableObject
{
    [Serializable]
    public class WeaponCatalogEntry
    {
        [SerializeField] private string weaponId;
        [SerializeField] private WeaponScript weapon;
        [SerializeField] private int price;
        [SerializeField] private bool unlockedByDefault;

        public string WeaponId => weaponId;
        public WeaponScript Weapon => weapon;
        public int Price => price;
        public bool UnlockedByDefault => unlockedByDefault;
    }

    [SerializeField] private string defaultStartingWeaponId = "Low_Poly_1323";
    [SerializeField] private List<WeaponCatalogEntry> entries = new List<WeaponCatalogEntry>();

    public string DefaultStartingWeaponId => defaultStartingWeaponId;
    public IReadOnlyList<WeaponCatalogEntry> Entries => entries;
}
