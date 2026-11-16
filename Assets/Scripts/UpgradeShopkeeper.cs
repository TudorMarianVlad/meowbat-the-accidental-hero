using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Clasa UpgradeShopkeeper gestioneaza modificarea atributelor de baza ale jucatorului.
// Controleaza calculul exponential al costurilor, nivelul fiecarui atribut
// si starea duala a panoului (cumparare normala vs. alegere recompensa gratuita la Level Up).
public class UpgradeShopkeeper : MonoBehaviour
{
    [Header("--- MAIN SETTINGS ---")]
    public GameObject upgradeUIPanel;
    public GameObject inventoryUIPanel;
    public BowWeapon bowScript; // Referinta directa catre scriptul arcului pentru modificarea cadentei de tragere

    [Header("--- PLAYER DATA ---")]
    public PlayerStats playerStats;
    public PlayerCombat playerCombat;
    public Inventory inventory;

    [Header("--- UPGRADE AMOUNTS (Balancing) ---")]
    // Valorile fixe adaugate la statistici la fiecare achizitie reusita
    public int damageIncrement = 5;
    public int healthIncrement = 10;
    public float staminaIncrement = 10f;
    public float speedIncrement = 0.5f;
    // Coeficientul de multiplicare a pretului (ex: 1.5f determina o crestere cu 50% a costului anterior)
    public float priceMultiplier = 1.5f;

    [Header("--- CURRENT UPGRADE COSTS ---")]
    public int damageCost = 50;
    public int healthCost = 40;
    public int staminaCost = 45;
    public int speedCost = 60;
    public int bowSpeedCost = 70;

    [Header("--- CURRENT UPGRADE LEVELS ---")]
    public int damageLevel = 1;
    public int healthLevel = 1;
    public int staminaLevel = 1;
    public int speedLevel = 1;
    public int bowLevel = 1;

    [Header("--- UI PRICE TEXT REFERENCES ---")]
    public TextMeshProUGUI damagePriceText;
    public TextMeshProUGUI healthPriceText;
    public TextMeshProUGUI staminaPriceText;
    public TextMeshProUGUI speedPriceText;
    public TextMeshProUGUI bowPriceText;

    [Header("--- UI LEVEL TEXT REFERENCES ---")]
    public TextMeshProUGUI damageLevelText;
    public TextMeshProUGUI healthLevelText;
    public TextMeshProUGUI staminaLevelText;
    public TextMeshProUGUI speedLevelText;
    public TextMeshProUGUI bowLevelText;

    // Variabile de control pentru masina de stari a ferestrei
    private bool isPlayerNear = false;
    private PlayerControls controls;
    // Flag critic care determina daca upgrade-ul curent este gratuit (oferit la trecerea de nivel)
    private bool isLevelUpReward = false;

    void Awake()
    {
        controls = new PlayerControls();
        // Inregistram actiunea de deschidere a magazinului la apasarea tastei de interactiune
        controls.UI.Interact.performed += ctx => TryOpenShop();
    }

    void Start()
    {
        // Dezactivam panoul grafic la pornirea scenei si desenam valorile text initiale
        if (upgradeUIPanel != null) upgradeUIPanel.SetActive(false);
        UpdateUI();
    }

    void OnEnable() { controls.Enable(); }
    void OnDisable() { controls.Disable(); }

    private void TryOpenShop()
    {
        // Permite comutarea starii magazinului doar daca jucatorul se afla in interiorul colizorului Trigger
        if (isPlayerNear) ToggleShop();
    }

    public void ToggleShop()
    {
        bool isShopOpen = upgradeUIPanel.activeSelf;
        bool newState = !isShopOpen;
        upgradeUIPanel.SetActive(newState);

        if (newState) UpdateUI();
        // Sincronizare cu controller-ul de miscare: daca magazinul este deschis, 
        // marcam personajul ca 'isBusy' pentru a-i bloca abilitatile ofensive sau de eschiva (Dash).
        if (playerCombat != null && playerCombat.playerMovement != null)
            playerCombat.playerMovement.isBusy = newState;
    }

    // --- LOGICA DE IMBUNATATIRE A DAUNELOR (Damage Upgrade) ---
    public void UpgradeDamage()
    {
        // Executam codul daca suntem in modul gratuit de Level Up 
        // sau daca jucatorul are destul aur in registrul de statistici.
        if (isLevelUpReward || playerStats.currentGold >= damageCost)
        {
            // Daca nu este recompensa gratuita, retragem costul financiar din contul jucatorului
            if (!isLevelUpReward) playerStats.SpendGold(damageCost);

            // Modificam daunele direct in hub-ul de combat
            playerCombat.attackDamage += damageIncrement;

            // Recalculam pretul exponential pentru urmatorul nivel si incrementam indicele de nivel
            damageCost = Mathf.RoundToInt(damageCost * priceMultiplier);
            damageLevel++;

            // Daca modificarea a fost pornita de un Level Up, apelam FinishLevelUp pentru a inchide meniul si a relua timpul.
            if (isLevelUpReward) FinishLevelUp();
            else { UpdateUI(); playerStats.UpdateUI(); } // Altfel, doar reimprospatam interfetele grafice live
        }
    }

