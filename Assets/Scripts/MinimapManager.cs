using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the minimap display and tracks game elements like rooms, enemies, and exits.
/// </summary>
public class MinimapManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the minimap RawImage")]
    public RawImage minimapDisplay;
    
    [Tooltip("Reference to the player transform")]
    public Transform playerTransform;
    
    [Tooltip("Reference to the dungeon generator")]
    public DungeonGenerator dungeonGenerator;

    [Header("Icon Settings")]
    [Tooltip("Sprite to represent enemies on the minimap")]
    public Sprite enemyIcon;
    
    [Tooltip("Sprite to represent ladder/exit on the minimap")]
    public Sprite ladderIcon;
    
    [Tooltip("Sprite to represent the player on the minimap")]
    public Sprite playerIcon;

    [Header("Display Settings")]
    [Tooltip("Pixel size of each tile on the minimap")]
    public int pixelsPerTile = 4;
    
    [Tooltip("Color for explored rooms")]
    public Color exploredRoomColor = new Color(0.3f, 0.3f, 0.3f);
    
    [Tooltip("Color for walls")]
    public Color wallColor = new Color(0.1f, 0.1f, 0.1f);
    
    [Tooltip("Color for current room")]
    public Color currentRoomColor = new Color(0.5f, 0.5f, 0.5f);
    
    [Tooltip("Color for ladder/exit")]
    public Color ladderColor = Color.green;
    
    [Tooltip("Color for enemies")]
    public Color enemyColor = Color.red;
    
    [Tooltip("Color for the player")]
    public Color playerColor = Color.cyan;

    private Texture2D minimapTexture;
    private List<Vector2Int> exploredRooms = new List<Vector2Int>();
    private List<Transform> trackedEnemies = new List<Transform>();
    private List<Transform> trackedLadders = new List<Transform>();
    private Vector2Int currentRoomCoords;
    private int dungeonWidth, dungeonHeight;

    /// <summary>
    /// Initializes the minimap with the dungeon size and initial explored areas.
    /// </summary>
    /// <param name="width">Width of the dungeon in tiles</param>
    /// <param name="height">Height of the dungeon in tiles</param>
    public void InitializeMinimap(int width, int height)
    {
        dungeonWidth = width;
        dungeonHeight = height;
        
        // Create texture for the minimap
        minimapTexture = new Texture2D(width * pixelsPerTile, height * pixelsPerTile);
        minimapTexture.filterMode = FilterMode.Point; // Pixel perfect rendering
        
        // Clear to black (unexplored)
        Color[] clearColors = new Color[minimapTexture.width * minimapTexture.height];
        for (int i = 0; i < clearColors.Length; i++)
        {
            clearColors[i] = Color.black;
        }
        minimapTexture.SetPixels(clearColors);
        minimapTexture.Apply();
        
        // Set the texture to the RawImage
        minimapDisplay.texture = minimapTexture;
        
        // Reset tracked lists
        trackedEnemies.Clear();
        trackedLadders.Clear();
        exploredRooms.Clear();
        
        // NEW: Draw the entire map immediately upon initialization
        RedrawBaseMap();
        minimapTexture.Apply();
    }

    /// <summary>
    /// Updates the minimap display with the latest game state information.
    /// </summary>
    public void UpdateMinimap()
    {
        // Only update if we have a valid texture
        if (minimapTexture == null) return;
        
        // Update player's current room
        UpdateCurrentRoom();
        
        // Redraw the base minimap (rooms and walls)
        RedrawBaseMap();
        
        // Draw enemies
        DrawEnemies();
        
        // Draw ladders/exits
        DrawLadders();
        
        // Draw player
        DrawPlayer();
        
        // Apply all changes
        minimapTexture.Apply();
    }

    /// <summary>
    /// Registers an enemy to be tracked on the minimap.
    /// </summary>
    /// <param name="enemyTransform">Transform of the enemy to track</param>
    public void RegisterEnemy(Transform enemyTransform)
    {
        if (!trackedEnemies.Contains(enemyTransform))
        {
            trackedEnemies.Add(enemyTransform);
        }
    }

    /// <summary>
    /// Unregisters an enemy from being tracked on the minimap.
    /// </summary>
    /// <param name="enemyTransform">Transform of the enemy to stop tracking</param>
    public void UnregisterEnemy(Transform enemyTransform)
    {
        trackedEnemies.Remove(enemyTransform);
    }

    /// <summary>
    /// Registers a ladder/exit to be shown on the minimap.
    /// </summary>
    /// <param name="ladderTransform">Transform of the ladder to track</param>
    public void RegisterLadder(Transform ladderTransform)
    {
        if (!trackedLadders.Contains(ladderTransform))
        {
            trackedLadders.Add(ladderTransform);
        }
    }

    /// <summary>
    /// Marks a room as explored so it's visible on the minimap.
    /// </summary>
    /// <param name="roomX">Room X coordinate</param>
    /// <param name="roomY">Room Y coordinate</param>
    public void ExploreRoom(int roomX, int roomY)
    {
        Vector2Int roomCoord = new Vector2Int(roomX, roomY);
        if (!exploredRooms.Contains(roomCoord))
        {
            exploredRooms.Add(roomCoord);
        }
    }

    private void UpdateCurrentRoom()
    {
        // Convert player position to room coordinates
        // This depends on your dungeon layout system
        if (playerTransform != null && dungeonGenerator != null)
        {
            Vector2 playerPos = playerTransform.position;
            int roomX = Mathf.FloorToInt(playerPos.x / dungeonGenerator.cellWidth);
            int roomY = Mathf.FloorToInt(playerPos.y / dungeonGenerator.cellHeight);
            
            currentRoomCoords = new Vector2Int(roomX, roomY);
            
            // COMMENTED OUT: Original room exploration logic - uncomment if you want to reuse
            // Automatically mark current room as explored
            // ExploreRoom(roomX, roomY);
        }
    }

    private void RedrawBaseMap()
    {
        // COMMENTED OUT: Original room exploration logic - uncomment if you want to reuse
        /*
        // Draw all explored rooms
        foreach (Vector2Int room in exploredRooms)
        {
            Color roomColor = (room == currentRoomCoords) ? currentRoomColor : exploredRoomColor;
            
            // Draw the room as a rectangle on the minimap
            DrawRectangle(
                room.x * pixelsPerTile, 
                room.y * pixelsPerTile, 
                dungeonGenerator.cellWidth * pixelsPerTile, 
                dungeonGenerator.cellHeight * pixelsPerTile, 
                roomColor
            );
        }
        */
        
        // NEW: Draw entire dungeon map
        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                Color roomColor = (new Vector2Int(x, y) == currentRoomCoords) ? currentRoomColor : exploredRoomColor;
                
                // Draw the room as a rectangle on the minimap
                DrawRectangle(
                    x * pixelsPerTile, 
                    y * pixelsPerTile, 
                    dungeonGenerator.cellWidth * pixelsPerTile, 
                    dungeonGenerator.cellHeight * pixelsPerTile, 
                    roomColor
                );
            }
        }
        
        // TODO: Add walls between rooms if they exist in your dungeon data
    }

    private void DrawEnemies()
    {
        foreach (Transform enemy in trackedEnemies)
        {
            if (enemy == null) continue;
            
            // COMMENTED OUT: Original room exploration check - uncomment if you want to reuse
            /*
            // Skip enemies that aren't in explored rooms
            Vector2 enemyPos = enemy.position;
            int roomX = Mathf.FloorToInt(enemyPos.x / dungeonGenerator.cellWidth);
            int roomY = Mathf.FloorToInt(enemyPos.y / dungeonGenerator.cellHeight);
            
            if (!exploredRooms.Contains(new Vector2Int(roomX, roomY)))
                continue;
            */
            
            // NEW: Show all enemies regardless of room exploration
            Vector2 enemyPos = enemy.position;
            
            // Convert world position to minimap position
            int minimapX = Mathf.RoundToInt(enemyPos.x * pixelsPerTile / dungeonGenerator.cellWidth);
            int minimapY = Mathf.RoundToInt(enemyPos.y * pixelsPerTile / dungeonGenerator.cellHeight);
            
            // Draw enemy icon
            DrawDot(minimapX, minimapY, 2, enemyColor);
        }
    }

    private void DrawLadders()
    {
        foreach (Transform ladder in trackedLadders)
        {
            if (ladder == null) continue;
            
            // Convert world position to minimap position
            Vector2 ladderPos = ladder.position;
            int minimapX = Mathf.RoundToInt(ladderPos.x * pixelsPerTile / dungeonGenerator.cellWidth);
            int minimapY = Mathf.RoundToInt(ladderPos.y * pixelsPerTile / dungeonGenerator.cellHeight);
            
            // Draw ladder icon
            DrawDot(minimapX, minimapY, 3, ladderColor);
        }
    }

    private void DrawPlayer()
    {
        if (playerTransform == null) return;
        
        // Convert world position to minimap position
        Vector2 playerPos = playerTransform.position;
        int minimapX = Mathf.RoundToInt(playerPos.x * pixelsPerTile / dungeonGenerator.cellWidth);
        int minimapY = Mathf.RoundToInt(playerPos.y * pixelsPerTile / dungeonGenerator.cellHeight);
        
        // Draw player icon
        DrawDot(minimapX, minimapY, 2, playerColor);
    }

    /// <summary>
    /// Draws a filled rectangle on the minimap texture.
    /// </summary>
    private void DrawRectangle(int x, int y, int width, int height, Color color)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                int pixelX = x + i;
                int pixelY = y + j;
                
                if (pixelX >= 0 && pixelX < minimapTexture.width && 
                    pixelY >= 0 && pixelY < minimapTexture.height)
                {
                    minimapTexture.SetPixel(pixelX, pixelY, color);
                }
            }
        }
    }

    /// <summary>
    /// Draws a filled circle representing a dot on the minimap.
    /// </summary>
    private void DrawDot(int x, int y, int radius, Color color)
    {
        for (int i = -radius; i <= radius; i++)
        {
            for (int j = -radius; j <= radius; j++)
            {
                if (i*i + j*j <= radius*radius)
                {
                    int pixelX = x + i;
                    int pixelY = y + j;
                    
                    if (pixelX >= 0 && pixelX < minimapTexture.width && 
                        pixelY >= 0 && pixelY < minimapTexture.height)
                    {
                        minimapTexture.SetPixel(pixelX, pixelY, color);
                    }
                }
            }
        }
    }
} 