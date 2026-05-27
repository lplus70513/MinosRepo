# AGENTS.md

## Project overview
- Unity 2022.3.62f2c1 2D roguelike card-battle game with hex-grid map exploration
- All code comments and debug logs are in Chinese (Simplified)
- No CI, no tests, no lint/formatter configured
- **No namespaces**: all game code is in the global namespace (`Assembly-CSharp`). Only Spine and SREditor have .asmdef files.

## Two-layer architecture
- **World map layer**: hex-grid exploration with movement points. `GameManager` persists `WorldMapState` (position, health, move points, cleared cells) across scene transitions via `SaveWorldMapState`/`SaveBattleResult`.
- **Battle layer**: card-based combat encounters entered via `GameManager.EnterEncounter("SceneName")` and exited via `ExitEncounter()`. Scenes load/unload additively.
- Key scenes: `Manager` (boot) → `MainMenu` → `WorldMap` ↔ encounter scenes (`MVPTestScene`, `BattleMapTest`, etc.)

## Entry points
- Boot scene: `MinosMaze/Assets/Scenes/Manager.unity` — `GameManager` singleton sets `DontDestroyOnLoad`, then additively loads `MainMenu.unity`
- World map: `MinosMaze/Assets/Scenes/WorldMap.unity`
- Gameplay test scenes: `MinosMaze/Assets/Scenes/MVPTestScene.unity`, `MinosMaze/Assets/Scenes/BattleMapTest.unity`

## Architecture: Action/Reaction system
The core gameplay engine is a custom event pipeline — **not** UnityEvents or C# events:
- `ActionSystem` singleton manages `GameAction` subclasses (`PlayCardGA`, `DealDamageGA`, `KillEnemyGA`, `MoveGA`, etc.)
- Each `GameAction` has three reaction lists: `PreReactions`, `PerformReactions`, `PostReactions`
- Flow order per action: PreSubscribers → PreReactions → Performer → PerformReactions → PostSubscribers → PostReactions
- Systems register via `ActionSystem.AttachPerformer<T>(Func<T, IEnumerator>)`
- Systems subscribe via `ActionSystem.SubscribeReaction<T>(Action<T>, ReactionTiming)` where timing is `PRE` or `POST`
- Only one action flows at a time (`IsPerforming` guard). Added reactions execute recursively within the same flow.
- See `MinosMaze/Assets/Scripts/Action/` for action types and `MinosMaze/Assets/Scripts/System/` for subscribers

## Input gating: Interactions
- `Interactions` singleton gates all player input
- `PlayerCanInteract()` returns `false` when `ActionSystem.IsPerforming`, when deck viewer is open, or when hero is STUNNED
- `PlayerCanHover()` additionally returns `false` during drag/targeting

## Serialization: SREditor
- Embedded package at `MinosMaze/Assets/SREditor/` enables `[SerializeReference]` for polymorphic types
- `Effect`, `TargetMode`, `PerkCondition`, and `AutoTargetEffect` abstract classes in `.asset` files use this — do not break the reference chain
- Concrete Effect types: `DealDamageEffect`, `DrawCardEffect`, `AddStatusEffectEffect`

## Singleton pattern
- `Singleton<T>` base class auto-creates instances via `FindObjectOfType` or `new GameObject`
- Most systems are singletons: `ActionSystem`, `HeroSystem`, `EnemySystem`, `CardSystem`, `CostSystem`, `Interactions`, `ManualTargetSystem`, `EnemyViewCreator`, `CardViewHoverSystem`, `PerkSystem`

## Status effects
- Managed on `CombatantView` via `AddStatusEffect`/`RemoveStatusEffect` with a `Dictionary<StatusEffectType, int>`
- Non-stackable types (WEAKNESS, VULNERABLE, FRAGILE, SLOW, CHAIN_LIGHTNING, ROOT, STUN) ignore duplicate applications
- ARMOR absorbs damage before health. FORTIFY adds bonus armor. FRAGILE halves armor gain.
- `StatusEffectSystem` subscribes to `EnemyTurnGA` (bleed ticks, decay) and `DealDamageGA` (chain lightning spread)
- Decayable effects reduce by 1 per turn: BLEED, WEAKNESS, VULNERABLE, FRAGILE, SLOW, ROOT, STUN

