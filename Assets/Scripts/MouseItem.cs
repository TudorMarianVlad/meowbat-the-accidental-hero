using UnityEngine;
using UnityEngine.UI;

// Gestioneaza afisarea vizuala a obiectului aflat sub cursor in timpul operatiunilor de Drag & Drop.
public class MouseItem : MonoBehaviour
{
    [Header("UI References")]
    // Componenta Image responsabila de randarea iconitei obiectului mutat.
    public Image itemImage;

    void Update()
    {
        // Urmarire cursor: alinierea pozitiei UI-ului cu coordonatele mouse-ului.
        transform.position = UnityEngine.Input.mousePosition;
    }

    // Actualizeaza sprite-ul si vizibilitatea iconitei.
    public void SetSprite(Sprite sprite)
    {
        itemImage.sprite = sprite;

        // Ajustare transparenta (Alpha) in functie de starea obiectului.
        Color color = itemImage.color;

        // Daca sprite-ul este null (drop), setam opacitatea la 0.
        color.a = (sprite == null) ? 0 : 1;

        itemImage.color = color;
    }
}