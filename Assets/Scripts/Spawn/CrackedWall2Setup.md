# CrackedWall2 Setup Guide

## Overview
This guide explains how to set up the second cracked wall puzzle component (`CrackedWall2`) that will be placed in the dungeon (not in the boss room). The component is identical to the first `CrackedWall` but operates independently.

## Component Location
- **CrackedWall2**: Place 2 instances in the dungeon (not in boss room)
- **CrackedWall**: Keep 1 instance in the boss room

## Setup Steps

### 1. Create the Second Prefab
1. Duplicate your existing cracked wall prefab
2. Rename it to "CrackedWall2" or similar
3. Replace the `CrackedWall` component with `CrackedWall2` component

### 2. Configure the Component
The `CrackedWall2` component has the same inspector fields as `CrackedWall`:

#### UI References
- **Puzzle UI Panel**: Assign the same UI panel used by the first wall
- **Background Image**: Assign the same background image
- **Back Button**: Assign the same back button
- **Spell Grid Controller**: Assign the same spell grid controller

#### Interaction Settings
- **Interaction Distance**: 2f (default)
- **Correct Spell Pattern**: "036" (same as first wall)

#### Player Reference
- **Player**: Will auto-find if not assigned

#### Visual Effects
- **Solved Wall Sprite**: Assign the normal wall sprite
- **Solve Effect**: Optional particle effect

#### Enemy Spawning
- **Enemy Prefab**: Assign enemy prefab for spawning
- **Spawn Distance**: 20f (default)
- **Spawn Interval**: 5f (default)
- **Max Enemies**: 10 (default)
- **Spawn Indicator Duration**: 2f (default)
- **Show Spawn Indicators**: true (default)

### 3. Place in Scene
1. Place 2 instances of the CrackedWall2 prefab in the dungeon
2. Ensure they are NOT in the boss room
3. Position them strategically for gameplay

### 4. UI Setup
Both walls will share the same UI elements:
- The same puzzle panel will be used for both walls
- The same spell grid controller will handle both puzzles
- Only one wall can be active at a time (puzzle mode pauses the other)

## Key Differences from CrackedWall
- Different class name (`CrackedWall2` vs `CrackedWall`)
- Different debug log prefixes for easier identification
- Independent puzzle solving state
- Can operate simultaneously with the first wall (when not in puzzle mode)

## Testing
1. Run the game and approach each wall
2. Test interaction by clicking or pressing 'E' when nearby
3. Verify the puzzle opens and can be solved
4. Check that enemy spawning works correctly
5. Ensure both walls can be solved independently

## Troubleshooting
- Use the "Validate References" context menu option to check setup
- Use "Debug Interaction Status" to troubleshoot interaction issues
- Check console logs for "CrackedWall2:" prefixed messages

## Notes
- Both walls use the same spell pattern "036" for consistency
- The UI system is shared between both walls
- Enemy spawning is independent for each wall
- Visual changes (sprite swapping) work independently
