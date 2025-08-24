using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Manages the game over screen and handles pausing all game elements when the player dies.
/// Uses the same logic as CrackedWall.cs to freeze projectiles, enemy AI, and enemy spawners.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The game over panel that appears when the player dies")]
    public GameObject gameOverPanel;
    [Tooltip("Background image for the game over screen")]
    public Image backgroundImage;
    [Tooltip("Button to restart the current level")]
    public Button restartButton;
    [Tooltip("Button to return to main menu")]
    public Button mainMenuButton;
    [Tooltip("Button to quit the game")]
    public Button quitButton;
    
    [Header("Game Over Settings")]
    [Tooltip("Delay before showing the game over screen (in seconds)")]
    public float gameOverDelay = 1f;
    [Tooltip("Whether to pause the game when game over occurs")]
    public bool pauseGameOnGameOver = true;
    
    [Header("Player Reference")]
    [Tooltip("Reference to the player GameObject")]
    public GameObject player;
    
    private bool isGameOver = false;
    private bool isGameOverScreenActive = false;
    
    // Lists to store frozen game elements (same as CrackedWall.cs)
    private List<Rigidbody2D> frozenProjectiles = new List<Rigidbody2D>();
    private List<Vector2> projectileVelocities = new List<Vector2>();
    private List<MonoBehaviour> frozenEnemyAI = new List<MonoBehaviour>();
    private List<MonoBehaviour> frozenEnemyShooters = new List<MonoBehaviour>();
    private List<MonoBehaviour> frozenEnemySpawners = new List<MonoBehaviour>();
    
    // Store original time scale
    private float originalTimeScale;
    
    void Start()
    {
        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("GameOverManager: No player found with 'Player' tag. Please assign the player reference manually.");
            }
        }
        
        // Validate and set up UI references
        ValidateUIReferences();
        
        // Set up UI event listeners
        SetupUIEventListeners();
        
        // Initially hide the game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("GameOverManager: Game over panel is not assigned! Please assign it in the inspector.");
        }
        
        // Initially hide the background image
        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(false);
        }
        
        // Subscribe to player health changes
        PlayerStats playerStats = player?.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.OnHealthChanged += OnPlayerHealthChanged;
        }
        else
        {
            Debug.LogWarning("GameOverManager: No PlayerStats component found on player!");
        }
        
        // Store original time scale
        originalTimeScale = Time.timeScale;
    }
    
    private void ValidateUIReferences()
    {
        // Check if we're in the scene (not a prefab)
        if (Application.isPlaying)
        {
            // If references are missing, try to find them in the scene
            if (gameOverPanel == null)
            {
                gameOverPanel = GameObject.Find("GameOverPanel");
                if (gameOverPanel != null)
                {
                    Debug.Log("GameOverManager: Found GameOverPanel in scene automatically.");
                }
            }
            
            if (restartButton == null)
            {
                Button foundButton = GameObject.Find("RestartButton")?.GetComponent<Button>();
                if (foundButton != null)
                {
                    restartButton = foundButton;
                    Debug.Log("GameOverManager: Found RestartButton in scene automatically.");
                }
            }
            
            if (mainMenuButton == null)
            {
                Button foundButton = GameObject.Find("MainMenuButton")?.GetComponent<Button>();
                if (foundButton != null)
                {
                    mainMenuButton = foundButton;
                    Debug.Log("GameOverManager: Found MainMenuButton in scene automatically.");
                }
            }
            
            if (quitButton == null)
            {
                Button foundButton = GameObject.Find("QuitButton")?.GetComponent<Button>();
                if (foundButton != null)
                {
                    quitButton = foundButton;
                    Debug.Log("GameOverManager: Found QuitButton in scene automatically.");
                }
            }
            
            if (backgroundImage == null)
            {
                backgroundImage = GameObject.Find("GameOverBackground")?.GetComponent<Image>();
                if (backgroundImage != null)
                {
                    Debug.Log("GameOverManager: Found background image in scene automatically.");
                }
            }
        }
    }
    
    private void SetupUIEventListeners()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartLevel);
        }
        else
        {
            Debug.LogError("GameOverManager: Restart button is not assigned! Please assign it in the inspector.");
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
        else
        {
            Debug.LogWarning("GameOverManager: Main menu button is not assigned. Main menu functionality will not work.");
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
        else
        {
            Debug.LogWarning("GameOverManager: Quit button is not assigned. Quit functionality will not work.");
        }
    }
    
    private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        // Check if player has died
        if (currentHealth <= 0 && !isGameOver)
        {
            TriggerGameOver();
        }
    }
    
    public void TriggerGameOver()
    {
        if (isGameOver) return; // Prevent multiple triggers
        
        isGameOver = true;
        Debug.Log("GameOverManager: Game Over triggered!");
        
        // Freeze all game elements
        FreezeGameElements();
        
        // Show game over screen after delay
        ShowGameOverScreen();
        
        // Pause the game if enabled (after showing screen)
        if (pauseGameOnGameOver)
        {
            Time.timeScale = 0f;
        }
    }
    
    private void ShowGameOverScreen()
    {
        Debug.Log("GameOverManager: ShowGameOverScreen called");
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            isGameOverScreenActive = true;
            
            // Show the background image
            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(true);
            }
            
            Debug.Log("GameOverManager: Game over screen displayed successfully.");
        }
        else
        {
            Debug.LogError("GameOverManager: Cannot show game over screen - gameOverPanel is null!");
            
            // Try to find it again
            gameOverPanel = GameObject.Find("GameOverPanel");
            if (gameOverPanel != null)
            {
                Debug.Log("GameOverManager: Found GameOverPanel, trying to show it now...");
                gameOverPanel.SetActive(true);
                isGameOverScreenActive = true;
                
                if (backgroundImage != null)
                {
                    backgroundImage.gameObject.SetActive(true);
                }
                
                Debug.Log("GameOverManager: Game over screen displayed after re-finding panel.");
            }
            else
            {
                Debug.LogError("GameOverManager: Still cannot find GameOverPanel! Please create a UI Panel named 'GameOverPanel'.");
            }
        }
    }
    
    private void FreezeGameElements()
    {
        // Freeze all spell projectiles
        FreezeSpellProjectiles();
        
        // Freeze enemy AI and shooter behavior
        FreezeEnemyBehavior();
        
        // Freeze enemy spawners
        FreezeEnemySpawners();
        
        // Pause all spawn indicators
        SpawnIndicator.PauseAllSpawnIndicators();
        
        // Disable player controls
        DisablePlayerControls();
        
        Debug.Log("GameOverManager: All game elements frozen.");
    }
    
    private void FreezeSpellProjectiles()
    {
        // Clear previous lists
        frozenProjectiles.Clear();
        projectileVelocities.Clear();
        
        // Find all spell projectiles in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // Check if this is a spell projectile
            if (IsSpellProjectile(obj))
            {
                Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
                if (rb != null && rb.simulated)
                {
                    // Store the projectile and its velocity
                    frozenProjectiles.Add(rb);
                    projectileVelocities.Add(rb.velocity);
                    
                    // Freeze the projectile
                    rb.simulated = false;
                    rb.velocity = Vector2.zero;
                    
                    Debug.Log($"GameOverManager: Frozen spell projectile: {obj.name}");
                }
            }
        }
        
        Debug.Log($"GameOverManager: Frozen {frozenProjectiles.Count} spell projectiles");
    }
    
    private bool IsSpellProjectile(GameObject obj)
    {
        // Check for common spell projectile identifiers
        // Adjust these conditions based on your spell system
        
        // Check by tag
        if (obj.CompareTag("SpellProjectile"))
            return true;
        
        // Check by component name
        if (obj.GetComponent("SpellProjectile") != null)
            return true;
        
        return false;
    }
    
    private void FreezeEnemyBehavior()
    {
        // Clear previous lists
        frozenEnemyAI.Clear();
        frozenEnemyShooters.Clear();
        
        // Find all enemies in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // Check if this is an enemy
            if (IsEnemy(obj))
            {
                // Freeze enemy AI components
                FreezeEnemyAIComponents(obj);
                
                // Freeze enemy shooter components
                FreezeEnemyShooterComponents(obj);
            }
        }
        
        Debug.Log($"GameOverManager: Frozen {frozenEnemyAI.Count} enemy AI components and {frozenEnemyShooters.Count} enemy shooter components");
    }
    
    private bool IsEnemy(GameObject obj)
    {
        // Check for common enemy identifiers
        // Adjust these conditions based on your enemy system
        
        // Check by tag
        if (obj.CompareTag("Enemy"))
            return true;
        
        // Check by component name
        if (obj.GetComponent("EnemyAI") != null || obj.GetComponent("EnemyShooter") != null)
            return true;
        
        return false;
    }
    
    private void FreezeEnemyAIComponents(GameObject enemy)
    {
        // Common enemy AI component names - adjust based on your system
        string[] aiComponentNames = {
            "EnemyAI"
        };
        
        foreach (string componentName in aiComponentNames)
        {
            MonoBehaviour aiComponent = enemy.GetComponent(componentName) as MonoBehaviour;
            if (aiComponent != null && aiComponent.enabled)
            {
                frozenEnemyAI.Add(aiComponent);
                aiComponent.enabled = false;
                Debug.Log($"GameOverManager: Frozen enemy AI component {componentName} on {enemy.name}");
            }
        }
    }
    
    private void FreezeEnemyShooterComponents(GameObject enemy)
    {
        // Common enemy shooter component names - adjust based on your system
        string[] shooterComponentNames = {
            "EnemyShooter"
        };
        
        foreach (string componentName in shooterComponentNames)
        {
            MonoBehaviour shooterComponent = enemy.GetComponent(componentName) as MonoBehaviour;
            if (shooterComponent != null && shooterComponent.enabled)
            {
                frozenEnemyShooters.Add(shooterComponent);
                shooterComponent.enabled = false;
                Debug.Log($"GameOverManager: Frozen enemy shooter component {componentName} on {enemy.name}");
            }
        }
    }
    
    private void FreezeEnemySpawners()
    {
        // Clear previous list
        frozenEnemySpawners.Clear();
        
        // Find all enemy spawners in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // Check if this is an enemy spawner
            if (IsEnemySpawner(obj))
            {
                // Freeze spawner components
                FreezeSpawnerComponents(obj);
            }
        }
        
        Debug.Log($"GameOverManager: Frozen {frozenEnemySpawners.Count} enemy spawner components");
    }
    
    private bool IsEnemySpawner(GameObject obj)
    {
        // Check for common enemy spawner identifiers
        // Adjust these conditions based on your spawner system
        
        // Check by tag
        if (obj.CompareTag("EnemySpawner"))
            return true;
        
        // Check by component name
        if (obj.GetComponent("EnemySpawner") != null || obj.GetComponent("CrackedWall") != null)
            return true;
        
        return false;
    }
    
    private void FreezeSpawnerComponents(GameObject spawner)
    {
        // Common spawner component names - adjust based on your system
        string[] spawnerComponentNames = {
            "EnemySpawner",
            "CrackedWall" // CrackedWall has enemy spawning functionality
        };
        
        foreach (string componentName in spawnerComponentNames)
        {
            MonoBehaviour spawnerComponent = spawner.GetComponent(componentName) as MonoBehaviour;
            if (spawnerComponent != null && spawnerComponent.enabled)
            {
                frozenEnemySpawners.Add(spawnerComponent);
                spawnerComponent.enabled = false;
                Debug.Log($"GameOverManager: Frozen enemy spawner component {componentName} on {spawner.name}");
            }
        }
    }
    
    private void DisablePlayerControls()
    {
        // Disable player movement
        PlayerController playerController = player?.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Disable player attack
        PlayerAttack playerAttack = player?.GetComponent<PlayerAttack>();
        if (playerAttack != null)
        {
            playerAttack.enabled = false;
        }
        
        Debug.Log("GameOverManager: Player controls disabled.");
    }
    
    private void UnfreezeGameElements()
    {
        // Unfreeze all spell projectiles
        UnfreezeSpellProjectiles();
        
        // Unfreeze enemy AI and shooter behavior
        UnfreezeEnemyBehavior();
        
        // Unfreeze enemy spawners
        UnfreezeEnemySpawners();
        
        // Resume all spawn indicators
        SpawnIndicator.ResumeAllSpawnIndicators();
        
        Debug.Log("GameOverManager: All game elements unfrozen.");
    }
    
    private void UnfreezeSpellProjectiles()
    {
        // Restore all frozen projectiles
        for (int i = 0; i < frozenProjectiles.Count; i++)
        {
            if (frozenProjectiles[i] != null)
            {
                // Restore the projectile's physics and velocity
                frozenProjectiles[i].simulated = true;
                frozenProjectiles[i].velocity = projectileVelocities[i];
                
                Debug.Log($"GameOverManager: Unfrozen spell projectile: {frozenProjectiles[i].gameObject.name}");
            }
        }
        
        Debug.Log($"GameOverManager: Unfrozen {frozenProjectiles.Count} spell projectiles");
        
        // Clear the lists
        frozenProjectiles.Clear();
        projectileVelocities.Clear();
    }
    
    private void UnfreezeEnemyBehavior()
    {
        // Unfreeze enemy AI components
        foreach (MonoBehaviour aiComponent in frozenEnemyAI)
        {
            if (aiComponent != null)
            {
                aiComponent.enabled = true;
                Debug.Log($"GameOverManager: Unfrozen enemy AI component {aiComponent.GetType().Name} on {aiComponent.gameObject.name}");
            }
        }
        
        // Unfreeze enemy shooter components
        foreach (MonoBehaviour shooterComponent in frozenEnemyShooters)
        {
            if (shooterComponent != null)
            {
                shooterComponent.enabled = true;
                Debug.Log($"GameOverManager: Unfrozen enemy shooter component {shooterComponent.GetType().Name} on {shooterComponent.gameObject.name}");
            }
        }
        
        Debug.Log($"GameOverManager: Unfrozen {frozenEnemyAI.Count} enemy AI components and {frozenEnemyShooters.Count} enemy shooter components");
        
        // Clear the lists
        frozenEnemyAI.Clear();
        frozenEnemyShooters.Clear();
    }
    
    private void UnfreezeEnemySpawners()
    {
        // Unfreeze enemy spawner components
        foreach (MonoBehaviour spawnerComponent in frozenEnemySpawners)
        {
            if (spawnerComponent != null)
            {
                spawnerComponent.enabled = true;
                Debug.Log($"GameOverManager: Unfrozen enemy spawner component {spawnerComponent.GetType().Name} on {spawnerComponent.gameObject.name}");
            }
        }
        
        Debug.Log($"GameOverManager: Unfrozen {frozenEnemySpawners.Count} enemy spawner components");
        
        // Clear the list
        frozenEnemySpawners.Clear();
    }
    
    private void ReenablePlayerControls()
    {
        // Re-enable player movement
        PlayerController playerController = player?.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        // Re-enable player attack
        PlayerAttack playerAttack = player?.GetComponent<PlayerAttack>();
        if (playerAttack != null)
        {
            playerAttack.enabled = true;
        }
        
        Debug.Log("GameOverManager: Player controls re-enabled.");
    }
    
    public void RestartLevel()
    {
        Debug.Log("GameOverManager: Restarting level...");
        
        try
        {
            // Restore time scale
            Time.timeScale = originalTimeScale;
            Debug.Log("GameOverManager: Time scale restored");
            
            // Unfreeze all game elements
            UnfreezeGameElements();
            Debug.Log("GameOverManager: Game elements unfrozen");
            
            // Re-enable player controls
            ReenablePlayerControls();
            Debug.Log("GameOverManager: Player controls re-enabled");
            
            // Reset game over state
            isGameOver = false;
            isGameOverScreenActive = false;
            Debug.Log("GameOverManager: Game over state reset");
            
            // Hide game over screen
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
            
            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(false);
            }
            Debug.Log("GameOverManager: Game over screen hidden");
            
            // Reset static game state before scene reload
            ResetGameState();
            Debug.Log("GameOverManager: Static game state reset");
            
            // Reload the current scene
            Debug.Log($"GameOverManager: Loading scene: {SceneManager.GetActiveScene().name}");
            
            // Use a simple scene reload approach
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
            
            // Fallback: if scene doesn't load, try again after a frame
            StartCoroutine(EnsureSceneReload(currentSceneName));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GameOverManager: Error during restart: {e.Message}");
            Debug.LogError($"GameOverManager: Stack trace: {e.StackTrace}");
            
            // Fallback restart without complex reset
            Debug.Log("GameOverManager: Attempting fallback restart...");
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
        }
    }
    
    /// <summary>
    /// Coroutine to ensure scene reload happens
    /// </summary>
    private System.Collections.IEnumerator EnsureSceneReload(string sceneName)
    {
        yield return new WaitForEndOfFrame();
        
        // Check if we're still in the same scene
        if (SceneManager.GetActiveScene().name == sceneName)
        {
            Debug.LogWarning("GameOverManager: Scene reload may have failed, trying again...");
            SceneManager.LoadScene(sceneName);
        }
    }
    
    /// <summary>
    /// Simple restart method that bypasses complex reset logic
    /// </summary>
    [ContextMenu("Simple Restart")]
    public void SimpleRestart()
    {
        Debug.Log("GameOverManager: Simple restart called");
        
        // Just reload the scene without any complex reset logic
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"GameOverManager: Simple restart - loading scene: {currentSceneName}");
        SceneManager.LoadScene(currentSceneName);
    }
    
    /// <summary>
    /// Reset all static game state that persists between scene reloads
    /// </summary>
    private void ResetGameState()
    {
        try
        {
            // Reset hole falling state
            Hole.ResetGlobalFallingState();
            Debug.Log("GameOverManager: Hole falling state reset");
            
            // Reset spawn indicator state
            SpawnIndicator.ResetGlobalPauseState();
            Debug.Log("GameOverManager: Spawn indicator state reset");
            
            // Reset hint manager state if it exists (simplified approach)
            if (HintManager.Instance != null)
            {
                Debug.Log("GameOverManager: HintManager found, resetting...");
                HintManager.Instance.ResetAllHintStates();
                HintManager.Instance.EmergencyResetHintMiniImages();
                Debug.Log("GameOverManager: HintManager reset complete");
            }
            else
            {
                Debug.Log("GameOverManager: No HintManager instance found");
            }
            
            Debug.Log("GameOverManager: Reset all static game state for scene reload");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GameOverManager: Error in ResetGameState: {e.Message}");
            Debug.LogError($"GameOverManager: Stack trace: {e.StackTrace}");
        }
    }
    
    public void ReturnToMainMenu()
    {
        Debug.Log("GameOverManager: Returning to main menu...");
        
        // Restore time scale
        Time.timeScale = originalTimeScale;
        
        // Load main menu scene (adjust scene name as needed)
        SceneManager.LoadScene("MainMenu");
    }
    
    public void QuitGame()
    {
        Debug.Log("GameOverManager: Quitting game...");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    void Update()
    {
        // Keyboard shortcut for testing game over (G key)
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("GameOverManager: G key pressed - testing game over!");
            ManualTriggerGameOver();
        }
        
        // Keyboard shortcut for testing simple restart (R key)
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("GameOverManager: R key pressed - testing simple restart!");
            SimpleRestart();
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        PlayerStats playerStats = player?.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= OnPlayerHealthChanged;
        }
        
        // Restore time scale if destroyed
        if (Time.timeScale == 0f)
        {
            Time.timeScale = originalTimeScale;
        }
    }
    
    // Public method to check game over status
    public bool IsGameOver()
    {
        return isGameOver;
    }
    
    public bool IsGameOverScreenActive()
    {
        return isGameOverScreenActive;
    }
    
    // Helper method to validate references
    [ContextMenu("Validate References")]
    public void ValidateReferences()
    {
        Debug.Log("=== GameOverManager Reference Validation ===");
        Debug.Log($"Game Over Panel: {(gameOverPanel != null ? "✓ Assigned" : "✗ Missing")}");
        Debug.Log($"Background Image: {(backgroundImage != null ? "✓ Assigned" : "✗ Missing (optional)")}");
        Debug.Log($"Restart Button: {(restartButton != null ? "✓ Assigned" : "✗ Missing")}");
        Debug.Log($"Main Menu Button: {(mainMenuButton != null ? "✓ Assigned" : "✗ Missing (optional)")}");
        Debug.Log($"Quit Button: {(quitButton != null ? "✓ Assigned" : "✗ Missing (optional)")}");
        Debug.Log($"Player: {(player != null ? "✓ Assigned" : "✗ Missing (will auto-find)")}");
        Debug.Log($"Game Over Delay: {gameOverDelay}s");
        Debug.Log($"Pause Game on Game Over: {pauseGameOnGameOver}");
        Debug.Log($"Is Game Over: {isGameOver}");
        Debug.Log($"Is Game Over Screen Active: {isGameOverScreenActive}");
        Debug.Log($"Frozen Projectiles: {frozenProjectiles.Count}");
        Debug.Log($"Frozen Enemy AI: {frozenEnemyAI.Count}");
        Debug.Log($"Frozen Enemy Shooters: {frozenEnemyShooters.Count}");
        Debug.Log($"Frozen Enemy Spawners: {frozenEnemySpawners.Count}");
        
        // Additional debugging for UI elements
        if (gameOverPanel != null)
        {
            Debug.Log($"Game Over Panel Active: {gameOverPanel.activeInHierarchy}");
            Debug.Log($"Game Over Panel Active Self: {gameOverPanel.activeSelf}");
            Canvas canvas = gameOverPanel.GetComponentInParent<Canvas>();
            Debug.Log($"Parent Canvas: {(canvas != null ? canvas.name : "None")}");
            if (canvas != null)
            {
                Debug.Log($"Canvas Render Mode: {canvas.renderMode}");
                Debug.Log($"Canvas Active: {canvas.gameObject.activeInHierarchy}");
            }
        }
        
        // Check if player has PlayerStats
        if (player != null)
        {
            PlayerStats playerStats = player.GetComponent<PlayerStats>();
            Debug.Log($"Player has PlayerStats: {playerStats != null}");
            if (playerStats != null)
            {
                Debug.Log($"Player Health: {playerStats.GetCurrentHealth()}/{playerStats.GetMaxHealth()}");
            }
        }
        
        Debug.Log("=============================================");
    }
    
    // Manual trigger for testing
    [ContextMenu("Trigger Game Over (Manual)")]
    public void ManualTriggerGameOver()
    {
        Debug.Log("GameOverManager: Manual game over trigger called!");
        TriggerGameOver();
    }
    
    // Immediate test without delay
    [ContextMenu("Show Game Over Screen (Immediate)")]
    public void ShowGameOverScreenImmediate()
    {
        Debug.Log("GameOverManager: Showing game over screen immediately!");
        ShowGameOverScreen();
    }
    
    // Debug method to find UI elements in scene
    [ContextMenu("Find UI Elements")]
    public void FindUIElements()
    {
        Debug.Log("=== Searching for UI Elements ===");
        
        // Look for game over related UI elements
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("gameover") || obj.name.ToLower().Contains("game over"))
            {
                Debug.Log($"Found potential game over UI element: {obj.name} (Active: {obj.activeInHierarchy})");
            }
        }
        
        // Look for buttons
        Button[] buttons = FindObjectsOfType<Button>();
        foreach (Button button in buttons)
        {
            if (button.name.ToLower().Contains("restart") || button.name.ToLower().Contains("menu") || button.name.ToLower().Contains("quit"))
            {
                Debug.Log($"Found potential game over button: {button.name}");
            }
        }
        
        // Look for panels
        GameObject[] panels = GameObject.FindGameObjectsWithTag("Panel");
        foreach (GameObject panel in panels)
        {
            Debug.Log($"Found panel with 'Panel' tag: {panel.name}");
        }
        
        Debug.Log("=================================");
    }
} 