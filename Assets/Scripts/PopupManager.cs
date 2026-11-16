using UnityEngine;

// Gestioneaza instantierea textelor plutitoare (Floating Text) in spatiul UI al jucatorului.
// Actioneaza ca un punct unic de acces (API) pentru afisarea indicatorilor textuali.
public class PopupManager : MonoBehaviour
{
    [Header("Setup")]
    // Prefab-ul cu componenta FloatingText.
    public GameObject floatingTextPrefab;
    // Punctul de spawn situat deasupra capului personajului.
    public Transform headPopupPoint;

    private Canvas playerCanvas;

    void Awake()
    {
        // Initializare defensiva: cautare Canvas in ierarhia locala.
        playerCanvas = GetComponentInChildren<Canvas>();
    }

    // API public pentru afisarea unui mesaj in UI.
    public void Show(string text, Color color)
    {
        // Verificare referinte (Guard Clause).
        if (floatingTextPrefab == null || headPopupPoint == null || playerCanvas == null)
        {
            Debug.LogWarning("PopupManager missing references!");
            return;
        }

        // Instantiere ca fiu al Canvas-ului pentru randare corecta in UI Space.
        GameObject textObj = Instantiate(floatingTextPrefab, headPopupPoint.position, Quaternion.identity, playerCanvas.transform);

        // Injectare date prin metoda Setup a scriptului FloatingText.
        FloatingText ft = textObj.GetComponent<FloatingText>();
        if (ft != null) ft.Setup(text, color);
    }
}