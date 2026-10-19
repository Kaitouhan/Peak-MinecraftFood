# 发布到 Thunderstore

上传文件：`MinecraftFoods-1.26.0-Thunderstore.zip`。

- 命名空间：`Kaitouhan`
- 包名：`MinecraftFoods`
- 版本：`1.26.0`
- 网站链接：`https://github.com/Kaitouhan/Peak-MinecraftFood`

上传前在 Thunderstore 的页面预览 README，并确认依赖自动列出：BepInExPack_PEAK、PEAKLib Core、PEAKLib Items、MonoDetour、loaforcsSoundAPI 2.0.13。


发布时保持同一团队与包名；已发布版本不能覆盖，后续更新须提高版本号。


本地候选版尚需游戏内与双人联机验收。上传前运行 Check.ps1 和 Package-Thunderstore.ps1；包外的 release-record.json 用于核对构建来源和 SHA256，不需要放进 Thunderstore 包。GitHub 推送需用户明确批准该次上传。

模组介绍正文目前统一为英文。包内仅带 settings.example.cfg，绝不能带开发者自己的 settings.cfg 覆盖用户设置。候选版追加三语言与 F10 设置，运行时交互尚待实测。
