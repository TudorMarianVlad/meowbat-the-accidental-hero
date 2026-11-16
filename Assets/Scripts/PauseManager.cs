using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Clasa PauseManager controleaza starea de inghetare a timpului global in joc,
// randarea meniului de pauza, interceptarea tastei dedicate (ex: Escape) si 
// expunerea metodelor publice pentru butoanele din interfata UI (Resume, Save, Quit).
public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    // Referinta catre panoul vizual de UI care contine butoanele meniului de pauza
    public GameObject pauseMenuUI;

    // Referinta catre elementul de text din interiorul canvasului dedicat mesajelor de avertizare/succes
    public TextMeshProUGUI saveWarningText;

    [Header("Save References")]
    // Referinte catre scripturile master din ierarhie, necesare pentru a fi trimise 
    // ca parametri catre SaveManager in momentul salvarii progresului.
    public PlayerStats playerStats;
    public PlayerCombat playerCombat;
    public UpgradeShopkeeper upgradeShop;

    
    public static bool isPaused = false;

    // Referinta catre clasa de controale generata din Unity New Input System
    private PlayerControls playerControls;

    private void Awake()
    {
        // Instantiem harta de actiuni hardware
        playerControls = new PlayerControls();
    }

    // Activam si dezactivam controalele in functie de ciclul de viata al scriptului,
    // gestionand curat abonarea la evenimente pentru a evita memory leaks in scena.
    private void OnEnable()
    {
        playerControls.Enable();

        playerControls.UI.Pause.performed += _ => TogglePause();
    }

    private void OnDisable()
    {
        playerControls.Disable();
        playerControls.UI.Pause.performed -= _ => TogglePause();
    }

    // Functie interna de tip comutator (Toggle) care determina actiunea pe baza starii curente
    private void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    public void SaveGameButton()
    {
        // Validare critica a dependentelor (Null Check):
        // Ne asiguram ca obiectele care detin datele ce trebuie salvate sunt prezente in memorie
        if (playerStats != null && upgradeShop != null)
        {
            // --- CRITICAL ARENA CHECK ---
            // If the player is currently inside the arena, block saving immediately and trigger UI warning
            if (playerStats.isInArena)
            {
                // Activam si coloram textul de avertizare direct pe Canvas-ul de Pauza
                if (saveWarningText != null)
                {
                    saveWarningText.text = "Cannot save in the Arena!";
                    saveWarningText.color = Color.red;
                    saveWarningText.gameObject.SetActive(true);
                }

                return; // Exits the method right here, preventing SaveGame from running!
            }


            // Aplicam injectarea de dependente: transmitem referintele actuale din scena 
            // catre metoda statica din SaveManager, care se va ocupa de serializarea lor in JSON.
            SaveManager.SaveGame(playerStats, upgradeShop);

            // Afisam un feedback vizual de succes de culoare verde direct in meniul de pauza
            if (saveWarningText != null)
            {
                saveWarningText.text = "Game Saved Successfully!";
                saveWarningText.color = Color.green;
                saveWarningText.gameObject.SetActive(true);
            }
        }
        
    }

    public void Resume()
    {
        // Ascundem textul de avertizare pentru a nu ramane vizibil la urmatoarea deschidere a meniului
        if (saveWarningText != null)
            saveWarningText.gameObject.SetActive(false);

        // 1. Ascundem panoul grafic de pe ecran
        pauseMenuUI.SetActive(false);

        // 2. Repornim motorul de timp al Unity (1f = timp normal, cadrele si fizica se calculeaza standard)
        Time.timeScale = 1f;

        // 3. Actualizam semnalizatorul global pentru a debloca input-ul celorlalte sisteme
        isPaused = false;
    }

    public void Pause()
    {
        // Resetam starea textului inainte de a afisa meniul de pauza
        if (saveWarningText != null)
            saveWarningText.gameObject.SetActive(false);

        // 1. Randam panoul grafic al meniului peste ecranul de joc
        pauseMenuUI.SetActive(true);

        // 2. Inghetam complet curgerea timpului in motorul Unity (0f = opreste update-urile fizice FixedUpdate, corutinele bazate pe secunde si animatiile)
        Time.timeScale = 0f;

        // 3. Activam semnalizatorul global de blocare a gameplay-ului
        isPaused = true;
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");

        // Inchide procesul sistemului de operare. 
        Application.Quit();
    }
}