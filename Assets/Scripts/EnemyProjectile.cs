using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float projectileSpeed = 7f;
    public int damage = 1;
    public float maxLifetime = 5f;
    
    [Header("Visual Effects")]
    public GameObject hitEffectPrefab;
    public TrailRenderer trailRenderer;
    
    private Vector2 direction;
    private bool hasHit = false;
    private GameObject sender; // The entity that fired this projectile
    private GameObject currentTarget; // Current target of the projectile
    private bool isPlayerProjectile = false; // Tracks if this has been converted to a player projectile
    private float damageMultiplier = 1.0f; // For reflected projectiles
    
    private void Start()
    {
        // Destroy after maximum lifetime to prevent clutter
        Destroy(gameObject, maxLifetime);
    }
    
    public void Initialize(Vector2 targetDirection, GameObject sender = null)
    {
        direction = targetDirection.normalized;
        this.sender = sender; // Store the sender
        
        // Set proper Z position
        Vector3 position = transform.position;
        position.z = 0;
        transform.position = position;
        
        // Rotate to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        Debug.Log($"Enemy projectile initialized with direction: {direction}");
    }
    
    private void Update()
    {
        if (hasHit) return;
        
        // If we have a target, adjust direction toward it
        if (currentTarget != null && !isTargetingDirection)
        {
            Vector2 targetDirection = ((Vector2)currentTarget.transform.position - (Vector2)transform.position).normalized;
            direction = Vector2.Lerp(direction, targetDirection, Time.deltaTime * 5f); // Adjust homing strength as needed
            
            // Update rotation to face new direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        
        // Move in the current direction
        Vector3 movement = new Vector3(
            direction.x * projectileSpeed * Time.deltaTime,
            direction.y * projectileSpeed * Time.deltaTime,
            0
        );
        
        transform.position += movement;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Enemy projectile hit: {other.gameObject.name}, isPlayerProjectile: {isPlayerProjectile}");
        
        if (hasHit) return;
        
        // If this is now a player projectile, don't hit the player
        if (isPlayerProjectile && other.CompareTag("Player"))
        {
            Debug.Log("Skipping collision with player because projectile was reflected");
            return;
        }
        
        // If this is still an enemy projectile, don't hit enemies
        if (!isPlayerProjectile && (other.CompareTag("Enemy") || other.GetComponent<EnemyAI>() != null))
        {
            Debug.Log("Skipping collision with enemy because projectile is from enemy");
            return;
        }
        
        // If this is now a player projectile and it hit the original sender, deal damage
        if (isPlayerProjectile && sender != null && other.gameObject == sender)
        {
            Debug.Log($"Reflected projectile hit original sender: {sender.name}");
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                int reflectedDamage = Mathf.RoundToInt(damage * damageMultiplier);
                damageable.TakeDamage(reflectedDamage);
                Debug.Log($"Dealt {reflectedDamage} reflected damage to {sender.name}");
            }
        }
        // If this is now a player projectile and it hit any enemy
        else if (isPlayerProjectile && (other.CompareTag("Enemy") || other.GetComponent<EnemyAI>() != null))
        {
            Debug.Log($"Reflected projectile hit enemy: {other.gameObject.name}");
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                int reflectedDamage = Mathf.RoundToInt(damage * damageMultiplier);
                damageable.TakeDamage(reflectedDamage);
                Debug.Log($"Dealt {reflectedDamage} reflected damage to {other.gameObject.name}");
            }
        }
        // If this is still an enemy projectile and it hit the player
        else if (!isPlayerProjectile && other.CompareTag("Player"))
        {
            Debug.Log("Hit player!");
            
            // Try to get the PlayerHealth component to damage player
            PlayerStats playerHealth = other.GetComponent<PlayerStats>();
            if (playerHealth != null)
            {
                // Pass the projectile's position as the damage source for knockback
                playerHealth.TakeDamage(damage, transform.position);
            }
            else
            {
                // Fallback to IDamageable interface if new component not found
                IDamageable damageable = other.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                }
                else
                {
                    Debug.LogWarning("Player has no health component! Cannot apply damage.");
                }
            }
        }
        
        hasHit = true;
        
        // Spawn hit effect
        SpawnHitEffect(transform.position + new Vector3(0,0,-1));
        
        // Disable components
        DisableComponents();
        
        // Destroy the projectile
        Destroy(gameObject, 0.1f);
    }
    
    // New properties and methods for reflection functionality
    
    // True if we're just targeting a direction, false if homing toward a gameobject
    private bool isTargetingDirection = true;
    
    // Get the original sender of this projectile
    public GameObject GetSender()
    {
        return sender;
    }
    
    // Set a new target for the projectile (used when reflecting)
    public void SetTarget(GameObject target)
    {
        currentTarget = target;
        isTargetingDirection = false;
        Debug.Log($"Projectile retargeted to {target.name}");
    }
    
    // Convert this from an enemy projectile to a player projectile
    public void ConvertToPlayerProjectile()
    {
        isPlayerProjectile = true;
        
        // Optional: Change appearance to show it's now friendly
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            // Add a blue tint to show it's now a friendly projectile
            renderer.color = new Color(0.7f, 0.7f, 1.0f, renderer.color.a);
        }
        
        // Could also change the trail color
        if (trailRenderer != null)
        {
            // Get the current gradient and modify its colors
            Gradient gradient = trailRenderer.colorGradient;
            GradientColorKey[] colorKeys = gradient.colorKeys;
            for (int i = 0; i < colorKeys.Length; i++)
            {
                Color color = colorKeys[i].color;
                // Add blue tint
                color.r *= 0.7f;
                color.g *= 0.7f;
                color.b = Mathf.Min(color.b * 1.5f, 1f);
                colorKeys[i].color = color;
            }
            
            // Apply the modified gradient
            Gradient newGradient = new Gradient();
            newGradient.SetKeys(colorKeys, gradient.alphaKeys);
            trailRenderer.colorGradient = newGradient;
        }
        
        Debug.Log("Projectile converted to player projectile");
    }
    
    // Reverse the direction of the projectile
    public void ReverseDirection()
    {
        direction = -direction;
        
        // Update rotation to face new direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        Debug.Log($"Projectile direction reversed to {direction}");
    }
    
    // Set damage multiplier for reflected projectiles
    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = multiplier;
        Debug.Log($"Projectile damage multiplier set to {multiplier}");
    }
    
    // Check if this is an enemy projectile (used by reflection system)
    public bool IsEnemyProjectile()
    {
        return !isPlayerProjectile;
    }
    
    // Existing methods below
    
    private void SpawnHitEffect(Vector3 position)
    {
        if (hitEffectPrefab == null) return;
        
        Debug.Log("Spawning enemy projectile hit effect");
        GameObject effectInstance = Instantiate(hitEffectPrefab, position, Quaternion.identity);
        
        // Ensure effect renders on top
        SpriteRenderer effectRenderer = effectInstance.GetComponent<SpriteRenderer>();
        if (effectRenderer != null)
        {
            // Use a high sorting order
            effectRenderer.sortingOrder = 0;
            Debug.Log("Set hit effect sorting order to 100");
        }
        
        // For particle systems, handle each particle renderer
        ParticleSystem ps = effectInstance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ParticleSystemRenderer psRenderer = ps.GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null)
            {
                // Use a high sorting order
                psRenderer.sortingOrder = 0;
                Debug.Log("Set particle system sorting order to 100");
            }
        }
        
        // Ensure the hit effect is destroyed
        if (ps != null)
        {
            float totalDuration = ps.main.duration + ps.main.startLifetimeMultiplier;
            Destroy(effectInstance, totalDuration);
            Debug.Log($"Enemy hit effect will be destroyed after {totalDuration} seconds (particle duration)");
        }
        else
        {
            // Otherwise use a fixed time
            Destroy(effectInstance, 3f);
            Debug.Log("Enemy hit effect will be destroyed after 3 seconds");
        }
    }
    
    private void DisableComponents()
    {
        // Disable collider
        Collider2D projectileCollider = GetComponent<Collider2D>();
        if (projectileCollider != null)
        {
            projectileCollider.enabled = false;
        }

        // Disable sprite renderer
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        // Disable trail renderer
        if (trailRenderer != null)
        {
            trailRenderer.enabled = false;
        }
    }
    
    // Fallback for non-trigger collisions
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Enemy projectile collision with: {collision.gameObject.name}");
        
        // Skip collisions based on projectile type
        if (!isPlayerProjectile && (collision.gameObject.CompareTag("Enemy") || 
            collision.gameObject.GetComponent<EnemyAI>() != null))
        {
            Debug.Log("Skipping collision with enemy because projectile is from enemy");
            return;
        }
        
        if (isPlayerProjectile && collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Skipping collision with player because projectile was reflected");
            return;
        }
        
        if (hasHit) return;
        hasHit = true;
        
        // Handle damage for reflected projectiles hitting enemies
        if (isPlayerProjectile && (collision.gameObject.CompareTag("Enemy") || 
            collision.gameObject.GetComponent<EnemyAI>() != null))
        {
            IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                int reflectedDamage = Mathf.RoundToInt(damage * damageMultiplier);
                damageable.TakeDamage(reflectedDamage);
                Debug.Log($"Dealt {reflectedDamage} reflected damage to {collision.gameObject.name}");
            }
        }
        
        // Handle player collision for enemy projectiles
        if (!isPlayerProjectile && collision.gameObject.CompareTag("Player"))
        {
            PlayerStats playerHealth = collision.gameObject.GetComponent<PlayerStats>();
            if (playerHealth != null)
            {
                // Pass the projectile's position as the damage source for knockback
                playerHealth.TakeDamage(damage, transform.position);
            }
        }
        
        // Spawn hit effect
        SpawnHitEffect(transform.position + new Vector3(0,0,1));
        
        // Disable components
        DisableComponents();
        
        // Destroy the projectile
        Destroy(gameObject, 0.1f);
    }
}

// Interface for objects that can take damage
public interface IDamageable
{
    void TakeDamage(int damage);
} 