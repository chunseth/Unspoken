using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public int maxHealth = 10;
    public int currentHealth;
    
    [Header("Mana Settings")]
    public int maxMana = 100;
    public int currentMana;
    public float manaRegenRate = 0f; // Mana per second
    
    [Header("Visual Feedback")]
    public float invincibilityDuration = 0.5f;
    public float flashDuration = 0.1f;
    public SpriteRenderer playerSprite;
    [Range(0f, 1f)]
    public float invincibilityOpacity = 0.3f;
    
    [Header("Screen Flash")]
    public RawImage damageFlashImage; // RawImage that covers the screen
    public Color flashColor = new Color(1f, 0f, 0f, 0.3f); // Red with transparency
    public float screenFlashDuration = 0.2f;
    
    [Header("Knockback")]
    public float knockbackForce = 10f;
    public float knockbackDuration = 0.25f;
    
    private bool isInvincible = false;
    private Coroutine currentInvincibilityCoroutine;
    private Coroutine currentScreenFlashCoroutine;
    private Coroutine currentKnockbackCoroutine;
    private Rigidbody2D rb;
    private Vector2 knockbackDirection;

    private PlayerAttack playerAttack;
    private PlayerController playerController;
    private bool isInKnockback = false;

    // Event that can be subscribed to by UI elements
    public delegate void HealthChangedDelegate(int currentHealth, int maxHealth);
    public event HealthChangedDelegate OnHealthChanged;
    
    // Add event for mana changes
    public delegate void ManaChangedDelegate(int currentMana, int maxMana);
    public event ManaChangedDelegate OnManaChanged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogWarning("No Rigidbody2D found on player - knockback will not work");
        }
        
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("No PlayerController found on player - knockback might not work properly");
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        currentMana = maxMana;
        
        // Find sprite renderer if not assigned
        if (playerSprite == null)
        {
            playerSprite = GetComponent<SpriteRenderer>();
            if (playerSprite == null)
            {
                playerSprite = GetComponentInChildren<SpriteRenderer>();
            }
        }

        // If no damage flash image is set, try to find one
        if (damageFlashImage == null)
        {
            damageFlashImage = GameObject.FindGameObjectWithTag("DamageFlash")?.GetComponent<RawImage>();
            if (damageFlashImage == null)
            {
                Debug.LogWarning("No damage flash RawImage assigned or found with 'DamageFlash' tag");
            }
            else
            {
                // Make sure image starts transparent
                Color transparent = flashColor;
                transparent.a = 0f;
                damageFlashImage.color = transparent;
            }
        }
        
        // Get reference to PlayerAttack component
        playerAttack = GetComponent<PlayerAttack>();
    }
    
    private void Update()
    {
        // Regenerate mana over time
        if (currentMana < maxMana)
        {
            float manaToAdd = manaRegenRate * Time.deltaTime;
            currentMana = Mathf.Min(maxMana, currentMana + Mathf.RoundToInt(manaToAdd));
            OnManaChanged?.Invoke(currentMana, maxMana);
        }
    }

    // Implement the IDamageable interface method
    void IDamageable.TakeDamage(int damage)
    {
        // Call our public method - use default direction (based on player facing)
        TakeDamage(damage);
    }
    
    // Main damage method that other code can call directly
    public void TakeDamage(int damage)
    {
        // Don't take damage during invincibility frames
        if (isInvincible) 
        {
            Debug.Log("Player is invincible - damage ignored");
            return;
        }
        
        currentHealth -= damage;
        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");
        
        // Invoke the event for UI updates
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        // Interrupt any attack in progress
        if (playerAttack != null)
        {
            playerAttack.InterruptAttack();
        }
        
        // Apply screen flash effect
        FlashScreen();
        
        // Apply knockback (default direction is backward from player's facing)
        ApplyKnockback();
        
        // Flash and brief invincibility
        StartInvincibilityFrames();
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    // Overloaded version that takes direction info for knockback
    public void TakeDamage(int damage, Vector2 damageSource)
    {
        // Calculate knockback direction away from damage source
        if (rb != null)
        {
            knockbackDirection = ((Vector2)transform.position - damageSource).normalized;
        }
        
        // Call the regular damage method
        TakeDamage(damage);
    }
    
    private void FlashScreen()
    {
        if (damageFlashImage != null)
        {
            // Stop any existing flash coroutine
            if (currentScreenFlashCoroutine != null)
            {
                StopCoroutine(currentScreenFlashCoroutine);
            }
            
            currentScreenFlashCoroutine = StartCoroutine(ScreenFlashCoroutine());
        }
    }
    
    private IEnumerator ScreenFlashCoroutine()
    {
        // Set flash color with full alpha
        damageFlashImage.color = flashColor;
        
        // Fade out
        float elapsed = 0f;
        Color startColor = flashColor;
        Color endColor = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        
        while (elapsed < screenFlashDuration)
        {
            damageFlashImage.color = Color.Lerp(startColor, endColor, elapsed / screenFlashDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure fully transparent at end
        damageFlashImage.color = endColor;
        currentScreenFlashCoroutine = null;
    }
    
    private void ApplyKnockback()
    {
        if (rb == null) return;
        
        // Default knockback direction if none set
        if (knockbackDirection == Vector2.zero)
        {
            // Default to opposite of player's facing direction
            knockbackDirection = -transform.right; // Assuming player's "right" is the facing direction
        }
        
        // Stop any existing knockback
        if (currentKnockbackCoroutine != null)
        {
            StopCoroutine(currentKnockbackCoroutine);
        }
        
        currentKnockbackCoroutine = StartCoroutine(KnockbackCoroutine());
    }
    
    private IEnumerator KnockbackCoroutine()
    {
        isInKnockback = true;
        
        // Disable player controller during knockback
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Store original values
        bool wasKinematic = rb.isKinematic;
        float originalGravity = rb.gravityScale;
        
        // Setup for knockback
        rb.isKinematic = false;
        rb.gravityScale = 0f; // Disable gravity during knockback
        
        // Apply the impulse force
        rb.velocity = Vector2.zero; // Clear any existing velocity
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        
        // Wait for knockback duration
        yield return new WaitForSeconds(knockbackDuration);
        
        // Reset velocity and restore original settings
        rb.velocity = Vector2.zero;
        rb.isKinematic = wasKinematic;
        rb.gravityScale = originalGravity;
        
        // Re-enable player controller after knockback
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        // Reset knockback direction
        knockbackDirection = Vector2.zero;
        currentKnockbackCoroutine = null;
        isInKnockback = false;
    }
    
    private void StartInvincibilityFrames()
    {
        // Stop any existing invincibility coroutine
        if (currentInvincibilityCoroutine != null)
        {
            StopCoroutine(currentInvincibilityCoroutine);
        }
        
        currentInvincibilityCoroutine = StartCoroutine(InvincibilityFrames());
    }
    
    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        
        // Flash the sprite with red opacity
        if (playerSprite != null)
        {
            // Store original color
            Color originalColor = playerSprite.color;
            
            // Create flash color: keep original green and blue, but max red with 0.3 opacity
            Color flashColor = new Color(
                1.0f,                  // Red at maximum
                originalColor.g,       // Keep original green
                originalColor.b,       // Keep original blue
                invincibilityOpacity   // Use configured opacity
            );
            
            float endTime = Time.time + invincibilityDuration;
            bool isFlashing = false;
            
            // Flash while invincible
            while (Time.time < endTime)
            {
                isFlashing = !isFlashing;
                playerSprite.color = isFlashing ? flashColor : originalColor;
                yield return new WaitForSeconds(flashDuration);
            }
            
            // Ensure sprite is back to normal after invincibility
            playerSprite.color = originalColor;
        }
        else
        {
            // If no sprite to flash, just wait
            yield return new WaitForSeconds(invincibilityDuration);
        }
        
        isInvincible = false;
        currentInvincibilityCoroutine = null;
    }
    
    // Add method to check if player is currently in knockback state
    public bool IsInKnockback()
    {
        return isInKnockback;
    }

    // Modify the SetInvincible method to handle knockback interaction
    public void SetInvincible(bool invincible)
    {
        isInvincible = invincible;
        
        if (invincible)
        {
            // Cancel any ongoing knockback when becoming invincible (e.g., from dodge)
            if (isInKnockback && currentKnockbackCoroutine != null)
            {
                StopCoroutine(currentKnockbackCoroutine);
                
                // Re-enable player control
                if (playerController != null && !playerController.enabled)
                {
                    playerController.enabled = true;
                }
                
                // Reset knockback state
                knockbackDirection = Vector2.zero;
                isInKnockback = false;
                currentKnockbackCoroutine = null;
            }
            
            // Optional: add visual feedback for dodge invincibility
            if (playerSprite != null)
            {
                // Apply a subtle invincibility effect (e.g., slight transparency)
                Color c = playerSprite.color;
                playerSprite.color = new Color(c.r, c.g, c.b, invincibilityOpacity + 0.3f);
            }
            
            // Make the player's collider ignore projectiles during invincibility
            // This is a more foolproof approach than relying on damage checks
            Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("EnemyProjectile"), true);
        }
        else
        {
            // Reset sprite to normal appearance
            if (playerSprite != null)
            {
                Color c = playerSprite.color;
                playerSprite.color = new Color(c.r, c.g, c.b, 1.0f);
            }
            
            // Re-enable collisions with projectiles
            Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("EnemyProjectile"), false);
        }
    }

    // Public method to check invincibility status
    public bool IsInvincible()
    {
        return isInvincible;
    }
    
    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        
        // Invoke the event for UI updates
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        Debug.Log($"Player healed {amount} health. Health: {currentHealth}/{maxHealth}");
    }
    
    // New methods for mana management
    public bool UseMana(int amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            OnManaChanged?.Invoke(currentMana, maxMana);
            return true;
        }
        return false;
    }
    
    public void AddMana(int amount)
    {
        currentMana = Mathf.Min(currentMana + amount, maxMana);
        OnManaChanged?.Invoke(currentMana, maxMana);
        Debug.Log($"Player gained {amount} mana. Mana: {currentMana}/{maxMana}");
    }
    
    // Life leech implementation
    public void ApplyLifeLeech(int damage, float leechPercent)
    {
        if (leechPercent <= 0) return;
        
        int healAmount = Mathf.RoundToInt(damage * (leechPercent / 100f));
        if (healAmount > 0)
        {
            Heal(healAmount);
            Debug.Log($"Life leech: Converted {damage} damage to {healAmount} health");
        }
    }
    
    // Mana return implementation
    public void ReturnMana(float spellCost, float returnPercent)
    {
        if (returnPercent <= 0) return;
        
        int manaAmount = Mathf.RoundToInt(spellCost * (returnPercent / 100f));
        if (manaAmount > 0)
        {
            AddMana(manaAmount);
            Debug.Log($"Mana return: Received {manaAmount} mana back from spell cast");
        }
    }
    
    private void Die()
    {
        Debug.Log("Player died!");
        
        // Find and trigger the GameOverManager
        GameOverManager gameOverManager = FindObjectOfType<GameOverManager>();
        if (gameOverManager != null)
        {
            gameOverManager.TriggerGameOver();
        }
        else
        {
            Debug.LogWarning("No GameOverManager found in scene! Player will be disabled as fallback.");
            // Fallback: disable the player if no GameOverManager is found
            gameObject.SetActive(false);
        }
    }
    
    // Public getter for health values
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
    
    // Public getter for mana values
    public int GetCurrentMana() => currentMana;
    public int GetMaxMana() => maxMana;
    public float GetManaPercentage() => (float)currentMana / maxMana;
} 