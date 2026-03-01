// Ignore Spelling: App

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;
using Helldivers2ModManager.Stores;
using Helldivers2ModManager.ViewModels;
using Helldivers2ModManager.Services;

namespace Helldivers2ModManager;

internal partial class App : Application
{
	public static readonly Version Version = new(1, 4, 0, 1); //版本号

	public static readonly string? VersionAddition = "Alpha"; //版本附加信息

    public new static App Current => (App)Application.Current;

	public IHost Host { get; }
	
	public LogLevel LogLevel { get; set; }

	private readonly ILogger? _logger;
	private Mutex? _singleInstanceMutex;
	private const string MutexName = "Global\\HD2ModManager_iRyougi_SingleInstance";

	public App()
	{
		AppDomain.CurrentDomain.UnhandledException += (_, e) => LogUnhandledException(e.ExceptionObject as Exception);
		DispatcherUnhandledException += (_, e) => LogUnhandledException(e.Exception);
		TaskScheduler.UnobservedTaskException += (_, e) => LogUnhandledException(e.Exception);

		HostApplicationBuilder builder = new();

		AddServices(builder.Services);
		builder.Services.AddSingleton<NavigationStore>(static services => new NavigationStore(services, services.GetRequiredService<DashboardPageViewModel>()));
		builder.Services.AddLogging(log =>
		{
#if DEBUG
			log.SetMinimumLevel(LogLevel.Trace);
			log.AddDebug();
#endif
			log.AddConsole();
			log.AddFile("ModManager");
		});
		builder.Services.AddTransient<MainWindow>();
		
		Host = builder.Build();

		_logger = Host.Services.GetRequiredService<ILogger<App>>();
	}

	protected override async void OnStartup(StartupEventArgs e)
	{
		// Single instance enforcement
		_singleInstanceMutex = new Mutex(true, MutexName, out bool isNewInstance);
		if (!isNewInstance)
		{
			var current = System.Diagnostics.Process.GetCurrentProcess();
			var others = System.Diagnostics.Process.GetProcessesByName(current.ProcessName)
				.Where(p => p.Id != current.Id)
				.ToArray();
			foreach (var other in others)
			{
				var hwnd = other.MainWindowHandle;
				if (hwnd != nint.Zero)
				{
					NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
					NativeMethods.SetForegroundWindow(hwnd);
				}
			}
			Shutdown();
			return;
		}

		base.OnStartup(e);

		// Initialize localization service
		var localizationService = Host.Services.GetRequiredService<LocalizationService>();
		var settingsService = Host.Services.GetRequiredService<SettingsService>();
		
		// Add localization service to application resources
		Current.Resources.Add("LocalizationService", localizationService);
		
		// Load settings first to get the language preference
		// Wait for initialization to complete before creating the window
		try
		{
			await settingsService.InitAsync(true);
			var language = settingsService.Language;
			await localizationService.InitializeAsync(language);
		}
		catch
		{
			await localizationService.InitializeAsync("en");
		}

		// Create and show window after settings are loaded
		MainWindow = Host.Services.GetRequiredService<MainWindow>();
		MainWindow.Show();
	}

	private static void AddServices(IServiceCollection services)
	{
		var tuples = Assembly.GetExecutingAssembly()
			.GetTypes()
			.Select(static type => (type, type.GetCustomAttribute<RegisterServiceAttribute>()))
			.Where(static tuple => tuple.Item2 is not null)
			.Cast<ValueTuple<Type, RegisterServiceAttribute>>()
			.ToArray();

		foreach (var (type, attr) in tuples)
		{
			switch (attr.Lifetime)
			{
				case ServiceLifetime.Singleton:
					if (attr.Contract is null)
						services.AddSingleton(type);
					else
						services.AddSingleton(attr.Contract, type);
					break;
				
				case ServiceLifetime.Scoped:
					if (attr.Contract is null)
						services.AddScoped(type);
					else
						services.AddScoped(attr.Contract, type);
					break;
				
				case ServiceLifetime.Transient:
					if (attr.Contract is null)
						services.AddTransient(type);
					else
						services.AddTransient(attr.Contract, type);
					break;
			}
		}
	}
	
	private void LogUnhandledException(Exception? ex)
	{
		if (_logger is null)
			MessageBox.Show($"An unhandled exception occurred before logging could be initialized!\n\n{ex?.ToString()}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
		else
			_logger?.LogError(ex, "An unhandled exception occured!");
	}

	protected override void OnExit(ExitEventArgs e)
	{
		_singleInstanceMutex?.ReleaseMutex();
		_singleInstanceMutex?.Dispose();
		base.OnExit(e);
	}
}

