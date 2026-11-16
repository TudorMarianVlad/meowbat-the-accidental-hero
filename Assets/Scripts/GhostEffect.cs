using UnityEngine;

// Gestioneaza disparitia graduala a unei clone vizuale (efect de tip ghost/trail).
public class GhostEffect : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color color;

    [Header("Settings")]
    // Viteza de disparitie a umbrei.
    [SerializeField] private float fadeSpeed = 5f;

    // Initializare culoare la instantiere.
    public void Init(Color startColor)
    {
        sr = GetComponent<SpriteRenderer>();
        color = startColor;
        sr.color = color;
    }

    private void Update()
    {
        // 1. Calcul estompare (Fade Out) proportional cu timpul.
        color.a -= fadeSpeed * Time.deltaTime;
        sr.color = color;

        // 2. Distrugere obiect dupa ce devine complet transparent (prevenire memory leak).
        if (color.a <= 0)
        {
            Destroy(gameObject);
        }
    }
}