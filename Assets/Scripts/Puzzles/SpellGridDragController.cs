using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;



/// <summary>
/// Allows the player to drag across a 3×3 grid to form a spell pattern using a drag gesture.
/// This version creates a UI line (via UILineRenderer) that extends from the first activated cell to the pointer's current location.
/// When the pointer comes close to another cell, that cell's center is locked in as a point in the line.
/// On pointer up, the cell indices are concatenated into a pattern which is used to look up the spell.
/// Attach this script to a dedicated UI panel (with a RectTransform) that defines your 3×3 grid area.
/// </summary>
[RequireComponent(typeof(Image))]
public class SpellGridDragController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    [Tooltip("Reference to the inner grid container where lines will be drawn")]
    public RectTransform innerGridContainer;
    

    
    [Header("UI Line Settings")]
    [Tooltip("Prefab reference for the UILineRenderer (a UI GameObject with the UILineRenderer component attached).")]
    public GameObject uiLinePrefab;    
    [Tooltip("Thickness of the UI line.")]
    public float lineThickness = 4f;
    
    [Header("Grid Settings")]
    [Tooltip("The activation radius (in local units) around each dot (center) required for activation.")]
    public float activationRadius = 30f;
    [Tooltip("Additional tolerance radius to snap the initial press to a cell, even if the press isn't exactly on the activation point.")]
    public float startTolerance = 40f;
    public float destroyLineTime;


    // List of activated cell indices over the current gesture.
    private List<int> currentPattern = new List<int>();
    // List of cell-center positions (in local coordinates) that have been activated.
    private List<Vector2> activatedCellPositions = new List<Vector2>();
    
    // Cached RectTransform of the grid panel.
    private RectTransform rectTransform;
    // Precomputed cell centers for the 3×3 grid (in local coordinates).
    private Vector2[] cellCentersLocal = new Vector2[9];
    
    // To avoid duplicate addition.
    private int lastCellIndex = -1;
    
    // The current UI line instance drawn during the drag.
    private UILineRenderer currentUILine;

    private Vector2[] cellBoundsMin = new Vector2[9];  // Add these as class fields
    private Vector2[] cellBoundsMax = new Vector2[9];  // to store cell boundaries

    // Add this field to track if we've started a valid gesture
    private bool isValidGesture = false;

    // Add this dictionary to store valid connections for each cell
    private Dictionary<int, HashSet<int>> validConnections = new Dictionary<int, HashSet<int>>()
    {
        {0, new HashSet<int>{1,2,3,4,5,6,7,8}},
        {1, new HashSet<int>{0,2,3,4,5,6,7,8}},
        {2, new HashSet<int>{0,1,3,4,5,6,7,8}},
        {3, new HashSet<int>{0,1,2,4,5,6,7,8}},
        {4, new HashSet<int>{0,1,2,3,5,6,7,8}},
        {5, new HashSet<int>{0,1,2,3,4,6,7,8}},
        {6, new HashSet<int>{0,1,2,3,4,5,7,8}},
        {7, new HashSet<int>{0,1,2,3,4,5,6,8}},
        {8, new HashSet<int>{0,1,2,3,4,5,6,7}}
    };

    // Add this dictionary to store intermediate cells that should be included
    private Dictionary<(int, int), int> intermediatePoints = new Dictionary<(int, int), int>()
    {
        {(0,2), 1}, {(2,0), 1},   // Horizontal top
        {(3,5), 4}, {(5,3), 4},   // Horizontal middle
        {(6,8), 7}, {(8,6), 7},   // Horizontal bottom
        {(0,6), 3}, {(6,0), 3},   // Vertical left
        {(1,7), 4}, {(7,1), 4},   // Vertical middle
        {(2,8), 5}, {(8,2), 5},   // Vertical right
        {(0,8), 4}, {(8,0), 4},   // Diagonal
        {(2,6), 4}, {(6,2), 4}    // Diagonal
    };

    // Instead, we'll just track the previous cell
    private int previousCellIndex = -1;

    // Add a field to track the original cell when an intermediate point is used
    private int originalCellIndex = -1;

    // Add this to track used paths between cells
    private HashSet<(int, int)> usedPaths = new HashSet<(int, int)>();

    // Event that fires when a pattern is completed
    public event Action<string> OnPatternCompleted;



    void Awake()
    {
        // If innerGridContainer is not set, default to this object's RectTransform
        if (innerGridContainer == null)
        {
            innerGridContainer = GetComponent<RectTransform>();
        }
        
        rectTransform = GetComponent<RectTransform>();
        
        // Ensure we have a transparent but raycast-able image
        Image image = GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0, 0, 0, 0); // Fully transparent
            image.raycastTarget = true;
        }
        
        // Use innerGridContainer's rect for cell calculations instead of this object's rect
        Rect rect = innerGridContainer.rect;
        float cellWidth = rect.width / 3f;
        float cellHeight = rect.height / 3f;
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int cellIndex = row * 3 + col;
                // Compute the boundaries of the cell.
                float left   = rect.xMin + col * cellWidth;
                float right  = rect.xMin + (col + 1) * cellWidth;
                float top    = rect.yMax - row * cellHeight;
                float bottom = rect.yMax - (row + 1) * cellHeight;
                
                cellBoundsMin[cellIndex] = new Vector2(left, bottom);
                cellBoundsMax[cellIndex] = new Vector2(right, top);

                Vector2 activationPoint;
                if (row == 0)
                {
                    // Top row: use the top edge.
                    if (col == 0)
                        activationPoint = new Vector2(left, top);       // Top left
                    else if (col == 1)
                        activationPoint = new Vector2((left + right) / 2, top); // Top middle
                    else
                        activationPoint = new Vector2(right, top);        // Top right
                }
                else if (row == 1)
                {
                    // Middle row: use the vertical midpoint.
                    if (col == 0)
                        activationPoint = new Vector2(left, (top + bottom) / 2);       // Middle left
                    else if (col == 1)
                        activationPoint = new Vector2((left + right) / 2, (top + bottom) / 2); // Center
                    else
                        activationPoint = new Vector2(right, (top + bottom) / 2);      // Middle right
                }
                else // row == 2
                {
                    // Bottom row: use the bottom edge.
                    if (col == 0)
                        activationPoint = new Vector2(left, bottom);    // Bottom left
                    else if (col == 1)
                        activationPoint = new Vector2((left + right) / 2, bottom); // Bottom middle
                    else
                        activationPoint = new Vector2(right, bottom);   // Bottom right
                }
                cellCentersLocal[cellIndex] = activationPoint;
            }
        }
    }

    /// <summary>
    /// Checks if the pointer (in local coordinates) is within activationRadius of any cell center.
    /// Returns the cell index if a cell is activated; otherwise, returns -1.
    /// </summary>
    private int GetActivatedCellIndex(Vector2 localPoint)
    {
        // For the initial press (when lastCellIndex is -1), check cell boundaries
        if (lastCellIndex == -1)
        {
            for (int i = 0; i < 9; i++)
            {
                // Check if point is within cell boundaries (plus tolerance)
                if (localPoint.x >= (cellBoundsMin[i].x - startTolerance) &&
                    localPoint.x <= (cellBoundsMax[i].x + startTolerance) &&
                    localPoint.y >= (cellBoundsMin[i].y - startTolerance) &&
                    localPoint.y <= (cellBoundsMax[i].y + startTolerance))
                {
                    return i;
                }
            }
            return -1;
        }
        
        // For subsequent points during the drag, use the activation radius around specific points
        for (int i = 0; i < cellCentersLocal.Length; i++)
        {
            if (Vector2.Distance(localPoint, cellCentersLocal[i]) <= activationRadius)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Instantiates the UI line object from the uiLinePrefab, which should have a UILineRenderer component.
    /// The instantiated object is made a child of this grid panel for correct positioning.
    /// </summary>
    private void CreateNewUILine()
    {
        if (uiLinePrefab == null)
        {
            Debug.LogError("uiLinePrefab is not assigned!");
            return;
        }
        // Instantiate as a child of the inner grid container instead of this object
        GameObject uiLineObj = Instantiate(uiLinePrefab, innerGridContainer, false);
        
        // Reset the RectTransform to ensure it matches the inner grid container's coordinate space
        RectTransform lineRect = uiLineObj.GetComponent<RectTransform>();
        if (lineRect != null)
        {
            lineRect.anchorMin = new Vector2(0, 0);
            lineRect.anchorMax = new Vector2(1, 1);
            lineRect.offsetMin = Vector2.zero;
            lineRect.offsetMax = Vector2.zero;
            lineRect.anchoredPosition = Vector2.zero;
        }
        
        // Get the UILineRenderer and initialize its settings.
        currentUILine = uiLineObj.GetComponent<UILineRenderer>();
        if (currentUILine != null)
        {
            currentUILine.Thickness = lineThickness;
            currentUILine.Points.Clear();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Reset state
        currentPattern.Clear();
        activatedCellPositions.Clear();
        lastCellIndex = -1;
        isValidGesture = false;
        usedPaths.Clear(); // Clear used paths when starting new gesture

        CreateNewUILine();

        // Convert all cell centers to screen space for comparison
        Vector2[] cellCentersScreen = new Vector2[cellCentersLocal.Length];
        for (int i = 0; i < cellCentersLocal.Length; i++)
        {
            Vector3 worldPos = innerGridContainer.TransformPoint(cellCentersLocal[i]);
            cellCentersScreen[i] = RectTransformUtility.WorldToScreenPoint(null, worldPos);
        }

        // Get click position in screen coordinates
        Vector2 screenPoint = eventData.position;

        // Check each cell's distance against the tolerance
        int activatedCell = -1;
        for (int i = 0; i < cellCentersScreen.Length; i++)
        {
            float dist = Vector2.Distance(screenPoint, cellCentersScreen[i]);
            
            // Only activate if within the start tolerance
            if (dist <= startTolerance)
            {
                activatedCell = i;
                break; // Take the first cell that's within tolerance
            }
        }

        // Only proceed if we found a cell within the tolerance
        if (activatedCell != -1)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(innerGridContainer, 
                eventData.position, eventData.pressEventCamera, out localPoint))
            {
                currentPattern.Add(activatedCell);
                previousCellIndex = -1;  // No previous cell for first activation
                lastCellIndex = activatedCell;
                activatedCellPositions.Add(cellCentersLocal[activatedCell]);
                UpdateUILine(eventData);
                isValidGesture = true;
            }
        }
        else
        {
            Debug.Log("No cell within start tolerance.");
            if (currentUILine != null)
            {
                Destroy(currentUILine.gameObject);
                currentUILine = null;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isValidGesture || currentUILine == null)
            return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(innerGridContainer, 
            eventData.position, eventData.pressEventCamera, out localPoint))
        {
            int cellIndex = GetActivatedCellIndex(localPoint);
            
            // Check that the new cell isn't the current cell, previous cell, or original cell
            if (cellIndex != -1 && 
                cellIndex != lastCellIndex && 
                cellIndex != previousCellIndex &&
                validConnections[lastCellIndex].Contains(cellIndex))
            {
                bool pathIsValid = true;
                
                // Check if there's an intermediate point to include
                var key = (lastCellIndex, cellIndex);
                if (intermediatePoints.ContainsKey(key))
                {
                    int intermediateCell = intermediatePoints[key];
                    // Check if either segment of the path is already used
                    if (IsPathUsed(lastCellIndex, intermediateCell) || 
                        IsPathUsed(intermediateCell, cellIndex))
                    {
                        pathIsValid = false;
                    }
                    else if (!currentPattern.Contains(intermediateCell))
                    {
                        // Add both segments of the path
                        AddUsedPath(lastCellIndex, intermediateCell);
                        AddUsedPath(intermediateCell, cellIndex);
                        
                        originalCellIndex = lastCellIndex;
                        previousCellIndex = lastCellIndex;
                        lastCellIndex = intermediateCell;
                        currentPattern.Add(intermediateCell);
                        activatedCellPositions.Add(cellCentersLocal[intermediateCell]);
                    }
                }
                else if (IsPathUsed(lastCellIndex, cellIndex))
                {
                    pathIsValid = false;
                }

                if (pathIsValid)
                {
                    if (!intermediatePoints.ContainsKey(key))
                    {
                        // Add the direct path if we didn't add intermediate paths
                        AddUsedPath(lastCellIndex, cellIndex);
                    }
                    
                    previousCellIndex = lastCellIndex;
                    lastCellIndex = cellIndex;
                    currentPattern.Add(cellIndex);
                    activatedCellPositions.Add(cellCentersLocal[cellIndex]);
                }
            }
            UpdateUILine(eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Only process up if we started with a valid gesture
        if (!isValidGesture)
            return;

        // Finalize the line so that it shows only the activated cell centers.
        UpdateUILine(null);

        // Build the pattern string from the activated cell indices.
        string patternString = string.Join("", currentPattern);

        // Log the pattern for debugging
        Debug.Log("Pattern completed: " + patternString);

        // Fire the pattern completed event
        OnPatternCompleted?.Invoke(patternString);

        Destroy(currentUILine.gameObject, destroyLineTime);
        ResetState();
    }

    private void ResetState()
    {
        currentPattern.Clear();
        activatedCellPositions.Clear();
        lastCellIndex = -1;
        previousCellIndex = -1;
        originalCellIndex = -1;  // Add this reset

        // Reset the valid gesture flag
        isValidGesture = false;
        usedPaths.Clear(); // Clear used paths when resetting state
    }

    /// <summary>
    /// Updates the points of the current UILineRenderer.
    /// If eventData is not null, the pointer's current position (in local space) is added as a dynamic endpoint.
    /// Otherwise, only the fixed activated points are used.
    /// </summary>
    /// <param name="eventData">Pointer event data (can be null).</param>
    private void UpdateUILine(PointerEventData eventData)
    {
        if (currentUILine == null)
            return;

        List<Vector2> newPoints = new List<Vector2>(activatedCellPositions);
        if (eventData != null)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(innerGridContainer,
                eventData.position, eventData.pressEventCamera, out localPoint))
            {
                newPoints.Add(localPoint);
            }
        }
        currentUILine.Points = newPoints;
        currentUILine.SetVerticesDirty();
    }

    // Update the Gizmos to show screen-space distances
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || cellCentersLocal == null || cellCentersLocal.Length == 0)
            return;

        Gizmos.color = Color.yellow;
        foreach (Vector2 center in cellCentersLocal)
        {
            Vector3 worldPos = rectTransform.TransformPoint(center);
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, worldPos);
            
            // Draw multiple circles to better visualize the tolerance area
            float[] radiusMultipliers = { 0.25f, 0.5f, 0.75f, 1f };
            foreach (float multiplier in radiusMultipliers)
            {
                Gizmos.DrawWireSphere(worldPos, startTolerance * multiplier);
            }
        }
    }

    // Helper method to add a path and its reverse to usedPaths
    private void AddUsedPath(int from, int to)
    {
        usedPaths.Add((from, to));
        usedPaths.Add((to, from));
    }

    // Helper method to check if a path is already used
    private bool IsPathUsed(int from, int to)
    {
        return usedPaths.Contains((from, to)) || usedPaths.Contains((to, from));
    }
}