using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Boss health bar component that displays boss health using a UI image and TextMeshPro text.
/// Appears when player enters boss room and disappears when boss dies.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The UI Image component that represents the health bar fill")]
    public Image healthBarImage;
    [Tooltip("The TextMeshPro component that displays the health text")]
    public TextMeshProUGUI healthText;
    [Tooltip("The background image of the health bar (optional)")]
    public Image backgroundImage;
    [Tooltip("The boss name text component")]
    public TextMeshProUGUI bossNameText;
    
    [Header("Boss Settings")]
    [Tooltip("Reference to the boss EnemyHealth component")]
    public EnemyHealth bossHealth;
    [Tooltip("Name of the boss to display")]
    public string bossName = "Royal Slime";
    [Tooltip("Whether to show health as percentage or absolute value")]
    public bool showAsPercentage = false;
    [Tooltip("Text format for health display (use {0} for health value, {1} for percentage)")]
    public string healthTextFormat = "{0}/{1}";
    
    [Header("Animation Settings")]
    [Tooltip("Speed of the health bar fill animation")]
    public float fillAnimationSpeed = 5f;
    [Tooltip("Speed of the color transition animation")]
    public float colorAnimationSpeed = 3f;
    [Tooltip("Whether to animate health changes")]
    public bool animateHealthChanges = true;
    [Tooltip("Fade in/out animation speed")]
    public float fadeSpeed = 2f;
    
    [Header("Color Settings")]
    [Tooltip("Color when health is at maximum (green)")]
    public Color fullHealthColor = Color.green;
    [Tooltip("Color when health is at minimum (red)")]
    public Color lowHealthColor = Color.red;
    [Tooltip("Color when health is at medium level (yellow)")]
    public Color mediumHealthColor = Color.yellow;
    [Tooltip("Threshold for medium health (0-1)")]
    [Range(0f, 1f)]
    public float mediumHealthThreshold = 0.5f;
    
    [Header("Display Settings")]
    [Tooltip("Whether to show the health bar when boss is at full health")]
    public bool hideWhenFull = false;
    [Tooltip("Whether to show the health bar when boss is dead")]
    public bool hideWhenDead = true;
    
    private float targetScaleX;
    private Color targetColor;
    private bool isAnimating = false;
    private CanvasGroup canvasGroup;
    private Coroutine animationCoroutine;
    private Vector3 originalScale;
    private bool isVisible = false;
    
    /// <summary>
    /// Current health percentage (0-1)
    /// </summary>
    public float HealthPercentage 
    { 
        get 
        { 
            if (bossHealth == null) 
            {
                Debug.LogWarning("BossHealthBar: bossHealth is null in HealthPercentage getter");
                return 0f;
            }
            
            // Check if the boss health component is still valid
            if (bossHealth.CurrentHealth == 0 && bossHealth.MaxHealth == 0)
            {
                Debug.LogWarning("BossHealthBar: Boss health component has zero values");
                return 0f;
            }
            
            float percentage = bossHealth.GetHealthPercent();
            Debug.Log($"BossHealthBar: Raw health percentage from boss: {percentage} (Current: {bossHealth.CurrentHealth}, Max: {bossHealth.MaxHealth})");
            return float.IsNaN(percentage) ? 0f : Mathf.Clamp01(percentage);
        } 
    }
    
    /// <summary>
    /// Whether the health bar is currently visible
    /// </summary>
    public bool IsVisible => isVisible;
    
    private void Awake()
    {
        Debug.Log("BossHealthBar: Awake() called");
        
        // Get or add CanvasGroup for fade animations
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Start hidden and disabled
        canvasGroup.alpha = 0f;
        isVisible = false;
        
        // Hide the entire panel hierarchy
        SetPanelVisibility(false);
        
        // Initialize health bar
        InitializeHealthBar();
    }
    
    private void Start()
    {
        Debug.Log("BossHealthBar: Start() called");
        
        // Find boss health if not assigned
        if (bossHealth == null)
        {
            Debug.Log("BossHealthBar: Looking for Royal Slime boss...");
            // Look for ALL Royal Slime bosses in the scene
            RoyalSlime[] allRoyalSlimes = FindObjectsOfType<RoyalSlime>();
            Debug.Log($"BossHealthBar: Found {allRoyalSlimes.Length} Royal Slime(s) in scene");
            
            foreach (RoyalSlime slime in allRoyalSlimes)
            {
                EnemyHealth health = slime.GetComponent<EnemyHealth>();
                Debug.Log($"BossHealthBar: Royal Slime at {slime.transform.position} - Health: {health?.CurrentHealth}/{health?.MaxHealth}");
            }
            
            // Look for Royal Slime boss in the scene
            RoyalSlime royalSlime = FindObjectOfType<RoyalSlime>();
            if (royalSlime != null)
            {
                Debug.Log($"BossHealthBar: Found Royal Slime at {royalSlime.transform.position}");
                bossHealth = royalSlime.GetComponent<EnemyHealth>();
                if (bossHealth != null)
                {
                    Debug.Log($"BossHealthBar: Found EnemyHealth component, max health: {bossHealth.MaxHealth}, current health: {bossHealth.CurrentHealth}");
                    Debug.Log($"BossHealthBar: Health percentage: {bossHealth.GetHealthPercent()}");
                }
                else
                {
                    Debug.LogWarning("BossHealthBar: Royal Slime found but no EnemyHealth component!");
                }
            }
            else
            {
                Debug.LogWarning("BossHealthBar: No Royal Slime found in scene!");
            }
            
            if (bossHealth == null)
            {
                Debug.LogWarning("BossHealthBar: No boss health component found!");
                return;
            }
        }
        
        Debug.Log("BossHealthBar: Subscribing to health events...");
        // Subscribe to health change events
        bossHealth.onHealthPercentChanged.AddListener(OnBossHealthChanged);
        bossHealth.onDeath.AddListener(OnBossDeath);
        
        // Set boss name
        if (bossNameText != null)
        {
            bossNameText.text = bossName;
            Debug.Log($"BossHealthBar: Set boss name to {bossName}");
        }
        
        // Don't call UpdateHealthDisplay() here as it might hide the health bar
        // The health bar will be updated when the boss health changes
        Debug.Log("BossHealthBar: Start() completed");
    }
    
    private void InitializeHealthBar()
    {
        Debug.Log("BossHealthBar: InitializeHealthBar() called");
        
        // Validate required components
        if (healthBarImage == null)
        {
            Debug.LogWarning("BossHealthBar: Health bar image not assigned!");
            return;
        }
        
        if (healthText == null)
        {
            Debug.LogWarning("BossHealthBar: Health text not assigned!");
            return;
        }
        
        // Store original scale for reference with validation
        Vector3 currentScale = healthBarImage.transform.localScale;
        
        Debug.Log($"BossHealthBar: Current scale of health bar image: {currentScale}");
        
        // Check for invalid scale values (NaN or zero)
        if (float.IsNaN(currentScale.x) || float.IsNaN(currentScale.y) || float.IsNaN(currentScale.z) ||
            currentScale.x == 0f || currentScale.y == 0f || currentScale.z == 0f)
        {
            Debug.LogWarning("BossHealthBar: Invalid scale detected, using default scale (1,1,1)");
            originalScale = Vector3.one;
            healthBarImage.transform.localScale = Vector3.one;
        }
        else
        {
            originalScale = currentScale;
        }
        
        Debug.Log($"BossHealthBar: Final originalScale set to: {originalScale}");
        
        // Set up the RectTransform for left-anchored scaling
        RectTransform rectTransform = healthBarImage.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Set pivot to left center (0, 0.5) so scaling happens from left edge
            rectTransform.pivot = new Vector2(0f, 0.5f);
            
            // Set anchor to left center
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(0f, 0.5f);
            
            Debug.Log($"BossHealthBar: Set pivot to {rectTransform.pivot}, anchors to {rectTransform.anchorMin}/{rectTransform.anchorMax}");
        }
        
        // Ensure we have a valid scale
        if (float.IsNaN(originalScale.x) || originalScale.x == 0f)
        {
            originalScale = Vector3.one;
            healthBarImage.transform.localScale = Vector3.one;
        }
        
        // Set initial color to full health color
        if (bossHealth != null)
        {
            healthBarImage.color = fullHealthColor;
        }
    }
    
    /// <summary>
    /// Event handler for when boss health changes
    /// </summary>
    /// <param name="healthPercent">Current health percentage (0-1)</param>
    private void OnBossHealthChanged(float healthPercent)
    {
        Debug.Log($"BossHealthBar: OnBossHealthChanged called with {healthPercent * 100f}%");
        UpdateHealthDisplay();
    }
    
    /// <summary>
    /// Event handler for when boss dies
    /// </summary>
    private void OnBossDeath()
    {
        if (hideWhenDead)
        {
            HideHealthBar();
        }
    }
    

    
    private void UpdateHealthDisplay()
    {
        Debug.Log("BossHealthBar: UpdateHealthDisplay() called");
        
        if (healthBarImage == null)
        {
            Debug.LogError("BossHealthBar: healthBarImage is null!");
            return;
        }
        
        if (bossHealth == null)
        {
            Debug.LogError("BossHealthBar: bossHealth is null!");
            return;
        }
        
        // Fix: Set originalScale if it's not set properly
        if (originalScale == Vector3.zero)
        {
            Debug.LogWarning("BossHealthBar: originalScale is zero, setting to current scale");
            originalScale = healthBarImage.transform.localScale;
            Debug.Log($"BossHealthBar: Set originalScale to {originalScale}");
            
            // Also set up RectTransform for proper left-anchored scaling
            RectTransform rectTransform = healthBarImage.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.pivot = new Vector2(0f, 0.5f);
                rectTransform.anchorMin = new Vector2(0f, 0.5f);
                rectTransform.anchorMax = new Vector2(0f, 0.5f);
                Debug.Log($"BossHealthBar: Set up RectTransform for left-anchored scaling");
            }
        }
        
        float healthPercentage = HealthPercentage;
        
        // Validate health percentage
        if (float.IsNaN(healthPercentage))
        {
            Debug.LogWarning("BossHealthBar: Invalid health percentage detected, using 0");
            healthPercentage = 0f;
        }
        
        // Clamp health percentage to valid range
        healthPercentage = Mathf.Clamp01(healthPercentage);
        
        // Update target values
        targetScaleX = healthPercentage;
        targetColor = GetHealthColor(healthPercentage);
        
        Debug.Log($"BossHealthBar: Health {healthPercentage * 100f}%, Scale: {targetScaleX}, Color: {targetColor}");
        
        // Update text
        UpdateHealthText();
        
        // For testing: always update immediately
        Debug.Log("BossHealthBar: Updating immediately (animation disabled for testing)");
        Debug.Log($"BossHealthBar: originalScale = {originalScale}, targetScaleX = {targetScaleX}");
        
        Vector3 newScale = originalScale;
        newScale.x = originalScale.x * targetScaleX;
        
        Debug.Log($"BossHealthBar: Calculated newScale = {newScale}");
        Debug.Log($"BossHealthBar: Before setting - healthBarImage.localScale = {healthBarImage.transform.localScale}");
        
        healthBarImage.transform.localScale = newScale;
        healthBarImage.color = targetColor;
        
        Debug.Log($"BossHealthBar: After setting - healthBarImage.localScale = {healthBarImage.transform.localScale}");
        Debug.Log($"BossHealthBar: Applied scale {newScale}, color {healthBarImage.color}");
        
        // Animate changes if enabled (commented out for testing)
        /*
        if (animateHealthChanges && gameObject.activeInHierarchy)
        {
            Debug.Log("BossHealthBar: Using animation");
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
            animationCoroutine = StartCoroutine(AnimateHealthChange());
        }
        else
        {
            // Update immediately
            Debug.Log("BossHealthBar: Updating immediately");
            Vector3 newScale = originalScale;
            newScale.x = originalScale.x * targetScaleX;
            healthBarImage.transform.localScale = newScale;
            healthBarImage.color = targetColor;
            
            Debug.Log($"BossHealthBar: Applied scale {newScale}, color {healthBarImage.color}");
        }
        */
        
        // Update visibility
        UpdateVisibility();
    }
    
    private void UpdateHealthText()
    {
        if (healthText == null || bossHealth == null) return;
        
        string healthValue;
        if (showAsPercentage)
        {
            healthValue = string.Format(healthTextFormat, 
                Mathf.RoundToInt(HealthPercentage * 100f), 
                "100%");
        }
        else
        {
            healthValue = string.Format(healthTextFormat, 
                bossHealth.CurrentHealth, 
                bossHealth.MaxHealth);
        }
        
        healthText.text = healthValue;
    }
    
    private Color GetHealthColor(float percentage)
    {
        if (percentage >= mediumHealthThreshold)
        {
            // Lerp from medium to full health color
            float t = (percentage - mediumHealthThreshold) / (1f - mediumHealthThreshold);
            return Color.Lerp(mediumHealthColor, fullHealthColor, t);
        }
        else
        {
            // Lerp from low to medium health color
            float t = percentage / mediumHealthThreshold;
            return Color.Lerp(lowHealthColor, mediumHealthColor, t);
        }
    }
    
    private IEnumerator AnimateHealthChange()
    {
        isAnimating = true;
        
        Debug.Log($"BossHealthBar: AnimateHealthChange - originalScale: {originalScale}, targetScaleX: {targetScaleX}");
        Debug.Log($"BossHealthBar: AnimateHealthChange - current localScale: {healthBarImage.transform.localScale}");
        
        // Validate originalScale to prevent NaN
        if (float.IsNaN(originalScale.x) || originalScale.x == 0f)
        {
            Debug.LogWarning("BossHealthBar: Invalid originalScale detected, aborting animation");
            isAnimating = false;
            yield break;
        }
        
        float startScaleX = healthBarImage.transform.localScale.x / originalScale.x;
        Color startColor = healthBarImage.color;
        float elapsed = 0f;
        
        Debug.Log($"BossHealthBar: AnimateHealthChange - startScaleX: {startScaleX}, startColor: {startColor}");
        
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * fillAnimationSpeed;
            float t = Mathf.Clamp01(elapsed);
            
            // Animate scale
            float newScaleX = Mathf.Lerp(startScaleX, targetScaleX, t);
            Vector3 newScale = originalScale;
            newScale.x = originalScale.x * newScaleX;
            healthBarImage.transform.localScale = newScale;
            
            // Animate color
            healthBarImage.color = Color.Lerp(startColor, targetColor, t);
            
            Debug.Log($"BossHealthBar: Animation frame - t: {t}, newScaleX: {newScaleX}, newScale: {newScale}, color: {healthBarImage.color}");
            
            yield return null;
        }
        
        // Ensure final values are exact
        Vector3 finalScale = originalScale;
        finalScale.x = originalScale.x * targetScaleX;
        healthBarImage.transform.localScale = finalScale;
        healthBarImage.color = targetColor;
        
        isAnimating = false;
    }
    
    private void UpdateVisibility()
    {
        if (canvasGroup == null) return;
        
        bool shouldShow = true;
        
        if (hideWhenFull && HealthPercentage >= 1f)
        {
            shouldShow = false;
        }
        
        if (hideWhenDead && HealthPercentage <= 0f)
        {
            shouldShow = false;
        }
        
        if (shouldShow != isVisible)
        {
            if (shouldShow)
            {
                ShowHealthBar();
            }
            else
            {
                HideHealthBar();
            }
        }
    }
    
    /// <summary>
    /// Shows the health bar with fade animation
    /// </summary>
    public void ShowHealthBar()
    {
        Debug.Log("BossHealthBar: ShowHealthBar() called");
        
        if (canvasGroup == null)
        {
            Debug.LogWarning("BossHealthBar: CanvasGroup is null!");
            return;
        }
        
        // Enable the entire panel
        SetPanelVisibility(true);
        
        // Update the health display immediately when showing
        UpdateHealthDisplay();
        
        StopAllCoroutines();
        StartCoroutine(FadeHealthBar(1f));
        isVisible = true;
        
        Debug.Log("BossHealthBar: ShowHealthBar() completed");
    }
    
    /// <summary>
    /// Hides the health bar with fade animation
    /// </summary>
    public void HideHealthBar()
    {
        if (canvasGroup == null) return;
        
        StopAllCoroutines();
        StartCoroutine(FadeHealthBar(0f));
        isVisible = false;
    }
    
    /// <summary>
    /// Sets the visibility of the entire panel hierarchy
    /// </summary>
    /// <param name="visible">Whether the panel should be visible</param>
    private void SetPanelVisibility(bool visible)
    {
        Debug.Log($"BossHealthBar: SetPanelVisibility({visible}) called");
        
        // Find the top-level panel (this GameObject or its parent)
        GameObject panelToHide = gameObject;
        
        // If this is a child of a panel, hide the parent panel instead
        Transform parent = transform.parent;
        while (parent != null)
        {
            // Check if parent is a UI panel or has CanvasGroup
            if (parent.GetComponent<CanvasGroup>() != null || 
                parent.GetComponent<Image>() != null ||
                parent.name.ToLower().Contains("panel"))
            {
                panelToHide = parent.gameObject;
                Debug.Log($"BossHealthBar: Found parent panel: {panelToHide.name}");
                break;
            }
            parent = parent.parent;
        }
        
        // Set the panel visibility
        Debug.Log($"BossHealthBar: Setting {panelToHide.name} active to {visible}");
        panelToHide.SetActive(visible);
    }
    
    private IEnumerator FadeHealthBar(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * fadeSpeed;
            float t = Mathf.Clamp01(elapsed);
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            
            yield return null;
        }
        
        canvasGroup.alpha = targetAlpha;
        
        // If we're hiding the health bar, disable the entire panel
        if (targetAlpha == 0f)
        {
            SetPanelVisibility(false);
        }
    }
    
    /// <summary>
    /// Returns whether the health bar is currently animating
    /// </summary>
    public bool IsAnimating()
    {
        return isAnimating;
    }
    
    /// <summary>
    /// Gets the current health value
    /// </summary>
    public int GetCurrentHealth()
    {
        return bossHealth != null ? bossHealth.CurrentHealth : 0;
    }
    
    /// <summary>
    /// Gets the maximum health value
    /// </summary>
    public int GetMaxHealth()
    {
        return bossHealth != null ? bossHealth.MaxHealth : 0;
    }
    
    /// <summary>
    /// Returns whether the boss is at full health
    /// </summary>
    public bool IsFullHealth()
    {
        return bossHealth != null && bossHealth.CurrentHealth >= bossHealth.MaxHealth;
    }
    
    /// <summary>
    /// Returns whether the boss is dead (health <= 0)
    /// </summary>
    public bool IsDead()
    {
        return bossHealth != null && bossHealth.CurrentHealth <= 0;
    }
    
    private void OnValidate()
    {
        // Clamp animation values in inspector
        mediumHealthThreshold = Mathf.Clamp01(mediumHealthThreshold);
        fillAnimationSpeed = Mathf.Max(0.1f, fillAnimationSpeed);
        colorAnimationSpeed = Mathf.Max(0.1f, colorAnimationSpeed);
        fadeSpeed = Mathf.Max(0.1f, fadeSpeed);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (bossHealth != null)
        {
            bossHealth.onHealthPercentChanged.RemoveListener(OnBossHealthChanged);
            bossHealth.onDeath.RemoveListener(OnBossDeath);
        }
    }
}
