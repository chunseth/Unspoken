using UnityEngine;
using System.Collections.Generic;

// An enumeration for our tile types.
public enum TileType
{
    Void,   // not used
    Floor,
    NonEssentialFloor,  // For secondary corridors that don't overwrite walls
    BossFloor,          // For boss room floor tiles
    SpecialFloor,       // For special room floor tiles
    Wall,
    WallLeft,
    WallRight,
    WallTop,
    WallBottom,

    // Add corner wall types
    WallCornerTopLeft,
    WallCornerTopRight,
    WallCornerBottomLeft,
    WallCornerBottomRight,
    
    // Boss room special wall
    CrackedWall,
    // Second cracked wall for random placement
    CrackedWall2,
    // Third cracked wall for additional puzzle placement
    CrackedWall3,
    // Fourth cracked wall for special room platform
    CrackedWall4,
    // Carryable object for puzzle interaction
    CarryableObject,
    // Hole that causes player to respawn and lose health
    Hole,
    // Puzzle solution hint for CrackedWall1
    PuzzleSolutionHint1,
    // Puzzle solution hint for CrackedWall2
    PuzzleSolutionHint2
}

/// <summary>
/// Data structure for a cell within the dungeon grid.
/// </summary>
[System.Serializable]
public class Cell
{
    public int cellX;
    public int cellY;
    public bool visited = false;
    public Room room; // room generated within this cell (if any)
}

/// <summary>
/// Data structure for a room inside a cell.
/// </summary>
[System.Serializable]
public class Room
{
    public int x, y, width, height;
    public bool isSpecialShape = false;
    public List<Rect> excludedAreas;
    public List<RoomDecoration> decorations;
    
    public Vector2Int Center
    {
        get { return new Vector2Int(x + width / 2, y + height / 2); }
    }
    
    public bool ContainsPosition(int posX, int posY)
    {
        if (posX < x || posX >= x + width || posY < y || posY >= y + height)
            return false;
            
        // For special shaped rooms, check excluded areas
        if (isSpecialShape && excludedAreas != null)
        {
            foreach (Rect excluded in excludedAreas)
            {
                if (posX >= excluded.x && posX < excluded.x + excluded.width &&
                    posY >= excluded.y && posY < excluded.y + excluded.height)
                {
                    return false; // Position is in excluded area
                }
            }
        }
        
        return true;
    }
}

/// <summary>
/// Data structure for room decorations.
/// </summary>
[System.Serializable]
public class RoomDecoration
{
    public GameObject prefab;
    public Vector2Int position;
}

/// <summary>
/// Configuration data for dungeon generation settings.
/// </summary>
[System.Serializable]
public class DungeonConfig
{
    [Header("Dungeon Settings")]
    public int dungeonWidth = 80;
    public int dungeonHeight = 60;
    public int cellWidth = 13;
    public int cellHeight = 13;
    public int bufferX = 2;
    public int bufferY = 2;
    [Range(0, 100)]
    public int roomChancePercent = 70;

    [Header("Corridor Settings")]
    public bool enhancedCorridors = true;
    [Range(1, 3)]
    public int corridorMinWidth = 2;
    [Range(1, 4)]
    public int corridorMaxWidth = 3;
    [Range(0, 1)]
    public float corridorWindingFactor = 0.2f;

    [Header("Room Variety")]
    public bool enableRoomVariety = true;
    [Range(0, 100)]
    public int specialShapedRoomChance = 30;
    
    [Header("Special Features")]
    public bool enableBossRoom = true;
    public bool enableSpecialRoom = true;
    public bool enablePuzzleBossRoomLock = true;
}
