---
paths:
  - "benchmarks/**"
---

# 性能基准规范（BenchmarkDotNet）

配套文档（含出处与置信度）：`docs/2026-10-18-benchmarkdotnet-standards.md`。基准是性能测试，但不进功能回归的阻断门禁。

- 基准不是单元测试：**禁止**在测试里对耗时做硬断言。
- 独立 `benchmarks/` 项目，`Release` 命令行运行，不挂调试器；不要硬写 `<Optimize>`——`Directory.Build.props` 已按配置设置。
- 基准方法必须使用或返回结果，防止 JIT 死代码消除。
- 结果绑定运行时/架构/OS/JIT，不外推。
- 报告回归用 `[JsonExporterAttribute.FullCompressed]` 生成 `*-report-full-compressed.json`，接 `benchmark-action/github-action-benchmark`（`tool: benchmarkdotnet`）。
- 门禁必须显式设阈值：默认 `alert-threshold` 200%、`fail-on-alert` false，不设置就永远不会失败。
- 托管 runner 噪声约 ±10~20%，阈值不要低于此；不稳定机器只告警不失败。
- 不在 `pull_request` 事件上运行并推送 gh-pages。
