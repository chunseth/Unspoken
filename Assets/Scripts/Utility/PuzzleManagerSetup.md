# Puzzle Manager Setup Guide

## Overview
The PuzzleManager system provides centralized tracking of all puzzles in the game and enables the boss room lock feature. When all puzzles are solved, the boss room becomes accessible through dynamically generated corridors.

## Features
- **Automatic Puzzle Detection**: Finds all cracked wall puzzles in the scene
- **Puzzle Completion Tracking**: Monitors the solved state of all puzzles
- **Boss Room Locking**: Prevents corridors from intersecting with the boss room until all puzzles are solved
- **Dynamic Unlocking**: Automatically creates corridors to the boss room when all puzzles are completed
- **Event System**: Fires events when all puzzles are solved

## Setup Instructions

### 1. Add PuzzleManager to Scene
1. Create an empty GameObject in your scene
2. Name it "PuzzleManager"
3. Add the `PuzzleManager` script component to it
4. The script will automatically find all puzzle objects when the scene starts

### 2. Configure DungeonGenerator
1. In the DungeonGenerator component, find the "Puzzle Integration" section
2. Set `enablePuzzleBossRoomLock` to `true` to enable the feature
3. The DungeonGenerator will automatically find the PuzzleManager at runtime

### 3. Puzzle Object Requirements
All puzzle objects must implement the `IsPuzzleSolved()` method:
- **CrackedWall**: Returns `isPuzzleSolved` field
- **CrackedWall2**: Returns `isPuzzleSolved` field  
- **CrackedWall3**: Returns `isSealed` field (wall sealing puzzle)
- **CrackedWall4**: Returns `isPuzzleSolved` field

## How It Works

### Puzzle Detection
The PuzzleManager automatically finds all puzzle objects in the scene:
```csharp
// Finds all cracked wall types
CrackedWall[] walls = FindObjectsOfType<CrackedWall>();
CrackedWall2[] walls2 = FindObjectsOfType<CrackedWall2>();
CrackedWall3[] walls3 = FindObjectsOfType<CrackedWall3>();
CrackedWall4[] walls4 = FindObjectsOfType<CrackedWall4>();
```

### Boss Room Locking
During dungeon generation:
1. The DungeonGenerator checks if the boss room is locked via `IsBossRoomLocked()`
2. If locked, corridor generation skips areas near the boss room
3. Non-essential corridors are also prevented from approaching the boss room

### Dynamic Unlocking
When all puzzles are solved:
1. The PuzzleManager detects completion and fires `OnAllPuzzlesSolved` event
2. The DungeonGenerator's `CheckBossRoomUnlock()` method creates corridors to the boss room
3. Players can now access the boss room through the newly created corridors

## API Reference

### PuzzleManager Methods
- `AreAllPuzzlesSolved()`: Returns true if all puzzles are completed
- `GetTotalPuzzleCount()`: Returns the total number of puzzles in the scene
- `GetSolvedPuzzleCount()`: Returns the number of solved puzzles
- `FindAllPuzzles()`: Manually refresh the puzzle list
- `ResetPuzzleStatus()`: Reset completion status (for level restart)

### Events
- `OnAllPuzzlesSolved`: Fired when all puzzles are completed

### DungeonGenerator Integration
- `enablePuzzleBossRoomLock`: Enable/disable the boss room lock feature
- `IsBossRoomLocked()`: Check if boss room is currently locked
- `IsNearBossRoom(x, y)`: Check if a position is near the boss room
- `CreateBossRoomCorridors()`: Manually create corridors to boss room

## Troubleshooting

### PuzzleManager Not Found
- Ensure the PuzzleManager GameObject exists in the scene
- Check that the PuzzleManager script is attached
- Verify the GameObject is active in the hierarchy

### Boss Room Not Unlocking
- Check that `enablePuzzleBossRoomLock` is enabled in DungeonGenerator
- Verify all puzzle objects implement `IsPuzzleSolved()` correctly
- Check console for puzzle completion logs
- Ensure PuzzleManager is finding all puzzle objects

### Corridors Not Generating
- Verify that `IsBossRoomLocked()` returns false after puzzles are solved
- Check that `CreateBossRoomCorridors()` is being called
- Ensure there are valid rooms to connect the boss room to

### Performance Considerations
- The PuzzleManager checks puzzle status every frame in Update()
- For large numbers of puzzles, consider implementing a more efficient checking system
- The boss room unlock check only runs when the feature is enabled

## Example Usage

### Manual Puzzle Status Check
```csharp
PuzzleManager puzzleManager = FindObjectOfType<PuzzleManager>();
if (puzzleManager.AreAllPuzzlesSolved())
{
    Debug.Log("All puzzles solved! Boss room unlocked.");
}
```

### Subscribe to Completion Event
```csharp
void Start()
{
    PuzzleManager puzzleManager = FindObjectOfType<PuzzleManager>();
    if (puzzleManager != null)
    {
        puzzleManager.OnAllPuzzlesSolved += OnAllPuzzlesCompleted;
    }
}

void OnAllPuzzlesCompleted()
{
    Debug.Log("All puzzles completed! Time to face the boss!");
}
```

### Force Boss Room Unlock
```csharp
DungeonGenerator dungeonGenerator = FindObjectOfType<DungeonGenerator>();
if (dungeonGenerator != null)
{
    dungeonGenerator.CreateBossRoomCorridors();
}
```
