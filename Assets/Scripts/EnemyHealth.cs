using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Registrul central pentru entitatile ostile; gestioneaza viata, moartea,
// mecanicile de spawn, drop-ul de loot si feedback-ul vizual (VFX).
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead = false;

    [Header("UI References")]
    public Slider healthSlider;
    public TextMeshProUGUI healthText;

    [Header("Floating Text")]
    public GameObject floatingTextPrefab;
    public Transform damagePopupPoint;

    [Header("Loot & Rewards")]
    public int xpReward = 50;
    public GameObject coinPrefab;
    public float dropForce = 3f;
    public int goldDroppedPerCoin = 5;

    [Header("Spawn on Death")]
    public GameObject spawnOnDeathPrefab;
    public bool shouldSpawnOnDeath = false;
    public Transform spawnPoint;

    [Header("Arena Logic")]
    public bool isArenaEnemy = false;

    [Header("VFX")]
    public GameObject healParticlePrefab;
    public GameObject explosionPrefab;
    public float explosionHeightOffset = 0.5f;
    public float deathExplosionDelay = 0.2f;
    public float suicideExplosionDelay = 0.5f;
    public float shockwaveDuration = 0.4f;
    public float shockwaveDelay = 0.1f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (healthSlider != null) { healthSlider.maxValue = maxHealth; healthSlider.value = currentHealth; }
        if (healthText != null) healthText.text = currentHealth + " / " + maxHealth;

        // Inregistrare automata in WaveManager daca inamicul este de arena.
        if (isArenaEnemy)
        {
            WaveManager wm = Object.FindFirstObjectByType<WaveManager>();
            if (wm != null) wm.RegisterEnemy(this.gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        // Feedback vizual: Damage Numbers.
        Canvas myCanvas = GetComponentInChildren<Canvas>();
        if (floatingTextPrefab != null && damagePopupPoint != null && myCanvas != null)
        {
            GameObject textObj = Instantiate(floatingTextPrefab, damagePopupPoint.position, Quaternion.identity, myCanvas.transform);
            FloatingText ft = textObj.GetComponent<FloatingText>();
            if (ft != null) ft.Setup(damage.ToString(), Color.red);
        }

        currentHealth -= damage;
        if (healthSlider != null) healthSlider.value = currentHealth;
        if (healthText != null) healthText.text = Mathf.Max(0, currentHealth) + " / " + maxHealth;

        if (animator != null) animator.SetTrigger("Hurt");

        if (currentHealth > 0)
        {
            if (spriteRenderer != null) StartCoroutine(FlashRed());
        }
        else
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (spriteRenderer != null) spriteRenderer.color = Color.white;

        // Dezactivare AI si componente fizice post-mortem.
        foreach (var ai in GetComponents<MonoBehaviour>())
            if (ai.GetType().Name.Contains("Enemy")) ai.enabled = false;

        if (animator != null) animator.SetBool("isDead", true);

        if (shouldSpawnOnDeath && spawnOnDeathPrefab != null) SpawnMiniShroom();

        GetComponent<Collider2D>().enabled = false;
        if (GetComponent<Rigidbody2D>() != null) GetComponent<Rigidbody2D>().simulated = false;

        DropLoot();
        TriggerExplosion(deathExplosionDelay);

        if (spriteRenderer != null) StartCoroutine(FadeOut());
        Destroy(gameObject, 1.1f);
    }

    // Gestionare VFX: Explozie si Shockwave.
    public void TriggerExplosion(float delay)
    {
        if (explosionPrefab != null) Invoke(nameof(SpawnExplosionVFX), delay);
    }

    private void SpawnExplosionVFX()
    {
        Vector3 spawnPosition = transform.position + new Vector3(0f, explosionHeightOffset, 0f);
        Instantiate(explosionPrefab, spawnPosition, Quaternion.identity);
        Invoke(nameof(TriggerShockwave), shockwaveDelay);
    }

    private void TriggerShockwave()
    {
        if (ScreenEffectsManager.instance != null)
            ScreenEffectsManager.instance.TriggerChromaticAberration(1f, shockwaveDuration);
    }

    // Mecanica de diviziune (exemplu: Shroom).
    void SpawnMiniShroom()
    {
        Vector3 startPos = (spawnPoint != null) ? spawnPoint.position : transform.position;
        GameObject mini = Instantiate(spawnOnDeathPrefab, startPos, Quaternion.identity);
        EnemyHealth miniHealth = mini.GetComponent<EnemyHealth>();

        if (this.isArenaEnemy && miniHealth != null)
        {
            WaveManager wm = Object.FindFirstObjectByType<WaveManager>();
            if (wm != null)
            {
                wm.RegisterEnemy(mini);
                miniHealth.isArenaEnemy = true;
                miniHealth.maxHealth += (wm.currentWave - 1) * 5;
                miniHealth.currentHealth = miniHealth.maxHealth;
            }
        }
        StartCoroutine(SimulateJump(mini, startPos, startPos + (startPos - transform.position).normalized * 1.5f));
    }

    private System.Collections.IEnumerator SimulateJump(GameObject mini, Vector3 start, Vector3 target)
    {
        float elapsed = 0f, duration = 0.6f;
        while (elapsed < duration && mini != null)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            mini.transform.position = Vector3.Lerp(start, target, percent) + Vector3.up * (4f * 1.2f * percent * (1f - percent));
            yield return null;
        }
    }

    void DropLoot()
    {
        PlayerStats stats = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerStats>();
        if (stats != null) stats.AddXP(xpReward);

        if (coinPrefab != null)
        {
            for (int i = 0; i < Random.Range(3, 7); i++)
            {
                GameObject coin = Instantiate(coinPrefab, transform.position + (Vector3)Random.insideUnitCircle * 0.2f, Quaternion.identity);
                coin.GetComponent<CoinPickup>().goldValue = goldDroppedPerCoin;
                coin.GetComponent<Rigidbody2D>()?.AddForce(new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * Random.Range(dropForce * 0.7f, dropForce * 1.3f), ForceMode2D.Impulse);
            }
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        if (healthSlider != null) healthSlider.value = currentHealth;
        if (healthText != null) healthText.text = currentHealth + " / " + maxHealth;

        if (healParticlePrefab != null) Destroy(Instantiate(healParticlePrefab, transform.position, Quaternion.identity, transform), 1.5f);
        StartCoroutine(FlashGreen());
    }

    private System.Collections.IEnumerator FlashGreen() { spriteRenderer.color = Color.green; yield return new WaitForSeconds(0.3f); spriteRenderer.color = Color.white; }
    private System.Collections.IEnumerator FlashRed() { spriteRenderer.color = Color.red; yield return new WaitForSeconds(0.15f); spriteRenderer.color = Color.white; }
    private System.Collections.IEnumerator FadeOut()
    {
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            spriteRenderer.color = new Color(1, 1, 1, Mathf.Lerp(1f, 0f, t));
            yield return null;
        }
    }
}