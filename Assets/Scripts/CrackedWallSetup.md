# Cracked Wall Puzzle Setup Guide

## Overview
The cracked wall puzzle system consists of:
1. A unique cracked wall that appears in the boss room
2. A UI overlay that appears when the wall is clicked
3. A spell grid for inputting the correct pattern (036)
4. Interaction system that pauses gameplay during the puzzle

## Setup Instructions

### 1. DungeonGenerator Setup
- Add the `crackedWallPrefab` reference in the "Boss Room Special Prefabs" section
- The cracked wall will automatically be placed in the boss room during generation

### 2. Cracked Wall Prefab Setup
Create a prefab for the cracked wall with:
- Visual representation of a cracked wall (SpriteRenderer or Image component)
- Add the `CrackedWall` script component
- Ensure it has a Collider2D for mouse interaction
- Set the `interactionDistance` (default: 2f)
- Assign a `solvedWallSprite` (normal wall sprite) for visual transformation
- Optionally assign a `solveEffect` prefab for particle effects
- Optionally assign an `enemyPrefab` for enemy spawning
- Configure enemy spawning settings (distance, interval, max count)

**IMPORTANT**: The UI references (puzzleUIPanel, backButton, spellGridController) should NOT be assigned in the prefab. These will be found automatically at runtime or assigned in the scene instance.

### 3. UI Setup
Create a UI panel for the puzzle with:

#### Main Panel (puzzleUIPanel)
- Full-screen overlay with semi-transparent background
- Contains all puzzle UI elements

#### Background Image (backgroundImage) - Optional
- Image component that serves as the puzzle background
- Can be a decorative image, pattern, or solid color
- Will appear/disappear with the puzzle panel
- Recommended to use a semi-transparent image for overlay effect

#### Back Button (backButton)
- Button to close the puzzle without solving
- Should be clearly visible and accessible

#### Spell Grid Container
- Add a `SpellGridDragController` component
- Set up the 3x3 grid as described in the SpellGridDragController documentation
- Ensure the `uiLinePrefab` is assigned
- Set `innerGridContainer` to the grid's RectTransform

### 4. CrackedWall Script Configuration
In the CrackedWall component, assign:
- `puzzleUIPanel`: Reference to the main UI panel (can be left null - will auto-find)
- `backgroundImage`: Reference to the background image (optional - can be left null - will auto-find)
- `backButton`: Reference to the back button (can be left null - will auto-find)
- `spellGridController`: Reference to the SpellGridDragController (can be left null - will auto-find)
- `player`: Reference to the player GameObject (or leave null to auto-find)
- `correctSpellPattern`: Set to "036" (default)
- `interactionDistance`: Set to desired interaction range
- `solvedWallSprite`: Sprite to change to when puzzle is solved (optional)
- `solveEffect`: Particle effect prefab to play when solved (optional)
- `enemyPrefab`: Enemy prefab to spawn when player is nearby (optional)
- `spawnDistance`: Distance at which enemies start spawning (default: 20)
- `spawnInterval`: Time between enemy spawns in seconds (default: 5)
- `maxEnemies`: Maximum number of enemies that can be spawned (default: 10)

**Note**: The script will automatically find UI elements with these names:
- "PuzzleUIPanel" for the main UI panel
- "PuzzleBackground" for the background image
- "BackButton" for the back button
- "SpellGrid" for the spell grid controller

**Debugging**: Right-click on the CrackedWall component in the inspector and use:
- "Validate References" to check current assignments
- "Find UI Elements" to search for UI elements in the scene

### 5. Player Setup
- Ensure the player GameObject has the "Player" tag
- The player should have a `PlayerController` component for movement control

## How It Works

1. **Wall Placement**: The cracked wall is automatically placed on a random wall segment in the boss room during dungeon generation.

2. **Enemy Spawning**: When the player is within `spawnDistance` (20 units), enemies spawn every `spawnInterval` (5 seconds) up to `maxEnemies` (10 total).

3. **Interaction**: When the player is within `interactionDistance` of the wall and clicks on it, the puzzle UI appears.

4. **Game Pause**: During the puzzle, player movement and other game interactions are disabled.

5. **Spell Input**: The player can drag on the 3x3 spell grid to create patterns.

6. **Pattern Recognition**: The system recognizes the pattern "036" as the correct solution.

7. **Puzzle Completion**: When the correct pattern is input, the puzzle is solved and gameplay resumes.

8. **Enemy Spawning Stops**: When the puzzle is solved, enemy spawning stops immediately.

9. **Visual Transformation**: The cracked wall changes to a normal wall sprite when solved.

10. **Back Button**: Players can click the back button to close the puzzle without solving it.

## Customization

- **Pattern**: Change `correctSpellPattern` to any other valid pattern
- **Visual Feedback**: 
  - Assign `solvedWallSprite` to change the wall's appearance when solved
  - Assign `solveEffect` to play particle effects when solved
  - Modify the `SolvePuzzle()` method for additional visual effects
- **Interaction Range**: Adjust `interactionDistance` for different interaction ranges
- **UI Styling**: Customize the UI panel appearance and layout
- **Wall Transformation**: The wall automatically changes from cracked to normal appearance
- **Enemy Spawning**:
  - Adjust `spawnDistance` to change when enemies start spawning
  - Modify `spawnInterval` to change spawn frequency
  - Set `maxEnemies` to limit total enemies spawned
  - Assign different `enemyPrefab` for different enemy types

## Troubleshooting

- **Wall not appearing**: Ensure `crackedWallPrefab` is assigned in DungeonGenerator
- **No interaction**: Check that the wall has a Collider2D and the player is within range
- **UI not showing**: 
  - Use "Validate References" context menu to check assignments
  - Use "Find UI Elements" context menu to locate UI components
  - Ensure UI elements have the correct names ("PuzzleUIPanel", "BackButton", "SpellGrid")
- **Pattern not recognized**: Ensure the SpellGridDragController is properly configured
- **Prefab losing references**: This is normal - UI references should be assigned in the scene, not the prefab
- **Missing references in prefab**: The script will auto-find UI elements at runtime if they have the correct names 