---
inclusion: always
---

# Common Mistakes & Pitfalls

A living document of recurring mistakes the AI agent makes in this workspace. Check this before executing commands or writing code.

## Shell Commands

- **Do NOT use `&&` to chain commands in PowerShell.** This workspace runs on Windows with PowerShell, where `&&` is not a valid statement separator. Use `;` instead, or run commands separately.
  - Bad: `git add -A && git commit -m "msg"`
  - Good: `git add -A; git commit -m "msg"` or run as two separate commands
