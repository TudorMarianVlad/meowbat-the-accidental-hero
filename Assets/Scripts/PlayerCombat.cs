using UnityEngine;
using System.Collections;

// Clasa PlayerCombat actioneaza ca hub central pentru actiunile ofensive si utilitare ale jucatorului.
// Gestioneaza declansarea atacurilor melee prin detectarea coliziunilor circulare, controlul cadentei,
// sistemul de protectie a interfetelor UI si corutina de consumare a obiectelor cu incetinire tactica.
public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    public Inventory inventory;
    public PlayerController playerMovement;
    public PlayerStats playerStats;
    private PopupManager popupManager;

    [Header("Settings")]
    // Viteza la care va fi incetinit jucatorul in timp ce consuma o potiune (Game Feel / Risc Tactic)
    public float slowSpeed = 2f;
    public Animator animator;
    // Punctul fizic din scena unde se genereaza cercul de atac (hitbox-ul sabiei)
    public Transform attackPoint;
    public float attackRange = 0.5f;
    // Masca de straturi folosita pentru a detecta prin coliziuni doar obiectele din stratul inamicilor
    public LayerMask enemyLayers;

    [Header("Combat Settings")]
    public float attackCooldown = 0.5f;
    // Daunele de baza ale jucatorului, stocate centralizat aici pentru a fi citite de arme
    public int attackDamage = 20;

    [Header("UI Panels to Block Attacks")]
    // Referinte folosite pentru implementarea sablonului "UI Shielding" (Scutul Interfetei)
    public GameObject inventoryUIPanel;
    public GameObject shopUIPanel;

    // Flag de stare folosit ca lacat (lock) pentru a impiedica dubla consumare simultana
    private bool isConsuming = false;

    void Awake()
    {
        // Initializare defensiva: cache-uim componentele de pe acelasi GameObject pentru performanta
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerController>();
        popupManager = GetComponent<PopupManager>();
    }

    void Update()
    {
        //Daca jocul este pus pe pauza globala, oprim orice proces de lupta
        if (PauseManager.isPaused) return;

        // Detectam apasarea butonului principal de atac (Click Stanga)
        if (Input.GetMouseButtonDown(0))
        {
            
            // Daca inventarul sau magazinul sunt deschise pe ecran, oprim executia (return).
            // Acest pas previne un bug critic unde jucatorul ar fi atacat sau ar fi baut o potiune 
            // in timp ce dadea click pe un slot de echipament sau pe un buton din magazin.
            if (inventoryUIPanel != null && inventoryUIPanel.activeInHierarchy) return;
            if (shopUIPanel != null && shopUIPanel.activeInHierarchy) return;
            if (isConsuming) return; // Blocam atacul daca jucatorul este in mijlocul actiunii de consum

            if (inventory.currentItemData != null)
            {
                // Cazul A: Obiectul selectat in Hotbar este o arma
                if (inventory.currentItemData.itemType == ItemType.Weapon)
                {
                    // Daca NU este arma la distanta (deci e sabie), executam atacul standard de proximitate
                    if (!inventory.currentItemData.isRangedWeapon) Attack();
                }
                // Cazul B: Obiectul detine proprietati de vindecare (este o potiune/mancare)
                else if (inventory.currentItemData.healAmount > 0)
                {
                    // Pornim firul asincron (Coroutine) pentru secventa de consum tactici
                    StartCoroutine(ConsumeItemRoutine());
                }
            }
        }
    }

    // Corutina care gestioneaza starile temporale ale consumului unui obiect utilitar
    IEnumerator ConsumeItemRoutine()
    {
        isConsuming = true;

        if (playerMovement != null)
        {
            // Salvam viteza originala a jucatorului din PlayerController pentru a o putea restaura ulterior
            float originalSpeed = playerMovement.moveSpeed;
            // Activam flag-ul 'isBusy' pe controller pentru a bloca alte mecanici (cum ar fi Dash-ul)
            playerMovement.isBusy = true;
            // Incetinim viteza de deplasare a personajului
            playerMovement.moveSpeed = slowSpeed;

            // Actualizam UI-ul barelor pentru a reflecta vizual modificarea statisticilor
            if (playerStats != null) playerStats.UpdateUI();
            // Injumatatim viteza animatiilor pentru a simula vizual efortul de consum (ex: bea incet)
            if (animator != null) animator.speed = 0.5f;

            // Asteptam 0.5 secunde (timpul fizic in care personajul "mananca")
            yield return new WaitForSeconds(0.5f);

            // Verificam din nou validitatea datelor in caz ca obiectul a fost modificat intre timp
            if (inventory != null && inventory.currentItemData != null)
            {
                int heal = inventory.currentItemData.healAmount;
                // Aplicam vindecarea in PlayerStats
                playerStats.Heal(heal);

                // Instantiem un indicator plutitor verde deasupra jucatorului cu valoarea primita
                if (popupManager != null) popupManager.Show($"+{heal}", Color.green);

                // Eliminam exact o unitate din slotul activ selectat pe Hotbar
                inventory.RemoveItem(inventory.GetSelectedSlotIndex(), 1);
            }

            // Mica pauza suplimentara (0.2 secunde) pentru finalizarea animatiei (Recovery Time)
            yield return new WaitForSeconds(0.2f);
            // Readucem viteza din controller la valoarea sa nativa de dinaintea consumului
            playerMovement.moveSpeed = originalSpeed;
            if (playerStats != null) playerStats.UpdateUI();
            // Resetam viteza animatiilor la valoarea nominala (1.0)
            if (animator != null) animator.speed = 1f;
            playerMovement.isBusy = false;
        }

        isConsuming = false;
    }

    // Functia principala de atac corp-la-corp (Melee Combat Detection)
    void Attack()
    {
        // Declansam animatia din Aseprite prin parametrul de tip Trigger
        if (animator != null) animator.SetTrigger("Attack");

        // Generam o sfera (cerc 2D) invizibila in punctul attackPoint cu raza attackRange.
        // Filtrul enemyLayers asigura o optimizare masiva: motorul de fizica Physics2D ignora 
        // decorul, zidurile sau jucatorul si returneaza doar colizorii inamicilor prinsi in raza.
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        // Iteram prin masivul de inamici loviti
        foreach (Collider2D enemy in hitEnemies)
        {
            // Extragem componenta de viata a inamicului
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // Aplicam daunele stocate centralizat in aceasta clasa
                enemyHealth.TakeDamage(attackDamage);
            }
        }
    }

    // Functie utilitara rulata exclusiv in editorul Unity. Deseneaza raza hitbox-ului sabiei
    // ca o sfera cu linii (WireSphere) in fereastra Scene pentru a ajuta la calibrarea distantelor.
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red; // Setam culoarea rosie pentru vizualizarea atacului
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}