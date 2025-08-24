# Puzzle1Hint Setup Guide

## Overview
The `Puzzle1Hint` object is an interactive hint system that freezes enemy and player movement, actions, spawning, and attacks when opened. It's similar to the CrackedWall puzzle system but designed specifically for displaying hints to players.

## Features
- **Game Freezing**: When opened, it freezes all game interactions including:
  - Player movement and actions
  - Enemy AI and shooting behavior
  - Spell projectiles
  - Enemy spawning
- **UI Display**: Shows a hint sprite in a UI overlay
- **Trigger-Based Interaction**: Automatically opens when player collides with the hint trigger
- **One-Time Trigger**: Each hint can only be triggered once per session
- **Self-Destruct**: The hint object is destroyed from the dungeon after being triggered
- **Audio/Visual Effects**: Optional particle effects and sound effects
- **Automatic Reference Finding**: Automatically finds UI elements in the prefab

## Setup Instructions

### 1. Create the Hint Object
1. Create an empty GameObject in your scene
2. Add the `Puzzle1Hint` script to it
3. Position it where you want the hint to be accessible
4. Ensure the object has a Collider2D with `isTrigger` set to true

### 2. Create the HintUIPanel Prefab
You'll need to create a prefab for the hint UI panel:

#### Required Prefab Structure:
- **HintUIPanel**: A GameObject containing all hint UI elements
  - Must have an **Image component** attached to it (this will display the hint sprite)
  - Should contain a **BackButton** as a child (Button component to close the hint)

#### Important Notes:
- The HintUIPanel prefab must have an Image component attached to it. The script will automatically find this Image component and use it to display the hint sprite.
- The BackButton should be a child of the HintUIPanel prefab. The script will automatically find it within the prefab.

### 3. Assign References in Inspector
In the Puzzle1Hint component inspector, assign:

#### UI References:
- **Hint UI Panel Prefab**: The HintUIPanel prefab (must have an Image component)
- **Target Canvas**: The Canvas where the hint UI will be instantiated (will auto-find GameCanvas)

#### Interaction Settings:
- **Has Been Triggered**: Whether the hint has been triggered (prevents multiple triggers)
- **Player**: Reference to the player GameObject (will auto-find if not assigned)

#### Hint Content:
- **Hint Sprite**: The sprite to display as the hint for this puzzle (will be applied to the instantiated HintUIPanel's Image component)

#### Visual Effects:
- **Open Effect**: Particle effect to play when hint opens (optional)
- **Open Sound**: Audio clip to play when hint opens (optional)

### 4. Configure Colliders
The script will automatically add the necessary colliders:
- A solid BoxCollider2D for blocking
- A trigger BoxCollider2D for interaction detection (20% larger than solid collider)
- A Rigidbody2D set to kinematic

### 5. Set Up Tags
Ensure your objects have the correct tags:
- Player objects should have the "Player" tag
- Enemy objects should have the "Enemy" tag
- Spell projectiles should have the "SpellProjectile" tag

## Usage

### Player Interaction
Players interact with the hint object by:
1. **Trigger Collision**: Walking into the hint trigger area
2. **One-Time Trigger**: Each hint can only be triggered once per session

### Opening the Hint
When the hint is opened:
1. The UI panel becomes visible
2. Game interactions are frozen
3. Optional effects and sounds play
4. The hint sprite is displayed
5. The hint is marked as triggered (cannot be triggered again)
6. The hint object is destroyed from the dungeon

### Closing the Hint
When the hint is closed:
1. The UI panel is hidden
2. Game interactions are restored
3. All frozen objects resume their previous state

## Component Freezing Details

### Player Components Frozen:
- `PlayerController`: Player movement and input
- `PlayerAttack`: Player attack abilities
- `PlayerStats`: Player statistics and health

### Enemy Components Frozen:
- `EnemyAI`: Enemy movement and behavior
- `EnemyShooter`: Enemy shooting abilities

### Other Objects Frozen:
- Spell projectiles (tagged with "SpellProjectile")
- Spawn indicators (paused globally)

## Debug Features

### Context Menu Options:
- **Validate References**: Check if all required references are assigned
- **Find UI Elements**: Search for UI elements in the scene
- **Test Interaction**: Manually trigger the hint interaction
- **Debug Interaction Status**: Show detailed interaction information

### Console Logging:
The script provides extensive debug logging to help troubleshoot issues:
- Interaction detection
- Component freezing/unfreezing
- UI element finding
- Distance calculations

## Customization

### Adding New Player Components to Freeze:
Edit the `playerComponentNames` array in the `FreezePlayerComponents()` method:
```csharp
string[] playerComponentNames = {
    "PlayerController",
    "PlayerAttack", 
    "PlayerStats",
    "YourCustomComponent" // Add your component here
};
```

### Adding New Enemy Components to Freeze:
Edit the component name arrays in the freezing methods:
```csharp
string[] aiComponentNames = {
    "EnemyAI",
    "YourCustomEnemyAI" // Add your component here
};
```

### Custom Spell Projectile Detection:
Modify the `IsSpellProjectile()` method to match your spell system:
```csharp
private bool IsSpellProjectile(GameObject obj)
{
    // Add your custom detection logic here
    if (obj.CompareTag("SpellProjectile"))
        return true;
    if (obj.GetComponent<YourSpellComponent>() != null)
        return true;
    return false;
}
```

## Troubleshooting

### Common Issues:

1. **Hint doesn't open when clicked**
   - Check if the player is within interaction distance
   - Verify the colliders are set up correctly
   - Use the "Debug Interaction Status" context menu option

2. **UI elements not found**
   - Ensure UI elements have the correct names
   - Use the "Find UI Elements" context menu option
   - Check that UI elements are in a Canvas

3. **Game doesn't freeze properly**
   - Verify that objects have the correct tags
   - Check the console for freezing/unfreezing logs
   - Ensure component names match your actual components

4. **Components not unfreezing**
   - Check if the hint object is destroyed while active
   - Verify that the `OnDestroy()` method is called
   - Use the "Validate References" context menu option

### Debug Commands:
- Press 'E' while near the hint to test interaction
- Use context menu options for detailed debugging
- Check the console for detailed logs

## Example Prefab Structure

```
HintUIPanel Prefab:
├── HintUIPanel (with Image component)
│   └── BackButton (Button)

Scene Hierarchy:
├── GameCanvas (auto-found by script)
│   └── Other UI elements...
├── Player (tagged with "Player")
├── Puzzle1Hint (with Puzzle1Hint script)
│   └── HintUIPanel Prefab (assigned as prefab reference)
└── Enemies (tagged with "Enemy")

When triggered, the HintUIPanel will be instantiated as a child of the GameCanvas.
```

## Performance Considerations

- The script uses `FindObjectsOfType<GameObject>()` to find objects to freeze, which can be expensive in large scenes
- Consider using object pooling or more targeted search methods for better performance
- The freezing system stores references to all frozen objects, so memory usage increases with the number of frozen objects
