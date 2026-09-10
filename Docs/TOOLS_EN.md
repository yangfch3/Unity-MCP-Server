# Built-in Tools Reference

[中文](TOOLS.md) | English

Detailed documentation for all built-in tools in Unity MCP Server, including parameters and usage examples.

> **The server processes requests serially on a single thread.** While any long-running tool is executing (`build_compile`, `build_runTests`, `util_delay`, `code_executeImmediate` in cross-frame mode), all subsequent requests queue up — there is no concurrency. Set timeout/duration parameters on these tools to the smallest value that works.

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

### `debug_screenshotGame`

Capture the Game view and return an image. PlayMode only; defaults to `JPG` with a maximum height of `1024`.

> **Disabled by default**: enable **Enable Game Screen Shot** under Window → MCP Server → Experimental to register it. The Agent may need to reconnect after toggling.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `maxWidth` | integer | `0` | Max width, `0`=unlimited |
| `maxHeight` | integer | `1024` | Max height, uniformly downscaled |
| `format` | string | `jpg` | `png` or `jpg` |
| `quality` | integer | `75` | JPG quality 1-100 |

### `debug_screenshotScene`

Capture the Scene view and return an image. UI is excluded by default and rendering uses a temporary Camera.

> **Disabled by default**: enable **Enable Scene Screen Shot** under Window → MCP Server → Experimental to register it. The Agent may need to reconnect after toggling.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `maxWidth` | integer | `0` | Max width, `0`=unlimited |
| `maxHeight` | integer | `1024` | Max height, uniformly downscaled |
| `format` | string | `jpg` | `png` or `jpg` |
| `quality` | integer | `75` | JPG quality 1-100 |
| `includeUI` | boolean | `false` | Composite UI through a temporary Canvas |

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

Internally calls `AssetDatabase.Refresh()`, then probes for 2 seconds to see whether compilation started; if not, it returns "nothing to compile, code is up to date". **When asset import takes longer than 2 seconds this verdict is a false negative** — the change has not actually been compiled yet. When in doubt, reflect on a known constant to confirm the version.

### `build_getCompileErrors`

Get current compile error list. No parameters.

### `build_runTests`

Run Unity Test Runner tests and return results.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `mode` | string | ❌ | `EditMode` | Test mode: `EditMode` or `PlayMode` |
| `testFilter` | string | ❌ | - | Test name filter |

---

## Util Tools

### `util_delay`

Wait for the given number of milliseconds and return. Gives Editor-side asynchronous processes (coroutines, asset loading, scene startup) time to advance while the main thread keeps ticking frames.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `ms` | int | ❌ | 1000 | Milliseconds to wait, range 100–120000 |

**Response format:**

```json
{ "waitedMs": 12016, "playMode": "Playing" }
```

`playMode` is the state when the wait ends (`Stopped` / `Paused` / `Playing`), which makes state transitions during the wait easy to spot.

Typical use: after `playmode_control(enter)`, wait for the game framework to come up before reading logs or taking a screenshot, so you don't capture an intermediate state. When the wait depends on a condition rather than a fixed duration, prefer polling it in `code_executeImmediate` cross-frame mode.

---

## Code Tools (Experimental, Unity 2022 Mono only)

> Must be manually enabled in the Experimental section of Window → MCP Server panel. Only available on Unity 2022 (Mono); not visible on Unity 6+.

### `code_executeImmediate`

Compile and execute C# code snippets at runtime.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `code` | string | ✅ | - | C# source code to compile and execute |
| `mainThread` | bool | ❌ | `true` | Execute on main thread. `true`=can call Unity API, no timeout; `false`=background thread with timeout, no Unity API |
| `timeout` | int | ❌ | see notes | Timeout in milliseconds. Background mode defaults to 5000, range 1000–30000; cross-frame mode defaults to 30000, range 1000–120000 |

**Response format:**

```json
{
  "success": true,
  "output": "Console output",
  "error": "",
  "warning": "System warning (empty in background mode)",
  "frames": 19,
  "elapsedMs": 1815
}
```

