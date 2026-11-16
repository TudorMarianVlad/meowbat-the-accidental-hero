using UnityEngine;

// Clasa EnemyHealerAI controleaza comportamentul decizional al unui inamic de tip suport.
// Utilizeaza un sistem bazat pe stari (Roaming, Chasing, Healing) pentru a asista 
// dinamic alte entitati inamice de pe harta.
public class EnemyHealerAI : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    // Distanta la care AI-ul se opreste fata de tinta pentru a initia vindecarea (Buffer zone)
    public float stopDistance = 2.5f;

    [Header("Healing Settings")]
    public float healCooldown = 3f;
    // Raza efectului zonal de vindecare (Area of Effect - AoE)
    public float healRadius = 4f;
    public int healAmount = 20;

    [Header("Roaming Settings")]
    // Parametrii pentru starea de patrulare aleatorie (Wander/Roam)
    public float roamRadius = 5f;
    public float roamWaitTime = 2f;
    private Vector2 roamTarget;
    private float nextRoamTime;
    private bool isRoaming = false;

    [Header("AI Settings")]
    // Flag care forteaza inamicul sa caute imediat o tinta la instantiere, 
    // sarind peste faza initiala de asteptare/patrulare.
    public bool instantChase = false;

    // Referinte si variabile de stare interna
    private EnemyHealth targetHealth;
    private float lastHealTime;
    private Rigidbody2D rb;
    private Animator animator;
    private bool isHealing = false;
    private float lastExecutionTime;

    void Start()
    {
        // Extragerea componentelor necesare pentru fizica si animatie
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (instantChase)
        {
            // Fortam scanarea mediului inca din primul cadru (frame)
            FindClosestHurtAlly();
        }
    }

    void Update()
    {
        // 1. BLOCAJE DE STARE (State Locks)
        // Daca inamicul curent a murit, oprim fizica si iesim din bucla (Early Return).
        if (GetComponent<EnemyHealth>().isDead) { StopMoving(); return; }

        // Daca animatia de vindecare ruleaza deja pe stratul 0 (Base Layer),
        // blocam miscarea pentru a nu glisa pe suprafata in timp ce face animatia.
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("HealState")) { StopMoving(); return; }

        // 2. SCANAREA MEDIULUI
        // Actualizam constant tinta. Chiar daca urmareste un inamic, daca un altul 
        // scade la o viata mai mica intre timp, AI-ul isi va schimba dinamic prioritatea.
        FindClosestHurtAlly();

        // 3. ARBORELE DE DECIZIE (Decision Tree)
        if (targetHealth != null)
        {
            // STAREA DE ASISTENTA (Chase/Heal Logic)
            isRoaming = false;

            // Calculam distanta Euclidiana pana la pacientul identificat
            float dist = Vector2.Distance(transform.position, targetHealth.transform.position);

            // Daca suntem prea departe, ne deplasam catre el, altfel declansam vindecarea
            if (dist > stopDistance) MoveToAlly();
            else TryHeal();
        }
        else
        {
            // STAREA DE PATRULARE (Roam Logic)
            // Daca toti aliatii sunt la viata maxima, AI-ul executa un comportament pasiv.
            RoamAround();
        }

        // 4. ACTUALIZAREA VIZUALA
        UpdateAnimations();
    }

    void RoamAround()
    {
        // Verificam daca tocmai am ajuns la destinatia aleatorie sau daca nu avem inca o destinatie
        if (!isRoaming || Vector2.Distance(transform.position, roamTarget) < 0.2f)
        {
            StopMoving();
            isRoaming = false;

            // Logica de temporizare: Asteptam sa treaca timpul de odihna plus un factor aleator 
            // pentru a face miscarea sa para organica, nu robotica.
            if (Time.time >= nextRoamTime)
            {
                // Generam un punct aleatoriu intr-un cerc imaginar in jurul pozitiei curente
                Vector2 randomDir = Random.insideUnitCircle * roamRadius;
                roamTarget = (Vector2)transform.position + randomDir;

                // Setam urmatorul moment in care AI-ul va avea voie sa aleaga alt punct
                nextRoamTime = Time.time + roamWaitTime + Random.Range(0, 2f);
                isRoaming = true;
            }
        }
        else
        {
            // Ne deplasam catre punctul de patrulare
            Vector2 dir = (roamTarget - (Vector2)transform.position).normalized;

            // Injumatatim viteza in timpul patrularii pentru un efect de relaxare (Wander)
            rb.linearVelocity = dir * (moveSpeed * 0.5f);

            // Trimitem directia miscarii catre Animator pentru a afisa sprite-ul corect
            animator.SetFloat("moveX", dir.x);
            animator.SetFloat("moveY", dir.y);
        }
    }

    // Functia utilizeaza un algoritm de tip "Greedy" pentru a gasi aliatul cu viata absoluta cea mai mica
    void FindClosestHurtAlly()
    {
        // Preluam toti inamicii din scena. Folosim FindObjectsSortMode.None pentru optimizarea memoriei,
        // evitand o sortare interna inutila a motorului Unity.
        EnemyHealth[] allEnemies = GameObject.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        // Setam valoarea initiala la infinit pentru a garanta ca prima verificare va suprascrie valoarea
        float lowestHealth = Mathf.Infinity;
        EnemyHealth bestTarget = null;

        // Iteram prin matricea de inamici
        foreach (EnemyHealth enemy in allEnemies)
        {
            // 1. Ignoram auto-selectia (nu se vindeca pe sine) si ignoram entitatile deja moarte
            if (enemy.gameObject == gameObject || enemy.currentHealth <= 0) continue;

            // 2. Selectam doar entitatile care au suferit daune (viata curenta e mai mica decat maximul)
            if (enemy.currentHealth < enemy.maxHealth)
            {
                // 3. Comparatia efectiva: prioritizam inamicul cel mai aproape de moarte (lowest health)
                if (enemy.currentHealth < lowestHealth)
                {
                    lowestHealth = enemy.currentHealth;
                    bestTarget = enemy;
                }
            }
        }

        // Atribuim rezultatul. Daca nimeni nu e ranit, va deveni null, trecand AI-ul in starea Roam.
        targetHealth = bestTarget;
    }

    void TryHeal()
    {
        StopMoving();

        // Validam cadenta de vindecare (Cooldown) pentru a nu face spam cu abilitatea
        if (Time.time >= lastHealTime + healCooldown)
        {
            isHealing = true;

            // Declansam parametrul din Animator. Acesta va porni animatia de cast.
            animator.SetTrigger("HealTrigger");

            // Inregistram momentul declansarii pentru a bloca incercarile ulterioare pana expira cooldown-ul
            lastHealTime = Time.time;
        }
    }

    void MoveToAlly()
    {
        isHealing = false;

        // Calculam vectorul de directie catre aliat si il normalizam
        Vector2 dir = ((Vector2)targetHealth.transform.position - (Vector2)transform.position).normalized;

        // Logica de "Panica" (Ternary Operator):
        // Daca tinta are sub 20% viata critica, Healer-ul primeste un impuls de viteza (1.5x) pentru a ajunge mai repede.
        float currentSpeed = (targetHealth.currentHealth < targetHealth.maxHealth * 0.2f) ? moveSpeed * 1.5f : moveSpeed;

        // Aplicam forta de miscare direct pe corpul fizic
        rb.linearVelocity = dir * currentSpeed;
    }

    void StopMoving()
    {
        // Anulam inertia pentru o oprire instantanee
        rb.linearVelocity = Vector2.zero;
    }

    // Aceasta metoda este declansata dintr-un frame specific al animatiei
    // garantand o sincronizare perfecta intre grafica si aplicarea efectului logic.
    public void ExecuteAreaHeal()
    {
        // 1. Mecanism de protectie (Debounce / Anti-Spam)
        // Previne un bug clasic din Unity in care Animation Events pot fi apelate de doua ori 
        // in aceeasi tranzitie datorita amestecului de stari (Blend Trees).
        if (Time.time < lastExecutionTime + 0.5f) return;
        lastExecutionTime = Time.time;

        Debug.Log("Area Heal Executed once!");

        // 2. Calculul Zonei de Efect (AoE - Area of Effect)
        // Generam un cerc invizibil fizic si stocam toti colizorii inamicilor care se afla in interior
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, healRadius);

        // Aplicam vindecarea pe fiecare inamic prins in raza
        foreach (var hit in hitColliders)
        {
            EnemyHealth health = hit.GetComponent<EnemyHealth>();

            // Validam componenta si ne asiguram din nou ca Healer-ul nu se vindeca pe el insusi
            if (health != null && health.gameObject != gameObject)
            {
                health.Heal(healAmount);
            }
        }

        // Resetam flag-ul de stare pentru a permite miscarea ulterioara
        isHealing = false;
    }

    void UpdateAnimations()
    {
        // Daca vectorul vitezei are o magnitudine semnificativa, inseamna ca AI-ul este in miscare
        bool isMoving = rb.linearVelocity.magnitude > 0.1f;
        animator.SetBool("isWalking", isMoving);

        // Sincronizam starea de vindecare pentru a mentine arborele de animatie corect
        animator.SetBool("isHealing", isHealing);
    }

    // Functie utilitara pentru editorul Unity. Deseneaza raza de vindecare in fereastra Scene,
    // facilitand testarea vizuala (Game Design Tooling).
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, healRadius);
    }
}