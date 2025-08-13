using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class PathCreatorBeam : MonoBehaviour, ISpecialAbility
{
    [Header("Ability Info")]
    public string abilityName = "Beam";
    public Sprite abilityIcon;
    public float cooldownTime = 10f;
    
    [Header("Beam Settings")]
    public float maxBeamDistance = 10f;
    public float beamWidth = 1f;
    public float beamDuration = 1.0f;
    public string wallLayerName = "Obstacle";
    [HideInInspector]
    public LayerMask wallLayer;
    
    [Header("Visual Effects")]
    public LineRenderer beamLineRenderer;
    public Color beamStartColor = Color.blue;
    public Color beamEndColor = Color.cyan;
    public float beamWidthStart = 0.5f;
    public float beamWidthEnd = 0.1f;
    public ParticleSystem hitEffect;
    
    private float lastActivationTime = -100f;
    private bool isCreatingPath = false;
    private DungeonGenerator dungeonGenerator;

    // ISpecialAbility implementation
    public string AbilityName => abilityName;
    public Sprite AbilityIcon => abilityIcon;
    public float Cooldown => cooldownTime;
    public bool IsOnCooldown => Time.time < lastActivationTime + cooldownTime;

    private void Awake()
    {
        // Set up the wall layer by name - more reliable than inspector reference
        wallLayer = LayerMask.GetMask(wallLayerName);
        
        Debug.Log($"PathCreatorBeam: Wall layer '{wallLayerName}' has mask value: {wallLayer.value}");
        
        if (wallLayer.value == 0)
        {
            Debug.LogError($"PathCreatorBeam: Failed to find layer named '{wallLayerName}'. Please check that this layer exists!");
        }
    }

    private void Start()
    {
        // Find the dungeon generator
        dungeonGenerator = FindObjectOfType<DungeonGenerator>();
        
        if (dungeonGenerator == null)
        {
            Debug.LogError("PathCreatorBeam: No DungeonGenerator found in the scene!");
        }
        
        // Setup line renderer if not assigned
        if (beamLineRenderer == null)
        {
            beamLineRenderer = gameObject.AddComponent<LineRenderer>();
            beamLineRenderer.startWidth = beamWidthStart;
            beamLineRenderer.endWidth = beamWidthEnd;
            beamLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            beamLineRenderer.startColor = beamStartColor;
            beamLineRenderer.endColor = beamEndColor;
            beamLineRenderer.positionCount = 2;
            beamLineRenderer.enabled = false;
        }
        
        // Debug setup info
        Debug.Log($"PathCreatorBeam: Initialized with dungeonGenerator={dungeonGenerator != null}");
    }

    public void ActivateAbility()
    {
        if (IsOnCooldown || isCreatingPath)
        {
            Debug.Log("PathCreatorBeam: On cooldown or already creating path");
            return;
        }
        
        if (dungeonGenerator == null)
        {
            Debug.LogError("PathCreatorBeam: No DungeonGenerator reference!");
            return;
        }
        
        Debug.Log("PathCreatorBeam: Activating ability");
        StartCoroutine(CreatePathCoroutine());
        lastActivationTime = Time.time;
    }
    
    private IEnumerator CreatePathCoroutine()
    {
        isCreatingPath = true;
        Debug.Log("PathCreatorBeam: Starting path creation");
        
        // Get the direction the player is facing using PlayerController
        Vector3 direction;
        PlayerController playerController = GetComponent<PlayerController>();
        
        if (playerController != null)
        {
            // Use the facing direction from the PlayerController
            Vector2 facingDir = playerController.GetFacingDirectionVector();
            direction = new Vector3(facingDir.x, facingDir.y, 0);
            Debug.Log($"PathCreatorBeam: Using player facing direction: {direction}");
        }
        else
        {
            // Fallback to camera direction if no PlayerController found
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Use camera forward projected onto XY plane for top-down games
                Vector3 camForward = mainCamera.transform.forward;
                camForward.z = 0;
                direction = camForward.normalized;
                Debug.Log($"PathCreatorBeam: Using camera direction: {direction}");
            }
            else
            {
                direction = transform.forward;
                Debug.Log($"PathCreatorBeam: Using transform direction: {direction}");
            }
        }
        
        // Normalize direction to match the path creation logic
        Vector2Int gridDirection = new Vector2Int(
            Mathf.RoundToInt(direction.x),
            Mathf.RoundToInt(direction.y)
        );
        
        // Handle diagonal directions by defaulting to horizontal (same logic as in CreatePathInDungeon)
        if (gridDirection.x != 0 && gridDirection.y != 0)
        {
            // If diagonal, prioritize horizontal movement (x axis)
            gridDirection.y = 0;
            gridDirection.x = gridDirection.x > 0 ? 1 : -1;
            direction = new Vector3(gridDirection.x, gridDirection.y, 0);
            Debug.Log($"PathCreatorBeam: Diagonal detected. Normalized direction to: {direction}");
        }
        // If direction is zero (e.g., looking straight up), default to right
        else if (gridDirection.x == 0 && gridDirection.y == 0)
        {
            direction = Vector3.right;
            Debug.Log($"PathCreatorBeam: Zero direction detected. Defaulting to right");
        }
        // Normalize to ensure we move by 1 in the non-zero direction
        else
        {
            if (gridDirection.x != 0)
            {
                gridDirection.x = gridDirection.x > 0 ? 1 : -1;
                gridDirection.y = 0;
            }
            else if (gridDirection.y != 0)
            {
                gridDirection.y = gridDirection.y > 0 ? 1 : -1;
                gridDirection.x = 0;
            }
            direction = new Vector3(gridDirection.x, gridDirection.y, 0);
        }
        
        Vector3 startPosition = transform.position;
        
        // Enable line renderer
        beamLineRenderer.enabled = true;
        beamLineRenderer.SetPosition(0, startPosition);
        
        // Calculate the target position
        RaycastHit hit;
        Vector3 endPosition;
        
        Debug.Log($"PathCreatorBeam: Casting ray from {startPosition} in direction {direction} with max distance {maxBeamDistance}");
        Debug.Log($"PathCreatorBeam: Wall layer mask value: {wallLayer.value}");
        
        bool hitSomething = Physics.Raycast(startPosition, direction, out hit, maxBeamDistance, wallLayer);
        
        if (hitSomething)
        {
            endPosition = hit.point;
            Debug.Log($"PathCreatorBeam: Hit something at {endPosition}, collider: {hit.collider.name}");
            
            // Instantiate hit effect
            if (hitEffect != null)
            {
                Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
        else
        {
            endPosition = startPosition + direction * maxBeamDistance;
            Debug.Log($"PathCreatorBeam: No hit, using end position {endPosition}");
        }
        
        beamLineRenderer.SetPosition(1, endPosition);
        
        // Wait a moment to show the beam
        float elapsedTime = 0f;
        while (elapsedTime < beamDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Update beam for visual effect
            float intensityFactor = 1 - (elapsedTime / beamDuration);
            beamLineRenderer.startWidth = beamWidthStart * intensityFactor;
            beamLineRenderer.endWidth = beamWidthEnd * intensityFactor;
            
            yield return null;
        }
        
        // Create the path by modifying dungeon tiles
        CreatePathInDungeon(startPosition, endPosition, direction);
        
        // Disable line renderer
        beamLineRenderer.enabled = false;
        isCreatingPath = false;
    }
    
    private void CreatePathInDungeon(Vector3 start, Vector3 end, Vector3 direction)
    {
        if (dungeonGenerator == null)
        {
            Debug.LogError("PathCreatorBeam: dungeonGenerator is null in CreatePathInDungeon");
            return;
        }
        
        Debug.Log($"PathCreatorBeam: Creating path from {start} to {end}");
        
        // Convert world positions to grid coordinates
        int startX = Mathf.RoundToInt(start.x);
        int startY = Mathf.RoundToInt(start.y);
        
        Debug.Log($"PathCreatorBeam: Grid start position: ({startX}, {startY})");
        
        // Normalize direction to grid (x,y)
        Vector2Int gridDirection = new Vector2Int(
            Mathf.RoundToInt(direction.x),
            Mathf.RoundToInt(direction.y)
        );
        
        Debug.Log($"PathCreatorBeam: Raw grid direction: {gridDirection}");
        
        // MODIFIED: Handle diagonal directions by defaulting to horizontal
        if (gridDirection.x != 0 && gridDirection.y != 0)
        {
            // If diagonal, prioritize horizontal movement (x axis)
            // For NE or SE, use East. For NW or SW, use West.
            gridDirection.y = 0;
            gridDirection.x = gridDirection.x > 0 ? 1 : -1;
            Debug.Log($"PathCreatorBeam: Diagonal detected. Defaulting to horizontal: {gridDirection}");
        }
        // If direction is zero (e.g., looking straight up), default to right
        else if (gridDirection.x == 0 && gridDirection.y == 0)
        {
            gridDirection.x = 1;
            Debug.Log($"PathCreatorBeam: Zero direction detected. Defaulting to right");
        }
        // Normalize to ensure we move by 1 in the non-zero direction
        else
        {
            if (gridDirection.x != 0)
            {
                gridDirection.x = gridDirection.x > 0 ? 1 : -1;
                gridDirection.y = 0;
            }
            else if (gridDirection.y != 0)
            {
                gridDirection.y = gridDirection.y > 0 ? 1 : -1;
            }
        }
        
        Debug.Log($"PathCreatorBeam: Normalized grid direction: {gridDirection}");
        
        // Create the path (up to 10 tiles or until we hit the edge of the map)
        List<Vector2Int> pathTiles = new List<Vector2Int>();
        for (int i = 0; i < maxBeamDistance; i++)
        {
            int x = startX + gridDirection.x * i;
            int y = startY + gridDirection.y * i;
            
            // Check if we're still within the dungeon bounds
            if (x < 0 || x >= dungeonGenerator.dungeonWidth || 
                y < 0 || y >= dungeonGenerator.dungeonHeight)
            {
                Debug.Log($"PathCreatorBeam: Reached dungeon boundary at ({x}, {y})");
                break;
            }
            
            pathTiles.Add(new Vector2Int(x, y));
        }
        
        Debug.Log($"PathCreatorBeam: Created path with {pathTiles.Count} tiles");
        
        // First, destroy any walls in the path
        foreach (Vector2Int tile in pathTiles)
        {
            TileType currentType = GetTileType(tile.x, tile.y);
            Debug.Log($"PathCreatorBeam: Tile at ({tile.x}, {tile.y}) is type: {currentType}");
            
            // If it's any kind of wall, destroy the gameobject
            if (IsWallTile(currentType))
            {
                Debug.Log($"PathCreatorBeam: Destroying wall at ({tile.x}, {tile.y})");
                DestroyWallAt(tile.x, tile.y);
            }
            
            // Set the tile to floor in the dungeon map
            dungeonGenerator.dungeonMap[tile.x, tile.y] = TileType.Floor;
            
            // Instantiate a floor prefab
            Vector3 pos = new Vector3(tile.x, tile.y, 0);
            Instantiate(dungeonGenerator.floorPrefab, pos, Quaternion.identity, dungeonGenerator.dungeonParent);
            Debug.Log($"PathCreatorBeam: Floor created at ({pos.x}, {pos.y}, {pos.z})");
        }
        
        // Now add walls around the new floor tiles
        AddWallsAroundPath(pathTiles);
    }
    
    // Added this method since GetTileType might not exist in DungeonGenerator
    private TileType GetTileType(int x, int y)
    {
        if (x < 0 || x >= dungeonGenerator.dungeonWidth ||
            y < 0 || y >= dungeonGenerator.dungeonHeight)
        {
            return TileType.Void;
        }
        
        return dungeonGenerator.dungeonMap[x, y];
    }
    
    private bool IsWallTile(TileType type)
    {
        return type == TileType.WallLeft || type == TileType.WallRight ||
               type == TileType.WallTop || type == TileType.WallBottom ||
               type == TileType.WallCornerTopLeft || type == TileType.WallCornerTopRight ||
               type == TileType.WallCornerBottomLeft || type == TileType.WallCornerBottomRight;
    }
    
    private void DestroyWallAt(int x, int y)
    {
        Vector3 wallPos = new Vector3(x, y, 0);
        Debug.Log($"PathCreatorBeam: Attempting to destroy walls at position {wallPos}");

        // Check for 3D colliders first
        Collider[] walls3D = Physics.OverlapBox(
            wallPos,
            new Vector3(0.6f, 0.6f, 0.6f), // Increased size to catch walls better
            Quaternion.identity,
            wallLayer
        );
        
        if (walls3D.Length > 0)
        {
            Debug.Log($"PathCreatorBeam: Found {walls3D.Length} 3D wall colliders");
            foreach (Collider wall in walls3D)
            {
                Debug.Log($"PathCreatorBeam: Destroying wall: {wall.gameObject.name} at {wall.transform.position}");
                Destroy(wall.gameObject);
            }
        }
        else
        {
            Debug.Log("PathCreatorBeam: No 3D wall colliders found, checking for 2D colliders");
            
            // Check for 2D colliders if no 3D colliders were found
            Collider2D[] walls2D = Physics2D.OverlapBoxAll(
                new Vector2(wallPos.x, wallPos.y),
                new Vector2(0.6f, 0.6f),
                0f,
                wallLayer
            );
            
            Debug.Log($"PathCreatorBeam: Found {walls2D.Length} 2D wall colliders");
            foreach (Collider2D wall in walls2D)
            {
                Debug.Log($"PathCreatorBeam: Destroying wall: {wall.gameObject.name} at {wall.transform.position}");
                Destroy(wall.gameObject);
            }
        }
        
        // As a fallback, try to find walls by tag
        GameObject[] taggedWalls = GameObject.FindGameObjectsWithTag("Wall");
        foreach (GameObject wall in taggedWalls)
        {
            // Check if this wall is close to our target position
            if (Vector3.Distance(wall.transform.position, wallPos) < 0.7f)
            {
                Debug.Log($"PathCreatorBeam: Destroying wall by tag: {wall.name} at {wall.transform.position}");
                Destroy(wall.gameObject);
            }
        }
    }
    
    private void AddWallsAroundPath(List<Vector2Int> pathTiles)
    {
        foreach (Vector2Int tile in pathTiles)
        {
            // Check all 8 adjacent tiles
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue; // Skip the center tile
                    
                    int nx = tile.x + dx;
                    int ny = tile.y + dy;
                    
                    // Check if the adjacent tile is within bounds
                    if (nx < 0 || nx >= dungeonGenerator.dungeonWidth ||
                        ny < 0 || ny >= dungeonGenerator.dungeonHeight)
                        continue;
                    
                    // If the adjacent tile is void, add a wall
                    if (dungeonGenerator.dungeonMap[nx, ny] == TileType.Void)
                    {
                        // Determine wall type based on relative position
                        TileType wallType = DetermineWallType(dx, dy);
                        dungeonGenerator.dungeonMap[nx, ny] = wallType;
                        
                        // FIXED: Use the correct coordinate system (x, y, 0) instead of (x, 0, y)
                        Vector3 pos = new Vector3(nx, ny, 0);
                        GameObject wallPrefab = GetWallPrefab(wallType);
                        if (wallPrefab != null)
                        {
                            Instantiate(wallPrefab, pos, Quaternion.identity, dungeonGenerator.dungeonParent);
                            Debug.Log($"PathCreatorBeam: Wall of type {wallType} created at ({pos.x}, {pos.y}, {pos.z})");
                        }
                    }
                }
            }
        }
    }
    
    private TileType DetermineWallType(int dx, int dy)
    {
        // Determine wall type based on the relative position
        if (dx == 0 && dy == 1) return TileType.WallTop;
        if (dx == 0 && dy == -1) return TileType.WallBottom;
        if (dx == 1 && dy == 0) return TileType.WallRight;
        if (dx == -1 && dy == 0) return TileType.WallLeft;
        
        // Corner cases
        if (dx == 1 && dy == 1) return TileType.WallCornerTopRight;
        if (dx == -1 && dy == 1) return TileType.WallCornerTopLeft;
        if (dx == 1 && dy == -1) return TileType.WallCornerBottomRight;
        if (dx == -1 && dy == -1) return TileType.WallCornerBottomLeft;
        
        return TileType.WallLeft; // Default fallback
    }
    
    private GameObject GetWallPrefab(TileType wallType)
    {
        switch (wallType)
        {
            case TileType.WallTop: return dungeonGenerator.wallTopPrefab;
            case TileType.WallBottom: return dungeonGenerator.wallBottomPrefab;
            case TileType.WallLeft: return dungeonGenerator.wallLeftPrefab;
            case TileType.WallRight: return dungeonGenerator.wallRightPrefab;
            case TileType.WallCornerTopLeft:
            case TileType.WallCornerTopRight:
            case TileType.WallCornerBottomLeft:
            case TileType.WallCornerBottomRight:
                return dungeonGenerator.wallPrefab;
            default: return dungeonGenerator.wallPrefab;
        }
    }
} 