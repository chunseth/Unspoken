# CrackedWall3LineDrawer Setup Guide

## Overview
The `CrackedWall3LineDrawer` script creates a line drawing system that shows a visual path from the player to the nearest CarryableObject when the player presses the 'E' key near CrackedWall3. This helps players understand the connection between the wall and the object they need to carry.

## Features
- **Proximity Detection**: Automatically detects when the player is within range of CrackedWall3
- **Key Input**: Responds to 'E' key press to trigger line drawing
- **Dynamic Line**: Continuously updates the line position as the player or object moves
- **Timed Display**: Shows the line for 10 seconds (configurable)
- **Visual Feedback**: Uses a yellow line by default (configurable color and thickness)

## Setup Instructions

### 1. Add the Script to CrackedWall3
1. Select the CrackedWall3 GameObject in your scene
2. In the Inspector, click "Add Component"
3. Search for and add "CrackedWall3LineDrawer"

### 2. Configure Settings

#### Interaction Settings
- **Interaction Distance**: How close the player must be to CrackedWall3 to trigger the line drawing (default: 3 units)
- **Interaction Key**: The key to press to draw the line (default: E)

#### Line Settings
- **Line Duration**: How long the line is displayed in seconds (default: 10)
- **Line Thickness**: Thickness of the line in pixels (default: 3)
- **Line Color**: Color of the line (default: Yellow)

#### References
- **Player**: Reference to the player GameObject (auto-assigned if player has "Player" tag)
- **CrackedWall3**: Reference to the CrackedWall3 component (auto-assigned if found in scene)

### 3. Automatic Setup
The script will automatically:
- Find the player GameObject (must have "Player" tag)
- Find the CrackedWall3 component in the scene
- Create a Canvas for UI rendering if none exists
- Set up the UILineRenderer component
- Configure all necessary UI elements

## How It Works

### Detection
1. The script continuously monitors the distance between the player and CrackedWall3
2. When the player enters the interaction range, it logs the event
3. When the player presses 'E' while in range, it triggers the line drawing

### Line Drawing
1. Finds the nearest CarryableObject in the scene
2. Creates a UI line that connects the player to the object
3. Updates the line position every frame to follow moving objects
4. Automatically hides the line after the specified duration

### UI System
- Creates a Canvas with ScreenSpaceOverlay render mode
- Uses the existing UILineRenderer component for drawing
- Converts world positions to screen coordinates for proper UI rendering

## Requirements

### Scene Requirements
- Player GameObject with "Player" tag
- CrackedWall3 GameObject with CrackedWall3 component
- At least one CarryableObject in the scene
- Main Camera (Camera.main)

### Script Dependencies
- UILineRenderer.cs (already exists in Puzzles folder)
- CrackedWall3.cs
- CarryableObject.cs

## Testing

### Manual Testing
1. Position the player near CrackedWall3 (within interaction distance)
2. Press 'E' key
3. Verify that a yellow line appears from the player to the nearest CarryableObject
4. Verify that the line follows both the player and object as they move
5. Verify that the line disappears after 10 seconds

### Debug Information
The script provides console logs for:
- Player entering/exiting interaction range
- Line drawing start/completion
- Missing references or setup issues

## Troubleshooting

### Common Issues

**No line appears when pressing E:**
- Check that the player is within the interaction distance
- Verify that there's at least one CarryableObject in the scene
- Check the console for error messages
- Ensure the player has the "Player" tag

**Line appears but doesn't follow objects:**
- Verify that the main camera is set as Camera.main
- Check that the Canvas is properly set up
- Ensure the UILineRenderer component is working

**Performance issues:**
- Reduce the line thickness if needed
- Consider increasing the interaction distance to reduce frequent range checks

### Debug Methods
The script includes public methods for testing:
- `IsLineActive()`: Returns whether a line is currently being drawn
- `TriggerLineDrawing()`: Manually triggers line drawing (for testing)

## Customization

### Visual Customization
- Change line color in the inspector
- Adjust line thickness for different visual styles
- Modify interaction distance based on your game's scale

### Behavior Customization
- Change the interaction key to any other key
- Adjust line duration for different gameplay needs
- Modify the interaction distance for different puzzle requirements

## Integration with Existing Systems

### CrackedWall3 Integration
- Works alongside the existing CrackedWall3 enemy spawning system
- Doesn't interfere with the wall sealing mechanics
- Can be used whether the wall is sealed or not

### CarryableObject Integration
- Works with all existing CarryableObject instances
- Automatically finds the nearest object regardless of distance
- Continues working even if objects are picked up or dropped

### Player Controller Integration
- Uses the existing player input system
- Doesn't interfere with player movement or abilities
- Works with the existing player tag system
