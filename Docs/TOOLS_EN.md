# Built-in Tools Reference

[中文](TOOLS.md) | English

Detailed documentation for all built-in tools in Unity MCP Server, including parameters and usage examples.

---

## Debug Tools

### `console_getLogs`

Get recent N log entries from Unity Console with level/keyword filtering and context mode.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `count` | int | ❌ | 20 | Number of log entries to return |
| `level` | string | ❌ | - | Log level filter: `Error`, `Warning`, `Log` |
| `keyword` | string | ❌ | - | Keyword filter (case-insensitive) |
| `beforeIndex` | int | ❌ | - | Context mode: anchor index (stable global ID) |

### `console_clearLogs`

Clear the log buffer. No parameters.

### `debug_getStackTrace`

Get full stack trace of the latest Error/Exception. No parameters.

### `debug_getPerformanceStats`

Get FPS, DrawCall, memory usage and other key performance metrics. No parameters.

### `debug_screenshot`

Capture Game/Scene view screenshot, returns base64 image. Off-screen rendering, immune to occlusion by other apps or Unity tabs.

> **Disabled by default**: only registered after enabling **Enable Screen Shot** in the Experimental section of Window → MCP Server panel. When off, the tool is invisible in `tools/list` (calls return tool not found). Toggling takes effect immediately while the server is running, but the Agent side may need to reconnect to refresh its tool list.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `view` | string | ❌ | `game` | View type: `game` or `scene` |
| `maxWidth` | integer | ❌ | `0` | Max width, uniformly downscaled if exceeded, `0`=unlimited |
| `maxHeight` | integer | ❌ | `0` | Max height, uniformly downscaled if exceeded, `0`=unlimited |
| `format` | string | ❌ | `png` | Image format: `png` or `jpg` (jpg is smaller, recommended for token saving) |
| `quality` | integer | ❌ | `75` | jpg quality 1-100 |

Behavior notes:

- **game**: Play mode only (Game view has no continuous rendering in edit mode, returns an error). Returns the full frame at the Game view target resolution (including Overlay UI), with black-frame detection and retry. A near-black frame is not treated as failure: the image is returned with a warning text (either rendering did not complete, or the game itself is on a black screen)
- **scene**: SceneView camera rendering + UI Canvas compositing, without gizmos/grid lines or other editor overlays. Canvas compositing semantics:
  - `Overlay` / `ScreenSpaceCamera` canvases: composited fullscreen (consistent with how SceneView displays Overlay; SSC is a deliberate approximation for UI inspection)
  - `WorldSpace` canvases: rendered naturally at their world position (visible only within the camera frustum)

---

## Editor Tools

### Project

#### `menu_execute`

Execute a Unity Editor menu item by path.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `path` | string | ✅ | - | Unity menu path |

#### `playmode_control`

Enter/exit/pause/resume/query PlayMode state.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `action` | string | ✅ | - | Action: `enter`, `exit`, `pause`, `resume`, `status` |

### Query

#### `editor_getSelection`

Get currently selected object info from Hierarchy/Project. No parameters.

#### `editor_getHierarchy`

Get GameObject tree structure, supports Prefab Stage and Selection subtree. In PlayMode, the default output automatically includes objects from the DontDestroyOnLoad scene.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `maxDepth` | int | ❌ | -1 | Max traversal depth, -1 for unlimited |
| `root` | string | ❌ | `""` | Root source: empty=Prefab Stage first, fallback Active Scene (automatically includes DontDestroyOnLoad in PlayMode); `"selection"`=current selection as root |

#### `editor_selectGameObject`

Select a GameObject in the Hierarchy by path or instanceID.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `path` | string | ❌ | - | GameObject path (e.g., `/Root/Child/Target`) |
| `instanceID` | int | ❌ | - | GameObject instanceID (either one, instanceID takes priority) |

#### `editor_getProjectPath`

Get the current Unity project root directory path. No parameters.

**Response format:**

```json
{
  "projectPath": "D:/MyUnityProject",
  "assetsPath": "D:/MyUnityProject/Assets"
}
```

#### `editor_getProjectStructure`

Get Assets directory structure.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `maxDepth` | int | ❌ | 3 | Max traversal depth |

#### `editor_getInspector`

Get serialized field values of the selected object's Inspector. No parameters.

#### `editor_findGameObjects`

Search GameObjects in scene by name/component type. In PlayMode, the search scope automatically includes the DontDestroyOnLoad scene.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `namePattern` | string | ❌ | - | Name match pattern (supports `*` and `?` wildcards; substring match without wildcards) |
| `componentType` | string | ❌ | - | Component type short name (e.g., `Camera`, `MeshRenderer`), case-insensitive |
| `maxResults` | int | ❌ | 50 | Max results to return |
| `activeOnly` | bool | ❌ | true | Search active GameObjects only |

### Mutation

#### `editor_addGameObject`

Add a GameObject to Prefab Stage or Active Scene.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `name` | string | ❌ | `"GameObject"` | Name for the new GameObject |
| `parentInstanceID` | int | ❌ | - | Parent node instanceID |
| `parentPath` | string | ❌ | - | Parent node path |

#### `editor_deleteGameObject`

Delete a GameObject and all its children.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `instanceID` | int | ❌ | - | instanceID of the GameObject to delete |
| `path` | string | ❌ | - | Path of the GameObject to delete (either one) |

#### `editor_addComponent`

Add a component to a specified GameObject.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `instanceID` | int | ❌ | - | Target GameObject instanceID |
| `path` | string | ❌ | - | Target GameObject path |
| `componentType` | string | ✅ | - | Component type name (e.g., `"BoxCollider"`) |

#### `editor_removeComponent`

