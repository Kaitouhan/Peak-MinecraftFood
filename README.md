# Minecraft Foods for PEAK

> 将 Minecraft Java 1.21.1 的 34 种食物带进 PEAK 的独立食物模组。

![Minecraft Foods cover](docs/icon.png)

**版本：1.0.0**  
**PEAK：v0.26.6b 测试**  
**多人：房主与所有玩家应安装相同版本。**

## 内容

- **34 种食物**：苹果、面包、牛奶桶、蛋糕、肉类、鱼类、农作物、汤类与稀有食物。
- **Minecraft 风格营养**：每种食物依据 Minecraft 的饥饿值与饱和度折算为 PEAK 回复量。
- **独立烹饪规则**：生肉和生鱼首次烹饪会在原物品栏槽位变为对应熟食；各食物有独立的烧焦与烧毁阈值。
- **特殊效果**：河豚中毒、紫颂果随机传送、金苹果短暂无敌及结束后的轻微反作用等效果按食物分别配置。
- **普通箱子掉落**：按稀有度和权重混入所有普通箱子的物品位置；默认总份额为 25%，可由房主配置。
- **火焰与过度烹饪**：所有食物接入 PEAK 的烹饪和烧毁流程。烧焦会改变颜色，过度烹饪会累积中毒风险。
- **原版风格资源**：使用 Jonesy__07 的 Minecraft Eating 1.0.3 所含 `MCEat.mp3` 作为成功食用音效；其它第三方素材权利归其原权利人。

## 食物效果举例

| 食物 | 主要效果 | 烹饪处理 |
| --- | --- | --- |
| 生牛肉 / 牛排 | 生肉营养较低，烤一次变牛排 | 原槽位原地转换，模型、名称、图标和数值同步变化 |
| 生鳕鱼 / 熟鳕鱼、 生鲑鱼 / 熟鲑鱼 | 熟食回复提高 | 首次烹饪变熟，继续加热会进入独立烧焦阶段 |
| 河豚 | 中毒及其它负面状态 | 不会通过烹饪变为安全食物，继续烹饪会增加毒性 |
| 紫颂果 | 随机传送 | 过熟后保留风险且增加中毒 |
| 金苹果 / 附魔金苹果 | 强力临时正面效果 | 过度烹饪会失去正面效果，并提高风险 |
| 腐肉、蜘蛛眼等 | 各自的负面状态 | 每种有不同变毒阈值，不使用统一规则 |

完整数值、概率和阈值见 [`assets/catalogue.json`](assets/catalogue.json)。

## 前置模组

Thunderstore 会自动安装以下依赖：

- `BepInEx-BepInExPack_PEAK` 5.4.2403
- `PEAKModding-PEAKLib_Core` 1.7.2
- `PEAKModding-PEAKLib_Items` 1.6.2
- `MonoDetour-MonoDetour_BepInEx_5` 0.6.7

## 安装

### Thunderstore Mod Manager

在 Thunderstore 中安装 `Kaitouhan-MinecraftFoods`；前置模组会自动处理。房主和加入者都应使用相同版本。

### 手动安装

1. 安装上方前置模组并至少运行一次 PEAK。
2. 将发行包 `plugins/MinecraftFoods` 复制到 `PEAK/BepInEx/plugins/MinecraftFoods`。
3. 启动游戏。首次启动会生成 `BepInEx/config/codex.peak.minecraftfoods.cfg`。

## 配置

- `Loot.MinecraftShare`：每个普通箱子掉落位置被 Minecraft 食物占用的总份额，默认 `0.25`。
- `Visuals.GripTemplate`：默认 `TrailMix`；可选 `SmallEgg` 以适配不同的拿取姿势。
- `Effects.AllowFoodEffectsInAirport`：机场测试区是否允许本模组食物效果。
- `Audio.MinecraftEatingSounds`：是否播放 `MCEat.mp3` 进食音效。

改动掉率或握持模板后请重启游戏。多人由房主的掉率配置决定。

## 反馈与二次开发

源码、构建说明和问题反馈入口：<https://github.com/Kaitouhan/Peak-MinecraftFood>

欢迎提交 Issue，说明 PEAK 版本、模组版本、复现步骤，以及 `BepInEx/LogOutput.log` 中相关片段。修改后可提交 Pull Request。

## 从源码构建

需要：.NET SDK、PEAK 安装目录、上述 BepInEx 与 PEAKLib 前置模组。

```powershell
./Build.ps1 -GamePath 'E:\SteamLibrary\steamapps\common\PEAK' -DotnetPath dotnet
```

输出 DLL 位于 `src/bin/Release/netstandard2.1/MinecraftFoods.dll`。请勿将 PEAK 的程序集或个人配置提交到仓库。

## 致谢与声明

- PEAK 为 Landfall 的游戏；本项目是非官方社区模组。
- Minecraft 及其原版资源相关权利归 Mojang Studios / Microsoft。本模组与 Mojang Studios、Microsoft 或 Landfall 均无隶属关系。
- 代码使用 [MIT License](LICENSE)；第三方贴图与音效不因本仓库而改变其原有权利归属。


