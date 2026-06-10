# AGENTS.md

## Project overview
- Unity 2022.3.62f2c1 2D roguelike card-battle game with hex-grid map exploration
- All code comments and debug logs are in Chinese (Simplified)
- No CI, no tests, no lint/formatter configured
- **No namespaces**: all game code is in the global namespace (`Assembly-CSharp`). Only Spine and SREditor have .asmdef files.

## Two-layer architecture
- **World map layer**: hex-grid exploration with movement points. `GameManager` persists `WorldMapState` (position, health, move points, cleared cells) across scene transitions via `SaveWorldMapState`/`SaveBattleResult`.
- **Battle layer**: card-based combat encounters entered via `GameManager.EnterEncounter("SceneName")` and exited via `ExitEncounter()`. Scenes load/unload additively.
- Key scenes: `0_Manager` (boot) → `1_MainMenu` → `2.0_WorldMap` ↔ encounter scenes (`2.1_BattleScene`, `2.2_RestSite`, `2.3_StatueScene`)
- **Caution**: Code references main menu as `"1_Mainmenu"` (lowercase 'm') in some places and `"1_MainMenu"` in others — inconsistency in GameManager.cs.

## Entry points
- Boot scene: `MinosMaze/Assets/Scenes/0_Manager.unity` — `GameManager` singleton sets `DontDestroyOnLoad`, then additively loads `1_MainMenu`
- World map: `MinosMaze/Assets/Scenes/2.0_WorldMap.unity` (serialized field `worldMapSceneName = "2.0_WorldMap"`)
- Battle: `MinosMaze/Assets/Scenes/2.1_BattleScene.unity`
- Events: `2.2_RestSite.unity`, `2.3_StatueScene.unity`

## Architecture: Action/Reaction system
The core gameplay engine is a custom event pipeline — **not** UnityEvents or C# events:
- `ActionSystem` singleton manages `GameAction` subclasses (30+ types in `Action/GameAction/`)
- Key actions: `PlayCardGA`, `DealDamageGA`, `KillEnemyGA`, `MoveGA`, `DrawCardsGA`, `EnemyTurnGA`, `AttackHeroGA`, `AddStatusEffectGA`, `BattleWinGA`, `BattleLoseGA`, `RefillCostGA`, `DiscardAllCardsGA`
- Each `GameAction` has three reaction lists: `PreReactions`, `PerformReactions`, `PostReactions`
- Flow order per action: PreSubscribers → PreReactions → Performer → PerformReactions → PostSubscribers → PostReactions
- Systems register via `ActionSystem.AttachPerformer<T>(Func<T, IEnumerator>)`
- Systems subscribe via `ActionSystem.SubscribeReaction<T>(Action<T>, ReactionTiming)` where timing is `PRE` or `POST`
- Only one action flows at a time (`IsPerforming` guard). Added reactions execute recursively within the same flow.

## Input gating: Interactions
- `Interactions` singleton gates all player input
- `PlayerCanInteract()` returns `false` when `ActionSystem.IsPerforming`, when deck viewer is open, or when hero is STUNNED
- `PlayerCanHover()` additionally returns `false` during drag/targeting

## Serialization: SREditor
- Embedded package at `MinosMaze/Assets/SREditor/` enables `[SerializeReference]` for polymorphic types
- `Effect`, `TargetMode`, `PerkCondition`, and `AutoTargetEffect` abstract classes in `.asset` files use this — do not break the reference chain

## Singleton pattern
- `Singleton<T>` base class (at `Action/Class/Singleton.cs`) auto-creates instances via `FindObjectOfType` or `new GameObject`
- Most systems use it: `ActionSystem`, `HeroSystem`, `CardSystem`, `CostSystem`, `Interactions`, `ManualTargetSystem`, `EnemyViewCreator`, `CardViewHoverSystem`, `PerkSystem`
- **Exception**: `GameManager` does NOT use `Singleton<T>` — it implements its own `_instance` + `FindObjectOfType` pattern manually

## Effects system
- `Effect` abstract base class: `Card/Class/Effect.cs`
- Concrete Effect subclasses live in `Data/Effects/` (16+ types):
  `DealDamageEffect`, `DrawCardEffects` (note: class name has trailing 's'), `AddStatusEffectEffect`, `AddCardToHandEffect`, `AddMovePointsEffect`, `AddPerkEffect`, `BonusDrawEffect`, `DealArmorDamageEffect`, `DoubleStatusEffect`, `FreePlayEffect`, `GainActionPointsEffect`, `IfAttackedThisTurnEffect`, `MultiEffect`, `PullTargetEffect`, `RandomPlayFromHandEffect`, `ReturnToDrawPileEffect`, `StepBackEffect`
- `BleedEffect` and `WeaknessEffect` have no C# class files — only serialized references in `.asset` cards. Their `GetGameAction()` returns `null`.

## Status effects
- Managed on `CombatantView` via `AddStatusEffect`/`RemoveStatusEffect` with a `Dictionary<StatusEffectType, int>`
- Non-stackable types (WEAKNESS, VULNERABLE, FRAGILE, SLOW, CHAIN_LIGHTNING, ROOT, STUN) ignore duplicate applications
- ARMOR absorbs damage before health. FORTIFY adds bonus armor. FRAGILE halves armor gain.
- `StatusEffectSystem` subscribes to `EnemyTurnGA` (bleed ticks, decay) and `DealDamageGA` (chain lightning spread)
- Decayable effects reduce by 1 per turn: BLEED, WEAKNESS, VULNERABLE, FRAGILE, SLOW, ROOT, STUN

