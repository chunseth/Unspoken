# SpellGridController Setup Guide

## Overview
The SpellGridController script creates a 3x3 grid system where clicking on specific image components toggles beams that shoot towards the center. GameObject 1/Image 1 is inactive and does nothing when clicked.

## Setup Instructions

### 1. Create the Script
- The `SpellGridController.cs` script has been created in the `Utility/` folder
- This script contains three classes:
  - `SpellGridController`: Main controller for the 3x3 grid system
  - `SpellGridClickHandler`: Handles clicks on individual grid positions
  - `SpellBeamBehavior`: Manages beam visual effects and animation

### 2. GameObject Structure
Ensure your hierarchy follows this structure:
```
spellGrid2/
├── spells/
│   ├── GameObject 0/
│   │   ├── Image 0 (active - toggles beam)
│   │   ├── Image 1 (active - toggles beam)
│   │   └── Image 2 (active - toggles beam)
│   ├── GameObject 1/
│   │   ├── Image 0 (active - toggles beam)
│   │   ├── Image 1 (INACTIVE - does nothing)
│   │   └── Image 2 (active - toggles beam)
│   └── GameObject 2/
│       ├── Image 0 (active - toggles beam)
│       ├── Image 1 (active - toggles beam)
│       └── Image 2 (active - toggles beam)
```

### 3. Component Setup
1. Add the `SpellGridController` component to a GameObject in your scene
2. Assign the `spellGrid2` reference to point to your spell grid parent GameObject
3. Configure the following settings in the inspector:

#### Grid Settings
- **spellGrid2**: Reference to the parent GameObject containing the spells
- **gridCenter**: Center position of the 3x3 grid (default: Vector2.zero)
- **cellSize**: Size of each grid cell (default: 1f)

#### Beam Settings
- **beamPrefab**: Optional prefab for custom beam visual effects
- **beamSpeed**: Speed of beam animation (default: 5f)
- **beamDuration**: How long beams stay active (default: 2f)
- **beamColor**: Color of active beams (default: cyan)

#### Visual Settings
- **beamMaterial**: Material for beam line renderer
- **beamWidth**: Width of beam lines (default: 0.1f)

### 4. How It Works

#### Grid Positions
The script maps grid positions as follows:
- GameObject 0/Image 0: Bottom-left corner
- GameObject 1/Image 1: Center (INACTIVE - does nothing)
- GameObject 2/Image 2: Top-right corner

#### Active Positions
All positions except GameObject 1/Image 1 are active for beam creation:
- GameObject 0: Image 0, Image 1, Image 2 (all active)
- GameObject 1: Image 0, Image 2 (active), Image 1 (inactive)
- GameObject 2: Image 0, Image 1, Image 2 (all active)

#### Beam Behavior
- Clicking on an active position toggles the beam on/off
- Beams shoot from the grid position towards the center
- Active positions show the beam color when toggled on
- Inactive positions show white
- GameObject 1/Image 1 shows no visual feedback and does nothing when clicked

### 5. Example Usage

```csharp
// Get reference to the controller
SpellGridController controller = FindObjectOfType<SpellGridController>();

// Toggle a specific beam
controller.ToggleBeam(0, 0); // Toggle GameObject 0/Image 0 beam
controller.ToggleBeam(1, 0); // Toggle GameObject 1/Image 0 beam
controller.ToggleBeam(1, 1); // Does nothing (inactive position)

// Get current beam states
Dictionary<(int, int), bool> states = controller.GetBeamStates();

// Clear all beams
controller.ClearAllBeams();
```

### 6. Customization

#### Custom Beam Prefab
Create a custom beam prefab with:
- LineRenderer component
- Custom materials and effects
- Assign it to the `beamPrefab` field

#### Visual Effects
Modify the `SpellBeamBehavior` class to add:
- Particle effects
- Sound effects
- Screen shake
- Custom animations

### 7. Troubleshooting

#### Common Issues
1. **"spellGrid2 reference is not assigned"**
   - Ensure the spellGrid2 GameObject is assigned in the inspector

2. **"GameObject X not found in spells"**
   - Check that the hierarchy structure matches the expected format
   - Verify GameObject names are exactly "GameObject 0", "GameObject 1", "GameObject 2"

3. **"Image X not found in GameObject X"**
   - Ensure each GameObject has Image components named "Image 0", "Image 1", "Image 2"

4. **Beams not appearing**
   - Check that the gridCenter position is correct
   - Verify the cellSize is appropriate for your scene scale
   - Ensure the beamMaterial is assigned if using custom materials

5. **GameObject 1/Image 1 not responding**
   - This is expected behavior - GameObject 1/Image 1 is intentionally inactive

### 8. Integration with Existing Systems

The script is designed to work alongside existing systems:
- Compatible with UI systems
- Can be integrated with puzzle mechanics
- Supports multiple instances for different grids
- Can be extended for more complex beam patterns

### 9. Beam Direction Examples

Based on the 3x3 grid layout:
- GameObject 0/Image 0: Beam goes from bottom-left towards center
- GameObject 0/Image 1: Beam goes from bottom-center towards center
- GameObject 0/Image 2: Beam goes from bottom-right towards center
- GameObject 1/Image 0: Beam goes from left-center towards center
- GameObject 1/Image 1: No beam (inactive)
- GameObject 1/Image 2: Beam goes from right-center towards center
- GameObject 2/Image 0: Beam goes from top-left towards center
- GameObject 2/Image 1: Beam goes from top-center towards center
- GameObject 2/Image 2: Beam goes from top-right towards center
