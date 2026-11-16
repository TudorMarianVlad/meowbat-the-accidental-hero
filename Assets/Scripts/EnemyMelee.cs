using System.Collections;
using UnityEngine;

// Clasa EnemyMelee gestioneaza exclusiv logica ofensiva de proximitate (corp-la-corp) a inamicilor.
// Este conceputa ca un modul independent, apelat de scriptul de inteligenta artificiala (AI) 
// atunci cand inamicul ajunge suficient de aproape de jucator.
public class EnemyMelee : MonoBehaviour
{
    [Header("Attack Settings")]
    // Distanta maxima la care lovitura inamicului este considerata valida
    public float attackRange = 0.8f;

    // Timpul de "Telegrafiere" (Windup). Aceasta este o mecanica cruciala de Game Design:
    // Inamicul se opreste si isi pregateste atacul timp de 0.5 secunde, oferind jucatorului 
    // un indiciu vizual si timpul necesar pentru a reactiona si a se feri.
    public float attackWindupTime = 0.5f;

    // Timpul de recuperare necesar intre doua atacuri consecutive (Cooldown)
    public float attackCooldown = 1.5f;

    // Cantitatea de daune aplicata jucatorului la o lovitura reusita
    public int damageAmount = 10;

    // Forta cu care jucatorul este impins inapoi (Knockback) pentru a crea spatiu dupa o lovitura
    public float knockbackThrust = 10f;

    // Proprietate cu nivel de acces mixt (Incapsulare corecta).
    // Orice alt script (ex: AI-ul de miscare) poate citi valoarea (get) pentru a sti daca inamicul ataca,
    // dar doar acest script poate modifica valoarea (private set), prevenind erorile externe de stare.
    public bool isAttacking { get; private set; }

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Aceasta functie actioneaza ca un declansator extern (Trigger), apelat de scriptul de AI (ex: EnemyMeleeAI)
    // in momentul in care distanta dintre inamic si jucator este mai mica sau egala cu attackRange.
    public void Attack(Transform target)
    {
        // Mecanism de tip Lock (Lacat): 
        // Ne asiguram ca nu declansam o noua secventa de atac daca inamicul este deja in mijlocul uneia.
        if (!isAttacking)
        {
            // Pornim un fir de executie asincron (Coroutine) pentru a putea folosi pauze temporale (Yield)
            StartCoroutine(AttackSequence(target));
        }
    }

    // Corutina care gestioneaza fluxul cronologic al atacului (Pregatire -> Lovire -> Recuperare)
    private IEnumerator AttackSequence(Transform target)
    {
        // Blocarea Starii
        isAttacking = true;

        // Declansarea vizualului
        // Setam trigger-ul in Animator pentru a porni animatia de pregatire a loviturii.
        if (animator != null) animator.SetTrigger("Attack");

        // Faza de Telegrafiere (Windup)
        // Oprim executia acestui script timp de 'attackWindupTime' secunde.
        // In acest timp, AI-ul de miscare vede ca isAttacking = true si mentine inamicul pe loc.
        yield return new WaitForSeconds(attackWindupTime);

        // Aceasta este logica de eschiva (Dodge/Dash)
        // Daca jucatorul a folosit Dash-ul si a iesit din raza (attackRange + 0.2f toleranta), atacul rateaza (Miss).
        if (target != null && Vector2.Distance(transform.position, target.position) <= attackRange + 0.2f)
        {
            // Extragem registrul central al jucatorului si aplicam scaderea vietii
            PlayerStats stats = target.GetComponent<PlayerStats>();
            if (stats != null) stats.TakeDamage(damageAmount);

            // Extragem controlerul jucatorului si aplicam forta fizica de respingere (Knockback)
            PlayerController pController = target.GetComponent<PlayerController>();
            if (pController != null) pController.Knockback(transform, knockbackThrust);
        }

        // Chiar daca atacul a lovit sau a ratat, inamicul trebuie sa astepte inainte de a lovi din nou.
        yield return new WaitForSeconds(attackCooldown);

        // Inamicul este liber sa se miste din nou si sa initieze un nou atac.
        isAttacking = false;
    }
}