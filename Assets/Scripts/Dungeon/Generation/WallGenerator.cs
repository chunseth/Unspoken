using UnityEngine;

/// <summary>
/// Handles the generation of walls around floor tiles in the dungeon.
/// </summary>
public class WallGenerator
{
    private DungeonConfig config;
    private TileType[,] dungeonMap;

    public WallGenerator(DungeonConfig config, TileType[,] dungeonMap)
    {
        this.config = config;
        this.dungeonMap = dungeonMap;
    }

    /// <summary>
    /// Adds walls around every floor tile that touches a void.
    /// </summary>
    public void AddWalls()
    {
        // First pass: Add basic walls around floors
        for (int x = 1; x < config.dungeonWidth - 1; x++)
        {
            for (int y = 1; y < config.dungeonHeight - 1; y++)
            {
                // Check if this is any type of floor tile or hole tile
                if (dungeonMap[x, y] == TileType.Floor || 
                    dungeonMap[x, y] == TileType.NonEssentialFloor || 
                    dungeonMap[x, y] == TileType.BossFloor ||
                    dungeonMap[x, y] == TileType.SpecialFloor ||
                    dungeonMap[x, y] == TileType.Hole)
                {
                    // Top Right Corner: Checking if right, top, and top-right are all void
                    if (dungeonMap[x + 1, y] == TileType.Void &&
                        dungeonMap[x, y + 1] == TileType.Void &&
                        dungeonMap[x + 1, y + 1] == TileType.Void)
                    {
                        dungeonMap[x + 1, y + 1] = TileType.WallCornerTopRight;
                    }

                    // Top Left Corner: Checking if left, top, and top-left are all void
                    else if (dungeonMap[x - 1, y] == TileType.Void &&
                             dungeonMap[x, y + 1] == TileType.Void &&
                             dungeonMap[x - 1, y + 1] == TileType.Void)
                    {
                        dungeonMap[x - 1, y + 1] = TileType.WallCornerTopLeft;
                    }

                    // Bottom Right Corner: Checking if right, bottom, and bottom-right are all void
                    else if (dungeonMap[x + 1, y] == TileType.Void &&
                             dungeonMap[x, y - 1] == TileType.Void &&
                             dungeonMap[x + 1, y - 1] == TileType.Void)
                    {
                        dungeonMap[x + 1, y - 1] = TileType.WallCornerBottomRight;
                    }

                    // Bottom Left Corner: Checking if left, bottom, and bottom-left are all void
                    else if (dungeonMap[x - 1, y] == TileType.Void &&
                             dungeonMap[x, y - 1] == TileType.Void &&
                             dungeonMap[x - 1, y - 1] == TileType.Void)
                    {
                        dungeonMap[x - 1, y - 1] = TileType.WallCornerBottomLeft;
                    }

                    // Check adjacent tiles—if any is Void, set it as a Wall.
                    if (dungeonMap[x + 1, y] == TileType.Void) dungeonMap[x + 1, y] = TileType.WallRight;
                    if (dungeonMap[x - 1, y] == TileType.Void) dungeonMap[x - 1, y] = TileType.WallLeft;
                    if (dungeonMap[x, y + 1] == TileType.Void) dungeonMap[x, y + 1] = TileType.WallTop;
                    if (dungeonMap[x, y - 1] == TileType.Void) dungeonMap[x, y - 1] = TileType.WallBottom;
                }
            }
        }
    }

    /// <summary>
    /// Checks if a tile type is a wall.
    /// </summary>
    public static bool IsWallTile(TileType tileType)
    {
        return tileType == TileType.Wall || 
               tileType == TileType.WallLeft || 
               tileType == TileType.WallRight || 
               tileType == TileType.WallTop || 
               tileType == TileType.WallBottom ||
               tileType == TileType.WallCornerTopLeft ||
               tileType == TileType.WallCornerTopRight ||
               tileType == TileType.WallCornerBottomLeft ||
               tileType == TileType.WallCornerBottomRight;
    }
}
