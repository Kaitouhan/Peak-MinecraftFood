# 版本管理与回退

## 固定版本

| 标签 | 用途 |
|---|---|
| `v1.25.0` | 已有1.25.0历史源码 |
| `v1.25.2` | 升级前的1.25.2源码，含与模组页面一致的介绍 |
| `v1.26.0` | 本次多语言、设置、烹饪平衡及睡眠更新 |

`main` 保存最新开发结果。发布标签创建后不移动、不覆盖，不强制推送历史。后续修复使用新版本号与新标签。

## 下载与安装回退

在 GitHub Releases 选择所需版本。安装游戏模组请使用 `MinecraftFoods-版本-Thunderstore.zip` 附件；自动生成的 Source code ZIP 是源码，不可直接作为插件安装。历史标签若没有编译包，需要用对应标签源码构建。

退出PEAK，备份当前MinecraftFoods插件目录及配置，再用目标版本的插件目录替换当前MinecraftFoods目录。不要同时保留两份DLL，不要删除其他模组。旧版配置路径可能与1.26.0不同，使用对应版本配置；保留备份以便恢复。

## 安全取回旧版源码

在新目录检出旧版本，不覆盖有未提交改动的工作目录：

```powershell
git clone --branch v1.25.2 https://github.com/Kaitouhan/Peak-MinecraftFood.git MinecraftFoods-v1.25.2
cd MinecraftFoods-v1.25.2
git switch -c local-fix-v1.25.2
```

根据该版本构建说明运行Build.ps1；需要本机PEAK及前置模组引用。取回当前发布版时将标签改为`v1.26.0`。

已发布更新如需整体撤回，使用新增回退提交保留历史，或发布修复版本；不要对共享main执行强制推送或覆盖已有标签。
