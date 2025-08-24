using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Handles the third cracked wall in the dungeon.
/// Spawns enemies when player is nearby.
/// Can be sealed by dropping a CarryableObject within 1 block distance.
/// Changes to normal wall sprite when sealed.
/// </summary>
public class CrackedWall3 : MonoBehaviour
{
    [Header("Sealing Settings")]
    [Tooltip("Distance within which a CarryableObject can seal the wall")]
    public float sealingDistance = 1f;
    [Tooltip("Whether the wall has been sealed")]
    public bool isSealed = false;
    
    [Header("Player Reference")]
    [Tooltip("Reference to the player GameObject")]
    public GameObject player;
    
    [Header("Visual Effects")]
    [Tooltip("Sprite to change to when wall is sealed (normal wall)")]
    public Sprite sealedWallSprite;
    [Tooltip("Optional particle effect to play when wall is sealed")]
    public GameObject sealEffect;
    
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
    
    private bool playerInSpawnRange = false;
    private SpriteRenderer spriteRenderer;
    private float spawnTimer = 0f;
    private int enemiesSpawned = 0;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private SpawnIndicator spawnIndicator;
    private Vector3 pendingSpawnPosition = Vector3.zero;
    private bool isSpawnIndicatorActive = false;
    
    void Start()
    {
        // Ensure we have the required components
        SetupComponents();
        
        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("CrackedWall3: No player found with 'Player' tag. Please assign the player reference manually.");
            }
        }
        
        // Get visual components
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer == null)
        {
            Debug.LogWarning("CrackedWall3: No SpriteRenderer component found. Visual changes may not work.");
        }
        
        // Create spawn indicator if needed
        if (showSpawnIndicators)
        {
            CreateSpawnIndicator();
        }
    }
    
    void Update()
    {
        if (player == null) return;
        
        // Check if wall is already sealed
        if (isSealed) return;
        
        // Check player distance for spawning
        float distance = Vector2.Distance(transform.position, player.transform.position);
        
        // Update spawn range
        bool wasInSpawnRange = playerInSpawnRange;
        playerInSpawnRange = distance <= spawnDistance;
        
        // Handle spawn range changes
        if (playerInSpawnRange != wasInSpawnRange)
        {
            if (playerInSpawnRange)
            {
                Debug.Log($"CrackedWall3: Player entered spawn range ({distance:F1} <= {spawnDistance})");
                StartEnemySpawning();
            }
            else
            {
                Debug.Log($"CrackedWall3: Player exited spawn range ({distance:F1} > {spawnDistance})");
                StopEnemySpawning();
            }
        }
        
        // Debug: Log spawn status every few seconds
        if (playerInSpawnRange && Time.frameCount % 300 == 0) // Every ~5 seconds at 60fps
        {
            Debug.Log($"CrackedWall3: Spawn status - Timer: {spawnTimer:F1}/{spawnInterval}, Enemies: {enemiesSpawned}/{maxEnemies}, Sealed: {isSealed}");
        }
        
        // Handle enemy spawning
        if (playerInSpawnRange && enemiesSpawned < maxEnemies)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                Debug.Log($"CrackedWall3: Attempting to spawn enemy {enemiesSpawned + 1}/{maxEnemies}");
                TrySpawnEnemy();
            }
        }
        
        // Handle spawn indicator (managed by coroutine, no manual timer needed)
        
        // Check for nearby CarryableObjects that could seal the wall
        CheckForCarryableObjects();
    }
    
    void SetupComponents()
    {
        // Add solid collider for blocking if not present
        Collider2D solidCollider = GetComponent<Collider2D>();
        if (solidCollider == null)
        {
            BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
            boxCollider.isTrigger = false; // Solid collider for blocking
            Debug.Log("CrackedWall3: Adding solid BoxCollider2D for blocking.");
        }
        
        // Add rigidbody if not present
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static; // Static for walls
            Debug.Log("CrackedWall3: Adding Rigidbody2D for interaction.");
        }
    }
    
    void CheckForCarryableObjects()
    {
        // Find all CarryableObjects in the scene
        CarryableObject[] carryableObjects = FindObjectsOfType<CarryableObject>();
        
        foreach (CarryableObject carryableObject in carryableObjects)
        {
            // Check if the object is not being carried and is within sealing distance
            if (!carryableObject.IsCarried())
            {
                float distance = Vector2.Distance(transform.position, carryableObject.transform.position);
                if (distance <= sealingDistance)
                {
                    SealWall(carryableObject);
                    break; // Only seal once
                }
            }
        }
    }
    
    void SealWall(CarryableObject sealingObject)
    {
        if (isSealed) return;
        
        Debug.Log("CrackedWall3: Wall sealed by CarryableObject!");
        
        isSealed = true;
        
        // Stop enemy spawning
        StopEnemySpawning();
        
        // Cancel any pending spawns
        CancelPendingSpawns();
        
        // Change visual appearance
        ChangeToSealedAppearance();
        
        // Play seal effect
        if (sealEffect != null)
        {
            Instantiate(sealEffect, transform.position, Quaternion.identity);
        }
        
        // Destroy the sealing object (optional - you can remove this if you want to keep it)
        Destroy(sealingObject.gameObject);
    }
    
    void ChangeToSealedAppearance()
    {
        if (sealedWallSprite == null)
        {
            Debug.LogWarning("CrackedWall3: No sealed wall sprite assigned. Wall appearance will not change.");
            return;
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sealedWallSprite;
            Debug.Log("CrackedWall3: Changed to sealed wall sprite.");
        }
        else
        {
            Debug.LogError("CrackedWall3: No SpriteRenderer component found to change sprite!");
        }
    }
    
    void StartEnemySpawning()
    {
        // Reset spawn timer when entering range
        spawnTimer = 0f;
    }
    
    void StopEnemySpawning()
    {
        // Stop spawning when leaving range
        spawnTimer = 0f;
    }
    
    void TrySpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("CrackedWall3: No enemy prefab assigned!");
            return;
        }
        
        if (enemiesSpawned >= maxEnemies)
        {
            Debug.Log("CrackedWall3: Maximum enemies reached, cannot spawn more.");
            return;
        }
        
        // Find a valid spawn position
        Vector3 spawnPosition = FindValidSpawnPosition();
        if (spawnPosition != Vector3.zero)
        {
            Debug.Log($"CrackedWall3: Found valid spawn position at {spawnPosition}");
            if (showSpawnIndicators && spawnIndicator != null)
            {
                StartSpawnIndicator(spawnPosition);
            }
            else
            {
                SpawnEnemy(spawnPosition);
            }
        }
        else
        {
            Debug.LogWarning("CrackedWall3: Could not find valid spawn position!");
        }
    }
    
    void StartSpawnIndicator(Vector3 spawnPosition)
    {
        if (spawnIndicator == null) return;
        
        // Don't start if wall is sealed or spawn indicators are paused
        if (isSealed || SpawnIndicator.AreSpawnIndicatorsPaused())
        {
            Debug.Log("CrackedWall3: Cannot start spawn indicator - wall sealed or indicators paused");
            return;
        }
        
        pendingSpawnPosition = spawnPosition;
        isSpawnIndicatorActive = true;
        
        // Start the spawn indicator traveling from the cracked wall to the spawn position
        Vector3 wallPosition = transform.position;
        spawnIndicator.StartSpawnIndicator(spawnPosition, wallPosition, enemyPrefab, spawnIndicatorDuration);
        
        // Schedule the actual spawn after the indicator duration
        StartCoroutine(SpawnAfterIndicator());
        
        Debug.Log($"CrackedWall3: Started spawn indicator traveling from wall to position {spawnPosition}");
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
            SpawnEnemy(pendingSpawnPosition);
            isSpawnIndicatorActive = false;
            pendingSpawnPosition = Vector3.zero;
        }
        else
        {
            // Clean up if we shouldn't spawn
            isSpawnIndicatorActive = false;
            pendingSpawnPosition = Vector3.zero;
            Debug.Log("CrackedWall3: Spawn cancelled due to pause or indicator not active");
        }
    }
    
    void SpawnEnemy(Vector3 spawnPosition)
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("CrackedWall3: No enemy prefab assigned!");
            return;
        }
        
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        spawnedEnemies.Add(enemy);
        enemiesSpawned++;
        
        Debug.Log($"CrackedWall3: Spawned enemy {enemiesSpawned}/{maxEnemies} at position {spawnPosition}");
    }
    
    Vector3 FindValidSpawnPosition()
    {
        DungeonGenerator dungeonGenerator = FindObjectOfType<DungeonGenerator>();
        if (dungeonGenerator == null)
        {
            Debug.LogError("CrackedWall3: No DungeonGenerator found in scene!");
            return Vector3.zero;
        }
        
        // Try to find a valid spawn position near the wall
        for (int attempt = 0; attempt < 20; attempt++)
        {
            // Generate a random position within spawn distance
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float randomDistance = Random.Range(5f, spawnDistance);
            Vector3 spawnPosition = transform.position + new Vector3(randomDirection.x, randomDirection.y, 0) * randomDistance;
            
            // Convert to grid coordinates
            int gridX = Mathf.RoundToInt(spawnPosition.x);
            int gridY = Mathf.RoundToInt(spawnPosition.y);
            
            // Check if position is valid
            if (gridX >= 0 && gridX < dungeonGenerator.dungeonWidth &&
                gridY >= 0 && gridY < dungeonGenerator.dungeonHeight)
            {
                TileType tileType = dungeonGenerator.GetTileType(gridX, gridY);
                if (tileType == TileType.Floor || tileType == TileType.BossFloor || tileType == TileType.NonEssentialFloor)
                {
                    Debug.Log($"CrackedWall3: Found valid spawn position at ({gridX}, {gridY}) - TileType: {tileType}");
                    return new Vector3(gridX, gridY, 0);
                }
            }
        }
        
        Debug.LogWarning("CrackedWall3: Failed to find valid spawn position after 20 attempts!");
        return Vector3.zero;
    }
    
    void CreateSpawnIndicator()
    {
        // Add SpawnIndicator component directly to this GameObject
        spawnIndicator = gameObject.AddComponent<SpawnIndicator>();
    }
    
    /// <summary>
    /// Cancels any pending enemy spawns when wall is sealed
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
            
            Debug.Log("CrackedWall3: Cancelled pending enemy spawn due to wall being sealed");
        }
    }
    
    void OnDestroy()
    {
        // Clean up any remaining spawned enemies
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
    }
    
    // Public method to check if wall is sealed
    public bool IsSealed()
    {
        return isSealed;
    }

    /// <summary>
    /// Returns whether this puzzle has been solved (wall is sealed)
    /// </summary>
    /// <returns>True if the puzzle is solved (wall is sealed), false otherwise</returns>
    public bool IsPuzzleSolved()
    {
        return isSealed;
    }
    
    // Public method to get current enemy spawn count
    public int GetEnemySpawnCount()
    {
        return enemiesSpawned;
    }
    
    // Public method to get maximum enemy spawn count
    public int GetMaxEnemySpawnCount()
    {
        return maxEnemies;
    }
    
    // Debug method to validate all references
    public void ValidateReferences()
    {
        Debug.Log("=== CrackedWall3 Reference Validation ===");
        Debug.Log($"Player: {(player != null ? "Assigned" : "NOT ASSIGNED")}");
        Debug.Log($"Enemy Prefab: {(enemyPrefab != null ? "Assigned" : "NOT ASSIGNED")}");
        Debug.Log($"Sealed Wall Sprite: {(sealedWallSprite != null ? "Assigned" : "NOT ASSIGNED")}");
        Debug.Log($"Seal Effect: {(sealEffect != null ? "Assigned" : "NOT ASSIGNED")}");
        Debug.Log($"Spawn Indicator: {(spawnIndicator != null ? "Created" : "NOT CREATED")}");
        Debug.Log($"Sealing Distance: {sealingDistance}");
        Debug.Log($"Spawn Distance: {spawnDistance}");
        Debug.Log($"Spawn Interval: {spawnInterval}");
        Debug.Log($"Max Enemies: {maxEnemies}");
        Debug.Log($"Show Spawn Indicators: {showSpawnIndicators}");
        Debug.Log($"Is Sealed: {isSealed}");
        Debug.Log($"Player In Spawn Range: {playerInSpawnRange}");
        Debug.Log($"Enemies Spawned: {enemiesSpawned}");
        Debug.Log("=== End Validation ===");
    }
    
    // Debug method to test interaction
    public void TestInteraction()
    {
        Debug.Log("=== CrackedWall3 Interaction Debug ===");
        Debug.Log($"Player Distance: {Vector2.Distance(transform.position, player.transform.position):F2}");
        Debug.Log($"Sealing Distance: {sealingDistance}");
        Debug.Log($"Spawn Distance: {spawnDistance}");
        Debug.Log($"Player In Spawn Range: {playerInSpawnRange}");
        Debug.Log($"Is Sealed: {isSealed}");
        Debug.Log($"Enemies Spawned: {enemiesSpawned}/{maxEnemies}");
        Debug.Log($"Spawn Timer: {spawnTimer:F2}/{spawnInterval}");
        
        // Check for nearby CarryableObjects
        CarryableObject[] carryableObjects = FindObjectsOfType<CarryableObject>();
        Debug.Log($"CarryableObjects in scene: {carryableObjects.Length}");
        foreach (CarryableObject obj in carryableObjects)
        {
            float distance = Vector2.Distance(transform.position, obj.transform.position);
            Debug.Log($"  - {obj.name}: Distance {distance:F2}, Carried: {obj.IsCarried()}");
        }
        Debug.Log("=== End Debug ===");
    }
    
    // Debug method to manually test spawning
    public void TestSpawnEnemy()
    {
        Debug.Log("CrackedWall3: Manually testing enemy spawn...");
        if (enemyPrefab == null)
        {
            Debug.LogError("CrackedWall3: Enemy prefab is null! Cannot spawn.");
            return;
        }
        
        Vector3 testPosition = transform.position + Vector3.right * 2f; // Spawn 2 units to the right
        SpawnEnemy(testPosition);
    }
}
