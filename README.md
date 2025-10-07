# Turn-Based Framework

A modular, reusable turn-based combat system for Unity with DOTS compatibility.

## Features

- ✅ **Turn Queue Manager** - Initiative/speed-based turn ordering
- ✅ **Turn State Machine** - 8 combat states with validation
- ✅ **Action Queue System** - Queue and execute actions with animations
- ✅ **Event-Driven Architecture** - Easy integration with UI and game systems
- ✅ **DOTS Compatible** - Designed to work with Unity DOTS/ECS
- ✅ **Highly Modular** - Use components independently or together

## Installation

### Via Git URL (Recommended)

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click `+` button → `Add package from git URL...`
3. Enter: `https://github.com/eternalTornado/TurnBasedFramework.git`

### Via Package Manager (Local)

1. Download this repository
2. Open Unity Package Manager (`Window > Package Manager`)
3. Click `+` button → `Add package from disk...`
4. Select the `package.json` file

### Via manifest.json

Add to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.eternaltornado.turnbasedframework": "https://github.com/eternalTornado/TurnBasedFramework.git",
    ...
  }
}
```

## Quick Start

### 1. Create a Turn Manager in your scene

```csharp
// Add TurnManager component to a GameObject
var turnManager = gameObject.AddComponent<TurnManager>();
```

### 2. Implement ITurnBasedUnit on your characters

```csharp
public class MyCharacter : MonoBehaviour, ITurnBasedUnit
{
    public string UnitName => "Hero";
    public int Speed => 10;
    public bool IsAlive => currentHP > 0;
    public bool IsPlayerControlled => true;
    
    public GameObject GameObject => gameObject;
    public Transform Transform => transform;
    
    public void OnTurnStart() { /* Your logic */ }
    public void OnTurnEnd() { /* Your logic */ }
}
```

### 3. Start Combat

```csharp
var units = new List<ITurnBasedUnit> { player, enemy1, enemy2 };
TurnManager.Instance.StartCombat(units);
```

### 4. Execute Actions

```csharp
var action = new AttackAction(currentUnit, target);
TurnManager.Instance.ExecuteAction(action);
```

## Documentation

Full documentation available at: [Documentation~/index.md](Documentation~/index.md)

## Examples

Import samples via Package Manager:
- `Basic Turn-Based Combat Example`

## Requirements

- Unity 2021.3 or higher
- (Optional) DOTS packages for full ECS integration

## License

MIT License - See [LICENSE.md](LICENSE.md)

## Support

- GitHub Issues: https://github.com/eternalTornado/TurnBasedFramework/issues
- Discussions: https://github.com/eternalTornado/TurnBasedFramework/discussions

## Changelog

See [CHANGELOG.md](CHANGELOG.md)