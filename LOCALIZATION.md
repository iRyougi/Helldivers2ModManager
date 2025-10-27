# 本地化系统使用指南 / Localization System Guide

## 概述 / Overview

本项目已经实现了完整的多语言支持系统，允许用户在设置中切换界面语言，并支持社区贡献新的语言翻译。

This project has implemented a complete multi-language support system that allows users to switch interface languages in settings and supports community contributions of new language translations.

## 功能特性 / Features

1. **动态语言切换** - 在设置页面选择语言后立即生效
2. **JSON格式语言文件** - 易于编辑和维护
3. **自动语言文件创建** - 首次运行时自动创建默认语言文件
4. **可扩展性** - 轻松添加新语言支持

1. **Dynamic Language Switching** - Changes take effect immediately after selecting language in settings
2. **JSON Language Files** - Easy to edit and maintain
3. **Automatic Language File Creation** - Default language files are created automatically on first run
4. **Extensibility** - Easy to add new language support

## 已支持的语言 / Supported Languages

- **en** - English (英语)
- **zh-cn** - 简体中文 (Simplified Chinese)

## 如何使用 / How to Use

### 切换语言 / Switching Language

1. 打开应用程序
2. 点击"Settings"按钮进入设置页面
3. 在"Language"下拉菜单中选择您想要的语言
4. 点击"OK"保存设置
5. 界面将更新为所选语言

1. Open the application
2. Click the "Settings" button to enter the settings page
3. Select your desired language from the "Language" dropdown menu
4. Click "OK" to save settings
5. The interface will update to the selected language

## 添加新语言 / Adding a New Language

### 步骤 / Steps

1. 在应用程序目录下找到 `Languages` 文件夹
2. 复制 `en.json` 文件并重命名为您的语言代码（例如：`ja.json` 用于日语，`fr.json` 用于法语）
3. 打开新文件并翻译所有值（保持键名不变）
4. 保存文件
5. 重启应用程序，新语言将自动出现在语言选择器中

1. Find the `Languages` folder in the application directory
2. Copy the `en.json` file and rename it to your language code (e.g., `ja.json` for Japanese, `fr.json` for French)
3. Open the new file and translate all values (keep the key names unchanged)
4. Save the file
5. Restart the application, and the new language will automatically appear in the language selector

### 语言代码参考 / Language Code Reference

常用语言代码：
- `en` - English
- `zh-cn` - 简体中文
- `zh-tw` - 繁体中文
- `ja` - 日本语
- `ko` - ???
- `fr` - Fran?ais
- `de` - Deutsch
- `es` - Espa?ol
- `ru` - Русский
- `pt` - Português

## 语言文件结构 / Language File Structure

语言文件是JSON格式，包含键值对：

```json
{
  "Window.Title": "Mod Manager",
  "Dashboard.Add": "Add",
  "Settings.GameDirectory": "Game Directory"
}
```

### 键名约定 / Key Naming Convention

- `Window.*` - 主窗口相关文本
- `Dashboard.*` - 控制面板页面文本
- `Settings.*` - 设置页面文本
- `Message.*` - 消息和提示文本
- `Dialog.*` - 对话框标题

## 为开发者 / For Developers

### 在代码中使用本地化 / Using Localization in Code

在C#代码中：

```csharp
// 注入LocalizationService
private readonly LocalizationService _localization;

public MyViewModel(LocalizationService localization)
{
    _localization = localization;
}

// 使用翻译
string text = _localization["Settings.Title"];
```

在XAML中（待实现）：

```xaml
<!-- 使用LocalizeExtension -->
<TextBlock Text="{ext:Localize Settings.Title}"/>
```

### 添加新的翻译键 / Adding New Translation Keys

1. 在 `LocalizationService.cs` 的 `CreateDefaultLanguageFilesAsync` 方法中添加新键
2. 为所有支持的语言添加翻译值
3. 在代码或XAML中使用新键

## 技术实现 / Technical Implementation

### 核心组件 / Core Components

1. **LocalizationService** - 管理语言加载和切换的单例服务
2. **SettingsService** - 保存用户的语言偏好
3. **LocalizeExtension** - XAML标记扩展（待完全实现）

### 文件位置 / File Locations

- 语言文件：`Languages/*.json`
- 服务实现：`Services/LocalizationService.cs`
- 设置集成：`Services/SettingsService.cs`
- XAML扩展：`Extensions/LocalizeExtension.cs`

## 贡献翻译 / Contributing Translations

欢迎贡献新的语言翻译！

1. Fork 本项目
2. 按照上述步骤添加新语言文件
3. 确保所有键都已翻译
4. 提交 Pull Request

Welcome to contribute new language translations!

1. Fork this project
2. Add a new language file following the steps above
3. Ensure all keys are translated
4. Submit a Pull Request

## 注意事项 / Notes

- 语言文件必须是有效的JSON格式
- 所有语言文件应包含相同的键
- 文件名必须是小写的语言代码
- 特殊字符需要正确转义

- Language files must be valid JSON format
- All language files should contain the same keys
- File names must be lowercase language codes
- Special characters need to be properly escaped

## 未来计划 / Future Plans

- [ ] 完善XAML中的本地化绑定支持
- [ ] 添加更多语言支持
- [ ] 实现语言文件热重载
- [ ] 添加翻译质量验证工具

- [ ] Improve localization binding support in XAML
- [ ] Add more language support
- [ ] Implement hot reload for language files
- [ ] Add translation quality validation tools
