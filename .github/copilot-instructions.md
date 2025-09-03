# XFramework AI Development Guide

## Architecture Overview

XFramework is a Unity-based game framework with a component-driven architecture centered around the **GameLauncher** singleton pattern. All framework components inherit from `XFrameworkComponent` and are registered with priority-based initialization.

### Core Components Access Pattern
Access all managers through the `Global` static class:
```csharp
await Global.AssetManager.LoadAssetAsync<GameObject>("UIPanel");
await Global.UIManager.OpenPanelAsync(panelId);
Global.EventManager.Subscribe<GameEvent>(OnGameEvent);
```

### Component Registration & Lifecycle
- Components auto-register in `Awake()` via `GameLauncher.Instance.Register(this)`
- Initialization order controlled by `Priority` (see `Consts.XFrameworkConsts.ComponentPriority`)
- Components must implement `Init()` and `Clear()` methods
- Framework shuts down in reverse priority order

## Key Development Patterns

### UI System Architecture
- **UIManager**: Manages UI panels with layer-based system and caching
- **UIPanelBase**: Base class for all UI panels - inherit and override lifecycle methods
- **UILayer**: Manages UI layers with Canvas sorting orders, configured via `UILayerConfigTable`

UI Panel pattern:
```csharp
public class MainMenuUI : UIPanelBase
{
    protected override void OnInit() { /* Setup UI */ }
    protected override void OnShow() { /* Animate in */ }
    protected override void OnHide() { /* Animate out */ }
}

```

### Asset Management (YooAsset Integration)
- All assets loaded via `Global.AssetManager.LoadAssetAsync<T>(address)`
- Must enable YooAsset's Addressable functionality
- Framework handles asset lifecycle - track with `AssetHandler`
- Support for multiple build modes: Editor, Offline, Online, WebGL
- Asset initialization handled in `GameLauncher.PreloadConfigTablesAsync()`

### Configuration System
- Config tables loaded from `StreamingAssets/GameConfigs/` directory
- Use `ConfigTableHelper.GetTable<T>()` to access configuration data
- Config table names defined in `Consts.ConfigConsts` and auto-preloaded
- All configs inherit from `ConfigTableBase`

### Async Operations Pattern
- Heavy use of UniTask throughout the framework
- UI operations return `UniTask<UIPanelBase>`
- Asset loading is async by default
- Use `await` for sequential operations, avoid blocking main thread

### State Management
- **ProcedureManager**: Game flow state machine for major game states
- **StateMachineManager**: Generic state machine for any object
- States inherit from `ProcedureBase` or implement `IState<T>`

### Framework Startup Sequence
1. **Critical Rule**: No game content in Startup scene - framework initialization only
2. Config tables preloaded before component initialization 
3. AssetManager initializes before any asset-dependent systems
4. Switch to first game scene only after AssetManager is ready
5. Use `YooAsset` with Addressable system enabled

## Development Conventions

### Error Handling & Logging
- Use `XFramework.Utils.Log` class with structured prefixes: `[XFramework] [ComponentName]`
- Log levels: Debug (disabled in builds), Info, Warning, Error, Fatal
- Always validate parameters with descriptive error messages

### Component Design
- Single responsibility - each component manages one system
- Use dependency injection through `Global` class
- Components should be stateless where possible
- Implement proper cleanup in `Clear()` method
- Set priority using `Consts.XFrameworkConsts.ComponentPriority` constants

### Resource Management
- All GameObjects created via framework should be tracked
- Use object pooling through `PoolManager` for frequently created objects
- UI panels are cached by default - manual cleanup required for memory management
- Asset handlers stored in managers for proper disposal

## Common Integration Points

### Event System
```csharp
// Subscribe to events (use int IDs)
Global.EventManager.Subscribe(eventId, OnGameEvent);

// Fire events
Global.EventManager.Fire(eventId, eventData);
```

### Cache & Pooling
- Use `Global.CachePool` for temporary data caching
- Use `Global.PoolManager` for GameObject pooling
- Both systems handle automatic cleanup

### Settings Management
- Game configuration through `Global.GameSetting`
- Persistent settings with automatic serialization

## File Structure Navigation
- **Assets/XFramework/Runtime/**: Core framework code
- **Assets/XFramework/Runtime/Components/**: Individual managers (UI, Asset, Pool, etc.)
- **Assets/XFramework/Runtime/Base/**: Framework foundation (GameLauncher, Global, XFrameworkComponent)
- **Assets/XFramework/Utils/**: Utility classes and helpers
- **Assets/XFramework/Consts/**: Framework and game constants

## Development Workflow
1. Create components by inheriting `XFrameworkComponent`
2. Set appropriate priority in `Consts.XFrameworkConsts.ComponentPriority`
3. Register component access in `Global.cs`
4. Implement async patterns for any I/O operations
5. Use framework logging and error handling conventions
6. Test component lifecycle (Init/Clear) for proper resource management
