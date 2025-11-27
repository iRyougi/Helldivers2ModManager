using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Helldivers2ModManager.Components;
using Helldivers2ModManager.Models;
using Helldivers2ModManager.Services;
using Helldivers2ModManager.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Windows;
using MessageBox = Helldivers2ModManager.Components.MessageBox;

namespace Helldivers2ModManager.ViewModels;

[RegisterService(ServiceLifetime.Transient)]
internal sealed partial class DashboardPageViewModel : PageViewModelBase
{
	public override string Title => _localizationService["Dashboard.Title"];

	// Localized labels
	public string AddLabel => _localizationService["Dashboard.Add"];
	public string ReportBugLabel => _localizationService["Dashboard.ReportBug"];
	public string SettingsLabel => _localizationService["Dashboard.Settings"];
	public string PurgeLabel => _localizationService["Dashboard.Purge"];
	public string DeployLabel => _localizationService["Dashboard.Deploy"];
	public string LaunchHD2Label => _localizationService["Dashboard.LaunchHD2"];
	public string SearchLabel => _localizationService["Dashboard.Search"];
	public string EditLabel => _localizationService["Dashboard.Edit"];
	public string UpdateLabel => _localizationService["Dashboard.Update"];
	public string AliasLabel => _localizationService["Dashboard.Alias"];
	public string AliasTooltip => _localizationService["Dashboard.AliasTooltip"];
	public string ModTitleDisplay => _localizationService["Dashboard.ModTitleDisplay"];
	public string ModTitleOriginal => _localizationService["Dashboard.ModTitleOriginal"];
	public string ModTitleAddedTime => _localizationService["Dashboard.ModTitleAddedTime"];
	public string ModTitleClickHint => _localizationService["Dashboard.ModTitleClickHint"];
	public string PurgeTooltip => _localizationService["Dashboard.PurgeTooltip"];
	public string DeployTooltip => _localizationService["Dashboard.DeployTooltip"];
	public string LaunchTooltip => _localizationService["Dashboard.LaunchTooltip"];
	public string UpdateTooltip => _localizationService["Dashboard.UpdateTooltip"];

	public IReadOnlyList<ModViewModel> Mods { get; private set; }

	public bool IsSearchEmpty => string.IsNullOrEmpty(SearchText);
	
	private static readonly ProcessStartInfo s_gameStartInfo = new("steam://run/553850") { UseShellExecute = true };
	private static readonly ProcessStartInfo s_reportStartInfo = new("https://github.com/iRyougi/Helldivers2ModManager/issues") { UseShellExecute = true };
	private static readonly ProcessStartInfo s_discordStartInfo = new("https://discord.gg/helldiversmodding") { UseShellExecute = true };
	private static readonly ProcessStartInfo s_githubStartInfo = new("https://github.com/iRyougi/Helldivers2ModManager") { UseShellExecute = true };
	private readonly ILogger<DashboardPageViewModel> _logger;
	private readonly Lazy<NavigationStore> _navStore;
	private readonly ModService _modService;
	private readonly SettingsService _settingsService;
	private readonly ProfileService _profileService;
	private readonly LocalizationService _localizationService;
	private readonly ModAliasService _aliasService;
	private ObservableCollection<ModViewModel> _mods;
	[ObservableProperty]
	private Visibility _editVisibility = Visibility.Hidden;
	[ObservableProperty]
	private ModViewModel? _editMod;
	[ObservableProperty]
	private string _searchText = string.Empty;
	[ObservableProperty]
	private bool _initialized = false;

	public DashboardPageViewModel(ILogger<DashboardPageViewModel> logger, IServiceProvider provider, SettingsService settingsService, ModService modService, ProfileService profileService, LocalizationService localizationService, ModAliasService aliasService)
	{
		_logger = logger;
		_navStore = new(provider.GetRequiredService<NavigationStore>);
		_settingsService = settingsService;
		_modService = modService;
		_profileService = profileService;
		_localizationService = localizationService;
		_aliasService = aliasService;
		_mods = [];

		// Listen to language changes
		_localizationService.PropertyChanged += (s, e) => UpdateLocalizedProperties();

		Mods = _mods;

		if (MessageBox.IsRegistered)
			_ = Init();
		else
			MessageBox.Registered += (_, _) => _ = Init();
	}

