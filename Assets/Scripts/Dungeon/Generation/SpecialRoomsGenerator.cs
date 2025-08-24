using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles the generation of special rooms like boss rooms and special puzzle rooms.
/// </summary>
public class SpecialRoomsGenerator
{
    private DungeonConfig config;
    private TileType[,] dungeonMap;
    private List<Room> rooms;
    private int cellsX, cellsY;
    private GameObject royalSlimeBossPrefab;
    private GameObject bossRoomFloorPrefab;
    private GameObject specialRoomFloorPrefab;
    private GameObject crackedWall4Prefab;

    public Room BossRoom { get; private set; }
    public Room SpecialRoom { get; private set; }
    public GameObject SpawnedBoss { get; private set; }
    public Vector2Int SpecialRoomEntrance { get; private set; }
    public Vector2Int SpecialRoomPlatformCenter { get; private set; }

    public SpecialRoomsGenerator(DungeonConfig config, TileType[,] dungeonMap, List<Room> rooms, int cellsX, int cellsY)
    {
        this.config = config;
        this.dungeonMap = dungeonMap;
        this.rooms = rooms;
        this.cellsX = cellsX;
        this.cellsY = cellsY;
    }

    public void SetBossPrefabs(GameObject royalSlimeBossPrefab, GameObject bossRoomFloorPrefab)
    {
        this.royalSlimeBossPrefab = royalSlimeBossPrefab;
        this.bossRoomFloorPrefab = bossRoomFloorPrefab;
    }

    public void SetSpecialRoomPrefabs(GameObject specialRoomFloorPrefab, GameObject crackedWall4Prefab)
    {
        this.specialRoomFloorPrefab = specialRoomFloorPrefab;
        this.crackedWall4Prefab = crackedWall4Prefab;
    }

    /// <summary>
    /// Creates a boss room in the center of the dungeon spanning 1.5x1.5 cells.
    /// </summary>
    public void CreateBossRoom()
    {
        if (!config.enableBossRoom) return;

        // Calculate the center cells (1.5x1.5 area)
        int centerCellX = cellsX / 2 - 1; // Start from left of center
        int centerCellY = cellsY / 2 - 1; // Start from bottom of center
        
        // Ensure we have at least 2x2 cells available for the 1.5x1.5 area
        if (centerCellX < 0 || centerCellY < 0 || centerCellX + 1 >= cellsX || centerCellY + 1 >= cellsY)
        {
            return;
        }

        // Calculate the boss room position and size (1.5 cells = 1 full cell + 0.5 cell)
        int bossRoomStartX = config.bufferX + centerCellX * config.cellWidth + config.cellWidth / 4; // Start 1/4 into the first cell
        int bossRoomStartY = config.bufferY + centerCellY * config.cellHeight + config.cellHeight / 4; // Start 1/4 into the first cell
        int bossRoomWidth = (int)(config.cellWidth * 1.5f);  // Span 1.5 cells horizontally
        int bossRoomHeight = (int)(config.cellHeight * 1.5f); // Span 1.5 cells vertically

        // Create the boss room
        BossRoom = new Room();
        BossRoom.x = bossRoomStartX;
        BossRoom.y = bossRoomStartY;
        BossRoom.width = bossRoomWidth;
        BossRoom.height = bossRoomHeight;
        BossRoom.isSpecialShape = false; // Boss room is always rectangular

        // Add the boss room to our list
        rooms.Add(BossRoom);

        // Carve out the boss room
        CarveBossRoom();
        
        // Spawn the Royal Slime boss in the center of the boss room
        SpawnRoyalSlimeBoss();
    }
    
    /// <summary>
    /// Spawns the Royal Slime boss in the center of the boss room.
    /// </summary>
    private void SpawnRoyalSlimeBoss()
    {
        if (royalSlimeBossPrefab == null)
        {
            Debug.LogWarning("SpecialRoomsGenerator: Royal Slime boss prefab is not assigned!");
            return;
        }
        
        if (BossRoom == null)
        {
            Debug.LogWarning("SpecialRoomsGenerator: Boss room is null, cannot spawn boss!");
            return;
        }
        
        // Calculate the center position of the boss room
        Vector2 bossRoomCenter = new Vector2(
            BossRoom.x + BossRoom.width / 2f,
            BossRoom.y + BossRoom.height / 2f
        );
        
        // Convert tile coordinates to world coordinates
        Vector3 bossSpawnPosition = new Vector3(bossRoomCenter.x, bossRoomCenter.y, -5f);
        
        // Spawn the boss
        SpawnedBoss = Object.Instantiate(royalSlimeBossPrefab, bossSpawnPosition, Quaternion.identity);
        
        // Force the position to be set correctly (in case animation overrides it)
        SpawnedBoss.transform.position = bossSpawnPosition;
        
        Debug.Log($"SpecialRoomsGenerator: Royal Slime boss spawned at position {bossSpawnPosition}");
    }

