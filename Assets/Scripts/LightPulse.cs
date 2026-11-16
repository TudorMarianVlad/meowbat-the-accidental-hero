using UnityEngine;
using UnityEngine.Rendering.Universal;

// Gestioneaza efectul de pulsare (oscilatie) a intensitatii luminii 2D.
public class LightPulse : MonoBehaviour
{
    [Header("Settings")]
    public Light2D targetLight;
    public float pulseSpeed = 2f;
    public float minIntensity = 1.5f;
    public float maxIntensity = 3.5f;

    void Awake()
    {
        // Initializare defensiva pentru componenta Light2D.
        if (targetLight == null) targetLight = GetComponent<Light2D>();
    }

    void Update()
    {
        if (targetLight != null)
        {
            // Calcul oscilatie folosind Mathf.PingPong pentru un efect de bouncing continuu.
            float pingPong = Mathf.PingPong(Time.time * pulseSpeed, maxIntensity - minIntensity);

            // Aplicare valoare calculata peste intensitatea minima.
            targetLight.intensity = minIntensity + pingPong;
        }
    }
}