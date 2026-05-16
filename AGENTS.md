# AGENTS.md

## Project overview
- Unity 2022.3.62f2c1 2D roguelike card-battle game with hex-grid map exploration
- All code comments and debug logs are in Chinese (Simplified)
- No CI, no tests, no lint/formatter configured

## Entry points
- Boot scene: `MinosMaze/Assets/Scenes/Manager.unity` — `GameManager` singleton sets `DontDestroyOnLoad`, then additively loads `MainMenu.unity`
- Gameplay test scenes: `MinosMaze/Assets/Scenes/MVPTestScene.unity`, `MinosMaze/Assets/Scenes/BattleMapTest.unity`

## Architecture: Action/Reaction system
The core gameplay engine is a custom event pipeline — **not** UnityEvents or C# events:
- `ActionSystem` singleton manages `GameAction` subclasses (`PlayCardGA`, `DealDamageGA`, `KillEnemyGA`, etc.)
- Each `GameAction` has three coroutine-driven phases: **PreReactions → PerformReactions → PostReactions**
- Systems register via `ActionSystem.AttachPerformer<T>()` and `ActionSystem.SubscribeReaction<T>()`
- See `MinosMaze/Assets/Scripts/Action/` for action types and `MinosMaze/Assets/Scripts/System/` for subscribers

## Serialization: SREditor
- Embedded package at `MinosMaze/Assets/SREditor/` enables `[SerializeReference]` for polymorphic types
- `Effect` and `TargetMode` abstract classes in ScriptableObject `.asset` files use this — do not break the reference chain

## Singleton pattern
- `Singleton<T>` base class auto-creates instances via `FindObjectOfType` or `new GameObject`
- Most systems are singletons: `ActionSystem`, `HeroSystem`, `EnemySystem`, `CardSystem`, `CostSystem`, `Interactions`, `ManualTargetSystem`, `EnemyViewCreator`, `CardViewHoverSystem`

## Animation & layout
- **DOTween** (`MinosMaze/Assets/Plugin/DOTween/`): all tweening (card movement, hand arrangement, damage shake, card discard scaling)
- **Unity Splines** (`com.unity.splines`): `HandView` positions cards along a spline curve

## Known stubs
- `BleedEffect.GetGameAction()` and `WeaknessEffect.GetGameAction()` return `null`
- `SettingManager` is an empty stub

## Key directories
| Directory | Purpose |
|---|---|
| `MinosMaze/Assets/Scripts/Action/` | GameAction types and reaction pipeline |
| `MinosMaze/Assets/Scripts/Card/` | Card system, hand view, cost, effects |
| `MinosMaze/Assets/Scripts/Data/` | ScriptableObject assets (cards, heroes, enemies, effects) |
| `MinosMaze/Assets/Scripts/Map/` | Hex grid generation (`HexGrid`, `HexCell`, `HexMetrics`) |
| `MinosMaze/Assets/Scripts/System/` | Gameplay systems (Hero, Enemy, MatchSetup, Interactions) |
| `MinosMaze/Assets/Scripts/Settings/` | App-level: GameManager, SettingManager, UIBinder |
| `MinosMaze/Assets/Scripts/Views/` | MVC views (`CombatantView`, `HeroView`, `EnemyView`) |
| `MinosMaze/Assets/Prefabs/` | Reusable prefabs |
| `MinosMaze/Assets/SREditor/` | Embedded SerializeReferenceEditor package |
