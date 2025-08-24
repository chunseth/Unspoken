# CrackedWall4 Setup Guide

## Overview
CrackedWall4 is a special wall component that spawns in the special room platform. It provides a puzzle interaction similar to CrackedWall1, with a UI overlay and spell grid that players must solve to progress.

## Features
- **Puzzle Interaction**: Click to open a spell grid puzzle when player is nearby
- **Spell Grid**: Interactive spell pattern input system
- **Visual Feedback**: Changes appearance when puzzle is solved
- **Game Freezing**: Freezes player and enemies during puzzle solving
- **UI Integration**: Seamless integration with puzzle UI system

## Setup Instructions

### 1. Create the Prefab
1. Create a new GameObject in your scene
2. Add a SpriteRenderer component
3. Assign a cracked wall sprite to the SpriteRenderer
4. Add the CrackedWall4 script component
5. Configure the settings (see Configuration section below)
6. Create a prefab from this GameObject

### 2. Assign to DungeonGenerator
1. Select your DungeonGenerator GameObject
2. In the inspector, find the "Dungeon Puzzle Prefabs" section
3. Assign your CrackedWall4 prefab to the "Cracked Wall 4 Prefab" field

### 3. Configuration

#### UI References
- **Puzzle UI Panel**: The UI panel that appears when the wall is clicked
- **Background Image**: The background image for the puzzle UI
- **Back Button**: The back button to close the puzzle
- **Spell Grid Controller**: Reference to the SpellGridDragController in the UI

#### Interaction Settings
- **Interaction Distance**: Distance the player must be within to interact (default: 2f)
- **Correct Spell Pattern**: The correct spell pattern to solve the puzzle (default: "036")

#### Player Reference
- **Player**: Reference to the player GameObject (auto-assigned if player has "Player" tag)

#### Visual Effects
- **Solved Wall Sprite**: Sprite to display when the puzzle is solved (normal wall appearance)
- **Solve Effect**: Optional particle effect to play when puzzle is solved

## Behavior

### Puzzle Interaction
- When the player is within interaction distance, the wall becomes clickable
- Clicking the wall opens the puzzle UI overlay
- The spell grid is initialized and ready for input
- Player and enemies are frozen during puzzle solving

### Spell Grid Puzzle
- Players must input the correct spell pattern ("036" by default)
- The puzzle uses the same SpellGridDragController as other cracked walls
- Incorrect patterns allow players to try again
- Correct patterns solve the puzzle and change the wall appearance

### Visual Feedback
- The wall sprite changes to indicate solved state
- Optional particle effects can play when solved
- Puzzle UI is automatically closed when solved

### Game State Management
- During puzzle solving, player movement is disabled
- All enemies are frozen in place
- Projectiles are stopped
- Game state is restored when puzzle is closed or solved

## Integration with Special Room
CrackedWall4 is automatically placed by the DungeonGenerator in the special room platform:
- The special room is created with holes and floor islands
- A platform is added on the opposite side from the entrance
- CrackedWall4 is placed along the wall of this platform
- This creates a challenging area where players must navigate holes to reach the wall

## Troubleshooting

### Wall Not Responding to Clicks
- Check that the player is within the interaction distance
- Verify the player reference is set correctly
- Ensure the wall has a Collider2D component (auto-added if missing)
- Check that the puzzle is not already solved

### Puzzle UI Not Appearing
- Verify that the Puzzle UI Panel is assigned
- Check that the SpellGridDragController is properly configured
- Ensure the Back Button is assigned and functional
- Check console for missing UI component warnings

### Spell Grid Not Working
- Verify that the SpellGridDragController reference is correct
- Check that the correct spell pattern is set
- Ensure the OnSpellCompleted event is properly connected

### Visual Issues
- Make sure the SpriteRenderer component is present
- Verify that the Solved Wall Sprite is assigned
- Check that the prefab has the correct sprite assigned

## Dependencies
- **SpellGridDragController**: Required for spell grid functionality
- **Puzzle UI System**: Required for UI overlay
- **Player GameObject**: Required for distance calculations
- **PlayerController**: Required for freezing player movement

## Notes
- CrackedWall4 is specifically designed for the special room platform
- It provides a unique challenge combining puzzle solving with the hole navigation mechanic
- The puzzle system is identical to CrackedWall1 for consistency
- The wall automatically manages game state during puzzle solving
- No enemy spawning or CarryableObject sealing functionality
