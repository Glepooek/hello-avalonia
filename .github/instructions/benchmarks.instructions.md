---
applyTo: "benchmarks/**"
---

# 性能基准规范（BenchmarkDotNet）

配套文档：`docs/2026-10-18-benchmarkdotnet-standards.md`。基准是性能测试，不作为功能回归的阻断门禁。

- 基准不是单元测试：禁止在测试中对耗时做硬断言。
- 独立 `benchmarks/` 项目，使用 Release 在命令行运行，不挂调试器；不要手动设置 `<Optimize>`，`Directory.Build.props` 已按配置处理。
- 基准方法必须使用或返回结果，防止 JIT 死代码消除。
- 命名：类为 `<被测类型>Benchmarks`，方法与被测成员同名（构造用 `Constructor`）；同一成员的多个场景使用 `[Params]`，不要加方法名后缀。
- 会修改状态的被测对象优先每次新建并扣除构造基线；`[IterationSetup]` 仅作最后手段。
- 结果与运行时、架构、OS、JIT 绑定，不要外推。
- 回归报告使用 `[JsonExporterAttribute.FullCompressed]`，接入 `benchmark-action/github-action-benchmark`（`tool: benchmarkdotnet`）。
- 门禁必须显式设置阈值：`alert-threshold` 默认 200%，`fail-on-alert` 默认 false，否则永远不会失败。
- 托管 runner 噪声约 ±10–20%，阈值不要低于此；不稳定的机器只告警不失败。
- 不要在 `pull_request` 事件上运行基准或推送 gh-pages。
