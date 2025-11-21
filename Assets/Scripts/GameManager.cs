using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public RoombaController roombaController;
    public ShopManager shopManager;

    [Header("Level Progress")]
    public int totalDustTiles = 0;
    public int totalWaterPools = 0;
    private int dustTilesCleared = 0;
    private int waterPoolsCleared = 0;
    private bool objectivesCalculated = false;

    [Header("Economy")]
    public int botcoins = 200;
    public int levelCompletionReward = 200;

    [Header("UI References")]
    public TextMeshProUGUI botcoinText;
    public TextMeshProUGUI dustTilesText;
    public TextMeshProUGUI waterPoolsText;
    public GameObject jobCompletePanel;
    public GameObject levelCompletePanel;
    public TextMeshProUGUI statsText;

    [Header("Charging Station")]
    public GameObject chargingStationIndicator;
    public float requiredCenterDistance = 1.5f;
    private Vector3 indicatorStartPos;
    private bool isLevelComplete = false;

    [Header("Audio")]
    public AudioClip levelCompleteSound;
    public AudioSource audioSource;

    void Start()
    {
        if (roombaController == null)
        {
            roombaController = FindFirstObjectByType<RoombaController>();
        }

        if (shopManager == null)
        {
            shopManager = FindFirstObjectByType<ShopManager>();
        }

        dustTilesCleared = 0;
        waterPoolsCleared = 0;
        isLevelComplete = false;
        objectivesCalculated = false;

        Debug.Log("Level starting - counters reset to 0");

        if (jobCompletePanel != null)
        {
            jobCompletePanel.SetActive(false);
        }

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }

        StartCoroutine(DelayedCalculateObjectives());

        botcoins = PlayerPrefs.GetInt("Botcoins", 200);
        Debug.Log("Loaded botcoins: " + botcoins);

        if (chargingStationIndicator != null)
        {
            indicatorStartPos = chargingStationIndicator.transform.localPosition;
            chargingStationIndicator.SetActive(false);
        }

        UpdateBotcoinUI();
    }

    System.Collections.IEnumerator DelayedCalculateObjectives()
    {
        Debug.Log("Waiting for end of frame to count objectives...");
        yield return new WaitForEndOfFrame();

        Debug.Log("End of frame reached, calculating objectives now...");

        CalculateLevelObjectives();

        objectivesCalculated = true;

        Debug.Log("Objectives calculated and ready!");

        UpdateDustTilesUI();
        UpdateWaterPoolsUI();
    }

    void CalculateLevelObjectives()
    {
        GameObject[] allDustTiles = GameObject.FindGameObjectsWithTag("DustTile");
        totalDustTiles = allDustTiles.Length;
        Debug.Log("Total dust tiles found: " + totalDustTiles);

        GameObject[] waterPools = GameObject.FindGameObjectsWithTag("WaterPuddle");
        totalWaterPools = waterPools.Length;
        Debug.Log("Total water pools: " + totalWaterPools);
    }

    void Update()
    {
        if (chargingStationIndicator != null && chargingStationIndicator.activeSelf)
        {
            float bounce = Mathf.Sin(Time.time * 3f) * 0.3f;
            Vector3 newPos = indicatorStartPos;
            newPos.y += bounce;
            chargingStationIndicator.transform.localPosition = newPos;
        }
        if (Input.GetKeyDown(KeyCode.R) && Input.GetKey(KeyCode.LeftShift))
        {
            Debug.Log("=== RESETTING ALL SAVED DATA ===");
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("Data cleared! Reloading scene...");
            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("=== CURRENT SAVED DATA ===");
            Debug.Log("Botcoins: " + PlayerPrefs.GetInt("Botcoins", -1));
            Debug.Log("Speed Level: " + PlayerPrefs.GetInt("SpeedLevel", -1));
            Debug.Log("Battery Level: " + PlayerPrefs.GetInt("BatteryLevel", -1));
            Debug.Log("Has Water Attachment: " + PlayerPrefs.GetInt("HasWaterAttachment", -1));
            Debug.Log("Water Resistance: " + PlayerPrefs.GetInt("WaterResistanceLevel", -1));
        }
    }

    public void OnDustTileCleared()
    {
        dustTilesCleared++;
        Debug.Log("Dust cleared: " + dustTilesCleared + "/" + totalDustTiles);

        UpdateDustTilesUI();
        CheckLevelComplete();
    }

    public void OnWaterPoolCleared()
    {
        waterPoolsCleared++;
        Debug.Log("Water cleared: " + waterPoolsCleared + "/" + totalWaterPools);

        UpdateWaterPoolsUI();
        CheckLevelComplete();
    }

    void CheckLevelComplete()
    {
        Debug.Log("========================================");
        Debug.Log("=== CheckLevelComplete CALLED ===");
        Debug.Log("Called from: " + System.Environment.StackTrace);
        Debug.Log("objectivesCalculated: " + objectivesCalculated);
        Debug.Log("totalDustTiles: " + totalDustTiles);
        Debug.Log("totalWaterPools: " + totalWaterPools);
        Debug.Log("dustTilesCleared: " + dustTilesCleared);
        Debug.Log("waterPoolsCleared: " + waterPoolsCleared);
        Debug.Log("========================================");

        if (!objectivesCalculated)
        {
            Debug.Log("EARLY EXIT: Objectives not yet calculated");
            return;
        }

        if (totalDustTiles <= 0 && totalWaterPools <= 0)
        {
            Debug.LogWarning("EARLY EXIT: No objectives found");
            return;
        }

        bool allDustCleared = (dustTilesCleared >= totalDustTiles);
        bool allWaterCleared = (waterPoolsCleared >= totalWaterPools);

        Debug.Log("Completion check - Dust: " + allDustCleared + " | Water: " + allWaterCleared);

        if (allDustCleared && allWaterCleared)
        {
            Debug.Log("=== TRIGGERING JOB COMPLETE ===");

            if (jobCompletePanel != null && !jobCompletePanel.activeSelf)
            {
                jobCompletePanel.SetActive(true);
            }

            if (chargingStationIndicator != null && !chargingStationIndicator.activeSelf)
            {
                chargingStationIndicator.SetActive(true);
            }
        }
    }

    void UpdateDustTilesUI()
    {
        if (dustTilesText != null)
        {
            dustTilesText.text = " " + dustTilesCleared + " ";
        }
    }

    void UpdateWaterPoolsUI()
    {
        if (waterPoolsText != null)
        {
            waterPoolsText.text = "Water: " + waterPoolsCleared + "/" + totalWaterPools;
        }
    }

    public void OnTrashCollected(int moneyAmount)
    {
        Debug.Log("Trash collected! Earned: $" + moneyAmount);
        
        // Add money immediately
        AddBotcoins(moneyAmount);
        
        // Optional: Show a popup or feedback
        // You could add a text popup here that shows "+$5!" at the collection location
    }

    public void OnReturnedToChargingStation()
    {
        if (isLevelComplete) return;

        if (!objectivesCalculated)
        {
            Debug.Log("At charging station but objectives not yet calculated - ignoring");
            return;
        }

        if (totalDustTiles <= 0 && totalWaterPools <= 0)
        {
            Debug.Log("At charging station but no objectives in level - ignoring");
            return;
        }

        bool allDustCleared = (dustTilesCleared >= totalDustTiles);
        bool allWaterCleared = (waterPoolsCleared >= totalWaterPools);

        if (allDustCleared && allWaterCleared)
        {
            Debug.Log("At charging station with all objectives complete!");
            CompleteLevel();
        }
        else
        {
            Debug.Log("At charging station but objectives incomplete:");
            Debug.Log("Dust: " + dustTilesCleared + "/" + totalDustTiles);
            Debug.Log("Water: " + waterPoolsCleared + "/" + totalWaterPools);
        }
    }

    void CompleteLevel()
    {
        if (isLevelComplete) return;

        isLevelComplete = true;

        Debug.Log("=== LEVEL COMPLETE ===");

        if (jobCompletePanel != null)
        {
            jobCompletePanel.SetActive(false);
        }

        // Stop ALL Roomba sounds
        if (roombaController != null)
        {
            // Stop motor sound (vacuum sound)
            if (roombaController.motorSound != null)
            {
                roombaController.motorSound.Stop();
                roombaController.motorSound.volume = 0f;
                Debug.Log("Stopped motor sound");
            }
            
            // Stop idle mode music
            if (roombaController.idleModeMusic != null)
            {
                roombaController.StopAllCoroutines(); // Stop fade coroutines
                roombaController.idleModeMusic.Stop();
                roombaController.idleModeMusic.volume = 0f;
                Debug.Log("Stopped idle mode music");
            }
            
            // Turn off idle mode if active
            if (roombaController.isIdleMode)
            {
                roombaController.isIdleMode = false;
                
                // Reset camera and UI
                if (roombaController.cameraController != null)
                {
                    roombaController.cameraController.SetBirdsEyeView(false);
                }
                
                if (roombaController.uiPositionManager != null)
                {
                    roombaController.uiPositionManager.SetBirdsEyeMode(false);
                }
            }
        }

        AddBotcoins(levelCompletionReward);

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);

            if (statsText != null)
            {
                statsText.text = "LEVEL COMPLETE!\n\n" +
                            "Dust Cleaned: " + dustTilesCleared + "/" + totalDustTiles + "\n" +
                            "Water Cleaned: " + waterPoolsCleared + "/" + totalWaterPools + "\n" +
                            "Payment: $" + levelCompletionReward + "\n\n" +
                            "Total Cash: $" + botcoins;
            }
        }

        if (audioSource != null && levelCompleteSound != null)
        {
            StartCoroutine(PlaySoundDelayed(0.3f));
        }

        if (roombaController != null)
        {
            roombaController.SetLevelComplete();
        }
    }

    System.Collections.IEnumerator PlaySoundDelayed(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); 

        if (audioSource != null && levelCompleteSound != null)
        {
            audioSource.PlayOneShot(levelCompleteSound);
        }
    }

    public void AddBotcoins(int amount)
    {
        botcoins += amount;
        PlayerPrefs.SetInt("Botcoins", botcoins);
        PlayerPrefs.Save();

        UpdateBotcoinUI();

        if (shopManager != null)
        {
            shopManager.UpdateUpgradeUI();
        }
    }

    public bool SpendBotcoins(int amount)
    {
        if (botcoins >= amount)
        {
            botcoins -= amount;
            PlayerPrefs.SetInt("Botcoins", botcoins);
            PlayerPrefs.Save();

            UpdateBotcoinUI();
            return true;
        }
        return false;
    }

    void UpdateBotcoinUI()
    {
        if (botcoinText != null)
        {
            botcoinText.text = "$" + botcoins;
        }
    }



    public void LoadNextLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("No more levels!");
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void Awake()
    {
        Debug.Log("=== GameManager Awake ===");

        if (jobCompletePanel != null)
        {
            Debug.Log("Job Complete Panel state: " + jobCompletePanel.activeSelf);
            jobCompletePanel.SetActive(false);
            Debug.Log("Job Complete Panel forcibly hidden");
        }

        if (levelCompletePanel != null)
        {
            Debug.Log("Level Complete Panel state: " + levelCompletePanel.activeSelf);
            levelCompletePanel.SetActive(false);
            Debug.Log("Level Complete Panel forcibly hidden");
        }

        Time.timeScale = 1;
    }
    public void ResetAllProgress()
    {
        Debug.Log("=== RESETTING ALL PROGRESS VIA BUTTON ===");

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Time.timeScale = 1;

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}