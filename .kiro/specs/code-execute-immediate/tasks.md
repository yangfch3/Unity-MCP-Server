# Implementation Plan: code-execute-immediate

## Overview

Implement the `code_executeImmediate` MCP tool that dynamically compiles and executes C# code snippets inside the Unity Editor. The implementation follows the existing `IMcpTool` pattern, adds a ConfigPanel toggle, and includes comprehensive tests. The entire tool is guarded by `#if !UNITY_6000_OR_NEWER` for Unity 2022 (Mono) only.

## Tasks

- [x] 1. Implement ExecuteImmediateTool core
  - [x] 1.1 Create `Editor/Tools/ExecuteImmediateTool.cs` with IMcpTool interface implementation
    - Wrap entire file in `#if !UNITY_6000_OR_NEWER` / `#endif`
    - Namespace: `UnityMcp.Editor.Tools`
    - Implement `Name` ("code_executeImmediate"), `Category` ("code"), `Description`, `InputSchema` (required `code` string parameter)
    - Implement `Execute()` method skeleton: extract `code` param, check for null/empty, check EditorPrefs toggle
    - Add `private static readonly object _executionLock` for concurrency safety
    - Add `private const string PREF_KEY = "McpServer_CodeExecuteImmediate"`
    - _Requirements: 6.1, 6.2, 4.3, 5.1_

  - [x] 1.2 Implement compilation logic using CSharpCodeProvider
    - Create `CompileCode(string source)` method
    - Configure `CompilerParameters`: `GenerateInMemory = true`, `GenerateExecutable = false`
    - Reference all loaded assemblies from `AppDomain.CurrentDomain.GetAssemblies()`, skipping dynamic assemblies and those with empty `Location`
    - Format compiler errors with line numbers on failure
    - _Requirements: 1.1, 1.2, 1.3_

  - [x] 1.3 Implement entry point resolution
    - Create `FindEntryPoint(Assembly assembly)` method
    - Search all types for `public static void Run()` via reflection
    - Return error if no entry point found
    - _Requirements: 1.4_

  - [x] 1.4 Implement background thread execution with timeout and Console.Out capture
    - Create `RunWithTimeout(MethodInfo method, int timeoutMs)` method
    - Redirect `Console.Out` to a `StringWriter` before execution
    - Restore original `Console.Out` in a `finally` block
    - Run compiled code on a background thread (`thread.IsBackground = true`)
    - Use `thread.Join(timeout)` with 10-second timeout
    - Call `thread.Abort()` on timeout
    - Capture and return partial output on timeout or exception
    - Wrap entire compile-execute flow in `lock (_executionLock)`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 3.1, 3.2_

  - [x] 1.5 Implement structured JSON response builder
    - Create `BuildResponse(string output, string error)` method
    - Serialize JSON with exactly three fields: `success` (bool), `output` (string), `error` (string)
    - Use `MiniJson.SerializeString` for string field escaping
    - Return `ToolResult.Success(jsonString)` for all toggle-ON results
    - Return `ToolResult.Error(...)` only when toggle is OFF
    - _Requirements: 7.1, 7.2, 7.3_

- [x] 2. Modify ConfigPanel to add experimental toggle
  - [x] 2.1 Add toggle UI to `Editor/UI/ConfigPanel.cs`
    - Add `private const string CodeExecuteImmediatePrefKey = "McpServer_CodeExecuteImmediate"`
    - Add a `bool` field loaded from `EditorPrefs` in `OnEnable()`
    - Add toggle in `OnGUI()` wrapped in `#if !UNITY_6000_OR_NEWER`, placed after the existing Agent Configuration section
    - Label the toggle as experimental (e.g., "[Experimental] Code Execute Immediate")
    - Persist value changes to `EditorPrefs`
    - _Requirements: 4.1, 4.2, 4.4_

- [x] 3. Checkpoint — Verify core implementation compiles
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Write unit tests for ExecuteImmediateTool
  - [x] 4.1 Create `Tests/Editor/ExecuteImmediateToolTests.cs` with required unit tests
    - Wrap entire file in `#if !UNITY_6000_OR_NEWER`
    - Test Name == "code_executeImmediate" and Category == "code"
    - Test InputSchema parses correctly and declares required `code` parameter
    - Test ToolRegistry.AutoDiscover() finds and registers the tool
    - Test missing/null `code` parameter returns error response
    - Test toggle OFF returns `ToolResult.Error` with "disabled" message
    - Test compilation error (invalid C# code) returns `success: false` with line numbers in error
    - Test no `Run()` entry point returns `success: false` with "No entry point found"
    - Test successful execution with `Console.WriteLine("hello")` returns `success: true`, output contains "hello"
    - Test runtime exception returns `success: false`, error contains exception message, output contains prior output
    - Test timeout with `while(true){}` returns `success: false`, error contains "timed out" and "10"
    - _Requirements: 1.1, 1.2, 1.4, 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 4.3, 6.1, 6.2, 6.3, 7.1, 7.2, 7.3_

  - [x] 4.2 Add ExecuteImmediateTool to `Tests/Editor/ToolRegistryTests.cs` assertions
    - Add the tool name to the existing auto-discovery assertion list (wrapped in `#if !UNITY_6000_OR_NEWER`)
    - _Requirements: 6.3_

- [x] 5. Checkpoint — Ensure all unit tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 6. Write property-based tests (optional)
  - [ ]* 6.1 Create `Tests/Editor/ExecuteImmediateToolPropertyTests.cs` with property tests
    - Wrap entire file in `#if !UNITY_6000_OR_NEWER`
    - Tag all tests with `[Category("Slow")]`
    - **Property 1: Output capture round-trip** — Generate random strings, build valid C# with `Console.WriteLine(s)` in `Run()`, verify output contains the string and `success` is true (100 iterations)
    - **Validates: Requirements 1.1, 2.1, 2.2**
  - [ ]* 6.2 Write property test for Console.Out restoration invariant
    - **Property 2: Console.Out restoration invariant** — Execute various code paths (valid, invalid, exception-throwing), verify `Console.Out` is the same object before and after (100 iterations)
    - **Validates: Requirements 2.4**
  - [ ]* 6.3 Write property test for exception preserving partial output
    - **Property 3: Exception preserves partial output and reports error** — Generate random (message, output) pairs, build exception-throwing code, verify partial output and error (100 iterations)
    - **Validates: Requirements 2.3**
  - [ ]* 6.4 Write property test for toggle OFF rejection
    - **Property 4: Toggle OFF rejects all inputs** — Generate random code strings with toggle OFF, verify all rejected without compilation (100 iterations)
    - **Validates: Requirements 4.3**
  - [ ]* 6.5 Write property test for response format invariant
    - **Property 5: Response format invariant** — Execute with various inputs (valid, invalid, empty) with toggle ON, parse response JSON, verify exactly 3 fields with correct types (100 iterations)
    - **Validates: Requirements 7.1, 7.2, 7.3**

- [x] 7. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional property-based tests tagged `[Category("Slow")]` and can be skipped for faster MVP
- Unit tests in task 4 are required per project Testing Standards ("All new or modified Tools must include corresponding unit tests")
- Each task references specific requirements for traceability
- All new files must be wrapped in `#if !UNITY_6000_OR_NEWER` to exclude from Unity 6+ builds
- The tool uses `ToolResult.Success(jsonString)` for all toggle-ON responses (matching CompileTool pattern), and `ToolResult.Error(...)` only when toggle is OFF