Remove a component from a specified GameObject.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `instanceID` | int | ❌ | - | Target GameObject instanceID |
| `path` | string | ❌ | - | Target GameObject path |
| `componentType` | string | ✅ | - | Component type name to remove |

#### `editor_reparentGameObject`

Change a GameObject's parent.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `instanceID` | int | ❌ | - | Target GameObject instanceID |
| `path` | string | ❌ | - | Target GameObject path |
| `newParentInstanceID` | int | ❌ | - | New parent instanceID |
| `newParentPath` | string | ❌ | - | New parent path |
| `worldPositionStays` | bool | ❌ | true | Whether to maintain world position |

#### `editor_setActive`

Set a GameObject's active state.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `instanceID` | int | ❌ | - | Target GameObject instanceID |
| `path` | string | ❌ | - | Target GameObject path |
| `active` | bool | ✅ | - | Active state |

#### `editor_setComponentEnabled`

Enable/disable a component on a GameObject.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `instanceID` | int | ❌ | - | Target GameObject instanceID |
| `path` | string | ❌ | - | Target GameObject path |
| `componentType` | string | ✅ | - | Component type name |
| `enabled` | bool | ✅ | - | Enable/disable state |

#### `editor_setTransform`

Modify Transform / RectTransform properties. Use instanceID or path (not both; ID takes priority).

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `instanceID` | int | ❌ | - | Target GameObject instanceID |
| `path` | string | ❌ | - | Target GameObject path |
| `localPosition` | [x,y,z] | ❌ | - | Local position |
| `localRotation` | [x,y,z] | ❌ | - | Local rotation (Euler angles) |
| `localScale` | [x,y,z] | ❌ | - | Local scale |
| `rect` | object | ❌ | - | RectTransform only, applies to UI nodes |
| `rect.anchoredPosition` | [x,y] | ❌ | - | Anchored position |
| `rect.sizeDelta` | [w,h] | ❌ | - | Size delta |
| `rect.pivot` | [x,y] | ❌ | - | Pivot |
| `rect.anchorMin` | [x,y] | ❌ | - | Anchor min |
| `rect.anchorMax` | [x,y] | ❌ | - | Anchor max |

#### `editor_setField`

Modify a component's serialized field value.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `instanceID` | int | ❌ | - | Target GameObject instanceID |
| `path` | string | ❌ | - | Target GameObject path |
| `componentType` | string | ✅ | - | Component type name |
| `fieldName` | string | ✅ | - | Serialized field name |
| `value` | any | ✅ | - | New value (type must match field) |

### Asset

#### `asset_deleteFolder`

Delete a specified Assets subdirectory and refresh AssetDatabase.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `path` | string | ✅ | - | Directory path relative to project root (e.g., `Assets/XLua/Gen`) |

---

## Build Tools

### `build_compile`

Trigger script compilation and return results. No parameters.

### `build_getCompileErrors`

Get current compile error list. No parameters.

### `build_runTests`

Run Unity Test Runner tests and return results.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `mode` | string | ❌ | `EditMode` | Test mode: `EditMode` or `PlayMode` |
| `testFilter` | string | ❌ | - | Test name filter |

---

## Code Tools (Experimental, Unity 2022 Mono only)

> Must be manually enabled in the Experimental section of Window → MCP Server panel. Only available on Unity 2022 (Mono); not visible on Unity 6+.

### `code_executeImmediate`

Compile and execute C# code snippets at runtime.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `code` | string | ✅ | - | C# source code to compile and execute |
| `mainThread` | bool | ❌ | `true` | Execute on main thread. `true`=can call Unity API, no timeout; `false`=background thread with timeout, no Unity API |
| `timeout` | int | ❌ | 5000 | Background mode timeout in milliseconds, only effective when `mainThread: false`, range 1000–30000 |

**Response format:**

```json
{
  "success": true,
  "output": "Console output",
  "error": "",
  "warning": "System warning (main thread mode only)"
}
```

**Conventions & Limitations:**

- Entry point: code must contain a `public static void Run()` static method
- Output: use `Console.WriteLine`; the tool captures and returns it
- Dual execution modes:
  - **Main thread mode** (default, `mainThread: true`): executes directly on the Unity main thread, can call Unity APIs (e.g., `GameObject.Find`, `AssetDatabase`), but has no timeout protection — infinite loops will freeze the Editor
  - **Background mode** (`mainThread: false`): executes on a background thread with timeout protection (default 5s, adjustable via `timeout` parameter, range 1–30s), but cannot call Unity APIs
- No async entry points: `async Run()` or `Task`-returning methods are not supported
- Single-file only: each call accepts one code string; multi-file compilation is not supported
- Loaded assemblies only: can reference assemblies already loaded in the Editor's AppDomain; external NuGet packages are not supported

**Example:**

```csharp
using System;
using System.Linq;

public class Example
{
    public static void Run()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var unityAsm = assemblies.Where(a => a.GetName().Name.StartsWith("UnityEngine"));
        Console.WriteLine($"Loaded {assemblies.Length} assemblies, Unity engine: {unityAsm.Count()}");
    }
}
```

```json
{
  "code": "using System;using System.Linq;public class Example{public static void Run(){var assemblies = AppDomain.CurrentDomain.GetAssemblies();var unityAsm = assemblies.Where(a => a.GetName().Name.StartsWith(\"UnityEngine\"));Console.WriteLine($\"已加载 {assemblies.Length} 个程序集，其中 Unity 引擎: {unityAsm.Count()}\");}}",
  "mainThread": false
}
{
  "success": true,
  "output": "已加载 164 个程序集，其中 Unity 引擎: 70\n",
  "error": "",
  "warning": ""
}
```
