using System;

// Container de date (DTO) utilizat pentru serializarea starii jocului.
// Grupeaza variabilele volatile pentru salvarea/incarcarea pe disc.
[Serializable]
public class SaveData
{
    // Statistici de baza ale jucatorului.
    public int gold;
    public int level;
    public int xp;
    public int maxHealth;
    public float maxStamina;
    public int maxXP;

    public float timeOfDay;

    // Progresul upgrade-urilor (persistenta achizitiilor din Shop).
    public int damageLevel;
    public int speedLevel;

    // Modificatori dinamici de combat si mobilitate.
    public int attackDamage;
    public float moveSpeed;

    // Structuri paralele pentru serializarea inventarului.
    // Deoarece ScriptableObjects nu sunt serializabile direct in JSON,
    // stocam ID-urile textuale si cantitatile asociate in vectori paraleli.
    public string[] inventoryItemNames;
    public int[] inventoryQuantities;

    // Date de localizare spatiala (descompunerea Vector3 pentru serializare stabila).
    public float pX;
    public float pY;
    public float pZ;
}