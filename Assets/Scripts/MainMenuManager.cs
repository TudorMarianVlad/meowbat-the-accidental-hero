using UnityEngine;
using UnityEngine.UI;
using System.IO;

// Gestioneaza starea meniului principal: coordoneaza interfata, 
// pauza globala si tranzitia catre gameplay (New Game sau Continue).
public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject hudPanel;

    [Header("Systems")]
    public PlayerStats playerStats;
    public MonoBehaviour waveManager;

    [Header("Buttons")]
    public Button continueButton;

    private void Start()
    {
        // Inghetare timp si stare de pauza la pornire.
        Time.timeScale = 0f;
        PauseManager.isPaused = true;

        // Validare fisier salvare pentru activarea butonului "Continue".
        string path = Application.persistentDataPath + "/savefile.json";
        if (continueButton != null)
        {
            continueButton.interactable = File.Exists(path);
        }

        mainMenuPanel.SetActive(true);
        hudPanel.SetActive(false);
    }

    // Metode apelate de butoane pentru tipul de joc.
    public void NewGame() => StartGame(false);
    public void ContinueGame() => StartGame(true);

    // Tranzitie de la Meniu la Gameplay.
    private void StartGame(bool loadSave)
    {
        // 1. Comutare UI.
        mainMenuPanel.SetActive(false);
        hudPanel.SetActive(true);

        // 2. Reluare timp si fizica.
        Time.timeScale = 1f;
        PauseManager.isPaused = false;

        // 3. Incarcare date (daca e cazul).
        if (loadSave && playerStats != null) playerStats.LoadPlayerData();

        // 4. Activare sistem inamici.
        if (waveManager != null) waveManager.enabled = true;
    }

    // Inchidere aplicatie.
    public void ExitGame() => Application.Quit();
}