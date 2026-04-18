# Implementation Plan: mcp-console-asset-tools

## Overview

增强 Unity MCP Server：新增 `asset_deleteFolder` 和 `console_clearLogs` 两个工具，增强 `console_getLogs`（级别过滤、关键字搜索、上下文模式），扩容日志缓冲区至 2500。所有改动位于 `Editor/Tools/` 和 `Tests/Editor/`，遵循现有 `IMcpTool` 模式。

## Tasks

- [x] 1. Expand log buffer and add index field to ConsoleTool
  - [x] 1.1 Update `MaxBufferSize` from 1000 to 2500 in `Editor/Tools/ConsoleTool.cs`
    - Change `private const int MaxBufferSize = 1000;` to `2500`
    - _Requirements: 6.1, 6.2_

  - [x] 1.2 Add `index` field to ConsoleTool JSON output
    - In the `Execute` method's JSON serialization loop, include `"index":{idx}` for each entry (the entry's position in `_buffer`)
    - _Requirements: 3.5_

  - [x] 1.3 Write property test for buffer capacity invariant
    - **Property 4: Buffer capacity invariant with FIFO eviction**
    - Inject N random entries (2501–5000) via `InjectLog`, verify `BufferCount == 2500` and buffer contains only the most recent 2500 entries
    - Use `[Category("Slow")]` attribute, 100 iterations with random data
    - **Validates: Requirements 6.1, 6.2**

- [x] 2. Implement console_getLogs filter mode (level + keyword)
  - [x] 2.1 Update `InputSchema` to include `level`, `keyword`, and `beforeIndex` parameters
    - Update the `InputSchema` property string in `ConsoleTool` to match the design's JSON schema
    - _Requirements: 3.1, 4.1, 5.1_

  - [x] 2.2 Implement filter mode in `Execute` method
    - Parse `level` and `keyword` parameters from the input dictionary
    - Validate `level` against valid values ("Error", "Warning", "Log"); return error for invalid values
    - Scan buffer from tail to head, collecting entries matching level and keyword (case-insensitive substring) filters, up to `count` entries
    - Return results in chronological order with `index` field
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3_

  - [x] 2.3 Write property test for combined log filtering invariant
    - **Property 2: Combined log filtering invariant**
    - Generate random buffer (random levels, random messages) + random level/keyword/count combinations
    - Verify: result.length ≤ count, all entries match level (if specified), all entries contain keyword case-insensitively (if specified)
    - Use `[Category("Slow")]` attribute, 100 iterations
    - **Validates: Requirements 3.1, 3.4, 4.1, 4.3**

- [x] 3. Implement console_getLogs context mode (beforeIndex)
  - [x] 3.1 Implement context mode branch in `Execute` method
    - Parse `beforeIndex` parameter; when present, enter context mode (ignore `level` and `keyword`)
    - Validate: `beforeIndex < 0` → error; `beforeIndex >= buffer.Count` → error
    - Compute slice: `start = max(0, beforeIndex - count + 1)`, `end = beforeIndex` (inclusive)
    - Return `buffer[start..end]` with `index` field, in chronological order
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

  - [x] 3.2 Write property test for context mode slice correctness
    - **Property 3: Context mode returns correct contiguous slice**
    - Generate random buffer + random valid `beforeIndex` + random `count`
    - Verify returned slice equals `buffer[max(0, beforeIndex - count + 1) .. beforeIndex]` inclusive, ordered chronologically
    - Use `[Category("Slow")]` attribute, 100 iterations
    - **Validates: Requirements 5.1, 5.2, 5.5, 5.6**

  - [x] 3.3 Write unit tests for ConsoleTool enhancements in `Tests/Editor/ConsoleToolTests.cs`
    - Test: no `level` param returns all levels
    - Test: invalid `level` returns error with valid values listed
    - Test: `beforeIndex` negative returns error
    - Test: `beforeIndex` out of range returns error
    - Test: context mode ignores `level` and `keyword` params
    - Test: keyword filter is case-insensitive
    - Test: combined level + keyword + count filtering
    - _Requirements: 3.1, 3.2, 3.3, 4.1, 4.2, 5.3, 5.4, 5.6_

- [x] 4. Checkpoint — Verify ConsoleTool enhancements
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Implement AssetDeleteFolderTool
  - [x] 5.1 Create `Editor/Tools/AssetDeleteFolderTool.cs`
    - Implement `IMcpTool` interface with Name `"asset_deleteFolder"`, Category `"editor"`
    - Implement `ValidatePath`: resolve path relative to `Path.GetDirectoryName(Application.dataPath)`, use `Path.GetFullPath` to normalize, verify resolved path starts with `Application.dataPath`
    - Implement `Execute`: validate empty path → error; validate path safety → error; check directory exists → error; `Directory.Delete(fullPath, true)` + `AssetDatabase.Refresh()` → success; wrap in try-catch for filesystem exceptions
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_

  - [x] 5.2 Write property test for path safety validation
    - **Property 1: Path safety validation**
    - Generate 100 random path strings containing `..`, absolute paths, special characters
    - For each path that after normalization does not resolve under Assets directory, verify the tool returns error without performing deletion
    - Use `[Category("Slow")]` attribute
    - **Validates: Requirements 1.3**

  - [x] 5.3 Write unit tests for AssetDeleteFolderTool in `Tests/Editor/AssetDeleteFolderToolTests.cs`
    - Test: Name and Category properties are correct
    - Test: empty path returns error
    - Test: non-existent directory returns error
    - Test: path traversal (`../../etc`) returns error
    - Test: auto-discovery via ToolRegistry finds `"asset_deleteFolder"`
    - _Requirements: 1.2, 1.3, 1.4, 1.5, 1.6_

- [x] 6. Implement ConsoleClearTool
  - [x] 6.1 Create `Editor/Tools/ConsoleClearTool.cs`
    - Implement `IMcpTool` interface with Name `"console_clearLogs"`, Category `"debug"`
    - `Execute` calls `ConsoleTool.ClearBuffer()` and returns `Success("Log buffer cleared.")`
    - InputSchema: empty object `{}`
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [x] 6.2 Write unit tests for ConsoleClearTool in `Tests/Editor/ConsoleClearToolTests.cs`
    - Test: Name and Category properties are correct
    - Test: clearing non-empty buffer results in BufferCount == 0 and returns success
    - Test: clearing already-empty buffer returns success without error
    - Test: auto-discovery via ToolRegistry finds `"console_clearLogs"`
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [x] 7. Update ToolRegistry tests and final wiring
  - [x] 7.1 Update `Tests/Editor/ToolRegistryTests.cs` to include new tools
    - Add `"asset_deleteFolder"` to `AutoDiscover_FindsAllExpectedTools` assertions
    - Add `"console_clearLogs"` to `AutoDiscover_FindsAllExpectedTools` assertions
    - Update `Assert.GreaterOrEqual` count from 13 to 15
    - Add `"asset_deleteFolder"` to `ListByCategory_Editor` assertions
    - Add `"console_clearLogs"` to `ListByCategory_Debug` assertions
    - _Requirements: 1.5, 1.6, 2.3, 2.4_

- [x] 8. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Each task references specific requirements for traceability
- Property tests use `[Category("Slow")]` NUnit attribute for selective CI execution
- All new tool files follow existing `IMcpTool` pattern — auto-discovered by `ToolRegistry`, no manual registration needed
- `ConsoleClearTool` reuses `ConsoleTool.ClearBuffer()` (internal static, same assembly)
- Test files go in `Tests/Editor/` matching existing naming convention
