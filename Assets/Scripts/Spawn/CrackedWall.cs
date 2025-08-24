using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Handles the cracked wall puzzle in the boss room.
/// When clicked while player is nearby, displays a UI overlay with a spell grid.
/// The puzzle is solved by inputting the spell pattern "036".
/// </summary>
public class CrackedWall : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [Tooltip("The UI panel that appears when the wall is clicked")]
    public GameObject puzzleUIPanel;
    [Tooltip("The background image for the puzzle UI")]
    public Image backgroundImage;
    [Tooltip("The back button to close the puzzle")]
    public Button backButton;
    [Tooltip("Reference to the SpellGridDragController in the UI")]
    public SpellGridDragController spellGridController;
    
    [Header("Interaction Settings")]
    [Tooltip("Distance the player must be within to interact with the wall")]
    public float interactionDistance = 2f;
    [Tooltip("The correct spell pattern to solve the puzzle")]
    public string correctSpellPattern = "036";
    
    [Header("Player Reference")]
    [Tooltip("Reference to the player GameObject")]
    public GameObject player;
    
    [Header("Visual Effects")]
    [Tooltip("Sprite to change to when puzzle is solved (normal wall)")]
    
    public Sprite solvedWallSprite;
    [Tooltip("Optional particle effect to play when puzzle is solved")]
    public GameObject solveEffect;
    
    [Header("Enemy Spawning")]
    [Tooltip("Enemy prefab to spawn when player is nearby")]
    public GameObject enemyPrefab;
    [Tooltip("Distance at which enemies start spawning")]
    public float spawnDistance = 20f;
    [Tooltip("Time between enemy spawns (in seconds)")]
    public float spawnInterval = 5f;
    [Tooltip("Maximum number of enemies that can be spawned")]
    public int maxEnemies = 10;
    [Tooltip("Duration to show spawn indicator before spawning")]
    public float spawnIndicatorDuration = 2f;
    [Tooltip("Whether to show spawn indicators")]
    public bool showSpawnIndicators = true;
    
    private bool isPuzzleActive = false;
    private bool isPuzzleSolved = false;
    private bool playerInRange = false;
    private bool playerInSpawnRange = false;
    private SpriteRenderer spriteRenderer;
    private Image uiImage;
    private float spawnTimer = 0f;
    private int enemiesSpawned = 0;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private List<Rigidbody2D> frozenProjectiles = new List<Rigidbody2D>();
    private List<Vector2> projectileVelocities = new List<Vector2>();
    private List<MonoBehaviour> frozenEnemyAI = new List<MonoBehaviour>();
    private List<MonoBehaviour> frozenEnemyShooters = new List<MonoBehaviour>();
    private SpawnIndicator spawnIndicator;
    private Vector3 pendingSpawnPosition = Vector3.zero;
    private bool isSpawnIndicatorActive = false;
    
    void Start()
    {
        // Set up UI references first (this will be called by CrackedWall2 first)
        SetupUIReferences();
        
        // Ensure we have the required components for interaction
        SetupInteractionComponents();
        
        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("CrackedWall: No player found with 'Player' tag. Please assign the player reference manually.");
            }
        }
        
        // Set up UI event listeners
        if (backButton != null)
        {
            backButton.onClick.AddListener(ClosePuzzle);
        }
        else
        {
            Debug.LogError("CrackedWall: Back button is not assigned! Please assign it in the inspector.");
        }
        
        // Initially hide the puzzle UI and background
        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("CrackedWall: Puzzle UI panel is not assigned! Please assign it in the inspector.");
        }
        
        // Initially hide the background image
        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("CrackedWall: Background image is not assigned. The puzzle will work without it.");
        }
        
        // Subscribe to spell pattern completion events
        if (spellGridController != null)
        {
            spellGridController.OnPatternCompleted += OnSpellPatternCompleted;
        }
        else
        {
            Debug.LogError("CrackedWall: Spell grid controller is not assigned! Please assign it in the inspector.");
        }
        
        // Cache visual components for sprite changing
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<Image>();
        
        if (spriteRenderer == null && uiImage == null)
        {
            Debug.LogWarning("CrackedWall: No SpriteRenderer or Image component found. Visual changes may not work.");
        }
        
        // Initialize spawn indicator
        if (showSpawnIndicators)
        {
            spawnIndicator = gameObject.AddComponent<SpawnIndicator>();
        }
    }
    
    private void SetupUIReferences()
    {
        // Validate and set up UI references
        ValidateUIReferences();
    }
    
    private void SetupInteractionComponents()
    {
        // Ensure we have a solid Collider2D for blocking (like other walls)
        Collider2D solidCollider = GetComponent<Collider2D>();
        if (solidCollider == null)
        {
            Debug.Log("CrackedWall: Adding solid BoxCollider2D for blocking.");
            gameObject.AddComponent<BoxCollider2D>();
        }
        else
        {
            // Ensure the main collider is NOT a trigger (solid for blocking)
            solidCollider.isTrigger = false;
        }
        
        // Ensure we have a Rigidbody2D for physics interaction (set to kinematic)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.Log("CrackedWall: Adding Rigidbody2D for interaction.");
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.simulated = true;
        }
        
        // Add a separate trigger collider for interaction detection
        // This will be slightly larger than the solid collider
        BoxCollider2D[] allColliders = GetComponents<BoxCollider2D>();
        bool hasTriggerCollider = false;
        
        foreach (BoxCollider2D collider in allColliders)
        {
            if (collider.isTrigger)
            {
                hasTriggerCollider = true;
                break;
            }
        }
        
        if (!hasTriggerCollider)
        {
            Debug.Log("CrackedWall: Adding trigger BoxCollider2D for interaction detection.");
            BoxCollider2D triggerCollider = gameObject.AddComponent<BoxCollider2D>();
            triggerCollider.isTrigger = true;
            
            // Make the trigger collider slightly larger than the solid collider
            BoxCollider2D solidBoxCollider = GetComponent<BoxCollider2D>();
            if (solidBoxCollider != null && !solidBoxCollider.isTrigger)
            {
                triggerCollider.size = solidBoxCollider.size * 1.2f; // 20% larger
                triggerCollider.offset = solidBoxCollider.offset;
            }
        }
        
        // Add components needed for mouse input detection
        SetupMouseInputComponents();
    }
    
    private void SetupMouseInputComponents()
    {
        // Add a GraphicRaycaster if this is a UI element, or ensure proper physics raycasting
        // For world-space objects, we need to ensure the collider can be hit by raycasts
        
        // Check if we're in a Canvas (UI element)
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log("CrackedWall: Detected as UI element, ensuring GraphicRaycaster setup.");
            // This is a UI element, make sure it has proper UI components
            if (GetComponent<Graphic>() == null)
            {
                // Add an Image component if none exists (for UI raycasting)
                Image image = gameObject.AddComponent<Image>();
                image.color = new Color(1, 1, 1, 0); // Transparent
                image.raycastTarget = true;
            }
        }
        else
        {
            Debug.Log("CrackedWall: Setting up for world-space mouse input.");
            // This is a world-space object, ensure it can be hit by physics raycasts
            // The collider should already be set up correctly
        }
    }
    
    private void ValidateUIReferences()
    {
        // Check if we're in the scene (not a prefab)
        if (Application.isPlaying)
        {
            // If references are missing, try to find them in the scene
            if (puzzleUIPanel == null)
            {
                puzzleUIPanel = GameObject.Find("PuzzleUIPanel");
                if (puzzleUIPanel != null)
                {
                    Debug.Log("CrackedWall: Found PuzzleUIPanel in scene automatically.");
                }
            }
            
            if (backButton == null)
            {
                Button foundButton = GameObject.Find("BackButton")?.GetComponent<Button>();
                if (foundButton != null)
                {
                    backButton = foundButton;
                    Debug.Log("CrackedWall: Found BackButton in scene automatically.");
                }
            }
            
            if (spellGridController == null)
            {
                spellGridController = GameObject.Find("SpellGrid")?.GetComponent<SpellGridDragController>();
                if (spellGridController != null)
                {
                    Debug.Log("CrackedWall: Found SpellGridDragController in scene automatically.");
                }
            }
            
            if (backgroundImage == null)
            {
                backgroundImage = GameObject.Find("PuzzleBackground")?.GetComponent<Image>();
                if (backgroundImage != null)
                {
                    Debug.Log("CrackedWall: Found background image in scene automatically.");
                }
            }
        }
    }
    

    
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("CrackedWall: OnPointerClick triggered!");
        TryInteract();
    }
    
    // Alternative interaction method using trigger detection
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("CrackedWall: Player entered trigger area!");
            // Store that player is in range
            playerInRange = true;
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("CrackedWall: Player exited trigger area!");
            playerInRange = false;
        }
    }
    
    // Check if player is in spawn range
    private void CheckSpawnRange()
    {
        if (player == null || isPuzzleSolved) return;
        
        float distance = Vector2.Distance(transform.position, player.transform.position);
        bool wasInSpawnRange = playerInSpawnRange;
        playerInSpawnRange = distance <= spawnDistance;
        
        if (playerInSpawnRange && !wasInSpawnRange)
        {
            Debug.Log($"CrackedWall: Player entered spawn range ({distance:F1} <= {spawnDistance})");
        }
        else if (!playerInSpawnRange && wasInSpawnRange)
        {
            Debug.Log($"CrackedWall: Player exited spawn range ({distance:F1} > {spawnDistance})");
        }
    }
    
    // Method to handle interaction attempts
    protected virtual void TryInteract()
    {
        if (isPuzzleSolved)
        {
            Debug.Log("Puzzle already solved!");
            return;
        }
        
        if (IsPlayerNearby())
        {
            Debug.Log("CrackedWall: Opening puzzle!");
            OpenPuzzle();
        }
        else
        {
            Debug.Log("Player is too far away to interact with the cracked wall.");
        }
    }
    
    // Public method that can be called from other scripts or input systems
    [ContextMenu("Test Interaction")]
    public void Interact()
    {
        Debug.Log("CrackedWall: Manual interaction triggered!");
        TryInteract();
    }
    
    // Method to test interaction from keyboard (for debugging)
    void Update()
    {
        // Check spawn range and handle enemy spawning
        CheckSpawnRange();
        HandleEnemySpawning();
        
        // Press 'E' key to test interaction
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (IsPlayerNearby())
            {
                Debug.Log("CrackedWall: E key pressed - testing interaction!");
                Interact();
            }
        }
        
        // Alternative mouse click detection using raycasting
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            CheckMouseClick();
        }
    }
    
    private void HandleEnemySpawning()
    {
        if (!playerInSpawnRange || isPuzzleSolved || enemyPrefab == null || isPuzzleActive) return;
        
        // Update spawn timer
        spawnTimer += Time.deltaTime;
        
        // Check if it's time to spawn an enemy
        if (spawnTimer >= spawnInterval && enemiesSpawned < maxEnemies)
        {
            // Don't spawn if puzzle is active or spawn indicators are paused
            if (isPuzzleActive || SpawnIndicator.AreSpawnIndicatorsPaused())
            {
                return;
            }
            
            // Find spawn position first
            Vector3 spawnPosition = FindValidSpawnPosition();
            
            if (spawnPosition != Vector3.zero)
            {
                if (showSpawnIndicators && spawnIndicator != null && !isSpawnIndicatorActive)
                {
                    // Start spawn indicator
                    StartSpawnIndicator(spawnPosition);
                }
                else
                {
                    // Spawn immediately if no indicator
                    SpawnEnemyAtPosition(spawnPosition);
                }
            }
            
            spawnTimer = 0f; // Reset timer
        }
    }
    
    private void StartSpawnIndicator(Vector3 spawnPosition)
    {
        if (spawnIndicator == null) return;
        
        // Don't start if puzzle is active or spawn indicators are paused
        if (isPuzzleActive || SpawnIndicator.AreSpawnIndicatorsPaused())
        {
            Debug.Log("CrackedWall: Cannot start spawn indicator - puzzle active or indicators paused");
            return;
        }
        
        pendingSpawnPosition = spawnPosition;
        isSpawnIndicatorActive = true;
        
        // Start the spawn indicator traveling from the cracked wall to the spawn position
        Vector3 wallPosition = transform.position;
        spawnIndicator.StartSpawnIndicator(spawnPosition, wallPosition, enemyPrefab, spawnIndicatorDuration);
        
        // Schedule the actual spawn after the indicator duration
        StartCoroutine(SpawnAfterIndicator());
        
        Debug.Log($"CrackedWall: Started spawn indicator traveling from wall to position {spawnPosition}");
    }
    
    private System.Collections.IEnumerator SpawnAfterIndicator()
    {
        float elapsedTime = 0f;
        
        // Wait for the full indicator duration (travel + pause + fade)
        while (elapsedTime < spawnIndicatorDuration)
        {
            // Check if spawn indicators are paused
            if (SpawnIndicator.AreSpawnIndicatorsPaused())
            {
                // Don't increment elapsed time while paused
                yield return null;
                continue;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Only spawn if the indicator is still active and we're not paused
        if (isSpawnIndicatorActive && pendingSpawnPosition != Vector3.zero && !SpawnIndicator.AreSpawnIndicatorsPaused())
        {
            SpawnEnemyAtPosition(pendingSpawnPosition);
            isSpawnIndicatorActive = false;
            pendingSpawnPosition = Vector3.zero;
        }
        else
        {
            // Clean up if we shouldn't spawn
            isSpawnIndicatorActive = false;
            pendingSpawnPosition = Vector3.zero;
            Debug.Log("CrackedWall: Spawn cancelled due to pause or indicator not active");
        }
    }
    
    protected virtual void SpawnEnemyAtPosition(Vector3 spawnPosition)
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("CrackedWall: No enemy prefab assigned!");
            return;
        }
        
        // Spawn the enemy
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        spawnedEnemies.Add(enemy);
        enemiesSpawned++;
        
        Debug.Log($"CrackedWall: Spawned enemy {enemiesSpawned}/{maxEnemies} at position {spawnPosition}");
        
        // Optional: Set the enemy's target to the player
        // You might need to adjust this based on your enemy AI system
        // enemy.GetComponent<EnemyAI>()?.SetTarget(player);
    }
    
    /// <summary>
    /// Cancels any pending enemy spawns when entering puzzle mode
    /// </summary>
    private void CancelPendingSpawns()
    {
        if (isSpawnIndicatorActive)
        {
            isSpawnIndicatorActive = false;
            pendingSpawnPosition = Vector3.zero;
            
            // Stop the spawn indicator if it's active
            if (spawnIndicator != null)
            {
                spawnIndicator.StopSpawnIndicator();
            }
            
            Debug.Log("CrackedWall: Cancelled pending enemy spawn due to puzzle pause");
        }
    }
    
    private void SpawnEnemy()
    {
        // Find a valid spawn position on floor tiles
        Vector3 spawnPosition = FindValidSpawnPosition();
        
        if (spawnPosition != Vector3.zero)
        {
            SpawnEnemyAtPosition(spawnPosition);
        }
        else
        {
            Debug.LogWarning("CrackedWall: Could not find valid spawn position for enemy!");
        }
    }
    
    protected virtual Vector3 FindValidSpawnPosition()
    {
        // Get reference to DungeonGenerator to access the dungeon map
        DungeonGenerator dungeonGenerator = FindObjectOfType<DungeonGenerator>();
        if (dungeonGenerator == null)
        {
            Debug.LogError("CrackedWall: No DungeonGenerator found in scene!");
            return Vector3.zero;
        }
        
        // Try multiple spawn attempts
        for (int attempt = 0; attempt < 20; attempt++)
        {
            // Calculate a random position around the wall
            Vector3 randomDirection = Random.insideUnitCircle.normalized;
            float distance = Random.Range(2f, 8f); // Between 2 and 8 units from wall
            Vector3 testPosition = transform.position + randomDirection * distance;
            
            // Convert world position to grid coordinates
            int gridX = Mathf.RoundToInt(testPosition.x);
            int gridY = Mathf.RoundToInt(testPosition.y);
            
            // Check if the position is within dungeon bounds
            if (gridX >= 0 && gridX < dungeonGenerator.dungeonWidth && 
                gridY >= 0 && gridY < dungeonGenerator.dungeonHeight)
            {
                // Get the tile type at this position
                TileType tileType = dungeonGenerator.GetTileType(gridX, gridY);
                
                // Check if it's a valid floor tile
                if (tileType == TileType.Floor || 
                    tileType == TileType.BossFloor || 
                    tileType == TileType.NonEssentialFloor)
                {
                    Debug.Log($"CrackedWall: Found valid spawn position at ({gridX}, {gridY}) - TileType: {tileType}");
                    return new Vector3(gridX, gridY, 0);
                }
            }
        }
        
        Debug.LogWarning("CrackedWall: Failed to find valid spawn position after 20 attempts!");
        return Vector3.zero;
    }
    
    private void CheckMouseClick()
    {
        // Get mouse position in world coordinates
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = transform.position.z; // Set to same Z as the wall
        
        // Check if mouse is over this object
        Collider2D hitCollider = Physics2D.OverlapPoint(mousePos);
        
        if (hitCollider != null && hitCollider.gameObject == gameObject)
        {
            Debug.Log("CrackedWall: Mouse click detected via raycast!");
            TryInteract();
        }
    }
    
    private bool IsPlayerNearby()
    {
        // First check if player is in trigger range
        if (playerInRange)
        {
            return true;
        }
        
        // Fallback to distance check
        if (player == null) return false;
        
        float distance = Vector2.Distance(transform.position, player.transform.position);
        bool inDistance = distance <= interactionDistance;
        
        if (inDistance)
        {
            Debug.Log($"CrackedWall: Player is within distance ({distance:F2} <= {interactionDistance})");
        }
        else
        {
            Debug.Log($"CrackedWall: Player is too far ({distance:F2} > {interactionDistance})");
        }
        
        return inDistance;
    }
    
    protected virtual void OpenPuzzle()
    {
        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(true);
            isPuzzleActive = true;
            
            // Show the background image
            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(true);
            }
            
            // Disable player movement and other game interactions
            DisableGameInteractions();
            
            Debug.Log("Cracked wall puzzle opened. Input the correct spell pattern.");
        }
    }
    
    private void ClosePuzzle()
    {
        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(false);
            isPuzzleActive = false;
            
            // Hide the background image
            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(false);
            }
            
            // Re-enable player movement and other game interactions
            EnableGameInteractions();
            
            Debug.Log("Puzzle closed without solving.");
        }
    }
    
    private void CheckForSpellPattern()
    {
        // This method is no longer needed since we're using events
    }
    
    public void OnSpellPatternCompleted(string pattern)
    {
        // Check if pattern is correct
        bool isCorrect = (pattern == correctSpellPattern);
        
        if (isCorrect)
        {
            SolvePuzzle();
        }
        else
        {
            Debug.Log($"Incorrect pattern: {pattern}. Try again.");
        }
    }
    
    protected virtual void SolvePuzzle()
    {
        isPuzzleSolved = true;
        isPuzzleActive = false;
        
        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(false);
        }
        
        // Hide the background image
        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(false);
        }
        
        // Re-enable game interactions
        EnableGameInteractions();
        
        // Visual feedback for solving the puzzle
        Debug.Log("Puzzle solved! The cracked wall reveals its secrets...");
        
        // Stop enemy spawning and clean up spawned enemies
        StopEnemySpawning();
        
        // Change the wall's appearance to a normal wall
        ChangeToNormalWall();
        
        // Play solve effect if assigned
        if (solveEffect != null)
        {
            Instantiate(solveEffect, transform.position, Quaternion.identity);
        }
        
        // Notify HintManager to destroy hint1miniimage when puzzle1 is solved
        if (HintManager.Instance != null)
        {
            HintManager.Instance.DestroyHintMiniImage("hint1");
            Debug.Log("CrackedWall: Notified HintManager to destroy hint1miniimage");
        }
    }
    
    private void StopEnemySpawning()
    {
        playerInSpawnRange = false;
        spawnTimer = 0f;
        
        // Stop any active spawn indicators
        if (spawnIndicator != null && isSpawnIndicatorActive)
        {
            spawnIndicator.StopSpawnIndicator();
            isSpawnIndicatorActive = false;
            pendingSpawnPosition = Vector3.zero;
        }
        
        Debug.Log($"CrackedWall: Stopped enemy spawning. Total enemies spawned: {enemiesSpawned}");
        
        // Optional: Destroy all spawned enemies when puzzle is solved
        // Uncomment the following lines if you want enemies to disappear when solved
        /*
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        spawnedEnemies.Clear();
        */
    }
    
    protected virtual void ChangeToNormalWall()
    {
        if (solvedWallSprite == null)
        {
            Debug.LogWarning("CrackedWall: No solved wall sprite assigned. Wall appearance will not change.");
            return;
        }
        
        // Change sprite based on component type
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = solvedWallSprite;
            Debug.Log("CrackedWall: Changed to normal wall sprite (SpriteRenderer).");
        }
        else if (uiImage != null)
        {
            uiImage.sprite = solvedWallSprite;
            Debug.Log("CrackedWall: Changed to normal wall sprite (Image).");
        }
        else
        {
            Debug.LogError("CrackedWall: No visual component found to change sprite!");
        }
        
        // Optional: Change the object's name to reflect its new state
        gameObject.name = gameObject.name.Replace("Cracked", "Normal");
    }
    
    private void DisableGameInteractions()
    {
        // Disable player movement
        PlayerController playerController = player?.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Freeze all spell projectiles
        FreezeSpellProjectiles();
        
        // Freeze enemy AI and shooter behavior
        FreezeEnemyBehavior();
        
        // Pause all spawn indicators and cancel pending spawns
        SpawnIndicator.PauseAllSpawnIndicators();
        CancelPendingSpawns();
        
        // Disable other game systems that should be paused
        // For example, camera movement, etc.
        
        // Set time scale to pause the game (optional)
        // Time.timeScale = 0f;
    }
    
    private void FreezeSpellProjectiles()
    {
        // Clear previous lists
        frozenProjectiles.Clear();
        projectileVelocities.Clear();
        
        // Find all spell projectiles in the scene
        // You may need to adjust the tag or component name based on your spell system
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // Check if this is a spell projectile (adjust the condition based on your spell system)
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
                    
                    Debug.Log($"CrackedWall: Frozen spell projectile: {obj.name}");
                }
            }
        }
        
        Debug.Log($"CrackedWall: Frozen {frozenProjectiles.Count} spell projectiles");
    }
    
    private bool IsSpellProjectile(GameObject obj)
    {
        // Check for common spell projectile identifiers
        // Adjust these conditions based on your spell system
        
        // Check by tag
        if (obj.CompareTag("SpellProjectile"))
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
        
        Debug.Log($"CrackedWall: Frozen {frozenEnemyAI.Count} enemy AI components and {frozenEnemyShooters.Count} enemy shooter components");
    }
    
    private bool IsEnemy(GameObject obj)
    {
        // Check for common enemy identifiers
        // Adjust these conditions based on your enemy system
        
        // Check by tag
        if (obj.CompareTag("Enemy"))
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
                Debug.Log($"CrackedWall: Frozen enemy AI component {componentName} on {enemy.name}");
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
                Debug.Log($"CrackedWall: Frozen enemy shooter component {componentName} on {enemy.name}");
            }
        }
    }
    
    private void EnableGameInteractions()
    {
        // Re-enable player movement
        PlayerController playerController = player?.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        // Unfreeze all spell projectiles
        UnfreezeSpellProjectiles();
        
        // Unfreeze enemy AI and shooter behavior
        UnfreezeEnemyBehavior();
        
        // Resume all spawn indicators
        SpawnIndicator.ResumeAllSpawnIndicators();
        
        // Re-enable other game systems
        
        // Restore time scale
        // Time.timeScale = 1f;
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
                
                Debug.Log($"CrackedWall: Unfrozen spell projectile: {frozenProjectiles[i].gameObject.name}");
            }
        }
        
        Debug.Log($"CrackedWall: Unfrozen {frozenProjectiles.Count} spell projectiles");
        
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
                Debug.Log($"CrackedWall: Unfrozen enemy AI component {aiComponent.GetType().Name} on {aiComponent.gameObject.name}");
            }
        }
        
        // Unfreeze enemy shooter components
        foreach (MonoBehaviour shooterComponent in frozenEnemyShooters)
        {
            if (shooterComponent != null)
            {
                shooterComponent.enabled = true;
                Debug.Log($"CrackedWall: Unfrozen enemy shooter component {shooterComponent.GetType().Name} on {shooterComponent.gameObject.name}");
            }
        }
        
        Debug.Log($"CrackedWall: Unfrozen {frozenEnemyAI.Count} enemy AI components and {frozenEnemyShooters.Count} enemy shooter components");
        
        // Clear the lists
        frozenEnemyAI.Clear();
        frozenEnemyShooters.Clear();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (spellGridController != null)
        {
            spellGridController.OnPatternCompleted -= OnSpellPatternCompleted;
        }
        
        // Stop any active spawn indicators
        if (spawnIndicator != null && isSpawnIndicatorActive)
        {
            spawnIndicator.StopSpawnIndicator();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw interaction range in the scene view
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        // Draw spawn range in the scene view
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnDistance);
    }
    
    // Helper method to check if all required references are assigned
    [ContextMenu("Validate References")]
    public void ValidateReferences()
    {
        Debug.Log("=== CrackedWall Reference Validation ===");
        Debug.Log($"Puzzle UI Panel: {(puzzleUIPanel != null ? "✓ Assigned" : "✗ Missing")}");
        Debug.Log($"Background Image: {(backgroundImage != null ? "✓ Assigned" : "✗ Missing (optional)")}");
        Debug.Log($"Back Button: {(backButton != null ? "✓ Assigned" : "✗ Missing")}");
        Debug.Log($"Spell Grid Controller: {(spellGridController != null ? "✓ Assigned" : "✗ Missing")}");
        Debug.Log($"Player: {(player != null ? "✓ Assigned" : "✗ Missing (will auto-find)")}");
        Debug.Log($"Interaction Distance: {interactionDistance}");
        Debug.Log($"Correct Pattern: {correctSpellPattern}");
        Debug.Log($"Solved Wall Sprite: {(solvedWallSprite != null ? "✓ Assigned" : "✗ Missing (optional)")}");
        Debug.Log($"Solve Effect: {(solveEffect != null ? "✓ Assigned" : "✗ Missing (optional)")}");
        Debug.Log($"Visual Component: {(spriteRenderer != null ? "SpriteRenderer" : uiImage != null ? "Image" : "✗ None found")}");
        Debug.Log($"Enemy Prefab: {(enemyPrefab != null ? "✓ Assigned" : "✗ Missing (optional)")}");
        Debug.Log($"Spawn Distance: {spawnDistance}");
        Debug.Log($"Spawn Interval: {spawnInterval}s");
        Debug.Log($"Max Enemies: {maxEnemies}");
        Debug.Log($"Enemies Spawned: {enemiesSpawned}");
        Debug.Log($"Player in Spawn Range: {playerInSpawnRange}");
        Debug.Log($"Frozen Projectiles: {frozenProjectiles.Count}");
        Debug.Log($"Frozen Enemy AI: {frozenEnemyAI.Count}");
        Debug.Log($"Frozen Enemy Shooters: {frozenEnemyShooters.Count}");
        Debug.Log("========================================");
    }
    
    // Helper method to find UI elements in the scene
    [ContextMenu("Find UI Elements")]
    public void FindUIElements()
    {
        Debug.Log("=== Searching for UI Elements ===");
        
        // Look for puzzle UI panel
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("puzzle") || obj.name.ToLower().Contains("ui"))
            {
                Debug.Log($"Found potential UI element: {obj.name}");
            }
        }
        
        // Look for buttons
        Button[] buttons = FindObjectsOfType<Button>();
        foreach (Button button in buttons)
        {
            if (button.name.ToLower().Contains("back"))
            {
                Debug.Log($"Found potential back button: {button.name}");
            }
        }
        
        // Look for spell grid controllers
        SpellGridDragController[] controllers = FindObjectsOfType<SpellGridDragController>();
        foreach (SpellGridDragController controller in controllers)
        {
            Debug.Log($"Found SpellGridDragController: {controller.name}");
        }
        
        Debug.Log("=================================");
    }
    
    // Debug method to find spell projectiles in the scene
    [ContextMenu("Find Spell Projectiles")]
    public void FindSpellProjectiles()
    {
        Debug.Log("=== Searching for Spell Projectiles ===");
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int projectileCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (IsSpellProjectile(obj))
            {
                Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
                Debug.Log($"Found potential spell projectile: {obj.name} (Tag: {obj.tag}, Has Rigidbody2D: {rb != null})");
                projectileCount++;
            }
        }
        
        Debug.Log($"Total potential spell projectiles found: {projectileCount}");
        Debug.Log("=========================================");
    }
    
    // Debug method to find enemies in the scene
    [ContextMenu("Find Enemies")]
    public void FindEnemies()
    {
        Debug.Log("=== Searching for Enemies ===");
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int enemyCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (IsEnemy(obj))
            {
                Debug.Log($"Found potential enemy: {obj.name} (Tag: {obj.tag})");
                
                // List all MonoBehaviour components on this enemy
                MonoBehaviour[] components = obj.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour component in components)
                {
                    Debug.Log($"  - Component: {component.GetType().Name} (Enabled: {component.enabled})");
                }
                
                enemyCount++;
            }
        }
        
        Debug.Log($"Total potential enemies found: {enemyCount}");
        Debug.Log("=============================");
    }
    
    /// <summary>
    /// Returns whether this puzzle has been solved
    /// </summary>
    /// <returns>True if the puzzle is solved, false otherwise</returns>
    public bool IsPuzzleSolved()
    {
        return isPuzzleSolved;
    }

    // Debug method to check interaction status
    [ContextMenu("Debug Interaction Status")]
    public void DebugInteractionStatus()
    {
        Debug.Log("=== CrackedWall Interaction Debug ===");
        Debug.Log($"Player in range: {playerInRange}");
        Debug.Log($"Player assigned: {player != null}");
        Debug.Log($"Puzzle solved: {isPuzzleSolved}");
        Debug.Log($"Puzzle active: {isPuzzleActive}");
        
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            Debug.Log($"Distance to player: {distance:F2}");
            Debug.Log($"Interaction distance: {interactionDistance}");
            Debug.Log($"Player position: {player.transform.position}");
            Debug.Log($"Wall position: {transform.position}");
        }
        
        // Check colliders
        Collider2D[] allColliders = GetComponents<Collider2D>();
        Debug.Log($"Number of Collider2D components: {allColliders.Length}");
        
        for (int i = 0; i < allColliders.Length; i++)
        {
            Collider2D collider = allColliders[i];
            Debug.Log($"Collider {i}: {collider.GetType().Name}, isTrigger: {collider.isTrigger}, enabled: {collider.enabled}");
            
            if (collider is BoxCollider2D boxCollider)
            {
                Debug.Log($"  Size: {boxCollider.size}, Offset: {boxCollider.offset}");
            }
        }
        
        // Check Rigidbody2D
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        Debug.Log($"Has Rigidbody2D: {rb != null}");
        if (rb != null)
        {
            Debug.Log($"Rigidbody is kinematic: {rb.isKinematic}");
            Debug.Log($"Rigidbody simulated: {rb.simulated}");
        }
        
        // Check mouse input setup
        Debug.Log("=== Mouse Input Setup ===");
        Canvas canvas = GetComponentInParent<Canvas>();
        Debug.Log($"Is UI element (has Canvas parent): {canvas != null}");
        
        if (canvas != null)
        {
            Graphic graphic = GetComponent<Graphic>();
            Debug.Log($"Has Graphic component: {graphic != null}");
            if (graphic != null)
            {
                Debug.Log($"Graphic raycastTarget: {graphic.raycastTarget}");
            }
        }
        else
        {
            // Check if collider can be hit by physics raycasts
            Vector3 testPos = transform.position;
            Collider2D hitCollider = Physics2D.OverlapPoint(testPos);
            Debug.Log($"Can be hit by physics raycast at own position: {hitCollider != null && hitCollider.gameObject == gameObject}");
        }
        
        Debug.Log("=====================================");
    }
} 