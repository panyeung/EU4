# Console 基线归因表

更新 2026-11-17。对应 `docs/实现规格-v1.0.md` 的 T17。目的：把 Editor Console 里的既有 error 逐条定性，下次不必重查，也不要把测试预期输出当成缺陷重立卡。

读者：任何要在本工程里判断「这次是不是新增了错误」的人。

## 1. 怎么用这张表

1. 跑任何证据之前，先记一次 `unity command console_status --project-path "C:/Users/panye/My project"`，把 `error` 数当基线。
2. 记录时写明取的是 `counts` 还是 `groundTruth`。两者当前差 1（见第 3 节），混用会算出假的「新增 1 条」。
3. 结束时再记一次。差值里剔除第 4 节列的两类工具噪声，剩下的才是真正的新增 error。

## 2. 基线七条（2026-11-17 16:12 采样）

采样点：`Logs/verify/gate-20261118/report.md` 第 C2 节，`counts = {"error":7,"warn":53,"log":27}`，`cursor=87`，`session=bda41b5ea9764200811a3f34d5c7c33c`。

| seq | 首行 | 定性 | 依据 |
|---|---|---|---|
| 17 | `ExecuteCommandByName: No command named 'list' is available.` | 工具噪声 | 历史上把 Pipeline bridge 当旧 CLI 用留下的残留。pipeline 只认 `unity command <子命令>`，`list` / `tests` / `scripts` 都不是合法子命令 |
| 18 | `…'tests' is available.` | 工具噪声 | 同上 |
| 19 | `…'scripts/compile' is available.` | 工具噪声 | 同上 |
| 20 | `…'tests' is available.` | 工具噪声 | 同上 |
| 21 | `…'scripts' is available.` | 工具噪声 | 同上 |
| 66 | `家族编年史记录写入失败（家族收养）：编年史预备记录序号无效。` | 负向测试的预期输出 | 见第 5 节 |
| 67 | `家族编年史记录写入失败（家族婚姻）：编年史预备记录序号无效。` | 负向测试的预期输出 | 见第 5 节 |

结论：**基线七条里没有一条是真实缺陷**。游戏逻辑的既有 error 数为 0。

`warn` 53 条绝大部分是各 UI updater 的 `rolled back …. Scene may remain dirty` 测试告警，属 updater 正常回滚路径，不是问题。

## 3. counts 与 groundTruth 差 1

同一时刻 `groundTruth.consoleErrors = 6`、`consoleWarnings = 52`，`counts` 为 7 和 53。差的 1 条是 pipeline 自己写入的 `seq=2`（`CS0618` 弃用警告，带 `seeded:true`）。

规则：写「新增 error = N」时注明来源。跨时点比较只用同一个来源。

## 4. 必须剔除的两类工具噪声

1. **pipeline 子命令拼错**（第 2 节 seq 17–21 那一类）。看到 `No command named …` 就是驱动侧拼错，不是游戏问题。
2. **`run_tests` 客户端超时产生的传输层 error**。测试耗时约 198s 时，180s 的客户端超时先断开，Editor 侧会记两条 `Pipeline: unhandled error serving POST /api/exec: System.InvalidOperationException: Cannot be changed after headers are sent.`（2026-11-17 那次是 `seq=157/158`）。正确做法是 `run_tests --detach` 再轮询 `test_status`，从源头避免。

## 5. seq 66 / 67 的完整证据链

这两条曾经被 `Logs/verify/gate-20261118/report.md` 的「风险与遗留」第 1 条判为「真实代码路径缺陷，建议单独立卡」。**这个判断不成立**，本文取代它。该报告是当时的证据快照，不改写，以本文为准。

- 产生点：`Assets/Scripts/Domain/HistoryLedger.cs:175` 把 `error = "编年史预备记录序号无效。"` 作为 `TryAppendFamilyFormation` 返回失败时的理由。
- 注入点：`Assets/Scripts/Application/GameSession.cs:506` 定义 `internal Action<HistoryLedger> FamilyFormationAppendFaultInjection`，`:1719` 与 `:1821` 在写入前 `?.Invoke(...)`。
- 只有两处赋值：`Assets/Tests/EditMode/FamilyFormationSessionTests.cs:271`（婚姻）与 `:316`（收养）。两处都在用前一条 `LogAssert.Expect(LogType.Error, "家族编年史记录写入失败（家族婚姻/收养）：编年史预备记录序号无效。")` 声明这条错误，断言的是「写编年史失败时成员、亲属、状态整体回滚」。
- 两个用例结束后都把注入置回 `null`（`:300` 与 `:345`）。
- 运行期没有任何生产代码路径赋值这个属性，因此只有跑 EditMode 时才会出现这两条。

判定：负向测试的预期输出。断言覆盖的是回滚语义，不是缺陷。

## 6. 检查手段

```bash
# 基线
unity command console_status --project-path "C:/Users/panye/My project"
# 按光标增量读取，把新增条目逐条归类
unity command console --since <cursor> --since_session <session> --project-path "C:/Users/panye/My project"
```

跑完把增量原文复制到 `Logs/verify/<stamp>/console-after.txt`，按第 2 节与第 4 节归类，再算新增。不要只凭 `counts` 下结论。
