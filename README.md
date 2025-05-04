# Lore, Legacy, and Monsters

A monster-collecting RPG game built in Unity where players can explore, battle, and collect monsters in a rich world full of adventures.

## Game Overview

Lore, Legacy, and Monsters is an RPG game inspired by classic monster-collecting games. Players explore different areas of the world, capturing and training monsters, battling trainers, completing quests, and advancing through the story.

## Project Setup

### Requirements

- Unity 2021.3 or newer
- C# IDE (Visual Studio, Visual Studio Code, etc.)

### Setup Instructions

1. Clone this repository
2. Open the project in Unity
3. Open the main scene: `Assets/Scenes/MainScene.unity`
4. Press Play to run the game

## Architecture Overview

The game is built using a modular, component-based architecture with several key systems:

### Core Systems

1. **Monster System** - Handles all monster data, instances, and mechanics
2. **Combat System** - Manages battles between monsters
3. **Inventory System** - Manages player items and equipment
4. **Shop System** - Provides buying and selling functionality
5. **Quest System** - Tracks missions and objectives
6. **World System** - Manages areas, exploration, and encounters
7. **Save/Load System** - Handles game state persistence
8. **Game Controller** - Coordinates all systems and manages game flow

### Data Management

- **ScriptableObjects** - Used for storing static game data (monster definitions, items, areas, etc.)
- **JSON Serialization** - Used for save data and dynamic content

### User Interface

- **Unity UI System** - Used for all game interfaces
- **Modular UI Components** - Each system has its own UI components
- **Event-Based Updates** - UI updates in response to system events

## Key Systems Documentation

### Monster System

The Monster System manages monster definitions, instances, and evolution. It handles:

- Monster data (stats, abilities, types)
- Player's monster collection
- Wild monster generation
- Monster evolution
- Experience and leveling

### Combat System

The Combat System handles all aspects of monster battles:

- Turn-based battle logic
- Ability effects and damage calculation
- Status effects
- Monster switching
- Battle rewards
- Trainer battles

### Inventory System

The Inventory System manages the player's items:

- Item categories (potions, monster balls, key items, etc.)
- Item usage effects
- Inventory capacity and organization
- Item acquisition and removal

### Shop System

The Shop System provides commerce functionality:

- Different shop types (general, specialized)
- Buying and selling items
- Price multipliers
- Special offers and sales
- Item stock management

### Quest System

The Quest System tracks player objectives:

- Main story quests
- Side quests
- Quest rewards
- Quest dependencies and progression
- Quest tracking and UI

### World System

The World System manages the game world and player movement:

- Areas and connections
- Player travel
- Random encounters
- Trainers and NPCs
- Area features (shops, healers, etc.)
- Area unlocking and progression

### Save/Load System

The Save/Load System handles game state persistence:

- JSON-based save files
- Auto-saving
- Save file management
- Cross-system state saving and loading

## Development Workflow

### Adding New Content

1. **New Monsters**: Create ScriptableObjects in `Assets/Resources/Monsters`
2. **New Items**: Create ScriptableObjects in `Assets/Resources/Items`
3. **New Areas**: Create ScriptableObjects in `Assets/Resources/Areas`
4. **New Quests**: Create ScriptableObjects in `Assets/Resources/Quests`

### Unit Testing

The project includes comprehensive unit tests for all core systems:

- Monster system tests
- Combat system tests
- Inventory system tests
- Shop system tests
- World system tests
- Save/Load system tests

Run tests from Unity Test Runner (Window > General > Test Runner).

### Debugging

- Debug controls are available in development builds
- Console logs provide detailed information
- Each manager has debug visualization options

## Credits

Developed by [Your Team Name]

## License

[Your License Information] 