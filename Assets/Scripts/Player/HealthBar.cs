using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Health bar component that displays health using a UI image and TextMeshPro text.
/// The image lerps in length and color based on remaining health percentage.
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The UI Image component that represents the health bar fill")]
    public Image healthBarImage;
    [Tooltip("The TextMeshPro component that displays the health text")]
    public TextMeshProUGUI healthText;
    [Tooltip("The background image of the health bar (optional)")]
    public Image backgroundImage;
    
    [Header("Health Settings")]
    [Tooltip("Reference to the PlayerStats component that manages health")]
    public PlayerStats playerStats;
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
    [Tooltip("Whether to show the health bar when health is full")]
    public bool hideWhenFull = false;
    [Tooltip("Whether to show the health bar when health is zero")]
    public bool hideWhenEmpty = false;
    [Tooltip("Fade animation speed for show/hide")]
    public float fadeSpeed = 2f;
    
    private float targetScaleX;
    private Color targetColor;
    private bool isAnimating = false;
    private CanvasGroup canvasGroup;
    private Coroutine animationCoroutine;
    private Vector3 originalScale;
    
    /// <summary>
    /// Current health percentage (0-1)
    /// </summary>
    public float HealthPercentage => playerStats != null ? playerStats.GetHealthPercentage() : 0f;
    
    /// <summary>
    /// Whether the health bar is currently visible
    /// </summary>
    public bool IsVisible => canvasGroup != null ? canvasGroup.alpha > 0f : true;
    
    private void Awake()
    {
        // Get or add CanvasGroup for fade animations
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Initialize health bar
        InitializeHealthBar();
    }
    
    private void Start()
    {
        // Find PlayerStats if not assigned
        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerStats>();
            if (playerStats == null)
            {
                return;
            }
        }
        
        // Subscribe to health change events
        playerStats.OnHealthChanged += OnPlayerHealthChanged;
        
        // Set initial values
        UpdateHealthDisplay();
        UpdateVisibility();
    }
    
    private void InitializeHealthBar()
    {
        // Validate required components
        if (healthBarImage == null)
        {
            return;
        }
        
        if (healthText == null)
        {
            return;
        }
        
        // Store original scale for reference
        originalScale = healthBarImage.transform.localScale;
        
        // Set up the RectTransform for left-anchored scaling
        RectTransform rectTransform = healthBarImage.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Set pivot to left center (0, 0.5) so scaling happens from left edge
            rectTransform.pivot = new Vector2(0f, 0.5f);
            
            // Set anchor to left center
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(0f, 0.5f);
        }
        
        // Set initial scale to full width
        healthBarImage.transform.localScale = originalScale;
    }
    
    /// <summary>
    /// Event handler for when player health changes
    /// </summary>
    /// <param name="currentHealth">Current health value</param>
    /// <param name="maxHealth">Maximum health value</param>
    private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        UpdateHealthDisplay();
    }
    
    private void UpdateHealthDisplay()
    {
        if (healthBarImage == null || playerStats == null) return;
        
        float healthPercentage = HealthPercentage;
        
        // Update target values
        targetScaleX = healthPercentage;
        targetColor = GetHealthColor(healthPercentage);
        
        // Update text
        UpdateHealthText();
        
        // Animate changes if enabled
        if (animateHealthChanges && gameObject.activeInHierarchy)
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
            animationCoroutine = StartCoroutine(AnimateHealthChange());
        }
        else
        {
            // Update immediately
            Vector3 newScale = originalScale;
            newScale.x = originalScale.x * targetScaleX;
            healthBarImage.transform.localScale = newScale;
            healthBarImage.color = targetColor;
        }
        
        // Update visibility
        UpdateVisibility();
    }
    
    private void UpdateHealthText()
    {
        if (healthText == null || playerStats == null) return;
        
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
                playerStats.GetCurrentHealth(), 
                playerStats.GetMaxHealth());
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
        
        float startScaleX = healthBarImage.transform.localScale.x / originalScale.x;
        Color startColor = healthBarImage.color;
        float elapsed = 0f;
        
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
        
        if (hideWhenEmpty && HealthPercentage <= 0f)
        {
            shouldShow = false;
        }
        
        if (shouldShow != IsVisible)
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
        if (canvasGroup == null) return;
        
        StopAllCoroutines();
        StartCoroutine(FadeHealthBar(1f));
    }
    
    /// <summary>
    /// Hides the health bar with fade animation
    /// </summary>
    public void HideHealthBar()
    {
        if (canvasGroup == null) return;
        
        StopAllCoroutines();
        StartCoroutine(FadeHealthBar(0f));
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
        return playerStats != null ? playerStats.GetCurrentHealth() : 0;
    }
    
    /// <summary>
    /// Gets the maximum health value
    /// </summary>
    public int GetMaxHealth()
    {
        return playerStats != null ? playerStats.GetMaxHealth() : 0;
    }
    
    /// <summary>
    /// Returns whether the entity is at full health
    /// </summary>
    public bool IsFullHealth()
    {
        return playerStats != null && playerStats.GetCurrentHealth() >= playerStats.GetMaxHealth();
    }
    
    /// <summary>
    /// Returns whether the entity is dead (health <= 0)
    /// </summary>
    public bool IsDead()
    {
        return playerStats != null && playerStats.GetCurrentHealth() <= 0;
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
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= OnPlayerHealthChanged;
        }
    }
}
