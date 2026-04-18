# Requirements Document

## Introduction

增强 Unity MCP Server 的日志查询与资产管理能力。具体包括：新增 `asset_deleteFolder` 工具用于删除指定 Asset 目录并刷新 AssetDatabase；新增 `console_clearLogs` 工具用于清空日志缓冲区；增强现有 `console_getLogs` 工具，支持按日志级别过滤、关键字搜索、上下文模式获取指定日志前后的记录；将日志缓冲区容量从 1000 扩容到 2500。

所有改动仅涉及 Tool 层面的通用实现，不涉及 Prompt 编排和 Editor 插件。

## Glossary

- **Console_Tool**: 现有的 `console_getLogs` MCP 工具，负责从内存环形缓冲区中读取 Unity Console 日志并返回给调用方。实现类为 `ConsoleTool`。
- **Asset_Delete_Folder_Tool**: 新增的 `asset_deleteFolder` MCP 工具，负责删除指定的 Asset 目录并触发 AssetDatabase 刷新。
- **Console_Clear_Tool**: 新增的 `console_clearLogs` MCP 工具，负责清空日志缓冲区。
- **Log_Buffer**: `ConsoleTool` 内部维护的内存环形缓冲区，用于存储通过 `Application.logMessageReceived` 捕获的日志条目。
- **Log_Entry**: 缓冲区中的单条日志记录，包含 Level（Error / Warning / Log）、Timestamp、Message 字段。返回给调用方时额外附带 Index 字段（全局递增的稳定 ID），用于上下文模式的 `beforeIndex` 参数。该 ID 在日志写入时分配，不随缓冲区淘汰而变化。
- **Log_Level**: 日志级别枚举值，取值为 "Error"、"Warning"、"Log" 三者之一。
- **AssetDatabase_Refresh**: Unity Editor API `AssetDatabase.Refresh()`，用于通知 Unity 重新扫描文件系统变更。
- **Tool_Registry**: 工具注册中心，通过反射自动发现所有 `IMcpTool` 实现并注册。
- **MCP_Tool**: 实现 `IMcpTool` 接口的工具类，由 Tool_Registry 自动发现和注册。

## Requirements

### Requirement 1: 删除 Asset 目录

**User Story:** As a developer, I want to delete a specified Asset directory via MCP tool, so that I can automate asset cleanup workflows (e.g., clearing generated code directories) without relying on Unity menu items that may be disabled during compile errors.

#### Acceptance Criteria

1. WHEN a valid `path` parameter is provided (relative to the project root directory, e.g. `Assets/XLua/Gen`) that points to an existing directory under the Unity Assets folder, THE Asset_Delete_Folder_Tool SHALL delete the directory and all its contents recursively, then invoke AssetDatabase_Refresh, and return a success message containing the deleted path.
2. WHEN the `path` parameter points to a directory that does not exist, THE Asset_Delete_Folder_Tool SHALL return an error message indicating the directory was not found.
3. WHEN the `path` parameter points to a location outside the Unity project's Assets folder, THE Asset_Delete_Folder_Tool SHALL return an error message indicating the path is invalid and refuse to perform the deletion.
4. WHEN the `path` parameter is empty or not provided, THE Asset_Delete_Folder_Tool SHALL return an error message indicating the path parameter is required.
5. THE Asset_Delete_Folder_Tool SHALL be auto-discovered by Tool_Registry without any manual registration.
6. THE Asset_Delete_Folder_Tool SHALL have the Name "asset_deleteFolder" and Category "editor".

### Requirement 2: 清空日志缓冲区

**User Story:** As a developer, I want to clear the log buffer via MCP tool, so that I can start a fresh log session before running a new test or workflow.

#### Acceptance Criteria

1. WHEN the Console_Clear_Tool is executed, THE Console_Clear_Tool SHALL remove all Log_Entry records from the Log_Buffer and return a success message.
2. WHEN the Console_Clear_Tool is executed on an already empty Log_Buffer, THE Console_Clear_Tool SHALL return a success message without error.
3. THE Console_Clear_Tool SHALL be auto-discovered by Tool_Registry without any manual registration.
4. THE Console_Clear_Tool SHALL have the Name "console_clearLogs" and Category "debug".

### Requirement 3: 按日志级别过滤

**User Story:** As a developer, I want to filter logs by level (Error / Warning / Log), so that I can quickly isolate error logs from the full log stream.

#### Acceptance Criteria

1. WHEN the `level` parameter is provided with a valid Log_Level value ("Error", "Warning", or "Log"), THE Console_Tool SHALL return only Log_Entry records whose Level field matches the specified value.
2. WHEN the `level` parameter is not provided, THE Console_Tool SHALL return Log_Entry records of all levels (preserving current default behavior).
3. WHEN the `level` parameter is provided with an invalid value, THE Console_Tool SHALL return an error message listing the valid Log_Level values.
4. THE Console_Tool SHALL apply the `level` filter in combination with the existing `count` parameter, returning at most `count` entries that match the specified level.
5. THE Console_Tool SHALL include an `index` field in each returned Log_Entry, representing a stable globally-incrementing ID assigned at write time, so that callers can use it as the `beforeIndex` parameter for context mode queries regardless of buffer eviction.

### Requirement 4: 按关键字过滤

**User Story:** As a developer, I want to filter logs by keyword, so that I can locate logs related to a specific module or feature by searching message content.

#### Acceptance Criteria

1. WHEN the `keyword` parameter is provided, THE Console_Tool SHALL return only Log_Entry records whose Message field contains the keyword as a case-insensitive substring.
2. WHEN the `keyword` parameter is not provided, THE Console_Tool SHALL return Log_Entry records without keyword filtering (preserving current default behavior).
3. THE Console_Tool SHALL apply the `keyword` filter in combination with the `level` filter and the `count` parameter, returning at most `count` entries that match all specified filters.

### Requirement 5: 上下文模式

**User Story:** As a developer, I want to retrieve N log entries before a specific log entry by index, so that I can understand the execution context leading up to an error or notable event.

#### Acceptance Criteria

1. WHEN the `beforeIndex` parameter is provided with a valid zero-based index within the current Log_Buffer, THE Console_Tool SHALL use that index as the anchor point for context retrieval.
2. WHEN both `beforeIndex` and `count` parameters are provided, THE Console_Tool SHALL return up to `count` Log_Entry records immediately preceding the entry at `beforeIndex` (exclusive of the anchor entry itself), plus the anchor entry, ordered chronologically.
3. WHEN the `beforeIndex` value exceeds the current Log_Buffer size, THE Console_Tool SHALL return an error message indicating the index is out of range.
4. WHEN the `beforeIndex` value is negative, THE Console_Tool SHALL return an error message indicating the index is invalid.
5. WHEN `beforeIndex` is provided and the number of entries before the anchor is less than `count`, THE Console_Tool SHALL return all available entries from the beginning of the buffer up to and including the anchor entry.
6. WHEN `beforeIndex` is provided together with `level` or `keyword` filters, THE Console_Tool SHALL ignore the `level` and `keyword` filters and operate in pure context mode, returning unfiltered entries around the anchor point.

### Requirement 6: 扩容日志缓冲区

**User Story:** As a developer, I want the log buffer to hold more entries, so that the window between an error occurring and AI analysis is long enough to preserve the error and its surrounding context.

#### Acceptance Criteria

1. THE Console_Tool SHALL maintain a Log_Buffer with a maximum capacity of 2500 Log_Entry records.
2. WHEN the Log_Buffer reaches maximum capacity and a new Log_Entry is received, THE Console_Tool SHALL remove the oldest entry to make room for the new entry (ring buffer behavior preserved).
