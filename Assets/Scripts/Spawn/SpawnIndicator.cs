using UnityEngine;
using System.Collections;

/// <summary>
/// Shows a spawn indicator with a trail before an enemy spawns.
/// Can be used by any enemy spawning system to provide visual feedback.
/// </summary>
public class SpawnIndicator : MonoBehaviour
{
    [Header("Spawn Indicator Settings")]
    [Tooltip("The icon to show at the spawn location")]
    public GameObject spawnIconPrefab;
    [Tooltip("Duration to show the indicator before spawning")]
    public float indicatorDuration = 0.5f;
    [Tooltip("How long the trail should be")]
    public float trailDuration = 1f;
    [Tooltip("Number of trail segments")]
    public int trailSegments = 10;
    [Tooltip("Color of the spawn indicator")]
    public Color indicatorColor = Color.red;
    [Tooltip("Color of the trail")]
    public Color trailColor = new Color(1f, 0f, 0f, 0.5f);
    
    [Header("Movement Settings")]
    [Tooltip("Speed of the indicator movement from source to spawn point")]
    public float travelSpeed = 5f;
    [Tooltip("Whether to use travel animation or instant appearance")]
    public bool useTravelAnimation = true;
    
    [Header("Animation Settings")]
    [Tooltip("Scale animation speed")]
    public float scaleSpeed = 2f;
    [Tooltip("Rotation speed of the indicator")]
    public float rotationSpeed = 90f;
    [Tooltip("Pulse animation intensity")]
    public float pulseIntensity = 0.2f;
    [Tooltip("Pulse animation speed")]
    public float pulseSpeed = 3f;
    
    [Header("Trail Settings")]
    [Tooltip("Trail fade speed")]
    public float trailFadeSpeed = 2f;
    [Tooltip("Trail scale over time")]
    public AnimationCurve trailScaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    
    // Static pause system for all spawn indicators
    private static bool globalPaused = false;
    private static float pauseStartTime = 0f;
    
    private GameObject currentIndicator;
    private TrailRenderer trailRenderer;
    private SpriteRenderer iconRenderer;
    private Vector3 originalScale;
    private float startTime;
    private bool isActive = false;
    private Vector3 sourcePosition;
    private Vector3 targetPosition;
    private bool isTraveling = false;
    private bool isPaused = false;
    private Coroutine currentCoroutine;
    
    void Start()
    {
        // Reset global pause state when scene starts
        ResetGlobalPauseState();
    }
    
    /// <summary>
    /// Pauses all spawn indicators in the scene
    /// </summary>
    public static void PauseAllSpawnIndicators()
    {
        globalPaused = true;
        pauseStartTime = Time.time;
        
        // Find all SpawnIndicator instances and pause them
        SpawnIndicator[] allIndicators = FindObjectsOfType<SpawnIndicator>();
        foreach (SpawnIndicator indicator in allIndicators)
        {
            indicator.PauseIndicator();
        }
        
        Debug.Log("SpawnIndicator: All spawn indicators paused");
    }
    
    /// <summary>
    /// Resumes all spawn indicators in the scene
    /// </summary>
    public static void ResumeAllSpawnIndicators()
    {
        globalPaused = false;
        
        // Find all SpawnIndicator instances and resume them
        SpawnIndicator[] allIndicators = FindObjectsOfType<SpawnIndicator>();
        foreach (SpawnIndicator indicator in allIndicators)
        {
            indicator.ResumeIndicator();
        }
        
        Debug.Log("SpawnIndicator: All spawn indicators resumed");
    }
    
    /// <summary>
    /// Returns whether spawn indicators are currently paused globally
    /// </summary>
    public static bool AreSpawnIndicatorsPaused()
    {
        return globalPaused;
    }
    
    /// <summary>
    /// Reset the global pause state when scene is reloaded
    /// </summary>
    public static void ResetGlobalPauseState()
    {
        globalPaused = false;
        pauseStartTime = 0f;
        Debug.Log("SpawnIndicator: Reset global pause state for scene reload");
    }
    
    /// <summary>
    /// Starts the spawn indicator at the specified position
    /// </summary>
    /// <param name="spawnPosition">Where the enemy will spawn</param>
    /// <param name="enemyPrefab">The enemy prefab that will spawn (for size reference)</param>
    /// <param name="customDuration">Optional custom duration for the indicator</param>
    public void StartSpawnIndicator(Vector3 spawnPosition, GameObject enemyPrefab = null, float customDuration = -1f)
    {
        StartSpawnIndicator(spawnPosition, Vector3.zero, enemyPrefab, customDuration);
    }
    
