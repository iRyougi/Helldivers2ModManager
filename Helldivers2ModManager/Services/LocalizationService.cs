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

    // Version number for tracking language file updates
    // Increment this whenever you update translations
    // �����������ļ��汾���޸����������ļ�ʱ�����Ӵ˰汾��
    private const int CurrentTranslationVersion = 4;
	
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
		else
		{
			// Check if language files need updating
			await UpdateLanguageFilesIfNeededAsync();
		}

		LoadAvailableLanguages();
		
		if (!AvailableLanguages.Contains(language))
			language = "en";
		
		await LoadLanguageAsync(language);
		_currentLanguage = language;
		
		_logger.LogInformation("Localization service initialized with language: {}", language);
	}

	private async Task UpdateLanguageFilesIfNeededAsync()
	{
		_logger.LogInformation("Checking language file versions");
		
		var languageFiles = s_languageDir.GetFiles("*.json");
		bool needsUpdate = false;

		foreach (var file in languageFiles)
		{
			try
			{
				var json = await File.ReadAllTextAsync(file.FullName);
				var doc = JsonDocument.Parse(json);
				var root = doc.RootElement;

				// Check if version property exists and matches
				if (root.TryGetProperty("__version", out var versionProp))
				{
					var version = versionProp.GetInt32();
					if (version < CurrentTranslationVersion)
					{
						_logger.LogInformation("Language file {} is outdated (version {} < {})", file.Name, version, CurrentTranslationVersion);
						needsUpdate = true;
						break;
					}
				}
				else
				{
					_logger.LogInformation("Language file {} has no version, needs update", file.Name);
					needsUpdate = true;
					break;
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to check version for {}", file.Name);
				needsUpdate = true;
				break;
			}
		}

		if (needsUpdate)
		{
			_logger.LogInformation("Updating language files to version {}", CurrentTranslationVersion);
			await CreateDefaultLanguageFilesAsync();
		}
		else
		{
			_logger.LogInformation("Language files are up to date");
		}
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
			var document = JsonDocument.Parse(json);
			var root = document.RootElement;
			
			_translations.Clear();
			foreach (var property in root.EnumerateObject())
			{
				// Skip the version property
				if (property.Name == "__version")
					continue;
					
				if (property.Value.ValueKind == JsonValueKind.String)
				{
					var value = property.Value.GetString();
					if (value != null)
					{
						_translations[property.Name] = value;
					}
				}
			}
			
			// Notify all properties changed to update UI
			OnPropertyChanged(string.Empty);
			_logger.LogInformation("Loaded {} translations", _translations.Count);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load language file: {}", filePath);
		}
	}

	private async Task CreateDefaultLanguageFilesAsync()
	{
		// Create English language file
		var enTranslations = new Dictionary<string, object>
		{
			["__version"] = CurrentTranslationVersion,
			
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
			["Message.ConfirmRemoveTitle"] = "Confirm Deletion",
			["Message.ConfirmRemoveMessage"] = "Are you sure you want to delete this mod?\n\nMod: {0}\n\nThis action cannot be undone.",
			
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

			// Conflict detection and update mismatch
			["Message.ConflictDetectedTitle"] = "Mod Conflicts Detected",
			["Message.ConflictDetectedBody"] = "The following mods modify the same game assets. Later mods in load order will override earlier ones — this may or may not cause issues depending on the mods.\n\n{0}\n\nDo you want to continue deploying anyway?",
			["Message.ConflictDetectedItem"] = "  Asset [{0}]: {1}",
			["Message.UpdateGuidMismatchTitle"] = "Different Mod Detected",
			["Message.UpdateGuidMismatchBody"] = "The selected archive appears to be a different mod than the one you are updating.\n\nOriginal:  {0}\n          (GUID: {1})\n\nSelected:  {2}\n          (GUID: {3})\n\nAre you sure you want to continue?",
		};

		var enPath = Path.Combine(s_languageDir.FullName, "en.json");
		await File.WriteAllTextAsync(enPath, JsonSerializer.Serialize(enTranslations, new JsonSerializerOptions { WriteIndented = true }));
		
		// Create Chinese (Simplified) language file
		var zhCnTranslations = new Dictionary<string, object>
		{
			["__version"] = CurrentTranslationVersion,
			
			// Main Window
			["Window.Title"] = "HD2 ģ�������",
			["Window.Help"] = "?",
			
			// Dashboard Page
			["Dashboard.Title"] = "�������",
			["Dashboard.Add"] = "����",
			["Dashboard.ReportBug"] = "�������",
			["Dashboard.Settings"] = "����",
			["Dashboard.Purge"] = "����",
			["Dashboard.Deploy"] = "����",
			["Dashboard.LaunchHD2"] = "������Ϸ",
			["Dashboard.Search"] = "������",
			["Dashboard.Edit"] = "�༭",
			["Dashboard.Update"] = "����",
			["Dashboard.ModTitleDisplay"] = "��ʾ���ƣ�",
			["Dashboard.ModTitleOriginal"] = "ԭʼ���ƣ�",
			["Dashboard.ModTitleAddedTime"] = "���ӽ���������ʱ�䣺",
			["Dashboard.ModTitleClickHint"] = "����༭����",
			["Dashboard.PurgeTooltip"] = "����Ϸ����Ŀ¼��ɾ�����в����ļ���",
			["Dashboard.DeployTooltip"] = "������ѡ����ģ�鲹���ļ���װ����Ϸ����Ŀ¼�С�",
			["Dashboard.LaunchTooltip"] = "ͨ��Steam������Ϸ��",
			["Dashboard.UpdateTooltip"] = "�Ӵ浵�ļ����´�ģ�顣",
			
			// Settings Page
			["Settings.Title"] = "����",
			["Settings.GameDir"] = "��ϷĿ¼",
			["Settings.GameDirectory"] = "��ϷĿ¼",
			["Settings.GameDirectoryDesc"] = "��������Helldivers 2��Ϸ��װλ�á�",
			["Settings.GameDirectoryHint"] = "�����\"...\"����ʾ��ѡ����ϷĿ¼��",
			["Settings.StorageDirectory"] = "�洢Ŀ¼",
			["Settings.StorageDirectoryDesc"] = "���Ǵ洢���й�������ģ������ļ���λ�á�",
			["Settings.StorageDirectoryWarning"] = "�ڸ��Ĵ���֮ǰ������������Ϊ�Ѱ�װ�ļ��ļ�¼�洢�ڴˣ�",
			["Settings.TempDirectory"] = "��ʱĿ¼",
			["Settings.TempDirectoryDesc"] = "��������ȡ�����д洢��ʱ�ļ���λ�á�",
			["Settings.TempDirectoryItem1"] = "- �����ļ�",
			["Settings.TempDirectoryItem2"] = "- �ݴ��ļ�",
			["Settings.TempDirectoryItem3"] = "- ��ѹ�ļ�",
			["Settings.AutoDetect"] = "�Զ����",
			["Settings.Opacity"] = "��͸����",
			["Settings.OpacityDesc"] = "���ô��ڵĲ�͸���ȡ�",
			["Settings.LogLevel"] = "��־����",
			["Settings.LogLevelDesc"] = "������־����ϸ�̶ȡ�",
			["Settings.LogLevelDefault"] = "Ĭ������£�ֻ��¼����͸����ص���Ϣ��",
			["Settings.Search"] = "����",
			["Settings.CaseSensitive"] = "���ִ�Сд",
			["Settings.CaseSensitiveDesc"] = "ʹ�������ִ�Сд��",
			["Settings.Utilities"] = "ʵ�ù���",
			["Settings.Reset"] = "����",
			["Settings.ResetDesc"] = "���������ûָ�ΪĬ��ֵ��",
			["Settings.DevOptions"] = "������ѡ��",
			["Settings.SkipList"] = "�����б�",
			["Settings.SkipListDesc"] = "�ڲ����ڼ���������ָ���ļ��ĵ�0��������",
			["Settings.Back"] = "����",
			["Settings.OK"] = "ȷ��",
			["Settings.DiscardChangesTitle"] = "�������ģ�",
			["Settings.DiscardChangesMessage"] = "����δ����ĸ��ġ��Ƿ�Ҫ������Щ���Ĳ����������棿",
			["Settings.Language"] = "����",
			["Settings.LanguageDesc"] = "ѡ����ϲ���Ľ������ԡ�",
			
			// MessageBox
			["MessageBox.Info"] = "��Ϣ",
			["MessageBox.Warning"] = "����",
			["MessageBox.Error"] = "����",
			["MessageBox.OK"] = "ȷ��",
			["MessageBox.Cancel"] = "ȡ��",
			["MessageBox.Yes"] = "��",
			["MessageBox.No"] = "��",
			["Message.ConfirmRemoveTitle"] = "ȷ��ɾ��",
			["Message.ConfirmRemoveMessage"] = "��ȷ��Ҫɾ����ģ����\n\nMod��{0}\n\n�˲����޷�����",
			
			// InputDialog
			["InputDialog.Title"] = "�༭Mod����",
			["InputDialog.Message"] = "Ϊ��Mod����һ���Զ������ƣ�",
			["InputDialog.OriginalName"] = "ԭ����:",
			["InputDialog.OK"] = "����",
			["InputDialog.Cancel"] = "ȡ��",
			
			// Messages
			["Message.LoadingSettings"] = "���ڼ�������",
			["Message.PleaseWait"] = "�������صȴ���",
			["Message.LoadingSettingsFailed"] = "��������ʧ�ܣ�",
			["Message.GoToSettingsNow"] = "����ת�����ã�",
			["Message.ResetSettingsConfirm"] = "������������������",
			["Message.SavingSettings"] = "���ڱ�������",
			["Message.SavingModConfig"] = "���ڱ���ģ������",
			["Message.GameDirEmpty"] = "��ϷĿ¼����Ϊ�գ�",
			["Message.StorageDirEmpty"] = "�洢Ŀ¼����Ϊ�գ�",
			["Message.TempDirEmpty"] = "��ʱĿ¼����Ϊ�գ�",
			["Message.InvalidSettings"] = "������Ч��",
			["Message.SaveSettingsFailed"] = "��������ʧ�ܣ�",
			["Message.ResetTitle"] = "���ã�",
			["Message.ResetConfirm"] = "�����Ҫ��������������",
			["Message.GameDirNotExist"] = "ѡ���Helldivers 2�ļ��в����ڣ�",
			["Message.GameDirInvalid"] = "ѡ���Helldivers 2�ļ��в�����ЧĿ¼�У�",
			["Message.GameDirNoData"] = "ѡ���Helldivers 2��·����������Ϊdata��Ŀ¼��",
			["Message.GameDirNoTools"] = "ѡ���Helldivers 2��·����������Ϊtools��Ŀ¼��",
			["Message.GameDirNoBin"] = "ѡ���Helldivers 2��·����������Ϊbin��Ŀ¼��",
			["Message.GameDirNoExe"] = "ѡ���Helldivers 2·������Ϊbin���ļ����в�������Ϊhelldivers2.exe���ļ���",
			["Message.FileNamePrompt"] = "�ļ�����",
			["Message.FileNameDesc"] = "��������Ҫ��������0�Ĵ浵�ļ���16���ַ����ơ�",
			["Message.FileNameLengthError"] = "�浵�ļ���ֻ����16���ַ�����",
			["Message.LookingForGame"] = "���ڲ�����Ϸ",
			["Message.GameNotFound"] = "�޷��Զ��ҵ�����Helldivers 2��Ϸ�����ֶ����á�",
			["Message.LoadingMods"] = "���ڼ���ģ��",
			["Message.LoadingModsFailedWithError"] = "����ģ��ʧ�ܣ�\n\n{0}",
			["Message.LoadingProfile"] = "���ڼ��������ļ�",
			["Message.LoadingProfileFailedWithError"] = "���������ļ�ʧ�ܣ�\n\n{0}",
			["Message.ProblemsLoadingMods"] = "����ģ��ʱ�������⣺",
			["Message.AddingMod"] = "��������ģ��",
			["Message.ModAddingFailedProblems"] = "�������⣬����ģ��ʧ�ܣ�",
			["Message.ModAddedWarnings"] = "������ģ�飬���о��棺",
			["Message.UnableToPurge"] = "�޷�������δ����Helldivers 2·������ת�����á�",
			["Message.PurgingMods"] = "��������ģ��",
			["Message.UnableToDeploy"] = "�޷�����δ����Helldivers 2·������ת�����á�",
			["Message.DeployingMods"] = "���ڲ���ģ��",
			["Message.DeploymentSuccess"] = "����ɹ���",
			["Message.RemovingMod"] = "�����Ƴ�ģ��",
			["Message.UpdatingMod"] = "���ڸ���ģ��",
			["Message.ModUpdateFailedProblems"] = "�������⣬����ģ��ʧ�ܣ�",
			["Message.ModUpdatedWarnings"] = "�Ѹ���ģ�飬���о��棺",
			["Message.ModUpdatedSuccess"] = "ģ����³ɹ����ѱ����á�",
			
			// Dialog Titles
			["Dialog.SelectGameFolder"] = "��ѡ������Helldivers 2�ļ���...",
			["Dialog.SelectStorageFolder"] = "��ѡ��һ���ļ��У������ù������洢��ģ��...",
			["Dialog.SelectTempFolder"] = "��ѡ��һ�������������ڴ洢��ʱ�ļ����ļ���...",
			["Dialog.SelectModArchive"] = "��ѡ��Ҫ���ӵ�ģ��浵...",
			["Dialog.SelectModArchiveUpdate"] = "��ѡ��Ҫ���µ�ģ��浵...",
			
			// Problem Messages
			["Problem.Errors"] = "����",
			["Problem.Warnings"] = "���棺",
			["Problem.CantParseManifest"] = "�޷������嵥�ļ���",
			["Problem.UnknownManifestVersion"] = "δ֪���嵥�汾��",
			["Problem.OutOfSupportManifest"] = "��֧�ֵ��嵥�汾������¡�\n\t\t�������汾 {0} ��֧�ִ��嵥�汾��",
			["Problem.Duplicate"] = "�����Ӿ�����ͬGUID��ģ�飡",
			["Problem.InvalidPathWithData"] = "����·��\"{0}\"��Ч��",
			["Problem.InvalidPath"] = "����·����Ч��",
			["Problem.NoManifestFoundDeleting"] = "��Ŀ¼��δ�ҵ��嵥�ļ���\n\t\t\t����������ɾ��",
			["Problem.NoManifestFoundInferring"] = "��Ŀ¼��δ�ҵ��嵥�ļ���\n\t\t\t��������Ŀ¼�ƶ�",
			["Problem.EmptyOptions"] = "�嵥������ѡ���ģ����ܲ���ִ���κβ�����",
			["Problem.EmptySubOptions"] = "�嵥��������ѡ���ģ������޷���Ԥ�ڹ�����",
			["Problem.EmptyIncludes"] = "�嵥�����յİ����б�����ģ����ܲ���ִ���κβ�����",
			["Problem.InvalidImagePathWithData"] = "�嵥ͼ��·��\"{0}\"��Ч��",
			["Problem.InvalidImagePath"] = "�嵥������Ч��ͼ��·����",
			["Problem.EmptyImagePath"] = "�嵥�����յ�ͼ��·����",

			// 冲突检测和更新不匹配
			["Message.ConflictDetectedTitle"] = "检测到Mod冲突",
			["Message.ConflictDetectedBody"] = "以下Mod修改了相同的游戏资源。加载顺序靠后的Mod会覆盖靠前的——这可能会也可能不会引发问题，具体取决于Mod本身。\n\n{0}\n\n是否仍然继续部署？",
			["Message.ConflictDetectedItem"] = "  资源 [{0}]：{1}",
			["Message.UpdateGuidMismatchTitle"] = "检测到不同的Mod",
			["Message.UpdateGuidMismatchBody"] = "所选存档似乎与您正在更新的Mod不同。\n\n原始Mod：{0}\n        （GUID：{1}）\n\n所选文件：{2}\n        （GUID：{3}）\n\n确定要继续吗？",
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
