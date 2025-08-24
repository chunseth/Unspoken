# Game Over Panel Troubleshooting Guide

## Quick Debug Steps

### 1. Test Manual Trigger
1. Select the GameOverManager GameObject in your scene
2. Right-click on the GameOverManager component in the Inspector
3. Select "Trigger Game Over (Manual)" from the context menu
4. Check the Console for debug messages

### 2. Use Keyboard Shortcut
1. Play the game
2. Press the **G** key
3. Check the Console for debug messages

### 3. Validate References
1. Select the GameOverManager GameObject
2. Right-click on the GameOverManager component
3. Select "Validate References" from the context menu
4. Check the Console output for missing references

### 4. Find UI Elements
1. Select the GameOverManager GameObject
2. Right-click on the GameOverManager component
3. Select "Find UI Elements" from the context menu
4. Check what UI elements are found in the scene

## Common Issues and Solutions

### Issue 1: GameOverPanel Not Found
**Symptoms**: Console shows "Game Over Panel: ✗ Missing"

**Solutions**:
1. **Create the UI Panel**:
   - Right-click in Hierarchy → UI → Panel
   - Rename it to exactly "GameOverPanel"
   - Make sure it's a child of a Canvas

2. **Check Canvas Setup**:
   - Ensure the GameOverPanel is under a Canvas
   - Canvas should be set to "Screen Space - Overlay" or "Screen Space - Camera"
   - Canvas should be active

3. **Check Panel Position**:
   - Set the panel's RectTransform to cover the full screen
   - Anchor points should be set to stretch across the screen

### Issue 2: Game Over Not Triggering
**Symptoms**: No debug messages when player dies

**Solutions**:
1. **Check PlayerStats Integration**:
   - Ensure the player has a PlayerStats component
   - Check that the player has the "Player" tag
   - Verify that GameOverManager can find the player

2. **Test Health System**:
   - Reduce player's max health in PlayerStats to 1
   - Take damage to trigger death
   - Check Console for "Player died!" message

### Issue 3: Panel Found But Not Showing
**Symptoms**: Console shows panel is found but screen doesn't appear

**Solutions**:
1. **Check Panel Hierarchy**:
   - Ensure GameOverPanel is not disabled in hierarchy
   - Check that parent Canvas is active
   - Verify no parent objects are disabled

2. **Check Canvas Settings**:
   - Canvas Render Mode should be appropriate for your setup
   - Canvas should have a Camera reference if using "Screen Space - Camera"
   - Canvas should be in the correct sorting order

3. **Check Panel Visibility**:
   - Panel should have a visible background (Image component)
   - Panel's alpha should not be 0
   - Panel should not be behind other UI elements

### Issue 4: Time Scale Issues
**Symptoms**: Game pauses but no UI appears

**Solutions**:
1. **Check Pause Settings**:
   - In GameOverManager, set "Pause Game on Game Over" to false temporarily
   - Test if panel appears without pausing

2. **Check Invoke Timing**:
   - Reduce "Game Over Delay" to 0 for immediate testing
   - Check if panel appears immediately

## Step-by-Step Setup Verification

### 1. Verify UI Creation
```
Hierarchy should look like:
├── Canvas
│   └── GameOverPanel (initially inactive)
│       ├── GameOverBackground (optional)
│       ├── GameOverText
│       ├── RestartButton
│       ├── MainMenuButton
│       └── QuitButton
```

### 2. Verify GameOverManager Setup
- GameOverManager component should be on a GameObject in the scene
- Player reference should be assigned or auto-found
- UI references should be assigned (or auto-found)

### 3. Verify Player Setup
- Player should have "Player" tag
- Player should have PlayerStats component
- PlayerStats should have health > 0 initially

## Debug Console Messages to Look For

### Successful Setup:
```
GameOverManager: Found GameOverPanel in scene automatically.
GameOverManager: Found RestartButton in scene automatically.
GameOverManager: No PlayerStats component found on player!
```

### Issues to Watch For:
```
GameOverManager: Game over panel is not assigned! Please assign it in the inspector.
GameOverManager: Cannot show game over screen - gameOverPanel is null!
GameOverManager: Still cannot find GameOverPanel! Please create a UI Panel named 'GameOverPanel'.
```

## Testing Checklist

- [ ] GameOverManager component exists in scene
- [ ] GameOverPanel UI element exists and is named correctly
- [ ] GameOverPanel is child of an active Canvas
- [ ] Player has PlayerStats component
- [ ] Player has "Player" tag
- [ ] Manual trigger works (G key or context menu)
- [ ] Health system triggers death properly
- [ ] Console shows appropriate debug messages

## Quick Fix Commands

If you need to quickly test the system:

1. **Create minimal UI**:
   ```
   Right-click Hierarchy → UI → Panel → Rename to "GameOverPanel"
   Add Text component with "GAME OVER" text
   Add Button component with "Restart" text
   ```

2. **Test immediately**:
   ```
   Select GameOverManager → Right-click → "Trigger Game Over (Manual)"
   ```

3. **Check results**:
   ```
   Look for "GameOverManager: Game over screen displayed successfully." in Console
   ``` 