    /// <summary>
    /// Starts the spawn indicator traveling from source to target position
    /// </summary>
    /// <param name="spawnPosition">Where the enemy will spawn</param>
    /// <param name="sourcePosition">Where the indicator starts from (use Vector3.zero for instant appearance)</param>
    /// <param name="enemyPrefab">The enemy prefab that will spawn (for size reference)</param>
    /// <param name="customDuration">Optional custom duration for the indicator</param>
    public void StartSpawnIndicator(Vector3 spawnPosition, Vector3 sourcePosition, GameObject enemyPrefab = null, float customDuration = -1f)
    {
        if (isActive)
        {
            Debug.LogWarning("SpawnIndicator: Already showing an indicator!");
            return;
        }
        
        // Check if spawn indicators are globally paused
        if (globalPaused)
        {
            Debug.Log("SpawnIndicator: Cannot start indicator while spawn indicators are paused");
            return;
        }
        
        float duration = customDuration > 0f ? customDuration : indicatorDuration;
        this.sourcePosition = sourcePosition;
        this.targetPosition = spawnPosition;
        
        // Create the spawn indicator
        CreateSpawnIndicator(enemyPrefab);
        
        // Start the indicator sequence
        currentCoroutine = StartCoroutine(SpawnIndicatorSequence(duration));
    }
    
    /// <summary>
    /// Pauses this specific spawn indicator
    /// </summary>
    public void PauseIndicator()
    {
        if (isActive && !isPaused)
        {
            isPaused = true;
            
            // Stop the current coroutine
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
                currentCoroutine = null;
            }
            
            // Pause trail emission
            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
            }
            
