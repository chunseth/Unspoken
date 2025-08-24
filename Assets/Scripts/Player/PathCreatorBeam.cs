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
        Vector3 endPosition = FindBeamEndPosition(startPosition, direction);
        
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
        
        // Create the path (up to the end position or until we hit the edge of the map or a boss room barrier)
        List<Vector2Int> pathTiles = new List<Vector2Int>();
        
        // Calculate how many tiles we should create based on the end position
        int endX = Mathf.RoundToInt(end.x);
        int endY = Mathf.RoundToInt(end.y);
        
        // Calculate the distance from start to end
        int distanceX = Mathf.Abs(endX - startX);
        int distanceY = Mathf.Abs(endY - startY);
        int maxTiles = Mathf.Max(distanceX, distanceY);
        
        // Limit to maxBeamDistance
        maxTiles = Mathf.Min(maxTiles, (int)maxBeamDistance);
        
        for (int i = 0; i <= maxTiles; i++)
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
            
            // Check if we've reached the end position
            if (x == endX && y == endY)
            {
                Debug.Log($"PathCreatorBeam: Reached end position at ({x}, {y})");
                break;
            }
            
            // NEW: Check if this position has a boss room barrier
            if (HasBossRoomBarrierAt(x, y))
            {
                Debug.Log($"PathCreatorBeam: Hit boss room barrier at ({x}, {y}), stopping path creation");
                break;
            }
            
            // NEW: Check if this position is a hole tile
            TileType currentTileType = GetTileType(x, y);
            if (currentTileType == TileType.Hole)
            {
                Debug.Log($"PathCreatorBeam: Hit hole tile at ({x}, {y}), stopping path creation");
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

    // NEW: Check if there's a boss room barrier at the specified position
    private bool HasBossRoomBarrierAt(int x, int y)
    {
        // Check if the dungeon generator has boss room barriers enabled
        if (dungeonGenerator == null || !dungeonGenerator.enablePuzzleBossRoomLock || !dungeonGenerator.enableBossRoom)
        {
            return false;
        }
        
        // Check if the boss room is currently locked (which means barriers should exist)
        if (!dungeonGenerator.IsBossRoomLocked())
        {
            return false;
        }
        
        // Check for boss room barrier GameObjects at this position
        Vector3 checkPos = new Vector3(x, y, 0);
        
        // Check for 3D colliders (barriers might be at different Z positions)
        Collider[] barriers3D = Physics.OverlapBox(
            checkPos,
            new Vector3(0.6f, 0.6f, 10f), // Check a wide Z range to catch barriers at different depths
            Quaternion.identity
        );
        
        foreach (Collider barrier in barriers3D)
        {
            // Check if this is a boss room barrier by checking if it's in the barrier objects list
            if (IsBossRoomBarrierObject(barrier.gameObject))
            {
                Debug.Log($"PathCreatorBeam: Found boss room barrier at ({x}, {y}): {barrier.gameObject.name}");
                return true;
            }
        }
        
        // Check for 2D colliders as well
        Collider2D[] barriers2D = Physics2D.OverlapBoxAll(
            new Vector2(x, y),
            new Vector2(0.6f, 0.6f),
            0f
        );
        
        foreach (Collider2D barrier in barriers2D)
        {
            // Check if this is a boss room barrier
            if (IsBossRoomBarrierObject(barrier.gameObject))
            {
                Debug.Log($"PathCreatorBeam: Found boss room barrier (2D) at ({x}, {y}): {barrier.gameObject.name}");
                return true;
            }
        }
        
        return false;
    }
    
    // NEW: Helper method to check if a GameObject is a boss room barrier
    private bool IsBossRoomBarrierObject(GameObject obj)
    {
        // Method 1: Check if this object is in the DungeonGenerator's bossRoomBarrierObjects list
        // This is the most reliable method since it directly checks the list of barrier objects
        if (dungeonGenerator != null && dungeonGenerator.bossRoomBarrierObjects.Contains(obj))
        {
            return true;
        }
        
        // Method 2: Check if this object is a child of the dungeon parent (like barriers are)
        if (dungeonGenerator.dungeonParent != null && obj.transform.IsChildOf(dungeonGenerator.dungeonParent))
        {
            // Check if the object has the characteristics of a boss room barrier
            // Boss room barriers typically have Collider2D and are placed at specific Z positions
            Collider2D collider = obj.GetComponent<Collider2D>();
            if (collider != null)
            {
                // Check if the object is positioned at a Z coordinate that suggests it's a barrier
                // Barriers are typically placed at Z = -5f or Z = 1f based on the CreateBarrierAtPosition method
                float zPos = obj.transform.position.z;
                if (zPos == -5f || zPos == 1f)
                {
                    // Additional check: see if this object matches the barrier prefab
                    if (dungeonGenerator.bossRoomBarrierPrefab != null)
                    {
                        // Compare the object's name or components with the barrier prefab
                        if (obj.name.Contains(dungeonGenerator.bossRoomBarrierPrefab.name) ||
                            obj.GetComponents<Component>().Length == dungeonGenerator.bossRoomBarrierPrefab.GetComponents<Component>().Length)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        
        // Method 3: Check by name or tag (fallback)
        if (obj.name.Contains("BossRoomBarrier") || obj.CompareTag("BossRoomBarrier"))
        {
            return true;
        }
        
        return false;
    }

    // NEW: Method to find where the beam should end, checking for both walls and boss room barriers
    private Vector3 FindBeamEndPosition(Vector3 startPosition, Vector3 direction)
    {
        Vector3 endPosition = startPosition + direction * maxBeamDistance; // Default to max distance
        bool hitSomething = false;
        Vector3 hitPoint = endPosition;
        GameObject hitObject = null;
        
        Debug.Log($"PathCreatorBeam: Checking for obstacles from {startPosition} in direction {direction}");
        
        // DEBUG: Log all boss room barriers that exist
        if (dungeonGenerator != null && dungeonGenerator.bossRoomBarrierObjects.Count > 0)
        {
            Debug.Log($"PathCreatorBeam: Found {dungeonGenerator.bossRoomBarrierObjects.Count} boss room barriers in scene");
            foreach (GameObject barrier in dungeonGenerator.bossRoomBarrierObjects)
            {
                if (barrier != null)
                {
                    Debug.Log($"PathCreatorBeam: Barrier at position {barrier.transform.position}, name: {barrier.name}");
                }
            }
        }
        else
        {
            Debug.Log("PathCreatorBeam: No boss room barriers found in scene");
        }
        
        // First, check for regular walls using the wall layer
        RaycastHit wallHit;
        bool hitWall = Physics.Raycast(startPosition, direction, out wallHit, maxBeamDistance, wallLayer);
        
        if (hitWall)
        {
            hitSomething = true;
            hitPoint = wallHit.point;
            hitObject = wallHit.collider.gameObject;
            Debug.Log($"PathCreatorBeam: Hit wall at {hitPoint}, collider: {hitObject.name}");
        }
        
        // Now check for boss room barriers by doing a broader raycast
        // We need to check for barriers that might not be on the wall layer
        RaycastHit[] allHits = Physics.RaycastAll(startPosition, direction, maxBeamDistance);
        
        foreach (RaycastHit hit in allHits)
        {
            // Skip if this is the same as the wall hit we already found
            if (hitWall && hit.collider == wallHit.collider)
                continue;
                
            // Check if this is a boss room barrier
            if (IsBossRoomBarrierObject(hit.collider.gameObject))
            {
                // If we found a barrier closer than the wall (or no wall was hit), use the barrier
                if (!hitSomething || Vector3.Distance(startPosition, hit.point) < Vector3.Distance(startPosition, hitPoint))
                {
                    hitSomething = true;
                    hitPoint = hit.point;
                    hitObject = hit.collider.gameObject;
                    Debug.Log($"PathCreatorBeam: Hit boss room barrier at {hitPoint}, collider: {hitObject.name}");
                }
            }
        }
        
        // NEW: Also check for 2D colliders (boss room barriers might be 2D)
        RaycastHit2D[] allHits2D = Physics2D.RaycastAll(startPosition, direction, maxBeamDistance);
        
        foreach (RaycastHit2D hit2D in allHits2D)
        {
            // Check if this is a boss room barrier
            if (IsBossRoomBarrierObject(hit2D.collider.gameObject))
            {
                Vector3 hitPoint2D = new Vector3(hit2D.point.x, hit2D.point.y, startPosition.z);
                
                // If we found a barrier closer than what we have (or no obstacle was hit), use the barrier
                if (!hitSomething || Vector3.Distance(startPosition, hitPoint2D) < Vector3.Distance(startPosition, hitPoint))
                {
                    hitSomething = true;
                    hitPoint = hitPoint2D;
                    hitObject = hit2D.collider.gameObject;
                    Debug.Log($"PathCreatorBeam: Hit boss room barrier (2D) at {hitPoint}, collider: {hitObject.name}");
                }
            }
        }
        
        // Set the end position to the closest obstacle
        if (hitSomething)
        {
            endPosition = hitPoint;
            
            // Instantiate hit effect if we have one
            if (hitEffect != null)
            {
                Instantiate(hitEffect, hitPoint, Quaternion.LookRotation(direction));
            }
        }
        else
        {
            Debug.Log($"PathCreatorBeam: No obstacles found, using max distance end position {endPosition}");
        }
        
        // DEBUG: Direct check for barriers in beam path
        if (dungeonGenerator != null && dungeonGenerator.bossRoomBarrierObjects.Count > 0)
        {
            Vector3 beamEnd = startPosition + direction * maxBeamDistance;
            foreach (GameObject barrier in dungeonGenerator.bossRoomBarrierObjects)
            {
                if (barrier != null)
                {
                    // Check if barrier is in the beam's path
                    Vector3 barrierPos = barrier.transform.position;
                    Vector3 toBarrier = barrierPos - startPosition;
                    float projection = Vector3.Dot(toBarrier, direction);
                    
                    if (projection > 0 && projection <= maxBeamDistance)
                    {
                        // Barrier is in front of the beam, check if it's close to the beam line
                        Vector3 projectedPoint = startPosition + direction * projection;
                        float distanceToBeam = Vector3.Distance(barrierPos, projectedPoint);
                        
                        if (distanceToBeam < 1.0f) // Within 1 unit of the beam line
                        {
                            Debug.Log($"PathCreatorBeam: Found barrier in beam path at {barrierPos}, distance to beam: {distanceToBeam}");
                            // If this barrier is closer than our current end position, use it
                            if (!hitSomething || projection < Vector3.Distance(startPosition, endPosition))
                            {
                                endPosition = projectedPoint;
                                hitSomething = true;
                                Debug.Log($"PathCreatorBeam: Adjusted end position to barrier at {endPosition}");
                            }
                        }
                    }
                }
            }
        }
        
        // NEW: Check for hole tiles in the beam path
        if (dungeonGenerator != null)
        {
            Vector3 beamEnd = startPosition + direction * maxBeamDistance;
            float stepSize = 0.5f; // Check every 0.5 units along the beam path
            
            for (float distance = 0; distance <= maxBeamDistance; distance += stepSize)
            {
                Vector3 checkPoint = startPosition + direction * distance;
                int gridX = Mathf.RoundToInt(checkPoint.x);
                int gridY = Mathf.RoundToInt(checkPoint.y);
                
                // Check if this grid position is a hole
                if (gridX >= 0 && gridX < dungeonGenerator.dungeonWidth && 
                    gridY >= 0 && gridY < dungeonGenerator.dungeonHeight)
                {
                    TileType tileType = dungeonGenerator.dungeonMap[gridX, gridY];
                    if (tileType == TileType.Hole)
                    {
                        Debug.Log($"PathCreatorBeam: Found hole tile at grid position ({gridX}, {gridY}), stopping beam at {checkPoint}");
                        // If this hole is closer than our current end position, use it
                        if (!hitSomething || distance < Vector3.Distance(startPosition, endPosition))
                        {
                            endPosition = checkPoint;
                            hitSomething = true;
                            Debug.Log($"PathCreatorBeam: Adjusted end position to hole at {endPosition}");
                        }
                        break; // Stop checking once we find the first hole
                    }
                }
            }
        }
        
        return endPosition;
    }
} 