## Perk system
- `PerkSystem` singleton holds a `List<Perk>`. `Perk` subscribes to game actions via `PerkCondition` and queues `AutoTargetEffect` as a new `GameAction` when conditions are met.
- `PerkCondition` has one subclass: `OnEnemyAttackCondition`

## Animation & rendering
- **DOTween** (`MinosMaze/Assets/Plugin/DOTween/`): all tweening (card movement, hand arrangement, damage shake, card discard scaling)
- **Spine** (`MinosMaze/Assets/Spine/`): `CombatantView` uses `SkeletonAnimation` for character sprites, plays "animation" track for attacks, freezes at first frame otherwise
- **Unity Splines** (`com.unity.splines`): `HandView` positions cards along a spline curve
- Custom shader `Custom/SpriteAlwaysVisible` used on CombatantView SpriteRenderers to render above other geometry
- 3D camera tagged `"3D Camera"` — `CombatantView` billboards toward it in `LateUpdate`

## Hex grid & movement
- Hex tilemap uses axial coordinates (q=x, r=z). 6 neighbor offsets: (1,0), (-1,0), (0,1), (-1,1), (1,-1), (0,-1)
- `HexMove` static class: `GetWalkableNeighbors`, `IsCellOccupied`, `HighlightMoveCellsInRange`
- `HexPathfinder` handles pathfinding. `HexRayCast` for mouse-to-hex picking.
- Battle hex grid and world map use separate systems: `HexGrid`/`HexCell` vs `WorldMapGrid`/`WorldMapState`

## Known stubs / missing
- `BleedEffect` and `WeaknessEffect` have no C# class files — only serialized references in `.asset` cards. Their `GetGameAction()` returns `null`.
- `SettingManager` is an empty stub (`Start` + `Update` only)

## Key directories
| Directory | Purpose |
|---|---|
| `MinosMaze/Assets/Scripts/Action/` | GameAction types, `ActionSystem`, `Singleton<T>`, `ReactionTiming` |
| `MinosMaze/Assets/Scripts/Card/` | Card system, hand view, cost, effects (`Card.cs`, `Effect.cs`, `TargetMode.cs`) |
| `MinosMaze/Assets/Scripts/Data/` | ScriptableObject data: cards, heroes, enemies, effects, perks, targets |
| `MinosMaze/Assets/Scripts/Map/` | Hex grid (`HexGrid`, `HexCell`, `HexMetrics`, `HexMove`, `HexPathfinder`) and world map (`WorldMapGrid`, `WorldMapState`) |
| `MinosMaze/Assets/Scripts/System/` | Gameplay systems (Hero, Enemy, MatchSetup, Interactions, StatusEffect, Move, Camera) |
| `MinosMaze/Assets/Scripts/Settings/` | App-level: `GameManager`, `SettingManager`, `UIBinder` |
| `MinosMaze/Assets/Scripts/Views/` | MVC views (`CombatantView`, `HeroView`, `EnemyView`, `WorldMapPlayerView`) |
| `MinosMaze/Assets/Scripts/UI/` | UI components (`StatusEffectsUI`, `CostUI`, `HealthBarUI`, `EndTurnButtonUI`) |
| `MinosMaze/Assets/Scripts/PopupText/` | Floating damage/heal text (uses DOTween) |
| `MinosMaze/Assets/Plugin/DOTween/` | DOTween tweening library |
| `MinosMaze/Assets/Spine/` | Spine 2D skeletal animation runtime |
| `MinosMaze/Assets/SREditor/` | Embedded SerializeReferenceEditor package |
| `MinosMaze/Assets/SO/` | Serialized ScriptableObject asset files (cards, etc.) |
| `MinosMaze/Assets/Prefabs/` | Reusable prefabs |
