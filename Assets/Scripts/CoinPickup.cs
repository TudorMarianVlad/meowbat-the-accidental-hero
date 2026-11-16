using UnityEngine;

// Gestioneaza colectarea resurselor economice (monede).
// Necesita atasarea pe un GameObject cu un colizor setat ca "IsTrigger".
public class CoinPickup : MonoBehaviour
{
    [Header("Coin Settings")]
    // Valoarea monedei, ajustabila direct din Inspector.
    public int goldValue = 1;

    // Metoda apelata automat de Physics2D la detectarea unei coliziuni (trigger).
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verificam optimizat daca obiectul care a declansat evenimentul este jucatorul.
        if (collision.CompareTag("Player"))
        {
            // Preluam componenta centrala a jucatorului.
            PlayerStats stats = collision.GetComponent<PlayerStats>();

            // Verificare de siguranta (Null Check) pentru prevenirea erorilor.
            if (stats != null)
            {
                // Transmitem valoarea catre PlayerStats, care va gestiona datele si UI-ul.
                stats.AddGold(goldValue);

                // Eliminam moneda din scena instantaneu pentru a preveni colectarile multiple (exploit).
                Destroy(gameObject);
            }
        }
    }
}