using UnityEngine;
using System.IO;

// Clasa statica pentru gestionarea persistentei datelor (Save/Load).
// Utilizeaza JsonUtility pentru serializarea starii jocului intr-un fisier local.
public static class SaveManager
{
    // Calea de stocare in directorul securizat al sistemului de operare.
    private static string savePath = Application.persistentDataPath + "/savefile.json";

    // Colecteaza datele active si le scrie intr-un fisier JSON pe disc.
    public static void SaveGame(PlayerStats stats, UpgradeShopkeeper shop)
    {
        string directory = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        SaveData data = new SaveData();

        // 1. Salvare statistici nucleu.
        data.gold = stats.currentGold;
        data.level = stats.currentLevel;
        data.xp = stats.currentXP;
        data.maxHealth = stats.maxHealth;
        data.maxStamina = stats.maxStamina;
        data.maxXP = stats.xpToNextLevel;

        // 2. Salvare progres upgrade-uri si modificatori.
        data.damageLevel = shop.damageLevel;
        data.speedLevel = shop.speedLevel;
        data.attackDamage = stats.GetComponent<PlayerCombat>().attackDamage;
        data.moveSpeed = stats.GetComponent<PlayerController>().moveSpeed;

        // 3. Serializare paralela inventar (ID-uri textuale si cantitati).
        Inventory inv = stats.GetComponent<Inventory>();
        if (inv != null)
        {
            data.inventoryItemNames = new string[inv.inventorySlots.Length];
            data.inventoryQuantities = new int[inv.inventorySlots.Length];
            for (int i = 0; i < inv.inventorySlots.Length; i++)
            {
                data.inventoryItemNames[i] = (inv.inventorySlots[i] != null) ? inv.inventorySlots[i].itemName : "";
                data.inventoryQuantities[i] = inv.slotQuantities[i];
            }
        }

        // 4. Salvare coordonate spatiale si ciclu zi-noapte.
        data.pX = stats.transform.position.x;
        data.pY = stats.transform.position.y;
        data.pZ = stats.transform.position.z;

        DayNightCycle timeSystem = Object.FindFirstObjectByType<DayNightCycle>();
        if (timeSystem != null) data.timeOfDay = timeSystem.currentTime;

        // Conversie obiect in JSON si scriere pe disc.
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
    }

    // Citeste si reconstruieste structura de date din fisierul JSON.
    public static SaveData LoadGame()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        return null;
    }
}