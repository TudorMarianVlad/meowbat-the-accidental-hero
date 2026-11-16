using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine; // Obligatoriu pentru manipularea camerelor virtuale inteligente (vcam) și a confiner-ului din cod
using System.Collections;

// Clasa PlayerStats reprezintă registrul central al tuturor atributelor cantitative ale jucătorului.
// Gestionează atributele vitale (viață, stamină, armură), progresia RPG (nivele, experiență, economie),
// regenerarea pasivă, algoritmul de atenuare a daunelor, logica de respawn fizic/vizual și deserializarea datelor.
public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;
    public Slider healthSlider;
    public TextMeshProUGUI healthText;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 10f; // Cantitatea de stamină recuperată per secundă
    public Slider staminaSlider;
    public TextMeshProUGUI staminaText;

    [Header("Armor")]
    public int maxArmor = 100;
    public int currentArmor = 0;
    public Slider armorSlider;
    public TextMeshProUGUI armorText;

    [Header("Experience")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100; // Pragul inițial de XP pentru trecerea la nivelul 2
    public int xpIncrement = 25;    // Coeficient de scalare a dificultății (crește pragul la fiecare nivel)
    public Slider xpSlider;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI xpAmountText;

    [Header("Currency")]
    public int currentGold = 0;
    public TextMeshProUGUI goldText;

    [Header("Death & Respawn")]
    public GameObject deathScreenPanel;
    public Transform respawnPoint;
    // Zona de coliziune 2D care limitează mișcarea camerei în hub-ul central (zona de siguranță)
    public Collider2D hubCameraBounds;

    [Header("Location State")]
    public bool isInArena = false;

    [Header("Level Up")]
    public UpgradeShopkeeper upgradeShop;
    public int goldRewardPerLevel = 25;
    public float levelUpMenuDelay = 1.2f; // Pauza de suspans înainte de deschiderea meniului de upgrade

    [Header("Level Up VFX")]
    public GameObject levelUpParticlePrefab;
    public Transform playerTransform;

    [Header("Stats Bar UI (Above Hotbar)")]
    // Referințe către panoul extins de statistici brute randate deasupra Hotbar-ului
    public TextMeshProUGUI barHealthText;
    public TextMeshProUGUI barStaminaText;
    public TextMeshProUGUI barAttackText;
    public TextMeshProUGUI barSpeedText;
    public TextMeshProUGUI barAttackSpeedText;
    public TextMeshProUGUI barArmorText;

    private Inventory inventory;
    private SpriteRenderer spriteRenderer;
    private PopupManager popupManager;

    void Start()
    {
        // Cache-uirea componentelor pentru optimizarea accesului la memorie
        inventory = GetComponent<Inventory>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        popupManager = GetComponent<PopupManager>();

        // Setarea stării inițiale de viață și stamină la valorile maxime admise
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        UpdateUI();
    }

    void Update()
    {
        // REGENERARE REZIDUALĂ PASIVĂ
        // Dacă stamina a fost consumată prin Sprint sau Dash, o regenerăm continuu.
        if (currentStamina < maxStamina)
        {
            // Înmulțirea cu Time.deltaTime transformă rata din „per cadru” în „per secundă”, independent de framerate.
            currentStamina += staminaRegenRate * Time.deltaTime;

            // Limitare superioară (Clamp): prevenim depășirea pragului maxim
            if (currentStamina > maxStamina) currentStamina = maxStamina;
            UpdateUI();
        }
    }

    // Algoritmul matematic de reducere a daunelor pe baza indicelui de armură (Damage Mitigation)
    public void TakeDamage(int damage)
    {
        float mitigationMultiplier = 100f / (100f + currentArmor);

        // 2. Apply the multiplier to the incoming damage and round it to the nearest whole integer
        // We use Mathf.Max(1, ...) to guarantee enemies always do at least 1 damage.
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage * mitigationMultiplier));

        currentHealth -= finalDamage;

        // Declanșăm numărul plutitor de daune (red) deasupra personajului
        if (popupManager != null) popupManager.Show($"-{finalDamage}", Color.red);

        if (currentHealth > 0)
        {
            // Feedback vizual instantaneu: Corutina schimbă culoarea sprite-ului în roșu pentru câteva fracțiuni de secundă
            if (spriteRenderer != null) StartCoroutine(FlashRed());
        }
        else Die(); // Tranziție spre starea de colaps

        UpdateUI();
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateUI();
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        if (popupManager != null) popupManager.Show("+" + amount, Color.yellow);
        UpdateUI();
    }

    public void SpendGold(int amount)
    {
        currentGold -= amount;
        if (popupManager != null) popupManager.Show("-" + amount, Color.yellow);
        UpdateUI();
    }

    // Metodă de interogare logică (Gatekeeper Method) apelată de PlayerController.
    // Verifică disponibilitatea resurselor curente înainte de a aproba execuția unui Dash sau Sprint.
    public bool UseStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            UpdateUI();
            return true; // Resursa este disponibilă și a fost reținută din registru
        }
        return false; // Resursă insuficientă, acțiunea este respinsă
    }

    private void Die()
    {
        // Resetăm starea logică a hărții de luptă (anulăm inamicii generați în valul curent)
        WaveManager wm = Object.FindFirstObjectByType<WaveManager>();
        if (wm != null) wm.ResetWaveSystem();

        // Blocăm complet motorul fizic și de timp al jocului, afișând ecranul de Game Over
        if (deathScreenPanel != null) deathScreenPanel.SetActive(true);
        Time.timeScale = 0f;
        PauseManager.isPaused = true;
    }

    // Logica complexă de relocare și reconstrucție a stării (Respawn Lifecycle)
    public void Respawn()
    {
        if (deathScreenPanel != null) deathScreenPanel.SetActive(false);

        // 1. Restaurarea totală a resurselor vitale
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        // Ne asigurăm că dacă jucătorul a murit în Arenă, jocul ridică interdicția de salvare.
        isInArena = false;
        UpdateUI();

        // 2. Repoziționarea fizică a avatarului la coordonatele punctului de Spawn din scenă
        if (respawnPoint != null) transform.position = respawnPoint.position;

        // 3. RECUPERAREA CAMEREI VIRTUALE (Cinemachine Camera Warp Integration)
        // Când un personaj este teleportat pe o distanță mare la respawn, camera tinde să execute un „pan” rapid (glisare lungă) 
        // care arată inestetic și strică imersiunea. Pentru a preveni asta, reconfigurăm limitele camerei:
        CinemachineCamera vcam = FindFirstObjectByType<CinemachineCamera>();
        if (vcam != null)
        {
            // Extragem confiner-ul 2D care oprește camera să iasă în afara hărții (ex: dincolo de pereții hărții)
            var confiner = vcam.GetComponent<CinemachineConfiner2D>();
            if (confiner != null && hubCameraBounds != null)
            {
                // Îi setăm noile limite fizice specifice Hub-ului Central (zona de siguranță)
                confiner.BoundingShape2D = hubCameraBounds;
                // Golim cache-ul geometric vechi pentru a forța Cinemachine să recalculeze marginile instantaneu
                confiner.InvalidateBoundingShapeCache();
            }

            // Metoda critică OnTargetObjectWarped anunță motorul Cinemachine că jucătorul a fost „teleportat”, 
            // forțând camera să se mute instantaneu în noua poziție fără nicio tranziție vizuală reziduală.
            vcam.OnTargetObjectWarped(transform, respawnPoint.position - transform.position);
        }

        // 4. Repornirea timpului global
        Time.timeScale = 1f;
        PauseManager.isPaused = false;
    }

    public void IncreaseMaxStamina(float amount)
    {
        maxStamina += amount;
        currentStamina = maxStamina;
        UpdateUI();
    }

    // Algoritm de agregare a indicelui total de apărare (Armor Aggregation Loop)
    public void RecalculateArmor()
    {
        if (inventory == null) inventory = GetComponent<Inventory>();
        if (inventory == null) return;

        int newArmor = 0;
        // Parcurgem exclusiv sloturile rezervate echipamentului de armură (indicii 46-49 din masiv: Cască, Pieptar, Pantaloni, Cizme)
        for (int i = 46; i <= 49; i++)
        {
            if (i < inventory.inventorySlots.Length && inventory.inventorySlots[i] != null)
                newArmor += inventory.inventorySlots[i].armorValue; // Adunăm statisticile din ScriptableObjects
        }
        currentArmor = newArmor;
        UpdateUI();
    }

    // Algoritmul liniar de acumulare XP cu buclă de Level-Up iterativă
    public void AddXP(int amount)
    {
        currentXP += amount;

        // Folosim o buclă 'while' în loc de 'if' ca o măsură de siguranță pentru Game Design. 
        // Dacă jucătorul primește o cantitate masivă de XP de la un Boss care acoperă mai mult de un nivel complet,
        // bucla va procesa succesiv mai multe Level-Up-uri până când XP-ul rezidual scade sub pragul necesar.
        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel; // Reținem surplusul de experiență pentru nivelul următor
            currentLevel++;
            xpToNextLevel += xpIncrement; // Creștem dificultatea (pragul) pentru următorul nivel
            LevelUp();
        }
        UpdateUI();
    }

    private void LevelUp()
    {
        // Recompensă economică scalabilă în funcție de noul nivel atins
        currentGold += (currentLevel * goldRewardPerLevel);
        UpdateUI();

        // Instanțierea sistemului de particule pentru feedback vizual glorios
        if (levelUpParticlePrefab != null)
        {
            GameObject vfx = Instantiate(levelUpParticlePrefab, transform.position, Quaternion.identity, transform);
            Destroy(vfx, 2.5f); // Auto-distrugerea particulelor din memorie după finalizarea ciclului de emisie
        }

        if (popupManager != null) popupManager.Show("LEVEL UP!", Color.magenta);

        // Lansăm corutina pentru deschiderea temporizată a meniului de selecție atribute
        StartCoroutine(WaitToOpenMenu());
    }

    private IEnumerator WaitToOpenMenu()
    {
        // Lăsăm 1.2 secunde ca jucătorul să vadă particulele și textul magenta înainte de a îngheța ecranul
        yield return new WaitForSeconds(levelUpMenuDelay);

        Time.timeScale = 0f;
        PauseManager.isPaused = true;

        // Deschidem magazinul într-o stare specială modificată (fără costuri, doar pentru alegerea bonusului de nivel)
        if (upgradeShop != null) upgradeShop.OpenAsLevelUpMenu();
    }

    // --- DESERIALIZAREA ȘI RESTAURAREA TOTALĂ A DATELOR (JSON Data Hydration) ---
    // Această metodă preia structura brută de date (SaveData) extrasă de SaveManager de pe disc
    // și re-injectează valorile în toate subsistemele live din scenă.
    public void LoadPlayerData()
    {
        SaveData data = SaveManager.LoadGame();
        if (data == null) return;

        // Pasul 0: Restaurarea coordonatelor spațiale (Teleportare fizică fără forțe reziduale)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Forțăm poziția direct prin corpul fizic pentru a preveni interpolările greșite ale motorului de fizică 2D
            rb.position = new Vector2(data.pX, data.pY);
            rb.linearVelocity = Vector2.zero; // Anulăm orice viteză anterioară salvării pentru a preveni alunecarea
        }
        else
        {
            transform.position = new Vector3(data.pX, data.pY, data.pZ);
        }

        // 1. Aplicarea statisticilor de bază în acest registru
        currentGold = data.gold;
        currentLevel = data.level;
        currentXP = data.xp;
        maxHealth = data.maxHealth;
        currentHealth = maxHealth;
        maxStamina = data.maxStamina;
        currentStamina = maxStamina;
        xpToNextLevel = data.maxXP;

        DayNightCycle timeScript = Object.FindFirstObjectByType<DayNightCycle>();
        if (timeScript != null)
        {
            timeScript.currentTime = data.timeOfDay;
        }

        // 2. Restaurarea stării magazinului de upgrade-uri (nivelele cumpărate anterior)
        if (upgradeShop != null)
        {
            upgradeShop.damageLevel = data.damageLevel;
            upgradeShop.speedLevel = data.speedLevel;
        }

        // 3. Sincronizarea modulelor de luptă și mișcare cu atributele din fișierul de salvare
        GetComponent<PlayerCombat>().attackDamage = data.attackDamage;
        GetComponent<PlayerController>().moveSpeed = data.moveSpeed;

        // 4. RESTAURAREA DINAMICĂ A OBIECTELOR DIN INVENTAR (Dynamic Asset Loading via Factory Pattern)
        Inventory inv = GetComponent<Inventory>();
        if (inv != null && data.inventoryItemNames != null)
        {
            for (int i = 0; i < inv.inventorySlots.Length; i++)
            {
                if (i >= data.inventoryItemNames.Length) break;

                string itemName = data.inventoryItemNames[i];

                if (!string.IsNullOrEmpty(itemName))
                {
                    // Deoarece fișierele JSON nu pot stoca direct obiecte complexe Unity (cum ar fi ScriptableObjects),
                    // salvăm doar numele text al acestora. La încărcare, folosim API-ul nativ 'Resources.Load' 
                    // pentru a căuta automat în folderul Assets/Resources/Items un fișier cu același nume și a-l încărca în RAM.
                    ItemData itemAsset = Resources.Load<ItemData>("Items/" + itemName);

                    if (itemAsset != null)
                    {
                        inv.inventorySlots[i] = itemAsset;
                        inv.slotQuantities[i] = data.inventoryQuantities[i];
                    }
                }
                else
                {
                    // Dacă string-ul din salvări este gol, înseamnă că slotul respectiv era liber la momentul salvării
                    inv.inventorySlots[i] = null;
                    inv.slotQuantities[i] = 0;
                }
            }

            // Forțăm toate sloturile grafice de pe Canvas să se redeseneze cu noile obiecte încărcate
            inv.RefreshInventoryUI();
        }

        UpdateUI();
        Debug.Log("Full Player Data & Position Loaded!");
    }

    // Interfața extinsă de redesenare a elementelor UI din joc
    public void UpdateUI()
    {
        // 1. Corelarea barelor clasice (Sliders normalizate între 0.0f și 1.0f)
        if (healthSlider != null) healthSlider.value = (float)currentHealth / maxHealth;
        if (healthText != null) healthText.text = currentHealth + " / " + maxHealth;

        if (staminaSlider != null) staminaSlider.value = currentStamina / maxStamina;
        if (staminaText != null) staminaText.text = Mathf.FloorToInt(currentStamina) + " / " + maxStamina;

        if (armorSlider != null) armorSlider.value = (float)currentArmor / maxArmor;
        if (armorText != null) armorText.text = currentArmor + " / " + maxArmor;

        if (xpSlider != null) xpSlider.value = (float)currentXP / xpToNextLevel;
        if (levelText != null) levelText.text = "Lvl. " + currentLevel;
        if (xpAmountText != null) xpAmountText.text = currentXP + " / " + xpToNextLevel;
        if (goldText != null) goldText.text = "" + currentGold;

        // 2. EXTRACTIA DE DATE DIN SUBSISTEME EXTERNE PENTRU BARA DE STATISTICI BRUTE
        // Pentru a randa textul detaliat (ATK, MS, AS), interogăm dinamic componentele active ale jucătorului.
        PlayerController moveScript = GetComponent<PlayerController>();
        PlayerCombat combatScript = GetComponent<PlayerCombat>();
        BowWeapon bowScript = GetComponentInChildren<BowWeapon>(); // Căutăm arcul în ierarhia de copii (weaponHolder)

        if (barHealthText != null) barHealthText.text = "HP: " + maxHealth;
        if (barStaminaText != null) barStaminaText.text = "STM: " + maxStamina;
        if (barArmorText != null) barArmorText.text = "DEF: " + currentArmor;

        if (moveScript != null && barSpeedText != null)
            barSpeedText.text = "MS: " + moveScript.moveSpeed;

        if (combatScript != null && barAttackText != null)
            barAttackText.text = "ATK: " + combatScript.attackDamage;

        // Randarea dinamică a vitezei de atac (Attack Speed) bazată pe arma la distanță activă
        if (bowScript != null && barAttackSpeedText != null)
            // Formatăm float-ul cu două zecimale pentru un aspect curat ("F2" -> ex: 1.50)
            barAttackSpeedText.text = "AS: " + bowScript.fireRate.ToString("F2");
        else
            barAttackSpeedText.text = "AS: ---"; // Afișaj implicit în caz că jucătorul ține în mână o armă melee

    }

    private System.Collections.IEnumerator FlashRed()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        spriteRenderer.color = Color.white;
    }
}