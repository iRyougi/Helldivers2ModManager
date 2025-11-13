using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Helldivers2ModManager.Services;

[RegisterService(ServiceLifetime.Singleton)]
internal sealed class LocalizationService : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	public ObservableCollection<string> AvailableLanguages { get; } = new();
	
	public string CurrentLanguage
	{
		get => _currentLanguage;
		set
		{
			if (_currentLanguage != value)
			{
				_currentLanguage = value;
				LoadLanguageAsync(value).ConfigureAwait(false);
				OnPropertyChanged();
			}
		}
	}

	public string this[string key]
	{
		get
		{
			if (_translations.TryGetValue(key, out var value))
				return value;
			
			_logger.LogWarning("Translation key not found: {}", key);
			return $"[{key}]";
		}
	}

	private static readonly DirectoryInfo s_languageDir = new("languages");
	private readonly ILogger<LocalizationService> _logger;
	private readonly Dictionary<string, string> _translations = new();
	private string _currentLanguage = "en";

	public LocalizationService(ILogger<LocalizationService> logger)
	{
		_logger = logger;
	}

	public async Task InitializeAsync(string language = "en")
	{
		_logger.LogInformation("Initializing localization service");
		
		if (!s_languageDir.Exists)
		{
			_logger.LogWarning("Language directory not found, creating default");
			s_languageDir.Create();
			await CreateDefaultLanguageFilesAsync();
		}

		LoadAvailableLanguages();
		
		if (!AvailableLanguages.Contains(language))
			language = "en";
		
		await LoadLanguageAsync(language);
		_currentLanguage = language;
		
		_logger.LogInformation("Localization service initialized with language: {}", language);
	}

	private void LoadAvailableLanguages()
	{
		AvailableLanguages.Clear();
		
		var files = s_languageDir.GetFiles("*.json");
		foreach (var file in files)
		{
			var langCode = Path.GetFileNameWithoutExtension(file.Name);
			AvailableLanguages.Add(langCode);
		}

		if (AvailableLanguages.Count == 0)
		{
			AvailableLanguages.Add("en");
		}
	}

	private async Task LoadLanguageAsync(string language)
	{
		_logger.LogInformation("Loading language: {}", language);
		
		var filePath = Path.Combine(s_languageDir.FullName, $"{language}.json");
		if (!File.Exists(filePath))
		{
			_logger.LogWarning("Language file not found: {}", filePath);
			return;
		}

		try
		{
			var json = await File.ReadAllTextAsync(filePath);
			var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
			
			if (translations != null)
			{
				_translations.Clear();
				foreach (var kvp in translations)
				{
					_translations[kvp.Key] = kvp.Value;
				}
				
				// Notify all properties changed to update UI
				OnPropertyChanged(string.Empty);
				_logger.LogInformation("Loaded {} translations", _translations.Count);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load language file: {}", filePath);
		}
	}

	private async Task CreateDefaultLanguageFilesAsync()
	{
		// Create English language file
		var enTranslations = new Dictionary<string, string>
		{
			// Main Window
			["Window.Title"] = "HD2 Mod Manager",
			["Window.Help"] = "?",
			
			// Dashboard Page
			["Dashboard.Title"] = "Dashboard",
			["Dashboard.Add"] = "Add",
			["Dashboard.ReportBug"] = "Report Bug",
			["Dashboard.Settings"] = "Settings",
			["Dashboard.Purge"] = "Purge",
			["Dashboard.Deploy"] = "Deploy",
			["Dashboard.LaunchHD2"] = "Launch HD2",
			["Dashboard.Search"] = "Search:",
			["Dashboard.Edit"] = "Edit",
			["Dashboard.Update"] = "Update",
			["Dashboard.ModTitleDisplay"] = "Display:",
			["Dashboard.ModTitleOriginal"] = "Original:",
			["Dashboard.ModTitleAddedTime"] = "Added to Manager:",
			["Dashboard.ModTitleClickHint"] = "Click to edit alias",
			["Dashboard.PurgeTooltip"] = "Removes all patch files from the game data directory.",
			["Dashboard.DeployTooltip"] = "Installs all selected mod patch files into the game data directory.",
			["Dashboard.LaunchTooltip"] = "Runs the game through Steam.",
			["Dashboard.UpdateTooltip"] = "Update the mod from an archive file.",
			
			// Settings Page
			["Settings.Title"] = "Settings",
			["Settings.GameDir"] = "Game Directory",
			["Settings.GameDirectory"] = "Game Directory",
			["Settings.GameDirectoryDesc"] = "This is where your Helldivers 2 game is installed.",
			["Settings.GameDirectoryHint"] = "(Clicking \"...\" will prompt you to select the games directory)",
			["Settings.StorageDirectory"] = "Storage Directory",
			["Settings.StorageDirectoryDesc"] = "This is where all manager and mod related files are stored.",
			["Settings.StorageDirectoryWarning"] = "Purge before changing this as a record of the installed files is stored in this!",
			["Settings.TempDirectory"] = "Temporary Directory",
			["Settings.TempDirectoryDesc"] = "This is where temporary files are stored during extraction.",
			["Settings.TempDirectoryItem1"] = "- Download files",
			["Settings.TempDirectoryItem2"] = "- Staging files",
			["Settings.TempDirectoryItem3"] = "- Decompressed files",
			["Settings.AutoDetect"] = "Auto detect",
			["Settings.Opacity"] = "Opacity",
			["Settings.OpacityDesc"] = "Sets the opacity of the window.",
			["Settings.LogLevel"] = "Log Level",
			["Settings.LogLevelDesc"] = "Sets the verbosity of the logs.",
			["Settings.LogLevelDefault"] = "By default only warnings and anything more severe will be logged.",
			["Settings.Search"] = "Search",
			["Settings.CaseSensitive"] = "Case Sensitive",
			["Settings.CaseSensitiveDesc"] = "Makes the search case sensitive.",
			["Settings.Utilities"] = "Utilities",
			["Settings.Reset"] = "Reset",
			["Settings.ResetDesc"] = "Restores all settings to their default values.",
			["Settings.DevOptions"] = "Dev Options",
			["Settings.SkipList"] = "Skip List",
			["Settings.SkipListDesc"] = "This skips the 0th index of all specified files during deployment.",
			["Settings.Back"] = "Back",
			["Settings.OK"] = "OK",
			["Settings.DiscardChangesTitle"] = "Discard Changes?",
			["Settings.DiscardChangesMessage"] = "You have unsaved changes. Do you want to discard them and return to the main screen?",
			["Settings.Language"] = "Language",
			["Settings.LanguageDesc"] = "Select your preferred language for the interface.",
			
			// MessageBox
			["MessageBox.Info"] = "Info",
			["MessageBox.Warning"] = "Warning",
			["MessageBox.Error"] = "Error",
			["MessageBox.OK"] = "OK",
			["MessageBox.Cancel"] = "Cancel",
			["MessageBox.Yes"] = "Yes",
			["MessageBox.No"] = "No",
			
			// InputDialog
			["InputDialog.Title"] = "Edit Mod Alias",
			["InputDialog.Message"] = "Set a custom alias for this mod:",
			["InputDialog.OriginalName"] = "Original name:",
			["InputDialog.OK"] = "OK",
			["InputDialog.Cancel"] = "CANCEL",
			
			// Messages
			["Message.LoadingSettings"] = "Loading settings",
			["Message.PleaseWait"] = "Please wait democratically.",
			["Message.LoadingSettingsFailed"] = "Loading settings failed!",
			["Message.GoToSettingsNow"] = "Go to settings now?",
			["Message.ResetSettingsConfirm"] = "Do you want to reset your settings?",
			["Message.SavingSettings"] = "Saving Settings",
			["Message.SavingModConfig"] = "Saving mod configuration",
			["Message.GameDirEmpty"] = "Game directory can not be left empty!",
			["Message.StorageDirEmpty"] = "Storage directory can not be left empty!",
			["Message.TempDirEmpty"] = "Temporary directory can not be left empty!",
			["Message.InvalidSettings"] = "Invalid settings!",
			["Message.SaveSettingsFailed"] = "Failed to save settings!",
			["Message.ResetTitle"] = "Reset?",
			["Message.ResetConfirm"] = "Do you really want to reset your settings?",
			["Message.GameDirNotExist"] = "The selected Helldivers 2 folder does not exist!",
			["Message.GameDirInvalid"] = "The selected Helldivers 2 folder does not reside in a valid directory!",
			["Message.GameDirNoData"] = "The selected Helldivers 2 root path does not contain a directory named \"data\"!",
			["Message.GameDirNoTools"] = "The selected Helldivers 2 root path does not contain a directory named \"tools\"!",
			["Message.GameDirNoBin"] = "The selected Helldivers 2 root path does not contain a directory named \"bin\"!",
			["Message.GameDirNoExe"] = "The selected Helldivers 2 path does not contain a file named \"helldivers2.exe\" in a folder called \"bin\"!",
			["Message.FileNamePrompt"] = "File name?",
			["Message.FileNameDesc"] = "Please enter the 16 character name of an archive file you want to skip patch 0 for.",
			["Message.FileNameLengthError"] = "Archive file names can only be 16 characters long.",
			["Message.LookingForGame"] = "Looking for game",
			["Message.GameNotFound"] = "Your Helldivers 2 game could not be found automatically. Please set it manually.",
			["Message.LoadingMods"] = "Loading mods",
			["Message.LoadingModsFailedWithError"] = "Loading mods failed!\n\n{0}",
			["Message.LoadingProfile"] = "Loading profile",
			["Message.LoadingProfileFailedWithError"] = "Loading profile failed!\n\n{0}",
			["Message.ProblemsLoadingMods"] = "Problems with loading mods:",
			["Message.AddingMod"] = "Adding Mod",
			["Message.ModAddingFailedProblems"] = "Mod adding failed due to problems:",
			["Message.ModAddedWarnings"] = "Mod added with warnings:",
			["Message.UnableToPurge"] = "Unable to purge! Helldivers 2 Path not set. Please go to settings.",
			["Message.PurgingMods"] = "Purging Mods",
			["Message.UnableToDeploy"] = "Unable to deploy! Helldivers 2 Path not set. Please go to settings.",
			["Message.DeployingMods"] = "Deploying Mods",
			["Message.DeploymentSuccess"] = "Deployment successful.",
			["Message.RemovingMod"] = "Removing Mod",
			["Message.UpdatingMod"] = "Updating Mod",
			["Message.ModUpdateFailedProblems"] = "Mod update failed due to problems:",
			["Message.ModUpdatedWarnings"] = "Mod updated with warnings:",
			["Message.ModUpdatedSuccess"] = "Mod updated successfully and has been disabled.",
			
			// Dialog Titles
			["Dialog.SelectGameFolder"] = "Please select you Helldivers 2 folder...",
			["Dialog.SelectStorageFolder"] = "Please select a folder where you want this manager to store its mods...",
			["Dialog.SelectTempFolder"] = "Please select a folder which you want this manager to use for temporary files...",
			["Dialog.SelectModArchive"] = "Please select a mod archive to add...",
			["Dialog.SelectModArchiveUpdate"] = "Please select a mod archive to update...",
			
			// Problem Messages
			["Problem.Errors"] = "Errors:",
			["Problem.Warnings"] = "Warnings:",
			["Problem.CantParseManifest"] = "Can't parse manifest!",
			["Problem.UnknownManifestVersion"] = "Unknown manifest version!",
			["Problem.OutOfSupportManifest"] = "Unsupported manifest version! Please update.\n\t\tManager version {0} does not support this version of the manifest.",
			["Problem.Duplicate"] = "A mod with the same GUID was already added!",
			["Problem.InvalidPathWithData"] = "The include path \"{0}\" is invalid!",
			["Problem.InvalidPath"] = "A include path is invalid!",
			["Problem.NoManifestFoundDeleting"] = "No manifest found in directory!\n\t\t\tAction: Deleting",
			["Problem.NoManifestFoundInferring"] = "No manifest found in directory!\n\t\t\tAction: Inferring from directory",
			["Problem.EmptyOptions"] = "Manifest contains empty options! This mod will likely do nothing.",
			["Problem.EmptySubOptions"] = "Manifest contains empty sub-options! This mod will likely not work as expected.",
			["Problem.EmptyIncludes"] = "Manifest contains empty include lists! This mod my not do anything.",
			["Problem.InvalidImagePathWithData"] = "Manifest image path \"{0}\" is invalid!",
			["Problem.InvalidImagePath"] = "Manifest contains invalid image path!",
			["Problem.EmptyImagePath"] = "Manifest constains empty image path!",
		};

		var enPath = Path.Combine(s_languageDir.FullName, "en.json");
		await File.WriteAllTextAsync(enPath, JsonSerializer.Serialize(enTranslations, new JsonSerializerOptions { WriteIndented = true }));
		
		// Create Chinese (Simplified) language file
		var zhCnTranslations = new Dictionary<string, string>
		{
			// Main Window
			["Window.Title"] = "HD2 模组管理器",
			["Window.Help"] = "?",
			
			// Dashboard Page
			["Dashboard.Title"] = "控制面板",
			["Dashboard.Add"] = "添加",
			["Dashboard.ReportBug"] = "报告错误",
			["Dashboard.Settings"] = "设置",
			["Dashboard.Purge"] = "清理",
			["Dashboard.Deploy"] = "部署",
			["Dashboard.LaunchHD2"] = "启动游戏",
			["Dashboard.Search"] = "搜索：",
			["Dashboard.Edit"] = "编辑",
			["Dashboard.Update"] = "更新",
			["Dashboard.ModTitleDisplay"] = "显示名称：",
			["Dashboard.ModTitleOriginal"] = "原始名称：",
			["Dashboard.ModTitleAddedTime"] = "添加进管理器的时间：",
			["Dashboard.ModTitleClickHint"] = "点击编辑别名",
			["Dashboard.PurgeTooltip"] = "从游戏数据目录中删除所有补丁文件。",
			["Dashboard.DeployTooltip"] = "将所有选定的模组补丁文件安装到游戏数据目录中。",
			["Dashboard.LaunchTooltip"] = "通过Steam运行游戏。",
			["Dashboard.UpdateTooltip"] = "从存档文件更新此模组。",
			
			// Settings Page
			["Settings.Title"] = "设置",
			["Settings.GameDir"] = "游戏目录",
			["Settings.GameDirectory"] = "游戏目录",
			["Settings.GameDirectoryDesc"] = "这是您的Helldivers 2游戏安装位置。",
			["Settings.GameDirectoryHint"] = "（点击\"...\"将提示您选择游戏目录）",
			["Settings.StorageDirectory"] = "存储目录",
			["Settings.StorageDirectoryDesc"] = "这是存储所有管理器和模组相关文件的位置。",
			["Settings.StorageDirectoryWarning"] = "在更改此项之前请先清理，因为已安装文件的记录存储在此！",
			["Settings.TempDirectory"] = "临时目录",
			["Settings.TempDirectoryDesc"] = "这是在提取过程中存储临时文件的位置。",
			["Settings.TempDirectoryItem1"] = "- 下载文件",
			["Settings.TempDirectoryItem2"] = "- 暂存文件",
			["Settings.TempDirectoryItem3"] = "- 解压文件",
			["Settings.AutoDetect"] = "自动检测",
			["Settings.Opacity"] = "不透明度",
			["Settings.OpacityDesc"] = "设置窗口的不透明度。",
			["Settings.LogLevel"] = "日志级别",
			["Settings.LogLevelDesc"] = "设置日志的详细程度。",
			["Settings.LogLevelDefault"] = "默认情况下，只记录警告和更严重的信息。",
			["Settings.Search"] = "搜索",
			["Settings.CaseSensitive"] = "区分大小写",
			["Settings.CaseSensitiveDesc"] = "使搜索区分大小写。",
			["Settings.Utilities"] = "实用工具",
			["Settings.Reset"] = "重置",
			["Settings.ResetDesc"] = "将所有设置恢复为默认值。",
			["Settings.DevOptions"] = "开发者选项",
			["Settings.SkipList"] = "跳过列表",
			["Settings.SkipListDesc"] = "在部署期间跳过所有指定文件的第0个索引。",
			["Settings.Back"] = "返回",
			["Settings.OK"] = "确定",
			["Settings.DiscardChangesTitle"] = "放弃更改？",
			["Settings.DiscardChangesMessage"] = "您有未保存的更改。是否要放弃这些更改并返回主界面？",
			["Settings.Language"] = "语言",
			["Settings.LanguageDesc"] = "选择您喜欢的界面语言。",
			
			// MessageBox
			["MessageBox.Info"] = "信息",
			["MessageBox.Warning"] = "警告",
			["MessageBox.Error"] = "错误",
			["MessageBox.OK"] = "确定",
			["MessageBox.Cancel"] = "取消",
			["MessageBox.Yes"] = "是",
			["MessageBox.No"] = "否",
			
			// InputDialog
			["InputDialog.Title"] = "编辑Mod别名",
			["InputDialog.Message"] = "为此Mod设置一个自定义名称：",
			["InputDialog.OriginalName"] = "原名称:",
			["InputDialog.OK"] = "设置",
			["InputDialog.Cancel"] = "取消",
			
			// Messages
			["Message.LoadingSettings"] = "正在加载设置",
			["Message.PleaseWait"] = "请民主地等待。",
			["Message.LoadingSettingsFailed"] = "加载设置失败！",
			["Message.GoToSettingsNow"] = "现在转到设置？",
			["Message.ResetSettingsConfirm"] = "您想重置您的设置吗？",
			["Message.SavingSettings"] = "正在保存设置",
			["Message.SavingModConfig"] = "正在保存模组配置",
			["Message.GameDirEmpty"] = "游戏目录不能为空！",
			["Message.StorageDirEmpty"] = "存储目录不能为空！",
			["Message.TempDirEmpty"] = "临时目录不能为空！",
			["Message.InvalidSettings"] = "设置无效！",
			["Message.SaveSettingsFailed"] = "保存设置失败！",
			["Message.ResetTitle"] = "重置？",
			["Message.ResetConfirm"] = "您真的要重置您的设置吗？",
			["Message.GameDirNotExist"] = "选择的Helldivers 2文件夹不存在！",
			["Message.GameDirInvalid"] = "选择的Helldivers 2文件夹不在有效目录中！",
			["Message.GameDirNoData"] = "选择的Helldivers 2根路径不包含名为data的目录！",
			["Message.GameDirNoTools"] = "选择的Helldivers 2根路径不包含名为tools的目录！",
			["Message.GameDirNoBin"] = "选择的Helldivers 2根路径不包含名为bin的目录！",
			["Message.GameDirNoExe"] = "选择的Helldivers 2路径在名为bin的文件夹中不包含名为helldivers2.exe的文件！",
			["Message.FileNamePrompt"] = "文件名？",
			["Message.FileNameDesc"] = "请输入您要跳过补丁0的存档文件的16个字符名称。",
			["Message.FileNameLengthError"] = "存档文件名只能是16个字符长。",
			["Message.LookingForGame"] = "正在查找游戏",
			["Message.GameNotFound"] = "无法自动找到您的Helldivers 2游戏，请手动设置。",
			["Message.LoadingMods"] = "正在加载模组",
			["Message.LoadingModsFailedWithError"] = "加载模组失败！\n\n{0}",
			["Message.LoadingProfile"] = "正在加载配置文件",
			["Message.LoadingProfileFailedWithError"] = "加载配置文件失败！\n\n{0}",
			["Message.ProblemsLoadingMods"] = "加载模组时出现问题：",
			["Message.AddingMod"] = "正在添加模组",
			["Message.ModAddingFailedProblems"] = "由于问题，添加模组失败：",
			["Message.ModAddedWarnings"] = "已添加模组，但有警告：",
			["Message.UnableToPurge"] = "无法清理！未设置Helldivers 2路径。请转到设置。",
			["Message.PurgingMods"] = "正在清理模组",
			["Message.UnableToDeploy"] = "无法部署！未设置Helldivers 2路径。请转到设置。",
			["Message.DeployingMods"] = "正在部署模组",
			["Message.DeploymentSuccess"] = "部署成功。",
			["Message.RemovingMod"] = "正在移除模组",
			["Message.UpdatingMod"] = "正在更新模组",
			["Message.ModUpdateFailedProblems"] = "由于问题，更新模组失败：",
			["Message.ModUpdatedWarnings"] = "已更新模组，但有警告：",
			["Message.ModUpdatedSuccess"] = "模组更新成功并已被禁用。",
			
			// Dialog Titles
			["Dialog.SelectGameFolder"] = "请选择您的Helldivers 2文件夹...",
			["Dialog.SelectStorageFolder"] = "请选择一个文件夹，用于让管理器存储其模组...",
			["Dialog.SelectTempFolder"] = "请选择一个供管理器用于存储临时文件的文件夹...",
			["Dialog.SelectModArchive"] = "请选择要添加的模组存档...",
			["Dialog.SelectModArchiveUpdate"] = "请选择要更新的模组存档...",
			
			// Problem Messages
			["Problem.Errors"] = "错误：",
			["Problem.Warnings"] = "警告：",
			["Problem.CantParseManifest"] = "无法解析清单文件！",
			["Problem.UnknownManifestVersion"] = "未知的清单版本！",
			["Problem.OutOfSupportManifest"] = "不支持的清单版本！请更新。\n\t\t管理器版本 {0} 不支持此清单版本。",
			["Problem.Duplicate"] = "已添加具有相同GUID的模组！",
			["Problem.InvalidPathWithData"] = "包含路径\"{0}\"无效！",
			["Problem.InvalidPath"] = "包含路径无效！",
			["Problem.NoManifestFoundDeleting"] = "在目录中未找到清单文件！\n\t\t\t操作：正在删除",
			["Problem.NoManifestFoundInferring"] = "在目录中未找到清单文件！\n\t\t\t操作：从目录推断",
			["Problem.EmptyOptions"] = "清单包含空选项！此模组可能不会执行任何操作。",
			["Problem.EmptySubOptions"] = "清单包含空子选项！此模组可能无法按预期工作。",
			["Problem.EmptyIncludes"] = "清单包含空的包含列表！此模组可能不会执行任何操作。",
			["Problem.InvalidImagePathWithData"] = "清单图像路径\"{0}\"无效！",
			["Problem.InvalidImagePath"] = "清单包含无效的图像路径！",
			["Problem.EmptyImagePath"] = "清单包含空的图像路径！",
		};

		var zhCnPath = Path.Combine(s_languageDir.FullName, "zh-cn.json");
		await File.WriteAllTextAsync(zhCnPath, JsonSerializer.Serialize(zhCnTranslations, new JsonSerializerOptions { WriteIndented = true }));
		
		_logger.LogInformation("Created default language files");
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
