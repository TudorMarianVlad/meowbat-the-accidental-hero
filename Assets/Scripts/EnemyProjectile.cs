using UnityEngine;

// Clasa EnemyProjectile controleaza comportamentul fizic si interactiunile logice
// ale proiectilelor generate de inamici (ex: pietrele aruncate sau alte atacuri la distanta).
public class EnemyProjectile : MonoBehaviour
{
    public float moveSpeed = 10f;
    public int damage = 10;
    // Forta de respingere aplicata jucatorului cand este lovit de acest proiectil
    public float knockbackThrust = 5f;

    private Rigidbody2D rb;

    void Awake()
    {
        // Preluam componenta fizica necesara manipularii vectorilor de viteza
        rb = GetComponent<Rigidbody2D>();
    }

    // Metoda publica de initializare apelata direct de inamicul care lanseaza proiectilul (ex: Shroom).
    // Transmite directia calculata spre jucator si valoarea daunelor pe care inamicul le provoaca.
    public void Setup(Vector2 direction, int projectileDamage)
    {
        damage = projectileDamage;

        // Stabilim viteza liniara a corpului fizic pe directia normalizata (lungime 1) inmultita cu viteza nominala
        rb.linearVelocity = direction.normalized * moveSpeed;

        // Trigonometrie 2D: Calculam unghiul de rotatie bazat pe vectorul directiei (Atan2),
        // il convertim in grade (* Mathf.Rad2Deg) si rotim proiectilul pe axa Z pentru a fi orientat spre tinta.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // GESTIUNE DEFENSIVA A MEMORIEI (Garbage Collection avoidance)
        // In caz ca proiectilul rateaza tinta si iese in afara hartii, il distrugem automat dupa 5 secunde.
        // Prevenim astfel acumularea infinita de obiecte in ierarhie si scaderea performantei jocului.
        Destroy(gameObject, 5f);
    }

    // Rutina apelata automat de motorul fizic 2D (Unity Physics2D) cand colizorul setat ca Trigger intersecteaza alt obiect.
    void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. INTERACTIUNEA CU JUCATORUL
        // Folosim CompareTag pentru o verificare optimizata la nivel de string-uri in Unity
        if (collision.CompareTag("Player"))
        {
            // Extragem registrul central de statistici al jucatorului pentru a-i scadea viata
            PlayerStats stats = collision.GetComponent<PlayerStats>();
            if (stats != null) stats.TakeDamage(damage);

            // Extragem scriptul de control al jucatorului pentru a-i aplica impulsul fizic de Knockback
            PlayerController pc = collision.GetComponent<PlayerController>();
            if (pc != null) pc.Knockback(transform, knockbackThrust);

            // Distrugem proiectilul imediat dupa impact pentru a nu trece prin jucator si a provoca daune multiple
            Destroy(gameObject);
        }
        // 2. INTERACTIUNEA CU MEDIUL (Ziduri, Obstacole statice)
        // Verificam daca obiectul lovit apartine stratului fizic al mediului inconjurator
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            // Distrugem piatra la contactul cu un perete pentru a simula spargerea/oprirea ei naturala
            Destroy(gameObject);
        }
    }
}