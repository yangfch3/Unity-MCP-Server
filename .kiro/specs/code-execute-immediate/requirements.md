# Requirements Document

## Introduction

Add an experimental MCP tool `code_executeImmediate` that compiles and executes dynamically submitted C# code snippets inside the Unity Editor. This enables AI agents to run arbitrary C# logic (e.g., batch scene modifications, asset queries, custom inspections) without creating persistent script files. The feature uses Mono's `CSharpCodeProvider` for runtime compilation and reflection for invocation, and is therefore restricted to Unity 2022 (Mono backend). A ConfigPanel toggle (default OFF) serves as the primary safety gate.

## Glossary

- **Execute_Immediate_Tool**: The `code_executeImmediate` MCP tool that accepts C# source code, compiles it at runtime, and executes it within the Unity Editor process.
- **Code_Provider**: The `Microsoft.CSharp.CSharpCodeProvider` class used to compile C# source code into an in-memory assembly at runtime.
- **Config_Toggle**: The EditorPrefs-backed boolean flag in ConfigPanel that enables or disables the Execute_Immediate_Tool. Default value is OFF (false).
- **Execution_Timeout**: The maximum wall-clock duration (10 seconds) allowed for a single code execution before the tool forcibly aborts it.
- **Captured_Output**: Text written to `System.Console.Write` / `System.Console.WriteLine` during code execution, redirected and collected as the tool's `output` field.
- **Tool_Response**: The structured JSON object `{success, output, error}` returned by the Execute_Immediate_Tool after each invocation.
- **ConfigPanel**: The `EditorWindow`-based IMGUI settings panel (`Window > MCP Server`) that displays server status and configuration options.

## Requirements

### Requirement 1: Dynamic C# Code Compilation

**User Story:** As an AI agent, I want to submit C# source code and have it compiled at runtime, so that I can execute arbitrary Editor logic without creating persistent script files.

#### Acceptance Criteria

1. WHEN a `code` parameter containing valid C# source code is provided, THE Execute_Immediate_Tool SHALL compile the source code into an in-memory assembly using Code_Provider (`CSharpCodeProvider.CompileAssemblyFromSource`).
2. WHEN the Code_Provider produces compilation errors, THE Execute_Immediate_Tool SHALL return a Tool_Response with `success` set to false and the `error` field containing all compiler error messages including line numbers.
3. THE Execute_Immediate_Tool SHALL reference the assemblies currently loaded in the Unity Editor's AppDomain so that compiled code can access UnityEngine, UnityEditor, and project types.
4. WHEN compilation succeeds, THE Execute_Immediate_Tool SHALL invoke the compiled entry point via reflection.

### Requirement 2: Execution Output Capture

**User Story:** As an AI agent, I want to receive the console output and any errors from executed code, so that I can interpret the results programmatically.

#### Acceptance Criteria

1. WHILE code is executing, THE Execute_Immediate_Tool SHALL redirect `System.Console.Out` to a `StringWriter` and collect all text written via `Console.Write` / `Console.WriteLine` as Captured_Output.
2. WHEN execution completes without exceptions, THE Execute_Immediate_Tool SHALL return a Tool_Response with `success` set to true, `output` set to the Captured_Output, and `error` set to an empty string.
3. WHEN execution throws an unhandled exception, THE Execute_Immediate_Tool SHALL return a Tool_Response with `success` set to false, `output` set to any Captured_Output collected before the exception, and `error` set to the exception message and stack trace.
4. THE Execute_Immediate_Tool SHALL restore the original `System.Console.Out` after each execution regardless of success or failure.

### Requirement 3: Execution Timeout

**User Story:** As a developer, I want code execution to be time-bounded, so that an infinite loop or long-running snippet does not permanently block the Unity Editor main thread.

#### Acceptance Criteria

1. WHEN code execution exceeds the Execution_Timeout of 10 seconds, THE Execute_Immediate_Tool SHALL abort the execution.
2. WHEN execution is aborted due to timeout, THE Execute_Immediate_Tool SHALL return a Tool_Response with `success` set to false, `output` set to any Captured_Output collected before the timeout, and `error` containing a timeout message that includes the Execution_Timeout duration.

### Requirement 4: ConfigPanel Experimental Toggle

**User Story:** As a developer, I want an opt-in toggle in ConfigPanel to enable this experimental feature, so that it is disabled by default and cannot be accidentally invoked.

#### Acceptance Criteria

1. THE ConfigPanel SHALL display a Config_Toggle labeled as experimental for the Execute_Immediate_Tool, with a default value of OFF (false).
2. THE Config_Toggle SHALL persist its value using `EditorPrefs`.
3. WHEN the Config_Toggle is OFF and the Execute_Immediate_Tool receives a request, THE Execute_Immediate_Tool SHALL return a Tool_Response with `success` set to false and `error` indicating the feature is disabled.
4. THE Config_Toggle UI element in ConfigPanel SHALL be wrapped in `#if !UNITY_6000_OR_NEWER` so that it is not displayed in Unity 6+.

### Requirement 5: Unity Version Guard

**User Story:** As a developer, I want the tool to compile only on Unity 2022 (Mono), so that it does not cause build errors on Unity 6+ (CoreCLR) where `CSharpCodeProvider` is unavailable.

#### Acceptance Criteria

1. THE Execute_Immediate_Tool class SHALL be wrapped in `#if !UNITY_6000_OR_NEWER` preprocessor directives so that the entire class is excluded from compilation on Unity 6+.
2. WHEN compiled on Unity 6+, THE Execute_Immediate_Tool class SHALL not exist in the assembly, and THE ToolRegistry SHALL not discover or register it.

### Requirement 6: Tool Interface Compliance

**User Story:** As a developer, I want the new tool to follow the existing `IMcpTool` pattern, so that it integrates seamlessly with ToolRegistry auto-discovery and the MCP protocol.

#### Acceptance Criteria

1. THE Execute_Immediate_Tool SHALL implement the `IMcpTool` interface with Name `"code_executeImmediate"` and Category `"code"`.
2. THE Execute_Immediate_Tool SHALL define an InputSchema that declares a required `code` parameter of type string.
3. THE Execute_Immediate_Tool SHALL be auto-discovered by `ToolRegistry.AutoDiscover()` via reflection without any manual registration.

### Requirement 7: Tool Response Format

**User Story:** As an AI agent, I want a consistent structured JSON response, so that I can reliably parse execution results.

#### Acceptance Criteria

1. THE Execute_Immediate_Tool SHALL return Tool_Response as a JSON string with exactly three fields: `success` (boolean), `output` (string), and `error` (string).
2. WHEN execution succeeds, THE Execute_Immediate_Tool SHALL return `success` as true, `output` as the Captured_Output, and `error` as an empty string.
3. WHEN execution fails due to compilation errors, timeout, or runtime exceptions, THE Execute_Immediate_Tool SHALL return `success` as false with the relevant error details in the `error` field.
