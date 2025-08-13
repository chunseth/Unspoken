using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackDuration = 0.1f;
    public float attackCooldown = 0.3f;
    public int attackDamage = 1;
    public float attackRange = 1f; // Length of the attack rectangle

    [Header("Sweep Attack Settings")]
    public float sweepAngle = 180f; // Fixed sweep angle for all attacks

    [Header("Dash Attack Settings")]
    public float minDashDistance = 2f;
    public float maxDashDistance = 6f;
    public float dashDuration = 0.3f;
    public float minSwipeDistance = 50f; // Minimum swipe distance to trigger dash (in screen pixels)
    public float dashCooldown = 1f;

    [Header("References")]
    public Transform attackPivot;
    public Sprite swordSprite;


    
    [Header("Environment")]
    public LayerMask obstacleLayer; // Add this to detect walls

    private bool isAttacking = false;
    private bool isHoldingAttack = false;
    private float attackHoldStartTime = 0f;
    private float currentHoldDuration = 0f;
    private float lastAttackTime = -Mathf.Infinity;
    private PlayerController playerController;
    private float currentAttackTime = 0f;
    private Quaternion startRotation;
    private Quaternion targetRotation;
    private SpriteRenderer attackSprite;

    private bool isDashing = false;
    private Vector2 pointerStartPosition;
    private bool isSwipeDetected = false;
    private float lastDashTime = -Mathf.Infinity;
    private float dashDistance = 0f;
    private Vector2 dashDirection;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }
    
    private void Start()
    {
        // Setup the sword sprite - do this in Start to ensure PlayerController has created the pivot
        SetupAttackPivot();
    }
    
    private void SetupAttackPivot()
    {
        // If attackPivot is not assigned, try to find it on the player
        if (attackPivot == null)
        {
            Transform foundPivot = transform.Find("AttackPivot");
            if (foundPivot != null)
            {
                attackPivot = foundPivot;
            }
            else
            {
                Debug.LogWarning("Attack pivot not found! PlayerAttack will not work properly.");
                return;
            }
        }
        
        // Setup the sword sprite
        attackSprite = attackPivot.gameObject.AddComponent<SpriteRenderer>();
        attackSprite.sprite = swordSprite;
        attackSprite.sortingOrder = 1;
        
        attackPivot.localPosition = Vector3.zero;
        attackPivot.localRotation = Quaternion.identity;
        attackPivot.localScale = new Vector3(attackRange, 1f, 1f);

        // Hide the sword initially
        attackSprite.enabled = false;
    }

    private void Update()
    {
        // Handle mouse input
        HandleMouseInput();
        
        if (isAttacking && !isDashing)
        {
            // Update regular attack animation
            currentAttackTime += Time.deltaTime;
            float progress = currentAttackTime / attackDuration;
            
            if (progress >= 1f)
            {
                // End attack
                EndAttack();
            }
            else
            {
                // Interpolate rotation
                attackPivot.rotation = Quaternion.Lerp(startRotation, targetRotation, progress);
            }
        }
        
        if (isDashing)
        {
            // Update dash movement
            float dashStep = dashDistance / dashDuration * Time.deltaTime;
            transform.position += new Vector3(dashDirection.x * dashStep, dashDirection.y * dashStep, 0f);
            
            // Update sweep animation during dash
            currentAttackTime += Time.deltaTime;
            float progress = currentAttackTime / dashDuration;
            
            if (progress >= 1f)
            {
                EndDash();
                EndAttack(); // End both dash and attack together
            }
            else
            {
                // Interpolate rotation during dash
                attackPivot.rotation = Quaternion.Lerp(startRotation, targetRotation, progress);
            }
        }
        
        // Track hold duration while button is being held
        if (isHoldingAttack)
        {
            currentHoldDuration = Time.time - attackHoldStartTime;
        }
    }
    
    private float GetPlayerFacingAngle()
    {
        // Always use the PlayerController's facing direction vector
        Vector2 facingDir = playerController.GetFacingDirectionVector();
        float angle = Mathf.Atan2(facingDir.y, facingDir.x) * Mathf.Rad2Deg;
        
        // Debug: Log the facing direction and initial angle
        PlayerController.FacingDirection facingEnum = playerController.GetFacingDirection();
        Debug.Log($"Facing Direction Vector: {facingDir}, Enum: {facingEnum}, Initial Angle: {angle}°");
        
        // Add 180 degree offset for specific angles (135° and 225°)
        // 135° = SouthEast, 225° = SouthWest
        if (Mathf.Approximately(angle, 135f) || (Mathf.Abs(facingDir.y) > Mathf.Abs(facingDir.x)))
        {
            angle += 180f;
            Debug.Log($"Applied vertical offset. New angle: {angle}°");
        }
        
        // Special fix for down-left direction (225° = SouthWest)
        if (Mathf.Approximately(angle, 45f))
        {
            angle += 1f;
            Debug.Log($"Applied down-left fix. New angle: {angle}°");
        }
        
        Debug.Log($"Final angle: {angle}°");
        return angle;
    }
    


    private void HandleMouseInput()
    {
        // Mouse button down (left click)
        if (Input.GetMouseButtonDown(0))
        {
            OnAttackButtonDown();
        }
        
        // Mouse button held
        if (Input.GetMouseButton(0) && isHoldingAttack)
        {
            OnAttackButtonDrag();
        }
        
        // Mouse button up
        if (Input.GetMouseButtonUp(0))
        {
            OnAttackButtonUp();
        }
    }
    
    private void OnAttackButtonDown()
    {
        // Can't start holding if we're on cooldown, attacking, or dashing
        if (Time.time < lastAttackTime + attackCooldown || isAttacking || isDashing)
        {
            return;
        }
        
        isHoldingAttack = true;
        attackHoldStartTime = Time.time;
        currentHoldDuration = 0f;
        
        // Store initial mouse position for swipe detection
        pointerStartPosition = Input.mousePosition;
        isSwipeDetected = false;
    }

    private void OnAttackButtonDrag()
    {
        if (!isHoldingAttack || isSwipeDetected)
            return;
            
        // Calculate swipe distance
        Vector2 currentPosition = Input.mousePosition;
        Vector2 swipeDirection = currentPosition - pointerStartPosition;
        
        // Check if swipe is mainly upward
        if (swipeDirection.magnitude >= minSwipeDistance && swipeDirection.y > Mathf.Abs(swipeDirection.x))
        {
            isSwipeDetected = true;
            TryDash();
        }
    }

    private void OnAttackButtonUp()
    {
        if (isHoldingAttack)
        {
            isHoldingAttack = false;
            
            // Only perform regular attack if we didn't dash
            if (!isSwipeDetected && !isDashing)
            {
                TryAttack();
            }
        }
    }
    
    private void TryDash()
    {
        if (Time.time < lastDashTime + dashCooldown || isDashing || isAttacking)
        {
            return;
        }
        
        // Calculate dash distance based on hold duration
        float calculatedDashDistance = CalculateDashDistance(currentHoldDuration);
        
        StartDash(calculatedDashDistance, sweepAngle);
    }
    
    private float CalculateDashDistance(float holdDuration)
    {
        // Calculate dash distance based on how long the button was held
        // Use a maximum hold time of 1 second for full charge
        float maxHoldTime = 1.0f;
        float t = Mathf.Clamp01(holdDuration / maxHoldTime);
        return Mathf.Lerp(minDashDistance, maxDashDistance, t);
    }
    

    
    private void StartDash(float distance, float attackSweepAngle)
    {
        // Get current facing direction
        dashDirection = playerController.GetFacingDirectionVector().normalized;
        
        // Check for walls in dash direction
        float actualDashDistance = distance;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dashDirection, distance, obstacleLayer);
        if (hit.collider != null)
        {
            // Limit dash distance to slightly less than the distance to the wall
            actualDashDistance = hit.distance * 0.5f;
            Debug.Log($"Dash limited by wall at distance {hit.distance}, using {actualDashDistance} instead");
        }
        
        // Now use the possibly adjusted distance
        dashDistance = actualDashDistance;
        
        isDashing = true;
        isAttacking = true;
        currentAttackTime = 0f;
        lastDashTime = Time.time;
        lastAttackTime = Time.time;
        
        // Show the sword during dash
        if (attackSprite != null)
        {
            attackSprite.enabled = true;
        }
        
        // Setup attack animation to match dash and include sweep
        // Add 90 degree offset to make sword swing from right to left
        float baseAngle = GetPlayerFacingAngle() + 90f;
        
        // Set start and target rotations for the sweeping dash
        // Ensure the sweep always goes from right to left in front of the player
        startRotation = Quaternion.Euler(0, 0, baseAngle - attackSweepAngle/2f);
        targetRotation = Quaternion.Euler(0, 0, baseAngle + attackSweepAngle/2f);
        
        attackPivot.rotation = startRotation;
        
        // Optionally play dash animation/effect here
    }
    
    private void EndDash()
    {
        isDashing = false;
        
        // Check for hits along the dash path
        CheckDashHits();
    }
    
    private void CheckDashHits()
    {
        // Cast a ray or use a box/circle collider to check for enemies hit during dash
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            transform.position - (Vector3)(dashDirection * dashDistance * 0.5f), 
            new Vector2(0.5f, 0.5f), // Size of the box
            0f, // No rotation
            dashDirection,
            dashDistance
        );
        
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // Optional: Make dash attacks deal more damage
                    damageable.TakeDamage(attackDamage * 2);
                }
            }
        }
    }

    public void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown || isAttacking)
        {
            return;
        }
        
        StartAttack(sweepAngle);
    }

    private void StartAttack(float attackSweepAngle)
    {
        isAttacking = true;
        currentAttackTime = 0f;
        lastAttackTime = Time.time;

        // Show the sword at the start of attack
        if (attackSprite != null)
        {
            attackSprite.enabled = true;
        }

        // Get the actual movement direction angle from PlayerController
        // Add 90 degree offset to make sword swing from right to left
        float baseAngle = GetPlayerFacingAngle() + 90f;
        
        // Ensure the sweep always goes from right to left in front of the player
        startRotation = Quaternion.Euler(0, 0, baseAngle - attackSweepAngle/2f);
        targetRotation = Quaternion.Euler(0, 0, baseAngle + attackSweepAngle/2f);
        
        attackPivot.rotation = startRotation;
        
        CheckHits();
    }

    private void EndAttack()
    {
        isAttacking = false;
        
        // Hide the sword at the end of attack
        if (attackSprite != null)
        {
            attackSprite.enabled = false;
        }
        
        // Only check for hits if this was a regular attack, not a dash
        if (!isDashing)
        {
            CheckHits();
        }
    }

    private void CheckHits()
    {
        // Create an arc of raycasts to check for enemies
        Vector2 origin = transform.position;
        float currentAngle = attackPivot.rotation.eulerAngles.z;
        
        // Cast multiple rays in an arc
        int rayCount = 5;
        float angleStep = sweepAngle / (rayCount - 1);
        float startAngle = currentAngle - sweepAngle/2f;
        
        for (int i = 0; i < rayCount; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, attackRange);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.gameObject != gameObject)
                {
                    IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(attackDamage);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns whether an attack is currently in progress
    /// </summary>
    public bool IsAttacking()
    {
        return isAttacking || isDashing;
    }
    
    /// <summary>
    /// Interrupts any current attack action (regular attack, dash, or charge)
    /// Called when player takes damage or other interrupting events
    /// </summary>
    public void InterruptAttack()
    {
        // Stop any dash in progress
        if (isDashing)
        {
            isDashing = false;
            // Optionally, show small recovery animation or effect here
        }
        
        // Stop any attack in progress
        if (isAttacking)
        {
            isAttacking = false;
            
            // Hide attack visuals
            if (attackSprite != null)
            {
                attackSprite.enabled = false;
            }
        }
        
        // Cancel any attack charging
        if (isHoldingAttack)
        {
            isHoldingAttack = false;
            currentHoldDuration = 0f;
        }
        
        // Reset attack time variables
        currentAttackTime = 0f;
        isSwipeDetected = false;
        
        // Optional: add a small cooldown after being interrupted
        // lastAttackTime = Time.time;
    }

    private void OnDrawGizmos()
    {
        if (isAttacking)
        {
            // Draw attack arc in editor for debugging
            Vector3 origin = transform.position;
            float currentAngle = attackPivot.rotation.eulerAngles.z;
            Vector2 direction = new Vector2(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                Mathf.Sin(currentAngle * Mathf.Deg2Rad)
            );
            
            Gizmos.color = Color.red;
            Gizmos.DrawRay(origin, direction * attackRange);
        }
        
        if (isDashing)
        {
            // Draw dash path in editor for debugging
            Gizmos.color = Color.blue;
            Vector3 dashStart = transform.position - (Vector3)(dashDirection * dashDistance);
            Gizmos.DrawLine(dashStart, transform.position);
        }
    }
} 