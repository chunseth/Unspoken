using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;

/// <summary>
/// Manages a 3x3 grid of spell beams that can be toggled on/off by clicking on specific image components.
/// Beams shoot towards the center of the grid from their respective positions.
/// GameObject 1/Image 1 is inactive and does nothing when clicked.
/// </summary>
public class SpellBeamGridController : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("The parent GameObject containing the spell grid")]
    public GameObject spellGrid2;
    [Tooltip("The center position of the 3x3 grid")]
    public Vector2 gridCenter = Vector2.zero;
    [Tooltip("Size of each grid cell")]
    public float cellSize = 1f;
    
    [Header("Beam Settings")]
    [Tooltip("Prefab for the beam visual effect")]
    public GameObject beamPrefab;
    [Tooltip("Speed of the beam animation")]
    public float beamSpeed = 5f;
    [Tooltip("Duration the beam stays active")]
    public float beamDuration = 2f;
    [Tooltip("Color of active beams")]
    public Color beamColor = Color.cyan;
    
    [Header("Visual Settings")]
    [Tooltip("Line renderer material for beams")]
    public Material beamMaterial;
    [Tooltip("Width of the beam lines")]
    public float beamWidth = 0.1f;
    
    [Header("Interaction Settings")]
    [Tooltip("Radius around each image for click detection (in pixels)")]
    public float clickRadius = 30f;
    
    // Dictionary to store active beams for each grid position
    private Dictionary<(int, int), GameObject> activeBeams = new Dictionary<(int, int), GameObject>();
    
    // Dictionary to store the Image components for each grid position
    private Dictionary<(int, int), Image> gridImages = new Dictionary<(int, int), Image>();
    
    // Event for pattern completion
    public event Action<string> OnPatternCompleted;
    
    void Start()
    {
        InitializeGrid();
    }
    
    void Update()
    {
        // Check for mouse clicks on the spell grid
        if (Input.GetMouseButtonDown(0))
        {
            CheckForGridClicks();
        }
    }
    
    /// <summary>
    /// Initializes the 3x3 grid and sets up click handlers for each position
    /// </summary>
    void InitializeGrid()
    {
        if (spellGrid2 == null)
        {
            Debug.LogError("SpellGridController: spellGrid2 reference is not assigned!");
            return;
        }
        
        // Check if the spells child exists
        Transform spellsTransform = spellGrid2.transform.Find("Spells");
        if (spellsTransform == null)
        {
            Debug.LogError($"SpellBeamGridController: 'spells' child not found under {spellGrid2.name}!");
            return;
        }
        
        // Find all the spells GameObjects (0-2) and their Image components (0-2)
        for (int gameObjectIndex = 0; gameObjectIndex < 3; gameObjectIndex++)
        {
            Transform spellTransform = spellGrid2.transform.Find($"Spells/GameObject {gameObjectIndex}");
            if (spellTransform != null)
            {
                for (int imageIndex = 0; imageIndex < 3; imageIndex++)
                {
                    // Skip GameObject 1/Image 1 as it should be inactive
                    if (gameObjectIndex == 1 && imageIndex == 1)
                    {
                        continue;
                    }
                    
                    // Find the Image component for this position
                    Transform imageTransform = spellTransform.Find($"Image {imageIndex}");
                    if (imageTransform != null)
                    {
                        Image imageComponent = imageTransform.GetComponent<Image>();
                        if (imageComponent != null)
                        {
                            gridImages[(gameObjectIndex, imageIndex)] = imageComponent;
                        }
                        else
                        {
                            Debug.LogError($"SpellBeamGridController: Image component not found on Image {imageIndex} GameObject in GameObject {gameObjectIndex}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"SpellBeamGridController: Image {imageIndex} not found in GameObject {gameObjectIndex}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"SpellGridController: GameObject {gameObjectIndex} not found in spells");
            }
        }
    }
    
    /// <summary>
    /// Toggles the beam for a specific grid position
    /// </summary>
    /// <param name="gameObjectIndex">GameObject index in the grid (0-2)</param>
    /// <param name="imageIndex">Image index in the grid (0-2)</param>
    public void ToggleBeam(int gameObjectIndex, int imageIndex)
    {
        // GameObject 1/Image 1 is inactive
        if (gameObjectIndex == 1 && imageIndex == 1)
        {
            return;
        }
        
        var position = (gameObjectIndex, imageIndex);
        
        if (activeBeams.ContainsKey(position))
        {
            // Beam is active, deactivate it
            DeactivateBeam(gameObjectIndex, imageIndex);
        }
        else
        {
            // Beam is inactive, activate it
            ActivateBeam(gameObjectIndex, imageIndex);
        }
        
        // Trigger pattern completion event
        TriggerPatternCompleted();
    }
    
    /// <summary>
    /// Activates a beam from the specified grid position towards the center
    /// </summary>
    /// <param name="gameObjectIndex">GameObject index in the grid (0-2)</param>
    /// <param name="imageIndex">Image index in the grid (0-2)</param>
    void ActivateBeam(int gameObjectIndex, int imageIndex)
    {
        var position = (gameObjectIndex, imageIndex);
        
        // Calculate the start position of the beam
        Vector2 startPos = GetGridPosition(gameObjectIndex, imageIndex);
        Vector2 centerPos = GetGridCenterPosition();
        
        Debug.Log($"SpellBeamGridController: Creating beam from {startPos} to {centerPos}");
        
        // Create the beam GameObject
        GameObject beam = CreateBeam(startPos, centerPos);
        
        // Store the active beam
        activeBeams[position] = beam;
        
        // Update the visual state of the grid image
        if (gridImages.ContainsKey(position))
        {
            gridImages[position].color = beamColor;
        }
        
        Debug.Log($"SpellGridController: Activated beam from GameObject {gameObjectIndex}/Image {imageIndex}");
    }
    
    /// <summary>
    /// Deactivates a beam from the specified grid position
    /// </summary>
    /// <param name="gameObjectIndex">GameObject index in the grid (0-2)</param>
    /// <param name="imageIndex">Image index in the grid (0-2)</param>
    void DeactivateBeam(int gameObjectIndex, int imageIndex)
    {
        var position = (gameObjectIndex, imageIndex);
        
        if (activeBeams.ContainsKey(position))
        {
            // Destroy the beam GameObject
            if (activeBeams[position] != null)
            {
                Destroy(activeBeams[position]);
            }
            
            // Remove from active beams
            activeBeams.Remove(position);
            
            // Reset the visual state of the grid image
            if (gridImages.ContainsKey(position))
            {
                gridImages[position].color = Color.white;
            }
            
            Debug.Log($"SpellGridController: Deactivated beam from GameObject {gameObjectIndex}/Image {imageIndex}");
        }
    }
    
    /// <summary>
    /// Creates a beam GameObject with visual effects
    /// </summary>
    /// <param name="startPos">Starting position of the beam</param>
    /// <param name="endPos">Ending position of the beam (center)</param>
    /// <returns>The created beam GameObject</returns>
    GameObject CreateBeam(Vector2 startPos, Vector2 endPos)
    {
        Debug.Log($"SpellBeamGridController: CreateBeam called with startPos: {startPos}, endPos: {endPos}");
        
        GameObject beam;
        
        if (beamPrefab != null)
        {
            Debug.Log($"SpellBeamGridController: Using beam prefab: {beamPrefab.name}");
            beam = Instantiate(beamPrefab, startPos, Quaternion.identity);
            
            // Check what components are on the prefab
            LineRenderer prefabLineRenderer = beam.GetComponent<LineRenderer>();
            if (prefabLineRenderer != null)
            {
                Debug.Log($"SpellBeamGridController: Prefab has LineRenderer - width: {prefabLineRenderer.startWidth}, color: {prefabLineRenderer.startColor}");
                Debug.Log($"SpellBeamGridController: Prefab LineRenderer positions: start={prefabLineRenderer.GetPosition(0)}, end={prefabLineRenderer.GetPosition(1)}");
            }
            else
            {
                Debug.LogWarning($"SpellBeamGridController: Prefab does not have LineRenderer component!");
                Component[] components = beam.GetComponents<Component>();
                Debug.Log($"SpellBeamGridController: Prefab components:");
                foreach (Component comp in components)
                {
                    Debug.Log($"  - {comp.GetType().Name}");
                }
            }
        }
        else
        {
            // Create a default beam with UILineRenderer (UI-based)
            beam = new GameObject("SpellBeam");
            
            // Make it a child of the Canvas to ensure proper UI rendering
            Canvas canvas = spellGrid2.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                beam.transform.SetParent(canvas.transform, false);
            }
            else
            {
                Debug.LogWarning($"SpellBeamGridController: No Canvas found, beam may not render properly");
            }
            
            // Add CanvasRenderer component (required for UI Graphics)
            CanvasRenderer canvasRenderer = beam.AddComponent<CanvasRenderer>();
            
            // Add UILineRenderer component
            UILineRenderer uiLineRenderer = beam.AddComponent<UILineRenderer>();
            
            // Set UI line properties
            uiLineRenderer.Thickness = beamWidth * 10f; // Convert to pixels
            uiLineRenderer.color = beamColor;
            
            // Ensure the color is visible (force alpha to 1.0 if it's too low)
            Color visibleColor = beamColor;
            if (visibleColor.a < 0.1f)
            {
                visibleColor.a = 1.0f;
            }
            uiLineRenderer.color = visibleColor;
            
            // Convert world coordinates to UI coordinates
            Vector2 uiStartPos = ConvertWorldToUIPosition(startPos, canvas);
            Vector2 uiEndPos = ConvertWorldToUIPosition(endPos, canvas);
            
            // Set initial points (will be updated by SpellBeamBehavior)
            uiLineRenderer.Points = new List<Vector2> { uiStartPos, uiEndPos };
        }
        
        // Add beam behavior component
        SpellBeamBehavior beamBehavior = beam.AddComponent<SpellBeamBehavior>();
        if (beamBehavior == null)
        {
            Debug.LogError($"SpellBeamGridController: Failed to add SpellBeamBehavior component!");
            Destroy(beam);
            return null;
        }
        
        beamBehavior.Initialize(startPos, endPos, beamSpeed, beamDuration);
        
        return beam;
    }
    
    /// <summary>
    /// Gets the actual world position of an Image component
    /// </summary>
    /// <param name="gameObjectIndex">GameObject index in the grid (0-2)</param>
    /// <param name="imageIndex">Image index in the grid (0-2)</param>
    /// <returns>World position of the Image component</returns>
    Vector2 GetGridPosition(int gameObjectIndex, int imageIndex)
    {
        var position = (gameObjectIndex, imageIndex);
        
        if (gridImages.ContainsKey(position))
        {
            Image imageComponent = gridImages[position];
            if (imageComponent != null)
            {
                // Convert UI position to world position
                Vector3 worldPos = imageComponent.transform.position;
                return new Vector2(worldPos.x, worldPos.y);
            }
        }
        
        // Fallback to calculated position if Image component not found
        Debug.LogWarning($"SpellBeamGridController: Image component not found for position ({gameObjectIndex}, {imageIndex}), using fallback position");
        return CalculateFallbackPosition(gameObjectIndex, imageIndex);
    }
    
    /// <summary>
    /// Calculates fallback position when Image component is not available
    /// </summary>
    /// <param name="gameObjectIndex">GameObject index in the grid (0-2)</param>
    /// <param name="imageIndex">Image index in the grid (0-2)</param>
    /// <returns>Calculated world position</returns>
    Vector2 CalculateFallbackPosition(int gameObjectIndex, int imageIndex)
    {
        // Convert 0-2 grid to -1 to 1 range
        float worldX = (gameObjectIndex - 1) * cellSize;
        float worldY = (imageIndex - 1) * cellSize;
        
        return new Vector2(worldX, worldY);
    }
    
    /// <summary>
    /// Converts world coordinates to UI coordinates for the Canvas
    /// </summary>
    /// <param name="worldPos">World position to convert</param>
    /// <param name="canvas">Canvas to convert to</param>
    /// <returns>UI position in local Canvas coordinates</returns>
    public static Vector2 ConvertWorldToUIPosition(Vector2 worldPos, Canvas canvas)
    {
        if (canvas == null)
        {
            Debug.LogWarning("SpellBeamGridController: Canvas is null, cannot convert coordinates");
            return worldPos;
        }
        
        // Convert world position to screen position
        Vector3 screenPos = Camera.main.WorldToScreenPoint(new Vector3(worldPos.x, worldPos.y, 0));
        
        // Convert screen position to local UI position
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform, 
            screenPos, 
            canvas.worldCamera, 
            out localPos
        );
        
        Debug.Log($"SpellBeamGridController: Converting world {worldPos} -> screen {screenPos} -> UI {localPos}");
        return localPos;
    }
    
    /// <summary>
    /// Calculates the actual center position of the grid based on UI positions
    /// </summary>
    /// <returns>World position of the grid center</returns>
    Vector2 GetGridCenterPosition()
    {
        // Try to find the center position by looking at GameObject 1/Image 1 (which should be the center)
        Transform centerTransform = spellGrid2.transform.Find("Spells/GameObject 1/Image 1");
        if (centerTransform != null)
        {
            Vector3 centerPos = centerTransform.position;
            Debug.Log($"SpellBeamGridController: Using actual center position: {centerPos}");
            return new Vector2(centerPos.x, centerPos.y);
        }
        
        // Fallback: calculate center from available positions
        Vector2 totalPos = Vector2.zero;
        int count = 0;
        
        foreach (var kvp in gridImages)
        {
            if (kvp.Value != null)
            {
                totalPos += new Vector2(kvp.Value.transform.position.x, kvp.Value.transform.position.y);
                count++;
            }
        }
        
        if (count > 0)
        {
            Vector2 calculatedCenter = totalPos / count;
            Debug.Log($"SpellBeamGridController: Calculated center from {count} positions: {calculatedCenter}");
            return calculatedCenter;
        }
        
        // Final fallback: use the spellGrid2 position
        if (spellGrid2 != null)
        {
            Vector3 fallbackCenter = spellGrid2.transform.position;
            Debug.Log($"SpellBeamGridController: Using fallback center (spellGrid2 position): {fallbackCenter}");
            return new Vector2(fallbackCenter.x, fallbackCenter.y);
        }
        
        Debug.LogWarning("SpellBeamGridController: No center position found, using default gridCenter");
        return gridCenter;
    }
    
    /// <summary>
    /// Checks for mouse clicks within the radius of any Image component
    /// </summary>
    private void CheckForGridClicks()
    {
        Debug.Log($"SpellBeamGridController: CheckForGridClicks called - Mouse position: {Input.mousePosition}");
        
        // For now, let's ignore UI conflicts and just check for clicks
        // TODO: Add proper UI conflict detection later
        Debug.Log("SpellBeamGridController: Proceeding with click detection (ignoring UI conflicts for now)");
        
        // Get mouse position in screen coordinates
        Vector2 mousePos = Input.mousePosition;
        
        Debug.Log($"SpellBeamGridController: Checking {gridImages.Count} grid positions for clicks");
        
        // Check each Image component to see if the click is within its radius
        foreach (var kvp in gridImages)
        {
            var position = kvp.Key;
            Image imageComponent = kvp.Value;
            
            if (imageComponent != null)
            {
                // Get the Canvas for proper coordinate conversion
                Canvas canvas = imageComponent.GetComponentInParent<Canvas>();
                
                // Convert Image position to screen coordinates
                Vector3 imageScreenPos;
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    // For Screen Space Overlay, use RectTransform position
                    RectTransform rectTransform = imageComponent.rectTransform;
                    imageScreenPos = RectTransformUtility.WorldToScreenPoint(null, rectTransform.position);
                }
                else
                {
                    // For other render modes, use Camera conversion
                    imageScreenPos = Camera.main.WorldToScreenPoint(imageComponent.transform.position);
                }
                
                Vector2 imagePos2D = new Vector2(imageScreenPos.x, imageScreenPos.y);
                
                // Calculate distance between mouse and image
                float distance = Vector2.Distance(mousePos, imagePos2D);
                
                // Check if click is within radius
                if (distance <= clickRadius)
                {
                    ToggleBeam(position.Item1, position.Item2);
                    return; // Only handle one click at a time
                }
            }
        }
    }
    
    /// <summary>
    /// Gets the current state of all beams
    /// </summary>
    /// <returns>Dictionary of active beam positions</returns>
    public Dictionary<(int, int), bool> GetBeamStates()
    {
        Dictionary<(int, int), bool> states = new Dictionary<(int, int), bool>();
        
        for (int gameObjectIndex = 0; gameObjectIndex < 3; gameObjectIndex++)
        {
            for (int imageIndex = 0; imageIndex < 3; imageIndex++)
            {
                // Skip GameObject 1/Image 1 as it's inactive
                if (gameObjectIndex == 1 && imageIndex == 1)
                {
                    continue;
                }
                
                states[(gameObjectIndex, imageIndex)] = activeBeams.ContainsKey((gameObjectIndex, imageIndex));
            }
        }
        
        return states;
    }
    
    /// <summary>
    /// Clears all active beams
    /// </summary>
    public void ClearAllBeams()
    {
        foreach (var beam in activeBeams.Values)
        {
            if (beam != null)
            {
                Destroy(beam);
            }
        }
        
        activeBeams.Clear();
        
        // Reset all grid image colors
        foreach (var image in gridImages.Values)
        {
            if (image != null)
            {
                image.color = Color.white;
            }
        }
    }
    
    /// <summary>
    /// Called when the puzzle is closed or solved - cleans up all beams
    /// </summary>
    public void OnPuzzleClosed()
    {
        ClearAllBeams();
    }
    
    /// <summary>
    /// Called when the puzzle is solved - cleans up all beams
    /// </summary>
    public void OnPuzzleSolved()
    {
        ClearAllBeams();
    }
    
    /// <summary>
    /// Triggers the pattern completed event with the current active beam pattern
    /// </summary>
    private void TriggerPatternCompleted()
    {
        string pattern = GetPatternString();
        OnPatternCompleted?.Invoke(pattern);
    }
    
    /// <summary>
    /// Converts the active beams to a numerical string pattern
    /// </summary>
    /// <returns>String representing active beam positions in numerical order</returns>
    private string GetPatternString()
    {
        List<int> activePositions = new List<int>();
        
        // Define the mapping from grid positions to numerical indices
        // This creates a 3x3 grid where each position has a unique number
        // GameObject 0: positions 0, 1, 2
        // GameObject 1: positions 3, 4, 5 (position 4 is inactive)
        // GameObject 2: positions 6, 7, 8
        
        for (int gameObjectIndex = 0; gameObjectIndex < 3; gameObjectIndex++)
        {
            for (int imageIndex = 0; imageIndex < 3; imageIndex++)
            {
                // Skip GameObject 1/Image 1 as it's inactive
                if (gameObjectIndex == 1 && imageIndex == 1)
                {
                    continue;
                }
                
                var position = (gameObjectIndex, imageIndex);
                if (activeBeams.ContainsKey(position))
                {
                    // Calculate the numerical position
                    int numericalPosition = gameObjectIndex * 3 + imageIndex;
                    activePositions.Add(numericalPosition);
                }
            }
        }
        
        // Sort the positions in numerical order
        activePositions.Sort();
        
        // Convert to string
        return string.Join("", activePositions);
    }
}



