using UnityEngine;

/// <summary>
/// Hole tile that causes the player to fall, respawn at last floor position, and lose health
/// </summary>
public class Hole : MonoBehaviour
{
    [Header("Hole Settings")]
    public int healthLoss = 1; // Amount of health to lose when falling
    public float respawnDelay = 0.5f; // Delay before respawning the player
    public float boundsTolerance = 0.3f; // Tolerance for bounds checking (increased for better detection)
    public float holeDetectionRadius = 0.8f; // Distance from hole center to detect player
    
    [Header("References")]
    public GameObject player; // Reference to the player GameObject
    
    private Vector3 lastFloorPosition; // Last floor position the player was on
    private bool isPlayerOnFloor = false; // Track if player is currently on a floor tile
    private bool isRespawning = false; // Prevent multiple respawns
    private bool isFalling = false; // Track if player is currently falling
    private static bool globalFalling = false; // Static flag to prevent multiple holes from triggering
    
    private DungeonGenerator dungeonGenerator; // Reference to dungeon generator for tile checking
    private PlayerController playerController; // Reference to player controller for input management
    private Rigidbody2D playerRigidbody; // Reference to player's rigidbody for momentum control
    private PlayerAttack playerAttack; // Reference to player attack for dash detection
    
    // Static method to reset the global falling state when scene is reloaded
    public static void ResetGlobalFallingState()
    {
        globalFalling = false;
    }
    
