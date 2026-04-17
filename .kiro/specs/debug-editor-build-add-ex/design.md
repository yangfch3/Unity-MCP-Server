# Design Document

## Overview

在现有 Unity MCP 插件基础上扩展 10 个新工具，分为 debug、editor、build 三类。所有工具实现 `IMcpTool` 接口，放置在 `Editor/Tools/` 目录下，由 `ToolRegistry.AutoDiscover()` 自动发现注册。

核心设计决策：
- **最小化核心代码改动**：新工具仅需实现 `IMcpTool` 接口，不修改 McpServer、ToolRegistry 等已有组件。唯一的核心改动是扩展 `ContentItem` 以支持 MCP image content type，并在 `JsonRpcDispatcher` 中适配 image 类型的序列化输出
- **复用已有基础设施**：日志捕获复用 ConsoleTool 的 `Application.logMessageReceived` 模式；JSON 序列化复用 `MiniJson.SerializeString`；结果返回复用 `ToolResult.Success/Error`
- **主线程安全**：所有 Unity API 调用通过 `MainThreadQueue` 调度到主线程执行，工具内部无需关心线程问题
- **MCP 协议兼容**：Screenshot_Tool 返回 base64 图片时使用 MCP 协议的 `image` content type

## Architecture

```
Editor/Tools/
├── ConsoleTool.cs          # 已有 - debug_getLogs (改名前: console_getLogs)
├── MenuTool.cs             # 已有 - menu_execute
├── PlayModeTool.cs         # 已有 - playmode_control
├── StackTraceTool.cs       # 新增 - debug_getStackTrace
├── PerformanceTool.cs      # 新增 - debug_getPerformanceStats
├── ScreenshotTool.cs       # 新增 - debug_screenshot
├── SelectionTool.cs        # 新增 - editor_getSelection
├── HierarchyTool.cs        # 新增 - editor_getHierarchy
├── ProjectStructureTool.cs # 新增 - editor_getProjectStructure
├── InspectorTool.cs        # 新增 - editor_getInspector
├── CompileTool.cs          # 新增 - build_compile
├── CompileErrorsTool.cs    # 新增 - build_getCompileErrors
└── TestRunnerTool.cs       # 新增 - build_runTests
```

所有新工具遵循已有工具的模式：
1. 实现 `IMcpTool` 接口的 5 个成员
2. 在 `Execute` 方法中完成业务逻辑
3. 通过 `ToolResult.Success/Error` 返回结果
4. JSON 手动拼接，使用 `MiniJson.SerializeString` 转义字符串

## Components and Interfaces

### 1. StackTraceTool (debug_getStackTrace)

```
class StackTraceTool : IMcpTool
    Category: "debug"
    InputSchema: 无参数

    // 复用 ConsoleTool 的日志捕获模式
    // 静态缓冲区保存最近一条 Error/Exception 的 message + stackTrace
    static lastError: { message, stackTrace, timestamp }

    Execute():
        if lastError == null → 返回 "当前无错误日志"
        else → 返回 { message, stackTrace, timestamp } JSON
```

Unity API 依赖：`Application.logMessageReceived(message, stackTrace, LogType)`

### 2. PerformanceTool (debug_getPerformanceStats)

```
class PerformanceTool : IMcpTool
    Category: "debug"
    InputSchema: 无参数

    Execute():
        fps = 1.0f / Time.unscaledDeltaTime
        drawCalls = UnityStats.drawCalls        // UnityEditor.UnityStats
        memoryMB = Profiler.GetTotalAllocatedMemoryLong() / (1024*1024)
        → 返回 { fps, drawCalls, memoryUsedMB } JSON
```

Unity API 依赖：`UnityEditor.UnityStats`、`UnityEngine.Profiling.Profiler`、`Time.unscaledDeltaTime`

### 3. ScreenshotTool (debug_screenshot)

```
class ScreenshotTool : IMcpTool
    Category: "debug"
    InputSchema: { view: "game"|"scene", 默认 "game" }

    Execute(view):
        window = 查找对应 EditorWindow (GameView / SceneView)
        if window == null → 返回错误 "视图未打开"
        pixels = InternalEditorUtility.ReadScreenPixel(window.position)
        pngBytes = texture.EncodeToPNG()
        base64 = Convert.ToBase64String(pngBytes)
        → 返回 ContentItem(type="image", data=base64, mimeType="image/png")
```

