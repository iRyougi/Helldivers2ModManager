# Mod别名功能 / Mod Alias Feature

## 功能说明 / Feature Description

该功能允许用户为Mod设置自定义别名，在管理器中显示自定义名称而不是原始名称。别名会被保存到独立的JSON文件中，重启软件后依然有效。

This feature allows users to set custom aliases for mods, displaying custom names in the manager instead of original names. Aliases are saved to a separate JSON file and persist across application restarts.

## 实现的文件 / Implemented Files

### 新增文件 / New Files

1. **Helldivers2ModManager/Services/ModAliasService.cs**
   - 管理Mod别名的服务类
   - 负责加载和保存别名数据到 `mod_aliases.json`
   - 提供获取和设置别名的方法

2. **Helldivers2ModManager/Components/InputDialog.xaml**
   - 自定义输入对话框UI
   - 用于编辑Mod别名的弹窗界面

3. **Helldivers2ModManager/Components/InputDialog.xaml.cs**
   - InputDialog的代码后置文件
   - 处理确定和取消按钮的逻辑

### 修改的文件 / Modified Files

1. **Helldivers2ModManager/ViewModels/ModViewModel.cs**
   - 添加 `Alias` 属性存储别名
   - 添加 `DisplayName` 属性，优先显示别名，否则显示原始名称
   - 添加 `SetAliasService()` 方法初始化别名服务
   - 添加 `UpdateAlias()` 方法更新别名

2. **Helldivers2ModManager/ViewModels/DashboardPageViewModel.cs**
   - 依赖注入 `ModAliasService`
   - 在初始化时加载别名服务
   - 为所有ModViewModel设置别名服务
   - 在保存配置时同时保存别名
   - 添加 `EditAliasCommand` 命令用于编辑别名
   - 更新搜索功能，支持搜索别名和原始名称

3. **Helldivers2ModManager/Views/DashboardPageView.xaml**
   - 将显示的文本从 `Name` 改为 `DisplayName`
   - 添加工具提示显示原始名称和显示名称
   - 添加"别名"按钮用于编辑Mod别名
   - 调整网格布局以容纳新按钮

## 使用方法 / Usage

1. 在Mod列表中，每个Mod项的右侧有一个"别名"按钮
2. 点击"别名"按钮会弹出输入对话框
3. 在对话框中输入自定义名称
4. 点击"OK"保存别名
5. 如果输入的名称与原始名称相同或为空，则会删除别名
6. 别名会自动保存到 `{StorageDirectory}/mod_aliases.json` 文件中
7. 鼠标悬停在Mod名称上会显示工具提示，显示当前显示名称和原始名称

## 数据存储 / Data Storage

别名数据存储在：`{StorageDirectory}/mod_aliases.json`

文件格式：
```json
{
	"guid-1": "自定义别名1",
	"guid-2": "自定义别名2"
}
```

## 技术细节 / Technical Details

- 使用Mod的GUID作为键存储别名
- 别名服务作为单例服务注册到DI容器
- 搜索功能同时支持搜索别名和原始名称
- 删除Mod时不会自动删除别名（可以考虑未来优化）
- 别名在Mod添加、更新时会自动保留
- `DisplayName` 是只读计算属性，所有绑定都使用 `OneWay` 模式

## 已知问题修复 / Bug Fixes

- ? 修复了 `Run` 元素中的绑定模式问题，明确指定 `Mode=OneWay` 以避免运行时异常
- ? 确保 `DisplayName` 只读属性不会被双向绑定
