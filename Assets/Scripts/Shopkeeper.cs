using UnityEngine;
using UnityEngine.InputSystem;

// Gestioneaza interactiunea cu NPC-ul comerciant: activarea meniului, 
// repozitionarea UI-ului si curatarea starii la iesirea din proximitate.
public class Shopkeeper : MonoBehaviour
{
    [Header("UI References")]
    public GameObject shopUIPanel;
    public GameObject inventoryUIPanel;
    // Pozitia offset pentru inventar la deschiderea magazinului.
    public Vector2 inventoryLeftPosition = new Vector2(-400, 0);

    private bool isPlayerNear = false;
    private PlayerControls controls;

    void Awake()
    {
        controls = new PlayerControls();
        // Abonare la actiunea de interactiune prin eveniment.
        controls.UI.Interact.performed += ctx => TryOpenShop();
    }

    void Start()
    {
        if (shopUIPanel != null) shopUIPanel.SetActive(false);
    }

    void OnEnable() { controls.Enable(); }
    void OnDisable() { controls.Disable(); }

    // Metoda de validare pentru deschiderea magazinului.
    private void TryOpenShop()
    {
        if (isPlayerNear) ToggleShop();
    }

    // Comutare stare ferestre UI si repozitionare inventar.
    void ToggleShop()
    {
        bool isShopOpen = shopUIPanel.activeSelf;
        shopUIPanel.SetActive(!isShopOpen);

        if (inventoryUIPanel != null)
        {
            inventoryUIPanel.SetActive(!isShopOpen);

            // Daca se deschide, mutam inventarul in pozitia setata.
            if (!isShopOpen)
            {
                RectTransform invRect = inventoryUIPanel.GetComponent<RectTransform>();
                if (invRect != null) invRect.anchoredPosition = inventoryLeftPosition;
            }
        }
    }

    // Gestionare proximitate: intrare in zona de interactiune.
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerNear = true;
    }

    // Gestionare proximitate: iesire din zona si resetare UI de siguranta.
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = false;
            // Curatare interfata daca jucatorul se indeparteaza.
            if (shopUIPanel != null) shopUIPanel.SetActive(false);
            if (inventoryUIPanel != null) inventoryUIPanel.SetActive(false);
        }
    }
}