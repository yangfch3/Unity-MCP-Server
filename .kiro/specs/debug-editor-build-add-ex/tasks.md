# Tasks

## Task 1: 扩展 ContentItem 支持 image 类型

- [x] 1.1 在 `Editor/Core/ToolResult.cs` 的 `ContentItem` 类中新增 `Data` 和 `MimeType` 可选属性，新增构造函数重载支持 image 类型
- [x] 1.2 在 `ToolResult` 中新增 `SuccessImage(string base64Data, string mimeType)` 静态工厂方法
- [x] 1.3 在 `Editor/Protocol/JsonRpcDispatcher.cs` 的 `HandleToolsCall` 响应序列化中，根据 `ContentItem.Type` 为 "image" 时输出 `data` + `mimeType` 字段而非 `text` 字段

## Task 2: 实现 debug 类工具

- [x] 2.1 创建 `Editor/Tools/StackTraceTool.cs`：通过 `Application.logMessageReceived` 捕获最近一条 Error/Exception 的 message + stackTrace + timestamp，Execute 返回 JSON 或"当前无错误日志"提示
- [x] 2.2 创建 `Editor/Tools/PerformanceTool.cs`：读取 `UnityEditor.UnityStats`、`Profiler.GetTotalAllocatedMemoryLong`、`Time.unscaledDeltaTime` 返回 fps/drawCalls/memoryUsedMB JSON
- [x] 2.3 创建 `Editor/Tools/ScreenshotTool.cs`：接受 view 参数（game/scene，默认 game），查找对应 EditorWindow 截图并返回 base64 PNG（使用 ToolResult.SuccessImage），视图未打开时返回错误。注意：优先使用 `InternalEditorUtility.ReadScreenPixel`（非公开 API），捕获 `MissingMethodException` 后回退到 `ScreenCapture.CaptureScreenshotAsTexture()` 或 `Texture2D.ReadPixels` 方案

## Task 3: 实现 editor 类工具

- [x] 3.1 创建 `Editor/Tools/SelectionTool.cs`：读取 `Selection.gameObjects` 和 `Selection.assetGUIDs`，返回 gameObjects（name/instanceID/path）和 assets（name/path）JSON，无选中时返回空列表
- [x] 3.2 创建 `Editor/Tools/HierarchyTool.cs`：接受 maxDepth 参数（默认 -1），递归遍历 `SceneManager.GetActiveScene().GetRootGameObjects()`，返回树结构 JSON（name/active/components/children）
- [x] 3.3 创建 `Editor/Tools/ProjectStructureTool.cs`：接受 maxDepth 参数（默认 3），递归遍历 `Application.dataPath` 目录，排除 .meta 文件，返回目录树 JSON（name/type/path/children）
- [x] 3.4 创建 `Editor/Tools/InspectorTool.cs`：读取 `Selection.activeGameObject` 的所有组件，通过 `SerializedObject`/`SerializedProperty` 遍历可见字段，返回组件和字段 JSON，无选中时返回提示，多选取第一个

## Task 4: 实现 build 类工具

- [x] 4.1 创建 `Editor/Tools/CompileTool.cs`：调用 `AssetDatabase.Refresh()` 触发编译，通过 `CompilationPipeline.compilationFinished` 回调等待完成（60s 超时），返回 success + errors JSON
- [x] 4.2 创建 `Editor/Tools/CompileErrorsTool.cs`：通过 `CompilationPipeline.assemblyCompilationFinished` 缓存最近编译错误，Execute 返回当前错误列表 JSON，无错误时返回空列表 + 提示
- [x] 4.3 创建 `Editor/Tools/TestRunnerTool.cs`：接受 mode（EditMode/PlayMode，默认 EditMode）和 testFilter 参数，通过 `TestRunnerApi` 执行测试，返回 summary + failures JSON
