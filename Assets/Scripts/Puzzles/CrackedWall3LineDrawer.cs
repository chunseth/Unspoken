using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Draws a line from the player to the nearest CarryableObject when 'E' is pressed near CrackedWall3.
/// The line is displayed for 10 seconds.
/// </summary>
public class CrackedWall3LineDrawer : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Distance the player must be within to trigger the line drawing")]
    public float interactionDistance = 3f;
    [Tooltip("Key to press to draw the line")]
    public KeyCode interactionKey = KeyCode.E;
    
    [Header("Line Settings")]
    [Tooltip("Duration the line is displayed (in seconds)")]
    public float lineDuration = 10f;
    [Tooltip("Line thickness in pixels")]
    public float lineThickness = 3f;
    [Tooltip("Color of the line")]
    public Color lineColor = Color.yellow;
    
    [Header("References")]
    [Tooltip("Reference to the player GameObject")]
    public GameObject player;
    [Tooltip("Reference to CrackedWall3")]
    public CrackedWall3 crackedWall3;
    
    // Private variables
    private bool playerInRange = false;
    private bool lineIsActive = false;
    private UILineRenderer lineRenderer;
    private Canvas canvas;
    private RectTransform canvasRectTransform;
    private Camera mainCamera;
    
    void Start()
    {
        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("CrackedWall3LineDrawer: No player found with 'Player' tag. Please assign the player reference manually.");
            }
        }
        
        // Find CrackedWall3 if not assigned
        if (crackedWall3 == null)
        {
            crackedWall3 = FindObjectOfType<CrackedWall3>();
            if (crackedWall3 == null)
            {
                Debug.LogWarning("CrackedWall3LineDrawer: No CrackedWall3 found in scene. Please assign the reference manually.");
            }
        }
        
        // Get main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("CrackedWall3LineDrawer: No main camera found!");
        }
        
        // Setup UI components
        SetupUIComponents();
    }
    
    void Update()
    {
        if (player == null || crackedWall3 == null) return;
        
        // Check if player is in range
        CheckPlayerDistance();
        
        // Handle input
        if (playerInRange && Input.GetKeyDown(interactionKey) && !lineIsActive)
        {
            DrawLineToCarryableObject();
        }
    }
    
    void CheckPlayerDistance()
    {
        float distance = Vector2.Distance(transform.position, player.transform.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= interactionDistance;
        
        // Log when entering/leaving range
        if (playerInRange != wasInRange)
        {
            if (playerInRange)
            {
                Debug.Log("CrackedWall3LineDrawer: Player entered interaction range");
            }
            else
            {
                Debug.Log("CrackedWall3LineDrawer: Player exited interaction range");
            }
        }
    }
    
    void SetupUIComponents()
    {
        // Find or create canvas
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            // Create canvas if none exists
            GameObject canvasGO = new GameObject("LineCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("CrackedWall3LineDrawer: Created new canvas for line rendering");
        }
        
        canvasRectTransform = canvas.GetComponent<RectTransform>();
        
        // Create line renderer GameObject
        GameObject lineGO = new GameObject("CrackedWall3Line");
        lineGO.transform.SetParent(canvas.transform, false);
        
        // Add CanvasRenderer component (required for UI graphics)
        lineGO.AddComponent<CanvasRenderer>();
        
        // Add UILineRenderer component
        lineRenderer = lineGO.AddComponent<UILineRenderer>();
        lineRenderer.Thickness = lineThickness;
        lineRenderer.color = lineColor;
        
        // Set RectTransform
        RectTransform lineRectTransform = lineGO.GetComponent<RectTransform>();
        lineRectTransform.anchorMin = Vector2.zero;
        lineRectTransform.anchorMax = Vector2.one;
        lineRectTransform.offsetMin = Vector2.zero;
        lineRectTransform.offsetMax = Vector2.zero;
        
        // Initially hide the line
        lineRenderer.enabled = false;
        
        Debug.Log("CrackedWall3LineDrawer: UI components setup complete");
    }
    
    void DrawLineToCarryableObject()
    {
        // Find the nearest CarryableObject
        CarryableObject nearestObject = FindNearestCarryableObject();
        
        if (nearestObject == null)
        {
            Debug.Log("CrackedWall3LineDrawer: No CarryableObject found in scene");
            return;
        }
        
        Debug.Log($"CrackedWall3LineDrawer: Drawing line to {nearestObject.name}");
        
        // Start the line drawing coroutine
        StartCoroutine(DrawLineCoroutine(nearestObject));
    }
    
    CarryableObject FindNearestCarryableObject()
    {
        CarryableObject[] carryableObjects = FindObjectsOfType<CarryableObject>();
        
        if (carryableObjects.Length == 0)
        {
            return null;
        }
        
        CarryableObject nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (CarryableObject obj in carryableObjects)
        {
            float distance = Vector2.Distance(player.transform.position, obj.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = obj;
            }
        }
        
        return nearest;
    }
    
    IEnumerator DrawLineCoroutine(CarryableObject targetObject)
    {
        lineIsActive = true;
        lineRenderer.enabled = true;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < lineDuration && targetObject != null)
        {
            // Update line points
            UpdateLinePoints(targetObject);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Hide the line
        lineRenderer.enabled = false;
        lineIsActive = false;
        
        Debug.Log("CrackedWall3LineDrawer: Line drawing completed");
    }
    
    void UpdateLinePoints(CarryableObject targetObject)
    {
        if (lineRenderer == null || mainCamera == null || player == null || targetObject == null) return;
        
        // Convert world positions to screen positions
        Vector3 playerScreenPos = mainCamera.WorldToScreenPoint(player.transform.position);
        Vector3 targetScreenPos = mainCamera.WorldToScreenPoint(targetObject.transform.position);
        
        // Convert screen positions to canvas positions
        Vector2 playerCanvasPos;
        Vector2 targetCanvasPos;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform, playerScreenPos, null, out playerCanvasPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform, targetScreenPos, null, out targetCanvasPos);
        
        // Update line points
        lineRenderer.Points.Clear();
        lineRenderer.Points.Add(playerCanvasPos);
        lineRenderer.Points.Add(targetCanvasPos);
        
        // Force the line to redraw
        lineRenderer.SetVerticesDirty();
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
    
    // Public method to check if line is currently active
    public bool IsLineActive()
    {
        return lineIsActive;
    }
    
    // Public method to manually trigger line drawing (for testing)
    public void TriggerLineDrawing()
    {
        if (!lineIsActive)
        {
            DrawLineToCarryableObject();
        }
    }
}
