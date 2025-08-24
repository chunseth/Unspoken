using UnityEngine;

public class CrownProjectileDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damage = 2;
    public LayerMask playerLayer;
    
    [Header("Collision Settings")]
    public bool canHitMultipleTimes = false;
    public float hitCooldown = 0.5f;
    
    private bool hasHitPlayer = false;
    private float lastHitTime = 0f;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if we hit the player
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            // Check if we can hit the player (either first hit or multiple hits allowed)
            if (!hasHitPlayer || (canHitMultipleTimes && Time.time - lastHitTime >= hitCooldown))
            {
                PlayerStats playerStats = other.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(damage, transform.position);
                    lastHitTime = Time.time;
                    hasHitPlayer = true;
                    
                    Debug.Log($"Crown projectile hit player for {damage} damage!");
                    
                    // If we can't hit multiple times, destroy the crown
                    if (!canHitMultipleTimes)
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}
