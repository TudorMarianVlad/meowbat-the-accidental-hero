using UnityEngine;
using UnityEngine.Rendering.Universal;

// Gestioneaza ciclul zi-noapte prin modificarea culorii luminii globale 
// si activarea sistemelor de particule (licurici/fluturi) in functie de timp.
public class DayNightCycle : MonoBehaviour
{
    [Header("Light Settings")]
    public Light2D globalLight;
    public Gradient dailyColorGradient;

    [Header("Time Settings")]
    public float dayDurationInSeconds = 120f;
    public float currentTime = 0f;

    [Header("Fireflies (Night)")]
    public ParticleSystem fireflies;
    [Range(0f, 1f)] public float fireflyStart = 0.75f;
    [Range(0f, 1f)] public float fireflyEnd = 0.25f;

    [Header("Butterflies (Day)")]
    public ParticleSystem butterflies;

    void Awake()
    {
        // Initializare referinta lumina.
        if (globalLight == null) globalLight = GetComponent<Light2D>();
    }

    void Update()
    {
        // 1. Avansare ceas.
        currentTime += Time.deltaTime;
        if (currentTime >= dayDurationInSeconds) currentTime = 0f;

        // 2. Calcul procentaj timp curent.
        float timePercentage = currentTime / dayDurationInSeconds;

        // 3. Actualizare culoare lumina globala via Gradient.
        if (globalLight != null)
        {
            globalLight.color = dailyColorGradient.Evaluate(timePercentage);
        }

        // 4. Logica Ambient Life: determinare stare zi/noapte.
        bool isNight = timePercentage >= fireflyStart || timePercentage <= fireflyEnd;

        // Gestionare particule licurici.
        if (fireflies != null)
        {
            if (isNight && !fireflies.isPlaying) fireflies.Play();
            else if (!isNight && fireflies.isPlaying) fireflies.Stop();
        }

        // Gestionare particule fluturi.
        if (butterflies != null)
        {
            if (!isNight && !butterflies.isPlaying) butterflies.Play();
            else if (isNight && butterflies.isPlaying) butterflies.Stop();
        }
    }
}