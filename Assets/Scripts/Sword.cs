using UnityEngine;

// Gestioneaza atacul cu sabia, sistemul de UI Shielding pentru prevenirea
// activarii accidentale in meniuri si efectele vizuale (Trail Renderer).
public class Sword : MonoBehaviour
{
    [SerializeField] private Animator myAnimator;
    private PlayerControls playerControls;
    private PlayerCombat playerCombat;

    [Header("VFX")]
    private TrailRenderer weaponTrail;
    public float trailDuration = 0.3f;

    private void Awake()
    {
        // Initializare componente si cautare ierarhica pentru PlayerCombat.
        if (myAnimator == null) myAnimator = GetComponent<Animator>();
        weaponTrail = GetComponentInChildren<TrailRenderer>();
        playerControls = new PlayerControls();
        playerCombat = GetComponentInParent<PlayerCombat>();
    }

    private void OnEnable()
    {
        playerControls.Enable();
        if (weaponTrail != null) weaponTrail.emitting = false;
    }

    private void OnDisable() => playerControls.Disable();

    void Start()
    {
        if (weaponTrail != null) weaponTrail.emitting = false;

        // Abonare la evenimentul de atac (input instant).
        playerControls.Combat.Attack.started += _ => Attack();
    }

    private void Attack()
    {
        // 1. Verificare pauza globala.
        if (PauseManager.isPaused) return;

        // 2. UI Shielding: Blocare atac daca meniurile sunt deschise.
        if (playerCombat != null)
        {
            if (playerCombat.inventoryUIPanel != null && playerCombat.inventoryUIPanel.activeInHierarchy) return;
            if (playerCombat.shopUIPanel != null && playerCombat.shopUIPanel.activeInHierarchy) return;
        }

        // 3. Activare VFX (Trail Renderer).
        if (weaponTrail != null)
        {
            CancelInvoke(nameof(StopTrail));
            weaponTrail.Clear();
            weaponTrail.emitting = true;
        }

        // 4. Declansare animatie de atac.
        myAnimator.SetTrigger("Attack");

        // Planificare oprire urma.
        Invoke(nameof(StopTrail), trailDuration);
    }

    private void StopTrail()
    {
        if (weaponTrail != null) weaponTrail.emitting = false;
    }
}