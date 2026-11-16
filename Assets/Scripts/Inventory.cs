using UnityEngine;
using System; 

// Clasa Inventory ruleaza ca master ledger (registru central) pentru toate obiectele detinute.
// Se ocupa de stocarea datelor, stivuirea obiectelor (stacking), consumul de munitie, 
// echiparea armelor active si algoritmul de interschimbare (Swap) intre sloturi.
public class Inventory : MonoBehaviour
{
    // Structura de date paralela: un masiv stocheaza metadatele obiectului (ScriptableObject),
    // iar celalalt masiv stocheaza cantitatea numerica aflata la acelasi index in memorie.
    public ItemData[] inventorySlots = new ItemData[50];
    public int[] slotQuantities = new int[50];

    [Header("Starting Items")]
    // Referinta catre arma oferita automat jucatorului la crearea unui profil nou
    public ItemData swordItem;

    [Header("References")]
    // Punctul de pivot (Transform child) unde se vor instantia armele in mana jucatorului
    public Transform weaponHolder;
    private PlayerStats playerStats;

    // Definitia evenimentului bazat pe sablonul de proiectare Observer (C# Action).
    // Orice script de interfata (HotbarUI, BackpackUI) se va abona aici pentru a afla cand s-a modificat ceva.
    public event Action OnInventoryChanged;

    // Stocheaza datele obiectului tinut in mana in mod curent
    public ItemData currentItemData;
    // Referinta catre clona fizica (clonata din Prefab) randata in scena
    public GameObject currentEquippedItem;

    // Variabila de stare care retine indexul slotului activ din Hotbar (implicit slotul 1)
    private int currentEquippedSlot = 1;

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();

        // Logica de acordare a echipamentului initial (Sabia de start)
        if (swordItem != null)
        {
            // Fortam plasarea sabiei in slotul 1 (primul slot vizibil din Hotbar)
            inventorySlots[1] = swordItem;
            slotQuantities[1] = 1;
        }

        // Anuntam sistemul grafic sa isi deseneze elementele pe ecran pentru prima data
        if (OnInventoryChanged != null) OnInventoryChanged.Invoke();

        // Echipam vizual si logic primul slot
        EquipItem(1);

