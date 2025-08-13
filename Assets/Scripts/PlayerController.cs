using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float sprintSpeed = 8f; // Speed when sprinting
    public float depletedSpeed = 3f; // Speed when stamina is depleted
    public float sprintStaminaCost = 20f; // Stamina cost per second while sprinting
    public float staminaRegenRate = 15f; // Stamina regeneration per second when not sprinting

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaBarWidth = 80f; // Width of the stamina bar
    public float staminaBarHeight = 20f; // Height of the stamina bar
    public Color staminaFullColor = Color.green;
    public Color staminaLowColor = Color.red;
    public float staminaLowThreshold = 0.3f; // 10% threshold for color change

    [Header("Stamina Bar Sprite")]
    public SpriteRenderer staminaBarSprite; // Sprite that shrinks to show stamina

    [Header("Attack Reference")]
    public Transform attackPivot; // Empty GameObject child that will hold the attack sprite
    
    [Header("Direction Sprites")]
    public SpriteRenderer playerSprite; // Reference to the player's sprite
    public Sprite spriteNorth;
    public Sprite spriteNorthEast;
    public Sprite spriteEast;
    public Sprite spriteSouthEast;
    public Sprite spriteSouth;
    public Sprite spriteSouthWest;
    public Sprite spriteWest;
    public Sprite spriteNorthWest;
    
    [Header("Abilities")]
    public PathCreatorBeam beamAbility; // Reference to the beam ability
    
    // Enum to track player direction
    public enum FacingDirection
    {
        North,      // 0 degrees
        NorthEast,  // 45 degrees
        East,       // 90 degrees
        SouthEast,  // 135 degrees
        South,      // 180 degrees
        SouthWest,  // 225 degrees
        West,       // 270 degrees
        NorthWest   // 315 degrees
    }
    
    private FacingDirection currentDirection = FacingDirection.South; // Default direction
    private Rigidbody2D rb;
    private Vector2 lastMoveDirection = Vector2.down; // Default facing down
    private float speedModifier = 1f; // Default speed modifier (no modification)
    private bool isSprinting = false;
    private bool staminaDepleted = false; // Track if stamina was completely depleted
    private bool wasRecharging = false; // Track if stamina was recharging
    private float targetStaminaBarScale;
    private float currentStaminaBarScale;
    private Vector3 originalStaminaBarScale;
    private float fullStaminaShowTimer = 0f; // Timer for showing full stamina bar
    private const float FULL_STAMINA_SHOW_DURATION = 0.1f; // Duration to show full stamina bar

    private void Awake()
    {
        // Grab reference to the Rigidbody2D
        rb = GetComponent<Rigidbody2D>();

        // Make sure the rigidbody doesn't rotate if it's top-down (optional)
        rb.freezeRotation = true;

        // Initialize stamina
        currentStamina = maxStamina;
        targetStaminaBarScale = 1f;
        currentStaminaBarScale = 1f;

        // Validate references
        if (attackPivot == null)
        {
            Debug.LogWarning("Attack pivot not assigned! Creating one...");
            GameObject pivot = new GameObject("AttackPivot");
            pivot.transform.SetParent(transform);
            pivot.transform.localPosition = Vector3.zero;
            attackPivot = pivot.transform;
        }
        
        // Ensure proper rendering settings
        playerSprite.sortingOrder = 10; // High value to render on top
        
        // If your game uses sorting layers, ensure it's on the right layer
        playerSprite.sortingLayerName = "Player";
        
        // Auto-find beam ability if not assigned
        if (beamAbility == null)
        {
            beamAbility = GetComponent<PathCreatorBeam>();
            if (beamAbility == null)
            {
                Debug.LogWarning("PathCreatorBeam not found on player! Please assign it in the inspector.");
            }
        }

        // Setup stamina bar if not assigned
        SetupStaminaBar();
    }

    private void SetupStaminaBar()
    {
        // Validate stamina bar sprite reference
        if (staminaBarSprite == null)
        {
            Debug.LogWarning("Stamina bar sprite not assigned! Please assign a SpriteRenderer in the inspector to position above the player's head.");
            return;
        }

        // Store original scale for reference
        originalStaminaBarScale = staminaBarSprite.transform.localScale;
        
        // Initialize the stamina bar with current values
        UpdateStaminaBarVisual();
        
        Debug.Log("Stamina bar sprite setup complete!");
    }

    private void Update()
    {
        HandleMovement();
        HandleSprintInput();
        UpdateStamina();
        UpdateFacingDirection();
        HandleAbilityInput();
        UpdateStaminaBarVisual();
        UpdateStaminaBarPosition();
    }

    private void HandleSprintInput()
    {
        // Check for Shift key to sprint
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            // Only allow sprinting if stamina is above 0 and hasn't been depleted
            if (currentStamina > 0 && !staminaDepleted)
            {
                isSprinting = true;
            }
            else
            {
                isSprinting = false;
            }
        }
        else
        {
            isSprinting = false;
        }
    }

    private void UpdateStamina()
    {
        if (isSprinting)
        {
            // Consume stamina while sprinting
            currentStamina -= sprintStaminaCost * Time.deltaTime;
            currentStamina = Mathf.Max(0, currentStamina);
            
            // Check if stamina was depleted
            if (currentStamina <= 0)
            {
                staminaDepleted = true;
                isSprinting = false; // Force stop sprinting
            }
        }
        else
        {
            // Regenerate stamina when not sprinting
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(maxStamina, currentStamina);
            
            // Track if we were recharging and just reached full stamina
            if (currentStamina >= maxStamina && wasRecharging)
            {
                fullStaminaShowTimer = FULL_STAMINA_SHOW_DURATION;
                wasRecharging = false;
            }
            
            // Reset depleted flag when stamina is fully restored
            if (currentStamina >= maxStamina)
            {
                staminaDepleted = false;
            }
            
            // Mark as recharging if stamina is not full
            if (currentStamina < maxStamina)
            {
                wasRecharging = true;
            }
        }

        // Update target scale for smooth lerping
        targetStaminaBarScale = currentStamina / maxStamina;
    }

    private void UpdateStaminaBarVisual()
    {
        if (staminaBarSprite != null)
        {
            // Update the timer
            if (fullStaminaShowTimer > 0)
            {
                fullStaminaShowTimer -= Time.deltaTime;
            }
            
            // Check if stamina is at 100% to hide/show the bar
            float staminaPercentage = currentStamina / maxStamina;
            bool shouldShowBar = staminaPercentage < 1f || staminaDepleted || fullStaminaShowTimer > 0;
            
            // Hide/show the stamina bar
            staminaBarSprite.enabled = shouldShowBar;
            
            // Only update visual properties if the bar is visible
            if (shouldShowBar)
            {
                // Lerp the scale for smooth animation
                currentStaminaBarScale = Mathf.Lerp(currentStaminaBarScale, targetStaminaBarScale, Time.deltaTime * 10f);
                
                // Update the sprite scale (only X axis to shrink horizontally)
                Vector3 newScale = originalStaminaBarScale;
                newScale.x *= currentStaminaBarScale;
                staminaBarSprite.transform.localScale = newScale;

                // Update color based on stamina level and depleted state
                if (staminaDepleted || staminaPercentage <= staminaLowThreshold)
                {
                    staminaBarSprite.color = staminaLowColor;
                }
                else
                {
                    staminaBarSprite.color = staminaFullColor;
                }
            }
        }
    }

    private void UpdateStaminaBarPosition()
    {
        if (staminaBarSprite != null)
        {
            // Position the stamina bar above the player's head in world space
            Vector3 position = transform.position;
            position.y += 0.55f; // Adjust this value to position above the player's head
            staminaBarSprite.transform.position = position;
        }
    }

    private void HandleAbilityInput()
    {
        // Check for Q key press to activate beam ability
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (beamAbility != null)
            {
                beamAbility.ActivateAbility();
            }
            else
            {
                Debug.LogWarning("Beam ability not assigned to player!");
            }
        }
    }

    private void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");
        Vector2 movement = new Vector2(moveX, moveY);

        // Only update lastMoveDirection and sprite if we're actually moving
        if (movement.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = movement.normalized;
            UpdateDirectionAndSprite(lastMoveDirection);
        }

        // Apply speed modifier and appropriate speed to movement
        float currentSpeed;
        if (staminaDepleted)
        {
            currentSpeed = depletedSpeed;
        }
        else if (isSprinting)
        {
            currentSpeed = sprintSpeed;
        }
        else
        {
            currentSpeed = speed;
        }
        
        rb.velocity = movement * currentSpeed * speedModifier;
    }

    private void UpdateDirectionAndSprite(Vector2 direction)
    {
        // Calculate angle in degrees (0 is right, 90 is up)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Convert to positive angle (0-360)
        if (angle < 0) angle += 360f;
        
        // Get facing direction without rotating the sprite
        FacingDirection newDirection;
        
        // Determine which of the 8 directions we're facing
        if (angle >= 337.5f || angle < 22.5f)
        {
            newDirection = FacingDirection.East;
        }
        else if (angle >= 22.5f && angle < 67.5f)
        {
            newDirection = FacingDirection.NorthEast;
        }
        else if (angle >= 67.5f && angle < 112.5f)
        {
            newDirection = FacingDirection.North;
        }
        else if (angle >= 112.5f && angle < 157.5f)
        {
            newDirection = FacingDirection.NorthWest;
        }
        else if (angle >= 157.5f && angle < 202.5f)
        {
            newDirection = FacingDirection.West;
        }
        else if (angle >= 202.5f && angle < 247.5f)
        {
            newDirection = FacingDirection.SouthWest;
        }
        else if (angle >= 247.5f && angle < 292.5f)
        {
            newDirection = FacingDirection.South;
        }
        else // angle >= 292.5f && angle < 337.5f
        {
            newDirection = FacingDirection.SouthEast;
        }
        
        // Only update sprite if the direction changed
        if (newDirection != currentDirection)
        {
            currentDirection = newDirection;
            
            // Set the appropriate sprite based on direction
            switch (currentDirection)
            {
                case FacingDirection.North:
                    SetSprite(spriteNorth);
                    break;
                case FacingDirection.NorthEast:
                    SetSprite(spriteNorthEast);
                    break;
                case FacingDirection.East:
                    SetSprite(spriteEast);
                    break;
                case FacingDirection.SouthEast:
                    SetSprite(spriteSouthEast);
                    break;
                case FacingDirection.South:
                    SetSprite(spriteSouth);
                    break;
                case FacingDirection.SouthWest:
                    SetSprite(spriteSouthWest);
                    break;
                case FacingDirection.West:
                    SetSprite(spriteWest);
                    break;
                case FacingDirection.NorthWest:
                    SetSprite(spriteNorthWest);
                    break;
            }
        }
        
        // Update attack pivot rotation based on exact direction
        // Only update if no attack is in progress
        if (attackPivot != null && !IsAttackInProgress())
        {
            attackPivot.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    private void SetSprite(Sprite newSprite)
    {
        if (playerSprite != null && newSprite != null)
        {
            playerSprite.sprite = newSprite;
        }
    }

    private void UpdateFacingDirection()
    {
        // This function is now handled by UpdateDirectionAndSprite
        // Keeping it empty to avoid breaking any external calls
    }

    // Public getter for current direction (useful for other scripts)
    public FacingDirection GetFacingDirection()
    {
        return currentDirection;
    }

    // Add this new method to convert direction enum to Vector2
    public Vector2 GetFacingDirectionVector()
    {
        switch (currentDirection)
        {
            case FacingDirection.North:
                return Vector2.up;
            case FacingDirection.NorthEast:
                return new Vector2(0.7071f, 0.7071f); // Normalized (1,1)
            case FacingDirection.East:
                return Vector2.right;
            case FacingDirection.SouthEast:
                return new Vector2(0.7071f, -0.7071f); // Normalized (1,-1)
            case FacingDirection.South:
                return Vector2.down;
            case FacingDirection.SouthWest:
                return new Vector2(-0.7071f, -0.7071f); // Normalized (-1,-1)
            case FacingDirection.West:
                return Vector2.left;
            case FacingDirection.NorthWest:
                return new Vector2(-0.7071f, 0.7071f); // Normalized (-1,1)
            default:
                return Vector2.down; // Default fallback
        }
    }

    // Method to set the speed modifier (for dodge, power-ups, debuffs, etc.)
    public void SetSpeedModifier(float modifier)
    {
        speedModifier = modifier;
        Debug.Log($"Player speed modifier set to: {modifier}");
    }

    // Method to get the current speed modifier
    public float GetSpeedModifier()
    {
        return speedModifier;
    }
    
    // Method to check if an attack is in progress
    private bool IsAttackInProgress()
    {
        PlayerAttack playerAttack = GetComponent<PlayerAttack>();
        if (playerAttack != null)
        {
            return playerAttack.IsAttacking();
        }
        return false;
    }
}
