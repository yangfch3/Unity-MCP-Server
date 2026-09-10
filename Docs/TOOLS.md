# 内置工具详细文档

中文 | [English](TOOLS_EN.md)

本文档详细描述 Unity MCP Server 的所有内置工具，包括参数说明和使用示例。

> **Server 单线程串行处理请求。** 任何耗时工具（`build_compile`、`build_runTests`、`util_delay`、`code_executeImmediate` 跨帧模式）执行期间，后续请求一律排队等待，无法并发。给这类工具设超时/时长参数时按需给最小值。

---

## Debug 工具

### `console_getLogs`

获取 Unity Console 最近 N 条日志，支持级别/关键字过滤和上下文模式。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `count` | int | ❌ | 20 | 返回的日志条数 |
| `level` | string | ❌ | - | 日志级别过滤：`Error`、`Warning`、`Log` |
| `keyword` | string | ❌ | - | 关键字过滤（大小写不敏感） |
| `beforeIndex` | int | ❌ | - | 上下文模式：锚点索引（稳定全局 ID） |

### `console_clearLogs`

清空日志缓冲区。无参数。

### `debug_getStackTrace`

获取最近一条 Error/Exception 的完整堆栈信息。无参数。

### `debug_getPerformanceStats`

获取 FPS、DrawCall、内存占用等关键性能指标。无参数。

### `debug_screenshotGame`

截取 Game 视图并返回图片。仅 PlayMode 可用，默认输出 `JPG`，最大高度 `1024`。

> **默认关闭**：在 Window → MCP Server → Experimental 中开启 **Enable Game Screen Shot** 后注册。切换后 Agent 可能需要重连刷新工具列表。

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `maxWidth` | integer | `0` | 最大宽度，`0`=不限制 |
| `maxHeight` | integer | `1024` | 最大高度，等比缩放 |
| `format` | string | `jpg` | `png` 或 `jpg` |
| `quality` | integer | `75` | JPG 质量 1-100 |

### `debug_screenshotScene`

截取 Scene 视图并返回图片。默认不合成 UI，使用临时 Camera 渲染。

> **默认关闭**：在 Window → MCP Server → Experimental 中开启 **Enable Scene Screen Shot** 后注册。切换后 Agent 可能需要重连刷新工具列表。

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `maxWidth` | integer | `0` | 最大宽度，`0`=不限制 |
| `maxHeight` | integer | `1024` | 最大高度，等比缩放 |
| `format` | string | `jpg` | `png` 或 `jpg` |
| `quality` | integer | `75` | JPG 质量 1-100 |
| `includeUI` | boolean | `false` | 是否通过临时 Canvas 合成 UI |

---

## Editor 工具

### Project（项目）

#### `menu_execute`

按路径执行 Unity Editor 菜单项。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `path` | string | ✅ | - | Unity 菜单路径 |

#### `playmode_control`

进入/退出/暂停/恢复/查询 PlayMode 状态。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `action` | string | ✅ | - | 操作：`enter`、`exit`、`pause`、`resume`、`status` |

### Query（查询）

#### `editor_getSelection`

获取当前 Hierarchy/Project 中选中的对象信息。无参数。

#### `editor_getHierarchy`

获取当前场景的 GameObject 树结构，支持 Prefab Stage 和 Selection 子树。PlayMode 下默认输出自动包含 DontDestroyOnLoad 场景中的对象。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `maxDepth` | int | ❌ | -1 | 最大遍历深度，-1 表示无限制 |
| `root` | string | ❌ | `""` | 根节点来源：空=Prefab Stage 优先回退 Active Scene（PlayMode 下自动包含 DontDestroyOnLoad）；`"selection"`=以当前选中 GameObject 为根 |

#### `editor_selectGameObject`

通过路径或 instanceID 选中 Hierarchy 中的 GameObject。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `path` | string | ❌ | - | GameObject 路径（如 `/Root/Child/Target`） |
| `instanceID` | int | ❌ | - | GameObject 的 instanceID（与 path 二选一，优先使用） |

#### `editor_getProjectPath`

获取当前 Unity 项目的根目录路径。无参数。

**响应格式：**

```json
{
  "projectPath": "D:/MyUnityProject",
  "assetsPath": "D:/MyUnityProject/Assets"
}
```

#### `editor_getProjectStructure`

获取 Assets 目录结构。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `maxDepth` | int | ❌ | 3 | 最大遍历深度 |

#### `editor_getInspector`

获取选中对象的 Inspector 序列化字段值。无参数。

#### `editor_findGameObjects`

按名称/组件类型搜索场景中的 GameObject。PlayMode 下搜索范围自动包含 DontDestroyOnLoad 场景。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `namePattern` | string | ❌ | - | 名称匹配模式（支持 `*` 和 `?` 通配符，无通配符时为子串匹配） |
| `componentType` | string | ❌ | - | 组件类型简短类名（如 `Camera`、`MeshRenderer`），大小写不敏感 |
| `maxResults` | int | ❌ | 50 | 最大返回数量 |
| `activeOnly` | bool | ❌ | true | 是否仅搜索激活状态的 GameObject |

