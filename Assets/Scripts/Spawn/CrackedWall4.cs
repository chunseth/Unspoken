using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Handles the fourth cracked wall puzzle in the special room platform.
/// When clicked while player is nearby, displays a UI overlay with a spell grid.
/// The puzzle is solved by inputting the spell pattern "036".
/// </summary>
public class CrackedWall4 : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [Tooltip("The UI panel that appears when the wall is clicked")]
    public GameObject puzzleUIPanel;
    [Tooltip("The background image for the puzzle UI")]
    public Image backgroundImage;
    [Tooltip("The back button to close the puzzle")]
    public Button backButton;
    [Tooltip("Reference to the SpellGridDragController in the UI")]
    public SpellGridDragController spellGridController;
    
    [Header("Interaction Settings")]
    [Tooltip("Distance the player must be within to interact with the wall")]
    public float interactionDistance = 2f;
    [Tooltip("The correct spell pattern to solve the puzzle")]
    public string correctSpellPattern = "036";
    
    [Header("Player Reference")]
    [Tooltip("Reference to the player GameObject")]
    public GameObject player;
    
    [Header("Visual Effects")]
    [Tooltip("Sprite to change to when puzzle is solved (normal wall)")]
    public Sprite solvedWallSprite;
    [Tooltip("Optional particle effect to play when puzzle is solved")]
    public GameObject solveEffect;
    
    private bool isPuzzleActive = false;
    private bool isPuzzleSolved = false;
    private bool playerInRange = false;
    private SpriteRenderer spriteRenderer;
    private Image uiImage;
    
    void Start()
    {
        // Set up UI references first
        SetupUIReferences();
        
        // Ensure we have the required components for interaction
        SetupInteractionComponents();
        
        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("CrackedWall4: No player found with 'Player' tag. Please assign the player reference manually.");
            }
        }
        
        // Set up UI event listeners
        if (backButton != null)
        {
            backButton.onClick.AddListener(ClosePuzzle);
        }
        else
        {
            Debug.LogError("CrackedWall4: Back button is not assigned! Please assign it in the inspector.");
        }
        
        // Initially hide the puzzle UI and background
        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(false);
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(false);
        }
    }
    
    void Update()
    {
        if (player == null) return;
        
        // Check if puzzle is already solved
        if (isPuzzleSolved) return;
        
        // Check player distance for interaction
        float distance = Vector2.Distance(transform.position, player.transform.position);
        
        // Update interaction range
        bool wasInRange = playerInRange;
        playerInRange = distance <= interactionDistance;
        
        // Handle range changes
        if (playerInRange && !wasInRange)
        {
            Debug.Log($"CrackedWall4: Player entered interaction range. Distance: {distance}");
        }
        else if (!playerInRange && wasInRange)
        {
            Debug.Log($"CrackedWall4: Player left interaction range. Distance: {distance}");
        }
        
        // Handle keyboard input (E key) for interaction
        if (Input.GetKeyDown(KeyCode.E) && playerInRange && !isPuzzleActive && !isPuzzleSolved)
        {
            Debug.Log("CrackedWall4: E key pressed, opening puzzle...");
            OpenPuzzle();
        }
        if (playerInRange != wasInRange)
        {
            if (playerInRange)
            {
                Debug.Log("CrackedWall4: Player entered interaction range");
            }
            else
            {
                Debug.Log("CrackedWall4: Player left interaction range");
                // Close puzzle if player leaves range
                if (isPuzzleActive)
                {
                    ClosePuzzle();
                }
            }
        }
    }
    
    void SetupUIReferences()
    {
        // Check if we're in the scene (not a prefab)
        if (Application.isPlaying)
        {
            // Try to find UI components if not assigned
            if (puzzleUIPanel == null)
            {
                puzzleUIPanel = GameObject.Find("PuzzleUIPanel4");
                if (puzzleUIPanel == null)
                {
                    Debug.LogWarning("CrackedWall4: Puzzle UI panel not found. Please assign it manually.");
                }
            }
            
            if (backgroundImage == null)
            {
                backgroundImage = GameObject.Find("PuzzleBackground4")?.GetComponent<Image>();
                if (backgroundImage == null)
                {
                    Debug.LogWarning("CrackedWall4: Puzzle background image not found. Please assign it manually.");
                }
            }
            
            if (backButton == null)
            {
                backButton = GameObject.Find("BackButton4")?.GetComponent<Button>();
                if (backButton == null)
                {
                    Debug.LogWarning("CrackedWall4: Puzzle back button not found. Please assign it manually.");
                }
            }
            
            if (spellGridController == null)
            {
                spellGridController = GameObject.Find("SpellGrid4")?.GetComponent<SpellGridDragController>();
                if (spellGridController == null)
                {
                    Debug.LogWarning("CrackedWall4: SpellGridDragController not found. Please assign it manually.");
                }
            }
        }
    }
    
    void SetupInteractionComponents()
    {
        // Ensure we have a Collider2D for interaction
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
            Debug.Log("CrackedWall4: Added BoxCollider2D component");
        }
        
        // Ensure the collider is properly configured
        if (collider is BoxCollider2D boxCollider)
        {
            // Set the collider size to match the sprite if we have one
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                boxCollider.size = spriteRenderer.sprite.bounds.size;
            }
            else
            {
                // Default size if no sprite
                boxCollider.size = new Vector2(1f, 1f);
            }
            
            // Ensure the collider is not a trigger (it should block movement)
            boxCollider.isTrigger = false;
            
            Debug.Log($"CrackedWall4: Configured BoxCollider2D with size {boxCollider.size}, isTrigger = {boxCollider.isTrigger}");
        }
        
        // Add a Canvas component if we don't have one (needed for UI raycasting)
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            Debug.Log("CrackedWall4: Added Canvas component for UI raycasting");
        }
        
        // Add a GraphicRaycaster if we don't have one
        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = gameObject.AddComponent<GraphicRaycaster>();
            Debug.Log("CrackedWall4: Added GraphicRaycaster component");
        }
        
        // Ensure we have a Rigidbody2D (static for walls)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.simulated = true; // Enable physics simulation for collision detection
            Debug.Log("CrackedWall4: Added static Rigidbody2D component with physics enabled");
        }
        else
        {
            // Ensure existing Rigidbody2D has physics enabled
            rb.isKinematic = true;
            rb.simulated = true;
            Debug.Log("CrackedWall4: Updated existing Rigidbody2D to enable physics");
        }
        
        // Get visual components
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning("CrackedWall4: No SpriteRenderer component found. Visual changes may not work.");
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"CrackedWall4: OnPointerClick detected. Puzzle solved: {isPuzzleSolved}, Player in range: {playerInRange}, Puzzle active: {isPuzzleActive}");
        HandleInteraction();
    }
    
    // Fallback method for mouse clicks (more reliable for 2D objects)
    void OnMouseDown()
    {
        Debug.Log($"CrackedWall4: OnMouseDown detected. Puzzle solved: {isPuzzleSolved}, Player in range: {playerInRange}, Puzzle active: {isPuzzleActive}");
        HandleInteraction();
    }
    
    // Centralized interaction handling
    void HandleInteraction()
    {
        if (isPuzzleSolved) 
        {
            Debug.Log("CrackedWall4: Puzzle already solved, ignoring interaction");
            return;
        }
        
        if (isPuzzleActive)
        {
            Debug.Log("CrackedWall4: Puzzle already active, ignoring interaction");
            return;
        }
        
        if (playerInRange)
        {
            Debug.Log("CrackedWall4: Player in range, opening puzzle");
            OpenPuzzle();
        }
        else
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            Debug.Log($"CrackedWall4: Player too far to interact. Distance: {distance}, Required: {interactionDistance}");
        }
    }
    
    void OpenPuzzle()
    {
        if (isPuzzleActive || isPuzzleSolved) return;
        
        Debug.Log("CrackedWall4: Opening puzzle...");
        Debug.Log($"CrackedWall4: puzzleUIPanel assigned: {puzzleUIPanel != null}");
        Debug.Log($"CrackedWall4: backgroundImage assigned: {backgroundImage != null}");
        Debug.Log($"CrackedWall4: spellGridController assigned: {spellGridController != null}");
        
        // Show the puzzle UI
        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(true);
            Debug.Log("CrackedWall4: Puzzle UI panel activated");
        }
        else
        {
            Debug.LogError("CrackedWall4: puzzleUIPanel is null! Cannot show puzzle.");
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(true);
            Debug.Log("CrackedWall4: Background image activated");
        }
        else
        {
            Debug.LogWarning("CrackedWall4: backgroundImage is null! Background may not show.");
        }
        
        // Subscribe to the spell grid pattern completion event
        if (spellGridController != null)
        {
            spellGridController.OnPatternCompleted += CheckSpellSolution;
        }
        
        isPuzzleActive = true;
        
        // Freeze player and enemies during puzzle
        FreezeGameObjects();
    }
    
    void ClosePuzzle()
    {
        if (!isPuzzleActive) return;
        
        Debug.Log("CrackedWall4: Closing puzzle...");
        
        // Hide the puzzle UI
        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(false);
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(false);
        }
        
        // Unsubscribe from spell grid events
        if (spellGridController != null)
        {
            spellGridController.OnPatternCompleted -= CheckSpellSolution;
        }
        
        isPuzzleActive = false;
        
        // Unfreeze player and enemies
        UnfreezeGameObjects();
    }
    
    void CheckSpellSolution(string spellPattern)
    {
        Debug.Log($"CrackedWall4: Spell pattern entered: {spellPattern}");
        
        if (spellPattern == correctSpellPattern)
        {
            SolvePuzzle();
        }
        else
        {
            Debug.Log("CrackedWall4: Incorrect spell pattern. Try again.");
            // You could add visual feedback here for incorrect patterns
        }
    }
    
    void SolvePuzzle()
    {
        if (isPuzzleSolved) return;
        
        Debug.Log("CrackedWall4: Puzzle solved!");
        
        isPuzzleSolved = true;
        
        // Change the wall sprite to solved state
        if (spriteRenderer != null && solvedWallSprite != null)
        {
            spriteRenderer.sprite = solvedWallSprite;
        }
        
        // Play solve effect if available
        if (solveEffect != null)
        {
            Instantiate(solveEffect, transform.position, Quaternion.identity);
        }
        
        // Convert all hole tiles to floor tiles
        ConvertHoleTilesToFloorTiles();
        
        // Close the puzzle UI
        ClosePuzzle();
        
        // Unfreeze game objects
        UnfreezeGameObjects();
    }
    
    void FreezeGameObjects()
    {
        // Freeze player movement
        if (player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false;
            }
        }
        
        // Freeze all enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null)
            {
                MonoBehaviour[] scripts = enemy.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour script in scripts)
                {
                    if (script != null && script.GetType().Name.Contains("Enemy"))
                    {
                        script.enabled = false;
                    }
                }
            }
        }
        
        // Freeze projectiles
        Rigidbody2D[] projectiles = FindObjectsOfType<Rigidbody2D>();
        foreach (Rigidbody2D projectile in projectiles)
        {
            if (projectile != null && projectile.CompareTag("Projectile"))
            {
                projectile.velocity = Vector2.zero;
                projectile.simulated = false;
            }
        }
    }
    
    void UnfreezeGameObjects()
    {
        // Unfreeze player movement
        if (player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = true;
            }
        }
        
        // Unfreeze all enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null)
            {
                MonoBehaviour[] scripts = enemy.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour script in scripts)
                {
                    if (script != null && script.GetType().Name.Contains("Enemy"))
                    {
                        script.enabled = true;
                    }
                }
            }
        }
        
        // Unfreeze projectiles
        Rigidbody2D[] projectiles = FindObjectsOfType<Rigidbody2D>();
        foreach (Rigidbody2D projectile in projectiles)
        {
            if (projectile != null && projectile.CompareTag("Projectile"))
            {
                projectile.simulated = true;
            }
        }
    }
    
    /// <summary>
    /// Returns whether this puzzle has been solved
    /// </summary>
    /// <returns>True if the puzzle is solved, false otherwise</returns>
    public bool IsPuzzleSolved()
    {
        return isPuzzleSolved;
    }

    void OnDrawGizmosSelected()
    {
        // Draw the interaction distance
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
    
    /// <summary>
    /// Converts all hole tiles to floor tiles by calling the DungeonGenerator's conversion method.
    /// This is called when puzzle4 is solved.
    /// </summary>
    void ConvertHoleTilesToFloorTiles()
    {
        Debug.Log("CrackedWall4: Converting hole tiles to floor tiles...");
        
        // Find the DungeonGenerator in the scene
        DungeonGenerator dungeonGenerator = FindObjectOfType<DungeonGenerator>();
        if (dungeonGenerator != null)
        {
            dungeonGenerator.ConvertHoleTilesToFloorTiles();
        }
        else
        {
            Debug.LogError("CrackedWall4: Could not find DungeonGenerator in the scene!");
        }
    }
}
