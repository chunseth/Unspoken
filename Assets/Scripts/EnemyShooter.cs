using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject projectilePrefab;
    public float fireRate;
    public float projectileSpeed;
    public float shootingRange;
    public LayerMask obstacleLayer; // Add this to check line of sight
    
    [Header("References")]
    public Transform firePoint; // Point where projectiles spawn
    
    private Transform player;
    private float nextFireTime;
    private EnemyAI enemyAI; // Updated reference name
    private float detectionRange; // Store the AI's detection range
    
    // Stun properties
    private bool isStunned = false;
    private float stunEndTime = 0f;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        enemyAI = GetComponent<EnemyAI>(); // Updated component reference
        
        if (player == null)
            Debug.LogWarning("Player not found! Make sure it has the 'Player' tag.");
            
        if (firePoint == null)
            firePoint = transform; // Use enemy position if no fire point is set
            
        // Get the detection range from EnemyAI
        if (enemyAI != null)
        {
            detectionRange = enemyAI.detectionRange;
        }
    }

    private void Update()
    {
        if (player == null || enemyAI == null) return;
        
        // Check if stun has expired
        if (isStunned && Time.time > stunEndTime)
        {
            isStunned = false;
            
            // Visual feedback for stun wearing off (optional)
            if (enemyAI != null)
            {
                // Return to normal behavior
                enemyAI.SetMovementMultiplier(1.0f);
            }
        }
        
        // Skip shooting logic if stunned
        if (isStunned) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // First check if player is within detection range
        if (distanceToPlayer > detectionRange)
        {
            // Don't try to manage state if player is out of detection range
            return;
        }
        
        // Only handle shooting behavior if player is in range AND we have line of sight
        if (distanceToPlayer <= shootingRange && HasLineOfSightToPlayer())
        {
            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + 1f/fireRate;
            }
            
            // Only suggest shooting state if we're in range and have line of sight
            if (enemyAI.currentState != EnemyState.Shooting)
            {
                enemyAI.SetState(EnemyState.Shooting);
            }
        }
    }

    private bool HasLineOfSightToPlayer()
    {
        if (player == null) return false;
        
        // Cast a ray to check for obstacles between enemy and player
        RaycastHit2D hit = Physics2D.Linecast(
            firePoint.position,
            player.position,
            obstacleLayer
        );
        
        // If nothing was hit, we have line of sight
        return hit.collider == null;
    }

    private void Shoot()
    {
        // Calculate direction to player
        Vector2 direction = ((Vector2)player.position - (Vector2)firePoint.position).normalized;
        
        // Create projectile
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        
        // Set projectile velocity
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = direction * projectileSpeed;
            
            // Calculate rotation to face movement direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            projectile.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        
        // Destroy projectile after 5 seconds to prevent cluttering the scene
        Destroy(projectile, 3f);
    }
    
    /// <summary>
    /// Apply stun effect to this enemy
    /// </summary>
    public void ApplyStun(float duration)
    {
        isStunned = true;
        stunEndTime = Time.time + duration;
        
        // Tell AI to stop movement
        if (enemyAI != null)
        {
            enemyAI.SetMovementMultiplier(0f);
        }
        
        // Visual feedback for stun (optional)
        // You could add particle effects or sprite changes here
        
        Debug.Log($"{gameObject.name} stunned for {duration} seconds");
    }

    /// <summary>
    /// Delays the enemy's next attack by the specified duration
    /// </summary>
    /// <param name="delay">Time in seconds to delay the next attack</param>
    public void DelayAttack(float delay)
    {
        // Add delay to the next fire time
        nextFireTime = Mathf.Max(nextFireTime, Time.time + delay);
        Debug.Log($"{gameObject.name} attack delayed for {delay} seconds");
    }

    // Optional: Visualize the shooting range in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootingRange);
    }
} 