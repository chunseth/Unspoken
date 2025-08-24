using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles the generation of corridors between rooms and cells in the dungeon.
/// </summary>
public class CorridorGenerator
{
    private DungeonConfig config;
    private TileType[,] dungeonMap;
    private Cell[,] cells;
    private int cellsX, cellsY;
    private List<Room> rooms;
    private Room bossRoom;
    private Room specialRoom;

    public CorridorGenerator(DungeonConfig config, TileType[,] dungeonMap, Cell[,] cells, int cellsX, int cellsY, List<Room> rooms)
    {
        this.config = config;
        this.dungeonMap = dungeonMap;
        this.cells = cells;
        this.cellsX = cellsX;
        this.cellsY = cellsY;
        this.rooms = rooms;
    }

    public void SetSpecialRooms(Room bossRoom, Room specialRoom)
    {
        this.bossRoom = bossRoom;
        this.specialRoom = specialRoom;
    }

    /// <summary>
    /// Returns a list of unvisited neighbour cells (up, down, left, right).
    /// </summary>
    public List<Cell> GetUnvisitedNeighbours(Cell cell)
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

    /// <summary>
    /// Carves a corridor between two cells by connecting their centers.
    /// </summary>
    public void CarveCorridorBetweenCells(Cell a, Cell b)
    {
        // Skip corridor generation if either cell is in the special room area
        if (config.enableSpecialRoom && (IsInSpecialRoomArea(a.cellX, a.cellY) || IsInSpecialRoomArea(b.cellX, b.cellY)))
        {
            return;
        }

        // Skip corridor generation if either cell is in the boss room area
        if (config.enableBossRoom && (IsInBossRoomArea(a.cellX, a.cellY) || IsInBossRoomArea(b.cellX, b.cellY)))
        {
            return;
        }

        // Convert cell coordinates to tile coordinates (center of each cell)
        int ax = config.bufferX + a.cellX * config.cellWidth + config.cellWidth / 2;
        int ay = config.bufferY + a.cellY * config.cellHeight + config.cellHeight / 2;
        int bx = config.bufferX + b.cellX * config.cellWidth + config.cellWidth / 2;
        int by = config.bufferY + b.cellY * config.cellHeight + config.cellHeight / 2;

        if (!config.enhancedCorridors)
        {
            // Standard corridor generation
            // Carve a horizontal corridor first, then vertical
            for (int x = Mathf.Min(ax, bx); x <= Mathf.Max(ax, bx); x++)
            {
                dungeonMap[x, ay] = TileType.Floor;
            }
            // Carve a vertical corridor
            for (int y = Mathf.Min(ay, by); y <= Mathf.Max(ay, by); y++)
            {
                dungeonMap[bx, y] = TileType.Floor;
            }
        }
        else
        {
            // Enhanced corridor generation with variable width and winding
            int corridorWidth = Random.Range(config.corridorMinWidth, config.corridorMaxWidth + 1);

            // Determine if we should make this corridor winding
            bool windingCorridor = Random.value < config.corridorWindingFactor;

            if (windingCorridor)
            {
                // Create a winding path using waypoints
                CarveWindingCorridor(ax, ay, bx, by, corridorWidth);
            }
            else
            {
                // Always carve horizontal then vertical (or vice versa)
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

    /// <summary>
    /// Carves a horizontal corridor with specified width.
    /// </summary>
    private void CarveHorizontalCorridor(int x1, int y1, int x2, int y2, int width)
    {
        int startX = Mathf.Min(x1, x2);
        int endX = Mathf.Max(x1, x2);

        for (int x = startX; x <= endX; x++)
        {
            for (int w = 0; w < width; w++)
            {
                int yPos = y1 - width / 2 + w;
                if (yPos >= 0 && yPos < config.dungeonHeight)
                {
                    // Skip if this position is in the boss room area
                    if (config.enableBossRoom && IsPositionInBossRoom(x, yPos))
                        continue;
                        
                    dungeonMap[x, yPos] = TileType.Floor;
                }
            }
        }
    }

    /// <summary>
    /// Carves a vertical corridor with specified width.
    /// </summary>
    private void CarveVerticalCorridor(int x1, int y1, int x2, int y2, int width)
    {
        int startY = Mathf.Min(y1, y2);
        int endY = Mathf.Max(y1, y2);

        for (int y = startY; y <= endY; y++)
        {
            for (int w = 0; w < width; w++)
            {
                int xPos = x1 - width / 2 + w;
                if (xPos >= 0 && xPos < config.dungeonWidth)
                {
                    // Skip if this position is in the boss room area
                    if (config.enableBossRoom && IsPositionInBossRoom(xPos, y))
                        continue;
                        
                    dungeonMap[xPos, y] = TileType.Floor;
                }
            }
        }
    }

    /// <summary>
    /// Carves a winding corridor using waypoints.
    /// </summary>
    private void CarveWindingCorridor(int ax, int ay, int bx, int by, int width)
    {
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

            int waypointX = Mathf.Clamp(baseX + randX, config.bufferX + 2, config.dungeonWidth - config.bufferX - 2);
            int waypointY = Mathf.Clamp(baseY + randY, config.bufferY + 2, config.dungeonHeight - config.bufferY - 2);

            // Ensure waypoint is not in boss room area
            if (config.enableBossRoom && IsPositionInBossRoom(waypointX, waypointY))
            {
                // Try to find an alternative position outside the boss room
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    randX = Random.Range(-8, 9);
                    randY = Random.Range(-8, 9);
                    waypointX = Mathf.Clamp(baseX + randX, config.bufferX + 2, config.dungeonWidth - config.bufferX - 2);
                    waypointY = Mathf.Clamp(baseY + randY, config.bufferY + 2, config.dungeonHeight - config.bufferY - 2);
                    
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

    /// <summary>
    /// Adds non-essential corridors for better connectivity.
    /// </summary>
    public void AddNonEssentialCorridors()
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
            if (config.enableBossRoom && room == bossRoom)
                continue;
                
            connectionCount[room] = 0;
        }

        // Count existing corridors by checking floor tiles around room perimeters
        foreach (Room room in rooms)
        {
            // Skip boss room for non-essential corridor connections
            if (config.enableBossRoom && room == bossRoom)
                continue;
                
            // Check the perimeter of the room for corridor connections
            for (int x = room.x - 1; x <= room.x + room.width; x++)
            {
                // Check top and bottom edges
                if (x >= 0 && x < config.dungeonWidth)
                {
                    int topY = room.y + room.height;
                    int bottomY = room.y - 1;

                    if (topY < config.dungeonHeight && (dungeonMap[x, topY] == TileType.Floor || dungeonMap[x, topY] == TileType.SpecialFloor))
                        connectionCount[room]++;

                    if (bottomY >= 0 && (dungeonMap[x, bottomY] == TileType.Floor || dungeonMap[x, bottomY] == TileType.SpecialFloor))
                        connectionCount[room]++;
                }
            }

            for (int y = room.y; y < room.y + room.height; y++)
            {
                // Check left and right edges
                if (y >= 0 && y < config.dungeonHeight)
                {
                    int leftX = room.x - 1;
                    int rightX = room.x + room.width;

                    if (leftX >= 0 && (dungeonMap[leftX, y] == TileType.Floor || dungeonMap[leftX, y] == TileType.SpecialFloor))
                        connectionCount[room]++;

                    if (rightX < config.dungeonWidth && (dungeonMap[rightX, y] == TileType.Floor || dungeonMap[rightX, y] == TileType.SpecialFloor))
                        connectionCount[room]++;
                }
            }
        }

        // First, ensure every room has at least one connection
        List<Room> roomsNeedingConnections = new List<Room>();
        foreach (Room room in rooms)
        {
            // Skip boss room for non-essential corridor connections
            if (config.enableBossRoom && room == bossRoom)
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

        // Then, ensure every room has at least two connections (except special room)
        roomsNeedingConnections.Clear();
        foreach (Room room in rooms)
        {
            // Skip boss room for non-essential corridor connections
            if (config.enableBossRoom && room == bossRoom)
                continue;
                
            // Skip special room - it may have multiple connections in small dungeons
            if (config.enableSpecialRoom && room == specialRoom)
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
    }

    /// <summary>
    /// Connects two rooms with a corridor.
    /// </summary>
    public void ConnectRoomsWithCorridor(Room roomA, Room roomB)
    {
        // Find center points of each room
        Vector2Int centerA = roomA.Center;
        Vector2Int centerB = roomB.Center;

        // Carve a winding corridor between the rooms
        CarveNonEssentialCorridor(centerA, centerB, roomA, roomB, 2);
    }

    /// <summary>
    /// Carves a non-essential corridor between two points.
    /// </summary>
    private void CarveNonEssentialCorridor(Vector2Int startPoint, Vector2Int endPoint, Room roomA, Room roomB, int width)
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

            int waypointX = Mathf.Clamp(baseX + randX, 2, config.dungeonWidth - 3);
            int waypointY = Mathf.Clamp(baseY + randY, 2, config.dungeonHeight - 3);

            // Ensure waypoint is not in boss room area
            if (config.enableBossRoom && IsPositionInBossRoom(waypointX, waypointY))
            {
                // Try to find an alternative position outside the boss room
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    randX = Random.Range(-12, 13);
                    randY = Random.Range(-12, 13);
                    waypointX = Mathf.Clamp(baseX + randX, 2, config.dungeonWidth - 3);
                    waypointY = Mathf.Clamp(baseY + randY, 2, config.dungeonHeight - 3);
                    
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

    /// <summary>
    /// Carves a non-essential corridor segment.
    /// </summary>
    private void CarveNonEssentialSegment(int x1, int y1, int x2, int y2, bool horizontal, int width, Room roomA, Room roomB)
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
                    if (config.enableSpecialRoom && IsPositionInSpecialRoom(x, yPos))
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
                    if (config.enableSpecialRoom && IsPositionInSpecialRoom(xPos, y))
                        continue;

                    if (CanCarveNonEssentialTile(xPos, y, roomA, roomB))
                    {
                        dungeonMap[xPos, y] = TileType.NonEssentialFloor;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if a non-essential tile can be carved at the specified position.
    /// </summary>
    private bool CanCarveNonEssentialTile(int x, int y, Room allowedRoomA, Room allowedRoomB)
    {
        // Check if position is within dungeon bounds
        if (x < 0 || x >= config.dungeonWidth || y < 0 || y >= config.dungeonHeight)
            return false;

        // Never allow carving in the boss room area
        if (config.enableBossRoom && IsPositionInBossRoom(x, y))
            return false;

        // Never allow carving in the special room area
        if (config.enableSpecialRoom && IsPositionInSpecialRoom(x, y))
            return false;

        // Always allow carving if the tile is already a corridor or if it's currently void
        if (dungeonMap[x, y] == TileType.Floor || dungeonMap[x, y] == TileType.NonEssentialFloor || 
            dungeonMap[x, y] == TileType.SpecialFloor || dungeonMap[x, y] == TileType.Void)
        {
            // Special case: don't allow non-essential floor tiles to be placed in special room area
            if (config.enableSpecialRoom && IsPositionInSpecialRoom(x, y) && dungeonMap[x, y] == TileType.Void)
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

    // Helper methods for room finding and connectivity
    private Room FindClosestRoom(Room sourceRoom, Room excludeRoom)
    {
        Room closest = null;
        float minDistance = float.MaxValue;

        foreach (Room room in rooms)
        {
            // Skip boss room and excluded room
            if (room != sourceRoom && room != excludeRoom && 
                !(config.enableBossRoom && room == bossRoom))
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

    private Room FindClosestRoom(Room sourceRoom, List<Room> excludeRooms)
    {
        Room closest = null;
        float minDistance = float.MaxValue;

        foreach (Room room in rooms)
        {
            // Skip boss room and excluded rooms
            if (room != sourceRoom && !excludeRooms.Contains(room) && 
                !(config.enableBossRoom && room == bossRoom))
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

    private List<Room> FindConnectedRooms(Room room)
    {
        List<Room> connected = new List<Room>();

        // Check corridors extending from each side of the room
        for (int x = room.x - 1; x <= room.x + room.width; x++)
        {
            // Check top and bottom
            if (x >= 0 && x < config.dungeonWidth)
            {
                int topY = room.y + room.height;
                int bottomY = room.y - 1;

                if (topY < config.dungeonHeight && (dungeonMap[x, topY] == TileType.Floor || dungeonMap[x, topY] == TileType.SpecialFloor))
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
            if (y >= 0 && y < config.dungeonHeight)
            {
                int leftX = room.x - 1;
                int rightX = room.x + room.width;

                if (leftX >= 0 && (dungeonMap[leftX, y] == TileType.Floor || dungeonMap[leftX, y] == TileType.SpecialFloor))
                {
                    Room connectedRoom = FindRoomConnectedByCorridor(new Vector2Int(leftX, y), room);
                    if (connectedRoom != null && !connected.Contains(connectedRoom))
                        connected.Add(connectedRoom);
                }

                if (rightX < config.dungeonWidth && (dungeonMap[rightX, y] == TileType.Floor || dungeonMap[rightX, y] == TileType.SpecialFloor))
                {
                    Room connectedRoom = FindRoomConnectedByCorridor(new Vector2Int(rightX, y), room);
                    if (connectedRoom != null && !connected.Contains(connectedRoom))
                        connected.Add(connectedRoom);
                }
            }
        }

        return connected;
    }

    private Room FindRoomConnectedByCorridor(Vector2Int startPoint, Room excludeRoom)
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
                if (next.x >= 0 && next.x < config.dungeonWidth &&
                    next.y >= 0 && next.y < config.dungeonHeight &&
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

    private Room GetRoomAtPosition(int x, int y)
    {
        foreach (Room room in rooms)
        {
            if (room.ContainsPosition(x, y))
            {
                return room;
            }
        }
        return null;
    }

    // Helper methods for checking room areas
    private bool IsInBossRoomArea(int cellX, int cellY)
    {
        if (!config.enableBossRoom) return false;

        // Calculate the center cells
        int centerCellX = cellsX / 2 - 1;
        int centerCellY = cellsY / 2 - 1;

        // Check if the cell is within the 1.5x1.5 center area
        return cellX >= centerCellX && cellX <= centerCellX + 1 &&
               cellY >= centerCellY && cellY <= centerCellY + 1;
    }

    private bool IsInSpecialRoomArea(int cellX, int cellY)
    {
        if (!config.enableSpecialRoom || specialRoom == null) return false;

        // Calculate which cell this position is in
        int roomCellX = (specialRoom.x - config.bufferX) / config.cellWidth;
        int roomCellY = (specialRoom.y - config.bufferY) / config.cellHeight;

        // Check if the cell is within the 2x1 special room area
        return cellX >= roomCellX && cellX < roomCellX + 2 &&
               cellY >= roomCellY && cellY < roomCellY + 1;
    }

    private bool IsPositionInBossRoom(int x, int y)
    {
        if (bossRoom == null) return false;
        
        return x >= bossRoom.x && x < bossRoom.x + bossRoom.width &&
               y >= bossRoom.y && y < bossRoom.y + bossRoom.height;
    }

    private bool IsPositionInSpecialRoom(int x, int y)
    {
        if (specialRoom == null) return false;
        
        return x >= specialRoom.x && x < specialRoom.x + specialRoom.width &&
               y >= specialRoom.y && y < specialRoom.y + specialRoom.height;
    }
}
