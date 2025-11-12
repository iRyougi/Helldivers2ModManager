using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Helldivers2ModManager.Stores;
using Helldivers2ModManager.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows.Media;

namespace Helldivers2ModManager.ViewModels;

[RegisterService(ServiceLifetime.Transient)]
internal sealed partial class MainViewModel : ObservableObject
{
	public string Title => $"HD2 {_localizationService["Window.Title"]} {Version} - {CurrentViewModel.Title}";

	public PageViewModelBase CurrentViewModel => _navigationStore.CurrentViewModel;

	public Brush Background => _background;

	public string Version => string.IsNullOrEmpty(App.VersionAddition) ? $"v{App.Version}" : $"v{App.Version} {App.VersionAddition}";

	private static readonly ProcessStartInfo s_helpStartInfo = new(@"https://www.iryougi.com/index.php/hd2help/") { UseShellExecute = true }; //帮助按钮在这里修改，现在的为placeholder
	private readonly NavigationStore _navigationStore;
	private readonly LocalizationService _localizationService;
	private readonly SettingsService _settingsService;
	private readonly SolidColorBrush _background;

	public MainViewModel(NavigationStore navigationStore, LocalizationService localizationService, SettingsService settingsService)
	{
		_navigationStore = navigationStore;
		_localizationService = localizationService;
		_settingsService = settingsService;
		
		// Initialize background with opacity from settings, or default if not initialized
		float initialOpacity = _settingsService.Initialized ? _settingsService.Opacity : SettingsService.OpacityDefault;
		_background = new SolidColorBrush(Color.FromScRgb(initialOpacity, 0, 0, 0));

		_navigationStore.Navigated += NavigationStore_Navigated;
		_localizationService.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Title));
		
		// Listen to opacity changes from settings
		_settingsService.PropertyChanged += SettingsService_PropertyChanged;
		
		// If settings is already initialized, update opacity immediately
		// Otherwise, wait for initialization to complete
		if (_settingsService.Initialized)
		{
			UpdateBackgroundOpacity();
		}
	}

	private void SettingsService_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(SettingsService.Opacity))
		{
			UpdateBackgroundOpacity();
		}
	}
	
	private void UpdateBackgroundOpacity()
	{
		if (_settingsService.Initialized)
		{
			_background.Color = Color.FromScRgb(_settingsService.Opacity, 0, 0, 0);
		}
	}

	private void NavigationStore_Navigated(object? sender, EventArgs e)
	{
		OnPropertyChanged(nameof(CurrentViewModel));
		OnPropertyChanged(nameof(Title));
	}

	[RelayCommand]
	void Help()
	{
		Process.Start(s_helpStartInfo);
	}
}
