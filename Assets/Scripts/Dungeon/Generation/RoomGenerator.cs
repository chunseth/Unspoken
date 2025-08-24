using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles the generation of rooms within the dungeon, including special room shapes and decorations.
/// </summary>
public class RoomGenerator
{
    private DungeonConfig config;
    private TileType[,] dungeonMap;
    private List<Room> rooms;
    private List<RoomDecoration> decorationsToPlace;
    private GameObject[] roomDecorations;
    private GameObject[] hazardPrefabs;

    public RoomGenerator(DungeonConfig config, TileType[,] dungeonMap, List<Room> rooms, List<RoomDecoration> decorationsToPlace)
    {
        this.config = config;
        this.dungeonMap = dungeonMap;
        this.rooms = rooms;
        this.decorationsToPlace = decorationsToPlace;
    }

    public void SetDecorationPrefabs(GameObject[] roomDecorations, GameObject[] hazardPrefabs)
    {
        this.roomDecorations = roomDecorations;
        this.hazardPrefabs = hazardPrefabs;
    }

    /// <summary>
    /// Generates a random room within a cell.
    /// </summary>
    public Room GenerateRoomInCell(int cellXIndex, int cellYIndex)
    {
        Room room = new Room();
        
        // Determine the cell's starting position in tile coordinates.
        int cellStartX = config.bufferX + cellXIndex * config.cellWidth;
        int cellStartY = config.bufferY + cellYIndex * config.cellHeight;

        // Randomize room size (ensuring it fits within the cell).
        int roomWidth = Random.Range(5, config.cellWidth - 1);
        int roomHeight = Random.Range(4, config.cellHeight - 1);

        // Randomly choose room shape if variety is enabled
        if (config.enableRoomVariety && Random.Range(0, 100) < config.specialShapedRoomChance)
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
                    CreateDecoratedRoom(room, cellStartX, cellStartY, roomWidth, roomHeight);
                    break;
            }
        }
        else
        {
            // Standard rectangular room
            int roomX = cellStartX + Random.Range(1, config.cellWidth - roomWidth);
            int roomY = cellStartY + Random.Range(1, config.cellHeight - roomHeight);
            room.x = roomX;
            room.y = roomY;
            room.width = roomWidth;
            room.height = roomHeight;
        }

        // Add the room to our list
        rooms.Add(room);
        return room;
    }

    /// <summary>
    /// Creates an L-shaped room by removing a rectangular section.
    /// </summary>
    private void CreateLShapedRoom(Room room, int cellStartX, int cellStartY, int width, int height)
    {
        // Randomize room position within the cell
        int roomX = cellStartX + Random.Range(1, config.cellWidth - width);
        int roomY = cellStartY + Random.Range(1, config.cellHeight - height);

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

    /// <summary>
    /// Creates a circular room by excluding corner sections.
    /// </summary>
    private void CreateCircularRoom(Room room, int cellStartX, int cellStartY, int width, int height)
    {
        // Make sure width and height are similar for a more circular shape
        if (Mathf.Abs(width - height) > 2)
        {
            int avg = (width + height) / 2;
            width = avg;
            height = avg;
        }

        // Randomize room position within the cell
        int roomX = cellStartX + Random.Range(1, config.cellWidth - width);
        int roomY = cellStartY + Random.Range(1, config.cellHeight - height);

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

    /// <summary>
    /// Creates a cross-shaped room by excluding corner sections.
    /// </summary>
    private void CreateCrossShapedRoom(Room room, int cellStartX, int cellStartY, int width, int height)
    {
        // Randomize room position within the cell
        int roomX = cellStartX + Random.Range(1, config.cellWidth - width);
        int roomY = cellStartY + Random.Range(1, config.cellHeight - height);

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

    /// <summary>
    /// Creates a decorated rectangular room with decorations and hazards.
    /// </summary>
    private void CreateDecoratedRoom(Room room, int cellStartX, int cellStartY, int width, int height)
    {
        // Standard rectangular room
        int roomX = cellStartX + Random.Range(1, config.cellWidth - width);
        int roomY = cellStartY + Random.Range(1, config.cellHeight - height);
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

    /// <summary>
    /// Carves out a room in the dungeonMap by marking its tiles as Floor.
    /// </summary>
    public void CarveRoom(Room room)
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

                    if (!inExcludedArea && x >= 0 && x < config.dungeonWidth && y >= 0 && y < config.dungeonHeight)
                    {
                        dungeonMap[x, y] = TileType.Floor;
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
                    if (x >= 0 && x < config.dungeonWidth && y >= 0 && y < config.dungeonHeight)
                    {
                        dungeonMap[x, y] = TileType.Floor;
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

    /// <summary>
    /// Gets a room at the specified position.
    /// </summary>
    public Room GetRoomAtPosition(int x, int y)
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

    /// <summary>
    /// Checks if a position is within any room.
    /// </summary>
    public bool IsInRoom(int x, int y)
    {
        return GetRoomAtPosition(x, y) != null;
    }
}
