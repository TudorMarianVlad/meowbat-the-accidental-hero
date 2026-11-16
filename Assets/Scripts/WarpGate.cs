using UnityEngine;
using Unity.Cinemachine;

// Gestioneaza teleportarea jucatorului intre zone (Hub/Arena), 
// reconfigurarea limitelor camerei si activarea secventelor de lupta (WaveManager).
public class WarpGate : MonoBehaviour
{
    public WaveManager waveManager;

    [Header("Destination")]
    public Transform targetLandingPad;

    [Header("Camera Settings")]
    public Collider2D newCameraBoundary;

    [Header("Location State Control")]
    public bool isEnteringArena; // True: spre Arena, False: inapoi in Hub.

    private static float lastTeleportTime = 0f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Guard Clause: prevenire teleportari accidentale multiple (Bouncing).
        if (Time.time - lastTeleportTime < 1f) return;

        if (collision.CompareTag("Player"))
        {
            lastTeleportTime = Time.time;

            // 1. Actualizare stare in sistemul de salvare.
            PlayerStats stats = collision.GetComponent<PlayerStats>();
            if (stats != null) stats.isInArena = isEnteringArena;

            // 2. Relocare fizica: resetare viteza si pozitionare.
            collision.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            collision.transform.position = targetLandingPad.position;

            // 3. Reconfigurare camera (Cinemachine).
            CinemachineCamera vcam = FindFirstObjectByType<CinemachineCamera>();
            if (vcam != null)
            {
                var confiner = vcam.GetComponent<CinemachineConfiner2D>();
                if (confiner != null && newCameraBoundary != null)
                {
                    confiner.BoundingShape2D = newCameraBoundary;
                    confiner.InvalidateBoundingShapeCache();
                }
                vcam.OnTargetObjectWarped(collision.transform, targetLandingPad.position - transform.position);
            }

            // 4. Activare sistem combat (daca intra in arena).
            if (isEnteringArena && waveManager != null)
            {
                waveManager.StartArena();
            }
        }
    }
}