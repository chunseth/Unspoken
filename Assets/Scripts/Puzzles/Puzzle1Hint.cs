using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Handles the puzzle1hint object that freezes game interactions when opened.
/// When player collides with the hint trigger, displays a UI overlay with hint information.
/// The puzzle freezes enemy and player movement, actions, spawning, and attacks when opened.
/// </summary>
public class Puzzle1Hint : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The UI panel prefab that appears when the hint is triggered")]
    public GameObject hintUIPanelPrefab;
    [Tooltip("The Canvas where the hint UI will be instantiated (will auto-find GameCanvas)")]
    public Canvas targetCanvas;
    
    [Header("Interaction Settings")]
    [Tooltip("Whether the hint has been triggered (to prevent multiple triggers)")]
    public bool hasBeenTriggered = false;
    
    [Header("Player Reference")]
    [Tooltip("Reference to the player GameObject")]
    public GameObject player;
    
    [Header("Hint Manager Integration")]
    [Tooltip("Reference to the HintManager (will auto-find if not assigned)")]
    public HintManager hintManager;
    
    [Header("Hint Content")]
    [Tooltip("The hint sprite to display for this puzzle")]
    public Sprite hintSprite;
    
    [Header("Visual Effects")]
    [Tooltip("Optional particle effect to play when hint is opened")]
    public GameObject openEffect;
    [Tooltip("Optional sound effect to play when hint is opened")]
    public AudioClip openSound;
    
    private bool isHintActive = false;
    private SpriteRenderer spriteRenderer;
    private Image uiImage;
    private AudioSource audioSource;
    private Image hintImage;
    private Button backButton;
    private GameObject instantiatedHintPanel;
    
    // Lists to store frozen game objects and their states
    private List<Rigidbody2D> frozenProjectiles = new List<Rigidbody2D>();
    private List<Vector2> projectileVelocities = new List<Vector2>();
    private List<MonoBehaviour> frozenEnemyAI = new List<MonoBehaviour>();
    private List<MonoBehaviour> frozenEnemyShooters = new List<MonoBehaviour>();
    private List<MonoBehaviour> frozenPlayerComponents = new List<MonoBehaviour>();
    private List<bool> frozenPlayerComponentStates = new List<bool>();
    private List<Rigidbody2D> frozenPlayerRigidbodies = new List<Rigidbody2D>();
    private List<Vector2> frozenPlayerVelocities = new List<Vector2>();
    
    void Start()
    {
        // Set up UI references
        SetupUIReferences();
        
        // Ensure we have the required components for trigger interaction
        SetupTriggerComponents();
        
        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("Puzzle1Hint: No player found with 'Player' tag. Please assign the player reference manually.");
            }
        }
        
        // Find HintManager if not assigned
        if (hintManager == null)
        {
            hintManager = HintManager.Instance;
            if (hintManager == null)
            {
                Debug.LogWarning("Puzzle1Hint: No HintManager found. Please ensure HintManager is in the scene.");
            }
            else
            {
                Debug.Log("Puzzle1Hint: Found HintManager automatically.");
            }
        }
        
        // Find canvas if not assigned
        if (targetCanvas == null)
        {
            // First try to find the specific GameCanvas
            GameObject gameCanvasObj = GameObject.Find("GameCanvas");
            if (gameCanvasObj != null)
            {
                targetCanvas = gameCanvasObj.GetComponent<Canvas>();
                if (targetCanvas != null)
                {
                    Debug.Log("Puzzle1Hint: Found GameCanvas automatically.");
                }
                else
                {
                    Debug.LogError("Puzzle1Hint: GameCanvas found but doesn't have Canvas component!");
                }
            }
            else
            {
                // Fallback to any Canvas in the scene
                targetCanvas = FindObjectOfType<Canvas>();
                if (targetCanvas != null)
                {
                    Debug.Log("Puzzle1Hint: Found Canvas automatically (fallback).");
                }
                else
                {
                    Debug.LogError("Puzzle1Hint: No Canvas found in scene! Please ensure there is a Canvas named 'GameCanvas' or any Canvas in the scene.");
                }
            }
        }
        
        // Find and set up the hint image from the UI panel
        SetupHintImage();
        
        // Cache visual components
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<Image>();
        
        if (spriteRenderer == null && uiImage == null)
        {
            Debug.LogWarning("Puzzle1Hint: No SpriteRenderer or Image component found. Visual changes may not work.");
        }
        
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && openSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    private void SetupUIReferences()
    {
        // Validate and set up UI references
        ValidateUIReferences();
    }
    
    private void SetupTriggerComponents()
    {
        // Ensure we have a trigger Collider2D for interaction
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider == null)
        {
            Debug.Log("Puzzle1Hint: Adding trigger BoxCollider2D for interaction.");
            BoxCollider2D newCollider = gameObject.AddComponent<BoxCollider2D>();
            newCollider.isTrigger = true;
        }
        else
        {
            // Ensure the collider is set to trigger
            triggerCollider.isTrigger = true;
            Debug.Log("Puzzle1Hint: Existing collider set to trigger mode.");
        }
        
        // Ensure we have a Rigidbody2D for physics interaction (set to kinematic)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.Log("Puzzle1Hint: Adding Rigidbody2D for trigger interaction.");
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.simulated = true;
        }
    }
    

    
    private void ValidateUIReferences()
    {
        // Check if the HintUIPanel prefab is assigned
        if (hintUIPanelPrefab == null)
        {
            Debug.LogError("Puzzle1Hint: HintUIPanel prefab is not assigned! Please assign the HintUIPanel prefab in the inspector.");
        }
        else
        {
            Debug.Log("Puzzle1Hint: HintUIPanel prefab is assigned.");
        }
        
        // Check if Canvas is found
        if (targetCanvas == null)
        {
            Debug.LogError("Puzzle1Hint: Target Canvas not found! Please ensure there is a Canvas named 'GameCanvas' in the scene.");
        }
        else
        {
            Debug.Log($"Puzzle1Hint: Target Canvas found: {targetCanvas.name}");
        }
    }
    
    private void SetupHintImage()
    {
        // Find the HintImage component as a child of the instantiated hint panel
        if (instantiatedHintPanel != null)
        {
            // First try to find by name
            Transform hintImageTransform = instantiatedHintPanel.transform.Find("HintImage");
            if (hintImageTransform != null)
            {
                hintImage = hintImageTransform.GetComponent<Image>();
                if (hintImage != null)
                {
                    Debug.Log("Puzzle1Hint: Found HintImage component by name.");
                }
            }
            
            // If not found by name, try to find any Image component in children
            if (hintImage == null)
            {
                hintImage = instantiatedHintPanel.GetComponentInChildren<Image>();
                if (hintImage != null)
                {
                    Debug.Log($"Puzzle1Hint: Found Image component '{hintImage.name}' in children of instantiated hint panel.");
                }
            }
            
            if (hintImage != null)
            {
                // Set the hint sprite if assigned
                if (hintSprite != null)
                {
                    hintImage.sprite = hintSprite;
                    
                    // Set the hint image to standard size
                    RectTransform rectTransform = hintImage.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        Vector2 currentSize = rectTransform.sizeDelta;
                        rectTransform.sizeDelta = currentSize; // Keep original size
                        Debug.Log($"Puzzle1Hint: Set hint sprite on '{hintImage.name}' Image component with standard size {rectTransform.sizeDelta}.");
                    }
                    else
                    {
                        Debug.Log($"Puzzle1Hint: Set hint sprite on '{hintImage.name}' Image component (no RectTransform found for size adjustment).");
                    }
                }
                else
                {
                    Debug.LogWarning("Puzzle1Hint: No hint sprite assigned. Please assign a sprite in the inspector.");
                }
            }
            else
            {
                Debug.LogError("Puzzle1Hint: No Image component found in instantiated hint panel. Please ensure the HintUIPanel prefab contains a child Image component named 'HintImage'.");
                
                // Debug: List all children to help identify the issue
                Transform[] children = instantiatedHintPanel.GetComponentsInChildren<Transform>();
                Debug.Log($"Puzzle1Hint: Instantiated panel has {children.Length} children:");
                foreach (Transform child in children)
                {
                    Debug.Log($"  - {child.name} (Image: {child.GetComponent<Image>() != null})");
                }
            }
        }
        else
        {
            Debug.LogError("Puzzle1Hint: Instantiated hint panel not found. Cannot set up hint image.");
        }
    }
    
    private void SetupBackButton()
    {
        // Find the back button as a child of the instantiated hint panel
        if (instantiatedHintPanel != null)
        {
            backButton = instantiatedHintPanel.GetComponentInChildren<Button>();
            if (backButton != null)
            {
                Debug.Log($"Puzzle1Hint: Found BackButton '{backButton.name}' as child of instantiated hint panel.");
                backButton.onClick.AddListener(CloseHint);
                Debug.Log("Puzzle1Hint: BackButton click listener added successfully.");
            }
            else
            {
                Debug.LogError("Puzzle1Hint: No BackButton found in instantiated hint panel. Please ensure the HintUIPanel prefab contains a Button component.");
                
                // Debug: List all children to help identify the issue
                Transform[] children = instantiatedHintPanel.GetComponentsInChildren<Transform>();
                Debug.Log($"Puzzle1Hint: Instantiated panel has {children.Length} children:");
                foreach (Transform child in children)
                {
                    Debug.Log($"  - {child.name} (Button: {child.GetComponent<Button>() != null})");
                }
            }
        }
        else
        {
            Debug.LogError("Puzzle1Hint: Instantiated hint panel not found. Cannot set up back button.");
        }
    }
    
    // Trigger interaction method
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasBeenTriggered && !isHintActive)
        {
            Debug.Log("Puzzle1Hint: Player entered trigger area!");
            OpenHint();
        }
    }
    
    // Public method that can be called from other scripts or input systems
    [ContextMenu("Test Interaction")]
    public void Interact()
    {
        Debug.Log("Puzzle1Hint: Manual interaction triggered!");
        if (!hasBeenTriggered && !isHintActive)
        {
            OpenHint();
        }
    }
    

    
    protected virtual void OpenHint()
    {
        if (hintUIPanelPrefab != null && targetCanvas != null && !hasBeenTriggered)
        {
            // Instantiate the hint UI panel as a child of the target canvas
            instantiatedHintPanel = Instantiate(hintUIPanelPrefab, targetCanvas.transform);
            isHintActive = true;
            hasBeenTriggered = true;
            
            // Set up the hint image and back button on the instantiated panel
            SetupHintImage();
            SetupBackButton();
            
            // Notify HintManager that this hint was triggered
            if (hintManager != null)
            {
                hintManager.NotifyPuzzle1HintTriggered(this);
            }
            
            // Play open effect if assigned
            if (openEffect != null)
            {
                Instantiate(openEffect, transform.position, Quaternion.identity);
            }
            
            // Play open sound if assigned
            if (audioSource != null && openSound != null)
            {
                audioSource.PlayOneShot(openSound);
            }
            
            // Disable player movement and other game interactions
            DisableGameInteractions();
            
            Debug.Log("Puzzle1Hint opened. Game interactions are now frozen.");
        }
        else
        {
            if (hintUIPanelPrefab == null)
            {
                Debug.LogError("Puzzle1Hint: HintUIPanel prefab is not assigned!");
            }
            if (targetCanvas == null)
            {
                Debug.LogError("Puzzle1Hint: Target Canvas not found! Please ensure there is a Canvas named 'GameCanvas' in the scene.");
            }
        }
    }
    
    private void CloseHint()
    {
        if (instantiatedHintPanel != null)
        {
            // Destroy the instantiated hint panel
            Destroy(instantiatedHintPanel);
            instantiatedHintPanel = null;
            isHintActive = false;
            
            // Notify HintManager that this hint was closed
            if (hintManager != null)
            {
                hintManager.NotifyPuzzle1HintClosed(this);
            }
            
            // Re-enable player movement and other game interactions
            EnableGameInteractions();
            
            // Destroy the hint object after the hint is closed
            DestroyHintObject();
            
            Debug.Log("Hint closed. Game interactions are now restored.");
        }
    }
    
    private void DestroyHintObject()
    {
        // Destroy the hint object from the dungeon
        Debug.Log("Puzzle1Hint: Destroying hint object from dungeon.");
        Destroy(gameObject);
    }
    
    private void DisableGameInteractions()
    {
        // Freeze player components
        FreezePlayerComponents();
        
        // Freeze all spell projectiles
        FreezeSpellProjectiles();
        
        // Freeze enemy AI and shooter behavior
        FreezeEnemyBehavior();
        
        // Pause all spawn indicators
        if (typeof(SpawnIndicator) != null)
        {
            SpawnIndicator.PauseAllSpawnIndicators();
        }
        
        // Disable other game systems that should be paused
        // For example, camera movement, etc.
        
        // Set time scale to pause the game (optional)
        // Time.timeScale = 0f;
    }
    
    private void FreezePlayerComponents()
    {
        // Clear previous lists
        frozenPlayerComponents.Clear();
        frozenPlayerComponentStates.Clear();
        frozenPlayerRigidbodies.Clear();
        frozenPlayerVelocities.Clear();
        
        if (player == null) return;
        
        // Freeze all MonoBehaviour components on the player
        MonoBehaviour[] allPlayerComponents = player.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in allPlayerComponents)
        {
            if (component != null && component.enabled && component != this)
            {
                frozenPlayerComponents.Add(component);
                frozenPlayerComponentStates.Add(true);
                component.enabled = false;
                Debug.Log($"Puzzle1Hint: Frozen player component {component.GetType().Name} on {player.name}");
            }
        }
        
        // Also freeze the player's Rigidbody2D to stop any ongoing movement
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null && playerRb.simulated)
        {
            // Store the current velocity to restore later
            frozenPlayerRigidbodies.Add(playerRb);
            frozenPlayerVelocities.Add(playerRb.velocity);
            playerRb.simulated = false;
            playerRb.velocity = Vector2.zero;
            Debug.Log($"Puzzle1Hint: Frozen player Rigidbody2D on {player.name}");
        }
        
        Debug.Log($"Puzzle1Hint: Frozen {frozenPlayerComponents.Count} player components and {frozenPlayerRigidbodies.Count} player rigidbodies");
    }
    
    private void FreezeSpellProjectiles()
    {
        // Clear previous lists
        frozenProjectiles.Clear();
        projectileVelocities.Clear();
        
        // Find all spell projectiles in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // Check if this is a spell projectile (adjust the condition based on your spell system)
            if (IsSpellProjectile(obj))
            {
                Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
                if (rb != null && rb.simulated)
                {
                    // Store the projectile and its velocity
                    frozenProjectiles.Add(rb);
                    projectileVelocities.Add(rb.velocity);
                    
                    // Freeze the projectile
                    rb.simulated = false;
                    rb.velocity = Vector2.zero;
                    
                    Debug.Log($"Puzzle1Hint: Frozen spell projectile: {obj.name}");
                }
            }
        }
        
        Debug.Log($"Puzzle1Hint: Frozen {frozenProjectiles.Count} spell projectiles");
    }
    
    private bool IsSpellProjectile(GameObject obj)
    {
        // Check for common spell projectile identifiers
        // Adjust these conditions based on your spell system
        
        // Check by tag
        if (obj.CompareTag("SpellProjectile"))
            return true;

        return false;
    }
    
    private void FreezeEnemyBehavior()
    {
        // Clear previous lists
        frozenEnemyAI.Clear();
        frozenEnemyShooters.Clear();
        
        // Find all enemies in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // Check if this is an enemy
            if (IsEnemy(obj))
            {
                // Freeze enemy AI components
                FreezeEnemyAIComponents(obj);
                
                // Freeze enemy shooter components
                FreezeEnemyShooterComponents(obj);
            }
        }
        
        Debug.Log($"Puzzle1Hint: Frozen {frozenEnemyAI.Count} enemy AI components and {frozenEnemyShooters.Count} enemy shooter components");
    }
    
    private bool IsEnemy(GameObject obj)
    {
        // Check for common enemy identifiers
        // Adjust these conditions based on your enemy system
        
        // Check by tag
        if (obj.CompareTag("Enemy"))
            return true;
        
        return false;
    }
    
    private void FreezeEnemyAIComponents(GameObject enemy)
    {
        // Common enemy AI component names - adjust based on your system
        string[] aiComponentNames = {
            "EnemyAI"
        };
        
        foreach (string componentName in aiComponentNames)
        {
            MonoBehaviour aiComponent = enemy.GetComponent(componentName) as MonoBehaviour;
            if (aiComponent != null && aiComponent.enabled)
            {
                frozenEnemyAI.Add(aiComponent);
                aiComponent.enabled = false;
                Debug.Log($"Puzzle1Hint: Frozen enemy AI component {componentName} on {enemy.name}");
            }
        }
    }
    
    private void FreezeEnemyShooterComponents(GameObject enemy)
    {
        // Common enemy shooter component names - adjust based on your system
        string[] shooterComponentNames = {
            "EnemyShooter"
        };
        
        foreach (string componentName in shooterComponentNames)
        {
            MonoBehaviour shooterComponent = enemy.GetComponent(componentName) as MonoBehaviour;
            if (shooterComponent != null && shooterComponent.enabled)
            {
                frozenEnemyShooters.Add(shooterComponent);
                shooterComponent.enabled = false;
                Debug.Log($"Puzzle1Hint: Frozen enemy shooter component {componentName} on {enemy.name}");
            }
        }
    }
    
    private void EnableGameInteractions()
    {
        // Re-enable player components
        UnfreezePlayerComponents();
        
        // Unfreeze all spell projectiles
        UnfreezeSpellProjectiles();
        
        // Unfreeze enemy AI and shooter behavior
        UnfreezeEnemyBehavior();
        
        // Resume all spawn indicators
        if (typeof(SpawnIndicator) != null)
        {
            SpawnIndicator.ResumeAllSpawnIndicators();
        }
        
        // Re-enable other game systems
        
        // Restore time scale
        // Time.timeScale = 1f;
    }
    
    private void UnfreezePlayerComponents()
    {
        // Restore all frozen player components
        for (int i = 0; i < frozenPlayerComponents.Count; i++)
        {
            if (frozenPlayerComponents[i] != null)
            {
                frozenPlayerComponents[i].enabled = frozenPlayerComponentStates[i];
                Debug.Log($"Puzzle1Hint: Unfrozen player component {frozenPlayerComponents[i].GetType().Name} on {frozenPlayerComponents[i].gameObject.name}");
            }
        }
        
        // Restore all frozen player rigidbodies
        for (int i = 0; i < frozenPlayerRigidbodies.Count; i++)
        {
            if (frozenPlayerRigidbodies[i] != null)
            {
                frozenPlayerRigidbodies[i].simulated = true;
                frozenPlayerRigidbodies[i].velocity = frozenPlayerVelocities[i];
                Debug.Log($"Puzzle1Hint: Unfrozen player Rigidbody2D on {frozenPlayerRigidbodies[i].gameObject.name}");
            }
        }
        
        Debug.Log($"Puzzle1Hint: Unfrozen {frozenPlayerComponents.Count} player components and {frozenPlayerRigidbodies.Count} player rigidbodies");
        
        // Clear the lists
        frozenPlayerComponents.Clear();
        frozenPlayerComponentStates.Clear();
        frozenPlayerRigidbodies.Clear();
        frozenPlayerVelocities.Clear();
    }
    
    private void UnfreezeSpellProjectiles()
    {
        // Restore all frozen projectiles
        for (int i = 0; i < frozenProjectiles.Count; i++)
        {
            if (frozenProjectiles[i] != null)
            {
                // Restore the projectile's physics and velocity
                frozenProjectiles[i].simulated = true;
                frozenProjectiles[i].velocity = projectileVelocities[i];
                
                Debug.Log($"Puzzle1Hint: Unfrozen spell projectile: {frozenProjectiles[i].gameObject.name}");
            }
        }
        
        Debug.Log($"Puzzle1Hint: Unfrozen {frozenProjectiles.Count} spell projectiles");
        
        // Clear the lists
        frozenProjectiles.Clear();
        projectileVelocities.Clear();
    }
    
    private void UnfreezeEnemyBehavior()
    {
        // Unfreeze enemy AI components
        foreach (MonoBehaviour aiComponent in frozenEnemyAI)
        {
            if (aiComponent != null)
            {
                aiComponent.enabled = true;
                Debug.Log($"Puzzle1Hint: Unfrozen enemy AI component {aiComponent.GetType().Name} on {aiComponent.gameObject.name}");
            }
        }
        
        // Unfreeze enemy shooter components
        foreach (MonoBehaviour shooterComponent in frozenEnemyShooters)
        {
            if (shooterComponent != null)
            {
                shooterComponent.enabled = true;
                Debug.Log($"Puzzle1Hint: Unfrozen enemy shooter component {shooterComponent.GetType().Name} on {shooterComponent.gameObject.name}");
            }
        }
        
        Debug.Log($"Puzzle1Hint: Unfrozen {frozenEnemyAI.Count} enemy AI components and {frozenEnemyShooters.Count} enemy shooter components");
        
        // Clear the lists
        frozenEnemyAI.Clear();
        frozenEnemyShooters.Clear();
    }
    
    void OnDestroy()
    {
        // If the hint is active when destroyed, restore game interactions
        if (isHintActive)
        {
            EnableGameInteractions();
            Debug.Log("Puzzle1Hint: Hint object destroyed while active. Game interactions restored.");
        }
    }
    
    // Helper method to check if all required references are assigned
    [ContextMenu("Validate References")]
    public void ValidateReferences()
    {
        Debug.Log("=== Puzzle1Hint Reference Validation ===");
        Debug.Log($"Hint UI Panel Prefab: {(hintUIPanelPrefab != null ? "✓ Assigned" : "✗ Missing")}");
        Debug.Log($"Target Canvas: {(targetCanvas != null ? "✓ Assigned" : "✗ Missing")}");
        Debug.Log($"Instantiated Panel: {(instantiatedHintPanel != null ? "✓ Active" : "✗ Not Active")}");
        Debug.Log($"Back Button: {(backButton != null ? "✓ Found" : "✗ Not found")}");
        Debug.Log($"Hint Image: {(hintImage != null ? "✓ Found" : "✗ Not found")}");
        Debug.Log($"Player: {(player != null ? "✓ Assigned" : "✗ Missing (will auto-find)")}");
        Debug.Log($"Has Been Triggered: {hasBeenTriggered}");
        Debug.Log($"Hint Sprite: {(hintSprite != null ? "✓ Assigned" : "✗ Missing")}");
        Debug.Log($"Open Effect: {(openEffect != null ? "✓ Assigned" : "✗ Missing (optional)")}");
        Debug.Log($"Open Sound: {(openSound != null ? "✓ Assigned" : "✗ Missing (optional)")}");
        Debug.Log($"Visual Component: {(spriteRenderer != null ? "SpriteRenderer" : uiImage != null ? "Image" : "✗ None found")}");
        Debug.Log($"Hint Active: {isHintActive}");
        Debug.Log($"Hint Manager: {(hintManager != null ? "✓ Connected" : "✗ Not Connected")}");
        Debug.Log($"Frozen Projectiles: {frozenProjectiles.Count}");
        Debug.Log($"Frozen Enemy AI: {frozenEnemyAI.Count}");
        Debug.Log($"Frozen Enemy Shooters: {frozenEnemyShooters.Count}");
        Debug.Log($"Frozen Player Components: {frozenPlayerComponents.Count}");
        Debug.Log("========================================");
    }
    
    // Helper method to find UI elements in the scene
    [ContextMenu("Find UI Elements")]
    public void FindUIElements()
    {
        Debug.Log("=== Searching for UI Elements ===");
        
        // Look for hint UI panel
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("hint") || obj.name.ToLower().Contains("ui"))
            {
                Debug.Log($"Found potential UI element: {obj.name}");
            }
        }
        
        Debug.Log("=================================");
    }
    
    /// <summary>
    /// Returns whether this hint is currently active
    /// </summary>
    /// <returns>True if the hint is active, false otherwise</returns>
    public bool IsHintActive()
    {
        return isHintActive;
    }
    
    /// <summary>
    /// Sets the hint sprite
    /// </summary>
    /// <param name="sprite">The new hint sprite</param>
    public void SetHintSprite(Sprite sprite)
    {
        hintSprite = sprite;
        if (hintImage != null)
        {
            hintImage.sprite = hintSprite;
            Debug.Log("Puzzle1Hint: Hint sprite updated.");
        }
        else
        {
            Debug.LogWarning("Puzzle1Hint: Cannot set hint sprite - hint image not found.");
        }
    }
    
    // Debug method to check interaction status
    [ContextMenu("Debug Interaction Status")]
    public void DebugInteractionStatus()
    {
        Debug.Log("=== Puzzle1Hint Interaction Debug ===");
        Debug.Log($"Has been triggered: {hasBeenTriggered}");
        Debug.Log($"Player assigned: {player != null}");
        Debug.Log($"Hint active: {isHintActive}");
        
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            Debug.Log($"Distance to player: {distance:F2}");
            Debug.Log($"Player position: {player.transform.position}");
            Debug.Log($"Hint position: {transform.position}");
        }
        
        // Check colliders
        Collider2D[] allColliders = GetComponents<Collider2D>();
        Debug.Log($"Number of Collider2D components: {allColliders.Length}");
        
        for (int i = 0; i < allColliders.Length; i++)
        {
            Collider2D collider = allColliders[i];
            Debug.Log($"Collider {i}: {collider.GetType().Name}, isTrigger: {collider.isTrigger}, enabled: {collider.enabled}");
            
            if (collider is BoxCollider2D boxCollider)
            {
                Debug.Log($"  Size: {boxCollider.size}, Offset: {boxCollider.offset}");
            }
        }
        
        // Check Rigidbody2D
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        Debug.Log($"Has Rigidbody2D: {rb != null}");
        if (rb != null)
        {
            Debug.Log($"Rigidbody is kinematic: {rb.isKinematic}");
            Debug.Log($"Rigidbody simulated: {rb.simulated}");
        }
        
        Debug.Log("=====================================");
    }
}
