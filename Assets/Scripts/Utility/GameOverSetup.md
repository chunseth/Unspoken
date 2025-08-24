# Game Over System Setup Guide

## Overview
The Game Over system provides a comprehensive solution for handling player death, including:
1. Automatic detection when player health reaches zero
2. Pausing all game elements (projectiles, enemies, spawners)
3. Displaying a game over screen with restart/main menu/quit options
4. Proper cleanup and scene management

## Setup Instructions

### 1. Create the Game Over UI

#### Main Panel (GameOverPanel)
Create a UI panel for the game over screen:
- **Canvas**: Create a new Canvas or use existing one
- **Panel**: Create a UI Panel named "GameOverPanel"
  - Set it to cover the full screen
  - Use a semi-transparent dark background (e.g., black with 0.8 alpha)
  - Initially set `SetActive(false)` in the inspector

#### Background Image (GameOverBackground) - Optional
- Create an Image component as a child of the GameOverPanel
- Name it "GameOverBackground"
- Can be a decorative image, pattern, or solid color
- Recommended to use a semi-transparent image for overlay effect

#### Game Over Text
- Add a Text component to the GameOverPanel
- Set text to "GAME OVER" or your preferred message
- Style it with large, bold font and appropriate color

#### Buttons
Create three buttons as children of the GameOverPanel:

**Restart Button (RestartButton)**
- Text: "Restart Level"
- Function: Will restart the current scene
- **Required**: Must be assigned in GameOverManager

**Main Menu Button (MainMenuButton)**
- Text: "Main Menu"
- Function: Will load the main menu scene
- **Optional**: Can be left unassigned if not needed

**Quit Button (QuitButton)**
- Text: "Quit Game"
- Function: Will quit the application
- **Optional**: Can be left unassigned if not needed

### 2. Add GameOverManager to Scene

#### Option A: Add to Existing GameObject
- Find an appropriate GameObject in your scene (e.g., GameManager, UI Manager)
- Add the `GameOverManager` script component

#### Option B: Create New GameObject
- Create an empty GameObject named "GameOverManager"
- Add the `GameOverManager` script component

### 3. Configure GameOverManager

#### UI References
Assign the UI elements in the inspector:
- **Game Over Panel**: Drag the GameOverPanel from the hierarchy
- **Background Image**: Drag the GameOverBackground Image component (optional)
- **Restart Button**: Drag the RestartButton from the hierarchy
- **Main Menu Button**: Drag the MainMenuButton from the hierarchy (optional)
- **Quit Button**: Drag the QuitButton from the hierarchy (optional)

#### Game Over Settings
- **Game Over Delay**: Time in seconds before showing the game over screen (default: 1f)
- **Pause Game on Game Over**: Whether to set Time.timeScale to 0 (default: true)

#### Player Reference
- **Player**: Drag the player GameObject from the hierarchy
- If left empty, the system will automatically find the player by tag "Player"

### 4. Automatic Reference Finding

The GameOverManager will automatically find UI elements if they're not assigned:
- Looks for "GameOverPanel" GameObject
- Looks for "RestartButton", "MainMenuButton", "QuitButton" by name
- Looks for "GameOverBackground" Image component
- Looks for player with "Player" tag

### 5. Integration with PlayerStats

The system automatically integrates with the existing `PlayerStats` component:
- Subscribes to the `OnHealthChanged` event
- Triggers game over when health reaches zero
- No additional setup required for the player

### 6. Scene Management

#### Main Menu Scene
If using the main menu button, ensure you have a scene named "MainMenu":
- Create a main menu scene
- Add it to Build Settings
- Name it exactly "MainMenu"

#### Build Settings
Make sure all your scenes are added to Build Settings:
- File → Build Settings
- Add all scenes including MainMenu
- Set the correct scene order

## How It Works

### Game Over Trigger
1. Player takes damage and health reaches zero
2. `PlayerStats.Die()` method is called
3. `GameOverManager.TriggerGameOver()` is called
4. All game elements are frozen (projectiles, enemies, spawners)
5. Player controls are disabled
6. Game over screen appears after the configured delay

### Game Elements Frozen
The system freezes the following elements using the same logic as CrackedWall.cs:
- **Spell Projectiles**: All objects with "SpellProjectile" tag or SpellProjectile component
- **Enemy AI**: All objects with "Enemy" tag or EnemyAI component
- **Enemy Shooters**: All objects with EnemyShooter component
- **Enemy Spawners**: All objects with "EnemySpawner" tag, EnemySpawner component, or CrackedWall component
- **Player Controls**: PlayerController and PlayerAttack components

### Restart Functionality
When restart is clicked:
1. Time scale is restored
2. All frozen elements are unfrozen
3. Player controls are re-enabled
4. Game over state is reset
5. Current scene is reloaded

## Customization

### Adding Custom Game Elements to Freeze
To freeze additional game elements, modify the `IsSpellProjectile()`, `IsEnemy()`, or `IsEnemySpawner()` methods in `GameOverManager.cs`:

```csharp
private bool IsSpellProjectile(GameObject obj)
{
    // Add your custom conditions here
    if (obj.CompareTag("YourCustomTag"))
        return true;
    
    if (obj.GetComponent("YourCustomComponent") != null)
        return true;
    
    return false;
}
```

### Custom Game Over Screen
You can customize the appearance by:
- Modifying the UI layout and styling
- Adding animations or transitions
- Including additional information (score, time, etc.)
- Adding sound effects

### Scene Names
If your main menu scene has a different name, modify the `ReturnToMainMenu()` method:

```csharp
public void ReturnToMainMenu()
{
    // Change "MainMenu" to your actual scene name
    SceneManager.LoadScene("YourMainMenuSceneName");
}
```

## Debugging

### Validate References
Use the "Validate References" context menu option on the GameOverManager to check if all references are properly assigned.

### Console Messages
The system provides detailed console messages for:
- Reference validation
- Game over triggering
- Element freezing/unfreezing
- UI state changes

### Common Issues
1. **Game Over Panel not showing**: Check if GameOverPanel is assigned and active
2. **Buttons not working**: Ensure button references are assigned and OnClick events are set up
3. **Elements not freezing**: Check if objects have the correct tags or components
4. **Scene not restarting**: Verify scene is added to Build Settings

## Testing

### Manual Testing
You can manually trigger game over for testing:
1. Select the GameOverManager in the inspector
2. Use the "Trigger Game Over" context menu option
3. Or call `TriggerGameOver()` from another script

### Health Testing
To test with player health:
1. Reduce player's max health in PlayerStats
2. Take damage until health reaches zero
3. Verify game over screen appears and elements are frozen 