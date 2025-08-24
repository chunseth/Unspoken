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
    
    [Header("Boss Room Barrier Prefabs")]
    public GameObject bossRoomBarrierPrefab;  // Physical barrier prefab for boss room locking
    
    [Header("Boss Room Special Prefabs")]
    public GameObject crackedWallPrefab;  // Special cracked wall for boss room
    
    [Header("Dungeon Puzzle Prefabs")]
    public GameObject crackedWall2Prefab;  // Second cracked wall for dungeon placement
    public GameObject crackedWall3Prefab;  // Third cracked wall for additional dungeon placement
    public GameObject carryableObjectPrefab;  // Carryable object for puzzle interaction
    public GameObject holePrefab;  // Hole that causes player to respawn and lose health
    public GameObject puzzleSolutionHint1Prefab;  // Puzzle solution hint for CrackedWall1
    public GameObject puzzleSolutionHint2Prefab;  // Puzzle solution hint for CrackedWall2



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
    public Color specialRoomColor = new Color(0f, 1f, 0f, 1f); // Green for special room
    public Color holeColor = new Color(0f, 0f, 0f, 1f); // Black for holes

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
public GameObject royalSlimeBossPrefab; // Royal Slime boss prefab
private Room bossRoom; // The boss room in the center
private GameObject spawnedBoss; // Reference to the spawned boss

    [Header("Special Room Settings")]
    public bool enableSpecialRoom = true;
    public GameObject specialRoomFloorPrefab; // Optional special floor for the special room
    public GameObject crackedWall4Prefab; // CrackedWall4 prefab for the platform
    private Room specialRoom; // The special room that always spawns
    private Vector2Int specialRoomEntrance; // Track where the corridor connects to the special room
    private Vector2Int specialRoomPlatformCenter; // Track the center of the platform for CrackedWall4 placement

    [Header("Puzzle Integration")]
    public bool enablePuzzleBossRoomLock = true; // Whether to lock boss room until all puzzles are solved
    private PuzzleManager puzzleManager; // Reference to the puzzle manager
    private bool bossRoomWasLocked = true; // Track previous lock state for boss room unlock
    private List<Vector2Int> bossRoomBarrierTiles = new List<Vector2Int>(); // Track barrier tiles around boss room
    public List<GameObject> bossRoomBarrierObjects = new List<GameObject>(); // Track actual barrier GameObjects

    void Start()
    {
        // Find the puzzle manager
        puzzleManager = FindObjectOfType<PuzzleManager>();
        if (puzzleManager == null)
        {
            enablePuzzleBossRoomLock = false;
        }
        
        GenerateDungeon();
        InitializeMinimap();
        InstantiateDungeon();
        
        // Refresh puzzle manager after dungeon generation is complete
        if (puzzleManager != null)
        {
            puzzleManager.ForceRefreshPuzzles();
            puzzleManager.SetDungeonInstantiated();
        }
        
        // Create barriers around boss room if it's locked
        if (enablePuzzleBossRoomLock && enableBossRoom && IsBossRoomLocked())
        {
            CreateBossRoomBarriers();
        }
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

        // Check if boss room should be unlocked (all puzzles solved)
        if (enablePuzzleBossRoomLock && puzzleManager != null)
        {
            CheckBossRoomUnlock();
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

                // 3. Create special room first (if enabled) so we know its location
        if (enableSpecialRoom)
        {
            CreateSpecialRoom();
        }

        // 4. Create boss room in the center if enabled (before corridor generation)
        if (enableBossRoom)
        {
            CreateBossRoom();
        }

        // 5. Connect cells using a depth-first search (DFS) algorithm.
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

        // 6. For each cell, generate a room with a given chance (skip center cells if boss room exists, and special room area).
        
        for (int x = 0; x < cellsX; x++)
        {
            for (int y = 0; y < cellsY; y++)
            {
                // Skip center cells if boss room is enabled
                if (enableBossRoom && IsInBossRoomArea(x, y))
                {
                    continue;
                }

                // Skip cells if special room is enabled and this cell is in the special room area
                if (enableSpecialRoom && IsInSpecialRoomArea(x, y))
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

        // 6.5. Connect the special room with exactly one corridor (after regular rooms are created)
        if (enableSpecialRoom)
        {
            ConnectSpecialRoom();
        }

        // 7. Add non-essential corridors for better connectivity
        AddNonEssentialCorridors();

        // 8. Add walls around floor tiles (after all floor types are placed)
        AddWalls();

        // 9. Add special boss room features (removed CrackedWall1 placement from boss room)
        // CrackedWall1 is now placed randomly in the dungeon via PlaceCrackedWall1InDungeon()
        
        // 10. Add CrackedWall1 randomly in the dungeon (not in boss room or special room)
        PlaceCrackedWall1InDungeon();
        
        // 11. Add CrackedWall2 instances in the dungeon (not in boss room)
        PlaceCrackedWall2InDungeon();
        
        // 12. Add CrackedWall3 and CarryableObject in the same room
        PlaceCrackedWall3AndCarryableObject();

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
        // Skip corridor generation if either cell is in the special room area
        if (enableSpecialRoom && (IsInSpecialRoomArea(a.cellX, a.cellY) || IsInSpecialRoomArea(b.cellX, b.cellY)))
        {
            return;
        }

        // Skip corridor generation if either cell is in the boss room area (boss room is created before corridors)
        if (enableBossRoom && (IsInBossRoomArea(a.cellX, a.cellY) || IsInBossRoomArea(b.cellX, b.cellY)))
        {
            return;
        }

        // Skip corridor generation if boss room is locked and either cell is in the boss room area
        if (enablePuzzleBossRoomLock && enableBossRoom && IsBossRoomLockedDuringGeneration() && 
            (IsInBossRoomArea(a.cellX, a.cellY) || IsInBossRoomArea(b.cellX, b.cellY)))
        {
            return;
        }

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
                {
                    // Skip if this position is in the boss room area
                    if (enableBossRoom && IsPositionInBossRoom(x, yPos))
                        continue;
                        
                    dungeonMap[x, yPos] = TileType.Floor;
                }
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
                {
                    // Skip if this position is in the boss room area
                    if (enableBossRoom && IsPositionInBossRoom(xPos, y))
                        continue;
                        
                    dungeonMap[xPos, y] = TileType.Floor;
                }
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
                        // Use BossFloor for boss room, SpecialFloor for special room, regular Floor for other rooms
                        if (enableBossRoom && room == bossRoom)
                        {
                            dungeonMap[x, y] = TileType.BossFloor;
                        }
                        else if (enableSpecialRoom && room == specialRoom)
                        {
                            dungeonMap[x, y] = TileType.SpecialFloor;
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
                    // Use BossFloor for boss room, SpecialFloor for special room, regular Floor for other rooms
                    if (enableBossRoom && room == bossRoom)
                    {
                        dungeonMap[x, y] = TileType.BossFloor;
                    }
                    else if (enableSpecialRoom && room == specialRoom)
                    {
                        dungeonMap[x, y] = TileType.SpecialFloor;
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
        // Count CrackedWall4 tiles before instantiation
        int crackedWall4Count = 0;
        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                if (dungeonMap[x, y] == TileType.CrackedWall4)
                {
                    crackedWall4Count++;
                }
            }
        }
        
        int puzzleHint1Count = 0;
        int puzzleHint2Count = 0;
        
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
                    case TileType.SpecialFloor:
                        // Use special room floor prefab if available, otherwise use regular floor
                        GameObject specialFloorToUse = specialRoomFloorPrefab != null ? specialRoomFloorPrefab : floorPrefab;
                        Instantiate(specialFloorToUse, pos, Quaternion.identity, dungeonParent);
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
                    case TileType.CrackedWall2:
                        Instantiate(crackedWall2Prefab, pos, Quaternion.identity, dungeonParent);
                        break;
                                            case TileType.CrackedWall3:
                            Instantiate(crackedWall3Prefab, pos, Quaternion.identity, dungeonParent);
                            break;
                        case TileType.CrackedWall4:
                            Instantiate(crackedWall4Prefab, pos, Quaternion.identity, dungeonParent);
                            break;
                        case TileType.CarryableObject:
                        Instantiate(carryableObjectPrefab, pos, Quaternion.identity, dungeonParent);
                        break;
                    case TileType.Hole:
                        Instantiate(holePrefab, pos, Quaternion.identity, dungeonParent);
                        break;
                    case TileType.PuzzleSolutionHint1:
                        puzzleHint1Count++;
                        if (puzzleSolutionHint1Prefab != null)
                        {
                            GameObject hintInstance = Instantiate(puzzleSolutionHint1Prefab, pos, Quaternion.identity, dungeonParent);
                        }
                        break;
                    case TileType.PuzzleSolutionHint2:
                        puzzleHint2Count++;
                        if (puzzleSolutionHint2Prefab != null)
                        {
                            GameObject hintInstance = Instantiate(puzzleSolutionHint2Prefab, pos, Quaternion.identity, dungeonParent);
                        }
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
        }
        else
        {
        }
    }

    // Place the player at a random floor position using GetRandomFloorPosition().
    void PlacePlayer()
    {
        if (player == null)
        {
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
                if (dungeonMap[x, y] == TileType.Floor || dungeonMap[x, y] == TileType.NonEssentialFloor || dungeonMap[x, y] == TileType.BossFloor || dungeonMap[x, y] == TileType.SpecialFloor)
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
        
        if (cornerRoomPositions.Count > 0)
        {
            Vector2Int randomPos = cornerRoomPositions[Random.Range(0, cornerRoomPositions.Count)];
            return new Vector3(randomPos.x, randomPos.y, 0);
        }
        else if (cornerCorridorPositions.Count > 0)
        {
            Vector2Int randomPos = cornerCorridorPositions[Random.Range(0, cornerCorridorPositions.Count)];
            return new Vector3(randomPos.x, randomPos.y, 0);
        }
        else if (outsideRoomPositions.Count > 0)
        {
            Vector2Int randomPos = outsideRoomPositions[Random.Range(0, outsideRoomPositions.Count)];
            return new Vector3(randomPos.x, randomPos.y, 0);
        }
        else if (insideRoomPositions.Count > 0)
        {
            Vector2Int randomPos = insideRoomPositions[Random.Range(0, insideRoomPositions.Count)];
            return new Vector3(randomPos.x, randomPos.y, 0);
        }
        else if (otherCorridorPositions.Count > 0)
        {
            Vector2Int randomPos = otherCorridorPositions[Random.Range(0, otherCorridorPositions.Count)];
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
        
        // Spawn the Royal Slime boss in the center of the boss room
        SpawnRoyalSlimeBoss();
    }
    
    // Spawn the Royal Slime boss in the center of the boss room
    private void SpawnRoyalSlimeBoss()
    {
        if (royalSlimeBossPrefab == null)
        {
            Debug.LogWarning("DungeonGenerator: Royal Slime boss prefab is not assigned!");
            return;
        }
        
        if (bossRoom == null)
        {
            Debug.LogWarning("DungeonGenerator: Boss room is null, cannot spawn boss!");
            return;
        }
        
        // Calculate the center position of the boss room
        Vector2 bossRoomCenter = new Vector2(
            bossRoom.x + bossRoom.width / 2f,
            bossRoom.y + bossRoom.height / 2f
        );
        
        // Convert tile coordinates to world coordinates
        Vector3 bossSpawnPosition = new Vector3(bossRoomCenter.x, bossRoomCenter.y, -5f);
        
        // Spawn the boss
        spawnedBoss = Instantiate(royalSlimeBossPrefab, bossSpawnPosition, Quaternion.identity);
        
        // Force the position to be set correctly (in case animation overrides it)
        spawnedBoss.transform.position = bossSpawnPosition;
        
        Debug.Log($"DungeonGenerator: Royal Slime boss spawned at position {bossSpawnPosition}");
    }

    // Create a special room that always spawns with 2 cells length and 1 cell height
    private void CreateSpecialRoom()
    {
        // Find a suitable location for the special room (avoid boss room area)
        Vector2Int? specialRoomLocation = FindSpecialRoomLocation();
        
        if (!specialRoomLocation.HasValue)
        {
            return;
        }

        // Calculate the special room position and size (2 cells length, 1 cell height)
        int specialRoomStartX = specialRoomLocation.Value.x;
        int specialRoomStartY = specialRoomLocation.Value.y;
        int specialRoomWidth = cellWidth * 2;  // Span 2 cells horizontally
        int specialRoomHeight = cellHeight;    // Span 1 cell vertically

        // Create the special room
        specialRoom = new Room();
        specialRoom.x = specialRoomStartX;
        specialRoom.y = specialRoomStartY;
        specialRoom.width = specialRoomWidth;
        specialRoom.height = specialRoomHeight;
        specialRoom.isSpecialShape = false; // Special room is always rectangular

        // Add the special room to our list
        rooms.Add(specialRoom);

        // Carve out the special room
        CarveRoom(specialRoom);

        // Fill the special room with holes (platform will be added later in ConnectSpecialRoom)
        FillSpecialRoomWithHoles();
    }



    // Find where the corridor connects to the special room
    private void FindSpecialRoomEntrance(Room connectedRoom)
    {
        // Find the center points of both rooms
        Vector2Int specialRoomCenter = new Vector2Int(
            specialRoom.x + specialRoom.width / 2,
            specialRoom.y + specialRoom.height / 2
        );
        
        Vector2Int connectedRoomCenter = new Vector2Int(
            connectedRoom.x + connectedRoom.width / 2,
            connectedRoom.y + connectedRoom.height / 2
        );
        
        // Determine which side of the special room the corridor connects to
        // by comparing the relative positions of the room centers
        Vector2 direction = ((Vector2)(specialRoomCenter - connectedRoomCenter)).normalized;
        
        // Find the entrance point on the special room boundary
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Horizontal connection (left or right side)
            if (direction.x > 0)
            {
                // Connected from the left, so entrance is on the left side
                specialRoomEntrance = new Vector2Int(specialRoom.x, specialRoom.y + specialRoom.height / 2);
            }
            else
            {
                // Connected from the right, so entrance is on the right side
                specialRoomEntrance = new Vector2Int(specialRoom.x + specialRoom.width - 1, specialRoom.y + specialRoom.height / 2);
            }
        }
        else
        {
            // Vertical connection (top or bottom side)
            if (direction.y > 0)
            {
                // Connected from the bottom, so entrance is on the bottom side
                specialRoomEntrance = new Vector2Int(specialRoom.x + specialRoom.width / 2, specialRoom.y);
            }
            else
            {
                // Connected from the top, so entrance is on the top side
                specialRoomEntrance = new Vector2Int(specialRoom.x + specialRoom.width / 2, specialRoom.y + specialRoom.height - 1);
            }
        }
    }

    // Add a platform on the opposite side from the entrance
    private void AddPlatformToSpecialRoom()
    {
        if (specialRoom == null) 
        {
            return;
        }
        
        // Calculate the opposite side from the entrance
        Vector2Int platformPosition = CalculateOppositePlatformPosition();
        
        // Store the platform center for CrackedWall4 placement
        specialRoomPlatformCenter = platformPosition;
        
        // Create a 3x3 platform attached to the wall
        CreatePlatform(platformPosition);
        
        // Create floor tile islands around the platform
        CreateFloorIslandsInSpecialRoom();
    }

    // Calculate the position for the platform on the opposite side from the entrance
    private Vector2Int CalculateOppositePlatformPosition()
    {
        Vector2Int specialRoomCenter = new Vector2Int(
            specialRoom.x + specialRoom.width / 2,
            specialRoom.y + specialRoom.height / 2
        );
        
        // Calculate the direction from center to entrance
        Vector2Int directionToEntrance = specialRoomEntrance - specialRoomCenter;
        
        // The opposite direction is the negative of the entrance direction
        Vector2Int oppositeDirection = -directionToEntrance;
        
        // Calculate the platform position on the opposite wall
        Vector2Int platformPosition;
        
        if (Mathf.Abs(oppositeDirection.x) > Mathf.Abs(oppositeDirection.y))
        {
            // Platform on left or right wall
            if (oppositeDirection.x > 0)
            {
                // Platform on right wall
                platformPosition = new Vector2Int(specialRoom.x + specialRoom.width - 1, specialRoom.y + specialRoom.height / 2);
            }
            else
            {
                // Platform on left wall
                platformPosition = new Vector2Int(specialRoom.x, specialRoom.y + specialRoom.height / 2);
            }
        }
        else
        {
            // Platform on top or bottom wall
            if (oppositeDirection.y > 0)
            {
                // Platform on top wall
                platformPosition = new Vector2Int(specialRoom.x + specialRoom.width / 2, specialRoom.y + specialRoom.height - 1);
            }
            else
            {
                // Platform on bottom wall
                platformPosition = new Vector2Int(specialRoom.x + specialRoom.width / 2, specialRoom.y);
            }
        }
        
        return platformPosition;
    }

    // Create a 3x3 platform attached to the wall
    private void CreatePlatform(Vector2Int centerPosition)
    {
        // Create a 3x3 platform with the center at the specified position
        for (int x = centerPosition.x - 1; x <= centerPosition.x + 1; x++)
        {
            for (int y = centerPosition.y - 1; y <= centerPosition.y + 1; y++)
            {
                // Check if the position is within the special room bounds
                if (x >= specialRoom.x && x < specialRoom.x + specialRoom.width &&
                    y >= specialRoom.y && y < specialRoom.y + specialRoom.height)
                {
                    // Place floor tiles for the platform
                    dungeonMap[x, y] = TileType.SpecialFloor;
                }
            }
        }
        
        // Place CrackedWall4 along the wall of the platform
        PlaceCrackedWall4OnPlatform(centerPosition);
    }

    // Place CrackedWall4 along the wall of the platform
    private void PlaceCrackedWall4OnPlatform(Vector2Int centerPosition)
    {
        if (crackedWall4Prefab == null)
        {
            return;
        }

        // Determine which wall the platform is attached to and place CrackedWall4 along it
        Vector2Int specialRoomCenter = new Vector2Int(
            specialRoom.x + specialRoom.width / 2,
            specialRoom.y + specialRoom.height / 2
        );
        
        // Calculate the direction from center to entrance
        Vector2Int directionToEntrance = specialRoomEntrance - specialRoomCenter;
        
        // The opposite direction is where the platform is located
        Vector2Int platformDirection = -directionToEntrance;
        
        // Place CrackedWall4 at the center of the platform wall
        if (Mathf.Abs(platformDirection.x) > Mathf.Abs(platformDirection.y))
        {
            // Platform is on left or right wall
            if (platformDirection.x > 0)
            {
                // Platform on right wall - place CrackedWall4 at the center of the right edge
                dungeonMap[centerPosition.x + 1, centerPosition.y] = TileType.CrackedWall4;
            }
            else
            {
                // Platform on left wall - place CrackedWall4 at the center of the left edge
                dungeonMap[centerPosition.x - 1, centerPosition.y] = TileType.CrackedWall4;
            }
        }
        else
        {
            // Platform is on top or bottom wall
            if (platformDirection.y > 0)
            {
                // Platform on top wall - place CrackedWall4 at the center of the top edge
                dungeonMap[centerPosition.x, centerPosition.y + 1] = TileType.CrackedWall4;
            }
            else
            {
                // Platform on bottom wall - place CrackedWall4 at the center of the bottom edge
                dungeonMap[centerPosition.x, centerPosition.y - 1] = TileType.CrackedWall4;
            }
        }
    }

    // Fill the entire special room with holes
    private void FillSpecialRoomWithHoles()
    {
        for (int x = specialRoom.x; x < specialRoom.x + specialRoom.width; x++)
        {
            for (int y = specialRoom.y; y < specialRoom.y + specialRoom.height; y++)
            {
                dungeonMap[x, y] = TileType.Hole;
            }
        }
    }
    
    // Create floor tile islands of various sizes in the special room
    private void CreateFloorIslandsInSpecialRoom()
    {
        // Define island sizes and their counts
        int[] islandSizes = { 3, 4, 5 };
        int[] islandCounts = { 4, 3, 2 }; // 4x 3x3 islands, 3x 4x4 islands, 2x 5x5 islands
        
        // Create islands for each size
        for (int i = 0; i < islandSizes.Length; i++)
        {
            int size = islandSizes[i];
            int count = islandCounts[i];
            
            for (int j = 0; j < count; j++)
            {
                CreateFloorIsland(size);
            }
        }
    }
    
    // Create a single floor island of specified size
    private void CreateFloorIsland(int size)
    {
        // Find a suitable position for the island
        Vector2Int? islandPosition = FindIslandPosition(size);
        
        if (islandPosition.HasValue)
        {
            // Create the island
            for (int x = islandPosition.Value.x; x < islandPosition.Value.x + size; x++)
            {
                for (int y = islandPosition.Value.y; y < islandPosition.Value.y + size; y++)
                {
                    dungeonMap[x, y] = TileType.SpecialFloor;
                }
            }
        }
        else
        {
        }
    }
    
    // Find a suitable position for an island of specified size
    private Vector2Int? FindIslandPosition(int size)
    {
        List<Vector2Int> possiblePositions = new List<Vector2Int>();
        
        // Look for positions where the entire island would fit within the special room
        for (int x = specialRoom.x; x <= specialRoom.x + specialRoom.width - size; x++)
        {
            for (int y = specialRoom.y; y <= specialRoom.y + specialRoom.height - size; y++)
            {
                // Check if this position is suitable (not too close to existing floor tiles)
                if (IsValidIslandPosition(x, y, size))
                {
                    possiblePositions.Add(new Vector2Int(x, y));
                }
            }
        }
        
        // Return a random position from the possible positions
        if (possiblePositions.Count > 0)
        {
            return possiblePositions[Random.Range(0, possiblePositions.Count)];
        }
        
        return null;
    }
    
    // Check if a position is valid for placing an island
    private bool IsValidIslandPosition(int x, int y, int size)
    {
        // Check if the island area is currently all holes (no existing floor tiles)
        for (int checkX = x; checkX < x + size; checkX++)
        {
            for (int checkY = y; checkY < y + size; checkY++)
            {
                if (dungeonMap[checkX, checkY] != TileType.Hole)
                {
                    return false; // Position overlaps with existing floor tiles
                }
            }
        }
        
        // Check if the island is not too close to existing floor tiles (minimum 1 tile gap)
        // But allow islands near the platform since it's created first
        for (int checkX = x - 1; checkX <= x + size; checkX++)
        {
            for (int checkY = y - 1; checkY <= y + size; checkY++)
            {
                // Skip if outside the special room bounds
                if (checkX < specialRoom.x || checkX >= specialRoom.x + specialRoom.width ||
                    checkY < specialRoom.y || checkY >= specialRoom.y + specialRoom.height)
                {
                    continue;
                }
                
                // Skip the island area itself
                if (checkX >= x && checkX < x + size && checkY >= y && checkY < y + size)
                {
                    continue;
                }
                
                // If there's a floor tile adjacent, this position is too close
                // But allow islands near the platform (platform is 3x3, so check if we're near it)
                if (dungeonMap[checkX, checkY] == TileType.SpecialFloor)
                {
                    // Check if this is part of the platform area
                    bool isPlatformArea = false;
                    if (specialRoomPlatformCenter != Vector2Int.zero)
                    {
                        // Check if the adjacent floor tile is within the 3x3 platform area
                        if (checkX >= specialRoomPlatformCenter.x - 1 && checkX <= specialRoomPlatformCenter.x + 1 &&
                            checkY >= specialRoomPlatformCenter.y - 1 && checkY <= specialRoomPlatformCenter.y + 1)
                        {
                            isPlatformArea = true;
                        }
                    }
                    
                    // If it's not the platform area, then it's too close to another island
                    if (!isPlatformArea)
                    {
                        return false;
                    }
                }
            }
        }
        
        return true;
    }

    // Connect the special room to nearby rooms with one or more corridors based on dungeon size
    private void ConnectSpecialRoom()
    {
        if (specialRoom == null) 
        {
            return;
        }

        // Calculate dungeon size to determine connection strategy
        int totalCells = cellsX * cellsY;
        bool isSmallDungeon = totalCells <= 24; // 6x4 or smaller dungeon

        if (isSmallDungeon)
        {
            // For small dungeons, connect to multiple nearby rooms
            ConnectSpecialRoomMultiple(isSmallDungeon);
        }
        else
        {
            // For larger dungeons, use the original single connection logic
            ConnectSpecialRoomSingle();
        }
    }

    // Connect the special room to a single nearest room (original logic for larger dungeons)
    private void ConnectSpecialRoomSingle()
    {
        // Find the nearest room to connect to (excluding boss room)
        Room nearestRoom = FindNearestRoomForSpecialRoom();
        
        if (nearestRoom != null)
        {
            // Connect the special room to the nearest room with a single corridor
            ConnectRoomsWithCorridor(specialRoom, nearestRoom);
            
            // Find and store the entrance point where the corridor connects to the special room
            FindSpecialRoomEntrance(nearestRoom);
            
            // Add a platform on the opposite side from the entrance
            AddPlatformToSpecialRoom();
        }
        else
        {
        }
    }

    // Connect the special room to multiple nearby rooms (for small dungeons)
    private void ConnectSpecialRoomMultiple(bool isSmallDungeon)
    {
        // Find all nearby rooms within a reasonable distance
        List<Room> nearbyRooms = FindNearbyRoomsForSpecialRoom(isSmallDungeon);
        
        if (nearbyRooms.Count > 0)
        {
            // Connect to each nearby room
            foreach (Room nearbyRoom in nearbyRooms)
            {
                ConnectRoomsWithCorridor(specialRoom, nearbyRoom);
            }
            
            // For multiple entrances, we'll use the first connection as the main entrance
            if (nearbyRooms.Count > 0)
            {
                FindSpecialRoomEntrance(nearbyRooms[0]);
                AddPlatformToSpecialRoom();
            }
        }
        else
        {
        }
    }

    // Find multiple nearby rooms for special room connection (for small dungeons)
    private List<Room> FindNearbyRoomsForSpecialRoom(bool isSmallDungeon)
    {
        List<Room> nearbyRooms = new List<Room>();
        
        foreach (Room room in rooms)
        {
            // Skip the special room itself and the boss room
            if (room == specialRoom || (enableBossRoom && room == bossRoom))
            {
                continue;
            }

            float distance = Vector2.Distance(
                new Vector2(specialRoom.x + specialRoom.width / 2, specialRoom.y + specialRoom.height / 2),
                new Vector2(room.x + room.width / 2, room.y + room.height / 2)
            );

            // Add all rooms regardless of distance
            nearbyRooms.Add(room);
        }

        // Sort by distance to prioritize closer rooms
        nearbyRooms.Sort((a, b) => {
            float distanceA = Vector2.Distance(
                new Vector2(specialRoom.x + specialRoom.width / 2, specialRoom.y + specialRoom.height / 2),
                new Vector2(a.x + a.width / 2, a.y + a.height / 2)
            );
            float distanceB = Vector2.Distance(
                new Vector2(specialRoom.x + specialRoom.width / 2, specialRoom.y + specialRoom.height / 2),
                new Vector2(b.x + b.width / 2, b.y + b.height / 2)
            );
            return distanceA.CompareTo(distanceB);
        });

        // Limit the number of connections based on dungeon size
        int maxConnections = isSmallDungeon ? 3 : 2; // More connections for smaller dungeons
        if (nearbyRooms.Count > maxConnections)
        {
            nearbyRooms = nearbyRooms.GetRange(0, maxConnections);
        }

        return nearbyRooms;
    }

    // Find the nearest room for the special room to connect to
    private Room FindNearestRoomForSpecialRoom()
    {
        Room nearest = null;
        float minDistance = float.MaxValue;

        foreach (Room room in rooms)
        {
            // Skip the special room itself and the boss room
            if (room == specialRoom || (enableBossRoom && room == bossRoom))
            {
                continue;
            }

            float distance = Vector2.Distance(
                new Vector2(specialRoom.x + specialRoom.width / 2, specialRoom.y + specialRoom.height / 2),
                new Vector2(room.x + room.width / 2, room.y + room.height / 2)
            );

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = room;
            }
        }

        return nearest;
    }

    // Find a suitable location for the special room (avoiding boss room area)
    private Vector2Int? FindSpecialRoomLocation()
    {
        List<Vector2Int> possibleLocations = new List<Vector2Int>();

        // Try to place the special room in different areas of the dungeon
        // We'll try the corners first, then other areas
        Vector2Int[] cornerOffsets = {
            new Vector2Int(0, 0),                    // Bottom-left corner
            new Vector2Int(cellsX - 2, 0),           // Bottom-right corner (2 cells wide)
            new Vector2Int(0, cellsY - 1),           // Top-left corner
            new Vector2Int(cellsX - 2, cellsY - 1)   // Top-right corner (2 cells wide)
        };

        foreach (Vector2Int offset in cornerOffsets)
        {
            Vector2Int location = new Vector2Int(
                bufferX + offset.x * cellWidth,
                bufferY + offset.y * cellHeight
            );

            // Check if this location is valid (within bounds and not overlapping boss room)
            if (IsValidSpecialRoomLocation(location))
            {
                possibleLocations.Add(location);
            }
        }

        // If no corner locations work, try other areas
        if (possibleLocations.Count == 0)
        {
            for (int x = 0; x <= cellsX - 2; x++) // -2 because room is 2 cells wide
            {
                for (int y = 0; y < cellsY; y++)
                {
                    Vector2Int location = new Vector2Int(
                        bufferX + x * cellWidth,
                        bufferY + y * cellHeight
                    );

                    if (IsValidSpecialRoomLocation(location))
                    {
                        possibleLocations.Add(location);
                    }
                }
            }
        }

        // Return a random valid location
        if (possibleLocations.Count > 0)
        {
            return possibleLocations[Random.Range(0, possibleLocations.Count)];
        }

        return null;
    }

    // Check if a location is valid for the special room
    private bool IsValidSpecialRoomLocation(Vector2Int location)
    {
        // Check if the room would fit within dungeon bounds
        if (location.x + cellWidth * 2 > dungeonWidth - bufferX || 
            location.y + cellHeight > dungeonHeight - bufferY)
        {
            return false;
        }

        // Check if the location overlaps with the boss room area (1.5x1.5 cells in center)
        if (enableBossRoom)
        {
            // Calculate the boss room area (1.5x1.5 cells in center)
            int centerCellX = cellsX / 2 - 1;
            int centerCellY = cellsY / 2 - 1;
            
            // Calculate boss room bounds in tile coordinates
            int bossRoomStartX = bufferX + centerCellX * cellWidth + cellWidth / 4;
            int bossRoomStartY = bufferY + centerCellY * cellHeight + cellHeight / 4;
            int bossRoomWidth = (int)(cellWidth * 1.5f);
            int bossRoomHeight = (int)(cellHeight * 1.5f);

            // Check for overlap with special room
            if (location.x < bossRoomStartX + bossRoomWidth &&
                location.x + cellWidth * 2 > bossRoomStartX &&
                location.y < bossRoomStartY + bossRoomHeight &&
                location.y + cellHeight > bossRoomStartY)
            {
                return false; // Overlap detected
            }
        }

        return true;
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
        }
        else
        {
        }
    }

    // Place CrackedWall1 randomly in the dungeon (not in boss room or special room)
    private void PlaceCrackedWall1InDungeon()
    {
        if (crackedWallPrefab == null)
        {
            return;
        }

        int wallsToPlace = 1; // Place 1 CrackedWall1 instance
        int wallsPlaced = 0;
        int attempts = 0;
        int maxAttempts = 100;
        Vector2Int? crackedWall1Position = null;

        while (wallsPlaced < wallsToPlace && attempts < maxAttempts)
        {
            attempts++;
            
            // Find a random wall position in the dungeon (not in boss room or special room)
            Vector2Int? wallPosition = FindRandomWallPositionForCrackedWall1();
            
            if (wallPosition.HasValue)
            {
                dungeonMap[wallPosition.Value.x, wallPosition.Value.y] = TileType.CrackedWall;
                crackedWall1Position = wallPosition.Value; // Store the position for hint placement
                wallsPlaced++;
            }
            else
            {
            }
        }

        if (attempts >= maxAttempts)
        {
            // Try fallback placement with smaller room size requirement
            if (wallsPlaced == 0)
            {
                Vector2Int? fallbackPosition = FindRandomWallPositionForCrackedWall1Fallback();
                if (fallbackPosition.HasValue)
                {
                    dungeonMap[fallbackPosition.Value.x, fallbackPosition.Value.y] = TileType.CrackedWall;
                    crackedWall1Position = fallbackPosition.Value;
                    wallsPlaced++;
                }
                else
                {
                }
            }
        }
        else
        {
        }

        // Place puzzle solution hint near CrackedWall1 if we have a position and prefab
        if (crackedWall1Position.HasValue && puzzleSolutionHint1Prefab != null)
        {
            PlacePuzzleSolutionHint1(crackedWall1Position.Value);
        }
        else if (crackedWall1Position.HasValue && puzzleSolutionHint1Prefab == null)
        {
        }
    }

    // Place puzzle solution hint near CrackedWall1
    private void PlacePuzzleSolutionHint1(Vector2Int crackedWall1Position)
    {
        // Try to find a floor position near the cracked wall for the hint
        Vector2Int? hintPosition = FindFloorPositionNearCrackedWall1(crackedWall1Position);
        
        if (hintPosition.HasValue)
        {
            dungeonMap[hintPosition.Value.x, hintPosition.Value.y] = TileType.PuzzleSolutionHint1;
        }
        else
        {
        }
    }

    // Find a floor position near CrackedWall1 for placing the hint
    private Vector2Int? FindFloorPositionNearCrackedWall1(Vector2Int crackedWall1Position)
    {
        // First try to find a position in the closest room to the cracked wall
        Vector2Int? closestRoomPosition = FindFloorPositionInClosestRoom(crackedWall1Position);
        if (closestRoomPosition.HasValue)
        {
            return closestRoomPosition.Value;
        }
        
        // Fallback: Search in a 5x5 area around the cracked wall for a floor tile
        int searchRadius = 2;
        
        for (int x = crackedWall1Position.x - searchRadius; x <= crackedWall1Position.x + searchRadius; x++)
        {
            for (int y = crackedWall1Position.y - searchRadius; y <= crackedWall1Position.y + searchRadius; y++)
            {
                // Check bounds
                if (x < 0 || x >= dungeonWidth || y < 0 || y >= dungeonHeight)
                    continue;
                
                // Check if this is a floor tile and not in boss room or special room
                if (dungeonMap[x, y] == TileType.Floor && !IsPositionInBossRoom(x, y) && !IsPositionInSpecialRoom(x, y))
                {
                    // Check if this position is not too close to the cracked wall (to avoid overlap)
                    float distance = Vector2Int.Distance(new Vector2Int(x, y), crackedWall1Position);
                    if (distance >= 1.5f) // At least 1.5 tiles away
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }
        }
        
        return null;
    }

    // Place puzzle solution hint near CrackedWall2
    private void PlacePuzzleSolutionHint2(Vector2Int crackedWall2Position)
    {
        // Try to find a floor position near the cracked wall for the hint
        Vector2Int? hintPosition = FindFloorPositionNearCrackedWall2(crackedWall2Position);
        
        if (hintPosition.HasValue)
        {
            dungeonMap[hintPosition.Value.x, hintPosition.Value.y] = TileType.PuzzleSolutionHint2;
        }
        else
        {
        }
    }

    // Find a floor position near CrackedWall2 for placing the hint
    private Vector2Int? FindFloorPositionNearCrackedWall2(Vector2Int crackedWall2Position)
    {
        // First try to find a position in the closest room to the cracked wall
        Vector2Int? closestRoomPosition = FindFloorPositionInClosestRoom(crackedWall2Position);
        if (closestRoomPosition.HasValue)
        {
            return closestRoomPosition.Value;
        }
        
        // Fallback: Search in a 5x5 area around the cracked wall for a floor tile
        int searchRadius = 2;
        
        for (int x = crackedWall2Position.x - searchRadius; x <= crackedWall2Position.x + searchRadius; x++)
        {
            for (int y = crackedWall2Position.y - searchRadius; y <= crackedWall2Position.y + searchRadius; y++)
            {
                // Check bounds
                if (x < 0 || x >= dungeonWidth || y < 0 || y >= dungeonHeight)
                    continue;
                
                // Check if this is a floor tile and not in boss room or special room
                if (dungeonMap[x, y] == TileType.Floor && !IsPositionInBossRoom(x, y) && !IsPositionInSpecialRoom(x, y))
                {
                    // Check if this position is not too close to the cracked wall (to avoid overlap)
                    float distance = Vector2Int.Distance(new Vector2Int(x, y), crackedWall2Position);
                    if (distance >= 1.5f) // At least 1.5 tiles away
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }
        }
        
        return null;
    }

    // Find a floor position in the closest room to a given position
    private Vector2Int? FindFloorPositionInClosestRoom(Vector2Int targetPosition)
    {
        if (rooms == null || rooms.Count == 0)
        {
            return null;
        }

        Room closestRoom = null;
        float closestDistance = float.MaxValue;

        // Find the closest room to the target position (3x3 or bigger, not boss/special room)
        foreach (Room room in rooms)
        {
            if (room == null) continue;

            // Only consider rooms that are 3x3 or bigger
            if (room.width < 3 || room.height < 3)
            {
                continue;
            }

            // Check if this room is the boss room or special room
            bool isBossRoom = false;
            bool isSpecialRoom = false;
            
            // Check a few sample points in the room to determine if it's boss/special room
            Vector2Int roomCenter = new Vector2Int(room.x + room.width / 2, room.y + room.height / 2);
            if (IsInBossRoomArea(roomCenter.x / cellWidth, roomCenter.y / cellHeight))
            {
                isBossRoom = true;
                continue;
            }
            
            if (IsPositionInSpecialRoom(roomCenter.x, roomCenter.y))
            {
                isSpecialRoom = true;
                continue;
            }
            
            // Calculate distance from target position to room center
            float distance = Vector2Int.Distance(targetPosition, roomCenter);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestRoom = room;
            }
        }

        if (closestRoom == null)
        {
            return null;
        }

        // Find a suitable floor position within the closest room
        // Start from the center and work outward
        Vector2Int searchCenter = new Vector2Int(closestRoom.x + closestRoom.width / 2, closestRoom.y + closestRoom.height / 2);
        
        // Search in expanding circles from the room center
        int maxSearchRadius = Mathf.Max(closestRoom.width, closestRoom.height) / 2;
        
        for (int radius = 0; radius <= maxSearchRadius; radius++)
        {
            for (int x = searchCenter.x - radius; x <= searchCenter.x + radius; x++)
            {
                for (int y = searchCenter.y - radius; y <= searchCenter.y + radius; y++)
                {
                    // Only check positions at the current radius (perimeter of the circle)
                    if (Mathf.Abs(x - searchCenter.x) == radius || Mathf.Abs(y - searchCenter.y) == radius)
                    {
                        // Check bounds
                        if (x < 0 || x >= dungeonWidth || y < 0 || y >= dungeonHeight)
                            continue;

                        // Check if this is within the room bounds
                        if (x < closestRoom.x || x >= closestRoom.x + closestRoom.width ||
                            y < closestRoom.y || y >= closestRoom.y + closestRoom.height)
                            continue;

                        // Check if this is a floor tile and not in boss room or special room
                        if (dungeonMap[x, y] == TileType.Floor && 
                            !IsPositionInBossRoom(x, y) && 
                            !IsPositionInSpecialRoom(x, y))
                        {
                            // Check if this position is not too close to the target position (to avoid overlap)
                            float distanceToTarget = Vector2Int.Distance(new Vector2Int(x, y), targetPosition);
                            if (distanceToTarget >= 1.5f) // At least 1.5 tiles away
                            {
                                return new Vector2Int(x, y);
                            }
                            else
                            {
                            }
                        }
                        else
                        {
                            if (radius == 0) // Only log for first radius to avoid spam
                            {
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    // Place CrackedWall2 instances in the dungeon (not in boss room or special room)
    private void PlaceCrackedWall2InDungeon()
    {
        if (crackedWall2Prefab == null)
        {
            return;
        }

        int wallsToPlace = 1; // Place 1 CrackedWall2 instance
        int wallsPlaced = 0;
        int attempts = 0;
        int maxAttempts = 100;
        Vector2Int? crackedWall2Position = null;

        while (wallsPlaced < wallsToPlace && attempts < maxAttempts)
        {
            attempts++;
            
            // Find a random wall position in the dungeon (not in boss room or special room)
            Vector2Int? wallPosition = FindRandomWallPositionForCrackedWall2();
            
            if (wallPosition.HasValue)
            {
                dungeonMap[wallPosition.Value.x, wallPosition.Value.y] = TileType.CrackedWall2;
                crackedWall2Position = wallPosition.Value; // Store the position for hint placement
                wallsPlaced++;
            }
            else
            {
            }
        }

        if (attempts >= maxAttempts)
        {
            // Try fallback placement with smaller room size requirement
            if (wallsPlaced == 0)
            {
                Vector2Int? fallbackPosition = FindRandomWallPositionForCrackedWall2Fallback();
                if (fallbackPosition.HasValue)
                {
                    dungeonMap[fallbackPosition.Value.x, fallbackPosition.Value.y] = TileType.CrackedWall2;
                    crackedWall2Position = fallbackPosition.Value;
                    wallsPlaced++;
                }
                else
                {
                }
            }
        }
        else
        {
        }

        // Place puzzle solution hint near CrackedWall2 if we have a position and prefab
        if (crackedWall2Position.HasValue && puzzleSolutionHint2Prefab != null)
        {
            PlacePuzzleSolutionHint2(crackedWall2Position.Value);
        }
        else if (crackedWall2Position.HasValue && puzzleSolutionHint2Prefab == null)
        {
        }
    }

    // Find a random wall position suitable for CrackedWall1 (not in boss room or special room, in rooms 4x4 or larger)
    private Vector2Int? FindRandomWallPositionForCrackedWall1()
    {
        List<Vector2Int> possiblePositions = new List<Vector2Int>();
        int totalWallTiles = 0;
        int notInBossRoom = 0;
        int notInSpecialRoom = 0;
        int notAlreadyCrackedWall = 0;
        int hasNearbyFloorTiles = 0;
        int inRoomOfSize = 0;

        // Determine minimum room size based on dungeon size
        int minRoomSize = DetermineMinimumRoomSizeForCrackedWall();

        // Scan the entire dungeon for wall positions
        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                // Check if this is a wall tile
                if (IsWallTile(dungeonMap[x, y]))
                {
                    totalWallTiles++;
                    
                    // Make sure it's not in the boss room or boss room area
                    if (!IsPositionInBossRoom(x, y) && !IsPositionInBossRoomArea(x, y))
                    {
                        notInBossRoom++;
                        
                        // Make sure it's not in the special room
                        if (!IsPositionInSpecialRoom(x, y))
                        {
                            notInSpecialRoom++;
                            
                            // Make sure it's not already a CrackedWall, CrackedWall2, CrackedWall3, or CrackedWall4
                            if (dungeonMap[x, y] != TileType.CrackedWall && 
                                dungeonMap[x, y] != TileType.CrackedWall2 && 
                                dungeonMap[x, y] != TileType.CrackedWall3 && 
                                dungeonMap[x, y] != TileType.CrackedWall4)
                            {
                                notAlreadyCrackedWall++;
                                
                                // Check if it's accessible (has floor tiles nearby)
                                if (HasNearbyFloorTiles(x, y))
                                {
                                    hasNearbyFloorTiles++;
                                    
                                    // Check if the position is in a room that meets the minimum size requirement
                                    if (IsPositionInRoomOfSize(x, y, minRoomSize, minRoomSize))
                                    {
                                        inRoomOfSize++;
                                        possiblePositions.Add(new Vector2Int(x, y));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Return a random position from the possible positions
        if (possiblePositions.Count > 0)
        {
            Vector2Int selectedPosition = possiblePositions[Random.Range(0, possiblePositions.Count)];
            return selectedPosition;
        }

        return null;
    }

    // Find a random wall position suitable for CrackedWall2 (not in boss room or special room, in rooms 4x4 or larger)
    private Vector2Int? FindRandomWallPositionForCrackedWall2()
    {
        List<Vector2Int> possiblePositions = new List<Vector2Int>();
        int totalWallTiles = 0;
        int notInBossRoom = 0;
        int notInSpecialRoom = 0;
        int notAlreadyCrackedWall = 0;
        int hasNearbyFloorTiles = 0;
        int inRoomOfSize = 0;

        // Determine minimum room size based on dungeon size
        int minRoomSize = DetermineMinimumRoomSizeForCrackedWall();

        // Scan the entire dungeon for wall positions
        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                // Check if this is a wall tile
                if (IsWallTile(dungeonMap[x, y]))
                {
                    totalWallTiles++;
                    
                    // Make sure it's not in the boss room or boss room area
                    if (!IsPositionInBossRoom(x, y) && !IsPositionInBossRoomArea(x, y))
                    {
                        notInBossRoom++;
                        
                        // Make sure it's not in the special room
                        if (!IsPositionInSpecialRoom(x, y))
                        {
                            notInSpecialRoom++;
                            
                            // Make sure it's not already a CrackedWall or CrackedWall2
                            if (dungeonMap[x, y] != TileType.CrackedWall && dungeonMap[x, y] != TileType.CrackedWall2)
                            {
                                notAlreadyCrackedWall++;
                                
                                // Check if it's accessible (has floor tiles nearby)
                                if (HasNearbyFloorTiles(x, y))
                                {
                                    hasNearbyFloorTiles++;
                                    
                                    // Check if the position is in a room that meets the minimum size requirement
                                    if (IsPositionInRoomOfSize(x, y, minRoomSize, minRoomSize))
                                    {
                                        inRoomOfSize++;
                                        possiblePositions.Add(new Vector2Int(x, y));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Return a random position from the possible positions
        if (possiblePositions.Count > 0)
        {
            Vector2Int selectedPosition = possiblePositions[Random.Range(0, possiblePositions.Count)];
            return selectedPosition;
        }

        return null;
    }

    // Fallback method for CrackedWall1 placement with minimal requirements
    private Vector2Int? FindRandomWallPositionForCrackedWall1Fallback()
    {
        List<Vector2Int> possiblePositions = new List<Vector2Int>();

        // Scan the entire dungeon for wall positions with minimal requirements
        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                // Check if this is a wall tile
                if (IsWallTile(dungeonMap[x, y]))
                {
                    // Make sure it's not in the boss room or boss room area
                    if (!IsPositionInBossRoom(x, y) && !IsPositionInBossRoomArea(x, y))
                    {
                        // Make sure it's not in the special room
                        if (!IsPositionInSpecialRoom(x, y))
                        {
                            // Make sure it's not already a CrackedWall, CrackedWall2, CrackedWall3, or CrackedWall4
                            if (dungeonMap[x, y] != TileType.CrackedWall && 
                                dungeonMap[x, y] != TileType.CrackedWall2 && 
                                dungeonMap[x, y] != TileType.CrackedWall3 && 
                                dungeonMap[x, y] != TileType.CrackedWall4)
                            {
                                // Check if it's accessible (has floor tiles nearby) - minimal requirement
                                if (HasNearbyFloorTiles(x, y))
                                {
                                    possiblePositions.Add(new Vector2Int(x, y));
                                }
                            }
                        }
                    }
                }
            }
        }

        // Return a random position from the possible positions
        if (possiblePositions.Count > 0)
        {
            Vector2Int selectedPosition = possiblePositions[Random.Range(0, possiblePositions.Count)];
            return selectedPosition;
        }

        return null;
    }

    // Fallback method for CrackedWall2 placement with minimal requirements
    private Vector2Int? FindRandomWallPositionForCrackedWall2Fallback()
    {
        List<Vector2Int> possiblePositions = new List<Vector2Int>();

        // Scan the entire dungeon for wall positions with minimal requirements
        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                // Check if this is a wall tile
                if (IsWallTile(dungeonMap[x, y]))
                {
                    // Make sure it's not in the boss room or boss room area
                    if (!IsPositionInBossRoom(x, y) && !IsPositionInBossRoomArea(x, y))
                    {
                        // Make sure it's not in the special room
                        if (!IsPositionInSpecialRoom(x, y))
                        {
                            // Make sure it's not already a CrackedWall or CrackedWall2
                            if (dungeonMap[x, y] != TileType.CrackedWall && dungeonMap[x, y] != TileType.CrackedWall2)
                            {
                                // Check if it's accessible (has floor tiles nearby) - minimal requirement
                                if (HasNearbyFloorTiles(x, y))
                                {
                                    possiblePositions.Add(new Vector2Int(x, y));
                                }
                            }
                        }
                    }
                }
            }
        }

        // Return a random position from the possible positions
        if (possiblePositions.Count > 0)
        {
            Vector2Int selectedPosition = possiblePositions[Random.Range(0, possiblePositions.Count)];
            return selectedPosition;
        }

        return null;
    }

    // Check if a tile type is a wall
    private bool IsWallTile(TileType tileType)
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

    // Check if a position is within a room of the specified minimum size
    private bool IsPositionInRoomOfSize(int x, int y, int minWidth, int minHeight)
    {
        foreach (Room room in rooms)
        {
            // Check if the position is within this room's bounds
            if (x >= room.x && x < room.x + room.width && y >= room.y && y < room.y + room.height)
            {
                // Check if the room meets the minimum size requirements
                if (room.width >= minWidth && room.height >= minHeight)
                {
                    return true;
                }
            }
        }
        return false;
    }

    // Determine the minimum room size requirement for CrackedWall placement based on dungeon size
    private int DetermineMinimumRoomSizeForCrackedWall()
    {
        // Calculate the total number of cells in the dungeon
        int totalCells = cellsX * cellsY;
        
        // For very small dungeons (less than 16 cells), use a smaller minimum room size
        if (totalCells < 16)
        {
            return 3;
        }
        // For small dungeons (16-25 cells), use a moderate minimum room size
        else if (totalCells < 25)
        {
            return 3;
        }
        // For medium dungeons (25-36 cells), use the standard minimum room size
        else if (totalCells < 36)
        {
            return 4;
        }
        // For large dungeons (36+ cells), use the standard minimum room size
        else
        {
            return 4;
        }
    }

    // Check if a position has floor tiles nearby (for accessibility)
    private bool HasNearbyFloorTiles(int x, int y)
    {
        // Check in a 3x3 area around the position
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int checkX = x + dx;
                int checkY = y + dy;
                
                // Make sure we're within bounds
                if (checkX >= 0 && checkX < dungeonWidth && checkY >= 0 && checkY < dungeonHeight)
                {
                    TileType tileType = dungeonMap[checkX, checkY];
                    if (tileType == TileType.Floor || tileType == TileType.BossFloor || tileType == TileType.NonEssentialFloor || tileType == TileType.SpecialFloor)
                    {
                        return true; // Found a nearby floor tile
                    }
                }
            }
        }
        
        return false; // No nearby floor tiles
    }

    // Place CrackedWall3 and CarryableObject in different rooms (not in boss room or special room)
    private void PlaceCrackedWall3AndCarryableObject()
    {
        if (crackedWall3Prefab == null)
        {
            return;
        }

        if (carryableObjectPrefab == null)
        {
            return;
        }

        // Find a suitable room for CrackedWall3 placement (not boss room or special room)
        Room crackWallRoom = FindSuitableRoomForCrackedWall3();
        
        if (crackWallRoom == null)
        {
            return;
        }

        // Place CrackedWall3 on a wall of the room
        Vector2Int? crackedWallPosition = FindWallPositionInRoom(crackWallRoom);
        if (crackedWallPosition.HasValue)
        {
            dungeonMap[crackedWallPosition.Value.x, crackedWallPosition.Value.y] = TileType.CrackedWall3;
        }
        else
        {
            return;
        }

        // Find a different room for CarryableObject placement (one room away)
        Room carryableRoom = FindRoomForCarryableObject(crackWallRoom);
        
        Vector2Int carryablePosition;
        bool placedInRoom = false;
        
        if (carryableRoom != null)
        {
            // Place CarryableObject in the center of the different room
            carryablePosition = new Vector2Int(
                carryableRoom.x + carryableRoom.width / 2,
                carryableRoom.y + carryableRoom.height / 2
            );
            
            // Make sure the position is within the room bounds
            carryablePosition.x = Mathf.Clamp(carryablePosition.x, carryableRoom.x + 1, carryableRoom.x + carryableRoom.width - 2);
            carryablePosition.y = Mathf.Clamp(carryablePosition.y, carryableRoom.y + 1, carryableRoom.y + carryableRoom.height - 2);
            
            placedInRoom = true;
        }
        else
        {
            // No suitable room found, place in corridor near CrackedWall3
            Vector2Int? corridorPosition = FindCorridorPositionNearCrackedWall3(crackedWallPosition.Value);
            
            if (corridorPosition.HasValue)
            {
                carryablePosition = corridorPosition.Value;
                placedInRoom = false;
            }
            else
            {
                return;
            }
        }
        
        dungeonMap[carryablePosition.x, carryablePosition.y] = TileType.CarryableObject;
    }

    // Find a suitable room for CrackedWall3 placement (not boss room or special room, 4x4 or larger)
    private Room FindSuitableRoomForCrackedWall3()
    {
        List<Room> suitableRooms = new List<Room>();

        foreach (Room room in rooms)
        {
            // Skip boss room
            if (enableBossRoom && room == bossRoom)
                continue;

            // Skip special room
            if (enableSpecialRoom && room == specialRoom)
                continue;

            // Check if room is large enough (at least 4x4)
            if (room.width >= 4 && room.height >= 4)
            {
                suitableRooms.Add(room);
            }
        }

        // Return a random suitable room
        if (suitableRooms.Count > 0)
        {
            return suitableRooms[Random.Range(0, suitableRooms.Count)];
        }

        return null;
    }

    // Find a room for CarryableObject placement (one room away from CrackedWall3 room)
    private Room FindRoomForCarryableObject(Room crackWallRoom)
    {
        List<Room> suitableRooms = new List<Room>();

        foreach (Room room in rooms)
        {
            // Skip boss room, special room, and the CrackedWall3 room
            if (enableBossRoom && room == bossRoom)
                continue;
            
            if (enableSpecialRoom && room == specialRoom)
                continue;
            
            if (room == crackWallRoom)
                continue;

            // Check if room is large enough (at least 5x5)
            if (room.width >= 5 && room.height >= 5)
            {
                // Calculate distance between room centers
                Vector2 crackWallCenter = new Vector2(
                    crackWallRoom.x + crackWallRoom.width / 2f,
                    crackWallRoom.y + crackWallRoom.height / 2f
                );
                
                Vector2 roomCenter = new Vector2(
                    room.x + room.width / 2f,
                    room.y + room.height / 2f
                );
                
                float distance = Vector2.Distance(crackWallCenter, roomCenter);
                
                // Prefer rooms that are reasonably close but not too close
                // Use actual tile distance instead of cell-based distance
                float minDistance = 8f;  // At least 8 tiles away to be in different areas
                float maxDistance = 20f; // No more than 20 tiles away to keep them close
                
                if (distance >= minDistance && distance <= maxDistance)
                {
                    suitableRooms.Add(room);
                }
            }
        }

        // Return a random suitable room
        if (suitableRooms.Count > 0)
        {
            return suitableRooms[Random.Range(0, suitableRooms.Count)];
        }

        // If no rooms are in the ideal range, pick the closest suitable room that's not the CrackedWall3 room
        Room closestRoom = null;
        float closestDistance = float.MaxValue;
        
        foreach (Room room in rooms)
        {
            if (enableBossRoom && room == bossRoom)
                continue;
            
            if (enableSpecialRoom && room == specialRoom)
                continue;
            
            if (room == crackWallRoom)
                continue;

            if (room.width >= 5 && room.height >= 5)
            {
                // Calculate distance between room centers
                Vector2 crackWallCenter = new Vector2(
                    crackWallRoom.x + crackWallRoom.width / 2f,
                    crackWallRoom.y + crackWallRoom.height / 2f
                );
                
                Vector2 roomCenter = new Vector2(
                    room.x + room.width / 2f,
                    room.y + room.height / 2f
                );
                
                float distance = Vector2.Distance(crackWallCenter, roomCenter);
                
                // Prefer rooms that are not too far (max 30 tiles away)
                if (distance <= 30f && distance < closestDistance)
                {
                    closestRoom = room;
                    closestDistance = distance;
                }
            }
        }

        if (closestRoom != null)
        {
            return closestRoom;
        }

        return null;
    }

    // Find a wall position within a specific room
    private Vector2Int? FindWallPositionInRoom(Room room)
    {
        List<Vector2Int> possiblePositions = new List<Vector2Int>();

        // Check top wall (excluding corners)
        for (int x = room.x + 1; x < room.x + room.width - 1; x++)
        {
            if (dungeonMap[x, room.y + room.height] == TileType.WallTop)
            {
                possiblePositions.Add(new Vector2Int(x, room.y + room.height));
            }
        }

        // Check bottom wall (excluding corners)
        for (int x = room.x + 1; x < room.x + room.width - 1; x++)
        {
            if (dungeonMap[x, room.y - 1] == TileType.WallBottom)
            {
                possiblePositions.Add(new Vector2Int(x, room.y - 1));
            }
        }

        // Check left wall (excluding corners)
        for (int y = room.y + 1; y < room.y + room.height - 1; y++)
        {
            if (dungeonMap[room.x - 1, y] == TileType.WallLeft)
            {
                possiblePositions.Add(new Vector2Int(room.x - 1, y));
            }
        }

        // Check right wall (excluding corners)
        for (int y = room.y + 1; y < room.y + room.height - 1; y++)
        {
            if (dungeonMap[room.x + room.width, y] == TileType.WallRight)
            {
                possiblePositions.Add(new Vector2Int(room.x + room.width, y));
            }
        }

        // Return a random position from the possible positions
        if (possiblePositions.Count > 0)
        {
            return possiblePositions[Random.Range(0, possiblePositions.Count)];
        }

        return null;
    }

    // Find a corridor position near the CrackedWall3 for CarryableObject placement
    private Vector2Int? FindCorridorPositionNearCrackedWall3(Vector2Int crackedWallPosition)
    {
        List<Vector2Int> possiblePositions = new List<Vector2Int>();
        
        // Search in a radius around the CrackedWall3 position
        int searchRadius = 10; // Search within 10 tiles
        
        for (int x = crackedWallPosition.x - searchRadius; x <= crackedWallPosition.x + searchRadius; x++)
        {
            for (int y = crackedWallPosition.y - searchRadius; y <= crackedWallPosition.y + searchRadius; y++)
            {
                // Check bounds
                if (x < 0 || x >= dungeonWidth || y < 0 || y >= dungeonHeight)
                    continue;
                
                // Calculate distance from CrackedWall3
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(crackedWallPosition.x, crackedWallPosition.y));
                if (distance > searchRadius)
                    continue;
                
                // Check if this is a corridor tile (Floor or NonEssentialFloor)
                if (dungeonMap[x, y] == TileType.Floor || dungeonMap[x, y] == TileType.NonEssentialFloor)
                {
                    // Make sure it's not too close to the CrackedWall3 (at least 3 tiles away)
                    if (distance >= 3)
                    {
                        // Check if the position is accessible (not blocked by walls)
                        bool isAccessible = true;
                        
                        // Check if there's a clear path (at least one adjacent tile is also a floor)
                        bool hasAdjacentFloor = false;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                
                                int checkX = x + dx;
                                int checkY = y + dy;
                                
                                if (checkX >= 0 && checkX < dungeonWidth && checkY >= 0 && checkY < dungeonHeight)
                                {
                                    if (dungeonMap[checkX, checkY] == TileType.Floor || 
                                        dungeonMap[checkX, checkY] == TileType.NonEssentialFloor)
                                    {
                                        hasAdjacentFloor = true;
                                        break;
                                    }
                                }
                            }
                            if (hasAdjacentFloor) break;
                        }
                        
                        if (hasAdjacentFloor)
                        {
                            possiblePositions.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }
        }
        
        // Return a random position from the possible positions
        if (possiblePositions.Count > 0)
        {
            return possiblePositions[Random.Range(0, possiblePositions.Count)];
        }
        
        return null;
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

    // Check if a cell is in the special room area (2x1 cells)
    private bool IsInSpecialRoomArea(int cellX, int cellY)
    {
        if (!enableSpecialRoom || specialRoom == null) return false;

        // Calculate which cell this position is in
        int roomCellX = (specialRoom.x - bufferX) / cellWidth;
        int roomCellY = (specialRoom.y - bufferY) / cellHeight;

        // Check if the cell is within the 2x1 special room area
        return cellX >= roomCellX && cellX < roomCellX + 2 &&
               cellY >= roomCellY && cellY < roomCellY + 1;
    }

    // Check if a specific tile position is within the boss room
    public bool IsPositionInBossRoom(int x, int y)
    {
        if (bossRoom == null) return false;
        
        return x >= bossRoom.x && x < bossRoom.x + bossRoom.width &&
               y >= bossRoom.y && y < bossRoom.y + bossRoom.height;
    }

    // Check if a specific tile position is within the boss room area (including surrounding area)
    private bool IsPositionInBossRoomArea(int x, int y)
    {
        if (!enableBossRoom || bossRoom == null) return false;
        
        // Convert tile position to cell position
        int cellX = (x - bufferX) / cellWidth;
        int cellY = (y - bufferY) / cellHeight;
        
        // Check if the cell is in the boss room area
        return IsInBossRoomArea(cellX, cellY);
    }

    // Check if a specific tile position is within the special room
    private bool IsPositionInSpecialRoom(int x, int y)
    {
        if (specialRoom == null) return false;
        
        return x >= specialRoom.x && x < specialRoom.x + specialRoom.width &&
               y >= specialRoom.y && y < specialRoom.y + specialRoom.height;
    }

    /// <summary>
    /// Checks if the boss room is locked (all puzzles not solved)
    /// </summary>
    /// <returns>True if boss room is locked, false if unlocked or feature disabled</returns>
    public bool IsBossRoomLocked()
    {
        if (!enablePuzzleBossRoomLock || puzzleManager == null)
            return false;

        bool allPuzzlesSolved = puzzleManager.AreAllPuzzlesSolved();
        return !allPuzzlesSolved;
    }

    /// <summary>
    /// Checks if boss room is locked during generation phase (before instantiation)
    /// During generation, we assume boss room is locked to prevent corridor generation through it
    /// </summary>
    /// <returns>True if boss room should be considered locked during generation</returns>
    private bool IsBossRoomLockedDuringGeneration()
    {
        if (!enablePuzzleBossRoomLock || !enableBossRoom)
            return false;

        // During generation phase, assume boss room is locked to prevent corridor generation through it
        return true;
    }

    /// <summary>
    /// Checks if a position is near the boss room (within 2 tiles)
    /// </summary>
    /// <param name="x">X coordinate to check</param>
    /// <param name="y">Y coordinate to check</param>
    /// <returns>True if position is near boss room, false otherwise</returns>
    private bool IsNearBossRoom(int x, int y)
    {
        if (!enableBossRoom || bossRoom == null)
            return false;

        // Check if position is within 2 tiles of the boss room
        int minX = bossRoom.x - 2;
        int maxX = bossRoom.x + bossRoom.width + 2;
        int minY = bossRoom.y - 2;
        int maxY = bossRoom.y + bossRoom.height + 2;

        return x >= minX && x <= maxX && y >= minY && y <= maxY;
    }

    /// <summary>
    /// Checks if the boss room should be unlocked and creates corridors if needed
    /// </summary>
    private void CheckBossRoomUnlock()
    {
        bool currentlyLocked = IsBossRoomLocked();
        
        // If boss room was locked but is now unlocked, create corridors to it
        if (bossRoomWasLocked && !currentlyLocked)
        {
            CreateBossRoomCorridors();
            bossRoomWasLocked = false;
        }
        else if (!bossRoomWasLocked && currentlyLocked)
        {
            // If boss room was unlocked but is now locked again (shouldn't happen in normal gameplay)
            CreateBossRoomBarriers();
            bossRoomWasLocked = true;
        }
    }

    /// <summary>
    /// Creates corridors to connect the boss room to the rest of the dungeon
    /// </summary>
    private void CreateBossRoomCorridors()
    {
        if (!enableBossRoom || bossRoom == null)
            return;

        // Remove any existing barriers around the boss room
        RemoveBossRoomBarriers();

        // Find the nearest room to connect to the boss room
        Room nearestRoom = FindNearestRoomToBossRoom();
        
        if (nearestRoom != null)
        {
            // Connect the boss room to the nearest room
            ConnectRoomsWithCorridor(bossRoom, nearestRoom);
        }
        else
        {
        }
    }

    /// <summary>
    /// Creates physical barriers around the boss room to prevent access
    /// </summary>
    private void CreateBossRoomBarriers()
    {
        if (!enableBossRoom || bossRoom == null)
            return;

        if (bossRoomBarrierPrefab == null)
        {
            return;
        }

        // Validate barrier prefab has required components
        if (!ValidateBarrierPrefab())
        {
            return;
        }
        
        // Clear existing barriers
        RemoveBossRoomBarriers();
        
        // Create a complete barrier ring around the boss room (2 tiles thick)
        int barrierDistance = 2;
        
        // Top and bottom barriers
        for (int x = bossRoom.x - barrierDistance; x <= bossRoom.x + bossRoom.width + barrierDistance; x++)
        {
            // Top barrier
            if (x >= 0 && x < dungeonWidth && bossRoom.y + bossRoom.height + barrierDistance < dungeonHeight)
            {
                Vector2Int barrierPos = new Vector2Int(x, bossRoom.y + bossRoom.height + barrierDistance);
                CreateBarrierAtPosition(barrierPos);
            }
            
            // Bottom barrier
            if (x >= 0 && x < dungeonWidth && bossRoom.y - barrierDistance >= 0)
            {
                Vector2Int barrierPos = new Vector2Int(x, bossRoom.y - barrierDistance);
                CreateBarrierAtPosition(barrierPos);
            }
        }

        // Left and right barriers
        for (int y = bossRoom.y - barrierDistance; y <= bossRoom.y + bossRoom.height + barrierDistance; y++)
        {
            // Right barrier
            if (y >= 0 && y < dungeonHeight && bossRoom.x + bossRoom.width + barrierDistance < dungeonWidth)
            {
                Vector2Int barrierPos = new Vector2Int(bossRoom.x + bossRoom.width + barrierDistance, y);
                CreateBarrierAtPosition(barrierPos);
            }
            
            // Left barrier
            if (y >= 0 && y < dungeonHeight && bossRoom.x - barrierDistance >= 0)
            {
                Vector2Int barrierPos = new Vector2Int(bossRoom.x - barrierDistance, y);
                CreateBarrierAtPosition(barrierPos);
            }
        }
    }

    /// <summary>
    /// Creates a barrier at the specified position with appropriate Z depth
    /// </summary>
    private void CreateBarrierAtPosition(Vector2Int position)
    {
        // Determine Z position based on tile type
        float zPosition;
        TileType tileType = dungeonMap[position.x, position.y];
        
        if (tileType == TileType.Floor || tileType == TileType.NonEssentialFloor || 
            tileType == TileType.WallLeft || tileType == TileType.WallRight || 
            tileType == TileType.WallTop || tileType == TileType.WallBottom ||
            tileType == TileType.WallCornerTopLeft || tileType == TileType.WallCornerTopRight ||
            tileType == TileType.WallCornerBottomLeft || tileType == TileType.WallCornerBottomRight)
        {
            // Place barrier underneath existing tiles
            zPosition = -5f;
        }
        else
        {
            // Place barrier on top of void tiles
            zPosition = 1f;
        }
        
        // Create physical barrier object
        Vector3 barrierWorldPos = new Vector3(position.x, position.y, zPosition);
        GameObject barrierObj = Instantiate(bossRoomBarrierPrefab, barrierWorldPos, Quaternion.identity, dungeonParent);
        bossRoomBarrierObjects.Add(barrierObj);
        bossRoomBarrierTiles.Add(position);
        
    }

    /// <summary>
    /// Removes physical barriers around the boss room
    /// </summary>
    private void RemoveBossRoomBarriers()
    {
        if (bossRoomBarrierObjects.Count == 0)
            return;

        // Destroy all barrier GameObjects
        foreach (GameObject barrierObj in bossRoomBarrierObjects)
        {
            if (barrierObj != null)
            {
                Destroy(barrierObj);
            }
        }

        // Clear the lists
        bossRoomBarrierObjects.Clear();
        bossRoomBarrierTiles.Clear();
    }

    /// <summary>
    /// Validates that the barrier prefab has the required components for proper collision
    /// </summary>
    /// <returns>True if prefab is valid, false otherwise</returns>
    private bool ValidateBarrierPrefab()
    {
        if (bossRoomBarrierPrefab == null)
            return false;

        // Check for Collider2D component
        Collider2D collider = bossRoomBarrierPrefab.GetComponent<Collider2D>();
        if (collider == null)
        {
            return false;
        }

        // Check if collider is set up for solid collision
        if (collider.isTrigger)
        {
        }

        // Check for Rigidbody2D component
        Rigidbody2D rb = bossRoomBarrierPrefab.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            // Note: We can't add components to prefabs at runtime, but we can warn about it
        }
        else if (rb.bodyType != RigidbodyType2D.Kinematic)
        {
        }

        // Check for visual component
        SpriteRenderer spriteRenderer = bossRoomBarrierPrefab.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
        }

        return true;
    }

    /// <summary>
    /// Updates the visual representation of a tile at the specified position
    /// </summary>
    private void UpdateTileAtPosition(int x, int y)
    {
        // Find existing tile GameObject at this position and destroy it
        Vector3 tilePosition = new Vector3(x, y, 0);
        Collider2D[] colliders = Physics2D.OverlapPointAll(tilePosition);
        
        foreach (Collider2D collider in colliders)
        {
            if (collider.transform.parent == dungeonParent)
            {
                Destroy(collider.gameObject);
            }
        }

        // Create new tile based on current dungeon map
        Vector3 pos = new Vector3(x, y, 0);
        switch (dungeonMap[x, y])
        {
            case TileType.Floor:
                Instantiate(floorPrefab, pos, Quaternion.identity, dungeonParent);
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
            // Add other cases as needed
        }
    }

    /// <summary>
    /// Finds the nearest room to the boss room for connection
    /// </summary>
    /// <returns>The nearest room, or null if none found</returns>
    private Room FindNearestRoomToBossRoom()
    {
        Room nearest = null;
        float minDistance = float.MaxValue;

        foreach (Room room in rooms)
        {
            // Skip the boss room itself and special room
            if (room == bossRoom || (enableSpecialRoom && room == specialRoom))
                continue;

            float distance = Vector2.Distance(
                new Vector2(bossRoom.x + bossRoom.width / 2, bossRoom.y + bossRoom.height / 2),
                new Vector2(room.x + room.width / 2, room.y + room.height / 2)
            );

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = room;
            }
        }

        return nearest;
    }



    // Initialize the minimap
    void InitializeMinimap()
    {
        if (minimapContainer == null)
        {
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

        // Always reveal cracked wall tiles on the minimap
        RevealCrackedWallTiles(ref textureNeedsUpdate);

        // Clear the previous player position if it exists
        if (lastPlayerPosition.x >= 0 && lastPlayerPosition.y >= 0
            && lastPlayerPosition.x < dungeonWidth && lastPlayerPosition.y < dungeonHeight)
        {
            Color tileColor = GetTileColor(lastPlayerPosition.x, lastPlayerPosition.y);
            drawingTexture.SetPixel(lastPlayerPosition.x, lastPlayerPosition.y, tileColor);
            textureNeedsUpdate = true;
        }

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

    private void RevealCrackedWallTiles(ref bool textureNeedsUpdate)
    {
        // Always reveal cracked wall tiles on the minimap so players know where to go
        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                if (dungeonMap[x, y] == TileType.CrackedWall)
                {
                    // Mark as explored and draw with cracked wall color
                    exploredTiles[x, y] = true;
                    Color crackedWallColor = new Color(0.8f, 0.4f, 0.2f, 1f); // Orange-brown color for cracked wall
                    drawingTexture.SetPixel(x, y, crackedWallColor);
                    textureNeedsUpdate = true;
                }
                else if (dungeonMap[x, y] == TileType.CrackedWall2)
                {
                    // Mark as explored and draw with cracked wall 2 color
                    exploredTiles[x, y] = true;
                    Color crackedWall2Color = new Color(0.6f, 0.2f, 0.8f, 1f); // Purple color for cracked wall 2
                    drawingTexture.SetPixel(x, y, crackedWall2Color);
                    textureNeedsUpdate = true;
                }
                else if (dungeonMap[x, y] == TileType.CrackedWall3)
                {
                    // Mark as explored and draw with cracked wall 3 color
                    exploredTiles[x, y] = true;
                    Color crackedWall3Color = new Color(0.2f, 0.8f, 0.6f, 1f); // Green color for cracked wall 3
                    drawingTexture.SetPixel(x, y, crackedWall3Color);
                    textureNeedsUpdate = true;
                }
                else if (dungeonMap[x, y] == TileType.CrackedWall4)
                {
                    // Mark as explored and draw with cracked wall 4 color
                    exploredTiles[x, y] = true;
                    Color crackedWall4Color = new Color(0.8f, 0.6f, 0.2f, 1f); // Gold color for cracked wall 4
                    drawingTexture.SetPixel(x, y, crackedWall4Color);
                    textureNeedsUpdate = true;
                }
                else if (dungeonMap[x, y] == TileType.CarryableObject)
                {
                    // Mark as explored and draw with carryable object color
                    exploredTiles[x, y] = true;
                    Color carryableObjectColor = new Color(1f, 1f, 0f, 1f); // Yellow color for carryable object
                    drawingTexture.SetPixel(x, y, carryableObjectColor);
                    textureNeedsUpdate = true;
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
            case TileType.SpecialFloor:
                return specialRoomColor;
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
            case TileType.CrackedWall2:
                return new Color(0.6f, 0.2f, 0.8f, 1f); // Purple color for cracked wall 2
            case TileType.CrackedWall3:
                return new Color(0.2f, 0.8f, 0.6f, 1f); // Green color for cracked wall 3
            case TileType.CrackedWall4:
                return new Color(0.8f, 0.6f, 0.2f, 1f); // Gold color for cracked wall 4
            case TileType.CarryableObject:
                return new Color(1f, 1f, 0f, 1f); // Yellow color for carryable object
            case TileType.Hole:
                return holeColor; // Black color for holes
            default:
                return unexploredColor;
        }
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

                                    if (topY < dungeonHeight && (dungeonMap[x, topY] == TileType.Floor || dungeonMap[x, topY] == TileType.SpecialFloor))
                    connectionCount[room]++;

                if (bottomY >= 0 && (dungeonMap[x, bottomY] == TileType.Floor || dungeonMap[x, bottomY] == TileType.SpecialFloor))
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

                                    if (leftX >= 0 && (dungeonMap[leftX, y] == TileType.Floor || dungeonMap[leftX, y] == TileType.SpecialFloor))
                    connectionCount[room]++;

                if (rightX < dungeonWidth && (dungeonMap[rightX, y] == TileType.Floor || dungeonMap[rightX, y] == TileType.SpecialFloor))
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

        // Then, ensure every room has at least two connections (except special room which may have multiple connections in small dungeons)
        roomsNeedingConnections.Clear();
        foreach (Room room in rooms)
        {
            // Skip boss room for non-essential corridor connections
            if (enableBossRoom && room == bossRoom)
                continue;
                
            // Skip special room - it may have multiple connections in small dungeons, so we don't force additional connections
            if (enableSpecialRoom && room == specialRoom)
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

                if (topY < dungeonHeight && (dungeonMap[x, topY] == TileType.Floor || dungeonMap[x, topY] == TileType.SpecialFloor))
                {
                    Room connectedRoom = FindRoomConnectedByCorridor(new Vector2Int(x, topY), room);
                    if (connectedRoom != null && !connected.Contains(connectedRoom))
                        connected.Add(connectedRoom);
                }

                if (bottomY >= 0 && (dungeonMap[x, bottomY] == TileType.Floor || dungeonMap[x, bottomY] == TileType.SpecialFloor))
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

                if (leftX >= 0 && (dungeonMap[leftX, y] == TileType.Floor || dungeonMap[leftX, y] == TileType.SpecialFloor))
                {
                    Room connectedRoom = FindRoomConnectedByCorridor(new Vector2Int(leftX, y), room);
                    if (connectedRoom != null && !connected.Contains(connectedRoom))
                        connected.Add(connectedRoom);
                }

                if (rightX < dungeonWidth && (dungeonMap[rightX, y] == TileType.Floor || dungeonMap[rightX, y] == TileType.SpecialFloor))
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
                    (dungeonMap[next.x, next.y] == TileType.Floor || dungeonMap[next.x, next.y] == TileType.SpecialFloor))
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

        // Calculate dungeon size to determine if special room can be a connection target
        int totalCells = cellsX * cellsY;
        bool isSmallDungeon = totalCells <= 24; // 6x4 or smaller dungeon

        foreach (Room room in rooms)
        {
            // Skip boss room and excluded room
            if (room != sourceRoom && room != excludeRoom && 
                !(enableBossRoom && room == bossRoom))
            {
                // For small dungeons, allow special room to be a connection target
                // For larger dungeons, skip special room (it should only have one connection)
                if (enableSpecialRoom && room == specialRoom && !isSmallDungeon)
                {
                    continue;
                }

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

        // Calculate dungeon size to determine if special room can be a connection target
        int totalCells = cellsX * cellsY;
        bool isSmallDungeon = totalCells <= 24; // 6x4 or smaller dungeon

        foreach (Room room in rooms)
        {
            // Skip boss room and excluded rooms
            if (room != sourceRoom && !excludeRooms.Contains(room) && 
                !(enableBossRoom && room == bossRoom))
            {
                // For small dungeons, allow special room to be a connection target
                // For larger dungeons, skip special room (it should only have one connection)
                if (enableSpecialRoom && room == specialRoom && !isSmallDungeon)
                {
                    continue;
                }

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
        // Skip connection if boss room is locked and either room is the boss room
        if (enablePuzzleBossRoomLock && enableBossRoom && IsBossRoomLockedDuringGeneration() && 
            (roomA == bossRoom || roomB == bossRoom))
        {
            return;
        }

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

                    // Double-check: never carve non-essential floor in special room area
                    if (enableSpecialRoom && IsPositionInSpecialRoom(x, yPos))
                        continue;

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

                    // Double-check: never carve non-essential floor in special room area
                    if (enableSpecialRoom && IsPositionInSpecialRoom(xPos, y))
                        continue;

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

        // Never allow carving in the special room area
        if (enableSpecialRoom && IsPositionInSpecialRoom(x, y))
            return false;

        // Never allow carving near boss room if it's locked
        if (enablePuzzleBossRoomLock && enableBossRoom && IsBossRoomLockedDuringGeneration() && IsNearBossRoom(x, y))
            return false;

        // Always allow carving if the tile is already a corridor (TileType.Floor, NonEssentialFloor, or SpecialFloor)
        // or if it's currently void (not yet carved)
        // BUT never allow non-essential floor tiles in special room area
        if (dungeonMap[x, y] == TileType.Floor || dungeonMap[x, y] == TileType.NonEssentialFloor || dungeonMap[x, y] == TileType.SpecialFloor || dungeonMap[x, y] == TileType.Void)
        {
            // Special case: don't allow non-essential floor tiles to be placed in special room area
            if (enableSpecialRoom && IsPositionInSpecialRoom(x, y) && dungeonMap[x, y] == TileType.Void)
                return false;
                
            return true;
        }

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

    /// <summary>
    /// Converts all hole tiles to floor tiles and updates their visual representation.
    /// This is called when puzzle4 (CrackedWall4) is solved.
    /// </summary>
    public void ConvertHoleTilesToFloorTiles()
    {
        int convertedCount = 0;
        
        // Find all hole tiles and convert them to floor tiles
        for (int x = 0; x < dungeonWidth; x++)
        {
            for (int y = 0; y < dungeonHeight; y++)
            {
                if (dungeonMap[x, y] == TileType.Hole)
                {
                    dungeonMap[x, y] = TileType.Floor;
                    convertedCount++;
                    
                    // Find the hole GameObject at this position and replace it with a floor tile
                    Vector3 tilePosition = new Vector3(x, y, 0);
                    Collider2D[] colliders = Physics2D.OverlapPointAll(tilePosition);
                    
                    foreach (Collider2D collider in colliders)
                    {
                        if (collider.CompareTag("Hole") || collider.gameObject.name.Contains("Hole"))
                        {
                            // Destroy the hole GameObject
                            Destroy(collider.gameObject);
                            
                            // Instantiate a floor tile at the same position
                            if (floorPrefab != null)
                            {
                                Instantiate(floorPrefab, tilePosition, Quaternion.identity, dungeonParent);
                            }
                            else
                            {
                            }
                            
                            break; // Only replace the first hole found at this position
                        }
                    }
                }
            }
        }
    }
}