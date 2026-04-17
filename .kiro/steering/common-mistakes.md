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