### Mutation（修改）

#### `editor_addGameObject`

在 Prefab Stage 或 Active Scene 中添加 GameObject。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `name` | string | ❌ | `"GameObject"` | 新 GameObject 的名称 |
| `parentInstanceID` | int | ❌ | - | 父节点的 instanceID |
| `parentPath` | string | ❌ | - | 父节点的路径 |

#### `editor_deleteGameObject`

删除指定的 GameObject 及其所有子对象。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `instanceID` | int | ❌ | - | 要删除的 GameObject 的 instanceID |
| `path` | string | ❌ | - | 要删除的 GameObject 的路径（与 instanceID 二选一） |

#### `editor_addComponent`

给指定 GameObject 添加组件。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `instanceID` | int | ❌ | - | 目标 GameObject 的 instanceID |
| `path` | string | ❌ | - | 目标 GameObject 的路径 |
| `componentType` | string | ✅ | - | 要添加的组件类型名（如 `"BoxCollider"`） |

#### `editor_removeComponent`

移除指定 GameObject 上的组件。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `instanceID` | int | ❌ | - | 目标 GameObject 的 instanceID |
| `path` | string | ❌ | - | 目标 GameObject 的路径 |
| `componentType` | string | ✅ | - | 要移除的组件类型名 |

#### `editor_reparentGameObject`

修改 GameObject 的父节点。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `instanceID` | int | ❌ | - | 目标 GameObject 的 instanceID |
| `path` | string | ❌ | - | 目标 GameObject 的路径 |
| `newParentInstanceID` | int | ❌ | - | 新父节点的 instanceID |
| `newParentPath` | string | ❌ | - | 新父节点的路径 |
| `worldPositionStays` | bool | ❌ | true | 是否保持世界坐标不变 |

#### `editor_setActive`

修改 GameObject 的激活状态。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `instanceID` | int | ❌ | - | 目标 GameObject 的 instanceID |
| `path` | string | ❌ | - | 目标 GameObject 的路径 |
| `active` | bool | ✅ | - | 激活状态 |

#### `editor_setComponentEnabled`

修改 GameObject 上指定组件的启用/禁用状态。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `instanceID` | int | ❌ | - | 目标 GameObject 的 instanceID |
| `path` | string | ❌ | - | 目标 GameObject 的路径 |
| `componentType` | string | ✅ | - | 组件类型名 |
| `enabled` | bool | ✅ | - | 启用/禁用状态 |

#### `editor_setTransform`

修改 Transform / RectTransform 属性。instanceID/path 二选一，按需传属性。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `instanceID` | int | ❌ | - | 目标 GameObject 的 instanceID |
| `path` | string | ❌ | - | 目标 GameObject 的路径 |
| `localPosition` | [x,y,z] | ❌ | - | 本地位置 |
| `localRotation` | [x,y,z] | ❌ | - | 本地旋转欧拉角 |
| `localScale` | [x,y,z] | ❌ | - | 本地缩放 |
| `rect` | object | ❌ | - | RectTransform 专用，仅 UI 节点生效 |
| `rect.anchoredPosition` | [x,y] | ❌ | - | 锚点位置 |
| `rect.sizeDelta` | [w,h] | ❌ | - | 尺寸偏移 |
| `rect.pivot` | [x,y] | ❌ | - | 轴心 |
| `rect.anchorMin` | [x,y] | ❌ | - | 最小锚点 |
| `rect.anchorMax` | [x,y] | ❌ | - | 最大锚点 |

#### `editor_setField`

修改 GameObject 上指定组件的序列化字段值。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `instanceID` | int | ❌ | - | 目标 GameObject 的 instanceID |
| `path` | string | ❌ | - | 目标 GameObject 的路径 |
| `componentType` | string | ✅ | - | 组件类型名 |
| `fieldName` | string | ✅ | - | 序列化字段名 |
| `value` | any | ✅ | - | 新值（类型需与字段匹配） |

### Asset（资产）

#### `asset_deleteFolder`

删除指定 Assets 子目录并刷新 AssetDatabase。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `path` | string | ✅ | - | 要删除的目录路径（相对于项目根目录，如 `Assets/XLua/Gen`） |

---

## Build 工具

### `build_compile`

触发脚本编译并返回编译结果。无参数。

内部先 `AssetDatabase.Refresh()`，再等 2 秒探测编译是否启动；未启动则返回「无需编译，代码已是最新」。**资源导入慢于 2 秒时这一判定会误报**，此时改动其实尚未编译。存疑就反射读一个已知常量确认版本。

### `build_getCompileErrors`

获取当前编译错误列表。无参数。

### `build_runTests`

运行 Unity Test Runner 测试并返回结果。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `mode` | string | ❌ | `EditMode` | 测试模式：`EditMode` 或 `PlayMode` |
| `testFilter` | string | ❌ | - | 测试名称过滤 |

---

## Util 工具

### `util_delay`