    /// <summary>
    /// Creates a special room that always spawns with 2 cells length and 1 cell height.
    /// </summary>
    public void CreateSpecialRoom()
    {
        if (!config.enableSpecialRoom) return;

        // Find a suitable location for the special room (avoid boss room area)
        Vector2Int? specialRoomLocation = FindSpecialRoomLocation();
        
        if (!specialRoomLocation.HasValue)
        {
            return;
        }

        // Calculate the special room position and size (2 cells length, 1 cell height)
        int specialRoomStartX = specialRoomLocation.Value.x;
        int specialRoomStartY = specialRoomLocation.Value.y;
        int specialRoomWidth = config.cellWidth * 2;  // Span 2 cells horizontally
        int specialRoomHeight = config.cellHeight;    // Span 1 cell vertically

        // Create the special room
        SpecialRoom = new Room();
        SpecialRoom.x = specialRoomStartX;
        SpecialRoom.y = specialRoomStartY;
        SpecialRoom.width = specialRoomWidth;
        SpecialRoom.height = specialRoomHeight;
        SpecialRoom.isSpecialShape = false; // Special room is always rectangular

        // Add the special room to our list
        rooms.Add(SpecialRoom);

        // Carve out the special room
        CarveSpecialRoom();

        // Fill the special room with holes (platform will be added later in ConnectSpecialRoom)
        FillSpecialRoomWithHoles();
    }