        // Cerem actualizarea coeficientului de aparare in caz ca aveam armura echipata nativ
        if (playerStats != null) playerStats.RecalculateArmor();
    }

    // Functie utilitara (Getter) folosita de sisteme externe (ex: salvare) pentru a interoga slotul activ
    public int GetSelectedSlotIndex()
    {
        return currentEquippedSlot;
    }

    // Algoritmul principal de adaugare a obiectelor in inventar (colectare de pe jos sau cumparare)
    public bool AddItem(ItemData item, int amount)
    {
        // ETAPA 1: GESTIUNEA OBIECTELOR STIVUIBILE (Stackable Items)
        if (item.isStackable)
        {
            // Scanam inventarul de la indexul 1 pentru a gasi un pachet existent de acelasi tip
            for (int i = 1; i < inventorySlots.Length; i++)
            {
                if (inventorySlots[i] != null && inventorySlots[i].itemID == item.itemID)
                {
                    // Verificam daca pachetul gasit mai are loc liber pana la limita sa maxima
                    if (slotQuantities[i] < item.maxStackSize)
                    {
                        // Incrementam cantitatea existenta si declansam evenimentul de UI
                        slotQuantities[i] += amount;
                        if (OnInventoryChanged != null) OnInventoryChanged.Invoke();
                        return true; // Obiectul a fost stackat cu succes, algoritmul se opreste
                    }
                }
            }
        }

        // ETAPA 2: PLASAREA INTR-UN SLOT COMPLET GOL
        // Daca obiectul nu se poate stacka sau nu s-a gasit un pachet cu loc liber, cautam un slot liber.
        int emptyIndex = FindEmptySlot();
        if (emptyIndex != -1)
        {
            inventorySlots[emptyIndex] = item;
            slotQuantities[emptyIndex] = amount;
            if (OnInventoryChanged != null) OnInventoryChanged.Invoke();
            return true;
        }

        return false; // Inventarul este complet plin, returnam false (obiectul ramane pe jos)
    }

    // Algoritm dedicat utilizarii munitiei (sagetilor) de catre arme de distanta (BowWeapon)
    public bool UseAmmo(string targetID)
    {
        // Parcurgem toate cele 50 de sloturi in cautarea id-ului specificat ("Arrow")
        for (int i = 1; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] != null && inventorySlots[i].itemID == targetID)
            {
                if (slotQuantities[i] > 0)
                {
                    // Consumam o unitate de munitie
                    slotQuantities[i]--;

                    // Daca stiva a ajuns la 0, curatam metadatele slotului (devine liber)
                    if (slotQuantities[i] <= 0)
                    {
                        inventorySlots[i] = null;
                    }

                    // Fortam reimprospatarea cifrelor de cantitate pe interfata grafica
                    if (OnInventoryChanged != null) OnInventoryChanged.Invoke();

                    return true; // Munitia a fost identificata si extrasa cu succes
                }
            }
        }
        return false; // Jucatorul nu detine munitia necesara tragerii
    }

    // Algoritm de prioritizare spatiala pentru stocarea obiectelor noi
    private int FindEmptySlot()
    {
        // Prioritatea 1: Rucsacul principal (Backpack, indicii 10-45). 
        // Obiectele noi cumparate sau culese merg automat acolo pentru a nu polua Hotbar-ul.
        for (int i = 10; i <= 45; i++) { if (inventorySlots[i] == null) return i; }

        // Prioritatea 2: Bara rapida (Hotbar, indicii 1-9). Folosita doar daca rucsacul e plin.
        for (int i = 1; i <= 9; i++) { if (inventorySlots[i] == null) return i; }

        return -1; // Deficit de spatiu stocare
    }

    // Elimina o cantitate specifica dintr-un slot (ex: aruncare sau consumare potiuni)
    public void RemoveItem(int slotIndex, int amount)
    {
        if (slotIndex < 0 || slotIndex >= slotQuantities.Length) return;

        slotQuantities[slotIndex] -= amount;

        if (slotQuantities[slotIndex] <= 0)
        {
            slotQuantities[slotIndex] = 0;
            inventorySlots[slotIndex] = null;

            // Coerenta vizuala: Daca obiectul sters complet era exact cel echipat in mana, 
            // distrugem clona grafica pentru ca jucatorul sa nu ramana cu o arma fantoma in mana.
            if (slotIndex == currentEquippedSlot)
            {
                currentItemData = null;
                if (currentEquippedItem != null) Destroy(currentEquippedItem);
            }
        }

        OnInventoryChanged?.Invoke();
    }

    // Gestioneaza instantierea si comutarea armelor active in ierarhia Unity
    public void EquipItem(int slotIndex)
    {
        // Constrangere arhitecturala: doar sloturile 1-9 (Hotbar) pot fi folosite ca arme active
        if (slotIndex < 1 || slotIndex > 9) return;

        currentEquippedSlot = slotIndex;

        // GESTIUNEA MEMORIEI (Pool Clean)
        // Daca aveam deja o arma in mana, o distrugem din scena inainte de a o crea pe urmatoarea
        if (currentEquippedItem != null) Destroy(currentEquippedItem);

        // Preluam noile metadate din slotul selectat
        currentItemData = inventorySlots[slotIndex];

        // Daca slotul nu este gol si contine un Prefab grafic asociat
        if (currentItemData != null && currentItemData.equipmentPrefab != null)
        {
            // Instantiem fizic arma in scena si o plasam automat ca obiect copil (Child) sub weaponHolder
            currentEquippedItem = Instantiate(currentItemData.equipmentPrefab, weaponHolder);
        }
    }

    // Algoritmul matematic de interschimbare (Swap) prin variabila temporara.
    // Permite mutarea obiectelor prin Drag & Drop intre rucsac, hotbar si sloturile de echipament.
    public void SwapItems(int indexA, int indexB)
    {
        // Validarea indicilor pentru a preveni erori de depasire a limitelor vectorului (IndexOutOfRangeException)
        if (indexA < 0 || indexA >= inventorySlots.Length || indexB < 0 || indexB >= inventorySlots.Length) return;

        // 1. Schimbul de referinte ItemData (Metadate) folosind o variabila temporara (temp)
        ItemData temp = inventorySlots[indexA];
        inventorySlots[indexA] = inventorySlots[indexB];
        inventorySlots[indexB] = temp;

        // 2. Schimbul corelat de cantitati numerice
        int tempQty = slotQuantities[indexA];
        slotQuantities[indexA] = slotQuantities[indexB];
        slotQuantities[indexB] = tempQty;

        // 3. Notificarea observatorilor (UI) pentru redesenare vizuala
        if (OnInventoryChanged != null) OnInventoryChanged.Invoke();

        // Daca tranzactia a implicat sloturile de armura, recalculam imediat statistica de aparare a jucatorului
        if (playerStats != null) playerStats.RecalculateArmor();

        // 4. Verificarea consistentei echiparii active
        // Daca vreunul dintre sloturile mutate era exact slotul pe care jucatorul il are selectat in mana in acest moment,
        // re-apelam functia EquipItem pentru a actualiza instantaneu modelul 3D/2D din mana personajului.
        if (indexA == currentEquippedSlot || indexB == currentEquippedSlot)
        {
            EquipItem(currentEquippedSlot);
        }
    }

    // Metoda publica ce permite fortarea manuala a actualizarii interfetei grafice din alte scripturi (ex: SaveManager la incarcare)
    public void RefreshInventoryUI()
    {
        OnInventoryChanged?.Invoke();
    }
}