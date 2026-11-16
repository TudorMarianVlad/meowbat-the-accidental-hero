using UnityEngine;

// Atribut defensiv care asigura prezenta componentelor critice direct pe GameObject
[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class EnemyMeleeAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 1f;
    public float detectionRange = 5f;
    public float stopDistance = 0.8f;
    public LayerMask visionObstacles;

    [Header("Roaming Settings")]
    public float idleMinTime = 1f;
    public float idleMaxTime = 3f;
    public float roamDuration = 2f;

    [Header("Enemy Type")]
    // Flag care transforma acest inamic intr-un inamic de tip Kamikaze (Bomber)
    public bool isSuicideBomber = false;

    [Header("References")]
    public Transform player;
    public EnemyMelee meleeCombat;

    private Rigidbody2D rb;
    private Animator animator;

    private Vector2 roamDirection;
    private float stateTimer;
    private bool isWalking = false;
    private bool isChasing = false;
    private bool isSelfDestructing = false;

    public bool instantChase = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (meleeCombat == null) meleeCombat = GetComponent<EnemyMelee>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        SetIdle();
    }

    void Update()
    {
        // Ignoram update-ul daca inamicul deja se autodistruge
        if (isSelfDestructing || player == null) return;

        // Daca inamicul ataca fizic in acest moment, il blocam pe loc
        if (meleeCombat != null && meleeCombat.isAttacking)
        {
            StopMoving();
            return;
        }

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        if (instantChase || CanSeePlayer())
        {
            isChasing = true;

            if (distToPlayer <= stopDistance)
            {
                // ARBORE DE DECIZIE PENTRU ATAC: Kamikaze vs Melee
                if (isSuicideBomber) StartSelfDestruct();
                else ExecuteStandardAttack(player);
            }
            else
            {
                isWalking = true;
                roamDirection = (player.position - transform.position).normalized;
            }
        }
        else
        {
            if (isChasing)
            {
                isChasing = false;
                SetIdle();
            }
            HandleRoaming();
        }

        UpdateAnimation(isWalking ? roamDirection : Vector2.zero);
    }

    private void StartSelfDestruct()
    {
        if (isSelfDestructing) return;
        isSelfDestructing = true;

        // 1. Lockdown Fizic
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        // 2. Lockdown Animatic
        isWalking = false;
        isChasing = false;
        UpdateAnimation(Vector2.zero);

        // 3. Declansarea Atacului
        animator.SetTrigger("Attack");
        if (meleeCombat != null) meleeCombat.Attack(player);

        // Ii spunem scriptului central sa se ocupe de explozie folosind timerul special de suicid
        EnemyHealth healthScript = GetComponent<EnemyHealth>();
        if (healthScript != null)
        {
            healthScript.TriggerExplosion(healthScript.suicideExplosionDelay);
        }

        // 4. Curatarea Memoriei
        Destroy(gameObject, 0.8f);

        this.enabled = false;
    }

    private void ExecuteStandardAttack(Transform target)
    {
        isWalking = false;
        rb.linearVelocity = Vector2.zero;

        Vector2 faceDir = (target.position - transform.position).normalized;
        animator.SetFloat("LastInputX", faceDir.x);
        animator.SetFloat("LastInputY", faceDir.y);

        meleeCombat.Attack(target);
    }

    void FixedUpdate()
    {
        if (isSelfDestructing) return;
        if (meleeCombat != null && meleeCombat.isAttacking) return;

        rb.MovePosition(rb.position + (isWalking ? roamDirection : Vector2.zero) * moveSpeed * Time.fixedDeltaTime);
    }

    void HandleRoaming()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            if (Random.value > 0.5f) SetIdle();
            else SetRoam();
        }
    }

    void SetIdle()
    {
        isWalking = false;
        roamDirection = Vector2.zero;
        stateTimer = Random.Range(idleMinTime, idleMaxTime);
    }

    void SetRoam()
    {
        isWalking = true;
        roamDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        stateTimer = roamDuration;
    }

    void StopMoving()
    {
        isWalking = false;
        isChasing = false;
        rb.linearVelocity = Vector2.zero;
        UpdateAnimation(Vector2.zero);
    }

    bool CanSeePlayer()
    {
        Vector2 dir = (player.position - transform.position).normalized;
        float dist = Vector2.Distance(transform.position, player.position);

        if (dist > detectionRange) return false;

        RaycastHit2D hit = Physics2D.Raycast((Vector2)transform.position + (dir * 0.2f), dir, dist, visionObstacles);

        return hit.collider == null || hit.collider.transform == player;
    }

    void UpdateAnimation(Vector2 direction)
    {
        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isChasing", isChasing);
        animator.SetFloat("moveX", direction.x);
        animator.SetFloat("moveY", direction.y);

        if (direction != Vector2.zero)
        {
            animator.SetFloat("LastInputX", direction.x);
            animator.SetFloat("LastInputY", direction.y);
        }
    }
}