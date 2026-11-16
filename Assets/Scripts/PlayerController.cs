using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Obligatoriu pentru utilizarea noului API bazat pe harti de actiuni (PlayerControls)

// Clasa PlayerController reprezinta motorul cinematic al jucatorului.
// Gestioneaza translatia pe baza de fizica Rigidbody2D, sistemele de Sprint si Dash (eschiva),
// orientarea dinamica a sprite-ului spre mouse, generarea de umbre (Ghost Trail) si fizica de Knockback.
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    // Viteza nominala de miscare a jucatorului. Modificata dinamic de magazinul de upgrade-uri.
    public float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.6f;
    // Costul de stamina consumat per secunda in timpul alergarii rapide
    [SerializeField] private float sprintCost = 15f;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 25f;
    // Durata fizica in secunde a starii de invulnerabilitate/viteza maxima a eschivei
    private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private float dashCost = 20f;

    [Header("Ghost Trail Settings")]
    // Intervalul de timp la care se genereaza o clona vizuala transparenta in timpul Dash-ului
    [SerializeField] private float ghostSpawnInterval = 0.05f;
    // Culoarea si transparenta (Alpha = 0.5) aplicate darei lasate in urma de jucator
    [SerializeField] private Color ghostColor = new Color(0.5f, 0.8f, 1f, 0.5f);

    [Header("References")]
    public PlayerStats playerStats;

    // Variabile interne pentru blocarea/gestionarea masinii de stari (State Machine Locks)
    private bool isKnockedBack = false;
    private bool isDashing = false;
    private bool canDash = true;
    // Flag setat de alte scripturi (ex: PlayerCombat la consum) pentru a bloca abilitatile de mobilitate
    public bool isBusy = false;

    private PlayerControls playerControls;
    // Stocheaza vectorul directional citit de la tastatura (tastele WASD / Sageti)
    private Vector2 movement;
    private Rigidbody2D rb;
    private Animator myAnimator;
    private SpriteRenderer mySpriteRenderer;
    private Vector2 dashDirection;

    private void Awake()
    {
        // Alocarea si cache-uirea componentelor fizice si vizuale de pe acelasi GameObject
        playerControls = new PlayerControls();
        rb = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        mySpriteRenderer = GetComponent<SpriteRenderer>();

        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
    }

    // Activarea si dezactivarea setului de controale odata cu ciclul de viata al scriptului
    private void OnEnable() => playerControls.Enable();
    private void OnDisable() => playerControls.Disable();

    private void Update()
    {
        // Guard Clause (Blocaj): Daca jucatorul este aruncat inapoi sau se afla in Dash, 
        // dezactivam citirea comenzilor standard de miscare pentru a nu anula fortele aplicate.
        if (isKnockedBack || isDashing) return;

        // INTERCEPTAREA MECANICII DE DASH (Eschiva)
        // Folosim proprietatea '.triggered' care reactioneaza ca un impuls discret in frame-ul apasarii (tasta Space/Shift)
        if (playerControls.Movement.Dash.triggered && canDash && !isBusy)
        {
            // Verificam daca jucatorul are suficienta stamina si o consumam instantaneu
            if (playerStats != null && playerStats.UseStamina(dashCost))
            {
                // Pornim corutina asincrona care va suprascrie viteza fizica
                StartCoroutine(DashRoutine());
            }
        }

        // Preluam directiile axelor din tastatura si actualizam parametrii din Animator
        PlayerInput();
    }

    private void FixedUpdate()
    {
        // FixedUpdate ruleaza la intervale de timp fixe, fiind dedicat exclusiv fizicii Rigidbody2D.
        if (isKnockedBack) return;

        // Daca nu suntem in eschiva, aplicam miscarea standard calculata de tastatura
        if (!isDashing) Move();

        // Actualizam constant unghiul de orientare al fetei personajului dupa cursor
        AdjustPlayerFacingDirection();
    }

    private void PlayerInput()
    {
        // Citim vectorul bidimensional (X si Y) generat de noul Input System
        movement = playerControls.Movement.Move.ReadValue<Vector2>();

        // Transmitem valorile vectorului catre parametrii din Blend Tree-ul Animatorului
        myAnimator.SetFloat("moveX", movement.x);
        myAnimator.SetFloat("moveY", movement.y);

        // Verificam daca vectorul are o magnitudine diferita de zero (inseamna ca o tasta este apasata)
        bool isMoving = movement != Vector2.zero;
        myAnimator.SetBool("isWalking", isMoving);

        if (isMoving)
        {
            // Salvam ultima directie valida de miscare. Aceasta previne resetarea animatiei 
            myAnimator.SetFloat("LastInputX", movement.x);
            myAnimator.SetFloat("LastInputY", movement.y);
        }
        else
        {
            // Daca jucatorul sta pe loc, orientarea animatiei de repaus (Idle) este dictata 
            // de pozitia mouse-ului pe ecran, convertita in coordonate de lume (World Space).
            Vector2 cursorDir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position).normalized;
            myAnimator.SetFloat("LastInputX", cursorDir.x);
            myAnimator.SetFloat("LastInputY", cursorDir.y);
        }
    }

    private void Move()
    {
        float targetSpeed = moveSpeed;

        // Verificam daca jucatorul nu este ocupat, daca tine apasata tasta de Sprint SI daca se misca efectiv
        if (!isBusy && playerControls.Movement.Sprint.IsPressed() && movement != Vector2.zero)
        {
            float sprintSpeed = moveSpeed * sprintMultiplier;

            // Consuma stamina in mod continuu 
            // Inmultirea cu Time.fixedDeltaTime transforma rata de consum intr-o valoare fixa per secunda
            if (playerStats != null && playerStats.UseStamina(sprintCost * Time.fixedDeltaTime))
            {
                targetSpeed = sprintSpeed;
                myAnimator.speed = 1.5f; // Acceleram redarea animatiei de mers pentru a se potrivi cu viteza marita
            }
        }
        else
        {
            // Daca nu alearga si nu face alta actiune, resetam viteza animatiei la valoarea nominala (1.0)
            if (!isBusy) myAnimator.speed = 1f;
        }

        // Aplic vectorul final de viteza direct pe corpul fizic Rigidbody2D
        rb.linearVelocity = movement * targetSpeed;
    }

    // Corutina care controleaza comportamentul temporal si deformatiile fizice din timpul eschivei (Dash)
    private IEnumerator DashRoutine()
    {
        isDashing = true;
        canDash = false;

        // Daca jucatorul se misca, facem Dash in directia mersului (tastatura).
        // Daca jucatorul sta pe loc, executam Dash in directia in care se afla cursorul mouse-ului.
        if (movement != Vector2.zero) dashDirection = movement.normalized;
        else
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dashDirection = ((Vector2)mousePos - (Vector2)transform.position).normalized;
        }

        // Modificam scala locala a transform-ului in functie de vectorul eschivei pentru a crea impact vizual:
        // Daca se misca mai mult pe verticala (axa Y), alungim sprite-ul pe inaltime.
        // Daca se misca pe orizontala (axa X), turtim sprite-ul pentru a sugera viteza extrema.
        if (Mathf.Abs(dashDirection.y) > Mathf.Abs(dashDirection.x)) transform.localScale = new Vector3(0.8f, 1.2f, 1.0f);
        else transform.localScale = new Vector3(1.2f, 0.8f, 1.0f);

        // Fortam viteza corpului la valoarea fixa de Dash
        rb.linearVelocity = dashDirection * dashSpeed;

        float dashTimer = dashDuration;
        // Bucla de rulare a efectului Ghost Trail pe durata eschivei
        while (dashTimer > 0)
        {
            CreateGhost(); // Generam o clona statica transparenta in pozitia curenta
            dashTimer -= ghostSpawnInterval; // Decrementam timpul cu intervalul de spawn
            yield return new WaitForSeconds(ghostSpawnInterval); // Intrerupem executia pana la urmatorul interval

            rb.linearVelocity = dashDirection * dashSpeed;
        }

        transform.localScale = Vector3.one; // Resetam scala jucatorului la dimensiunile originale (1, 1, 1)
        rb.linearVelocity = Vector2.zero;   // Oprim brusc personajul din miscare pentru control precis
        isDashing = false;

        // Gestiunea timpului de reincarcare (Cooldown) al abilitatii
        yield return new WaitForSeconds(dashCooldown);
        canDash = true; // Jucatorul poate folosi din nou Dash-ul
    }

    // Genereaza o clona vizuala statica desprinsa de jucator
    private void CreateGhost()
    {
        // Instantiem un GameObject complet nou in scena
        GameObject ghost = new GameObject("DashGhost");
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.localScale;

        // Adaugam dinamic o componenta SpriteRenderer si copiem proprietatile vizuale actuale ale jucatorului
        SpriteRenderer ghostSR = ghost.AddComponent<SpriteRenderer>();
        ghostSR.sprite = mySpriteRenderer.sprite;
        ghostSR.flipX = mySpriteRenderer.flipX;
        ghostSR.sortingLayerName = mySpriteRenderer.sortingLayerName;
        // Plasam umbra cu un strat mai in spate (-1) fata de jucator pentru a nu-i acoperi fata
        ghostSR.sortingOrder = mySpriteRenderer.sortingOrder - 1;

        // Injectam componenta logica responsabila de estompare (Fade out) si ii transmitem culoarea de tip fantoma
        GhostEffect effect = ghost.AddComponent<GhostEffect>();
        effect.Init(ghostColor);
    }

    // Functie publica apelata de inamici sau proiectile (ex: EnemyMelee, EnemyProjectile) pentru a respinge jucatorul
    public void Knockback(Transform enemyTransform, float knockbackForce)
    {
        if (!isKnockedBack)
        {
            isKnockedBack = true;

            // Calculam vectorul de respingere: directia de la inamic catre jucator (impingere in exterior)
            Vector2 direction = (transform.position - enemyTransform.position).normalized;

            // Anulam viteza curenta de mers 
            rb.linearVelocity = Vector2.zero;

            // Aplicam forta directionala sub forma de Impulse (forta instantanee aplicata o singura data, pentru explozii/lovituri)
            rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);

            // Lansam corutina de control al duratei de uluire (Stun duration)
            StartCoroutine(KnockbackRoutine());
        }
    }

    private IEnumerator KnockbackRoutine()
    {
        // Jucatorul pierde controlul tastaturii timp de exact 0.2 secunde (durata uluirii)
        yield return new WaitForSeconds(0.2f);
        rb.linearVelocity = Vector2.zero; // Oprim alunecarea reziduala din knockback
        isKnockedBack = false; // Restauram controlul jucatorului asupra tastaturii
    }

    // Transmite unghiul directional dintre mouse si jucator catre parametrii FaceX/FaceY din Animator
    private void AdjustPlayerFacingDirection()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mousePos - transform.position).normalized;
        myAnimator.SetFloat("FaceX", direction.x);
        myAnimator.SetFloat("FaceY", direction.y);
    }
}