    void Start()
    {
        // Reset global falling state when scene starts
        ResetGlobalFallingState();
        
        // Find the player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        
        // Find the dungeon generator
        dungeonGenerator = FindObjectOfType<DungeonGenerator>();
        
        // Find the player controller and rigidbody
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            playerRigidbody = player.GetComponent<Rigidbody2D>();
            playerAttack = player.GetComponent<PlayerAttack>();
        }
        

    }
    
    void Update()
    {
        if (player == null || dungeonGenerator == null) return;
        
        // Check if player is on a floor tile and update last floor position
        UpdateLastFloorPosition();
    }
    
    void OnTriggerStay2D(Collider2D other)
    {
        // Check if the player is staying in the hole
        if (other.gameObject == player && !isRespawning && !isFalling && !globalFalling)
        {
            // Check if the player is mostly within the hole
            if (IsPlayerFullyInHole(other))
            {
                FallInHole();
            }
        }
    }
    
    // Fallback trigger method in case OnTriggerStay2D isn't working
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the player entered the hole
        if (other.gameObject == player && !isRespawning && !isFalling && !globalFalling)
        {
            // Check if the player is mostly within the hole
            if (IsPlayerFullyInHole(other))
            {
                FallInHole();
            }
            else
            {
                // Fallback: if bounds check fails, use a simpler center-point check
                if (IsPlayerCenterInHole(other))
                {
                    FallInHole();
                }
            }
        }
    }
    
    /// <summary>
    /// Handle the player falling in the hole
    /// </summary>
    void FallInHole()
    {
        if (isRespawning || isFalling || globalFalling) return;
        
        // Check if player is currently dashing - if so, they're invulnerable to fall damage
        if (playerAttack != null && playerAttack.IsAttacking())
        {
            return;
        }
        
        isFalling = true;
        globalFalling = true; // Set global flag to prevent other holes from triggering
        isRespawning = true;
        
        // Stop all player input and momentum
        StopPlayerInputAndMomentum();
        
        // Reduce player health
        ReducePlayerHealth();
        
        // Respawn the player after a delay
        Invoke(nameof(RespawnPlayer), respawnDelay);
    }
    
    /// <summary>
    /// Reduce the player's health
    /// </summary>
    void ReducePlayerHealth()
    {
        // Try to find PlayerStats component
        PlayerStats playerStats = player.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.TakeFallDamage(healthLoss);
        }
    }
    
    /// <summary>
    /// Respawn the player at their last floor position
    /// </summary>
    void RespawnPlayer()
    {
        if (player == null) return;
        
        // Use last floor position if available, otherwise use a fallback position
        Vector3 respawnPosition = lastFloorPosition;
        
        if (respawnPosition == Vector3.zero)
        {
            // Fallback: find the nearest floor tile
            respawnPosition = FindNearestFloorPosition();
        }
        
        // Move the player slightly backwards from the respawn position to avoid immediate re-falling
        Vector3 safeRespawnPosition = GetSafeRespawnPosition(respawnPosition);
        
        // Move the player to the safe respawn position
        player.transform.position = safeRespawnPosition;
        

        
        // Restore player input and momentum
        RestorePlayerInputAndMomentum();
        
        // Reset flags
        isRespawning = false;
        isFalling = false;
        globalFalling = false; // Reset global flag
    }
    
    /// <summary>
    /// Update the last floor position when player is on a floor tile
    /// </summary>
    void UpdateLastFloorPosition()
    {
        if (player == null || dungeonGenerator == null) return;
        
        // Get player's current grid position
        Vector2Int playerGridPos = new Vector2Int(
            Mathf.RoundToInt(player.transform.position.x),
            Mathf.RoundToInt(player.transform.position.y)
        );
        
        // Check if player is on a floor tile
        if (IsFloorTile(playerGridPos.x, playerGridPos.y))
        {
            lastFloorPosition = player.transform.position;
            isPlayerOnFloor = true;
        }
        else
        {
            isPlayerOnFloor = false;
        }
    }
    
    /// <summary>
    /// Check if a tile is a floor tile
    /// </summary>
    bool IsFloorTile(int x, int y)
    {
        if (dungeonGenerator == null) return false;
        
        TileType tileType = dungeonGenerator.GetTileType(x, y);
        return tileType == TileType.Floor || 
               tileType == TileType.BossFloor || 
               tileType == TileType.SpecialFloor || 
               tileType == TileType.NonEssentialFloor;
    }
    
    /// <summary>
    /// Check if the player is within the hole tile
    /// </summary>
    bool IsPlayerFullyInHole(Collider2D playerCollider)
    {
        if (playerCollider == null) return false;
        
        // Get the player's position
        Vector3 playerPosition = player.transform.position;
        
        // Get the hole's position (this GameObject's position)
        Vector3 holePosition = transform.position;
        
        // Calculate distance between player and hole centers
        float distance = Vector2.Distance(
            new Vector2(playerPosition.x, playerPosition.y),
            new Vector2(holePosition.x, holePosition.y)
        );
        
        // Use a simple distance-based check - if player is close enough to hole center, they're in it
        bool playerInHole = distance <= holeDetectionRadius;
        
        return playerInHole;
    }
    
    /// <summary>
    /// Fallback method: Check if the player's center is within the hole
    /// </summary>
    bool IsPlayerCenterInHole(Collider2D playerCollider)
    {
        if (playerCollider == null) return false;
        
        // Get the bounds of the hole (this GameObject's collider)
        Collider2D holeCollider = GetComponent<Collider2D>();
        if (holeCollider == null) return false;
        
        Bounds holeBounds = holeCollider.bounds;
        
        // Check if the player's center is within the hole bounds
        Vector3 playerCenter = playerCollider.bounds.center;
        bool centerInHole = holeBounds.Contains(playerCenter);
        
        return centerInHole;
    }
    
    /// <summary>
    /// Get a safe respawn position slightly backwards from the given position
    /// </summary>
    Vector3 GetSafeRespawnPosition(Vector3 originalPosition)
    {
        if (dungeonGenerator == null) return originalPosition;
        
        // Calculate the direction from the hole to the player's last position
        Vector3 holePosition = transform.position;
        Vector3 directionFromHole = (originalPosition - holePosition).normalized;
        
        // If we can't determine direction (player was exactly at hole center), use a default
        if (directionFromHole == Vector3.zero)
        {
            directionFromHole = Vector3.left; // Default to left
        }
        
        // Try the calculated direction first, then fallback directions
        Vector3[] directions = {
            directionFromHole,  // Try the calculated direction first
            -directionFromHole, // Then the opposite direction
            Vector3.left,       // Then left
            Vector3.right,      // Then right
            Vector3.down,       // Then down
            Vector3.up,         // Then up
            Vector3.left + Vector3.down,  // Then diagonal combinations
            Vector3.right + Vector3.down,
            Vector3.left + Vector3.up,
            Vector3.right + Vector3.up
        };
        
        float safeDistance = 1.0f; // Distance to move backwards (increased for better safety)
        
        foreach (Vector3 direction in directions)
        {
            Vector3 testPosition = originalPosition + (direction.normalized * safeDistance);
            
            // Check if this position is on a floor tile
            Vector2Int gridPos = new Vector2Int(
                Mathf.RoundToInt(testPosition.x),
                Mathf.RoundToInt(testPosition.y)
            );
            
            if (IsFloorTile(gridPos.x, gridPos.y))
            {
                return testPosition;
            }
        }
        
        // If no safe position found, return the original position
        return originalPosition;
    }
    
    /// <summary>
    /// Find the nearest floor position as a fallback respawn point
    /// </summary>
    Vector3 FindNearestFloorPosition()
    {
        if (dungeonGenerator == null) return Vector3.zero;
        
        Vector2Int playerGridPos = new Vector2Int(
            Mathf.RoundToInt(player.transform.position.x),
            Mathf.RoundToInt(player.transform.position.y)
        );
        
        // Search in expanding circles for the nearest floor tile
        for (int radius = 1; radius <= 10; radius++)
        {
            for (int x = playerGridPos.x - radius; x <= playerGridPos.x + radius; x++)
            {
                for (int y = playerGridPos.y - radius; y <= playerGridPos.y + radius; y++)
                {
                    // Check if this position is at the current radius
                    if (Mathf.Abs(x - playerGridPos.x) == radius || Mathf.Abs(y - playerGridPos.y) == radius)
                    {
                        if (IsFloorTile(x, y))
                        {
                            return new Vector3(x, y, player.transform.position.z);
                        }
                    }
                }
            }
        }
        
        // If no floor tile found, return the player's current position
        return player.transform.position;
    }
    
    /// <summary>
    /// Set the player reference (can be called from other scripts)
    /// </summary>
    public void SetPlayer(GameObject playerObject)
    {
        player = playerObject;
    }
    
    /// <summary>
    /// Stop all player input and momentum
    /// </summary>
    void StopPlayerInputAndMomentum()
    {
        // Disable player controller to stop input
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Stop all momentum by zeroing velocity
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }
    }
    
    /// <summary>
    /// Restore player input and momentum
    /// </summary>
    void RestorePlayerInputAndMomentum()
    {
        // Re-enable player controller
        if (playerController != null)
        {
            playerController.enabled = true;
        }
    }
    
    /// <summary>
    /// Set the dungeon generator reference (can be called from other scripts)
    /// </summary>
    public void SetDungeonGenerator(DungeonGenerator generator)
    {
        dungeonGenerator = generator;
    }
}
