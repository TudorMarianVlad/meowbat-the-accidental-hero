using UnityEngine;
using UnityEngine.InputSystem;

// Clasa InventoryToggle gestioneaza starea de activare/dezactivare a ferestrei de inventar (Toggle)
// prin ascultarea evenimentelor de la tastatura si repozitionarea panoului in spatiul Canvas.
public class InventoryToggle : MonoBehaviour
{
    [Header("Assign in Inspector")]
    // Referinta catre obiectul parinte din ierarhie care contine elementele grafice ale inventarului
    public GameObject inventoryUIGameObject;

    [Header("UI Position Settings")]
    // Componenta specializata de pozitionare 2D in spatiul UI a panoului de inventar
    private RectTransform inventoryRect;
    // Coordonatele X si Y locale relative la punctele de ancorare (implicit centrul ecranului: 0, 0)
    public Vector2 centerPosition = new Vector2(0, 0);

    // Variabila de stare booleana pentru a urmari daca inventarul este deschis sau inchis
    private bool isOpen = false;
    // Referinta catre clasa C# generata automat de Unity New Input System pe baza hartii de actiuni
    private PlayerControls controls;

    void Awake()
    {
        // Instantiem clasa de controale hardware
        controls = new PlayerControls();

        // Ne abonam la evenimentul '.performed' al actiunii Inventory din harta de UI.
        // Folosim o expresie lambda (ctx => Toggle()) pentru a apela automat functia locala Toggle 
        // in frame-ul exact in care jucatorul apasa tasta dedicata (ex: tasta 'I' sau 'Tab').
        controls.UI.Inventory.performed += ctx => Toggle();
    }

    // Activam si dezactivam harta de controale in concordanta cu ciclul de viata al obiectului.
    // Aceasta practica previne erorile de tip "input rezidual" daca scriptul este dezactivat temporar.
    void OnEnable() { controls.Enable(); }
    void OnDisable() { controls.Disable(); }

    void Start()
    {
        // Initializarea componentei grafice de pozitionare
        if (inventoryUIGameObject != null)
        {
            inventoryRect = inventoryUIGameObject.GetComponent<RectTransform>();
        }

        // Stare initiala defensiva: ne asiguram ca inventarul porneste complet ascuns la inceputul jocului
        inventoryUIGameObject.SetActive(false);
        isOpen = false;
    }

    // Functia principala de comutare a starii ferestrei
    private void Toggle()
    {
        // Inversam starea logica actuala
        isOpen = !isOpen;

        // Aplicam starea pe obiectul de UI pentru a-l randa sau a-l ascunde de pe ecran
        inventoryUIGameObject.SetActive(isOpen);

        if (isOpen)
        {
            // Cand inventarul este deschis in mod normal, ii fortam pozitia ancorata la coordonatele tinta (0, 0).
            if (inventoryRect != null)
            {
                inventoryRect.anchoredPosition = centerPosition;
            }

            // Cand deschidem un meniu, deblocam cursorul si il facem vizibil
            // pentru a-i permite jucatorului sa interactioneze cu sloturile
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}