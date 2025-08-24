using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum RoyalSlimeState
{
    Idle,
    PreparingCharge,
    Charging,
    RecoilForCrown,
    ThrowingCrown,
    Stunned
}

public class RoyalSlime : MonoBehaviour
{
    [Header("Boss Settings")]
    public float detectionRange = 15f;
    public float moveSpeed = 3f;
    public float chargeSpeed = 15f;
    public float chargeDistance = 10f; // 10 tiles
    public float crownThrowRange = 8f;
    public float recoilDistance = 1f; // 1 tile
    
    [Header("Attack Timing")]
    public float chargePreparationTime = 1.5f;
    public float chargeDuration = 0.8f;
    public float recoilTime = 0.5f;
    public float crownThrowTime = 0.8f;
    public float attackCooldown = 3f;
    
    [Header("Damage Settings")]
    public int chargeDamage = 3;
    public int crownDamage = 2;
    
    [Header("Projectiles")]
    public GameObject crownPrefab;
    public float crownSpeed = 12f;
    public float crownArcHeight = 3f;
    
    [Header("References")]
    public Transform firePoint;
    public LayerMask obstacleLayer;
    public LayerMask playerLayer;
    
    // State management
    private RoyalSlimeState currentState = RoyalSlimeState.Idle;
    private Transform player;
    private Rigidbody2D rb;
    private Vector3 originalPosition;
    private Vector2 chargeTarget;
    private Vector2 crownThrowTarget;
    private float nextAttackTime;
    private bool isAttacking = false;
    
    // Animation references (to be set up in prefab)
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    
    // Visual feedback
    [Header("Visual Feedback")]
    public Color chargeWarningColor = Color.red;
    public Color crownThrowColor = Color.yellow;
    private Color originalColor;
    
    [Header("Attack Indicators")]
    public GameObject chargeIndicatorPrefab;
    public GameObject crownIndicatorPrefab;
    private GameObject currentIndicator;
    
    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
            
        if (firePoint == null)
            firePoint = transform;
            
        originalPosition = transform.position;
        
        // Ensure Rigidbody2D doesn't interfere with rotation
        if (rb != null)
        {
            rb.freezeRotation = true; // Prevent physics from affecting rotation
        }
        
