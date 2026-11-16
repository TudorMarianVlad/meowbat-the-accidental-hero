using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

// Gestioneaza selectia vizuala a sloturilor rapide (1-9) si sincronizarea
// cu scriptul de inventar pentru echiparea obiectelor.
public class HotbarUI : MonoBehaviour
{
    [Header("References")]
    public Inventory playerInventory;
    // Chenar vizual pentru evidentierea slotului activ.
    public RectTransform selectorHighlight;

    [Header("UI Slots")]
    // Vector cu cele 9 obiecte UI corespunzatoare hotbar-ului.
    public GameObject[] slotParents;

    private int selectedSlotIndex = -1;

    void Start()
    {
        // Corutina pentru amanarea selectiei initiale (evitarea Race Conditions).
        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        // Asteptam randarea cadrului pentru sincronizarea cu Inventory.Awake().
        yield return new WaitForEndOfFrame();
        SelectSlot(1);
    }

    void Update()
    {
        // Interogare hardware: detectare apasare taste 1-9 in cadrul curent.
        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(1);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(2);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectSlot(3);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectSlot(4);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectSlot(5);
        if (Keyboard.current.digit6Key.wasPressedThisFrame) SelectSlot(6);
        if (Keyboard.current.digit7Key.wasPressedThisFrame) SelectSlot(7);
        if (Keyboard.current.digit8Key.wasPressedThisFrame) SelectSlot(8);
        if (Keyboard.current.digit9Key.wasPressedThisFrame) SelectSlot(9);
    }

    // Gestioneaza selectia vizuala si actualizarea inventarului.
    void SelectSlot(int index)
    {
        // Guard Clause pentru optimizare: evitam recalcularea daca slotul este deja selectat.
        if (selectedSlotIndex == index) return;
        selectedSlotIndex = index;

        // Mapare: index (1-9) catre vector (0-8).
        if (index - 1 < slotParents.Length && slotParents[index - 1] != null)
        {
            selectorHighlight.position = slotParents[index - 1].transform.position;
        }

        // Transmitere index catre nucleul logic al inventarului.
        if (playerInventory != null)
        {
            playerInventory.EquipItem(index);
        }
    }
}