    /// <summary>
    /// Connects the special room to nearby rooms with one or more corridors based on dungeon size.
    /// </summary>
    public void ConnectSpecialRoom()
    {
        if (SpecialRoom == null) return;

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

    /// <summary>
    /// Connects the special room to a single nearest room (original logic for larger dungeons).
    /// </summary>
    private void ConnectSpecialRoomSingle()
    {
        // Find the nearest room to connect to (excluding boss room)
        Room nearestRoom = FindNearestRoomForSpecialRoom();
        
        if (nearestRoom != null)
        {
            // Find and store the entrance point where the corridor connects to the special room
            FindSpecialRoomEntrance(nearestRoom);
            
            // Add a platform on the opposite side from the entrance
            AddPlatformToSpecialRoom();
        }
    }

    /// <summary>
    /// Connects the special room to multiple nearby rooms (for small dungeons).
    /// </summary>
    private void ConnectSpecialRoomMultiple(bool isSmallDungeon)
    {
        // Find all nearby rooms within a reasonable distance
        List<Room> nearbyRooms = FindNearbyRoomsForSpecialRoom(isSmallDungeon);
        
        if (nearbyRooms.Count > 0)
        {
            // For multiple entrances, we'll use the first connection as the main entrance
            FindSpecialRoomEntrance(nearbyRooms[0]);
            AddPlatformToSpecialRoom();
        }
    }

    /// <summary>
    /// Finds multiple nearby rooms for special room connection (for small dungeons).
    /// </summary>
    private List<Room> FindNearbyRoomsForSpecialRoom(bool isSmallDungeon)
    {
        List<Room> nearbyRooms = new List<Room>();
        
        foreach (Room room in rooms)
        {
            // Skip the special room itself and the boss room
            if (room == SpecialRoom || (config.enableBossRoom && room == BossRoom))
            {
                continue;
            }

            // Add all rooms regardless of distance
            nearbyRooms.Add(room);
        }

        // Sort by distance to prioritize closer rooms
        nearbyRooms.Sort((a, b) => {
            float distanceA = Vector2.Distance(
                new Vector2(SpecialRoom.x + SpecialRoom.width / 2, SpecialRoom.y + SpecialRoom.height / 2),
                new Vector2(a.x + a.width / 2, a.y + a.height / 2)
            );
            float distanceB = Vector2.Distance(
                new Vector2(SpecialRoom.x + SpecialRoom.width / 2, SpecialRoom.y + SpecialRoom.height / 2),
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

    /// <summary>
    /// Finds the nearest room for the special room to connect to.
    /// </summary>
    private Room FindNearestRoomForSpecialRoom()
    {
        Room nearest = null;
        float minDistance = float.MaxValue;

        foreach (Room room in rooms)
        {
            // Skip the special room itself and the boss room
            if (room == SpecialRoom || (config.enableBossRoom && room == BossRoom))
            {
                continue;
            }

            float distance = Vector2.Distance(
                new Vector2(SpecialRoom.x + SpecialRoom.width / 2, SpecialRoom.y + SpecialRoom.height / 2),
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

    /// <summary>
    /// Finds a suitable location for the special room (avoiding boss room area).
    /// </summary>
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
                config.bufferX + offset.x * config.cellWidth,
                config.bufferY + offset.y * config.cellHeight
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
                        config.bufferX + x * config.cellWidth,
                        config.bufferY + y * config.cellHeight
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

    /// <summary>
    /// Checks if a location is valid for the special room.
    /// </summary>
    private bool IsValidSpecialRoomLocation(Vector2Int location)
    {
        // Check if the room would fit within dungeon bounds
        if (location.x + config.cellWidth * 2 > config.dungeonWidth - config.bufferX || 
            location.y + config.cellHeight > config.dungeonHeight - config.bufferY)
        {
            return false;
        }

        // Check if the location overlaps with the boss room area (1.5x1.5 cells in center)
        if (config.enableBossRoom)
        {
            // Calculate the boss room area (1.5x1.5 cells in center)
            int centerCellX = cellsX / 2 - 1;
            int centerCellY = cellsY / 2 - 1;
            
            // Calculate boss room bounds in tile coordinates
            int bossRoomStartX = config.bufferX + centerCellX * config.cellWidth + config.cellWidth / 4;
            int bossRoomStartY = config.bufferY + centerCellY * config.cellHeight + config.cellHeight / 4;
            int bossRoomWidth = (int)(config.cellWidth * 1.5f);
            int bossRoomHeight = (int)(config.cellHeight * 1.5f);

            // Check for overlap with special room
            if (location.x < bossRoomStartX + bossRoomWidth &&
                location.x + config.cellWidth * 2 > bossRoomStartX &&
                location.y < bossRoomStartY + bossRoomHeight &&
                location.y + config.cellHeight > bossRoomStartY)
            {
                return false; // Overlap detected
            }
        }

        return true;
    }

    /// <summary>
    /// Finds where the corridor connects to the special room.
    /// </summary>
    private void FindSpecialRoomEntrance(Room connectedRoom)
    {
        // Find the center points of both rooms
        Vector2Int specialRoomCenter = new Vector2Int(
            SpecialRoom.x + SpecialRoom.width / 2,
            SpecialRoom.y + SpecialRoom.height / 2
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
                SpecialRoomEntrance = new Vector2Int(SpecialRoom.x, SpecialRoom.y + SpecialRoom.height / 2);
            }
            else
            {
                // Connected from the right, so entrance is on the right side
                SpecialRoomEntrance = new Vector2Int(SpecialRoom.x + SpecialRoom.width - 1, SpecialRoom.y + SpecialRoom.height / 2);
            }
        }
        else
        {
            // Vertical connection (top or bottom side)
            if (direction.y > 0)
            {
                // Connected from the bottom, so entrance is on the bottom side
                SpecialRoomEntrance = new Vector2Int(SpecialRoom.x + SpecialRoom.width / 2, SpecialRoom.y);
            }
            else
            {
                // Connected from the top, so entrance is on the top side
                SpecialRoomEntrance = new Vector2Int(SpecialRoom.x + SpecialRoom.width / 2, SpecialRoom.y + SpecialRoom.height - 1);
            }
        }
    }

    /// <summary>
    /// Adds a platform on the opposite side from the entrance.
    /// </summary>
    private void AddPlatformToSpecialRoom()
    {
        if (SpecialRoom == null) return;
        
        // Calculate the opposite side from the entrance
        Vector2Int platformPosition = CalculateOppositePlatformPosition();
        
        // Store the platform center for CrackedWall4 placement
        SpecialRoomPlatformCenter = platformPosition;
        
        // Create a 3x3 platform attached to the wall
        CreatePlatform(platformPosition);
        
        // Create floor tile islands around the platform
        CreateFloorIslandsInSpecialRoom();
    }

    /// <summary>
    /// Calculates the position for the platform on the opposite side from the entrance.
    /// </summary>
    private Vector2Int CalculateOppositePlatformPosition()
    {
        Vector2Int specialRoomCenter = new Vector2Int(
            SpecialRoom.x + SpecialRoom.width / 2,
            SpecialRoom.y + SpecialRoom.height / 2
        );
        
        // Calculate the direction from center to entrance
        Vector2Int directionToEntrance = SpecialRoomEntrance - specialRoomCenter;
        
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
                platformPosition = new Vector2Int(SpecialRoom.x + SpecialRoom.width - 1, SpecialRoom.y + SpecialRoom.height / 2);
            }
            else
            {
                // Platform on left wall
                platformPosition = new Vector2Int(SpecialRoom.x, SpecialRoom.y + SpecialRoom.height / 2);
            }
        }
        else
        {
            // Platform on top or bottom wall
            if (oppositeDirection.y > 0)
            {
                // Platform on top wall
                platformPosition = new Vector2Int(SpecialRoom.x + SpecialRoom.width / 2, SpecialRoom.y + SpecialRoom.height - 1);
            }
            else
            {
                // Platform on bottom wall
                platformPosition = new Vector2Int(SpecialRoom.x + SpecialRoom.width / 2, SpecialRoom.y);
            }
        }
        
        return platformPosition;
    }

    /// <summary>
    /// Creates a 3x3 platform attached to the wall.
    /// </summary>
    private void CreatePlatform(Vector2Int centerPosition)
    {
        // Create a 3x3 platform with the center at the specified position
        for (int x = centerPosition.x - 1; x <= centerPosition.x + 1; x++)
        {
            for (int y = centerPosition.y - 1; y <= centerPosition.y + 1; y++)
            {
                // Check if the position is within the special room bounds
                if (x >= SpecialRoom.x && x < SpecialRoom.x + SpecialRoom.width &&
                    y >= SpecialRoom.y && y < SpecialRoom.y + SpecialRoom.height)
                {
                    // Place floor tiles for the platform
                    dungeonMap[x, y] = TileType.SpecialFloor;
                }
            }
        }
        
        // Place CrackedWall4 along the wall of the platform
        PlaceCrackedWall4OnPlatform(centerPosition);
    }

    /// <summary>
    /// Places CrackedWall4 along the wall of the platform.
    /// </summary>
    private void PlaceCrackedWall4OnPlatform(Vector2Int centerPosition)
    {
        if (crackedWall4Prefab == null) return;

        // Determine which wall the platform is attached to and place CrackedWall4 along it
        Vector2Int specialRoomCenter = new Vector2Int(
            SpecialRoom.x + SpecialRoom.width / 2,
            SpecialRoom.y + SpecialRoom.height / 2
        );
        
        // Calculate the direction from center to entrance
        Vector2Int directionToEntrance = SpecialRoomEntrance - specialRoomCenter;
        
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

    /// <summary>
    /// Fills the entire special room with holes.
    /// </summary>
    private void FillSpecialRoomWithHoles()
    {
        for (int x = SpecialRoom.x; x < SpecialRoom.x + SpecialRoom.width; x++)
        {
            for (int y = SpecialRoom.y; y < SpecialRoom.y + SpecialRoom.height; y++)
            {
                dungeonMap[x, y] = TileType.Hole;
            }
        }
    }
    
    /// <summary>
    /// Creates floor tile islands of various sizes in the special room.
    /// </summary>
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
    
    /// <summary>
    /// Creates a single floor island of specified size.
    /// </summary>
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
    }
    
    /// <summary>
    /// Finds a suitable position for an island of specified size.
    /// </summary>
    private Vector2Int? FindIslandPosition(int size)
    {
        List<Vector2Int> possiblePositions = new List<Vector2Int>();
        
        // Look for positions where the entire island would fit within the special room
        for (int x = SpecialRoom.x; x <= SpecialRoom.x + SpecialRoom.width - size; x++)
        {
            for (int y = SpecialRoom.y; y <= SpecialRoom.y + SpecialRoom.height - size; y++)
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
    
    /// <summary>
    /// Checks if a position is valid for placing an island.
    /// </summary>
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
                if (checkX < SpecialRoom.x || checkX >= SpecialRoom.x + SpecialRoom.width ||
                    checkY < SpecialRoom.y || checkY >= SpecialRoom.y + SpecialRoom.height)
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
                    if (SpecialRoomPlatformCenter != Vector2Int.zero)
                    {
                        // Check if the adjacent floor tile is within the 3x3 platform area
                        if (checkX >= SpecialRoomPlatformCenter.x - 1 && checkX <= SpecialRoomPlatformCenter.x + 1 &&
                            checkY >= SpecialRoomPlatformCenter.y - 1 && checkY <= SpecialRoomPlatformCenter.y + 1)
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

    /// <summary>
    /// Carves out the boss room in the dungeonMap.
    /// </summary>
    private void CarveBossRoom()
    {
        for (int x = BossRoom.x; x < BossRoom.x + BossRoom.width; x++)
        {
            for (int y = BossRoom.y; y < BossRoom.y + BossRoom.height; y++)
            {
                if (x >= 0 && x < config.dungeonWidth && y >= 0 && y < config.dungeonHeight)
                {
                    dungeonMap[x, y] = TileType.BossFloor;
                }
            }
        }
    }

    /// <summary>
    /// Carves out the special room in the dungeonMap.
    /// </summary>
    private void CarveSpecialRoom()
    {
        for (int x = SpecialRoom.x; x < SpecialRoom.x + SpecialRoom.width; x++)
        {
            for (int y = SpecialRoom.y; y < SpecialRoom.y + SpecialRoom.height; y++)
            {
                if (x >= 0 && x < config.dungeonWidth && y >= 0 && y < config.dungeonHeight)
                {
                    dungeonMap[x, y] = TileType.SpecialFloor;
                }
            }
        }
    }
}
