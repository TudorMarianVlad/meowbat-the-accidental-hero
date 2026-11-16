using UnityEngine;

// Gestioneaza armele la distanta: orbitarea, calculul traiectoriei 
// si instantierea proiectilelor pe baza resurselor din inventar.
public class BowWeapon : MonoBehaviour
{
    [Header("References")]
    // Punctul de generare a proiectilului.
    public Transform firePoint;
    public Animator animator;

    [Header("Orbit Settings")]
    // Raza de orbitare in jurul jucatorului.
    public float rotateRadius = 1.0f;
    // Cooldown-ul implicit intre trageri.
    public float timeBetweenShots = 0.5f;

    [Header("Fixes")]
    // Offset pentru centrarea orbitei pe trunchiul caracterului.
    public Vector3 centerOffset = new Vector3(0, 0.5f, 0);
    // Corectie unghiulara pentru alinierea corecta a sprite-ului.
    public float rotationOffset = -45f;

    // Timpul permis pentru urmatoarea tragere.
    private float nextAttackTime = 0f;
    private PlayerCombat playerCombat;
    private Inventory inventory;

    // Rata de tragere (upgradabila).
    public float fireRate = 1.0f;

    void Start()
    {
        // Preluare referinte din parinte.
        playerCombat = GetComponentInParent<PlayerCombat>();
        inventory = GetComponentInParent<Inventory>();

        // Initializare defensiva Animator.
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 1. Verificare pauza globala.
        if (PauseManager.isPaused) return;

        // 2. Actualizarea continua a rotatiei pe orbita.
        HandleRotation();

        // 3. UI Shielding: Blocare actiuni daca meniurile sunt deschise.
        if (playerCombat != null)
        {
            if (playerCombat.inventoryUIPanel != null && playerCombat.inventoryUIPanel.activeInHierarchy) return;
            if (playerCombat.shopUIPanel != null && playerCombat.shopUIPanel.activeInHierarchy) return;
        }

        // 4. Verificare input pentru tragere.
        HandleShooting();
    }

    // Calculeaza matematica din spatele rotatiei si orbitarii.
    void HandleRotation()
    {
        // Conversie cursor din Screen in World Space.
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        // Calcul centru orbita.
        Vector3 circleCenter = transform.parent.position + centerOffset;

        // 1. Distanta pentru Deadzone.
        float distance = Vector3.Distance(circleCenter, mousePos);

        // 2. Prevenire Jitter: actualizam doar daca distanta > 0.2f.
        if (distance > 0.2f)
        {
            // Normalizare vector directie.
            Vector3 direction = (mousePos - circleCenter).normalized;

            // 3. Aplicare pozitie pe orbita.
            transform.position = circleCenter + (direction * rotateRadius);

            // 4. Calcul unghi (Atan2) si aplicare rotatie cu offset.
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
        }
    }

    // Gestioneaza cadenta de tragere (Cooldown).
    void HandleShooting()
    {
        // Verificare input tragere si trecerea timpului de cooldown.
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            Shoot();
            nextAttackTime = Time.time + fireRate;
        }
    }

    // Imbunatateste performanta armei (apelata din Shop).
    public void UpgradeFireRate()
    {
        // Reducere timp de tragere.
        fireRate -= 0.1f;

        // Plafonare valoare minima la 0.2s.
        fireRate = Mathf.Max(fireRate, 0.2f);

        // Actualizare UI.
        PlayerStats stats = GetComponentInParent<PlayerStats>();
        if (stats != null) stats.UpdateUI();
    }

    // Functia principala de instantiere a proiectilului.
    void Shoot()
    {
        // 1. Consum munitie din inventar.
        bool hasAmmo = inventory.UseAmmo("Arrow");

        // Oprire executie daca nu exista munitie.
        if (!hasAmmo)
        {
            Debug.Log("Fara sageti in inventar!");
            return;
        }

        // 2. Declansare animatie de tragere.
        if (animator != null) animator.SetTrigger("Shoot");

        // 3. Instantiere proiectil din ScriptableObject.
        if (inventory.currentItemData != null && inventory.currentItemData.projectilePrefab != null)
        {
            // Creare clona proiectil.
            GameObject arrowObj = Instantiate(inventory.currentItemData.projectilePrefab, firePoint.position, transform.rotation);
            Projectile arrowScript = arrowObj.GetComponent<Projectile>();

            if (arrowScript != null)
            {
                // Calcul directie stabila spre cursor.
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0f;
                Vector3 circleCenter = transform.parent.position + centerOffset;
                Vector2 stableDirection = (mousePos - circleCenter).normalized;

                // Transmitere viteza si daune catre scriptul proiectilului.
                arrowScript.moveSpeed = inventory.currentItemData.launchForce;
                arrowScript.Setup(stableDirection, playerCombat.attackDamage);
            }
        }
    }
}