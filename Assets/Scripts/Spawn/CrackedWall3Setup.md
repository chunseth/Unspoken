# CrackedWall3 Setup Guide

This guide explains how to set up the third cracked wall puzzle component (`CrackedWall3`) that will be placed in the dungeon alongside the CarryableObject. The component includes enemy spawning functionality and puzzle solving capabilities.

## Overview

The CrackedWall3 component:
- Spawns enemies when the player is nearby (within spawn distance)
- Provides a puzzle interface when clicked
- Can be solved by inputting the correct spell pattern
- Changes appearance when solved
- Includes spawn indicators and visual effects

## Setup Instructions

### 1. Create the Prefab

1. Create a new GameObject in your scene
2. Add a SpriteRenderer component with your cracked wall sprite
3. Add the CrackedWall3 script component
4. Configure the inspector fields (see Configuration section below)
5. Create a prefab from this GameObject

### 2. Assign to DungeonGenerator

1. In the DungeonGenerator inspector, find the "Dungeon Puzzle Prefabs" section
2. Assign your CrackedWall3 prefab to the "Cracked Wall 3 Prefab" field
3. The wall will automatically be placed in the same room as the CarryableObject

## Configuration

### UI References
- **Puzzle UI Panel**: The UI panel that appears when the wall is clicked
- **Background Image**: The background image for the puzzle UI
- **Back Button**: The button to close the puzzle
- **Spell Grid Controller**: Reference to the SpellBeamGridController in the UI

### Interaction Settings
- **Interaction Distance**: How close the player must be to interact (default: 2f)
- **Correct Spell Pattern**: The pattern to solve the puzzle (default: "036")

### Player Reference
- **Player**: Reference to the player GameObject (auto-found if tagged "Player")

### Visual Effects
- **Solved Wall Sprite**: Sprite to change to when puzzle is solved
- **Solve Effect**: Optional particle effect to play when puzzle is solved

### Enemy Spawning
- **Enemy Prefab**: Enemy prefab to spawn when player is nearby
- **Spawn Distance**: Distance at which enemies start spawning (default: 20f)
- **Spawn Interval**: Time between enemy spawns in seconds (default: 5f)
- **Max Enemies**: Maximum number of enemies that can be spawned (default: 10)
- **Spawn Indicator Duration**: Duration to show spawn indicator before spawning (default: 2f)
- **Show Spawn Indicators**: Whether to show spawn indicators (default: true)

## How It Works

### Enemy Spawning
1. Player approaches the wall (within spawn distance)
2. Enemies start spawning at regular intervals
3. Spawn indicators show where enemies will appear
4. Spawning stops when player leaves the area or puzzle is active

### Puzzle Interaction
1. Player clicks on the wall when in interaction range
2. Puzzle UI appears and game elements are frozen
3. Player must input the correct spell pattern
4. Wall changes appearance when solved
5. Puzzle UI closes and game resumes

### Spawn Indicators
- Visual indicators show where enemies will spawn
- Indicators travel from the wall to the spawn position
- Helps players anticipate enemy locations

## Integration with CarryableObject

The CrackedWall3 is designed to work with the CarryableObject in puzzle scenarios:
- Both objects are placed in the same room by the DungeonGenerator
- The carryable object can be used to solve puzzles or unlock the cracked wall
- Players must carry the object to specific locations or use it as a key

## Debug Features

### Validate References
Call the `ValidateReferences()` method to check if all required components are assigned.

### Test Interaction
Call the `TestInteraction()` method to debug interaction and spawning status.

### Manual Interaction
Call the `ManualInteraction()` method to manually trigger the puzzle interface.

## Troubleshooting

### Enemies Not Spawning
- Check that the enemy prefab is assigned
- Verify spawn distance is appropriate
- Ensure the player has the "Player" tag
- Check console logs for spawn-related messages

### Puzzle Not Working
- Verify all UI references are assigned
- Check that the SpellBeamGridController is properly set up
- Ensure the correct spell pattern is configured
- Check console logs for puzzle-related errors

### Visual Issues
- Verify the solved wall sprite is assigned
- Check that the wall has a SpriteRenderer component
- Ensure proper sorting layers are set up

### Spawn Indicators Not Working
- Check that "Show Spawn Indicators" is enabled
- Verify spawn indicator duration is set
- Ensure the SpawnIndicator component is working

## Example Usage

1. Player enters room with CrackedWall3 and CarryableObject
2. Enemies start spawning when player approaches the wall
3. Player picks up the CarryableObject
4. Player carries the object to the CrackedWall3
5. Player clicks on the wall to open the puzzle
6. Player solves the puzzle using the carried object
7. Wall changes appearance and enemies stop spawning

## Console Logs

The component provides detailed console logging for debugging:
- "CrackedWall3: Player entered spawn range" - When spawning begins
- "CrackedWall3: Spawned enemy X/Y at position" - When enemies spawn
- "CrackedWall3: Opening puzzle!" - When puzzle interface opens
- "CrackedWall3: Puzzle solved!" - When puzzle is completed

This creates an engaging puzzle mechanic that combines enemy spawning, object carrying, and puzzle solving for a dynamic gameplay experience.