## Perk system
- `PerkSystem` singleton (located at `Data/Perk/PerkSystem.cs`) holds a `List<Perk>`. `Perk` subscribes to game actions via `PerkCondition` and queues `AutoTargetEffect` as a new `GameAction` when conditions are met.
- `PerkCondition` subclasses (7 total, in `Scripts/PerkCondition/`): `OnEnemyAttackCondition`, `OnAttackCardPlayedCondition`, `OnBleedAppliedCondition`, `OnCardDrawnCondition`, `OnTurnEndCondition`, `OnTurnStartCondition`, `OnUnblockedDamageCondition`

## Animation & rendering
- **DOTween** (`MinosMaze/Assets/Plugin/DOTween/`): all tweening (card movement, hand arrangement, damage shake, card discard scaling)
- **Spine** (`MinosMaze/Assets/Spine/`): `CombatantView` uses `SkeletonAnimation` for character sprites, plays "animation" track for attacks, freezes at first frame otherwise
- **Unity Splines** (`com.unity.splines`): `HandView` positions cards along a spline curve
- Custom shader `Custom/SpriteAlwaysVisible` used on CombatantView SpriteRenderers to render above other geometry
- 3D camera tagged `"3D Camera"` — `CombatantView` billboards toward it in `LateUpdate`

## Hex grid & movement
- Hex tilemap uses axial coordinates (q=x, r=z). 6 neighbor offsets: (1,0), (-1,0), (0,1), (-1,1), (1,-1), (0,-1)
- `HexMove` static class: `GetWalkableNeighbors`, `IsCellOccupied`, `HighlightMoveCellsInRange`
- `HexPathfinder` handles pathfinding. `HexRayCast` (in `System/`) for mouse-to-hex picking.
- Battle hex grid and world map use separate systems: `HexGrid`/`HexCell` vs `WorldMapGrid`/`WorldMapState`

## Key directories
| Directory | Purpose |
|---|---|
| `Scripts/Action/` | `ActionSystem`, `Singleton<T>`, `GameAction` base, `ReactionTiming` |
| `Scripts/Action/GameAction/` | 30+ GameAction subclasses |
| `Scripts/Action/` (root) | Also contains `EnemySystem.cs`, `DamageSystem.cs` |
| `Scripts/Card/` | Card system, hand view, cost (`CardSystem`, `CostSystem`, `CardViewHoverSystem`) |
| `Scripts/Card/Class/` | Base classes: `Card.cs`, `Effect.cs`, `TargetMode.cs` |
| `Scripts/Data/` | ScriptableObject data: cards, heroes, enemies, perks, targets |
| `Scripts/Data/Effects/` | All concrete `Effect` subclasses (16+) |
| `Scripts/Data/Perk/` | `PerkSystem.cs`, `Perk.cs`, `AutoTargetEffect` subclasses |
| `Scripts/Data/Hero & Enemy/` | `EnemyViewCreator.cs`, hero/enemy data |
| `Scripts/Map/` | Hex grid (`HexGrid`, `HexCell`, `HexMetrics`, `HexMove`, `HexPathfinder`) and world map (`WorldMapGrid`, `WorldMapState`) |
| `Scripts/System/` | `HeroSystem`, `MoveSystem`, `MatchSetupSystem`, `Interactions`, `StatusEffectSystem`, `CameraController`, `ManualTargetSystem`, `BattleResultSystem`, `RewardSystem`, `SceneTransitionSystem`, `MapCollapseSystem`, `WorldMapMovementSystem`, `WorldMapPlayerSystem`, `PlayerMovementSystem`, `HexRayCast` |
| `Scripts/Settings/` | `GameManager`, `SettingManager` (stub), `UIBinder`, `QuitGame` |
| `Scripts/Views/` | MVC views (`CombatantView`, `HeroView`, `EnemyView`, `WorldMapPlayerView`) |
| `Scripts/UI/` | UI components (`StatusEffectsUI`, `CostUI`, `HealthBarUI`, `EndTurnButtonUI`) |
| `Scripts/PopupText/` | Floating damage/heal text (uses DOTween) |
| `Scripts/Enums/` | `StatusEffectType.cs` enum |
| `Scripts/Extensions/` | `ListExtensions.cs` |
| `Scripts/Interfaces/` | `IHaveCaster.cs` |
| `Scripts/PerkCondition/` | 7 PerkCondition subclasses |
| `Scripts/Utility/` | `MouseUtil.cs` |
| `Plugin/DOTween/` | DOTween tweening library |
| `Spine/` | Spine 2D skeletal animation runtime |
| `SREditor/` | Embedded SerializeReferenceEditor package |
| `SO/` | Serialized ScriptableObject asset files (cards, etc.) |
| `Prefabs/` | Reusable prefabs |

Note: All directories above are relative to `MinosMaze/Assets/`.

## Known stubs / missing
- `BleedEffect` and `WeaknessEffect` have no C# class files — only serialized references in `.asset` cards
- `SettingManager` is an empty stub (`Start` + `Update` only)
