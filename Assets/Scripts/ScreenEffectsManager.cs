using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class ScreenEffectsManager : MonoBehaviour
{
    // Singleton instanța pentru acces global
    public static ScreenEffectsManager instance;

    public Volume globalVolume;
    private ChromaticAberration chromaticAberration;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        if (globalVolume != null && globalVolume.profile.TryGet(out chromaticAberration))
        {
            chromaticAberration.intensity.value = 0f;
        }
        
    }

    // Funcție publică pe care o poate apela orice script din joc
    public void TriggerChromaticAberration(float maxIntensity = 1f, float fadeDuration = 0.5f)
    {
        if (chromaticAberration != null)
        {
            StopAllCoroutines(); // Oprim orice altă distorsiune care rulează deja
            StartCoroutine(FadeAberration(maxIntensity, fadeDuration));
        }
    }

    private IEnumerator FadeAberration(float maxIntensity, float duration)
    {
        float timer = 0f;

        // Lovitura instantanee (Spike-ul la intensitate maximă)
        chromaticAberration.intensity.value = maxIntensity;

        // Estomparea treptată înapoi la 0
        while (timer < duration)
        {
            timer += Time.deltaTime;
            chromaticAberration.intensity.value = Mathf.Lerp(maxIntensity, 0f, timer / duration);
            yield return null;
        }

        chromaticAberration.intensity.value = 0f;
    }
}