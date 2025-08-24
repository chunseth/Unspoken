# Boss Health Bar System Setup

## Overview
The Boss Health Bar system displays the Royal Slime boss's health in the UI when the player enters the boss room. It automatically appears and disappears based on player location and boss status.

## Components

### 1. BossHealthBar.cs
- **Purpose**: Displays boss health with animated health bar and text
- **Features**: 
  - Animated health bar fill
  - Color transitions (green → yellow → red)
  - Fade in/out animations
  - Boss name display
  - Health percentage or absolute values

### 2. BossRoomDetector.cs
- **Purpose**: Detects when player enters/leaves boss room
- **Features**:
  - Integrates with DungeonGenerator
  - Automatic health bar show/hide
  - Performance-optimized checking

## Setup Instructions

### 1. Create Boss Health Bar UI
1. **Create Canvas**: Ensure you have a UI Canvas in your scene
2. **Create Health Bar Panel**:
   - Create a Panel in the Canvas
   - Position it at the top of the screen (e.g., top-center)
   - Add `BossHealthBar` component to this panel

3. **Set up UI Elements**:
   - **Background Image**: Optional background for the health bar
   - **Health Bar Image**: Main health bar fill (should be a colored rectangle)
   - **Health Text**: TextMeshPro text for health numbers
   - **Boss Name Text**: TextMeshPro text for boss name

### 2. Configure BossHealthBar Component
```
Boss Settings:
- Boss Health: Leave empty (auto-detects Royal Slime)
- Boss Name: "Royal Slime" (or your boss name)
- Show As Percentage: false (recommended)
- Health Text Format: "{0}/{1}" (current/max)

Animation Settings:
- Fill Animation Speed: 5
- Color Animation Speed: 3
- Animate Health Changes: true
- Fade Speed: 2

Color Settings:
- Full Health Color: Green
- Medium Health Color: Yellow
- Low Health Color: Red
- Medium Health Threshold: 0.5

Display Settings:
- Hide When Full: false
- Hide When Dead: true
```

### 3. Set up BossRoomDetector
1. **Add to Player**: Add `BossRoomDetector` component to the player GameObject
2. **Configure References**:
   - Boss Health Bar: Reference to your BossHealthBar component
   - Dungeon Generator: Reference to DungeonGenerator (auto-detects)
3. **Detection Settings**:
   - Check Interval: 0.5 seconds (good balance of responsiveness/performance)

### 4. Health Bar UI Layout Example
```
Boss Health Bar Panel (anchored to top-center)
├── Background Image (optional)
├── Boss Name Text (TextMeshPro)
└── Health Bar Container
    ├── Health Bar Background
    └── Health Bar Fill (Image with BossHealthBar component)
        └── Health Text (TextMeshPro)
```

## Integration with Existing Systems

### DungeonGenerator Integration
- Uses `IsPositionInBossRoom()` method to detect player location
- Automatically works with existing boss room generation
- No additional configuration needed

### EnemyHealth Integration
- Subscribes to `onHealthPercentChanged` and `onDeath` events
- Automatically finds Royal Slime boss in scene
- Updates health bar in real-time

### Player Integration
- BossRoomDetector attached to player GameObject
- Tracks player position and boss room entry/exit
- Manages health bar visibility

## Visual Features

### Health Bar Animation
- Smooth fill animation when health changes
- Color transitions based on health percentage
- Fade in/out when entering/leaving boss room
- **Complete panel hide/show** - entire health bar panel is disabled when not in boss room

### Color System
- **Green (100-50%)**: Full to medium health
- **Yellow (50-25%)**: Medium health
- **Red (25-0%)**: Low health

### Text Display
- Shows current/max health (e.g., "150/200")
- Displays boss name
- Updates in real-time

## Performance Considerations

### Optimization
- Health bar only updates when health changes
- Room detection checks every 0.5 seconds
- Uses Unity events for efficient communication
- Automatic cleanup on destroy

### Memory Management
- Properly unsubscribes from events
- No memory leaks from event subscriptions
- Efficient object finding and caching

## Troubleshooting

### Health Bar Not Appearing
1. Check if BossRoomDetector is on the player
2. Verify DungeonGenerator reference
3. Ensure boss room exists and is accessible
4. Check if Royal Slime has EnemyHealth component

### Health Bar Not Updating
1. Verify BossHealthBar references to UI elements
2. Check if EnemyHealth events are firing
3. Ensure health bar image has proper RectTransform setup
4. Verify pivot and anchor settings

### Performance Issues
1. Increase check interval in BossRoomDetector
2. Reduce animation speeds if needed
3. Check for multiple BossHealthBar instances
4. Verify event subscriptions are clean

## Customization

### Different Boss Types
- Change `bossName` in BossHealthBar
- Modify color schemes for different bosses
- Adjust health bar position and size
- Customize text formats

### UI Styling
- Modify health bar appearance
- Add boss-specific backgrounds
- Customize text fonts and colors
- Add additional UI elements (boss portrait, etc.)

### Animation Tweaking
- Adjust animation speeds
- Modify color thresholds
- Change fade timing
- Add additional visual effects
