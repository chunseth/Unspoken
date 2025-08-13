using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

// An enumeration for our tile types.
public enum TileType
{
    Void,   // not used
    Floor,
    NonEssentialFloor,  // For secondary corridors that don't overwrite walls
    BossFloor,          // For boss room floor tiles
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
    CrackedWall
}



/// <summary>
/// Generates procedural dungeons with rooms, corridors, and features like enemies and exits.
/// </summary>
public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Settings")]
    /// <summary>
    /// Width of the dungeon in tiles
    /// </summary>
    public int dungeonWidth = 80;      // total dungeon grid width (in tiles)
    /// <summary>
    /// Height of the dungeon in tiles
    /// </summary>
    public int dungeonHeight = 60;     // total dungeon grid height (in tiles)
    /// <summary>
    /// Width of each cell in the dungeon grid
    /// </summary>
    public int cellWidth = 13;         // size of each cell (in tiles)
    /// <summary>
    /// Height of each cell in the dungeon grid
    /// </summary>
    public int cellHeight = 13;
    /// <summary>
    /// Buffer space around the edge of the dungeon
    /// </summary>
    public int bufferX = 2;            // border from edge where generation starts
    public int bufferY = 2;
    [Range(0, 100)]
    public int roomChancePercent = 70; // chance a cell will have a room

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
    public GameObject[] roomDecorations; // Prefabs like tables, chairs, bookshelves
    public GameObject[] hazardPrefabs; // Like spikes, fire pits, etc.
    [Range(0, 100)]
    public int specialShapedRoomChance = 30; // % chance for special shaped rooms



    [Header("Basic Prefabs")]
    public GameObject floorPrefab;
    public GameObject nonEssentialFloorPrefab;  // Prefab for secondary corridors
    public GameObject wallPrefab;
    public GameObject wallLeftPrefab;
    public GameObject wallRightPrefab;
    public GameObject wallTopPrefab;
    public GameObject wallBottomPrefab;
    public GameObject stairPrefab;
    
    [Header("Boss Room Special Prefabs")]
    public GameObject crackedWallPrefab;  // Special cracked wall for boss room



    [Header("Player Settings")]
    // Instead of a prefab, reference the existing player GameObject in the scene.
    public GameObject player;

    [Header("Parent Object for Tiles")]
    public Transform dungeonParent;

    [Header("Minimap Settings")]
    public RectTransform minimapContainer;  // UI panel to hold the minimap
    public Color unexploredColor = new Color(0, 0, 0, 1);      // Black
    public Color floorColor = new Color(0.8f, 0.8f, 0.8f, 1f); // Light grey
    public Color wallColor = new Color(0.3f, 0.3f, 0.3f, 1f);  // Dark grey
    public Color playerColor = new Color(1f, 0f, 0f, 1f);      // Red dot for player

    public Color stairsColor = new Color(0f, 1f, 1f, 1f); // Bright cyan for stairs
    public Color bossRoomColor = new Color(1f, 0f, 1f, 1f); // Magenta for boss room

    private RenderTexture renderTexture;
    private Texture2D drawingTexture;
    private RawImage minimapImage;
    private bool[,] exploredTiles;

    // 2D array representing the dungeon map.
    public TileType[,] dungeonMap;

    // Data structure for a cell within the dungeon grid.
    private class Cell
    {
        public int cellX;
        public int cellY;
        public bool visited = false;
        public Room room; // room generated within this cell (if any)
    }

    // Data structure for a room inside a cell.
    private class Room
    {
        public int x, y, width, height;
        public bool isSpecialShape = false;
        public List<Rect> excludedAreas;
        public List<RoomDecoration> decorations;
    }

    private class RoomDecoration
    {
        public GameObject prefab;
        public Vector2Int position;
    }

    private Cell[,] cells; // grid of cells
    private int cellsX;    // number of cells horizontally
    private int cellsY;    // number of cells vertically

    private Vector2Int lastPlayerPosition = new Vector2Int(-1, -1); // Track last position to clear old player marker

    private List<Room> rooms = new List<Room>();
    private List<RoomDecoration> decorationsToPlace = new List<RoomDecoration>();

    private Vector2Int stairsPosition; // Keep track of where stairs are placed
    private bool playerEnteredStairsRoom = false; // Track if player has entered the stairs room
    
    [Header("Boss Room Settings")]
    public bool enableBossRoom = true;
    public GameObject bossRoomFloorPrefab; // Optional special floor for boss room
    private Room bossRoom; // The boss room in the center

    void Start()
    {
        GenerateDungeon();
        InitializeMinimap();
        InstantiateDungeon();
    }

    void Update()
    {
        if (player != null && drawingTexture != null && exploredTiles != null)
        {
            // Convert world position to grid position
            Vector2 playerPos = new Vector2(player.transform.position.x, player.transform.position.y);
            UpdateMinimapExploration(playerPos);

            // Check if player entered the room with stairs
            CheckIfPlayerEnteredStairsRoom();
        }
    }

    /// <summary>
    /// Generates a new dungeon level with rooms, corridors, and features.
    /// </summary>
    /// <param name="level">The current dungeon level</param>
    /// <returns>True if generation was successful</returns>
    public bool GenerateDungeon(int level = 1)
    {
        // Clear the rooms list at the start of generation
        rooms.Clear();
        if (decorationsToPlace == null)
            decorationsToPlace = new List<RoomDecoration>();
        else
            decorationsToPlace.Clear();

        // 1. Initialize the dungeonMap with "Void" tiles.
        dungeonMap = new TileType[dungeonWidth, dungeonHeight];
        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                dungeonMap[x, y] = TileType.Void;
            }
        }

        // 2. Calculate number of cells based on dungeon size, cell size, and buffer.
        cellsX = (dungeonWidth - 2 * bufferX) / cellWidth;
        cellsY = (dungeonHeight - 2 * bufferY) / cellHeight;
        cells = new Cell[cellsX, cellsY];
        for (int x = 0; x < cellsX; x++)
        {
            for (int y = 0; y < cellsY; y++)
            {
                cells[x, y] = new Cell();
                cells[x, y].cellX = x;
                cells[x, y].cellY = y;
            }
        }

        // 3. Connect cells using a depth-first search (DFS) algorithm.
        List<Cell> stack = new List<Cell>();
        Cell startCell = cells[0, 0];  // you could also choose a random starting cell
        startCell.visited = true;
        stack.Add(startCell);

        while (stack.Count > 0)
        {
            Cell current = stack[stack.Count - 1];
            List<Cell> neighbours = GetUnvisitedNeighbours(current);
            if (neighbours.Count > 0)
            {
                Cell chosen = neighbours[Random.Range(0, neighbours.Count)];
                chosen.visited = true;
                // Carve a corridor between the current cell and the chosen neighbour.
                CarveCorridorBetweenCells(current, chosen);
                stack.Add(chosen);
            }
            else
            {
                stack.RemoveAt(stack.Count - 1);
            }
        }

        // 4. Create boss room in the center if enabled
        if (enableBossRoom)
        {
            CreateBossRoom();
        }

        // 5. For each cell, generate a room with a given chance (skip center cells if boss room exists).
        for (int x = 0; x < cellsX; x++)
        {
            for (int y = 0; y < cellsY; y++)
            {
                // Skip center cells if boss room is enabled
                if (enableBossRoom && IsInBossRoomArea(x, y))
                {
                    continue;
                }

                if (Random.Range(0, 100) < roomChancePercent)
                {
                    cells[x, y].room = GenerateRoomInCell(x, y);
                    CarveRoom(cells[x, y].room);
                }
            }
        }

        // 5. Add non-essential corridors for better connectivity
        AddNonEssentialCorridors();

        // 6. Add walls around floor tiles (after all floor types are placed)
        AddWalls();

        // 7. Add special boss room features
        if (enableBossRoom && bossRoom != null)
        {
            PlaceCrackedWallInBossRoom();
        }

        // After generation, update minimap
        MinimapManager minimapManager = FindObjectOfType<MinimapManager>();
        if (minimapManager != null)
        {
            minimapManager.InitializeMinimap(dungeonWidth, dungeonHeight);
        }

        return true;
    }

    // Helper method: returns a list of unvisited neighbour cells (up, down, left, right).
    List<Cell> GetUnvisitedNeighbours(Cell cell)
    {
        List<Cell> neighbours = new List<Cell>();
        int x = cell.cellX;
        int y = cell.cellY;
        // Up
        if (y + 1 < cellsY && !cells[x, y + 1].visited)
            neighbours.Add(cells[x, y + 1]);
        // Down
        if (y - 1 >= 0 && !cells[x, y - 1].visited)
            neighbours.Add(cells[x, y - 1]);
        // Right
        if (x + 1 < cellsX && !cells[x + 1, y].visited)
            neighbours.Add(cells[x + 1, y]);
        // Left
        if (x - 1 >= 0 && !cells[x - 1, y].visited)
            neighbours.Add(cells[x - 1, y]);

        return neighbours;
    }

    // Carves a corridor between two cells by connecting their centers.
    void CarveCorridorBetweenCells(Cell a, Cell b)
    {
        // Convert cell coordinates to tile coordinates (center of each cell)
        int ax = bufferX + a.cellX * cellWidth + cellWidth / 2;
        int ay = bufferY + a.cellY * cellHeight + cellHeight / 2;
        int bx = bufferX + b.cellX * cellWidth + cellWidth / 2;
        int by = bufferY + b.cellY * cellHeight + cellHeight / 2;

        if (!enhancedCorridors)
        {
            // Standard corridor generation (existing code)
            // Carve a horizontal corridor first, then vertical
            for (int x = Mathf.Min(ax, bx); x <= Mathf.Max(ax, bx); x++)
            {
                dungeonMap[x, ay] = TileType.Floor;
            }
            // Carve a vertical corridor.
            for (int y = Mathf.Min(ay, by); y <= Mathf.Max(ay, by); y++)
            {
                dungeonMap[bx, y] = TileType.Floor;
            }
        }
        else
        {
            // Enhanced corridor generation with variable width and winding
            int corridorWidth = Random.Range(corridorMinWidth, corridorMaxWidth + 1);

            // Determine if we should make this corridor winding
            bool windingCorridor = Random.value < corridorWindingFactor;

            if (windingCorridor)
            {
                // Create a winding path using waypoints (not diagonal connections)
                CarveWindingCorridor(ax, ay, bx, by, corridorWidth);
            }
            else
            {
                // Always carve horizontal then vertical (or vice versa)
                // This ensures there are no diagonal-only connections
                bool horizontalFirst = Random.value > 0.5f;

                if (horizontalFirst)
                {
                    // Horizontal corridor with width
                    CarveHorizontalCorridor(ax, ay, bx, ay, corridorWidth);

                    // Vertical corridor with width
                    CarveVerticalCorridor(bx, ay, bx, by, corridorWidth);
                }
                else
                {
                    // Vertical corridor with width
                    CarveVerticalCorridor(ax, ay, ax, by, corridorWidth);

                    // Horizontal corridor with width
                    CarveHorizontalCorridor(ax, by, bx, by, corridorWidth);
                }
            }
        }
    }

    void CarveHorizontalCorridor(int x1, int y1, int x2, int y2, int width)
    {
        int startX = Mathf.Min(x1, x2);
        int endX = Mathf.Max(x1, x2);

        for (int x = startX; x <= endX; x++)
        {
            for (int w = 0; w < width; w++)
            {
                int yPos = y1 - width / 2 + w;
                if (yPos >= 0 && yPos < dungeonHeight)
                    dungeonMap[x, yPos] = TileType.Floor;
            }
        }
    }

    void CarveVerticalCorridor(int x1, int y1, int x2, int y2, int width)
    {
        int startY = Mathf.Min(y1, y2);
        int endY = Mathf.Max(y1, y2);

        for (int y = startY; y <= endY; y++)
        {
            for (int w = 0; w < width; w++)
            {
                int xPos = x1 - width / 2 + w;
                if (xPos >= 0 && xPos < dungeonWidth)
                    dungeonMap[xPos, y] = TileType.Floor;
            }
        }
    }

    void CarveWindingCorridor(int ax, int ay, int bx, int by, int width)
    {
        // Instead of just one midpoint, use multiple waypoints with proper connections
        // Create 1-3 waypoints for complex paths
        int numWaypoints = Random.Range(1, 4);
        Vector2Int[] waypoints = new Vector2Int[numWaypoints + 2]; // +2 for start and end points

        // Set start and end points
        waypoints[0] = new Vector2Int(ax, ay);
        waypoints[numWaypoints + 1] = new Vector2Int(bx, by);

        // Create intermediate waypoints that aren't too close to edges
        for (int i = 1; i <= numWaypoints; i++)
        {
            float progress = i / (float)(numWaypoints + 1);

            // Base position interpolating between start and end
            int baseX = Mathf.RoundToInt(Mathf.Lerp(ax, bx, progress));
            int baseY = Mathf.RoundToInt(Mathf.Lerp(ay, by, progress));

            // Add some randomness
            int randX = Random.Range(-4, 5);
            int randY = Random.Range(-4, 5);

            int waypointX = Mathf.Clamp(baseX + randX, bufferX + 2, dungeonWidth - bufferX - 2);
            int waypointY = Mathf.Clamp(baseY + randY, bufferY + 2, dungeonHeight - bufferY - 2);

            // Ensure waypoint is not in boss room area
            if (enableBossRoom && IsPositionInBossRoom(waypointX, waypointY))
            {
                // Try to find an alternative position outside the boss room
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    randX = Random.Range(-8, 9);
                    randY = Random.Range(-8, 9);
                    waypointX = Mathf.Clamp(baseX + randX, bufferX + 2, dungeonWidth - bufferX - 2);
                    waypointY = Mathf.Clamp(baseY + randY, bufferY + 2, dungeonHeight - bufferY - 2);
                    
                    if (!IsPositionInBossRoom(waypointX, waypointY))
                        break;
                }
            }

            waypoints[i] = new Vector2Int(waypointX, waypointY);
        }

        // Carve corridors between waypoints (always horizontal then vertical)
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Vector2Int start = waypoints[i];
            Vector2Int end = waypoints[i + 1];

            // Always carve horizontal then vertical to avoid diagonal-only connections
            CarveHorizontalCorridor(start.x, start.y, end.x, start.y, width);
            CarveVerticalCorridor(end.x, start.y, end.x, end.y, width);
        }
    }

    // Generate a random room within a cell.
    Room GenerateRoomInCell(int cellXIndex, int cellYIndex)
    {
        Room room = new Room();
        // Determine the cell's starting position in tile coordinates.
        int cellStartX = bufferX + cellXIndex * cellWidth;
        int cellStartY = bufferY + cellYIndex * cellHeight;

        // Randomize room size (ensuring it fits within the cell).
        int roomWidth = Random.Range(5, cellWidth - 1);
        int roomHeight = Random.Range(4, cellHeight - 1);

        // Randomly choose room shape if variety is enabled
        if (enableRoomVariety && Random.Range(0, 100) < specialShapedRoomChance)
        {
            // Create special room shapes
            int shapeType = Random.Range(0, 4);
            switch (shapeType)
            {
                case 0: // L-shaped room
                    CreateLShapedRoom(room, cellStartX, cellStartY, roomWidth, roomHeight);
                    break;
                case 1: // Circular room
                    CreateCircularRoom(room, cellStartX, cellStartY, roomWidth, roomHeight);
                    break;
                case 2: // Cross-shaped room
                    CreateCrossShapedRoom(room, cellStartX, cellStartY, roomWidth, roomHeight);
                    break;
                case 3: // Default rectangular with random decorations
                    // Standard rectangular room with decorations
                    CreateDecoratedRoom(room, cellStartX, cellStartY, roomWidth, roomHeight);
                    break;
            }
        }
        else
        {
            // Randomize room position within the cell.
            int roomX = cellStartX + Random.Range(1, cellWidth - roomWidth);
            int roomY = cellStartY + Random.Range(1, cellHeight - roomHeight);
            room.x = roomX;
            room.y = roomY;
            room.width = roomWidth;
            room.height = roomHeight;
        }

        // Add the room to our list
        rooms.Add(room);

        return room;
    }

    void CreateLShapedRoom(Room room, int cellStartX, int cellStartY, int width, int height)
    {
        // Randomize room position within the cell
        int roomX = cellStartX + Random.Range(1, cellWidth - width);
        int roomY = cellStartY + Random.Range(1, cellHeight - height);

        // Set the main room properties
        room.x = roomX;
        room.y = roomY;
        room.width = width;
        room.height = height;
        room.isSpecialShape = true;

        // Define the L-shape by removing a rectangular section
        int cutWidth = width / 2;
        int cutHeight = height / 2;
        room.excludedAreas = new List<Rect>();

        // Randomly choose which corner to cut out
        bool cutTopRight = Random.value > 0.5f;
        bool cutTopSide = Random.value > 0.5f;

        int cutX = cutTopRight ? roomX + width - cutWidth : roomX;
        int cutY = cutTopSide ? roomY + height - cutHeight : roomY;

        room.excludedAreas.Add(new Rect(cutX, cutY, cutWidth, cutHeight));
    }

    void CreateCircularRoom(Room room, int cellStartX, int cellStartY, int width, int height)
    {
        // Make sure width and height are similar for a more circular shape
        if (Mathf.Abs(width - height) > 2)
        {
            int avg = (width + height) / 2;
            width = avg;
            height = avg;
        }

        // Randomize room position within the cell
        int roomX = cellStartX + Random.Range(1, cellWidth - width);
        int roomY = cellStartY + Random.Range(1, cellHeight - height);

        // Set the main room properties
        room.x = roomX;
        room.y = roomY;
        room.width = width;
        room.height = height;
        room.isSpecialShape = true;

        // For a circular room we'll use the excludedAreas to define corners to exclude
        room.excludedAreas = new List<Rect>();

        // Define corner squares to exclude to create a more circular shape
        int cornerSize = (int)(width * 0.3f);
        room.excludedAreas.Add(new Rect(roomX, roomY, cornerSize, cornerSize)); // Bottom left
        room.excludedAreas.Add(new Rect(roomX + width - cornerSize, roomY, cornerSize, cornerSize)); // Bottom right
        room.excludedAreas.Add(new Rect(roomX, roomY + height - cornerSize, cornerSize, cornerSize)); // Top left
        room.excludedAreas.Add(new Rect(roomX + width - cornerSize, roomY + height - cornerSize, cornerSize, cornerSize)); // Top right
    }

    void CreateCrossShapedRoom(Room room, int cellStartX, int cellStartY, int width, int height)
    {
        // Randomize room position within the cell
        int roomX = cellStartX + Random.Range(1, cellWidth - width);
        int roomY = cellStartY + Random.Range(1, cellHeight - height);

        // Set the main room properties
        room.x = roomX;
        room.y = roomY;
        room.width = width;
        room.height = height;
        room.isSpecialShape = true;

        room.excludedAreas = new List<Rect>();

        // Calculate the "arms" of the cross - the central portion will remain
        int armWidth = width / 3;
        int armHeight = height / 3;

        // Define the corners to exclude
        room.excludedAreas.Add(new Rect(roomX, roomY, armWidth, armHeight)); // Bottom left
        room.excludedAreas.Add(new Rect(roomX + width - armWidth, roomY, armWidth, armHeight)); // Bottom right
        room.excludedAreas.Add(new Rect(roomX, roomY + height - armHeight, armWidth, armHeight)); // Top left
        room.excludedAreas.Add(new Rect(roomX + width - armWidth, roomY + height - armHeight, armWidth, armHeight)); // Top right
    }

    void CreateDecoratedRoom(Room room, int cellStartX, int cellStartY, int width, int height)
    {
        // Standard rectangular room
        int roomX = cellStartX + Random.Range(1, cellWidth - width);
        int roomY = cellStartY + Random.Range(1, cellHeight - height);
        room.x = roomX;
        room.y = roomY;
        room.width = width;
        room.height = height;

        // Add decorations and hazards
        if (roomDecorations != null && roomDecorations.Length > 0 && Random.value > 0.5f)
        {
            room.decorations = new List<RoomDecoration>();
            int decorationCount = Random.Range(1, 4);

            for (int i = 0; i < decorationCount; i++)
            {
                // Add decoration at random position within room
                int decorX = Random.Range(room.x + 1, room.x + room.width - 1);
                int decorY = Random.Range(room.y + 1, room.y + room.height - 1);
                int prefabIndex = Random.Range(0, roomDecorations.Length);

                room.decorations.Add(new RoomDecoration
                {
                    prefab = roomDecorations[prefabIndex],
                    position = new Vector2Int(decorX, decorY)
                });
            }
        }

        // Add hazards
        if (hazardPrefabs != null && hazardPrefabs.Length > 0 && Random.value > 0.7f)
        {
            if (room.decorations == null)
                room.decorations = new List<RoomDecoration>();

            int hazardCount = Random.Range(1, 3);

            for (int i = 0; i < hazardCount; i++)
            {
                // Add hazard at random position within room
                int hazardX = Random.Range(room.x + 1, room.x + room.width - 1);
                int hazardY = Random.Range(room.y + 1, room.y + room.height - 1);
                int prefabIndex = Random.Range(0, hazardPrefabs.Length);

                room.decorations.Add(new RoomDecoration
                {
                    prefab = hazardPrefabs[prefabIndex],
                    position = new Vector2Int(hazardX, hazardY)
                });
            }
        }
    }

    // Carve out a room in the dungeonMap by marking its tiles as Floor.
    void CarveRoom(Room room)
    {
        if (room.isSpecialShape && room.excludedAreas != null)
        {
            // For special shapes, we need to check excluded areas
            for (int x = room.x; x < room.x + room.width; x++)
            {
                for (int y = room.y; y < room.y + room.height; y++)
                {
                    bool inExcludedArea = false;

                    // Check if this point is in any excluded area
                    foreach (Rect excluded in room.excludedAreas)
                    {
                        if (x >= excluded.x && x < excluded.x + excluded.width &&
                            y >= excluded.y && y < excluded.y + excluded.height)
                        {
                            inExcludedArea = true;
                            break;
                        }
                    }

                    if (!inExcludedArea && x >= 0 && x < dungeonWidth && y >= 0 && y < dungeonHeight)
                    {
                        // Use BossFloor for boss room, regular Floor for other rooms
                        if (enableBossRoom && room == bossRoom)
                        {
                            dungeonMap[x, y] = TileType.BossFloor;
                        }
                        else
                        {
                            dungeonMap[x, y] = TileType.Floor;
                        }
                    }
                }
            }
        }
        else
        {
            // Standard rectangular room
            for (int x = room.x; x < room.x + room.width; x++)
            {
                for (int y = room.y; y < room.y + room.height; y++)
                {
                    if (x >= 0 && x < dungeonWidth && y >= 0 && y < dungeonHeight)
                    {
                        // Use BossFloor for boss room, regular Floor for other rooms
                        if (enableBossRoom && room == bossRoom)
                        {
                            dungeonMap[x, y] = TileType.BossFloor;
                        }
                        else
                        {
                            dungeonMap[x, y] = TileType.Floor;
                        }
                    }
                }
            }
        }

        // Add decorations if any
        if (room.decorations != null)
        {
            foreach (RoomDecoration decoration in room.decorations)
            {
                // Schedule decoration placement for after the dungeon is fully carved
                decorationsToPlace.Add(decoration);
            }
        }
    }

    // Add walls around every floor tile that touches a void.
    void AddWalls()
    {
        // First pass: Add basic walls around floors
        for (int x = 1; x < dungeonWidth - 1; x++)
        {
            for (int y = 1; y < dungeonHeight - 1; y++)
            {
                // Check if this is any type of floor tile
                if (dungeonMap[x, y] == TileType.Floor || 
                    dungeonMap[x, y] == TileType.NonEssentialFloor || 
                    dungeonMap[x, y] == TileType.BossFloor)
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





    List<Vector2Int> FindCorridorTiles()
    {
        List<Vector2Int> corridorTiles = new List<Vector2Int>();

        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                if ((dungeonMap[x, y] == TileType.Floor || dungeonMap[x, y] == TileType.NonEssentialFloor) && !IsInRoom(x, y))
                {
                    corridorTiles.Add(new Vector2Int(x, y));
                }
            }
        }

        return corridorTiles;
    }

    // Instantiate prefabs based on the dungeonMap data.
    public void InstantiateDungeon()
    {
        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                Vector3 pos = new Vector3(x, y, 0);
                switch (dungeonMap[x, y])
                {
                    case TileType.Floor:
                        Instantiate(floorPrefab, pos, Quaternion.identity, dungeonParent);
                        break;
                    case TileType.NonEssentialFloor:
                        // Use non-essential floor prefab for secondary corridors
                        GameObject nonEssentialFloorToUse = nonEssentialFloorPrefab != null ? nonEssentialFloorPrefab : floorPrefab;
                        Instantiate(nonEssentialFloorToUse, pos, Quaternion.identity, dungeonParent);
                        break;
                    case TileType.BossFloor:
                        // Use boss room floor prefab if available, otherwise use regular floor
                        GameObject bossFloorToUse = bossRoomFloorPrefab != null ? bossRoomFloorPrefab : floorPrefab;
                        Instantiate(bossFloorToUse, pos, Quaternion.identity, dungeonParent);
                        break;
                    case TileType.WallLeft:
                        Instantiate(wallLeftPrefab, pos, Quaternion.identity, dungeonParent);
                        break;
                    case TileType.WallRight:
                        Instantiate(wallRightPrefab, pos, Quaternion.identity, dungeonParent);
                        break;
                    case TileType.WallTop:
                        Instantiate(wallTopPrefab, pos, Quaternion.identity, dungeonParent);
                        break;
                    case TileType.WallBottom:
                        Instantiate(wallBottomPrefab, pos, Quaternion.identity, dungeonParent);
                        break;

                    case TileType.WallCornerTopLeft:
                        Instantiate(wallPrefab, pos, Quaternion.identity, dungeonParent);
                        break;
                    case TileType.WallCornerTopRight:
                        Instantiate(wallPrefab, pos, Quaternion.identity, dungeonParent);
                        break;
                    case TileType.WallCornerBottomLeft:
                        Instantiate(wallPrefab, pos, Quaternion.identity, dungeonParent);
                        break;
                    case TileType.WallCornerBottomRight:
                        Instantiate(wallPrefab, pos, Quaternion.identity, dungeonParent);
                        break;
                    case TileType.CrackedWall:
                        Instantiate(crackedWallPrefab, pos, Quaternion.identity, dungeonParent);
                        break;
                        // Void tiles are ignored.
                }
            }
        }

        // Place decorations after all tiles are created
        foreach (RoomDecoration decoration in decorationsToPlace)
        {
            Vector3 pos = new Vector3(decoration.position.x, decoration.position.y, -0.1f);
            Instantiate(decoration.prefab, pos, Quaternion.identity, dungeonParent);
        }

        // Position the player (stairs will be placed later)
        PlacePlayer();
    }

    // Randomly pick a floor tile position to place the stair/exit.
    private void PlaceStairs()
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();

        // Find all valid positions (floor tiles in rooms)
        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                if (dungeonMap[x, y] == TileType.Floor && IsValidLadderPosition(x, y))
                {
                    validPositions.Add(new Vector2Int(x, y));
                }
            }
        }

        if (validPositions.Count > 0)
        {
            Vector2Int chosenPos = validPositions[Random.Range(0, validPositions.Count)];
            Vector3 pos = new Vector3(chosenPos.x, chosenPos.y, 0);
            Instantiate(stairPrefab, pos, Quaternion.identity, dungeonParent);

            // Store the stairs position for later use
            stairsPosition = chosenPos;
            playerEnteredStairsRoom = false;

            // Mark the stairs on the minimap with cyan color
            MarkSpecialOnMinimap(pos, stairsColor);

            Debug.Log("Stairs placed at position: " + stairsPosition);
        }
        else
        {
            Debug.LogWarning("No valid positions found for stairs!");
        }
    }

    // Place the player at a random floor position using GetRandomFloorPosition().
    void PlacePlayer()
    {
        if (player == null)
        {
            Debug.LogWarning("Player GameObject not assigned in DungeonGenerator!");
            return;
        }

        Vector3 pos = GetRandomFloorPosition();

        // If dungeonParent has a position offset, account for it
        if (dungeonParent != null)
        {
            pos = dungeonParent.TransformPoint(pos);
        }

        player.transform.position = pos + new Vector3(0, 0, -2);
    }

    // Find a random floor tile position, prioritizing corner rooms or corner corridors.
    Vector3 GetRandomFloorPosition()
    {
        List<Vector2Int> cornerRoomPositions = new List<Vector2Int>();
        List<Vector2Int> cornerCorridorPositions = new List<Vector2Int>();
        List<Vector2Int> outsideRoomPositions = new List<Vector2Int>();
        List<Vector2Int> insideRoomPositions = new List<Vector2Int>();
        List<Vector2Int> otherCorridorPositions = new List<Vector2Int>();

        // Categorize all floor positions
        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                if (dungeonMap[x, y] == TileType.Floor || dungeonMap[x, y] == TileType.NonEssentialFloor || dungeonMap[x, y] == TileType.BossFloor)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    Room room = GetRoomAtPosition(x, y);
                    
                    if (room != null)
                    {
                        // Check if this is a corner room first
                        if (IsCornerRoom(room))
                        {
                            cornerRoomPositions.Add(pos);
                        }
                        // Then check if it's an outside room
                        else if (IsOutsideRoom(room))
                        {
                            outsideRoomPositions.Add(pos);
                        }
                        else
                        {
                            insideRoomPositions.Add(pos);
                        }
                    }
                    else
                    {
                        // This is a corridor tile - check if it's in a corner area
                        if (IsCornerPosition(x, y))
                        {
                            cornerCorridorPositions.Add(pos);
                        }
                        else
                        {
                            otherCorridorPositions.Add(pos);
                        }
                    }
                }
            }
        }

        // Priority order: corner rooms > corner corridors > outside rooms > inside rooms > other corridors
        Debug.Log($"Player placement options - Corner rooms: {cornerRoomPositions.Count}, Corner corridors: {cornerCorridorPositions.Count}, Outside rooms: {outsideRoomPositions.Count}, Inside rooms: {insideRoomPositions.Count}, Other corridors: {otherCorridorPositions.Count}");
        
        if (cornerRoomPositions.Count > 0)
        {
            Vector2Int randomPos = cornerRoomPositions[Random.Range(0, cornerRoomPositions.Count)];
            Debug.Log($"Player placed in corner room at ({randomPos.x}, {randomPos.y})");
            return new Vector3(randomPos.x, randomPos.y, 0);
        }
        else if (cornerCorridorPositions.Count > 0)
        {
            Vector2Int randomPos = cornerCorridorPositions[Random.Range(0, cornerCorridorPositions.Count)];
            Debug.Log($"Player placed in corner corridor at ({randomPos.x}, {randomPos.y}) - no corner rooms available");
            return new Vector3(randomPos.x, randomPos.y, 0);
        }
        else if (outsideRoomPositions.Count > 0)
        {
            Vector2Int randomPos = outsideRoomPositions[Random.Range(0, outsideRoomPositions.Count)];
            Debug.Log($"Player placed in outside room at ({randomPos.x}, {randomPos.y}) - no corner options available");
            return new Vector3(randomPos.x, randomPos.y, 0);
        }
        else if (insideRoomPositions.Count > 0)
        {
            Vector2Int randomPos = insideRoomPositions[Random.Range(0, insideRoomPositions.Count)];
            Debug.Log($"Player placed in inside room at ({randomPos.x}, {randomPos.y}) - no outside options available");
            return new Vector3(randomPos.x, randomPos.y, 0);
        }
        else if (otherCorridorPositions.Count > 0)
        {
            Vector2Int randomPos = otherCorridorPositions[Random.Range(0, otherCorridorPositions.Count)];
            Debug.Log($"Player placed in corridor at ({randomPos.x}, {randomPos.y}) - no rooms available");
            return new Vector3(randomPos.x, randomPos.y, 0);
        }

        // Fallback if no floor tiles
        return new Vector3(dungeonWidth / 2, dungeonHeight / 2, 0);
    }

    // Check if a room is on the outside edge of the dungeon
    private bool IsOutsideRoom(Room room)
    {
        // Calculate the room's center position
        int roomCenterX = room.x + room.width / 2;
        int roomCenterY = room.y + room.height / 2;

        // Calculate the dungeon's center
        int dungeonCenterX = dungeonWidth / 2;
        int dungeonCenterY = dungeonHeight / 2;

        // Calculate distance from dungeon center
        float distanceFromCenter = Vector2.Distance(
            new Vector2(roomCenterX, roomCenterY),
            new Vector2(dungeonCenterX, dungeonCenterY)
        );

        // Calculate the maximum possible distance from center (corner to center)
        float maxDistance = Vector2.Distance(
            new Vector2(0, 0),
            new Vector2(dungeonCenterX, dungeonCenterY)
        );

        // A room is considered "outside" if it's in the outer 30% of the dungeon
        float outsideThreshold = maxDistance * 0.9f;
        
        return distanceFromCenter >= outsideThreshold;
    }

    // Check if a room is in one of the four corner cells of the dungeon
    private bool IsCornerRoom(Room room)
    {
        // Don't consider the boss room as a corner room
        if (enableBossRoom && room == bossRoom)
            return false;

        // Calculate which cell this room is in
        int roomCellX = (room.x - bufferX) / cellWidth;
        int roomCellY = (room.y - bufferY) / cellHeight;

        // Check if the room is in one of the four corner cells
        bool isCorner = (roomCellX == 0 && roomCellY == 0) ||                    // Bottom-left corner
                       (roomCellX == 0 && roomCellY == cellsY - 1) ||            // Top-left corner
                       (roomCellX == cellsX - 1 && roomCellY == 0) ||            // Bottom-right corner
                       (roomCellX == cellsX - 1 && roomCellY == cellsY - 1);     // Top-right corner
        
        // Debug logging
        if (isCorner)
        {
            Debug.Log($"Corner room found in cell ({roomCellX}, {roomCellY}) at room center ({room.x + room.width / 2}, {room.y + room.height / 2})");
        }
        
        return isCorner;
    }

    // Check if a specific tile position is in one of the four corner cells of the dungeon
    private bool IsCornerPosition(int x, int y)
    {
        // Calculate which cell this position is in
        int cellX = (x - bufferX) / cellWidth;
        int cellY = (y - bufferY) / cellHeight;

        // Check if the position is in one of the four corner cells
        return (cellX == 0 && cellY == 0) ||                    // Bottom-left corner
               (cellX == 0 && cellY == cellsY - 1) ||            // Top-left corner
               (cellX == cellsX - 1 && cellY == 0) ||            // Bottom-right corner
               (cellX == cellsX - 1 && cellY == cellsY - 1);     // Top-right corner
    }

    // Create a boss room in the center of the dungeon spanning 1.5x1.5 cells
    private void CreateBossRoom()
    {
        // Calculate the center cells (1.5x1.5 area)
        int centerCellX = cellsX / 2 - 1; // Start from left of center
        int centerCellY = cellsY / 2 - 1; // Start from bottom of center
        
        // Ensure we have at least 2x2 cells available for the 1.5x1.5 area
        if (centerCellX < 0 || centerCellY < 0 || centerCellX + 1 >= cellsX || centerCellY + 1 >= cellsY)
        {
            Debug.LogWarning("Dungeon too small for boss room! Skipping boss room generation.");
            return;
        }

        // Calculate the boss room position and size (1.5 cells = 1 full cell + 0.5 cell)
        int bossRoomStartX = bufferX + centerCellX * cellWidth + cellWidth / 4; // Start 1/4 into the first cell
        int bossRoomStartY = bufferY + centerCellY * cellHeight + cellHeight / 4; // Start 1/4 into the first cell
        int bossRoomWidth = (int)(cellWidth * 1.5f);  // Span 1.5 cells horizontally
        int bossRoomHeight = (int)(cellHeight * 1.5f); // Span 1.5 cells vertically

        // Create the boss room
        bossRoom = new Room();
        bossRoom.x = bossRoomStartX;
        bossRoom.y = bossRoomStartY;
        bossRoom.width = bossRoomWidth;
        bossRoom.height = bossRoomHeight;
        bossRoom.isSpecialShape = false; // Boss room is always rectangular

        // Add the boss room to our list
        rooms.Add(bossRoom);

        // Carve out the boss room
        CarveRoom(bossRoom);

        Debug.Log($"Boss room created at ({bossRoomStartX}, {bossRoomStartY}) with size {bossRoomWidth}x{bossRoomHeight}");
    }

    // Place a cracked wall in the boss room for the puzzle
    private void PlaceCrackedWallInBossRoom()
    {
        if (bossRoom == null) return;

        // Find a suitable wall position in the boss room
        // We'll place it on one of the walls, preferably not in a corner
        List<Vector2Int> possiblePositions = new List<Vector2Int>();

        // Check top wall (excluding corners)
        for (int x = bossRoom.x + 1; x < bossRoom.x + bossRoom.width - 1; x++)
        {
            if (dungeonMap[x, bossRoom.y + bossRoom.height] == TileType.WallTop)
            {
                possiblePositions.Add(new Vector2Int(x, bossRoom.y + bossRoom.height));
            }
        }

        // Check bottom wall (excluding corners)
        for (int x = bossRoom.x + 1; x < bossRoom.x + bossRoom.width - 1; x++)
        {
            if (dungeonMap[x, bossRoom.y - 1] == TileType.WallBottom)
            {
                possiblePositions.Add(new Vector2Int(x, bossRoom.y - 1));
            }
        }

        // Check left wall (excluding corners)
        for (int y = bossRoom.y + 1; y < bossRoom.y + bossRoom.height - 1; y++)
        {
            if (dungeonMap[bossRoom.x - 1, y] == TileType.WallLeft)
            {
                possiblePositions.Add(new Vector2Int(bossRoom.x - 1, y));
            }
        }

        // Check right wall (excluding corners)
        for (int y = bossRoom.y + 1; y < bossRoom.y + bossRoom.height - 1; y++)
        {
            if (dungeonMap[bossRoom.x + bossRoom.width, y] == TileType.WallRight)
            {
                possiblePositions.Add(new Vector2Int(bossRoom.x + bossRoom.width, y));
            }
        }

        // Choose a random position from the possible positions
        if (possiblePositions.Count > 0)
        {
            Vector2Int chosenPos = possiblePositions[Random.Range(0, possiblePositions.Count)];
            dungeonMap[chosenPos.x, chosenPos.y] = TileType.CrackedWall;
            Debug.Log($"Cracked wall placed at ({chosenPos.x}, {chosenPos.y}) in boss room");
        }
        else
        {
            Debug.LogWarning("No suitable position found for cracked wall in boss room!");
        }
    }

    // Check if a cell is in the boss room area (1.5x1.5 center cells)
    private bool IsInBossRoomArea(int cellX, int cellY)
    {
        if (!enableBossRoom) return false;

        // Calculate the center cells
        int centerCellX = cellsX / 2 - 1;
        int centerCellY = cellsY / 2 - 1;

        // Check if the cell is within the 1.5x1.5 center area
        // This covers the center cell and part of the adjacent cells
        return cellX >= centerCellX && cellX <= centerCellX + 1 &&
               cellY >= centerCellY && cellY <= centerCellY + 1;
    }

    // Check if a specific tile position is within the boss room
    private bool IsPositionInBossRoom(int x, int y)
    {
        if (bossRoom == null) return false;
        
        return x >= bossRoom.x && x < bossRoom.x + bossRoom.width &&
               y >= bossRoom.y && y < bossRoom.y + bossRoom.height;
    }



    // Initialize the minimap
    void InitializeMinimap()
    {
        if (minimapContainer == null)
        {
            Debug.LogWarning("Minimap container not assigned in DungeonGenerator!");
            return;
        }

        // Create a render texture for the minimap
        renderTexture = new RenderTexture(dungeonWidth, dungeonHeight, 0);
        renderTexture.filterMode = FilterMode.Point; // Pixelated look

        // Create a texture for drawing
        drawingTexture = new Texture2D(dungeonWidth, dungeonHeight);
        drawingTexture.filterMode = FilterMode.Point;

        // Initialize the texture with all unexplored (black)
        Color32[] pixels = new Color32[dungeonWidth * dungeonHeight];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = unexploredColor;
        }
        drawingTexture.SetPixels32(pixels);
        drawingTexture.Apply();

        // Initialize the explored tiles array
        exploredTiles = new bool[dungeonWidth, dungeonHeight];

        // Create a UI RawImage to display the minimap
        minimapImage = minimapContainer.GetComponentInChildren<RawImage>();
        if (minimapImage == null)
        {
            minimapImage = new GameObject("MinimapImage").AddComponent<RawImage>();
            minimapImage.rectTransform.SetParent(minimapContainer, false);
            minimapImage.rectTransform.anchorMin = Vector2.zero;
            minimapImage.rectTransform.anchorMax = Vector2.one;
            minimapImage.rectTransform.offsetMin = Vector2.zero;
            minimapImage.rectTransform.offsetMax = Vector2.zero;
        }

        // Set the texture to our drawing texture
        minimapImage.texture = drawingTexture;

        // Reveal the entire dungeon on minimap
        RevealEntireDungeon();
    }

    // Update the texture used by the minimap
    void UpdateMinimapTexture()
    {
        if (minimapImage != null && drawingTexture != null)
        {
            minimapImage.texture = drawingTexture;
        }
    }

    public void UpdateMinimapExploration(Vector2 playerPosition, float corridorViewRadius = 2f)
    {
        if (drawingTexture == null || exploredTiles == null)
        {
            return;
        }

        int playerX = Mathf.RoundToInt(playerPosition.x);
        int playerY = Mathf.RoundToInt(playerPosition.y);
        Vector2Int currentPlayerPos = new Vector2Int(playerX, playerY);

        bool textureNeedsUpdate = false;

        // Clear the previous player position if it exists
        if (lastPlayerPosition.x >= 0 && lastPlayerPosition.y >= 0
            && lastPlayerPosition.x < dungeonWidth && lastPlayerPosition.y < dungeonHeight)
        {
            Color tileColor = GetTileColor(lastPlayerPosition.x, lastPlayerPosition.y);
            drawingTexture.SetPixel(lastPlayerPosition.x, lastPlayerPosition.y, tileColor);
            textureNeedsUpdate = true;
        }

        // COMMENTED OUT EXPLORATION CODE - UNCOMMENT LATER FOR FOG OF WAR
        /*
        // First, check if player is in a room
        Room currentRoom = GetRoomAtPosition(playerX, playerY);
        if (currentRoom != null)
        {
            // Reveal the entire room
            for (int x = currentRoom.x - 1; x <= currentRoom.x + currentRoom.width; x++)
            {
                for (int y = currentRoom.y - 1; y <= currentRoom.y + currentRoom.height; y++)
                {
                    if (x >= 0 && x < dungeonWidth && y >= 0 && y < dungeonHeight)
                    {
                        if (!exploredTiles[x, y])
                        {
                            exploredTiles[x, y] = true;
                            Color tileColor = GetTileColor(x, y);
                            drawingTexture.SetPixel(x, y, tileColor);
                            textureNeedsUpdate = true;
                        }
                    }
                }
            }

            // Reveal corridors connected to room
            for (int x = currentRoom.x - 1; x <= currentRoom.x + currentRoom.width; x++)
            {
                for (int y = currentRoom.y - 1; y <= currentRoom.y + currentRoom.height; y++)
                {
                    if (x >= 0 && x < dungeonWidth && y >= 0 && y < dungeonHeight)
                    {
                        if (dungeonMap[x, y] == TileType.Floor && !IsInRoom(x, y))
                        {
                            RevealTilesAroundPoint(x, y, 1, ref textureNeedsUpdate);
                        }
                    }
                }
            }
        }
        else // Player is in a corridor
        {
            RevealTilesAroundPoint(playerX, playerY, (int)corridorViewRadius, ref textureNeedsUpdate);
        }
        */

        // Draw the player's new position
        if (playerX >= 0 && playerX < dungeonWidth && playerY >= 0 && playerY < dungeonHeight)
        {
            drawingTexture.SetPixel(playerX, playerY, playerColor);
            lastPlayerPosition = currentPlayerPos;
            textureNeedsUpdate = true;
        }

        if (textureNeedsUpdate)
        {
            drawingTexture.Apply();
            UpdateMinimapTexture();
        }
    }

    private void RevealTilesAroundPoint(int centerX, int centerY, int radius, ref bool textureNeedsUpdate)
    {
        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                if (x >= 0 && x < dungeonWidth && y >= 0 && y < dungeonHeight)
                {
                    if (!exploredTiles[x, y])
                    {
                        exploredTiles[x, y] = true;
                        Color tileColor = GetTileColor(x, y);
                        drawingTexture.SetPixel(x, y, tileColor);
                        textureNeedsUpdate = true;
                    }
                }
            }
        }
    }

    private bool IsInRoom(int x, int y)
    {
        return GetRoomAtPosition(x, y) != null;
    }

    private Room GetRoomAtPosition(int x, int y)
    {
        foreach (Room room in rooms)
        {
            if (x >= room.x && x < room.x + room.width &&
                y >= room.y && y < room.y + room.height)
            {
                // For special shaped rooms, check if the point is in an excluded area
                if (room.isSpecialShape && room.excludedAreas != null)
                {
                    bool inExcludedArea = false;
                    foreach (Rect excluded in room.excludedAreas)
                    {
                        if (x >= excluded.x && x < excluded.x + excluded.width &&
                            y >= excluded.y && y < excluded.y + excluded.height)
                        {
                            inExcludedArea = true;
                            break;
                        }
                    }

                    if (inExcludedArea)
                        continue;
                }

                return room;
            }
        }
        return null;
    }

    private Color GetTileColor(int x, int y)
    {
        switch (dungeonMap[x, y])
        {
            case TileType.Floor:
                return floorColor;
            case TileType.NonEssentialFloor:
                return floorColor; // Use same color as regular floor for now
            case TileType.BossFloor:
                return bossRoomColor;
            case TileType.WallLeft:
                return wallColor;
            case TileType.WallRight:
                return wallColor;
            case TileType.WallTop:
                return wallColor;
            case TileType.WallBottom:
                return wallColor;

            case TileType.WallCornerTopLeft:
                return wallColor;
            case TileType.WallCornerTopRight:
                return wallColor;
            case TileType.WallCornerBottomLeft:
                return wallColor;
            case TileType.WallCornerBottomRight:
                return wallColor;
            case TileType.CrackedWall:
                return new Color(0.8f, 0.4f, 0.2f, 1f); // Orange-brown color for cracked wall
            default:
                return unexploredColor;
        }
    }

    // Reveal the entire dungeon on the minimap
    private void RevealEntireDungeon()
    {
        if (drawingTexture == null || exploredTiles == null)
        {
            return;
        }

        // Reveal all tiles in the dungeon
        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                exploredTiles[x, y] = true;
                Color tileColor = GetTileColor(x, y);
                drawingTexture.SetPixel(x, y, tileColor);
            }
        }

        // Apply the changes to the texture
        drawingTexture.Apply();
        UpdateMinimapTexture();
    }

    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }

    public void ClearCurrentDungeon()
    {
        // Destroy all children of dungeonParent
        while (dungeonParent.childCount > 0)
        {
            DestroyImmediate(dungeonParent.GetChild(0).gameObject);
        }

        // Reset the dungeon map
        dungeonMap = new TileType[dungeonWidth, dungeonHeight];
    }


    private bool IsValidLadderPosition(int x, int y)
    {
        // Don't place stairs in the boss room
        if (enableBossRoom && IsPositionInBossRoom(x, y))
            return false;

        // Check if position is in a room
        Room room = GetRoomAtPosition(x, y);
        if (room == null) return false;

        // Check if position is not too close to room edges
        if (x <= room.x + 1 || x >= room.x + room.width - 1 ||
            y <= room.y + 1 || y >= room.y + room.height - 1)
            return false;

        return true;
    }

    // Add this public method to access tile types
    public TileType GetTileType(int x, int y)
    {
        if (x < 0 || x >= dungeonWidth || y < 0 || y >= dungeonHeight)
            return TileType.Void;

        return dungeonMap[x, y];
    }

    public void ResetMinimap()
    {
        // Reset the explored tiles array
        exploredTiles = new bool[dungeonWidth, dungeonHeight];

        // Clear the drawing texture
        if (drawingTexture != null)
        {
            Color32[] resetColors = new Color32[dungeonWidth * dungeonHeight];
            for (int i = 0; i < resetColors.Length; i++)
            {
                resetColors[i] = new Color32((byte)unexploredColor.r,
                                             (byte)unexploredColor.g,
                                             (byte)unexploredColor.b,
                                             (byte)unexploredColor.a);
            }
            drawingTexture.SetPixels32(resetColors);
            drawingTexture.Apply();
        }
    }

    public void MarkSpecialOnMinimap(Vector3 position, Color color)
    {
        if (drawingTexture != null)
        {
            int x = Mathf.RoundToInt(position.x);
            int y = Mathf.RoundToInt(position.y);

            if (x >= 0 && x < dungeonWidth && y >= 0 && y < dungeonHeight)
            {
                // Mark only the center pixel instead of a 3x3 area
                drawingTexture.SetPixel(x, y, color);
                exploredTiles[x, y] = true;

                // Optionally mark 1 pixel in each cardinal direction for a small + shape
                // This makes it slightly more visible than just 1 pixel
                if (x + 1 < dungeonWidth)
                {
                    drawingTexture.SetPixel(x + 1, y, color);
                    exploredTiles[x + 1, y] = true;
                }
                if (x - 1 >= 0)
                {
                    drawingTexture.SetPixel(x - 1, y, color);
                    exploredTiles[x - 1, y] = true;
                }
                if (y + 1 < dungeonHeight)
                {
                    drawingTexture.SetPixel(x, y + 1, color);
                    exploredTiles[x, y + 1] = true;
                }
                if (y - 1 >= 0)
                {
                    drawingTexture.SetPixel(x, y - 1, color);
                    exploredTiles[x, y - 1] = true;
                }

                drawingTexture.Apply();
            }
        }
    }



    // New method to check if player enters the stairs room
    private void CheckIfPlayerEnteredStairsRoom()
    {
        if (playerEnteredStairsRoom)
            return;

        if (stairsPosition == Vector2Int.zero)
        {
            Debug.LogWarning("Stairs position not set properly!");
            return;
        }

        // Get the player's grid position
        Vector2Int playerGridPos = new Vector2Int(
            Mathf.RoundToInt(player.transform.position.x),
            Mathf.RoundToInt(player.transform.position.y)
        );

        // Get the room that contains the stairs
        Room stairsRoom = GetRoomAtPosition(stairsPosition.x, stairsPosition.y);

        if (stairsRoom == null)
        {
            Debug.LogWarning("Could not find room containing stairs at position: " + stairsPosition);
            return;
        }

        // Check if player is in the same room as the stairs
        if (IsPlayerInRoom(stairsRoom, playerGridPos))
        {
            // Player has entered the stairs room!
            playerEnteredStairsRoom = true;

            // Make the stairs more visible on the minimap once discovered
            Vector3 stairsPos = new Vector3(stairsPosition.x, stairsPosition.y, 0);
            MarkSpecialOnMinimap(stairsPos, stairsColor);

            Debug.Log("Player entered room with stairs!");
        }
    }

    // Helper method to check if player is in a specific room
    private bool IsPlayerInRoom(Room room, Vector2Int playerPos)
    {
        // Check if player position is within room boundaries
        if (playerPos.x >= room.x && playerPos.x < room.x + room.width &&
            playerPos.y >= room.y && playerPos.y < room.y + room.height)
        {
            // For special shaped rooms, check if the player is in an excluded area
            if (room.isSpecialShape && room.excludedAreas != null)
            {
                foreach (Rect excluded in room.excludedAreas)
                {
                    if (playerPos.x >= excluded.x && playerPos.x < excluded.x + excluded.width &&
                        playerPos.y >= excluded.y && playerPos.y < excluded.y + excluded.height)
                    {
                        return false; // Player is in an excluded part of the room
                    }
                }
            }

            return true; // Player is in the room
        }

        return false; // Player is not in the room
    }

    void AddNonEssentialCorridors()
    {
        // Skip if we have too few rooms
        if (rooms.Count < 3)
            return;

        // First, count existing connections for each room
        Dictionary<Room, int> connectionCount = new Dictionary<Room, int>();

        // Initialize all rooms with 0 connections (excluding boss room)
        foreach (Room room in rooms)
        {
            // Skip boss room for non-essential corridor connections
            if (enableBossRoom && room == bossRoom)
                continue;
                
            connectionCount[room] = 0;
        }

        // Count existing corridors by checking floor tiles around room perimeters
        foreach (Room room in rooms)
        {
            // Skip boss room for non-essential corridor connections
            if (enableBossRoom && room == bossRoom)
                continue;
                
            // Check the perimeter of the room for corridor connections
            for (int x = room.x - 1; x <= room.x + room.width; x++)
            {
                // Check top and bottom edges
                if (x >= 0 && x < dungeonWidth)
                {
                    int topY = room.y + room.height;
                    int bottomY = room.y - 1;

                    if (topY < dungeonHeight && dungeonMap[x, topY] == TileType.Floor)
                        connectionCount[room]++;

                    if (bottomY >= 0 && dungeonMap[x, bottomY] == TileType.Floor)
                        connectionCount[room]++;
                }
            }

            for (int y = room.y; y < room.y + room.height; y++)
            {
                // Check left and right edges
                if (y >= 0 && y < dungeonHeight)
                {
                    int leftX = room.x - 1;
                    int rightX = room.x + room.width;

                    if (leftX >= 0 && dungeonMap[leftX, y] == TileType.Floor)
                        connectionCount[room]++;

                    if (rightX < dungeonWidth && dungeonMap[rightX, y] == TileType.Floor)
                        connectionCount[room]++;
                }
            }
        }

        // First, ensure every room has at least one connection
        List<Room> roomsNeedingConnections = new List<Room>();
        foreach (Room room in rooms)
        {
            // Skip boss room for non-essential corridor connections
            if (enableBossRoom && room == bossRoom)
                continue;
                
            if (connectionCount[room] == 0)
            {
                roomsNeedingConnections.Add(room);
            }
        }

        // Connect isolated rooms to the nearest room
        foreach (Room isolatedRoom in roomsNeedingConnections)
        {
            Room closestRoom = FindClosestRoom(isolatedRoom, isolatedRoom);

            if (closestRoom != null)
            {
                ConnectRoomsWithCorridor(isolatedRoom, closestRoom);
                connectionCount[isolatedRoom]++;
                connectionCount[closestRoom]++;
            }
        }

        // Then, ensure every room has at least two connections
        roomsNeedingConnections.Clear();
        foreach (Room room in rooms)
        {
            // Skip boss room for non-essential corridor connections
            if (enableBossRoom && room == bossRoom)
                continue;
                
            if (connectionCount[room] < 2)
            {
                roomsNeedingConnections.Add(room);
            }
        }

        // Add a second connection for rooms that need it
        foreach (Room roomNeedingConnection in roomsNeedingConnections)
        {
            // Find the next best room to connect to (not already connected)
            List<Room> connectedRooms = FindConnectedRooms(roomNeedingConnection);
            Room bestRoom = FindClosestRoom(roomNeedingConnection, connectedRooms);

            if (bestRoom != null)
            {
                ConnectRoomsWithCorridor(roomNeedingConnection, bestRoom);
                connectionCount[roomNeedingConnection]++;
                connectionCount[bestRoom]++;
            }
        }

        // Removed distant room connections to reduce corridor density
    }

    // Check if two rooms are adjacent or very close
    bool AreRoomsAdjacent(Room a, Room b)
    {
        // Calculate distances between room edges
        int horizontalDistance = Mathf.Max(0, Mathf.Min(a.x + a.width, b.x + b.width) - Mathf.Max(a.x, b.x));
        int verticalDistance = Mathf.Max(0, Mathf.Min(a.y + a.height, b.y + b.height) - Mathf.Max(a.y, b.y));

        // If there's overlap in one dimension and rooms are close in the other dimension, they're adjacent
        return (horizontalDistance > 0 && Mathf.Abs(a.y - b.y) <= 5) ||
               (verticalDistance > 0 && Mathf.Abs(a.x - b.x) <= 5);
    }

    // New method to connect distant rooms with more complex corridors
    void ConnectDistantRooms(Room roomA, Room roomB, float normalizedDistance)
    {
        // Find center points of each room
        Vector2Int centerA = new Vector2Int(
            roomA.x + roomA.width / 2,
            roomA.y + roomA.height / 2
        );

        Vector2Int centerB = new Vector2Int(
            roomB.x + roomB.width / 2,
            roomB.y + roomB.height / 2
        );

        // Number of waypoints increases with distance
        int baseWaypoints = 2;
        int additionalWaypoints = Mathf.RoundToInt(normalizedDistance * 6); // Up to 6 more waypoints for most distant rooms
        int numWaypoints = baseWaypoints + additionalWaypoints;

        // More complex corridor for distant rooms
        Vector2Int[] waypoints = new Vector2Int[numWaypoints + 2]; // +2 for start and end points

        // Set start and end points
        waypoints[0] = centerA;
        waypoints[numWaypoints + 1] = centerB;

        // Direction bias (to create more vertical or horizontal oriented corridors)
        bool horizontalBias = Random.value > 0.5f;

        // Create intermediate waypoints with more variance for distant rooms
        for (int i = 1; i <= numWaypoints; i++)
        {
            float progress = i / (float)(numWaypoints + 1);

            // Base position interpolating between start and end
            int baseX = Mathf.RoundToInt(Mathf.Lerp(centerA.x, centerB.x, progress));
            int baseY = Mathf.RoundToInt(Mathf.Lerp(centerA.y, centerB.y, progress));

            // Add significant randomness for distant rooms
            int randXRange = Mathf.RoundToInt(10 * normalizedDistance) + 5; // 5-15 range based on distance
            int randYRange = Mathf.RoundToInt(10 * normalizedDistance) + 5;

            // Apply directional bias
            if (horizontalBias)
                randXRange = Mathf.RoundToInt(randXRange * 0.5f);
            else
                randYRange = Mathf.RoundToInt(randYRange * 0.5f);

            int randX = Random.Range(-randXRange, randXRange + 1);
            int randY = Random.Range(-randYRange, randYRange + 1);

            int waypointX = Mathf.Clamp(baseX + randX, 2, dungeonWidth - 3);
            int waypointY = Mathf.Clamp(baseY + randY, 2, dungeonHeight - 3);

            waypoints[i] = new Vector2Int(waypointX, waypointY);
        }

        // Carve the corridor that might pass by rooms without connecting directly
        CarveComplexCorridor(waypoints, roomA, roomB);
    }

    // New method to carve more complex corridors
    void CarveComplexCorridor(Vector2Int[] waypoints, Room roomA, Room roomB)
    {
        // Randomly choose narrow corridors for some paths
        int corridorWidth = Random.value > 0.7f ? 1 : 2;

        // Carve path between all waypoints
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Vector2Int start = waypoints[i];
            Vector2Int end = waypoints[i + 1];

            // Sometimes add a zigzag instead of just horizontal+vertical
            if (Random.value > 0.6f)
            {
                // Calculate a midpoint for the zigzag
                Vector2Int mid = new Vector2Int(
                    start.x + (end.x - start.x) / 2,
                    start.y + (end.y - start.y) / 2
                );

                // Randomly offset the midpoint
                mid.x += Random.Range(-5, 6);
                mid.y += Random.Range(-5, 6);
                mid.x = Mathf.Clamp(mid.x, 2, dungeonWidth - 3);
                mid.y = Mathf.Clamp(mid.y, 2, dungeonHeight - 3);

                // Ensure midpoint is not in boss room area
                if (enableBossRoom && IsPositionInBossRoom(mid.x, mid.y))
                {
                    // Try to find an alternative position outside the boss room
                    for (int attempt = 0; attempt < 10; attempt++)
                    {
                        mid.x = Mathf.Clamp(start.x + (end.x - start.x) / 2 + Random.Range(-8, 9), 2, dungeonWidth - 3);
                        mid.y = Mathf.Clamp(start.y + (end.y - start.y) / 2 + Random.Range(-8, 9), 2, dungeonHeight - 3);
                        
                        if (!IsPositionInBossRoom(mid.x, mid.y))
                            break;
                    }
                }

                // Carve to midpoint, then to endpoint
                if (Random.value > 0.5f)
                {
                    // Horizontal first, then vertical to mid
                    CarveNonEssentialSegment(start.x, start.y, mid.x, start.y, true, corridorWidth, roomA, roomB);
                    CarveNonEssentialSegment(mid.x, start.y, mid.x, mid.y, false, corridorWidth, roomA, roomB);

                    // Horizontal first, then vertical to end
                    CarveNonEssentialSegment(mid.x, mid.y, end.x, mid.y, true, corridorWidth, roomA, roomB);
                    CarveNonEssentialSegment(end.x, mid.y, end.x, end.y, false, corridorWidth, roomA, roomB);
                }
                else
                {
                    // Vertical first, then horizontal to mid
                    CarveNonEssentialSegment(start.x, start.y, start.x, mid.y, false, corridorWidth, roomA, roomB);
                    CarveNonEssentialSegment(start.x, mid.y, mid.x, mid.y, true, corridorWidth, roomA, roomB);

                    // Vertical first, then horizontal to end
                    CarveNonEssentialSegment(mid.x, mid.y, mid.x, end.y, false, corridorWidth, roomA, roomB);
                    CarveNonEssentialSegment(mid.x, end.y, end.x, end.y, true, corridorWidth, roomA, roomB);
                }
            }
            else
            {
                // Standard approach: horizontal then vertical
                if (Random.value > 0.5f)
                {
                    CarveNonEssentialSegment(start.x, start.y, end.x, start.y, true, corridorWidth, roomA, roomB);
                    CarveNonEssentialSegment(end.x, start.y, end.x, end.y, false, corridorWidth, roomA, roomB);
                }
                // Or vertical then horizontal
                else
                {
                    CarveNonEssentialSegment(start.x, start.y, start.x, end.y, false, corridorWidth, roomA, roomB);
                    CarveNonEssentialSegment(start.x, end.y, end.x, end.y, true, corridorWidth, roomA, roomB);
                }
            }
        }
    }

    // Find rooms that are already connected to the given room
    List<Room> FindConnectedRooms(Room room)
    {
        List<Room> connected = new List<Room>();

        // Check corridors extending from each side of the room
        for (int x = room.x - 1; x <= room.x + room.width; x++)
        {
            // Check top and bottom
            if (x >= 0 && x < dungeonWidth)
            {
                int topY = room.y + room.height;
                int bottomY = room.y - 1;

                if (topY < dungeonHeight && dungeonMap[x, topY] == TileType.Floor)
                {
                    Room connectedRoom = FindRoomConnectedByCorridor(new Vector2Int(x, topY), room);
                    if (connectedRoom != null && !connected.Contains(connectedRoom))
                        connected.Add(connectedRoom);
                }

                if (bottomY >= 0 && dungeonMap[x, bottomY] == TileType.Floor)
                {
                    Room connectedRoom = FindRoomConnectedByCorridor(new Vector2Int(x, bottomY), room);
                    if (connectedRoom != null && !connected.Contains(connectedRoom))
                        connected.Add(connectedRoom);
                }
            }
        }

        for (int y = room.y; y < room.y + room.height; y++)
        {
            // Check left and right
            if (y >= 0 && y < dungeonHeight)
            {
                int leftX = room.x - 1;
                int rightX = room.x + room.width;

                if (leftX >= 0 && dungeonMap[leftX, y] == TileType.Floor)
                {
                    Room connectedRoom = FindRoomConnectedByCorridor(new Vector2Int(leftX, y), room);
                    if (connectedRoom != null && !connected.Contains(connectedRoom))
                        connected.Add(connectedRoom);
                }

                if (rightX < dungeonWidth && dungeonMap[rightX, y] == TileType.Floor)
                {
                    Room connectedRoom = FindRoomConnectedByCorridor(new Vector2Int(rightX, y), room);
                    if (connectedRoom != null && !connected.Contains(connectedRoom))
                        connected.Add(connectedRoom);
                }
            }
        }

        return connected;
    }

    Room FindRoomConnectedByCorridor(Vector2Int startPoint, Room excludeRoom)
    {
        // Simple flood fill to find a room connected by a corridor
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(startPoint);
        visited.Add(startPoint);

        // Directions: up, right, down, left
        Vector2Int[] directions = {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // Check if we've reached a room
            Room currentRoom = GetRoomAtPosition(current.x, current.y);
            if (currentRoom != null && currentRoom != excludeRoom)
            {
                return currentRoom;
            }

            // Check all four directions
            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = current + dir;

                // Check if the next position is valid and not visited
                if (next.x >= 0 && next.x < dungeonWidth &&
                    next.y >= 0 && next.y < dungeonHeight &&
                    !visited.Contains(next) &&
                    dungeonMap[next.x, next.y] == TileType.Floor)
                {
                    queue.Enqueue(next);
                    visited.Add(next);
                }
            }
        }

        return null; // No room found
    }

    Room FindClosestRoom(Room sourceRoom, Room excludeRoom)
    {
        Room closest = null;
        float minDistance = float.MaxValue;

        foreach (Room room in rooms)
        {
            // Skip boss room and excluded room
            if (room != sourceRoom && room != excludeRoom && !(enableBossRoom && room == bossRoom))
            {
                float distance = Vector2.Distance(
                    new Vector2(sourceRoom.x + sourceRoom.width / 2, sourceRoom.y + sourceRoom.height / 2),
                    new Vector2(room.x + room.width / 2, room.y + room.height / 2)
                );

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = room;
                }
            }
        }

        return closest;
    }

    Room FindClosestRoom(Room sourceRoom, List<Room> excludeRooms)
    {
        Room closest = null;
        float minDistance = float.MaxValue;

        foreach (Room room in rooms)
        {
            // Skip boss room and excluded rooms
            if (room != sourceRoom && !excludeRooms.Contains(room) && !(enableBossRoom && room == bossRoom))
            {
                float distance = Vector2.Distance(
                    new Vector2(sourceRoom.x + sourceRoom.width / 2, sourceRoom.y + sourceRoom.height / 2),
                    new Vector2(room.x + room.width / 2, room.y + room.height / 2)
                );

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = room;
                }
            }
        }

        return closest;
    }

    void ConnectRoomsWithCorridor(Room roomA, Room roomB)
    {
        // Find center points of each room
        Vector2Int centerA = new Vector2Int(
            roomA.x + roomA.width / 2,
            roomA.y + roomA.height / 2
        );

        Vector2Int centerB = new Vector2Int(
            roomB.x + roomB.width / 2,
            roomB.y + roomB.height / 2
        );

        // Carve a winding corridor between the rooms
        CarveNonEssentialCorridor(centerA, centerB, roomA, roomB, 2);
    }

    void CarveNonEssentialCorridor(Vector2Int startPoint, Vector2Int endPoint, Room roomA, Room roomB, int width)
    {
        // Create 1-2 waypoints for the corridor path
        int numWaypoints = Random.Range(1, 3);
        Vector2Int[] waypoints = new Vector2Int[numWaypoints + 2]; // +2 for start and end points

        // Set start and end points
        waypoints[0] = startPoint;
        waypoints[numWaypoints + 1] = endPoint;

        // Create intermediate waypoints
        for (int i = 1; i <= numWaypoints; i++)
        {
            float progress = i / (float)(numWaypoints + 1);

            // Base position interpolating between start and end
            int baseX = Mathf.RoundToInt(Mathf.Lerp(startPoint.x, endPoint.x, progress));
            int baseY = Mathf.RoundToInt(Mathf.Lerp(startPoint.y, endPoint.y, progress));

            // Add some randomness
            int randX = Random.Range(-8, 9); // More randomness for non-essential paths
            int randY = Random.Range(-8, 9);

            int waypointX = Mathf.Clamp(baseX + randX, 2, dungeonWidth - 3);
            int waypointY = Mathf.Clamp(baseY + randY, 2, dungeonHeight - 3);

            // Ensure waypoint is not in boss room area
            if (enableBossRoom && IsPositionInBossRoom(waypointX, waypointY))
            {
                // Try to find an alternative position outside the boss room
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    randX = Random.Range(-12, 13);
                    randY = Random.Range(-12, 13);
                    waypointX = Mathf.Clamp(baseX + randX, 2, dungeonWidth - 3);
                    waypointY = Mathf.Clamp(baseY + randY, 2, dungeonHeight - 3);
                    
                    if (!IsPositionInBossRoom(waypointX, waypointY))
                        break;
                }
            }

            waypoints[i] = new Vector2Int(waypointX, waypointY);
        }

        // Carve the corridor, checking for room intersections
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Vector2Int start = waypoints[i];
            Vector2Int end = waypoints[i + 1];

            // Carve horizontal segment
            CarveNonEssentialSegment(
                start.x, start.y,
                end.x, start.y,
                true, width, roomA, roomB);

            // Carve vertical segment
            CarveNonEssentialSegment(
                end.x, start.y,
                end.x, end.y,
                false, width, roomA, roomB);
        }
    }

    void CarveNonEssentialSegment(int x1, int y1, int x2, int y2, bool horizontal, int width, Room roomA, Room roomB)
    {
        int start, end;

        if (horizontal)
        {
            // Horizontal segment
            start = Mathf.Min(x1, x2);
            end = Mathf.Max(x1, x2);

            for (int x = start; x <= end; x++)
            {
                for (int w = 0; w < width; w++)
                {
                    int yPos = y1 - width / 2 + w;

                    if (CanCarveNonEssentialTile(x, yPos, roomA, roomB))
                    {
                        dungeonMap[x, yPos] = TileType.NonEssentialFloor;
                    }
                }
            }
        }
        else
        {
            // Vertical segment
            start = Mathf.Min(y1, y2);
            end = Mathf.Max(y1, y2);

            for (int y = start; y <= end; y++)
            {
                for (int w = 0; w < width; w++)
                {
                    int xPos = x1 - width / 2 + w;

                    if (CanCarveNonEssentialTile(xPos, y, roomA, roomB))
                    {
                        dungeonMap[xPos, y] = TileType.NonEssentialFloor;
                    }
                }
            }
        }
    }

    bool CanCarveNonEssentialTile(int x, int y, Room allowedRoomA, Room allowedRoomB)
    {
        // Check if position is within dungeon bounds
        if (x < 0 || x >= dungeonWidth || y < 0 || y >= dungeonHeight)
            return false;

        // Never allow carving in the boss room area
        if (enableBossRoom && IsPositionInBossRoom(x, y))
            return false;

        // Always allow carving if the tile is already a corridor (TileType.Floor or NonEssentialFloor)
        // or if it's currently void (not yet carved)
        if (dungeonMap[x, y] == TileType.Floor || dungeonMap[x, y] == TileType.NonEssentialFloor || dungeonMap[x, y] == TileType.Void)
            return true;

        // Check if the position is inside one of our allowed rooms
        Room containingRoom = GetRoomAtPosition(x, y);
        if (containingRoom != null)
        {
            // Only allow if it's one of our two rooms
            return (containingRoom == allowedRoomA || containingRoom == allowedRoomB);
        }

        // If we get here, the tile is not in a room and not a floor or void
        return false;
    }
}