等待指定毫秒后返回。用于给 Editor 侧异步过程（协程、资源加载、场景启动）留出推进时间，期间主线程照常走帧。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `ms` | int | ❌ | 1000 | 等待毫秒数，范围 100–120000 |

**响应格式：**

```json
{ "waitedMs": 12016, "playMode": "Playing" }
```

`playMode` 为等待结束时的状态（`Stopped` / `Paused` / `Playing`），便于判断期间是否发生状态跃迁。

典型用法：`playmode_control(enter)` 之后等游戏框架起来，再读日志或截图，避免拿到中间态。若等待逻辑需要判断具体条件而非固定时长，优先用 `code_executeImmediate` 的跨帧模式轮询条件。

---

## Code 工具（实验性，仅 Unity 2022 Mono）

> 需在 Window → MCP Server 面板的 Experimental 区域手动开启。仅在 Unity 2022 (Mono) 下可用，Unity 6+ 不可见。

### `code_executeImmediate`

动态编译并执行 C# 代码片段。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `code` | string | ✅ | - | 要编译执行的 C# 源代码 |
| `mainThread` | bool | ❌ | `true` | 指定是否在主线程执行。`true`=可调用 Unity API 但无超时保护；`false`=后台线程执行，有超时保护但不可调用 Unity API |
| `timeout` | int | ❌ | 见说明 | 超时毫秒数。后台模式默认 5000，范围 1000–30000；跨帧模式默认 30000，范围 1000–120000 |

**响应格式：**

```json
{
  "success": true,
  "output": "Console 输出",
  "error": "",
  "warning": "系统警告（后台模式为空）",
  "frames": 19,
  "elapsedMs": 1815
}
```

`frames` / `elapsedMs` 仅跨帧模式返回。`warning` 在主线程模式（无超时保护提示）和跨帧模式（驱动方式提示）下都会填充，仅后台模式为空。

**三种执行模式：**

| 入口签名 | `mainThread` | 行为 |
|---------|-------------|------|
| `void Run()` | `true`（默认） | 主线程同步执行，可调用 Unity API，**无超时保护**，死循环会冻结 Editor |
| `void Run()` | `false` | 后台线程执行，有超时保护，**不可调用 Unity API** |
| `IEnumerator Run()` | `true`（默认） | 主线程**跨帧**执行，可调用 Unity API，有超时保护 |

`IEnumerator Run()` 搭配 `mainThread: false` 会被拒绝——帧只在主线程推进。

**跨帧模式（`IEnumerator Run()`）**

由 `EditorApplication.update` 逐帧驱动，Edit Mode 与 PlayMode 均可用。用于等待加载、等待回调、等待输入被消费等需要跨帧的场景，一次调用即可拿到完整结果，无需拆成「启动协程」+「读日志」两步。

支持的 yield 形式：

- `yield return null` — 等一帧
- `yield return <IEnumerator>` — 嵌套子协程，同帧进入子协程第一步，子协程结束后同帧恢复父级
- 其它 yield 值一律按等一帧处理。**不支持 `WaitForSeconds` 等 YieldInstruction 语义**，需要等时间请用 `while` 循环配合自行计时

```csharp
using System;
using System.Collections;
using UnityEngine;

public class Probe
{
    public static IEnumerator Run()
    {
        for (int i = 0; i < 30; i++) yield return null;   // 等 30 帧

        yield return WaitForCanvas();                     // 嵌套等待

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

注意事项：

- **执行期间整个 Server 阻塞**（见文档开头）。实测 20 秒跨帧期间，一个 100 ms 的 `util_delay` 墙钟耗时 18 秒。**把 `timeout` 设成够用的最小值**，别无脑拉到 120000
- **Edit Mode 帧率不固定**（Editor 失焦时可能低至 10 fps），不要用帧数估算时间，反之亦然
- 域重载（编译、进出 PlayMode）会中断执行并返回相应 error

**其它约定与限制：**

- 入口方法：必须包含 `public static void Run()` 或 `public static IEnumerator Run()`
- 输出方式：通过 `Console.WriteLine` 输出，工具会捕获并返回（跨帧模式全程捕获）。`Debug.Log` 不进 `output`，需用 `console_getLogs` 读取
- 不支持 `async Run()` 或返回 `Task` 的入口
- 仅单文件编译：每次调用只接受一段代码字符串，不支持多文件
- 仅限已加载程序集：可引用 Editor AppDomain 中已加载的程序集，不支持外部 NuGet 包
- **C# 语法支持到 7.x**：编译器为 Mono mcs（已启用 `-langversion:latest`）。元组、`out var`、模式匹配、插值字符串、`default` 字面量可用；**局部函数与 C# 8 语法（`switch` 表达式、`using` 声明、`??=`）不可用** —— mcs 解析器未实现，请改用 `private static` 方法

**示例：**

```csharp
using System;
using System.Linq;

public class Example
{
    public static void Run()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var unityAsm = assemblies.Where(a => a.GetName().Name.StartsWith("UnityEngine"));
        Console.WriteLine($"已加载 {assemblies.Length} 个程序集，其中 Unity 引擎: {unityAsm.Count()}");
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