	private void UpdateLocalizedProperties()
	{
		OnPropertyChanged(nameof(Title));
		OnPropertyChanged(nameof(AddLabel));
		OnPropertyChanged(nameof(ReportBugLabel));
		OnPropertyChanged(nameof(SettingsLabel));
		OnPropertyChanged(nameof(PurgeLabel));
		OnPropertyChanged(nameof(DeployLabel));
		OnPropertyChanged(nameof(LaunchHD2Label));
		OnPropertyChanged(nameof(SearchLabel));
		OnPropertyChanged(nameof(EditLabel));
		OnPropertyChanged(nameof(UpdateLabel));
		OnPropertyChanged(nameof(AliasLabel));
		OnPropertyChanged(nameof(AliasTooltip));
		OnPropertyChanged(nameof(ModTitleDisplay));
		OnPropertyChanged(nameof(ModTitleOriginal));
		OnPropertyChanged(nameof(ModTitleAddedTime));
		OnPropertyChanged(nameof(ModTitleClickHint));
		OnPropertyChanged(nameof(PurgeTooltip));
		OnPropertyChanged(nameof(DeployTooltip));
		OnPropertyChanged(nameof(LaunchTooltip));
		OnPropertyChanged(nameof(UpdateTooltip));
	}

	protected override void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(SearchText))
		{
			OnPropertyChanged(nameof(IsSearchEmpty));
			ClearSearchCommand.NotifyCanExecuteChanged();
			UpdateView();
		}

		base.OnPropertyChanged(e);
	}

	private async Task SaveEnabled()
	{
		WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage()
		{
			Title = _localizationService["Message.SavingModConfig"],
			Message = _localizationService["Message.PleaseWait"]
		});

		await _profileService.SaveAsync(_settingsService, _mods.Select(static vm => vm.Data));
		
		// Save aliases as well
		await _aliasService.SaveAsync();

		WeakReferenceMessenger.Default.Send(new MessageBoxHideMessage());
	}

	private void UpdateView()
	{
		if (IsSearchEmpty)
			Mods = _mods;
		else
			Mods = _mods.Where(vm =>
			{
				if (_settingsService.CaseSensitiveSearch)
				{
					return vm.DisplayName.Contains(SearchText, StringComparison.InvariantCulture) ||
					       vm.Name.Contains(SearchText, StringComparison.InvariantCulture);
				}
				return vm.DisplayName.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase) ||
				       vm.Name.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase);
			}).ToArray();
		OnPropertyChanged(nameof(Mods));
	}

	private async Task Init()
	{
		_logger.LogInformation("Initializing dashboard...");

		_logger.LogInformation("Loading settings...");
		WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage
		{
			Title = _localizationService["Message.LoadingSettings"],
			Message = _localizationService["Message.PleaseWait"],
		});
		try
		{
			if (!await _settingsService.InitAsync(true))
				_settingsService.InitDefault(true);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading settings failed");
			WeakReferenceMessenger.Default.Send(new MessageBoxConfirmMessage
			{
				Title = _localizationService["Message.LoadingSettingsFailed"],
				Message = _localizationService["Message.GoToSettingsNow"],
				Confirm = _navStore.Value.Navigate<SettingsPageViewModel>,
			});
			return;
		}
		_logger.LogInformation("Settings loaded successfully");
		WeakReferenceMessenger.Default.Send(new MessageBoxHideMessage());

		_logger.LogInformation("Validating settings");
		if (!_settingsService.Validate())
		{
			_logger.LogError("Settings invalid");
			WeakReferenceMessenger.Default.Send(new MessageBoxConfirmMessage
			{
				Title = _localizationService["Message.InvalidSettings"],
				Message = _localizationService["Message.GoToSettingsNow"],
				Confirm = _navStore.Value.Navigate<SettingsPageViewModel>,
			});
			return;
		}
		_logger.LogInformation("Settings valid");
		
		// Initialize alias service
		_logger.LogInformation("Loading mod aliases...");
		await _aliasService.InitAsync(_settingsService);
		
		// Provide alias service to mod service
		_modService.SetAliasService(_aliasService);
		
		_logger.LogInformation("Loading mods...");
		WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage
		{
			Title = _localizationService["Message.LoadingMods"],
			Message = _localizationService["Message.PleaseWait"],
		});
		ModProblem[] problems;
		try
		{
			problems = await Task.Run(() => _modService.Init(_settingsService));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading mods failed");
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage
			{
				Message = string.Format(_localizationService["Message.LoadingModsFailedWithError"], ex),
			});
			return;
		}
		_modService.ModAdded += ModService_ModAdded;
		_modService.ModRemoved += ModService_ModRemoved;
		if (problems.Length != 0)
			_logger.LogWarning("Loaded mods with {} problems", problems.Length);
		else
			_logger.LogInformation("Mods loaded successfully");
		WeakReferenceMessenger.Default.Send(new MessageBoxHideMessage());

		_logger.LogInformation("Loading profile...");
		WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage
		{
			Title = _localizationService["Message.LoadingProfile"],
			Message = _localizationService["Message.PleaseWait"],
		});
		IReadOnlyList<ModData>? result;
		try
		{
			result = await _profileService.LoadAsync(_settingsService, _modService);
			result ??= _profileService.InitDefault(_modService);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading profile failed");
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage
			{
				Message = string.Format(_localizationService["Message.LoadingProfileFailedWithError"], ex),
			});
			return;
		}
		WeakReferenceMessenger.Default.Send(new MessageBoxHideMessage());
		_logger.LogInformation("Profile loaded successfully");

		_logger.LogInformation("Applying profile");
		_mods = new(result.Select(data =>
		{
			var vm = new ModViewModel(data);
			vm.SetAliasService(_aliasService);
			return vm;
		}).ToList());
		UpdateView();

		if (problems.Length > 0)
			ShowProblems(problems, _localizationService["Message.ProblemsLoadingMods"], false, true);
		Initialized = true;
		_logger.LogInformation("Initialization successful");

