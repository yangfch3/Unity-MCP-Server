# 常见错误与陷阱

AI agent 在本工作区反复犯过的错误，执行命令或写代码前先过一遍。

## PowerShell

- 禁止用 Win PowerShell 的 `Set-Content` 编辑含中文的 UTF-8 文件，请使用 agent 内置工具或编码安全工具操作文件内容
