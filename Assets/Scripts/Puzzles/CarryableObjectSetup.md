# CarryableObject Setup Guide

This guide explains how to set up the CarryableObject component that allows players to pick up and carry objects, with movement and action restrictions while carrying.

## Overview

The CarryableObject is an interactable object that:
- Can be picked up by clicking or pressing E when the player is nearby
- Follows the player while being carried
- Restricts player movement speed, attacks, and abilities while carrying
- Can be dropped by pressing the E key (configurable)
- Provides visual feedback when highlighted

## Setup Instructions

### 1. Create the Prefab

1. Create a new GameObject in your scene
2. Add a SpriteRenderer component with your desired sprite
3. Add a Collider2D component (BoxCollider2D or CircleCollider2D)
4. Add a Rigidbody2D component (set to Dynamic)
5. Add the CarryableObject script component
6. Configure the inspector fields (see Configuration section below)
7. Create a prefab from this GameObject

### 2. Assign to DungeonGenerator

1. In the DungeonGenerator inspector, find the "Dungeon Puzzle Prefabs" section
2. Assign your CarryableObject prefab to the "Carryable Object Prefab" field
3. The object will automatically be placed in the same room as CrackedWall3

## Configuration

### Interaction Settings
- **Interaction Distance**: How close the player must be to interact (default: 2f)
- **Drop Key**: Key to pick up and drop the object (default: E)

### Carrying Settings
- **Carry Offset**: Position offset from player when carrying (default: 0, 0.5f, 0)
- **Carry Speed Multiplier**: Speed multiplier while carrying (default: 0.5f = half speed)

### Visual Feedback
- **Highlighted Sprite**: Optional sprite to show when object is highlighted
- **Highlight Color**: Color to tint the object when highlighted (default: Yellow)
- **Pickup Effect**: Optional particle effect to play when picked up
- **Drop Effect**: Optional particle effect to play when dropped

### Audio
- **Pickup Sound**: Audio clip to play when picked up
- **Drop Sound**: Audio clip to play when dropped

### References
- **Player**: Reference to the player GameObject (auto-found if tagged "Player")

## How It Works

### Pickup Process
1. Player approaches the object (within interaction distance)
2. Object shows visual feedback (highlighted sprite/color)
3. Player clicks on the object OR presses E key
4. Object is parented to player and follows them
5. Player movement speed is reduced
6. Player attacks and abilities are disabled
7. Pickup effects are played

### Carrying State
- Object smoothly follows the player with the specified offset
- Player cannot run, attack, or use abilities
- Player movement speed is multiplied by the carry speed multiplier
- Object maintains its original rotation

### Drop Process
1. Player presses the drop key (E by default)
2. Object is unparented from player
3. Physics and collider are re-enabled
4. Player restrictions are removed
5. Drop effects are played
6. Object can be picked up again

## Player Restrictions While Carrying

When carrying an object, the player:
- Cannot sprint (sprint input is ignored)
- Cannot attack (PlayerAttack component is disabled)
- Cannot use abilities (PathCreatorBeam component is disabled)
- Moves at reduced speed (multiplied by carry speed multiplier)

## Visual Features

### Gizmos
- Yellow wire sphere shows interaction range when selected
- Green wire sphere shows carry offset position when being carried

### Minimap
- Carryable objects appear as yellow dots on the minimap
- They are always visible (not affected by exploration)

## Troubleshooting

### Object Not Pickupable
- Check that the player has the "Player" tag
- Verify interaction distance is appropriate
- Ensure the object has a Collider2D component
- Check that the object is not already being carried

### Player Restrictions Not Working
- Verify the player has PlayerController, PlayerAttack, and PathCreatorBeam components
- Check that the components are enabled before pickup
- Ensure the SpeedModifier property is accessible in PlayerController

### Object Not Following Player
- Check that the object is properly parented to the player
- Verify the carry offset is appropriate
- Ensure the object's physics are disabled while carrying

### Visual Feedback Not Working
- Check that the object has a SpriteRenderer component
- Verify the highlighted sprite and color are assigned
- Ensure the object is within interaction range

## Integration with CrackedWall3

The CarryableObject is designed to work with CrackedWall3 in puzzle scenarios:
- Both objects are placed in the same room by the DungeonGenerator
- The carryable object can be used to solve puzzles or unlock the cracked wall
- Players must carry the object to specific locations or use it as a key

## Example Usage

1. Player enters room with CrackedWall3 and CarryableObject
2. Player approaches the CarryableObject and clicks on it OR presses E to pick it up
3. Player moves slowly to the CrackedWall3 while carrying the object
4. Player uses the carried object to interact with or solve the CrackedWall3 puzzle
5. Player presses E to drop the object after completing the puzzle

This creates an engaging puzzle mechanic that requires strategic movement and object manipulation.
