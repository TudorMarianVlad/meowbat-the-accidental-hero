using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Clasa InventorySlotUI controleaza comportamentul vizual si interactiunile mecanice ale unui singur slot din UI.
// Implementeaza interfetele de manipulare prin gesturi (Drag & Drop) oferite de motorul grafic Unity.
public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("Setup")]
    // Indexul numeric unic pe care acest slot grafic il reprezinta in masivul logic din clasa Inventory
    public int slotIndex;
    // Referinta catre imaginea de UI care va afisa sprite-ul obiectului
    public Image itemIcon;
    // Referinta catre textul TMP care va afisa stiva numerica
    public TextMeshProUGUI quantityText;
    public Inventory inventory;
    // Scriptul utilitar care muta o copie a iconitei atasata de cursor in timpul drag-ului
    public MouseItem mouseItem;
    // Tipul de obiect acceptat de acest slot (folosit pentru a restrictiona sloturile de echipament/armura)
    public ItemType allowedType = ItemType.Generic;

    // Variabila statica (globala la nivel de clasa) care retine instanta precisa a slotului de la care a pornit miscarea
    public static InventorySlotUI slotBeingDragged;

    private void Awake()
    {
        // Initializare automata (Fallback): daca referinta inventarului lipseste, o cautam in scena
        if (inventory == null) inventory = FindFirstObjectByType<Inventory>();

        // Automatizarea configurarii ierarhice a componentelor interne ale slotului din Canvas
        if (itemIcon == null)
        {
            // Cautam componenta copil numita "ItemIcon" (tratare insensibila la majuscule)
            Transform t = transform.Find("ItemIcon") ?? transform.Find("itemicon");
            if (t) itemIcon = t.GetComponent<Image>();
        }
        if (quantityText == null)
        {
            Transform t = transform.Find("QuantityText");
            if (t) quantityText = t.GetComponent<TextMeshProUGUI>();
        }
    }

    private void Start()
    {
        if (inventory != null)
        {
            // Inscriem metoda locala de reimprospatare in delegatul de evenimente al inventarului (Observer Pattern)
            inventory.OnInventoryChanged += UpdateSlotUI;
        }

        // Folosim Invoke pentru a introduce o mica temporizare de 0.1 secunde la pornirea scenei.
        // Ofera suficient timp clasei logice Inventory sa isi genereze masivele si obiectele de start,
        // prevenind citirea prematura a unor sloturi goale de catre interfata.
        Invoke("UpdateSlotUI", 0.1f);
    }

    private void OnDestroy()
    {
        // GESTIUNE CORECTA A MEMORIEI: Dezabonarea obligatorie de la eveniment in momentul distrugerii obiectului.
        // Previne bug-urile de tip "MissingReferenceException" (Memory Leaks) in care evenimentul ar fi incercat
        // sa apeleze o componenta UI care nu mai exista pe ecran.
        if (inventory != null) inventory.OnInventoryChanged -= UpdateSlotUI;
    }

    // Functia principala de desenare si sincronizare vizuala a slotului
    public void UpdateSlotUI()
    {
        if (inventory == null) return;

        // Extragem starea logica actuala direct din registrele paralele ale inventarului
        ItemData item = inventory.inventorySlots[slotIndex];
        int qty = inventory.slotQuantities[slotIndex];

        // LOGICA DE REDESENARE A ICONITEI
        if (itemIcon != null)
        {
            if (item != null)
            {
                // Asociem sprite-ul din ScriptableObject componentei grafice Image
                itemIcon.sprite = item.icon;
                // Corectie critica de opacitate: fortam culoarea completa (Alpha = 1) pentru a elimina transparenta implicita
                itemIcon.color = Color.white;
                itemIcon.gameObject.SetActive(true);
            }
            else
            {
                // Daca slotul este gol in memorie, ascundem componenta de imagine pentru a nu desena un patrat alb gol
                itemIcon.gameObject.SetActive(false);
            }
        }

        // LOGICA DE AFISARE A STIVEI NUMERICE
        if (quantityText != null)
        {
            // Afisam textul doar daca pachetul este valid, stivuibil SI cantitatea depaseste o singura unitate
            if (item != null && item.isStackable && qty > 1)
            {
                quantityText.text = qty.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                // Ascundem textul pentru obiectele unice sau cand cantitatea este 1 (aspect curat de RPG)
                quantityText.gameObject.SetActive(false);
            }
        }
    }

    // INTERFATA IBeginDragHandler: Apelata in frame-ul exact in care jucatorul incepe sa traga slotul cu mouse-ul
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Guard Clause: Daca slotul nu contine niciun obiect, blocam operatiunea de Drag
        if (inventory.inventorySlots[slotIndex] == null) return;

        // Salvam referinta acestui slot in variabila statica globala pentru a fi procesata ulterior la Drop
        slotBeingDragged = this;

        // Transmitem iconita obiectului catre utilitarul cursorului (MouseItem) pentru a urmari mouse-ul vizual
        mouseItem.SetSprite(inventory.inventorySlots[slotIndex].icon);

        // Feedback vizual: Reducem opacitatea imaginii originale din slot la 50% (Alpha = 0.5f)
        // pentru a indica vizual jucatorului ca obiectul este "ridicat" din locul sau.
        if (itemIcon != null) itemIcon.color = new Color(1f, 1f, 1f, 0.5f);
    }

    // INTERFATA IDragHandler: Obligatorie din punct de vedere sintactic pentru functionarea Drag-ului, 
    // chiar daca logica de translatie efectiva este delegata in totalitate scriptului MouseItem.
    public void OnDrag(PointerEventData eventData) { }

    // INTERFATA IEndDragHandler: Apelata automat cand jucatorul elibereaza butonul mouse-ului, indiferent unde se afla pe ecran
    public void OnEndDrag(PointerEventData eventData)
    {
        // Resetam starea globala si curatam imaginea atasata de cursor
        slotBeingDragged = null;
        mouseItem.SetSprite(null);

        // Repornim desenarea standard pentru a re-ajusta culorile si iconitele
        UpdateSlotUI();
    }

    // INTERFATA IDropHandler: Se declanseaza exclusiv pe slotul DE DESTINATIE (cel de sub cursor in momentul eliberarii)
    public void OnDrop(PointerEventData eventData)
    {
        // Validam daca miscarea a pornit de la un slot valid
        if (slotBeingDragged == null) return;

        // Extragem metadatele obiectului care este mutat in acest moment
        ItemData draggedItem = inventory.inventorySlots[slotBeingDragged.slotIndex];
        if (draggedItem == null) return;

        // Pasul 1: Verificam daca slotul curent (destinatia) are o restrictie (ex: accepta doar Armor)
        // si daca obiectul tras nu corespunde acelui tip. In caz de nepotrivire, anulam mutarea.
        if (allowedType != ItemType.Generic && draggedItem.itemType != allowedType) return;

        // Pasul 2: Gestionam cazul de Interschimbare (Swap bidirectional).
        // Daca in acest slot de destinatie exista deja un obiect (existingItem), trebuie sa verificam 
        // daca slotul de unde a plecat miscarea (originea) poate accepta tipul obiectului curent.
        ItemData existingItem = inventory.inventorySlots[this.slotIndex];
        if (existingItem != null && slotBeingDragged.allowedType != ItemType.Generic && existingItem.itemType != slotBeingDragged.allowedType) return;

        // Daca toate filtrele de siguranta au fost trecute cu succes, apelam algoritmul central 
        // din Inventory pentru a efectua schimbul de indici in memorie.
        inventory.SwapItems(slotBeingDragged.slotIndex, this.slotIndex);
    }
}