Unity API 依赖：`EditorWindow.GetWindow`、`InternalEditorUtility.ReadScreenPixel`、`Texture2D.EncodeToPNG`

> **API 兼容性风险**：`InternalEditorUtility.ReadScreenPixel` 是 Unity 内部 API（`UnityEditorInternal` 命名空间），不属于公开稳定 API，存在跨 Unity 版本变更或移除的风险。备选方案：使用 `ScreenCapture.CaptureScreenshotAsTexture()`（仅 Game 视图）或 `EditorWindow.Repaint()` + `Texture2D.ReadPixels()` 从 RenderTexture 读取像素。实现时应优先尝试 `ReadScreenPixel`，捕获 `MissingMethodException` 后回退到备选方案。

### 4. SelectionTool (editor_getSelection)

```
class SelectionTool : IMcpTool
    Category: "editor"
    InputSchema: 无参数

    Execute():
        gameObjects = Selection.gameObjects
        projectAssets = Selection.assetGUIDs → AssetDatabase.GUIDToAssetPath
        → 返回 {
            gameObjects: [{ name, instanceID, path }],
            assets: [{ name, path }]
          }
```

Unity API 依赖：`UnityEditor.Selection`、`AssetDatabase.GUIDToAssetPath`

### 5. HierarchyTool (editor_getHierarchy)

```
class HierarchyTool : IMcpTool
    Category: "editor"
    InputSchema: { maxDepth: int, 默认 -1 }

    Execute(maxDepth):
        scene = SceneManager.GetActiveScene()
        roots = scene.GetRootGameObjects()
        tree = BuildTree(roots, currentDepth=0, maxDepth)
        → 返回树结构 JSON

    BuildTree(gameObjects, depth, maxDepth) → recursive:
        for each go:
            node = { name, active: go.activeSelf, components: [组件类型名] }
            if maxDepth == -1 || depth < maxDepth:
                node.children = BuildTree(go.children, depth+1, maxDepth)
        return nodes
```

Unity API 依赖：`SceneManager.GetActiveScene`、`GameObject.transform`、`GetComponents`

### 6. ProjectStructureTool (editor_getProjectStructure)

```
class ProjectStructureTool : IMcpTool
    Category: "editor"
    InputSchema: { maxDepth: int, 默认 3 }

    Execute(maxDepth):
        root = Application.dataPath  // Assets 目录
        tree = BuildDirTree(root, depth=0, maxDepth)
        → 返回目录树 JSON

    BuildDirTree(path, depth, maxDepth) → recursive:
        entries = Directory.GetFileSystemEntries(path)
        for each entry:
            skip if entry.EndsWith(".meta")
            node = { name, type: "file"|"directory", path: 相对路径 }
            if isDirectory && (maxDepth == -1 || depth < maxDepth):
                node.children = BuildDirTree(entry, depth+1, maxDepth)
        return nodes
```

Unity API 依赖：`Application.dataPath`、`System.IO.Directory`

### 7. InspectorTool (editor_getInspector)

```
class InspectorTool : IMcpTool
    Category: "editor"
    InputSchema: 无参数

    Execute():
        go = Selection.activeGameObject
        if go == null → 返回 "未选中任何 GameObject"
        components = go.GetComponents<Component>()
        for each component:
            so = new SerializedObject(component)
            prop = so.GetIterator()
            遍历所有可见属性 → { name, type, value }
        → 返回 { gameObject: name, components: [...] }
```

Unity API 依赖：`Selection.activeGameObject`、`SerializedObject`、`SerializedProperty`

### 8. CompileTool (build_compile)

```
class CompileTool : IMcpTool
    Category: "build"
    InputSchema: 无参数

    Execute():
        注册 CompilationPipeline.compilationStarted / compilationFinished 回调
        AssetDatabase.Refresh()  // 触发编译
        等待 2 秒检测是否触发编译
        if 未触发编译 → 返回 { success: true, errors: [], message: "无需编译，代码已是最新" }
        else → 等待编译完成（TaskCompletionSource + 60s 超时）
        收集 CompilerMessage[] 中的错误
        → 返回 { success: bool, errors: [{ file, line, column, message }] }
```

Unity API 依赖：`CompilationPipeline`、`AssetDatabase.Refresh`

