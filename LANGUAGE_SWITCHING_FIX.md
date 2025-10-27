# 语言切换功能修复说明 / Language Switching Fix Guide

## 问题 / Problem

您选择了 zh-cn 语言，但界面仍然显示英文。

You selected zh-cn language, but the interface still shows English.

## 原因 / Root Cause

虽然本地化服务已经实现，但界面上的文本仍然是硬编码的，没有绑定到 LocalizationService。

Although the localization service has been implemented, the text on the interface is still hardcoded and not bound to the LocalizationService.

## 已修复内容 / What Has Been Fixed

### 1. SettingsPageViewModel.cs 更新

添加了本地化文本属性：
- ? `Title` - 页面标题
- ? `GameDirectoryLabel` - 游戏目录
- ? `StorageDirectoryLabel` - 存储目录  
- ? `TempDirectoryLabel` - 临时目录
- ? `OpacityLabel` - 不透明度
- ? `LanguageLabel` - 语言
- ? `LogLevelLabel` - 日志级别
- ? `SearchLabel` - 搜索
- ? `UtilitiesLabel` - 实用工具
- ? `DevOptionsLabel` - 开发者选项
- ? `SkipListLabel` - 跳过列表
- ? `OKLabel` - 确定按钮
- 以及所有相关的描述文本...

Added localized text properties:
- ? All labels and descriptions for settings page

### 2. 语言变化监听

添加了 `UpdateLocalizedProperties()` 方法，当语言切换时自动更新所有文本：

```csharp
_localizationService.PropertyChanged += (s, e) => UpdateLocalizedProperties();
```

Added `UpdateLocalizedProperties()` method that automatically updates all text when language changes.

### 3. SettingsPageView.xaml 更新

将所有硬编码文本替换为数据绑定：

```xaml
<!-- 之前 / Before -->
<TextBlock Text="Game Directory"/>

<!-- 现在 / Now -->
<TextBlock Text="{Binding GameDirectoryLabel}"/>
```

Replaced all hardcoded text with data bindings.

## 如何测试 / How to Test

### 步骤 / Steps:

1. **关闭正在运行的应用程序**
   - 如果应用正在运行，请完全退出

2. **重新编译项目**
   ```
   在 Visual Studio 中按 Ctrl+Shift+B
   或者使用菜单: 生成 > 重新生成解决方案
   ```

3. **运行应用程序**

4. **打开设置页面**
   - 点击 "Settings" 按钮

5. **切换语言**
   - 在 "Language" 下拉菜单中选择 "zh-cn"
   - 您应该**立即**看到设置页面的所有文本变为中文！
   - 包括：
     * 游戏目录
     * 存储目录
     * 临时目录
     * 不透明度
     * 语言
     * 日志级别
     * 搜索
     * 实用工具
     * 开发者选项
     * 等等...

6. **保存设置**
   - 点击 "确定" 按钮（现在应该显示中文）
   - 下次启动时会自动使用中文

## 预期效果 / Expected Results

### 切换到中文后（zh-cn）：

| 英文 / English | 中文 / Chinese |
|---|---|
| Game Directory | 游戏目录 |
| Storage Directory | 存储目录 |
| Temporary Directory | 临时目录 |
| Opacity | 不透明度 |
| Language | 语言 |
| Log Level | 日志级别 |
| Search | 搜索 |
| Case Sensitive | 区分大小写 |
| Utilities | 实用工具 |
| Reset | 重置 |
| Dev Options | 开发者选项 |
| Skip List | 跳过列表 |
| OK | 确定 |
| Auto detect | 自动检测 |

## 重要提示 / Important Notes

### ? 已完成本地化的部分：

- ? **Settings 页面** - 完全本地化，支持动态切换

### ? 待完成本地化的部分：

- ? Dashboard 页面（控制面板）
- ? 主窗口标题
- ? 消息框和对话框
- ? 按钮工具提示

这些部分需要类似的方式逐步更新。

## 故障排除 / Troubleshooting

### 问题：编译失败，提示文件被锁定

**解决方案：**
1. 关闭所有运行中的 Helldivers2ModManager 实例
2. 在任务管理器中确认没有相关进程
3. 重新编译

**Solution:**
1. Close all running instances of Helldivers2ModManager
2. Confirm no related processes in Task Manager
3. Rebuild

### 问题：切换语言后文本没有变化

**检查：**
1. 确保您已重新编译并运行新版本
2. 检查 `Languages/zh-cn.json` 文件是否存在
3. 查看日志文件是否有错误

**Check:**
1. Ensure you've rebuilt and are running the new version
2. Check if `Languages/zh-cn.json` file exists
3. Look at log files for any errors

### 问题：某些文本仍然是英文

**说明：**
- Dashboard 等其他页面尚未更新
- 这是正常的，因为我们首先完成了 Settings 页面
- 其他页面将在后续版本中更新

**Explanation:**
- Other pages like Dashboard haven't been updated yet
- This is normal, as we completed the Settings page first
- Other pages will be updated in future versions

## 下一步计划 / Next Steps

### 阶段 1: ? 已完成
- ? 核心本地化服务
- ? Settings 页面完全本地化

### 阶段 2: ?? 进行中
- ? Dashboard 页面本地化
- ? 主窗口本地化
- ? 消息框本地化

### 阶段 3: ?? 计划中
- ?? 添加更多语言支持
- ?? 社区翻译贡献
- ?? 翻译质量验证工具

## 技术细节 / Technical Details

### 工作原理 / How It Works

1. **LocalizationService** 管理所有翻译
   - 从 JSON 文件加载翻译
   - 提供索引器访问 `_localization["Key"]`
   - 触发 PropertyChanged 事件通知界面更新

2. **ViewModel** 暴露本地化属性
   - 每个文本都有对应的属性
   - 监听 LocalizationService 的变化
   - 自动通知 View 更新

3. **View** 绑定到 ViewModel 属性
   - 使用 `{Binding PropertyName}` 语法
   - 当属性变化时自动更新显示

### 数据流 / Data Flow

```
用户选择语言
   ↓
SettingsPageViewModel.Language setter
   ↓
LocalizationService.CurrentLanguage
   ↓
加载新的 JSON 文件
   ↓
触发 PropertyChanged 事件
   ↓
UpdateLocalizedProperties()
   ↓
更新所有本地化属性
   ↓
WPF 数据绑定更新界面
   ↓
? 界面显示新语言！
```

## 验证清单 / Verification Checklist

测试完成后，请验证：

- [ ] Settings 页面所有标题都显示为中文
- [ ] 所有描述文本都显示为中文
- [ ] 按钮文本显示为中文（OK → 确定）
- [ ] 切换回英语后所有文本恢复英文
- [ ] 保存设置后重启应用，语言偏好被记住
- [ ] Language 下拉菜单正确显示可用语言

After testing, verify:

- [ ] All titles on Settings page show in Chinese
- [ ] All description texts show in Chinese
- [ ] Button text shows in Chinese (OK → 确定)
- [ ] Switching back to English restores all text
- [ ] After saving and restarting, language preference is remembered
- [ ] Language dropdown correctly shows available languages

---

**现在您可以享受完整的多语言支持了！** ??

**Now you can enjoy full multi-language support!** ??