    // --- LOGICA DE IMBUNATATIRE A VIETII (Health Upgrade) ---
    public void UpgradeHealth()
    {
        if (isLevelUpReward || playerStats.currentGold >= healthCost)
        {
            if (!isLevelUpReward) playerStats.SpendGold(healthCost);

            // Incrementam valoarea maxima admisa a vietii
            playerStats.maxHealth += healthIncrement;
            // Oferim gratuit vindecare egala cu valoarea adaugata (aspect corect de Game Design)
            playerStats.Heal(healthIncrement);

            healthCost = Mathf.RoundToInt(healthCost * priceMultiplier);
            healthLevel++;

            if (isLevelUpReward) FinishLevelUp();
            else { UpdateUI(); playerStats.UpdateUI(); }
        }
    }

    // --- LOGICA DE IMBUNATATIRE A STAMINEI (Stamina Upgrade) ---
    public void UpgradeStamina()
    {
        if (isLevelUpReward || playerStats.currentGold >= staminaCost)
        {
            if (!isLevelUpReward) playerStats.SpendGold(staminaCost);

            // Modificam rezerva totala de stamina din PlayerStats
            playerStats.IncreaseMaxStamina(staminaIncrement);

            staminaCost = Mathf.RoundToInt(staminaCost * priceMultiplier);
            staminaLevel++;

            if (isLevelUpReward) FinishLevelUp();
            else { UpdateUI(); playerStats.UpdateUI(); }
        }
    }

    // --- LOGICA DE IMBUNATATIRE A VITEZEI DE DEPLASARE (Movement Speed Upgrade) ---
    public void UpgradeMoveSpeed()
    {
        if (isLevelUpReward || playerStats.currentGold >= speedCost)
        {
            if (!isLevelUpReward) playerStats.SpendGold(speedCost);

            // Injectam noua viteza direct in motorul cinematic PlayerController
            if (playerCombat != null && playerCombat.playerMovement != null)
                playerCombat.playerMovement.moveSpeed += speedIncrement;

            speedCost = Mathf.RoundToInt(speedCost * priceMultiplier);
            speedLevel++;

            if (isLevelUpReward) FinishLevelUp();
            else { UpdateUI(); playerStats.UpdateUI(); }
        }
    }

    // --- LOGICA DE IMBUNATATIRE A CADENTEI ARCULUI (Bow Attack Speed Upgrade) ---
    public void UpgradeBowSpeed()
    {
        // Validare suplimentara: verificam daca scriptul arcului este instantiat si valid in ierarhie
        if ((isLevelUpReward || playerStats.currentGold >= bowSpeedCost) && bowScript != null)
        {
            if (!isLevelUpReward) playerStats.SpendGold(bowSpeedCost);

            // Apelam metoda interna din BowWeapon pentru modificarea timpului de reincarcare
            bowScript.UpgradeFireRate();

            bowSpeedCost = Mathf.RoundToInt(bowSpeedCost * priceMultiplier);
            bowLevel++;

            if (isLevelUpReward) FinishLevelUp();
            else { UpdateUI(); playerStats.UpdateUI(); }
        }
    }

    // --- METODA APELATA DE PLAYERSTATS LA CRESTEREA IN NIVEL (State Overriding) ---
    public void OpenAsLevelUpMenu()
    {
        // Schimbam modul de functionare al ferestrei: activam flag-ul de gratuitate
        isLevelUpReward = true;
        upgradeUIPanel.SetActive(true);

        UpdateUI();

        // Fortam toate textele de cost sa afiseze string-ul special "FREE", mascand preturile numerice reale
        damagePriceText.text = "FREE";
        healthPriceText.text = "FREE";
        staminaPriceText.text = "FREE";
        speedPriceText.text = "FREE";
        bowPriceText.text = "FREE";
    }

    // Reseteaza starea ferestrei si reporneste fluxul normal de timp din motorul grafic
    private void FinishLevelUp()
    {
        isLevelUpReward = false;
        upgradeUIPanel.SetActive(false);

        // Readucem timpul global la starea sa live (1f) si deblocam pauza pentru a relua gameplay-ul
        Time.timeScale = 1f;
        PauseManager.isPaused = false;

        // Repornim desenarea textelor pentru a readuce preturile numerice calculate inapoi pe ecran
        UpdateUI();
    }

    // Actualizeaza elementele de tip text din interfata cu valorile matematice actuale din memorie
    void UpdateUI()
    {
        if (damagePriceText != null) damagePriceText.text = damageCost.ToString();
        if (healthPriceText != null) healthPriceText.text = healthCost.ToString();
        if (staminaPriceText != null) staminaPriceText.text = staminaCost.ToString();
        if (speedPriceText != null) speedPriceText.text = speedCost.ToString();
        if (bowPriceText != null) bowPriceText.text = bowSpeedCost.ToString();

        if (damageLevelText != null) damageLevelText.text = "Level " + damageLevel;
        if (healthLevelText != null) healthLevelText.text = "Level " + healthLevel;
        if (staminaLevelText != null) staminaLevelText.text = "Level " + staminaLevel;
        if (speedLevelText != null) speedLevelText.text = "Level " + speedLevel;
        if (bowLevelText != null) bowLevelText.text = "Level " + bowLevel;
    }

    private void OnTriggerEnter2D(Collider2D collision) { if (collision.CompareTag("Player")) isPlayerNear = true; }

    // Callback executat cand jucatorul paraseste zona fizica a NPC-ului de upgrade-uri
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = false;
            // Inchidem automat panoul daca jucatorul se indeparteaza
            if (upgradeUIPanel != null) upgradeUIPanel.SetActive(false);

            // Eliberam starea de busy a controllerului pentru a-i reda mobilitatea completa in gameplay
            if (playerCombat != null && playerCombat.playerMovement != null)
                playerCombat.playerMovement.isBusy = false;
        }
    }
}