using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    
    [Header("Defense Settings")]
    [SerializeField] private int defense = 0;
    [SerializeField] private EnemyType enemyType = EnemyType.Normal;
    [SerializeField] private bool isDefenseReduced = false;
    [SerializeField] private float defenseReductionPercent = 0f;
    [SerializeField] private float defenseReductionDuration = 0f;
    
    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private Color critFlashColor = new Color(1.0f, 0.5f, 0.0f); // Orange
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private GameObject deathEffectPrefab;
    
    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private bool canBeKnockedBack = true;
    
    [Header("Events")]
    public UnityEvent<int> onDamaged;
    public UnityEvent<float> onHealthPercentChanged;
    public UnityEvent onDeath;
    
    // Accessed by DungeonGenerator for boss rooms
    public int MaxHealth { 
        get { return maxHealth; } 
        set { 
            maxHealth = value; 
            // Adjust current health proportionally when max health changes
            float healthPercent = (float)currentHealth / maxHealth;
            currentHealth = Mathf.RoundToInt(value * healthPercent);
            onHealthPercentChanged?.Invoke(GetHealthPercent());
        }
    }
    
    public int CurrentHealth {
        get { return currentHealth; }
    }
    
    public int Defense {
        get { return defense; }
        set { defense = value; }
    }
    
    public EnemyType Type {
        get { return enemyType; }
    }
    
    private bool isDead = false;
    private bool isInvulnerable = false;
    private bool isBeingKnockedBack = false;
    private Rigidbody2D rb;
    
    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // Get or add Rigidbody2D for knockback
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; // No gravity for top-down game
            rb.drag = 5f; // Add some drag to slow down movement
        }
        
        currentHealth = maxHealth;
    }
    
    /// <summary>
    /// Sets the enemy's invulnerable state (used during dodge)
    /// </summary>
    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;
        Debug.Log($"Enemy {gameObject.name} invulnerability set to: {invulnerable}");
    }
    
    /// <summary>
    /// Inflicts damage with support for penetration and critical hits
    /// </summary>
    public void TakeDamage(int damage, float penetrationPercent = 0f, bool isCritical = false)
    {
        TakeDamage(damage, penetrationPercent, isCritical, Vector2.zero);
    }
    
    /// <summary>
    /// Inflicts damage with support for penetration, critical hits, and knockback
    /// </summary>
    public void TakeDamage(int damage, float penetrationPercent, bool isCritical, Vector2 knockbackDirection)
    {
        if (isDead || isInvulnerable) return;
        
        // Calculate effective defense
        float effectiveDefense = defense;
        
        // Apply defense reduction if active
        if (isDefenseReduced)
        {
            effectiveDefense *= (1 - defenseReductionPercent);
        }
        
        // Apply penetration
        effectiveDefense *= (1 - penetrationPercent);
        
        // Apply defense reduction (minimum 0)
        int reducedDamage = Mathf.Max(1, damage - Mathf.RoundToInt(effectiveDefense));
        
        // Apply critical multiplier if critical hit
        if (isCritical)
        {
            reducedDamage = Mathf.RoundToInt(reducedDamage * 2f); // Default 2x damage for crits
            StartCoroutine(FlashCriticalDamage());
        }
        else
        {
            StartCoroutine(FlashDamage());
        }
        
        // Apply knockback if enabled and direction is provided
        if (canBeKnockedBack && knockbackDirection != Vector2.zero && !isBeingKnockedBack)
        {
            ApplyKnockback(knockbackDirection);
        }
        
        // Apply the actual damage
        currentHealth -= reducedDamage;
        
        // Clamp health to valid range
        currentHealth = Mathf.Max(0, currentHealth);
        
        // Trigger events
        onDamaged?.Invoke(reducedDamage);
        onHealthPercentChanged?.Invoke(GetHealthPercent());
        
        // Check for death
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Standard damage method for backward compatibility
    /// </summary>
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, 0f, false, Vector2.zero);
    }
    
    /// <summary>
    /// Restores health
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead) return;
        
        int oldHealth = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        
        if (currentHealth != oldHealth)
        {
            onHealthPercentChanged?.Invoke(GetHealthPercent());
        }
    }
    
    /// <summary>
    /// Returns current health as percentage (0-1)
    /// </summary>
    public float GetHealthPercent()
    {
        return (float)currentHealth / maxHealth;
    }
    
    /// <summary>
    /// Handles death logic and effects
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        // Spawn death effect if available
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position + new Vector3(0,0,-3f), Quaternion.identity);
        }
        
        // Invoke death event so other components can respond
        onDeath?.Invoke();
        
        // Destroy the enemy GameObject
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Visual flash effect when taking damage
    /// </summary>
    private IEnumerator FlashDamage()
    {
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = damageFlashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }
    }
    
    /// <summary>
    /// Enhanced visual flash effect for critical hits
    /// </summary>
    private IEnumerator FlashCriticalDamage()
    {
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            
            // Brighter flash for crits
            spriteRenderer.color = critFlashColor;
            
            // Flash for longer
            yield return new WaitForSeconds(flashDuration * 2);
            
            // Fade back to normal
            float elapsedTime = 0f;
            float fadeTime = flashDuration;
            
            while (elapsedTime < fadeTime)
            {
                spriteRenderer.color = Color.Lerp(critFlashColor, originalColor, elapsedTime / fadeTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            spriteRenderer.color = originalColor;
        }
    }
    
    /// <summary>
    /// Applies defense reduction effect
    /// </summary>
    public void ReduceDefense(float reductionPercent, float duration)
    {
        defenseReductionPercent = reductionPercent;
        defenseReductionDuration = duration;
        isDefenseReduced = true;
        
        // Start a coroutine to restore defense after duration
        StartCoroutine(RestoreDefenseAfterDuration());
    }
    
    private IEnumerator RestoreDefenseAfterDuration()
    {
        yield return new WaitForSeconds(defenseReductionDuration);
        isDefenseReduced = false;
        defenseReductionPercent = 0f;
    }
    
    /// <summary>
    /// Applies knockback force to the enemy
    /// </summary>
    private void ApplyKnockback(Vector2 direction)
    {
        if (rb == null) return;
        
        isBeingKnockedBack = true;
        
        // Normalize direction and apply force
        Vector2 normalizedDirection = direction.normalized;
        rb.AddForce(normalizedDirection * knockbackForce, ForceMode2D.Impulse);
        
        // Start coroutine to end knockback after duration
        StartCoroutine(EndKnockbackAfterDuration());
    }
    
    /// <summary>
    /// Ends knockback effect after the specified duration
    /// </summary>
    private IEnumerator EndKnockbackAfterDuration()
    {
        yield return new WaitForSeconds(knockbackDuration);
        isBeingKnockedBack = false;
        
        // Stop the rigidbody movement
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }

    // Implement the IDamageable.TakeDamage method
    void IDamageable.TakeDamage(int damage)
    {
        TakeDamage(damage);
    }
}

/// <summary>
/// Defines different types of enemies for elemental interactions
/// </summary>
public enum EnemyType
{
    Normal,     // Standard enemy with no elemental affinity
    Fire,       // Fire-based enemy
    Ice,        // Ice-based enemy
    Lightning,  // Lightning-based enemy
    Earth,      // Earth-based enemy
    Water,      // Water-based enemy
    Shadow,     // Shadow/dark-based enemy
    Light       // Light-based enemy
}

/// <summary>
/// Defines different spell elements for damage calculations
/// </summary>
public enum SpellElement
{
    Physical,   // Non-elemental physical damage
    Fire,       // Fire element
    Ice,        // Ice element
    Lightning,  // Lightning element
    Earth,      // Earth element
    Water,      // Water element
    Shadow,     // Shadow/dark element
    Light       // Light element
}