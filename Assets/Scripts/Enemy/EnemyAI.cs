using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public enum EnemyState
{
    Idle,           // Not active, no movement
    Pursuit,        // Actively pursuing player
    Shooting        // In range and shooting
}

public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRange = 10f;
    public float shootingRange = 5f;
    public LayerMask obstacleLayer;
    public float corridorCheckDistance = 1f;

    [Header("Movement")]
    public float moveSpeed = 5f;
    
    [Header("Combat")]
    public float attackRate = 1f;
    
    [Header("Pathfinding")]
    public float nodeSpacing = 1f;
    public float pathUpdateRate = 0.5f;
    public float collisionRadius = 0.5f;

    // State and movement properties
    [HideInInspector] public EnemyState currentState = EnemyState.Idle;
    private float baseMoveSpeed;
    private float currentMoveSpeed;
    private float movementMultiplier = 1.0f;

    // Private variables
    private Transform player;
    private Rigidbody2D rb;
    private Vector2 targetPosition;
    private List<Vector2> currentPath = new List<Vector2>();
    private float nextPathUpdate;
    private float nextAttackTime;
    private Vector2 lastKnownPlayerPos;
    private bool isInCorridor = false;
    private EnemyShooter shooter;

    // Add these fields to the class for stun management
    private bool isStunned = false;
    private float stunEndTime = 0f;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        shooter = GetComponent<EnemyShooter>();
        
        if (player == null)
            return;
            
        // Store the base move speed for reference
        baseMoveSpeed = moveSpeed;
        currentMoveSpeed = baseMoveSpeed;
            
        SetState(EnemyState.Idle);
    }

    private void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Update corridor status
        isInCorridor = CheckIfInCorridor();

        // State machine
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdleState(distanceToPlayer);
                break;

            case EnemyState.Pursuit:
                HandlePursuitState(distanceToPlayer);
                break;

            case EnemyState.Shooting:
                HandleShootingState(distanceToPlayer);
                break;
        }
    }

    /// <summary>
    /// Sets a multiplier for the enemy's movement speed (for status effects)
    /// </summary>
    /// <param name="multiplier">Value between 0-1 where 0 is stopped and 1 is normal speed</param>
    public void SetMovementMultiplier(float multiplier)
    {
        movementMultiplier = Mathf.Clamp01(multiplier);
        currentMoveSpeed = baseMoveSpeed * movementMultiplier;
        
        // If completely immobilized, stop current movement
        if (Mathf.Approximately(movementMultiplier, 0f))
        {
            rb.velocity = Vector2.zero;
        }
        

    }

    /// <summary>
    /// Gets the current movement multiplier
    /// </summary>
    public float GetMovementMultiplier()
    {
        return movementMultiplier;
    }

    private void HandleIdleState(float distanceToPlayer)
    {
        if (distanceToPlayer <= detectionRange)
        {
            SetState(EnemyState.Pursuit);
        }
    }

    private void HandlePursuitState(float distanceToPlayer)
    {
        if (distanceToPlayer > detectionRange)
        {
            SetState(EnemyState.Idle);
            return;
        }

        if (distanceToPlayer <= shootingRange && HasLineOfSight())
        {
            SetState(EnemyState.Shooting);
            return;
        }

        // Update path based on corridor or open space
        if (Time.time >= nextPathUpdate)
        {
            if (isInCorridor)
            {
                CalculateCorridorPath();
            }
            else
            {
                CalculateDirectPath();
            }
            nextPathUpdate = Time.time + pathUpdateRate;
        }

        // Follow current path - using the current speed which may be modified by effects
        FollowPath();
    }

    private void HandleShootingState(float distanceToPlayer)
    {
        if (distanceToPlayer > shootingRange || !HasLineOfSight())
        {
            SetState(EnemyState.Pursuit);
            return;
        }

        // Maintain optimal shooting distance
        MaintainShootingDistance();
    }



    private bool CheckIfInCorridor()
    {
        // Cast rays in cardinal directions
        int wallCount = 0;
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            
            if (Physics2D.Raycast(transform.position, direction, corridorCheckDistance, obstacleLayer))
            {
                wallCount++;
            }
        }
        
        // If we have walls on two opposite sides, we're in a corridor
        return wallCount >= 2;
    }

    private void CalculateCorridorPath()
    {
        currentPath.Clear();
        
        // Get the grid-based positions
        Vector2Int currentGrid = Vector2Int.RoundToInt((Vector2)transform.position);
        Vector2Int playerGrid = Vector2Int.RoundToInt((Vector2)player.position);
        
        // Calculate horizontal and vertical paths
        List<Vector2> horizontalPath = new List<Vector2>();
        List<Vector2> verticalPath = new List<Vector2>();
        
        // Try horizontal first
        for (int x = currentGrid.x; x != playerGrid.x; x += (playerGrid.x > currentGrid.x ? 1 : -1))
        {
            horizontalPath.Add(new Vector2(x, currentGrid.y));
        }
        for (int y = currentGrid.y; y != playerGrid.y; y += (playerGrid.y > currentGrid.y ? 1 : -1))
        {
            horizontalPath.Add(new Vector2(playerGrid.x, y));
        }
        
        // Try vertical first
        for (int y = currentGrid.y; y != playerGrid.y; y += (playerGrid.y > currentGrid.y ? 1 : -1))
        {
            verticalPath.Add(new Vector2(currentGrid.x, y));
        }
        for (int x = currentGrid.x; x != playerGrid.x; x += (playerGrid.x > currentGrid.x ? 1 : -1))
        {
            verticalPath.Add(new Vector2(x, playerGrid.y));
        }
        
        // Choose the shorter path
        currentPath = horizontalPath.Count < verticalPath.Count ? horizontalPath : verticalPath;
        currentPath.Add(player.position);
    }

    private void CalculateDirectPath()
    {
        currentPath.Clear();
        
        if (HasLineOfSight())
        {
            currentPath.Add(player.position);
        }
        else
        {
            // Simple direct path with obstacle avoidance
            Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, detectionRange, obstacleLayer);
            
            if (hit.collider != null)
            {
                // Try to find path around obstacle
                Vector2 normal = hit.normal;
                Vector2 parallel = new Vector2(-normal.y, normal.x);
                
                // Try both directions parallel to the wall
                Vector2 pos1 = hit.point + parallel * 2f;
                Vector2 pos2 = hit.point - parallel * 2f;
                
                // Choose the position closer to the player
                currentPath.Add(Vector2.Distance(pos1, player.position) < Vector2.Distance(pos2, player.position) ? pos1 : pos2);
                currentPath.Add(player.position);
            }
        }
    }

    private void FollowPath()
    {
        if (currentPath.Count == 0) return;

        // Don't move if being knocked back
        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null && enemyHealth.IsBeingKnockedBack)
        {
            return;
        }

        // Stop movement if multiplier is zero
        if (Mathf.Approximately(movementMultiplier, 0f))
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 direction = (currentPath[0] - (Vector2)transform.position).normalized;
        rb.velocity = direction * currentMoveSpeed; // Use modified speed

        // Remove waypoint if we're close enough
        if (Vector2.Distance(transform.position, currentPath[0]) < 0.1f)
        {
            currentPath.RemoveAt(0);
        }
    }

    private void MaintainShootingDistance()
    {
        // Don't move if being knocked back
        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null && enemyHealth.IsBeingKnockedBack)
        {
            return;
        }

        // Don't adjust position if movement is disabled
        if (Mathf.Approximately(movementMultiplier, 0f))
        {
            rb.velocity = Vector2.zero;
            return;
        }
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        Vector2 directionToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        
        // ONLY move closer if too far, never back away
        if (distanceToPlayer > shootingRange * 1.2f) // Too far
        {
            rb.velocity = directionToPlayer * currentMoveSpeed;
        }
        else
        {
            // If we're in range or too close, just stay put
            rb.velocity = Vector2.zero;
        }
    }

    private bool HasLineOfSight()
    {
        if (player == null) return false;
        
        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position,
            collisionRadius,
            ((Vector2)player.position - (Vector2)transform.position).normalized,
            Vector2.Distance(transform.position, player.position),
            obstacleLayer
        );
        
        return hit.collider == null;
    }

    public void SetState(EnemyState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
        }
    }

    private void OnDrawGizmos()
    {
        // Draw detection ranges
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootingRange);
        
        // Draw current path
        if (currentPath.Count > 0)
        {
            Gizmos.color = Color.green;
            Vector2 previousPoint = transform.position;
            foreach (Vector2 point in currentPath)
            {
                Gizmos.DrawLine(previousPoint, point);
                Gizmos.DrawSphere(point, 0.1f);
                previousPoint = point;
            }
        }
        
        // Draw corridor check rays
        if (isInCorridor)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Gizmos.DrawRay(transform.position, direction * corridorCheckDistance);
            }
        }
        
        // Display current state as text
        #if UNITY_EDITOR
        UnityEditor.Handles.BeginGUI();
        Vector3 screenPos = UnityEditor.HandleUtility.WorldToGUIPoint(transform.position + Vector3.up * 1.2f);
        UnityEditor.Handles.color = Color.white;
        string stateText = currentState.ToString();
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 10;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, stateText, style);
        UnityEditor.Handles.EndGUI();
        #endif
    }

    /// <summary>
    /// Sets the enemy's stunned state
    /// </summary>
    /// <param name="stunned">Whether the enemy is stunned</param>
    /// <param name="duration">Duration of stun in seconds (0 = permanent until explicitly unstunned)</param>
    public void SetStunned(bool stunned, float duration = 0f)
    {
        isStunned = stunned;
        
        if (stunned)
        {
            // Stop all movement immediately
            rb.velocity = Vector2.zero;
            
            // Set the stun end time if a duration was provided
            if (duration > 0f)
            {
                stunEndTime = Time.time + duration;
                StartCoroutine(UnstunAfterDelay(duration));
            }
            

        }
        else
        {
            stunEndTime = 0f;
        }
    }

    /// <summary>
    /// Gets the current stunned state
    /// </summary>
    public bool IsStunned()
    {
        return isStunned;
    }

    /// <summary>
    /// Coroutine to automatically unstun after a delay
    /// </summary>
    private IEnumerator UnstunAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (Time.time >= stunEndTime)
        {
            SetStunned(false);
        }
    }
}