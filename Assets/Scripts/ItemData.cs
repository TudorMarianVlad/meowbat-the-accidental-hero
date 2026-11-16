using UnityEngine;

// Categorii de obiecte pentru filtrare si logica de echipare.
public enum ItemType
{
    Generic,
    Consumable,
    Weapon,
    Helmet,
    Chest,
    Legs,
    Boots,
    Ammo
}

// Structura de date pentru obiecte; foloseste ScriptableObject pentru persistenta datelor.
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Information")]
    public string itemName;
    // ID unic pentru sistemul de salvare (Save/Load).
    public string itemID;
    public Sprite icon;
    // Prefab pentru vizualizarea obiectului in mana jucatorului.
    public GameObject equipmentPrefab;

    [Header("Settings")]
    public ItemType itemType;
    // Flag pentru logica de rotatie in clasa ActiveWeapon.
    public bool isRangedWeapon;
    // Permite stivuirea (Stacking) in inventar.
    public bool isStackable;
    public int maxStackSize = 20;

    // Atribute pentru consumabile si armuri.
    public int healAmount;
    public int armorValue;

    [Header("Weapon Info")]
    // Prefab pentru proiectile (folosit de arme ranged).
    public GameObject projectilePrefab;
    public float launchForce = 10f;

    [Header("Shop Settings")]
    // Pretul de achizitie in ShopManager.
    public int buyPrice = 50;
}