`frames` / `elapsedMs` are returned in cross-frame mode only. `warning` is populated in both main-thread mode (no-timeout notice) and cross-frame mode (driver notice); it is empty only in background mode.

**Three execution modes:**

| Entry signature | `mainThread` | Behavior |
|----------------|-------------|----------|
| `void Run()` | `true` (default) | Synchronous on the main thread, can call Unity APIs, **no timeout protection** — infinite loops freeze the Editor |
| `void Run()` | `false` | Background thread with timeout protection, **cannot call Unity APIs** |
| `IEnumerator Run()` | `true` (default) | **Cross-frame** on the main thread, can call Unity APIs, with timeout protection |

`IEnumerator Run()` combined with `mainThread: false` is rejected — frames only advance on the main thread.

**Cross-frame mode (`IEnumerator Run()`)**

Driven frame by frame via `EditorApplication.update`; works in both Edit Mode and PlayMode. Use it to wait for loading, callbacks, or input consumption — a single call returns the complete result, with no need to split into "kick off a coroutine" plus "read the logs".

Supported yield forms:

- `yield return null` — wait one frame
- `yield return <IEnumerator>` — nested coroutine; enters the child's first step in the same frame and resumes the parent in the same frame after the child completes
- Any other yield value is treated as waiting one frame. **`WaitForSeconds` and other YieldInstruction semantics are not supported** — use a `while` loop with your own timing to wait on wall-clock time

```csharp
using System;
using System.Collections;
using UnityEngine;

public class Probe
{
    public static IEnumerator Run()
    {
        for (int i = 0; i < 30; i++) yield return null;   // wait 30 frames

        yield return WaitForCanvas();                     // nested wait

        Console.WriteLine("done at frame " + Time.frameCount);
    }

    private static IEnumerator WaitForCanvas()
    {
        int guard = 0;
        while (UnityEngine.Object.FindObjectOfType<Canvas>() == null && guard++ < 600)
            yield return null;
    }
}
```

Caveats:

- **The whole server blocks while it runs** (see the note at the top). Measured: during a 20-second cross-frame run, a 100 ms `util_delay` took 18 seconds wall-clock. **Set `timeout` to the smallest value that works** instead of maxing it out at 120000
- **Edit Mode frame rate is not fixed** (can drop to ~10 fps when the Editor is unfocused) — do not use frame counts to estimate wall-clock time, or vice versa
- A domain reload (compilation, entering/exiting PlayMode) aborts execution and returns a corresponding error

**Other conventions & limitations:**

- Entry point: code must contain `public static void Run()` or `public static IEnumerator Run()`
- Output: use `Console.WriteLine`; the tool captures and returns it (captured for the whole run in cross-frame mode). `Debug.Log` does not reach `output` — read it with `console_getLogs`
- No `async Run()` or `Task`-returning entry points
- Single-file only: each call accepts one code string; multi-file compilation is not supported
- Loaded assemblies only: can reference assemblies already loaded in the Editor's AppDomain; external NuGet packages are not supported
- **C# syntax up to 7.x**: the compiler is Mono mcs (with `-langversion:latest` enabled). Tuples, `out var`, pattern matching, interpolated strings and the `default` literal work; **local functions and C# 8 syntax (`switch` expressions, `using` declarations, `??=`) do not** — mcs never implemented them, so use `private static` methods instead

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
  "code": "using System;using System.Linq;public class Example{public static void Run(){var assemblies = AppDomain.CurrentDomain.GetAssemblies();var unityAsm = assemblies.Where(a => a.GetName().Name.StartsWith(\"UnityEngine\"));Console.WriteLine($\"Loaded {assemblies.Length} assemblies, Unity engine: {unityAsm.Count()}\");}}",
  "mainThread": false
}
{
  "success": true,
  "output": "Loaded 164 assemblies, Unity engine: 70\n",
  "error": "",
  "warning": ""
}
```