### 9. CompileErrorsTool (build_getCompileErrors)

```
class CompileErrorsTool : IMcpTool
    Category: "build"
    InputSchema: 无参数

    // 通过 CompilationPipeline.assemblyCompilationFinished 事件回调
    // 在每次程序集编译完成时缓存 CompilerMessage[] 中的错误到静态列表
    static cachedErrors: List<CompilerMessage>

    Execute():
        → 返回 { errors: [{ file, line, column, code, message }] }
        → 无错误时返回 { errors: [], message: "当前无编译错误" }
```

Unity API 依赖：`CompilationPipeline.assemblyCompilationFinished`

### 10. TestRunnerTool (build_runTests)

```
class TestRunnerTool : IMcpTool
    Category: "build"
    InputSchema: { mode: "EditMode"|"PlayMode", 默认 "EditMode"; testFilter: string, 可选 }

    Execute(mode, testFilter):
        api = ScriptableObject.CreateInstance<TestRunnerApi>()
        filter = new Filter { testMode, testNames = testFilter }
        注册 testFinished / runFinished 回调
        api.Execute(executionSettings)
        等待运行完成（TaskCompletionSource）
        → 返回 {
            summary: { total, passed, failed, skipped },
            failures: [{ testName, message }]
          }
```

Unity API 依赖：`UnityEditor.TestTools.TestRunner.Api`

## Data Models

### StackTrace 响应

```json
{
  "message": "NullReferenceException: Object reference not set...",
  "stackTrace": "at MyScript.Update() in Assets/Scripts/MyScript.cs:42\n...",
  "timestamp": "2025-01-15T10:30:00Z"
}
```

### Performance 响应

```json
{
  "fps": 60.2,
  "drawCalls": 128,
  "memoryUsedMB": 512.3
}
```

### Screenshot 响应

使用 MCP 协议的 image content type：
```json
{
  "content": [
    { "type": "image", "data": "<base64>", "mimeType": "image/png" }
  ]
}
```

需要扩展 `ContentItem` 支持 image 类型（新增 Data 和 MimeType 字段），并在 `JsonRpcDispatcher` 的响应序列化中处理 image 类型的输出格式。

### Selection 响应

```json
{
  "gameObjects": [
    { "name": "Player", "instanceID": 12345, "path": "/Player" }
  ],
  "assets": [
    { "name": "PlayerMaterial.mat", "path": "Assets/Materials/PlayerMaterial.mat" }
  ]
}
```

### Hierarchy 响应

```json
[
  {
    "name": "Main Camera",
    "active": true,
    "components": ["Transform", "Camera", "AudioListener"],
    "children": []
  },
  {
    "name": "Canvas",
    "active": true,
    "components": ["Transform", "Canvas", "CanvasScaler"],
    "children": [
      {
        "name": "Button",
        "active": true,
        "components": ["RectTransform", "Button", "Image"],
        "children": []
      }
    ]
  }
]
```

### ProjectStructure 响应

```json
[
  {
    "name": "Scripts",
    "type": "directory",
    "path": "Assets/Scripts",
    "children": [
      { "name": "PlayerController.cs", "type": "file", "path": "Assets/Scripts/PlayerController.cs" }
    ]
  }
]
```

### Inspector 响应

```json
{
  "gameObject": "Player",
  "components": [
    {
      "type": "Transform",
      "fields": [
        { "name": "m_LocalPosition", "type": "Vector3", "value": "(0, 1, 0)" },
        { "name": "m_LocalRotation", "type": "Quaternion", "value": "(0, 0, 0, 1)" }
      ]
    }
  ]
}
```

### Compile 响应

```json
{
  "success": false,
  "errors": [
    {
      "file": "Assets/Scripts/MyScript.cs",
      "line": 42,
      "column": 5,
      "message": "CS1002: ; expected"
    }
  ]
}
```

### CompileErrors 响应

```json
{
  "errors": [
    {
      "file": "Assets/Scripts/MyScript.cs",
      "line": 42,
      "column": 5,
      "code": "CS1002",
      "message": "; expected"
    }
  ]
}
```

### TestRunner 响应

```json
{
  "summary": { "total": 10, "passed": 8, "failed": 1, "skipped": 1 },
  "failures": [
    { "testName": "MyTest.ShouldWork", "message": "Expected 1 but got 2" }
  ]
}
```