/// <summary>
/// Component that handles beam visual effects and behavior
/// </summary>
public class SpellBeamBehavior : MonoBehaviour
{
    private Vector2 startPos;
    private Vector2 endPos;
    private float speed;
    private float duration;
    private float timer;
    private UILineRenderer uiLineRenderer;
    private LineRenderer lineRenderer; // Keep for compatibility
    
    public void Initialize(Vector2 start, Vector2 end, float beamSpeed, float beamDuration)
    {
        startPos = start;
        endPos = end;
        speed = beamSpeed;
        duration = beamDuration;
        timer = 0f;
        
        Debug.Log($"SpellBeamBehavior: Initializing beam from {start} to {end} with speed {beamSpeed}, duration {beamDuration}");
        
        // Try to get UILineRenderer first (preferred for UI)
        uiLineRenderer = GetComponent<UILineRenderer>();
        if (uiLineRenderer != null)
        {
            Debug.Log($"SpellBeamBehavior: UILineRenderer found, starting animation");
            // Animate the beam from start to end
            StartCoroutine(AnimateUIBeam());
        }
        else
        {
            // Fallback to LineRenderer for world-space beams
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                Debug.Log($"SpellBeamBehavior: LineRenderer found, starting animation");
                // Animate the beam from start to end
                StartCoroutine(AnimateBeam());
            }
            else
            {
                Debug.LogError($"SpellBeamBehavior: No line renderer component found on {gameObject.name}!");
            }
        }
    }
    
    System.Collections.IEnumerator AnimateBeam()
    {
        Debug.Log($"SpellBeamBehavior: Starting beam animation");
        
        float distance = Vector2.Distance(startPos, endPos);
        float animationTime = distance / speed;
        float currentTime = 0f;
        
        Debug.Log($"SpellBeamBehavior: Beam distance: {distance}, animation time: {animationTime}s");
        
        while (currentTime < animationTime)
        {
            float progress = currentTime / animationTime;
            Vector2 currentEnd = Vector2.Lerp(startPos, endPos, progress);
            
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, currentEnd);
            
            currentTime += Time.deltaTime;
            yield return null;
        }
        
        // Set final position
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
        
        Debug.Log($"SpellBeamBehavior: Beam animation complete, keeping active for {duration}s");
        
        // Keep beam active for duration
        yield return new WaitForSeconds(duration);
        
        // Fade out
        float fadeTime = 0.5f;
        float fadeTimer = 0f;
        Color startColor = lineRenderer.startColor;
        Color endColor = lineRenderer.endColor;
        
        while (fadeTimer < fadeTime)
        {
            float alpha = Mathf.Lerp(1f, 0f, fadeTimer / fadeTime);
            lineRenderer.startColor = new Color(startColor.r, startColor.g, startColor.b, alpha);
            lineRenderer.endColor = new Color(endColor.r, endColor.g, endColor.b, alpha);
            
            fadeTimer += Time.deltaTime;
            yield return null;
        }
        
        // Destroy the beam
        Destroy(gameObject);
    }
    
    System.Collections.IEnumerator AnimateUIBeam()
    {
        Debug.Log($"SpellBeamBehavior: Starting UI beam animation");
        
        // Get the Canvas for coordinate conversion
        Canvas canvas = uiLineRenderer.GetComponentInParent<Canvas>();
        
        float distance = Vector2.Distance(startPos, endPos);
        float animationTime = distance / speed;
        float currentTime = 0f;
        
        Debug.Log($"SpellBeamBehavior: UI Beam distance: {distance}, animation time: {animationTime}s");
        
        while (currentTime < animationTime)
        {
            float progress = currentTime / animationTime;
            Vector2 currentEnd = Vector2.Lerp(startPos, endPos, progress);
            
            // Convert world coordinates to UI coordinates
            Vector2 uiStartPos = SpellBeamGridController.ConvertWorldToUIPosition(startPos, canvas);
            Vector2 uiCurrentEnd = SpellBeamGridController.ConvertWorldToUIPosition(currentEnd, canvas);
            
            // Update UILineRenderer points
            uiLineRenderer.Points = new List<Vector2> { uiStartPos, uiCurrentEnd };
            uiLineRenderer.SetVerticesDirty(); // Force UI update
            
            currentTime += Time.deltaTime;
            yield return null;
        }
        
        // Set final position
        Vector2 uiFinalStartPos = SpellBeamGridController.ConvertWorldToUIPosition(startPos, canvas);
        Vector2 uiFinalEndPos = SpellBeamGridController.ConvertWorldToUIPosition(endPos, canvas);
        uiLineRenderer.Points = new List<Vector2> { uiFinalStartPos, uiFinalEndPos };
        uiLineRenderer.SetVerticesDirty();
        
        Debug.Log($"SpellBeamBehavior: UI Beam animation complete, keeping active for {duration}s");
        
        // Keep beam active for duration
        yield return new WaitForSeconds(duration);
        
        // Fade out
        float fadeTime = 0.5f;
        float fadeTimer = 0f;
        Color startColor = uiLineRenderer.color;
        
        while (fadeTimer < fadeTime)
        {
            float alpha = Mathf.Lerp(1f, 0f, fadeTimer / fadeTime);
            uiLineRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            uiLineRenderer.SetVerticesDirty();
            
            fadeTimer += Time.deltaTime;
            yield return null;
        }
        
        // Destroy the beam
        Destroy(gameObject);
    }
}
