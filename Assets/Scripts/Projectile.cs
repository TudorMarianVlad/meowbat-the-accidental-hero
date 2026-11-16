using UnityEngine;

// Gestioneaza comportamentul proiectilelor: viteza, orientarea trigonometrica, 
// detecția impactului si prevenirea erorilor de auto-lovire (Friendly Fire).
public class Projectile : MonoBehaviour
{
    public float moveSpeed = 10f;
    public int damage = 10;

    [Header("Fix Diagonal Sprite")]
    // Offset pentru alinierea sprite-ului cu vectorul de miscare.
    public float rotationOffset = 0f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Initializeaza viteza, directia si daunele proiectilului.
    public void Setup(Vector2 direction, int weaponDamage)
    {
        damage = weaponDamage;

        // Aplicare viteza liniara.
        rb.linearVelocity = direction.normalized * moveSpeed;

        // Calcul trigonometric pentru alinierea proiectilului cu directia tragerii.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);

        // Distrugere automata dupa 5 secunde pentru eliberarea memoriei.
        Destroy(gameObject, 5f);
    }

    // Gestionare coliziuni (Trigger).
    void OnTriggerEnter2D(Collider2D collision)
    {
        // Guard Clause: ignoram coliziunea cu jucatorul (prevenire daune proprii).
        if (collision.CompareTag("Player")) return;

        // Detectie inamic si aplicare daune.
        EnemyHealth enemy = collision.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
        }
        // Detectie impact cu decorul (Environment).
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            Destroy(gameObject);
        }
    }
}