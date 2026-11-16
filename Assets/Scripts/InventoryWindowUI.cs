using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Clasa InventoryWindowUI controleaza afisarea in masa a sloturilor din rucsac (rucsacul principal).
// Se ocupa de initializarea automata a referintelor din ierarhia interfetei, maparea indicilor 
// vizuali peste structura de date logica si actualizarea grafica la modificarea inventarului.
public class InventoryWindowUI : MonoBehaviour
{
    public Inventory playerInventory;
    // Obiectul parinte din scena (de obicei detinatorul unei componente Grid Layout Group) 
    // care contine cele 36 de sloturi fizice ale rucsacului
    public GameObject gridContainer;

    [Header("Drag & Drop Ref")]
    public MouseItem mouseItem;

    // Masive folosite pentru cache-uirea componentelor grafice, optimizand performanta (evitam GetComponent in bucle)
    private Image[] slotImages;
    private TextMeshProUGUI[] slotTexts;

    // Indexul de pornire din clasa Inventory. Primele sloturi (1-9) sunt rezervate Hotbar-ului,
    // asa ca rucsacul incepe de la indexul logic 10.
    private int startDataIndex = 10;

    void Awake()
    {
        // Alocam memorie pentru cele 36 de sloturi vizibile din grila rucsacului
        slotImages = new Image[36];
        slotTexts = new TextMeshProUGUI[36];

        // Parcurgem copiii din gridContainer pentru a le extrage dinamic componentele necesare.
        for (int i = 0; i < gridContainer.transform.childCount; i++)
        {
            //daca in container sunt mai mult de 36 de sloturi, ignoram surplusul
            if (i >= 36) break;

            // Preluam transform-ul slotului curent pe baza indexului sau ierarhic
            Transform slot = gridContainer.transform.GetChild(i);

            // Cautam componenta copil responsabila de afisarea sprite-ului obiectului
            Transform iconTransform = slot.Find("ItemIcon");
            if (iconTransform != null) slotImages[i] = iconTransform.GetComponent<Image>();

            // Cautam componenta de text pentru afisarea cantitatii stivei
            Transform textTransform = slot.Find("QuantityText");
            if (textTransform != null) slotTexts[i] = textTransform.GetComponent<TextMeshProUGUI>();

            // Extragem scriptul de control individual al slotului (InventorySlotUI)
            InventorySlotUI slotScript = slot.GetComponent<InventorySlotUI>();
            if (slotScript != null)
            {
                //Translatam indexul vizual local i (0-35) in indexul logic real (10-45).
                // Astfel, primul slot vizibil din grila va sti ca el modifica de fapt slotul 10 din Inventory.
                slotScript.slotIndex = i + startDataIndex;
                slotScript.inventory = playerInventory;
                slotScript.mouseItem = mouseItem;
            }
        }
    }

    void Start()
    {
        if (playerInventory != null)
        {
            // Inscriem metoda UpdateUI in delegatul sistemului bazat pe evenimente (Observer Pattern)
            playerInventory.OnInventoryChanged += UpdateUI;

            // Fortam o prima desenare completa a interfetei rucsacului la pornire
            UpdateUI();
        }
    }

    void OnDestroy()
    {
        // Dezabonare obligatorie pentru a preveni scurgerile de memorie (Memory Leaks)
        if (playerInventory != null) playerInventory.OnInventoryChanged -= UpdateUI;
    }

    // Metoda principala de reimprospatare in masa a elementelor grafice din rucsac
    public void UpdateUI()
    {
        // daca inventarul nu s-a conectat inca, oprim executia pentru a evita erori de tip Null
        if (playerInventory == null) return;

        // Iteram prin toate cele 36 de elemente cache-uite din interfata
        for (int i = 0; i < 36; i++)
        {
            if (i >= slotImages.Length) break;

            // Calculam indexul corespunzator din baza de date a inventarului
            int dataIndex = i + startDataIndex;
            if (dataIndex >= playerInventory.inventorySlots.Length) break;

            // Extragem metadatele si cantitatea curenta
            ItemData item = playerInventory.inventorySlots[dataIndex];
            int qty = playerInventory.slotQuantities[dataIndex];

            // Actualizarea vizuala a iconitei
            if (slotImages[i] != null)
            {
                if (item != null)
                {
                    // Slotul contine un obiect: aplicam sprite-ul si fortam culoarea opaca (Color.white)
                    slotImages[i].sprite = item.icon;
                    slotImages[i].color = Color.white;
                }
                else
                {
                    // Slotul este gol: eliminam sprite-ul si ascundem complet componenta (Color.clear, Alpha = 0)
                    // pentru a lasa fundalul slotului vizibil si curat.
                    slotImages[i].sprite = null;
                    slotImages[i].color = Color.clear;
                }
            }

            // Actualizarea textului de cantitate
            if (slotTexts[i] != null)
            {
                // Conditie stricta de afisare: obiectul exista, este stivuibil (Stackable) si cantitatea depaseste 1 unitate
                if (item != null && item.isStackable && qty > 1)
                {
                    slotTexts[i].text = qty.ToString();
                    slotTexts[i].gameObject.SetActive(true); // Afisam textul cifrei
                }
                else
                {
                    // Ascundem componenta de text daca avem doar un singur obiect sau daca slotul este gol
                    slotTexts[i].gameObject.SetActive(false);
                }
            }
        }
    }
}