            Debug.Log("SpawnIndicator: Indicator paused");
        }
    }
    
    /// <summary>
    /// Resumes this specific spawn indicator
    /// </summary>
    public void ResumeIndicator()
    {
        if (isActive && isPaused)
        {
            isPaused = false;
            
            // Resume trail emission
            if (trailRenderer != null)
            {
                trailRenderer.emitting = true;
            }
            
            // Check if we should still spawn or if we've been paused too long
            float elapsedTime = Time.time - startTime;
            float remainingDuration = indicatorDuration - elapsedTime;
            
            if (remainingDuration > 0f)
            {
                // Continue with the remaining time
                currentCoroutine = StartCoroutine(SpawnIndicatorSequence(remainingDuration));
                Debug.Log($"SpawnIndicator: Indicator resumed with {remainingDuration:F2}s remaining");
            }
            else
            {
                // If we've exceeded the duration, clean up immediately without spawning
                Debug.Log("SpawnIndicator: Indicator was paused too long, cleaning up without spawning");
                CleanupIndicator();
            }
        }
    }
    
    /// <summary>
    /// Returns whether this specific spawn indicator is paused
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }
    
    private void CreateSpawnIndicator(GameObject enemyPrefab)
    {
        // Create the main indicator object
        currentIndicator = new GameObject("SpawnIndicator");
        
        // Set initial position based on whether we're using travel animation
        if (useTravelAnimation && sourcePosition != Vector3.zero)
        {
            currentIndicator.transform.position = sourcePosition;
            isTraveling = true;
        }
        else
        {
            currentIndicator.transform.position = targetPosition;
            isTraveling = false;
        }
        
        currentIndicator.transform.SetParent(transform);
        
        // Add sprite renderer for the icon
        iconRenderer = currentIndicator.AddComponent<SpriteRenderer>();
        
        // Set the icon sprite
        if (spawnIconPrefab != null)
        {
            SpriteRenderer prefabRenderer = spawnIconPrefab.GetComponent<SpriteRenderer>();
            if (prefabRenderer != null)
            {
                iconRenderer.sprite = prefabRenderer.sprite;
            }
        }
        else
        {
            // Create a default warning icon if no prefab is assigned
            CreateDefaultIcon();
        }
        
        // Set up the icon
        iconRenderer.color = indicatorColor;
        iconRenderer.sortingOrder = 10; // Ensure it's visible above other objects
        
        // Scale based on enemy size if provided
        if (enemyPrefab != null)
        {
            SpriteRenderer enemyRenderer = enemyPrefab.GetComponent<SpriteRenderer>();
            if (enemyRenderer != null)
            {
                float enemySize = Mathf.Max(enemyRenderer.bounds.size.x, enemyRenderer.bounds.size.y);
                currentIndicator.transform.localScale = Vector3.one * (enemySize * 0.8f);
            }
        }
        
        originalScale = currentIndicator.transform.localScale;
        
        // Add trail renderer
        trailRenderer = currentIndicator.AddComponent<TrailRenderer>();
        SetupTrailRenderer();
        
        isActive = true;
        startTime = Time.time;
    }
    
    private void CreateDefaultIcon()
    {
        // Create a simple warning triangle sprite
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                // Create a triangle shape
                float centerX = 16f;
                float centerY = 16f;
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                
                // Triangle vertices
                Vector2 v1 = new Vector2(16f, 28f); // Top
                Vector2 v2 = new Vector2(8f, 8f);   // Bottom left
                Vector2 v3 = new Vector2(24f, 8f);  // Bottom right
                
                Vector2 point = new Vector2(x, y);
                
                // Check if point is inside triangle
                bool inside = IsPointInTriangle(point, v1, v2, v3);
                
                if (inside)
                {
                    pixels[y * 32 + x] = indicatorColor;
                }
                else
                {
                    pixels[y * 32 + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        iconRenderer.sprite = sprite;
    }
    
    private bool IsPointInTriangle(Vector2 point, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        float d1 = Sign(point, v1, v2);
        float d2 = Sign(point, v2, v3);
        float d3 = Sign(point, v3, v1);
        
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        
        return !(hasNeg && hasPos);
    }
    
    private float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }
    
    private void SetupTrailRenderer()
    {
        trailRenderer.time = trailDuration;
        trailRenderer.startWidth = 0.5f;
        trailRenderer.endWidth = 0.1f;
        trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        trailRenderer.startColor = trailColor;
        trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        trailRenderer.sortingOrder = 9; // Just below the icon
        trailRenderer.emitting = true;
    }
    
    private IEnumerator SpawnIndicatorSequence(float duration)
    {
        float elapsed = 0f;
        float travelStartTime = Time.time;
        float travelDistance = Vector3.Distance(sourcePosition, targetPosition);
        float travelTime = travelDistance / travelSpeed;
        
        // Phase 1: Travel to spawn point (if using travel animation)
        if (useTravelAnimation && sourcePosition != Vector3.zero)
        {
            while (isTraveling && elapsed < travelTime)
            {
                // Check for pause state
                if (globalPaused)
                {
                    yield return null;
                    continue;
                }
                
                elapsed = Time.time - travelStartTime;
                float travelProgress = elapsed / travelTime;
                
                // Move indicator towards target
                currentIndicator.transform.position = Vector3.Lerp(sourcePosition, targetPosition, travelProgress);
                
                // Update animation during travel
                UpdateIndicatorAnimation(travelProgress);
                
                yield return null;
            }
            
            // Ensure we're exactly at the target position
            currentIndicator.transform.position = targetPosition;
            isTraveling = false;
        }
        
        // Phase 2: Brief moment at spawn point (no pause)
        bool wasPaused = false;
        
        // Just update animation for one frame to ensure we're at the target
        UpdateIndicatorAnimation(0.5f);
        yield return null;
        
        // Phase 3: Final animation before cleanup
        float finalStartTime = Time.time;
        float finalDuration = 0.5f; // Short final animation
        
        while ((Time.time - finalStartTime) < finalDuration)
        {
            // Check for pause state
            if (globalPaused)
            {
                yield return null;
                continue;
            }
            
            float finalProgress = (Time.time - finalStartTime) / finalDuration;
            UpdateIndicatorAnimation(0.8f + finalProgress * 0.2f); // Fade out animation
            
            yield return null;
        }
        
        // Clean up
        CleanupIndicator();
    }
    
    private void UpdateIndicatorAnimation(float progress)
    {
        if (currentIndicator == null) return;
        
        // Rotation animation
        currentIndicator.transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        
        // Scale pulse animation
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
        currentIndicator.transform.localScale = originalScale * pulse;
        
        // Fade out towards the end (only during final phase)
        if (progress > 0.8f)
        {
            float fadeProgress = (progress - 0.8f) / 0.2f;
            Color currentColor = iconRenderer.color;
            currentColor.a = 1f - fadeProgress;
            iconRenderer.color = currentColor;
        }
        
        // Update trail
        UpdateTrail(progress);
    }
    
    private void UpdateTrail(float progress)
    {
        if (trailRenderer == null) return;
        
        // Scale trail based on progress
        float trailScale = trailScaleCurve.Evaluate(progress);
        trailRenderer.startWidth = 0.5f * trailScale;
        trailRenderer.endWidth = 0.1f * trailScale;
        
        // Fade trail towards the end
        if (progress > 0.7f)
        {
            float fadeProgress = (progress - 0.7f) / 0.3f;
            Color startColor = trailColor;
            startColor.a = trailColor.a * (1f - fadeProgress);
            trailRenderer.startColor = startColor;
            
            Color endColor = trailColor;
            endColor.a = 0f;
            trailRenderer.endColor = endColor;
        }
    }
    
    private void CleanupIndicator()
    {
        if (currentIndicator != null)
        {
            // Stop trail emission
            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
            }
            
            // Destroy the indicator after a short delay to let trail fade
            Destroy(currentIndicator, trailDuration);
        }
        
        isActive = false;
        isTraveling = false;
        isPaused = false;
        currentIndicator = null;
        trailRenderer = null;
        iconRenderer = null;
    }
    
    /// <summary>
    /// Stops the current spawn indicator immediately
    /// </summary>
    public void StopSpawnIndicator()
    {
        if (isActive)
        {
            CleanupIndicator();
        }
    }
    
    /// <summary>
    /// Returns whether a spawn indicator is currently active
    /// </summary>
    public bool IsIndicatorActive()
    {
        return isActive;
    }
    
    /// <summary>
    /// Returns whether the indicator is currently traveling
    /// </summary>
    public bool IsTraveling()
    {
        return isTraveling;
    }
    
    private void OnDestroy()
    {
        if (currentIndicator != null)
        {
            Destroy(currentIndicator);
        }
    }
}
