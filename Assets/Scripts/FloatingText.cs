using UnityEngine;
using TMPro;

// Gestioneaza afisarea textului dinamic (daune, vindecari) instantiat in spatiul UI.
public class FloatingText : MonoBehaviour
{
    [Header("References")]
    // Referinta catre componenta TextMeshPro.
    public TextMeshProUGUI textMesh;

    [Header("Settings")]
    // Viteza de translatie verticala.
    public float floatSpeed = 2f;
    // Durata totala de vizibilitate inainte de distrugere.
    public float fadeDuration = 1f;
    // Deviatie spatiala pentru varierea pozitiei textului.
    public Vector3 randomOffset = new Vector3(0.5f, 0, 0);

    private float timer;
    private Color startingColor;

    // Initializare text si culoare la instantiere.
    public void Setup(string text, Color color)
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshProUGUI>();

        textMesh.text = text;
        textMesh.color = color;
        startingColor = color;
        timer = fadeDuration;

        // Aplicare decalaj aleatoriu.
        transform.position += new Vector3(
            Random.Range(-randomOffset.x, randomOffset.x),
            Random.Range(-randomOffset.y, randomOffset.y),
            0
        );
    }

    void Update()
    {
        // Deplasare verticala constanta.
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            // Eliminare din memorie dupa expirarea timpului.
            Destroy(gameObject);
        }
        else
        {
            // Calcul estompare (Fade-out) folosind raportul timp ramas/durata totala.
            float newAlpha = timer / fadeDuration;
            textMesh.color = new Color(startingColor.r, startingColor.g, startingColor.b, newAlpha);
        }
    }
}