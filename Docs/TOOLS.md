# 内置工具详细文档

中文 | [English](TOOLS_EN.md)

本文档详细描述 Unity MCP Server 的所有内置工具，包括参数说明和使用示例。

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

### `debug_screenshot`（暂时禁用）

> ⚠️ 该工具当前已禁用（`ScreenshotTool.cs` 整体注释），不会被注册。如需恢复，移除源文件中的注释包裹即可。

截取 Game/Scene 视图截图，返回 base64 PNG。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `view` | string | ❌ | `game` | 视图类型：`game` 或 `scene` |

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

### `build_getCompileErrors`

获取当前编译错误列表。无参数。

### `build_runTests`

运行 Unity Test Runner 测试并返回结果。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `mode` | string | ❌ | `EditMode` | 测试模式：`EditMode` 或 `PlayMode` |
| `testFilter` | string | ❌ | - | 测试名称过滤 |

---

## Code 工具（实验性，仅 Unity 2022 Mono）

> 需在 Window → MCP Server 面板的 Experimental 区域手动开启。仅在 Unity 2022 (Mono) 下可用，Unity 6+ 不可见。

### `code_executeImmediate`

动态编译并执行 C# 代码片段。

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `code` | string | ✅ | - | 要编译执行的 C# 源代码 |
| `mainThread` | bool | ❌ | `true` | 指定是否在主线程执行。`true`=可调用 Unity API 但无超时保护；`false`=后台线程执行，有超时保护但不可调用 Unity API |
| `timeout` | int | ❌ | 5000 | 后台模式超时时间（毫秒），仅 `mainThread: false` 时生效，范围 1000–30000 |

**响应格式：**

```json
{
  "success": true,
  "output": "Console 输出",
  "error": "",
  "warning": "系统警告（仅主线程模式）"
}
```

**约定与限制：**

- 入口方法：代码必须包含 `public static void Run()` 静态方法
- 输出方式：通过 `Console.WriteLine` 输出，工具会捕获并返回
- 双模式执行：
  - **主线程模式**（默认，`mainThread: true`）：在 Unity 主线程直接执行，可调用 Unity API（如 `GameObject.Find`、`AssetDatabase` 等），但无超时保护，死循环将冻结 Editor
  - **后台模式**（`mainThread: false`）：在后台线程执行，有超时保护（默认 5 秒，可通过 `timeout` 参数调整，范围 1–30 秒），但不可调用 Unity API
- 不支持异步入口：`async Run()` 或返回 `Task` 的方法不受支持
- 仅单文件编译：每次调用只接受一段代码字符串，不支持多文件
- 仅限已加载程序集：可引用 Editor AppDomain 中已加载的程序集，不支持外部 NuGet 包

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
