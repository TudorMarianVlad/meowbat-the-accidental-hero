using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// Gestioneaza ciclul de viata al valurilor de inamici: instantiere, 
// scalarea statisticilor pe baza dificultatii si curatarea memoriei.
public class WaveManager : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    public GameObject slimePrefab;
    public GameObject bombPrefab;
    public GameObject shroomPrefab;

    [Header("Spawn Probabilities")]
    [Range(0, 100)] public float bombChance = 20f;
    [Range(0, 100)] public float shroomChance = 30f;
    public int shroomUnlockWave = 5;

    [Header("Difficulty Scaling")]
    public float healthIncreasePerWave = 15f;
    public int goldIncreasePerWave = 5;

    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    public int currentWave = 0;
    public int enemiesPerWave = 3;

    [Header("UI & Barriers")]
    public GameObject waveUIObject;
    public GameObject mobsUIObject;
    public GameObject arenaGate;

    private TextMeshProUGUI waveTMP;
    private TextMeshProUGUI mobsTMP;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool waveActive = false;

    void Start()
    {
        // Initializare UI.
        if (waveUIObject != null) waveTMP = waveUIObject.GetComponentInChildren<TextMeshProUGUI>();
        if (mobsUIObject != null) mobsTMP = mobsUIObject.GetComponentInChildren<TextMeshProUGUI>();

        HideWaveText();
        if (mobsUIObject != null) mobsUIObject.SetActive(false);
    }

    public void RegisterEnemy(GameObject enemy)
    {
        if (!activeEnemies.Contains(enemy)) activeEnemies.Add(enemy);
    }

    public void StartArena()
    {
        if (!waveActive)
        {
            if (arenaGate != null) arenaGate.SetActive(true);
            StartCoroutine(SpawnWave());
        }
    }

    // Corutina pentru generarea esalonata a inamicilor.
    IEnumerator SpawnWave()
    {
        waveActive = true;
        currentWave++;

        if (waveUIObject != null) { waveUIObject.SetActive(true); waveTMP.text = "WAVE " + currentWave; }
        if (mobsUIObject != null) mobsUIObject.SetActive(true);

        int countToSpawn = enemiesPerWave + ((currentWave - 1) * 2);

        for (int i = 0; i < countToSpawn; i++)
        {
            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject prefabToSpawn = slimePrefab;

            // Selectie aleatorie ponderata.
            float roll = Random.Range(0, 100);
            if (roll < bombChance) prefabToSpawn = bombPrefab;
            else if (currentWave >= shroomUnlockWave && roll < (bombChance + shroomChance)) prefabToSpawn = shroomPrefab;

            GameObject enemy = Instantiate(prefabToSpawn, sp.position, Quaternion.identity);
            activeEnemies.Add(enemy);

            // Scalare statistici inamic.
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.isArenaEnemy = true;
                int wavesSince = (prefabToSpawn == shroomPrefab) ? Mathf.Max(0, currentWave - shroomUnlockWave) : currentWave - 1;
                health.maxHealth += wavesSince * (int)healthIncreasePerWave;
                health.currentHealth = health.maxHealth;
                health.goldDroppedPerCoin += wavesSince * goldIncreasePerWave;
            }

            // Fortare AI agresiv.
            foreach (var ai in enemy.GetComponents<MonoBehaviour>())
            {
                if (ai.GetType().Name.Contains("AI"))
                    ai.GetType().GetField("instantChase")?.SetValue(ai, true);
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    // Resetare completa a starii arenei.
    public void ResetWaveSystem()
    {
        foreach (GameObject enemy in activeEnemies) if (enemy != null) Destroy(enemy);
        activeEnemies.Clear();
        StopAllCoroutines();
        currentWave = 0;
        waveActive = false;
        HideWaveText();
        if (mobsUIObject != null) mobsUIObject.SetActive(false);
        if (arenaGate != null) arenaGate.SetActive(false);
    }

    void Update()
    {
        if (!waveActive) return;

        // Curatare lista de referinte nule (inamic ucis).
        activeEnemies.RemoveAll(item => item == null);
        if (mobsTMP != null) mobsTMP.text = "MOBS LEFT: " + activeEnemies.Count;

        // Conditie sfarsit val.
        if (activeEnemies.Count == 0)
        {
            waveActive = false;
            waveTMP.text = "WAVE CLEARED!";
            Invoke(nameof(HideWaveText), 2f);
            Invoke(nameof(TriggerNextWave), 3f);
        }
    }

    void HideWaveText() => waveUIObject?.SetActive(false);
    void TriggerNextWave() => StartCoroutine(SpawnWave());
}