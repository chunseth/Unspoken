using UnityEngine;
using System.Collections; // Added for IEnumerator

/// <summary>
/// Detects when the player enters the boss room and manages the boss health bar visibility.
/// Integrates with DungeonGenerator to detect boss room entry.
/// </summary>
public class BossRoomDetector : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the BossHealthBar component")]
    public BossHealthBar bossHealthBar;
    [Tooltip("Reference to the DungeonGenerator")]
    public DungeonGenerator dungeonGenerator;
    
    [Header("Detection Settings")]
    [Tooltip("How often to check if player is in boss room (seconds)")]
    public float checkInterval = 0.5f;
    
    private bool playerInBossRoom = false;
    private bool bossHealthBarShown = false;
    private float lastCheckTime = 0f;
    
    private void Awake()
    {
        Debug.Log("BossRoomDetector: Awake() called");
    }
    
    private void Start()
    {
        Debug.Log("BossRoomDetector: Start() called");
        
        // Find DungeonGenerator if not assigned
        if (dungeonGenerator == null)
        {
            Debug.Log("BossRoomDetector: Looking for DungeonGenerator...");
            dungeonGenerator = FindObjectOfType<DungeonGenerator>();
            if (dungeonGenerator != null)
            {
                Debug.Log("BossRoomDetector: Found DungeonGenerator");
            }
        }
        
        // Find BossHealthBar if not assigned (include inactive objects)
        if (bossHealthBar == null)
        {
            Debug.Log("BossRoomDetector: Looking for BossHealthBar (including inactive)...");
            bossHealthBar = FindObjectOfType<BossHealthBar>(true); // true = include inactive
            if (bossHealthBar != null)
            {
                Debug.Log($"BossRoomDetector: Found BossHealthBar: {bossHealthBar.name} - Active: {bossHealthBar.gameObject.activeInHierarchy}");
            }
        }
        
        if (dungeonGenerator == null)
        {
            Debug.LogWarning("BossRoomDetector: No DungeonGenerator found!");
        }
        
        if (bossHealthBar == null)
        {
            Debug.LogWarning("BossRoomDetector: No BossHealthBar found!");
        }
        
        Debug.Log("BossRoomDetector: Start() completed");
        
        // Check if assigned components are active
        CheckForDisabledComponents();
    }
    
    private void CheckForDisabledComponents()
    {
        Debug.Log("BossRoomDetector: Checking for disabled components...");
        
        // Check for disabled DungeonGenerator
        DungeonGenerator[] allDungeonGenerators = FindObjectsOfType<DungeonGenerator>(true); // true = include inactive
        if (allDungeonGenerators.Length > 0)+
        {
            Debug.Log($"BossRoomDetector: Found {allDungeonGenerators.Length} DungeonGenerator(s), checking if any are active...");
            foreach (DungeonGenerator dg in allDungeonGenerators)
            {
                Debug.Log($"BossRoomDetector: DungeonGenerator '{dg.name}' - Active: {dg.gameObject.activeInHierarchy}");
            }
        }
        
        // Check for disabled BossHealthBar
        BossHealthBar[] allBossHealthBars = FindObjectsOfType<BossHealthBar>(true); // true = include inactive
        if (allBossHealthBars.Length > 0)
        {
            Debug.Log($"BossRoomDetector: Found {allBossHealthBars.Length} BossHealthBar(s), checking if any are active...");
            foreach (BossHealthBar bhb in allBossHealthBars)
            {
                Debug.Log($"BossRoomDetector: BossHealthBar '{bhb.name}' - Active: {bhb.gameObject.activeInHierarchy}");
            }
        }
    }
    
    private void Update()
    {
        // Check at intervals to avoid performance issues
        if (Time.time - lastCheckTime >= checkInterval)
        {
            CheckPlayerInBossRoom();
            lastCheckTime = Time.time;
        }
    }
    
    private void CheckPlayerInBossRoom()
    {
        // Debug logging to see what's happening
        if (dungeonGenerator == null)
        {
            Debug.LogWarning("BossRoomDetector: DungeonGenerator is null!");
            return;
        }
        
        if (bossHealthBar == null)
        {
            Debug.LogWarning("BossRoomDetector: BossHealthBar is null!");
            return;
        }
        
        // Get player position
        Vector2Int playerPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y)
        );
        
        // Check if player is in boss room using DungeonGenerator's method
        bool currentlyInBossRoom = dungeonGenerator.IsPositionInBossRoom(playerPos.x, playerPos.y);
        
        // Debug logging
        if (Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
        {
            Debug.Log($"BossRoomDetector: Player at ({playerPos.x}, {playerPos.y}), In boss room: {currentlyInBossRoom}, Was in boss room: {playerInBossRoom}");
        }
        
        // Handle state change
        if (currentlyInBossRoom && !playerInBossRoom)
        {
            // Player just entered boss room
            OnPlayerEnterBossRoom();
        }
        else if (!currentlyInBossRoom && playerInBossRoom)
        {
            // Player just left boss room
            OnPlayerLeaveBossRoom();
        }
        
        playerInBossRoom = currentlyInBossRoom;
    }
    
    private void OnPlayerEnterBossRoom()
    {
        Debug.Log("Player entered boss room - showing boss health bar");
        
        if (bossHealthBar != null)
        {
            bossHealthBar.ShowHealthBar();
            bossHealthBarShown = true;
        }
    }
    
    private void OnPlayerLeaveBossRoom()
    {
        Debug.Log("Player left boss room - hiding boss health bar");
        
        if (bossHealthBar != null)
        {
            bossHealthBar.HideHealthBar();
            bossHealthBarShown = false;
        }
    }
    
    /// <summary>
    /// Returns whether the player is currently in the boss room
    /// </summary>
    public bool IsPlayerInBossRoom()
    {
        return playerInBossRoom;
    }
    
    /// <summary>
    /// Returns whether the boss health bar is currently shown
    /// </summary>
    public bool IsBossHealthBarShown()
    {
        return bossHealthBarShown;
    }
    
    /// <summary>
    /// Manually show the boss health bar (for testing or special cases)
    /// </summary>
    public void ShowBossHealthBar()
    {
        if (bossHealthBar != null)
        {
            bossHealthBar.ShowHealthBar();
            bossHealthBarShown = true;
        }
    }
    
    /// <summary>
    /// Manually hide the boss health bar (for testing or special cases)
    /// </summary>
    public void HideBossHealthBar()
    {
        if (bossHealthBar != null)
        {
            bossHealthBar.HideHealthBar();
            bossHealthBarShown = false;
        }
    }
    
    /// <summary>
    /// Manual test method - call this to test if the boss health bar system works
    /// </summary>
    [ContextMenu("Test Boss Health Bar")]
    public void TestBossHealthBar()
    {
        Debug.Log("BossRoomDetector: Manual test called");
        
        if (bossHealthBar == null)
        {
            Debug.LogError("BossRoomDetector: BossHealthBar is null in test!");
            return;
        }
        
        if (dungeonGenerator == null)
        {
            Debug.LogError("BossRoomDetector: DungeonGenerator is null in test!");
            return;
        }
        
        Debug.Log("BossRoomDetector: Testing boss health bar show/hide...");
        ShowBossHealthBar();
        
        // Hide after 3 seconds
        StartCoroutine(TestHideAfterDelay());
    }
    
    private IEnumerator TestHideAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        Debug.Log("BossRoomDetector: Hiding boss health bar after test delay");
        HideBossHealthBar();
    }
}
