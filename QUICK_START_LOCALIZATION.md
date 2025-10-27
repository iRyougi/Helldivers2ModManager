# 快速开始 - 本地化功能 / Quick Start - Localization Feature

## ?? 功能概述

您的 Helldivers 2 Mod Manager 现在支持多语言！目前已内置：
- ???? English (英语)
- ???? 简体中文

## ?? 立即使用

### 1. 首次运行
当您首次运行更新后的应用程序时，系统会自动：
- 创建 `Languages` 文件夹
- 生成 `en.json` 和 `zh-cn.json` 语言文件
- 使用默认语言（英语）

### 2. 切换语言

1. 启动 Mod Manager
2. 点击 **Settings** 按钮
3. 找到 **Language** 部分
4. 从下拉菜单选择您想要的语言
5. 点击 **OK** 保存
6. 您的语言偏好会被保存，下次启动自动应用

## ?? 当前状态

### ? 已完成
- ? 核心本地化服务
- ? 语言选择器（在设置页面）
- ? 语言偏好保存/加载
- ? 支持添加新语言
- ? 两种语言的完整翻译键（英语/简体中文）

### ? 进行中
- ? 界面文本本地化（需要逐步更新每个页面）
- ? XAML 绑定支持

### ?? 翻译覆盖范围

所有翻译键已准备就绪，包括：
- 主窗口文本
- 控制面板页面
- 设置页面
- 所有提示消息
- 对话框标题

## ?? 添加新语言

### 步骤：

1. **找到语言文件夹**
   - 位置：`[应用程序目录]/Languages/`

2. **复制英语文件**
   ```
   复制: Languages/en.json
   重命名为: Languages/[语言代码].json
   ```
   
   语言代码示例：
   - `ja.json` - 日语
   - `ko.json` - 韩语  
   - `fr.json` - 法语
   - `de.json` - 德语
   - `es.json` - 西班牙语
   - `ru.json` - 俄语

3. **翻译文件**
   - 打开新创建的 JSON 文件
   - 翻译所有值（右侧的文本）
   - ?? 不要修改键名（左侧的代码）
   
   示例：
   ```json
   {
     "Settings.Title": "Settings",      // ? 不要改这个
     "Settings.Title": "Einstellungen"  // ? 只改这个
   }
   ```

4. **重启应用**
   - 重新启动 Mod Manager
   - 新语言会自动出现在语言选择器中

## ?? 语言文件示例

### 英语 (en.json)
```json
{
  "Dashboard.Add": "Add",
  "Dashboard.Settings": "Settings",
  "Settings.OK": "OK"
}
```

### 简体中文 (zh-cn.json)
```json
{
  "Dashboard.Add": "添加",
  "Dashboard.Settings": "设置",
  "Settings.OK": "确定"
}
```

## ?? 开发者信息

### 在代码中使用翻译

```csharp
// 注入服务
public class MyViewModel
{
    private readonly LocalizationService _localization;
    
    public MyViewModel(LocalizationService localization)
    {
        _localization = localization;
    }
    
    // 使用翻译
    public void ShowMessage()
    {
        string message = _localization["Message.PleaseWait"];
        // "Please wait democratically." 或 "请民主地等待。"
    }
}
```

### 监听语言变化

```csharp
_localization.PropertyChanged += (s, e) =>
{
    // 更新所有绑定的属性
    OnPropertyChanged(nameof(MyLocalizedProperty));
};
```

## ?? 界面更新计划

下一步将逐步更新所有界面元素以支持动态语言切换：

**优先级列表：**
1. ? 主窗口标题
2. ? 控制面板按钮和标签
3. ? 设置页面所有文本
4. ? 消息框和对话框
5. ? 工具提示

## ?? 提示

- 语言文件是 UTF-8 编码的 JSON 格式
- 支持所有 Unicode 字符
- 特殊字符需要转义（如 `\"` 表示引号）
- 文件名必须小写
- 保持所有语言文件的键名一致

## ?? 问题排查

### 语言选择器不显示我的新语言
- 检查文件名是否正确（小写、.json 扩展名）
- 确认文件位于 `Languages/` 文件夹
- 重启应用程序

### 翻译不显示
- 当前版本界面文本仍为硬编码
- 后续更新会逐步启用翻译

### JSON 文件错误
- 使用 JSON 验证器检查语法
- 确保所有引号、逗号正确
- 不要有尾随逗号

## ?? 获取帮助

如果您遇到问题或想贡献翻译：
1. 查看 `LOCALIZATION.md` 了解详细信息
2. 查看 `LOCALIZATION_IMPLEMENTATION.md` 了解技术细节
3. 在 GitHub 提交 Issue 或 Pull Request

---

**感谢您帮助 Helldivers 2 Mod Manager 变得更加国际化！** ??

**Thank you for helping make Helldivers 2 Mod Manager more international!** ??
