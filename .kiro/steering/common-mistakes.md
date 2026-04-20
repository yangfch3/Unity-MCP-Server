---
inclusion: always
---

# Common Mistakes & Pitfalls

A living document of recurring mistakes the AI agent makes in this workspace. Check this before executing commands or writing code.

## Shell Commands

- **Do NOT use `&&` to chain commands in PowerShell.** This workspace runs on Windows with PowerShell, where `&&` is not a valid statement separator. Use `;` instead, or run commands separately.
  - Bad: `git add -A && git commit -m "msg"`
  - Good: `git add -A; git commit -m "msg"` or run as two separate commands

- **Do NOT use PowerShell `Set-Content` / `Get-Content` for UTF-8 files with CJK characters.** PowerShell's default encoding mangles multi-byte characters. Use the agent's like `fsWrite` / `strReplace` / `edit_file` / `replace_in_file` tools instead for any file content modifications.

## Unity / C# Runtime

- **`CSharpCodeProvider` (mcs) 隐式引用 `mscorlib.dll`。** `CompileCode` 遍历 `AppDomain.CurrentDomain.GetAssemblies()` 时必须跳过 `mscorlib.dll`，否则编译器会报 CS0433 "The imported type is defined multiple times"（`System.Console`、`System.Object` 等核心类型重复）。

- **`Application.dataPath` 返回正斜杠路径。** Unity 的 `Application.dataPath` 在 Windows 上返回 `D:/Project/Assets`（`/` 分隔符），而 `Path.GetFullPath()` 返回 `D:\Project\Assets`（`\` 分隔符）。做路径前缀匹配时，两侧都要经过 `Path.GetFullPath()` 规范化，否则 `StartsWith` 会失败。

