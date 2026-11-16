using UnityEngine;
using UnityEngine.UI;

// Gestioneaza efectul vizual de alerta (overlay rosu care pulseaza) la viata scazuta.
public class DamageFlash : MonoBehaviour
{
    // Referinta catre imaginea UI (overlay).
    private Image overlayImage;

    [Header("Manual Setup")]
    // Referinta la statisticile jucatorului.
    public PlayerStats playerStats;

    [Header("Settings")]
    // Opacitatea maxima a efectului pentru a nu bloca vizibilitatea.
    public float maxAlpha = 0.4f;

    // Viteza de pulsare a efectului.
    public float pulseSpeed = 4f;

    void Start()
    {
        // Preluare componenta Image.
        overlayImage = GetComponent<Image>();

        // Initializare defensiva (Fallback) folosind tag-ul "Player".
        if (playerStats == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerStats = player.GetComponent<PlayerStats>();
        }
    }

    void Update()
    {
        // Oprire executie daca referinta lipseste.
        if (playerStats == null) return;

        // Calcul procentaj viata (cast la float pentru precizie cu virgula mobila).
        float healthPercent = (float)playerStats.currentHealth / playerStats.maxHealth;

        // Declansare pulsare daca viata scade sub pragul critic de 30%.
        if (healthPercent <= 0.3f)
        {
            // Calcul unda sinusoidala normalizata in intervalul [0, 1].
            float alpha = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;

            Color newColor = overlayImage.color;

            // Aplicare alpha in functie de unda si opacitatea maxima.
            newColor.a = alpha * maxAlpha;
            overlayImage.color = newColor;
        }
        else
        {
            // Daca viata a crescut peste 30%, ascundem efectul activ.
            if (overlayImage.color.a > 0)
            {
                Color newColor = overlayImage.color;

                // Fade-out fluid (Lerp) catre opacitate 0 pentru a preveni disparitia brusca.
                newColor.a = Mathf.Lerp(newColor.a, 0, Time.deltaTime * 5f);
                overlayImage.color = newColor;
            }
        }
    }
}