## ContentItem 扩展

Screenshot_Tool 需要返回 image 类型内容。需要扩展 `ContentItem` 类以支持 MCP 协议的 image content：

```
// 现有 ContentItem 新增可选字段
class ContentItem
    Type: string      // "text" 或 "image"
    Text: string      // text 类型使用
    Data: string      // image 类型使用（base64）
    MimeType: string  // image 类型使用（如 "image/png"）
```

`JsonRpcDispatcher.HandleToolsCall` 中的响应序列化需要根据 `Type` 字段选择输出 `text` 还是 `data`+`mimeType`。

## Error Handling

| 场景 | 处理方式 |
|------|----------|
| StackTrace 无错误日志 | 返回 Success + 提示文本 |
| Performance 指标获取失败 | 捕获异常，返回可用指标，不可用字段标记为 -1 |
| Screenshot 视图未打开 | 返回 Error + "视图未打开" |
| Selection 无选中对象 | 返回 Success + 空列表 JSON |
| Hierarchy 空场景 | 返回 Success + 空数组 |
| ProjectStructure Assets 目录不存在 | 返回 Error（理论上不会发生） |
| Inspector 无选中 GameObject | 返回 Success + 提示文本 |
| Inspector SerializedProperty 遍历异常 | 跳过异常字段，继续遍历 |
| Compile 超时 | 60 秒后返回 Error + 超时信息 |
| CompileErrors 无错误 | 返回 Success + 空列表 + 提示 |
| TestRunner 测试框架不可用 | 返回 Error + 提示信息 |

## Correctness Properties

### Property 1: Hierarchy 树遍历深度限制正确性

*For any* 任意深度的 GameObject 树结构和任意 maxDepth 值（≥0），HierarchyTool 返回的树结构中任意节点的深度应不超过 maxDepth。当 maxDepth=-1 时，返回的节点总数应等于场景中实际的 GameObject 总数。

**Validates: Requirements 5.1, 5.3, 5.4**

### Property 2: ProjectStructure .meta 文件过滤完整性

*For any* 包含任意数量 .meta 文件的目录结构，ProjectStructureTool 返回的结果中不应包含任何以 ".meta" 结尾的文件条目。

**Validates: Requirements 6.5**

### Property 3: ProjectStructure 目录遍历深度限制正确性

*For any* 任意深度的目录结构和任意 maxDepth 值（≥1），ProjectStructureTool 返回的树结构中任意条目的深度应不超过 maxDepth。

**Validates: Requirements 6.2**

### Property 4: 工具注册完整性与命名规范

*For any* 通过 ToolRegistry.AutoDiscover() 注册的工具集合，所有 10 个新工具应被发现并注册。每个工具的 Name 属性应匹配 `{Category}_{action}` 格式，且 InputSchema 应为合法 JSON 字符串。

**Validates: Requirements 11.1, 11.5, 11.6**

## Testing Strategy

### 单元测试（Unity Test Framework / NUnit）

每个工具一个测试类，覆盖正常路径和边界条件：

- **StackTraceTool**: 有错误日志时返回堆栈、无错误时返回提示
- **PerformanceTool**: 返回 JSON 包含必要字段
- **ScreenshotTool**: 参数默认值、视图未打开错误（需 mock）
- **SelectionTool**: 有选中/无选中、GameObject vs Asset
- **HierarchyTool**: 空场景、嵌套层级、maxDepth 限制
- **ProjectStructureTool**: .meta 过滤、深度限制、空目录
- **InspectorTool**: 有选中/无选中、多选取第一个
- **CompileTool**: 编译成功/失败、超时
- **CompileErrorsTool**: 有错误/无错误
- **TestRunnerTool**: 默认模式、过滤、结果摘要

### 属性测试

聚焦于纯逻辑层（树遍历、过滤、注册），通过构造测试用 GameObject 树或临时目录结构验证 property。

```
// Feature: debug-editor-build-add-ex, Property 1: Hierarchy 树遍历深度限制正确性
// Feature: debug-editor-build-add-ex, Property 2: ProjectStructure .meta 文件过滤完整性
// Feature: debug-editor-build-add-ex, Property 3: ProjectStructure 目录遍历深度限制正确性
// Feature: debug-editor-build-add-ex, Property 4: 工具注册完整性与命名规范
```