        // Start a coroutine to monitor position changes
        StartCoroutine(MonitorPosition());
    }
    
    private IEnumerator MonitorPosition()
    {
        Vector3 lastPosition = transform.position;
        
        while (true)
        {
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
            
            if (Vector3.Distance(transform.position, lastPosition) > 0.01f)
            {
                lastPosition = transform.position;
            }
        }
    }
    

    
    private void Update()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // State machine
        switch (currentState)
        {
            case RoyalSlimeState.Idle:
                HandleIdleState(distanceToPlayer);
                break;
                
            case RoyalSlimeState.PreparingCharge:
                HandlePreparingCharge();
                break;
                
            case RoyalSlimeState.Charging:
                HandleCharging();
                break;
                
            case RoyalSlimeState.RecoilForCrown:
                HandleRecoilForCrown();
                break;
                
            case RoyalSlimeState.ThrowingCrown:
                HandleThrowingCrown();
                break;
                
            case RoyalSlimeState.Stunned:
                HandleStunned();
                break;
        }
    }
    
    private IEnumerator AnimateRotation(float targetZRotation, float duration)
    {
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetZRotation);
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            // Use smooth step for more natural animation
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, smoothProgress);
            
            yield return null;
        }
        
        // Ensure we reach the exact target rotation
        transform.rotation = targetRotation;
    }
    
    private void LateUpdate()
    {
        // If we're in Idle state and not attacking, ensure we stay at the correct Z position
        if (currentState == RoyalSlimeState.Idle && !isAttacking)
        {
            Vector3 currentPos = transform.position;
            if (Mathf.Abs(currentPos.z - originalPosition.z) > 0.01f)
            {
                transform.position = new Vector3(currentPos.x, currentPos.y, originalPosition.z);
            }
        }
    }
    
    private void HandleIdleState(float distanceToPlayer)
    {
        if (distanceToPlayer <= detectionRange && Time.time >= nextAttackTime && !isAttacking)
        {
            // Choose attack based on distance and positioning
            if (distanceToPlayer < 5f)
            {
                // Too close - charge attack
                StartChargeAttack();
            }
            else if (distanceToPlayer >= 5f && distanceToPlayer <= 7f)
            {
                // Perfect range for crown throw
                StartCrownThrow();
            }
            else if (distanceToPlayer > 7f)
            {
                // Too far - move closer first
                MoveTowardsPlayer();
            }
        }
        else if (distanceToPlayer <= detectionRange && !isAttacking)
        {
            // Move towards player when not attacking, but only if too far away
            if (distanceToPlayer > 7f)
            {
                MoveTowardsPlayer();
            }
            else
            {
                // Stop moving if we're in the right range
                rb.velocity = Vector2.zero;
            }
        }
    }
    
    private void HandlePreparingCharge()
    {
        // This state is handled by coroutine, just maintain position
        rb.velocity = Vector2.zero;
    }
    
    private void HandleCharging()
    {
        // This state is handled by coroutine, movement is controlled there
    }
    
    private void HandleRecoilForCrown()
    {
        // This state is handled by coroutine, just maintain recoil position
        rb.velocity = Vector2.zero;
    }
    
    private void HandleThrowingCrown()
    {
        // This state is handled by coroutine, just maintain position
        rb.velocity = Vector2.zero;
    }
    
    private void HandleStunned()
    {
        rb.velocity = Vector2.zero;
    }
    
    private void MoveTowardsPlayer()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Only move if we're too far away (> 7 tiles)
        if (distanceToPlayer > 7f)
        {
            Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
            rb.velocity = direction * moveSpeed;
            
            // Update sprite direction
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
        }
        else
        {
            // Stop moving if we're close enough
            rb.velocity = Vector2.zero;
        }
    }
    
    private void StartChargeAttack()
    {
        if (isAttacking) return;
        
        isAttacking = true;
        currentState = RoyalSlimeState.PreparingCharge;
        
        // Calculate charge target (10 tiles in direction of player)
        Vector2 directionToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        chargeTarget = (Vector2)transform.position + (directionToPlayer * chargeDistance);
        
        // Start charge preparation
        StartCoroutine(ChargeAttackSequence());
    }
    
    private void StartCrownThrow()
    {
        if (isAttacking) return;
        
        isAttacking = true;
        currentState = RoyalSlimeState.RecoilForCrown;
        
        // Store player position for crown throw target
        crownThrowTarget = player.position;
        
        // Start crown throw sequence
        StartCoroutine(CrownThrowSequence());
    }
    
    private IEnumerator ChargeAttackSequence()
    {
        // Show charge indicator
        ShowChargeIndicator();
        
        // Phase 1: Preparation (jump to indicate charge)
        if (animator != null)
            animator.SetTrigger("ChargePrepare");
        
        // Visual warning
        if (spriteRenderer != null)
            spriteRenderer.color = chargeWarningColor;
        
        yield return new WaitForSeconds(chargePreparationTime);
        
        // Phase 2: Charge forward
        currentState = RoyalSlimeState.Charging;
        
        if (animator != null)
            animator.SetTrigger("Charge");
        
        // Restore color
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        
        // Charge movement
        Vector2 direction = (chargeTarget - (Vector2)transform.position).normalized;
        float chargeStartTime = Time.time;
        bool hasHitPlayer = false;
        
        while (Time.time - chargeStartTime < chargeDuration)
        {
            rb.velocity = direction * chargeSpeed;
            
            // Check for collision with walls or player
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 1f, obstacleLayer);
            if (hit.collider != null)
            {
                // Hit a wall, stop charging
                break;
            }
            
            // Check for player collision during charge
            if (!hasHitPlayer && player != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, player.position);
                if (distanceToPlayer < 1.5f) // Charge hitbox
                {
                    PlayerStats playerStats = player.GetComponent<PlayerStats>();
                    if (playerStats != null)
                    {
                        playerStats.TakeDamage(chargeDamage, transform.position);
                        hasHitPlayer = true;
                    }
                }
            }
            
            yield return null;
        }
        
        // End charge
        rb.velocity = Vector2.zero;
        
        // Animate rotation back to normal
        StartCoroutine(AnimateRotation(0f, 0.3f));
        
        // Hide indicator
        HideIndicator();
        
        currentState = RoyalSlimeState.Idle;
        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
    }
    
    private IEnumerator CrownThrowSequence()
    {
        // Show crown indicator
        ShowCrownIndicator();
        
        // Phase 1: Recoil back 1 tile
        Vector2 recoilDirection = ((Vector2)transform.position - crownThrowTarget).normalized;
        Vector2 recoilTarget = (Vector2)transform.position + (recoilDirection * recoilDistance);
        
        if (animator != null)
            animator.SetTrigger("CrownRecoil");
        
        // Animate rotation to -15 degrees Z for recoil preparation
        StartCoroutine(AnimateRotation(-15f, recoilTime * 0.5f));
        
        // Visual feedback
        if (spriteRenderer != null)
            spriteRenderer.color = crownThrowColor;
        
        // Recoil movement
        float recoilStartTime = Time.time;
        while (Time.time - recoilStartTime < recoilTime)
        {
            Vector2 direction = (recoilTarget - (Vector2)transform.position).normalized;
            rb.velocity = direction * (recoilDistance / recoilTime);
            yield return null;
        }
        
        // Ensure we're at the recoil position (preserve Z position)
        Vector3 recoilPosition3D = new Vector3(recoilTarget.x, recoilTarget.y, transform.position.z);
        transform.position = recoilPosition3D;
        rb.velocity = Vector2.zero;
        
        // Phase 2: Throw crown
        currentState = RoyalSlimeState.ThrowingCrown;
        
        if (animator != null)
            animator.SetTrigger("CrownThrow");
        
        // Animate rotation to 20 degrees Z for throwing motion
        StartCoroutine(AnimateRotation(20f, crownThrowTime * 0.3f));
        
        yield return new WaitForSeconds(crownThrowTime * 0.5f); // Half time for windup
        
        // Spawn and throw crown
        if (crownPrefab != null)
        {
            GameObject crown = Instantiate(crownPrefab, firePoint.position, Quaternion.identity);
            SetupCrownProjectile(crown);
            StartCoroutine(ThrowCrownInArc(crown, crownThrowTarget));
        }
        
        yield return new WaitForSeconds(crownThrowTime * 0.5f); // Half time for follow-through
        
        // End crown throw
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        
        // Animate rotation back to normal
        StartCoroutine(AnimateRotation(0f, 0.3f));
        
        // Hide indicator
        HideIndicator();
        
        currentState = RoyalSlimeState.Idle;
        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
    }
    
    private IEnumerator ThrowCrownInArc(GameObject crown, Vector2 target)
    {
        Vector2 startPos = crown.transform.position;
        Vector2 direction = (target - startPos).normalized;
        float distance = Vector2.Distance(startPos, target);
        float flightTime = distance / crownSpeed;
        
        float elapsedTime = 0f;
        
        // Phase 1: Throw crown to target
        while (elapsedTime < flightTime)
        {
            float progress = elapsedTime / flightTime;
            
            // Calculate position along arc
            Vector2 linearPos = Vector2.Lerp(startPos, target, progress);
            float arcHeight = Mathf.Sin(progress * Mathf.PI) * crownArcHeight;
            Vector2 arcPos = linearPos + Vector2.up * arcHeight;
            
            crown.transform.position = arcPos;
            
            // Rotate crown as it flies
            crown.transform.Rotate(0, 0, 360f * Time.deltaTime);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure crown reaches target
        crown.transform.position = target;
        
        // Check if player is at the landing location and damage them
        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(target, player.position);
            if (distanceToPlayer < 1.5f) // Crown explosion radius
            {
                PlayerStats playerStats = player.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(crownDamage, target);
                }
            }
        }
        
        // Brief pause at landing location
        yield return new WaitForSeconds(0.3f);
        
        // Phase 2: Return crown to boss
        Vector2 returnStartPos = crown.transform.position;
        Vector2 returnTarget = transform.position;
        float returnDistance = Vector2.Distance(returnStartPos, returnTarget);
        float returnFlightTime = returnDistance / crownSpeed;
        
        elapsedTime = 0f;
        
        while (elapsedTime < returnFlightTime)
        {
            float progress = elapsedTime / returnFlightTime;
            
            // Calculate position along return arc
            Vector2 linearPos = Vector2.Lerp(returnStartPos, returnTarget, progress);
            float arcHeight = Mathf.Sin(progress * Mathf.PI) * crownArcHeight * 0.5f; // Smaller arc for return
            Vector2 arcPos = linearPos + Vector2.up * arcHeight;
            
            crown.transform.position = arcPos;
            
            // Rotate crown as it returns
            crown.transform.Rotate(0, 0, 360f * Time.deltaTime);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure crown reaches boss
        crown.transform.position = returnTarget;
        
        // Destroy crown after returning
        Destroy(crown, 0.1f);
    }
    
    private void SetupCrownProjectile(GameObject crown)
    {
        // Add a trigger collider to the crown for collision detection
        CircleCollider2D crownCollider = crown.GetComponent<CircleCollider2D>();
        if (crownCollider == null)
        {
            crownCollider = crown.AddComponent<CircleCollider2D>();
        }
        crownCollider.isTrigger = true;
        crownCollider.radius = 0.15f; // 0.3 x 0.3 crown size (radius = 0.15)
        
        // Add a script to handle collision with player
        CrownProjectileDamage crownDamageScript = crown.GetComponent<CrownProjectileDamage>();
        if (crownDamageScript == null)
        {
            crownDamageScript = crown.AddComponent<CrownProjectileDamage>();
        }
        crownDamageScript.damage = crownDamage;
        crownDamageScript.playerLayer = playerLayer;
    }
    
    private void ShowChargeIndicator()
    {
        if (chargeIndicatorPrefab != null && currentIndicator == null)
        {
            currentIndicator = Instantiate(chargeIndicatorPrefab, chargeTarget, Quaternion.identity);
        }
    }
    
    private void ShowCrownIndicator()
    {
        if (crownIndicatorPrefab != null && currentIndicator == null)
        {
            currentIndicator = Instantiate(crownIndicatorPrefab, crownThrowTarget, Quaternion.identity);
        }
    }
    
    private void HideIndicator()
    {
        if (currentIndicator != null)
        {
            Destroy(currentIndicator);
            currentIndicator = null;
        }
    }
    
    public void SetStunned(bool stunned, float duration = 0f)
    {
        if (stunned)
        {
            currentState = RoyalSlimeState.Stunned;
            rb.velocity = Vector2.zero;
            
            if (duration > 0f)
                StartCoroutine(UnstunAfterDelay(duration));
        }
        else
        {
            currentState = RoyalSlimeState.Idle;
        }
    }
    
    private IEnumerator UnstunAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentState == RoyalSlimeState.Stunned)
        {
            currentState = RoyalSlimeState.Idle;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw crown throw range
        Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
        Gizmos.DrawWireSphere(transform.position, crownThrowRange);
        
        // Draw charge target if charging
        if (currentState == RoyalSlimeState.PreparingCharge || currentState == RoyalSlimeState.Charging)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, chargeTarget);
            Gizmos.DrawWireSphere(chargeTarget, 0.5f);
        }
        
        // Draw crown throw target if recoiling
        if (currentState == RoyalSlimeState.RecoilForCrown || currentState == RoyalSlimeState.ThrowingCrown)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(crownThrowTarget, 0.3f);
        }
    }
}
