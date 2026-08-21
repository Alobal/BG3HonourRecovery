# v1.0.0

首个可发布版本。

## 功能

- 扫描 `profile8.lsf` 中的 `DisabledSingleSaveSessions` GUID。
- 显示匹配的 `HonourMode.WebP` 缩略图，单击缩略图打开原文件。
- 支持多选恢复与一键全部恢复。
- 自动备份默认开启，可由用户关闭。
- Windows x64 自包含单文件，无需单独安装 .NET 或 LSLib。

## 数据安全

- 只删除用户选中的失败战役记录，并校验未选择记录的数量不变。
- 先写入同目录临时文件并重新解析，随后原子替换原文件。
- 替换前检测游戏或云同步造成的外部文件改写。
- 最终校验失败时自动回滚；回滚失败时保留恢复文件并报告路径。

## 校验

下载后可在 PowerShell 中运行：

```powershell
Get-FileHash .\BG3HonourRecovery.exe -Algorithm SHA256
```

将结果与 Release 附件 `SHA256SUMS.txt` 比较。
