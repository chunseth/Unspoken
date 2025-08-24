using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// An interactable object that can be picked up and carried by the player.
/// While carrying, the player cannot run, attack, or use abilities.
/// </summary>
public class CarryableObject : MonoBehaviour, IPointerClickHandler
{
    [Header("Interaction Settings")]
    [Tooltip("Distance the player must be within to interact with the object")]
    public float interactionDistance = 2f;
    [Tooltip("Key to drop the object while carrying")]
    public KeyCode dropKey = KeyCode.E;
    
    [Header("Carrying Settings")]
    [Tooltip("Offset from player position when carrying")]
    public Vector3 carryOffset = new Vector3(0, 0.5f, 0);
    [Tooltip("Speed multiplier while carrying (0.5 = half speed)")]
    public float carrySpeedMultiplier = 0.5f;
    
    [Header("Visual Feedback")]
    [Tooltip("Sprite to show when object is highlighted")]
    public Sprite highlightedSprite;
    [Tooltip("Color to tint the object when highlighted")]
    public Color highlightColor = Color.yellow;
    [Tooltip("Particle effect to play when picked up")]
    public GameObject pickupEffect;
    [Tooltip("Particle effect to play when dropped")]
    public GameObject dropEffect;
    
    [Header("Audio")]
    [Tooltip("Sound to play when picked up")]
    public AudioClip pickupSound;
    [Tooltip("Sound to play when dropped")]
    public AudioClip dropSound;
    
    [Header("References")]
    [Tooltip("Reference to the player GameObject")]
    public GameObject player;
    
    // Private variables
    private bool isCarried = false;
    private bool playerInRange = false;
    private SpriteRenderer spriteRenderer;
    private Sprite originalSprite;
    private Color originalColor;
    private AudioSource audioSource;
    private PlayerController playerController;
    private PlayerAttack playerAttack;
    private PathCreatorBeam beamAbility;
    private Rigidbody2D rb;
    private Collider2D objectCollider;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
    
    void Start()
    {
        // Get component references
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody2D>();
        objectCollider = GetComponent<Collider2D>();
        
        // Store original appearance
        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
            originalColor = spriteRenderer.color;
        }
        
        // Store original transform
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;
        
        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("CarryableObject: No player found with 'Player' tag. Please assign the player reference manually.");
            }
        }
        
        // Get player components
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            playerAttack = player.GetComponent<PlayerAttack>();
            beamAbility = player.GetComponent<PathCreatorBeam>();
        }
        
        // Add AudioSource if not present
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    void Update()
    {
        if (isCarried)
        {
            // Update position to follow player
            UpdateCarryPosition();
            
            // Check for drop input
            if (Input.GetKeyDown(dropKey))
            {
                DropObject();
            }
        }
        else
        {
            // Check if player is in range for interaction
            CheckPlayerDistance();
            
            // Check for pickup input (E key)
            if (playerInRange && Input.GetKeyDown(dropKey))
            {
                PickupObject();
            }
        }
    }
    
    void CheckPlayerDistance()
    {
        if (player == null) return;
        
        float distance = Vector2.Distance(transform.position, player.transform.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= interactionDistance;
        
        // Update visual feedback when entering/leaving range
        if (playerInRange != wasInRange)
        {
            UpdateVisualFeedback(playerInRange);
        }
    }
    
    void UpdateVisualFeedback(bool highlighted)
    {
        if (spriteRenderer == null) return;
        
        if (highlighted)
        {
            // Show highlighted appearance
            if (highlightedSprite != null)
            {
                spriteRenderer.sprite = highlightedSprite;
            }
            spriteRenderer.color = highlightColor;
        }
        else
        {
            // Restore original appearance
            spriteRenderer.sprite = originalSprite;
            spriteRenderer.color = originalColor;
        }
    }
    
    void UpdateCarryPosition()
    {
        if (player == null) return;
        
        // Calculate target position (player position + offset)
        Vector3 targetPosition = player.transform.position + carryOffset;
        
        // Smoothly move to target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
        
        // Keep the object's rotation stable
        transform.rotation = originalRotation;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isCarried) return; // Can't click while being carried
        
        if (playerInRange)
        {
            PickupObject();
        }
        else
        {
            Debug.Log("CarryableObject: Player is too far away to pick up the object. Move closer or press E when in range.");
        }
    }
    
    void PickupObject()
    {
        if (isCarried) return;
        
        Debug.Log("CarryableObject: Picking up object!");
        
        isCarried = true;
        
        // Disable physics and collider
        if (rb != null)
        {
            rb.simulated = false;
        }
        if (objectCollider != null)
        {
            objectCollider.enabled = false;
        }
        
        // Parent to player
        transform.SetParent(player.transform);
        
        // Apply carry restrictions to player
        ApplyCarryRestrictions();
        
        // Play effects
        PlayPickupEffects();
        
        // Update visual feedback
        UpdateVisualFeedback(false);
    }
    
    void DropObject()
    {
        if (!isCarried) return;
        
        Debug.Log("CarryableObject: Dropping object!");
        
        isCarried = false;
        
        // Unparent from player
        transform.SetParent(originalParent);
        
        // Re-enable physics and collider
        if (rb != null)
        {
            rb.simulated = true;
        }
        if (objectCollider != null)
        {
            objectCollider.enabled = true;
        }
        
        // Remove carry restrictions from player
        RemoveCarryRestrictions();
        
        // Play effects
        PlayDropEffects();
        
        // Check if player is still in range for future interactions
        CheckPlayerDistance();
    }
    
    void ApplyCarryRestrictions()
    {
        // Apply speed restriction to player
        if (playerController != null)
        {
            playerController.SpeedModifier = carrySpeedMultiplier;
        }
        
        // Disable attack
        if (playerAttack != null)
        {
            playerAttack.enabled = false;
        }
        
        // Disable beam ability
        if (beamAbility != null)
        {
            beamAbility.enabled = false;
        }
    }
    
    void RemoveCarryRestrictions()
    {
        // Remove speed restriction from player
        if (playerController != null)
        {
            playerController.SpeedModifier = 1f;
        }
        
        // Re-enable attack
        if (playerAttack != null)
        {
            playerAttack.enabled = true;
        }
        
        // Re-enable beam ability
        if (beamAbility != null)
        {
            beamAbility.enabled = true;
        }
    }
    
    void PlayPickupEffects()
    {
        // Play pickup sound
        if (audioSource != null && pickupSound != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }
        
        // Play pickup particle effect
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }
    }
    
    void PlayDropEffects()
    {
        // Play drop sound
        if (audioSource != null && dropSound != null)
        {
            audioSource.PlayOneShot(dropSound);
        }
        
        // Play drop particle effect
        if (dropEffect != null)
        {
            Instantiate(dropEffect, transform.position, Quaternion.identity);
        }
    }
    
    // Public method to check if object is being carried
    public bool IsCarried()
    {
        return isCarried;
    }
    
    // Public method to force drop the object (useful for external scripts)
    public void ForceDrop()
    {
        if (isCarried)
        {
            DropObject();
        }
    }
    
    // Method to reset object to original position (useful for respawning)
    public void ResetToOriginalPosition()
    {
        if (isCarried)
        {
            DropObject();
        }
        
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.SetParent(originalParent);
        
        if (rb != null)
        {
            rb.simulated = true;
        }
        if (objectCollider != null)
        {
            objectCollider.enabled = true;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        // Draw carry offset if object is being carried
        if (isCarried && player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(player.transform.position + carryOffset, 0.2f);
        }
    }
}
