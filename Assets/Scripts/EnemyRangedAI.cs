using UnityEngine;

// Atribut defensiv care obliga detinerea componentelor de fizica si animatie, 
// eliminand riscul aparitiei erorilor de tip lipsa referinta la rulare.
[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class EnemyRangedAI : MonoBehaviour
{
    [Header("Movement & Range")]
    public float moveSpeed = 1.5f;
    public float detectionRange = 7f;
    // Distanta ideala la care inamicul se opreste pentru a ataca (ex: aruncarea pietrei)
    public float stopDistance = 5f;
    // Distanta critica: daca jucatorul se apropie mai mult de atat, inamicul intra in starea de retragere (Kiting)
    public float retreatDistance = 3f;
    public LayerMask visionObstacles;

    [Header("Roaming")]
    public float idleMinTime = 1f;
    public float idleMaxTime = 3f;
    public float roamDuration = 2f;

    [Header("Combat")]
    public float attackCooldown = 2.5f;
    public GameObject rockPrefab;
    // Punctul de pivot local de unde este instantiat fizic proiectilul
    public Transform shootPoint;
    public float rockSpeed = 8f;

    [Header("AI Settings")]
    // Flag setat de WaveManager pentru a forta inamicul sa ataca direct in Arena
    public bool instantChase = false;

    private Rigidbody2D rb;
    private Animator animator;
    private Transform player;

    private Vector2 moveDirection;
    private float stateTimer;
    private float lastAttackTime;
    private bool isWalking = false;
    private bool isChasing = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Cautare globala automata a jucatorului prin eticheta (Tag Fallback)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        // Initializare in starea pasiva de repaus
        SetIdle();
    }

    void Update()
    {
        if (player == null) return;

        // Calculam distanta absoluta (Euclidiana) dintre inamic si jucator
        float distToPlayer = Vector2.Distance(transform.position, player.position);

        // MASINA DE STARI (Finite State Machine)
        // Verificam daca inamicul vede jucatorul (sau daca este activat modul Arena prin instantChase)
        if (instantChase || CanSeePlayer())
        {
            isChasing = true;
            // Gestionam pozitionarea tactica in functie de distanta
            HandleCombatMovement(distToPlayer);
        }
        else
        {
            // Daca jucatorul a pierdut linia de vizibilitate, AI-ul revine la comportamentul de patrulare
            if (isChasing)
            {
                isChasing = false;
                SetIdle();
            }
            HandleRoaming();
        }

        // Sincronizam starile logice cu arborele de animatii (Blend Tree)
        UpdateAnimation();
    }

    // Algoritmul principal de pozitionare tactica (Kiting/Retreat Logic)
    void HandleCombatMovement(float dist)
    {
        // Calculam vectorul directional orientat catre jucator si il normalizam (lungime egala cu 1)
        Vector2 dirToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;

        // Situatia A: Jucatorul este prea departe -> Inamicul inainteaza spre el
        if (dist > stopDistance)
        {
            moveDirection = dirToPlayer;
            isWalking = true;
        }
        // Situatia B: Jucatorul este prea aproape (sub retreatDistance) -> Inamicul da inapoi (Kiting)
        // Inmultim vectorul cu -1 pentru a inversa complet directia de mers
        else if (dist < retreatDistance)
        {
            moveDirection = -dirToPlayer;
            isWalking = true;
        }
        // Situatia C: Inamicul se afla in zona perfecta (intre retreatDistance si stopDistance)
        else
        {
            // Se opreste de pe loc si anuleaza viteza fizica a corpului
            moveDirection = Vector2.zero;
            isWalking = false;
            rb.linearVelocity = Vector2.zero;

            // Verificam daca a trecut timpul de reincarcare al abilitatii (Attack Cooldown)
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                // Declansam animatia de atac. Aruncarea efectiva a pietrei va fi gestionata 
                // de un Animation Event care va apela metoda LaunchRock() de mai jos.
                animator.SetTrigger("Attack");
                lastAttackTime = Time.time;
            }
        }
    }

    void FixedUpdate()
    {
        // Translatia fizica se executa exclusiv in FixedUpdate pentru a asigura fluiditatea 
        // interactiunii cu colizoarele hartii (Tilemap Composite Collider)
        if (isWalking)
        {
            rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime);
        }
    }

    // --- METODA DECLANSATA PRIN EVENIMENT DE ANIMATIE (Animation Event) ---
    // Sincronizeaza generarea proiectilului cu momentul exact in care inamicul executa miscarea de aruncare.
    public void LaunchRock()
    {
        if (rockPrefab == null || shootPoint == null) return;

        // Instantiem o clona a pietrei in pozitia curenta a punctului de tragere
        GameObject rock = Instantiate(rockPrefab, shootPoint.position, Quaternion.identity);

        // Recalculam directia exacta de la punctul de tragere catre corpul jucatorului
        Vector2 shootDir = ((Vector2)player.position - (Vector2)shootPoint.position).normalized;

        // Extragem componenta logica a proiectilului (EnemyProjectile)
        EnemyProjectile proj = rock.GetComponent<EnemyProjectile>();
        if (proj != null)
        {
            // Transmitem vectorul de miscare si setam valoarea daunelor (ex: 15 puncte de daune)
            proj.Setup(shootDir, 15);
        }
    }

    void HandleRoaming()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            // Alegere aleatorie cu probabilitate de 50% intre starea de veghe si miscare
            if (Random.value > 0.5f) SetIdle();
            else SetRoam();
        }
    }

    void SetIdle()
    {
        isWalking = false;
        moveDirection = Vector2.zero;
        stateTimer = Random.Range(idleMinTime, idleMaxTime);
    }

    void SetRoam()
    {
        isWalking = true;
        moveDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        stateTimer = roamDuration;
    }

    // Calculeaza linia de vizibilitate directa (Raycasting 2D) pentru a preveni vederea prin perete
    bool CanSeePlayer()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > detectionRange) return false;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;

        // Trimitem o raza fizica invizibila. Masca 'visionObstacles' ignora jucatorul sau inamicii 
        // si verifica doar daca raza loveste un element static din decor (Environment).
        RaycastHit2D hit = Physics2D.Raycast((Vector2)transform.position + (dir * 0.2f), dir, dist, visionObstacles);

        return hit.collider == null || hit.collider.transform == player;
    }

    void UpdateAnimation()
    {
        animator.SetBool("isWalking", isWalking);

        // Determinam directia de orientare a fetei: daca urmareste, priveste spre jucator, 
        // daca patruleaza, priveste in directia de mers.
        Vector2 faceDir = isChasing ? ((Vector2)player.position - (Vector2)transform.position).normalized : moveDirection;

        if (faceDir != Vector2.zero)
        {
            animator.SetFloat("moveX", faceDir.x);
            animator.SetFloat("moveY", faceDir.y);
            animator.SetFloat("LastInputX", faceDir.x);
            animator.SetFloat("LastInputY", faceDir.y);

            // --- CORECTIA DINAMICA A PUNCTULUI DE TRAGERE (ShootPoint Flipping) ---
            // Deoarece inamicul isi schimba orientarea vizuala, punctul local de spawn al pietrei 
            // trebuie oglindit matematic pe axa X, altfel piatra ar aparea din spatele lui cand trage spre stanga.
            if (shootPoint != null)
            {
                Vector3 localPos = shootPoint.localPosition;

                // Fortam semnul coordonatei X local: negativ daca priveste spre stanga (x < 0), 
                // respectiv pozitiv daca priveste spre dreapta (x > 0).
                float targetX = Mathf.Abs(localPos.x) * (faceDir.x < 0 ? -1 : 1);

                // Aplicam noua pozitie locala modificata
                shootPoint.localPosition = new Vector3(targetX, localPos.y, localPos.z);
            }
        }
    }
}