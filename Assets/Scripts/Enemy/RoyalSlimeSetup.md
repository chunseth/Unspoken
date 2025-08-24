# Royal Slime Boss Damage System Setup

## Overview
The Royal Slime boss now has damage functionality for both its charge attack and crown throw attack. The system includes visual indicators and proper collision detection.

## Damage Settings
- **Charge Damage**: 3 damage (default)
- **Crown Damage**: 2 damage (default)
- Both attacks can be configured in the inspector

## Attack Mechanics

### Charge Attack
- **Range**: 10 tiles forward
- **Hitbox**: 1.5 tiles radius around the boss during charge
- **Damage**: 3 damage on contact
- **Visual**: Red color flash + charge indicator
- **Knockback**: Applied away from boss position

### Crown Throw Attack
- **Range**: 8 tiles
- **Hitbox**: 1.5 tiles radius at landing location
- **Damage**: 2 damage on explosion
- **Projectile**: Can also damage player during flight (0.15 tile radius - matches 0.3x0.3 crown size)
- **Return**: Crown flies back to boss after landing (0.3s pause at target)
- **Visual**: Yellow color flash + crown indicator
- **Knockback**: Applied away from explosion center

## Required Setup

### 1. Player Setup
- Ensure the player has a `PlayerStats` component
- Player must be tagged as "Player"
- Player layer must be set correctly

### 2. Crown Prefab Setup
- Create a crown prefab with a `CircleCollider2D` (optional - will be added automatically)
- The `CrownProjectileDamage` script will be added automatically
- Set the crown sprite and visual effects

### 3. Attack Indicators (Optional)
- Create indicator prefabs for charge and crown attacks
- Assign them to `chargeIndicatorPrefab` and `crownIndicatorPrefab` in the inspector
- Indicators should show where the attack will land

### 4. Layer Setup
- Set `playerLayer` to the player's layer
- Set `obstacleLayer` to walls/obstacles layer

## Visual Feedback
- **Charge Warning**: Boss turns red during preparation
- **Crown Warning**: Boss turns yellow during preparation
- **Attack Indicators**: Show landing zones for attacks
- **Screen Flash**: Player screen flashes red when taking damage
- **Knockback**: Player is pushed away from damage source

## Debug Information
The system logs damage events to the console:
- "Royal Slime charge attack hit player for X damage!"
- "Royal Slime crown explosion hit player for X damage!"
- "Crown projectile hit player for X damage!"

## Balancing
- Adjust `chargeDamage` and `crownDamage` values for difficulty
- Modify `attackCooldown` to control attack frequency
- Change hitbox sizes (`1.5f` values) for precision
- Adjust `chargeDistance` and `crownThrowRange` for attack range