#if DEBUG && FALSE
		ShowProblems(Enum.GetValues<ModProblemKind>().Select(static k => new ModProblem { Directory = new DirectoryInfo(@"C:\ModStorage\Test"), Kind = k }), "Problem test:", true);
#endif
	}

	private void ShowProblems(IEnumerable<ModProblem> problems, string prefix, bool error, bool isInit = false)
	{
		var sb = new StringBuilder();
		sb.AppendLine(prefix);

		var errors = problems.Where(static p => p.IsError).ToArray();
		if (errors.Length != 0)
		{
			sb.AppendLine(_localizationService["Problem.Errors"]);
			foreach (var e in errors)
			{
				sb.Append("\t - \"");
				sb.Append(e.Directory.FullName);
				sb.AppendLine("\"");

				sb.Append("\t\t");
				string desc = e.Kind switch
				{
					ModProblemKind.CantParseManifest => _localizationService["Problem.CantParseManifest"],
					ModProblemKind.UnknownManifestVersion => _localizationService["Problem.UnknownManifestVersion"],
					ModProblemKind.OutOfSupportManifest => string.Format(_localizationService["Problem.OutOfSupportManifest"], App.Version),
					ModProblemKind.Duplicate => _localizationService["Problem.Duplicate"],
					ModProblemKind.InvalidPath => e.ExtraData is not null
						? string.Format(_localizationService["Problem.InvalidPathWithData"], e.ExtraData)
						: _localizationService["Problem.InvalidPath"],
					_ => throw new NotImplementedException()
				};
				sb.AppendLine(desc);
			}
		}

		var warnings = problems.Where(static p => !p.IsError).ToArray();
		if (warnings.Length != 0)
		{
			sb.AppendLine(_localizationService["Problem.Warnings"]);
			foreach (var w in warnings)
			{
				sb.Append("\t - \"");
				sb.Append(w.Directory.FullName);
				sb.AppendLine("\"");

				sb.Append("\t\t");
				string desc = w.Kind switch
				{
					ModProblemKind.NoManifestFound => isInit
						? _localizationService["Problem.NoManifestFoundDeleting"]
						: _localizationService["Problem.NoManifestFoundInferring"],
					ModProblemKind.EmptyOptions => _localizationService["Problem.EmptyOptions"],
					ModProblemKind.EmptySubOptions => _localizationService["Problem.EmptySubOptions"],
					ModProblemKind.EmptyIncludes => _localizationService["Problem.EmptyIncludes"],
					ModProblemKind.InvalidImagePath => w.ExtraData is not null
						? string.Format(_localizationService["Problem.InvalidImagePathWithData"], w.ExtraData)
						: _localizationService["Problem.InvalidImagePath"],
					ModProblemKind.EmptyImagePath => _localizationService["Problem.EmptyImagePath"],
					_ => throw new NotImplementedException()
				};
				sb.AppendLine(desc);
			}
		}

		if (error)
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage
			{
				Message = sb.ToString(),
			});
		else
			WeakReferenceMessenger.Default.Send(new MessageBoxWarningMessage
			{
				Message = sb.ToString(),
			});
	}

	private void ModService_ModAdded(ModData mod)
	{
		var vm = new ModViewModel(mod);
		vm.SetAliasService(_aliasService);
		_mods.Add(vm);
		SearchText = string.Empty;
		UpdateView();
	}

	private void ModService_ModRemoved(ModData mod)
	{
		var vm = _mods.First((vm) => vm.Data == mod);
		if (vm is not null)
			_mods.Remove(vm);
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	async Task Add()
	{
		var dialog = new OpenFileDialog
		{
			CheckFileExists = true,
			CheckPathExists = true,
			InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Download"),
			Filter = "Archive|*.rar;*.7z;*.zip;*.tar",
			Multiselect = false,
			Title = _localizationService["Dialog.SelectModArchive"]
		};

		if (dialog.ShowDialog() ?? false)
		{
			WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage
			{
				Title = _localizationService["Message.AddingMod"],
				Message = _localizationService["Message.PleaseWait"]
			});
			try
			{
				var problems = await _modService.TryAddModFromArchiveAsync(new FileInfo(dialog.FileName));
				if (problems.Length > 0)
				{
					var error = problems.Any(static p => p.IsError);
					var prefix = error
						? _localizationService["Message.ModAddingFailedProblems"]
						: _localizationService["Message.ModAddedWarnings"];
					ShowProblems(problems, prefix, error);
				}
				else
					WeakReferenceMessenger.Default.Send(new MessageBoxHideMessage());
			}
			catch(Exception ex)
			{
				_logger.LogError(ex, "Failed to add mod");
				WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
				{
					Message = ex.Message
				});
			}
		}
	}

	[RelayCommand]
	void Browse()
	{
		throw new NotImplementedException();
	}

	[RelayCommand]
	void Create()
	{
		_navStore.Value.Navigate<CreatePageViewModel>();
	}

	[RelayCommand]
	[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This is a command of a view model and should not be static.")]
	void ReportBug()
	{
		Process.Start(s_reportStartInfo);
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	async Task Settings()
	{
		await SaveEnabled();
		
		_navStore.Value.Navigate<SettingsPageViewModel>();
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	async Task Purge()
	{
		if (string.IsNullOrEmpty(_settingsService.GameDirectory))
		{
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
			{
				Message = _localizationService["Message.UnableToPurge"]
			});
			return;
		}

		WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage()
		{
			Title = _localizationService["Message.PurgingMods"],
			Message = _localizationService["Message.PleaseWait"]
		});

		await _modService.PurgeAsync();

		WeakReferenceMessenger.Default.Send(new MessageBoxHideMessage());
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	async Task Deploy()
	{
		if (string.IsNullOrEmpty(_settingsService.GameDirectory))
		{
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
			{
				Message = _localizationService["Message.UnableToDeploy"]
			});
			return;
		}

		WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage()
		{
			Title = _localizationService["Message.DeployingMods"],
			Message = _localizationService["Message.PleaseWait"]
		});

		var mods = _mods.Where(static vm => vm.Enabled).ToArray();
		var guids = mods.Select(static vm => vm.Guid).ToArray();

		try
		{
			await SaveEnabled();

			await _modService.DeployAsync(guids);

			WeakReferenceMessenger.Default.Send(new MessageBoxInfoMessage()
			{
				Message = _localizationService["Message.DeploymentSuccess"]
			});
		}
		catch(Exception ex)
		{
			_logger.LogError(ex, "Unknown deployment error");
			WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
			{
				Message = ex.Message
			});
		}
	}

	[RelayCommand]
	void MoveUp(ModViewModel modVm)
	{
		var index = _mods.IndexOf(modVm);
		if (index <= 0)
			return;
		_mods.Move(index, index - 1);
	}

	[RelayCommand]
	void MoveDown(ModViewModel modVm)
	{
		var index = _mods.IndexOf(modVm);
		if (index >= _mods.Count - 1)
			return;
		_mods.Move(index, index + 1);
	}

	[RelayCommand]
	async Task Remove(ModViewModel modVm)
	{
		// Show confirmation dialog before removing
		var modName = modVm.DisplayName;
		var confirmMessage = string.Format(_localizationService["Message.ConfirmRemoveMessage"], modName);
		
		WeakReferenceMessenger.Default.Send(new MessageBoxConfirmMessage
		{
			Title = _localizationService["Message.ConfirmRemoveTitle"],
			Message = confirmMessage,
			Confirm = async () =>
			{
				WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage()
				{
					Title = _localizationService["Message.RemovingMod"],
					Message = _localizationService["Message.PleaseWait"]
				});

				try
				{
					await _modService.RemoveAsync(modVm.Data);

					WeakReferenceMessenger.Default.Send(new MessageBoxHideMessage());
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Unknown mod removal error");
					WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
					{
						Message = ex.Message
					});
				}
			}
		});
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	async Task Update(ModViewModel modVm)
	{
		var dialog = new OpenFileDialog
		{
			CheckFileExists = true,
			CheckPathExists = true,
			InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Download"),
			Filter = "Archive|*.rar;*.7z;*.zip;*.tar",
			Multiselect = false,
			Title = _localizationService["Dialog.SelectModArchiveUpdate"]
		};

		if (dialog.ShowDialog() ?? false)
		{
			WeakReferenceMessenger.Default.Send(new MessageBoxProgressMessage
			{
				Title = _localizationService["Message.UpdatingMod"],
				Message = _localizationService["Message.PleaseWait"]
			});
			try
			{
				var problems = await _modService.UpdateModFromArchiveAsync(modVm.Data, new FileInfo(dialog.FileName));
				
				// Save configuration immediately after update to persist the new GUID
				await SaveEnabled();
				
				if (problems.Length > 0)
				{
					var error = problems.Any(static p => p.IsError);
					var prefix = error
						? _localizationService["Message.ModUpdateFailedProblems"]
						: _localizationService["Message.ModUpdatedWarnings"];
					ShowProblems(problems, prefix, error);
				}
				else
					WeakReferenceMessenger.Default.Send(new MessageBoxInfoMessage()
					{
						Message = _localizationService["Message.ModUpdatedSuccess"]
					});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to update mod");
				WeakReferenceMessenger.Default.Send(new MessageBoxErrorMessage()
				{
					Message = ex.Message
				});
			}
		}
	}

	[RelayCommand]
	[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This is a command of a view model and should not be static.")]
	void Run()
	{
		Process.Start(s_gameStartInfo);
	}

	[RelayCommand]
	[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This is a command of a view model and should not be static.")]
	void Github()
	{
		Process.Start(s_githubStartInfo);
	}

	[RelayCommand]
	[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This is a command of a view model and should not be static.")]
	void Discord()
	{
		Process.Start(s_discordStartInfo);
	}

	[RelayCommand]
	void Edit(ModViewModel vm)
	{
		EditMod = vm;
		EditVisibility = Visibility.Visible;
	}

	[RelayCommand]
	void EditDone()
	{
		EditVisibility = Visibility.Hidden;
		EditMod = null;
	}

	bool CanClearSearch()
	{
		return !IsSearchEmpty;
	}

	[RelayCommand(CanExecute = nameof(CanClearSearch))]
	void ClearSearch()
	{
		SearchText = string.Empty;
	}

	[RelayCommand]
	async Task EditAlias(ModViewModel modVm)
	{
		var currentAlias = modVm.Alias ?? modVm.Name;
		
		var message = $"{_localizationService["InputDialog.Message"]}\n{_localizationService["InputDialog.OriginalName"]} {modVm.Name}";
		
		var dialog = new InputDialog(
			_localizationService["InputDialog.Title"],
			message,
			currentAlias)
		{
			Owner = Application.Current.MainWindow
		};

		if (dialog.ShowDialog() == true)
		{
			var newAlias = dialog.InputText.Trim();
			// If the new alias is the same as original name or empty, remove the alias
			if (string.IsNullOrWhiteSpace(newAlias) || newAlias == modVm.Name)
			{
				modVm.UpdateAlias(null);
			}
			else
			{
				modVm.UpdateAlias(newAlias);
			}
			await _aliasService.SaveAsync();
		}
	}
}
