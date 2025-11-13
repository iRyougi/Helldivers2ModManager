using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Helldivers2ModManager.Services;

[RegisterService(ServiceLifetime.Singleton)]
internal sealed class ModAliasService : INotifyPropertyChanged
{
	[MemberNotNullWhen(true, nameof(_aliases))]
	public bool Initialized { get; private set; }

	private readonly ILogger<ModAliasService> _logger;
	private readonly FileInfo _aliasFile;
	private Dictionary<Guid, string> _aliases = null!;
	private SettingsService? _settingsService;

	public event PropertyChangedEventHandler? PropertyChanged;

	public ModAliasService(ILogger<ModAliasService> logger)
	{
		_logger = logger;
		_aliasFile = new FileInfo("mod_aliases.json");
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public async Task InitAsync(SettingsService settingsService)
	{
		if (Initialized)
			return;

		_logger.LogInformation("Initializing mod alias service");
		_settingsService = settingsService;
		
		var aliasFilePath = Path.Combine(settingsService.StorageDirectory, "mod_aliases.json");
		_aliasFile.Refresh();

		if (!File.Exists(aliasFilePath))
		{
			_logger.LogInformation("Alias file not found, creating new dictionary");
			_aliases = new Dictionary<Guid, string>();
		}
		else
		{
			_logger.LogInformation("Loading aliases from file");
			await LoadAsync(aliasFilePath);
		}

		Initialized = true;
		_logger.LogInformation("Mod alias service initialization complete");
	}

	public string? GetAlias(Guid modGuid)
	{
		GuardInitialized();
		return _aliases.TryGetValue(modGuid, out var alias) ? alias : null;
	}

	public void SetAlias(Guid modGuid, string? alias)
	{
		GuardInitialized();

		if (string.IsNullOrWhiteSpace(alias))
		{
			// Remove alias if empty
			if (_aliases.Remove(modGuid))
			{
				_logger.LogInformation("Removed alias for mod {}", modGuid);
				OnPropertyChanged();
			}
		}
		else
		{
			_aliases[modGuid] = alias.Trim();
			_logger.LogInformation("Set alias for mod {} to \"{}\"", modGuid, alias);
			OnPropertyChanged();
		}
	}

	public async Task SaveAsync()
	{
		GuardInitialized();

		if (_settingsService is null)
			throw new InvalidOperationException("Settings service not initialized!");

		var aliasFilePath = Path.Combine(_settingsService.StorageDirectory, "mod_aliases.json");
		_logger.LogInformation("Saving mod aliases to {}", aliasFilePath);

		var stream = File.Open(aliasFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
		var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
		{
			Indented = true,
			IndentCharacter = '\t',
			IndentSize = 1,
		});

		writer.WriteStartObject();
		foreach (var (guid, alias) in _aliases)
		{
			writer.WriteString(guid.ToString(), alias);
		}
		writer.WriteEndObject();

		await writer.DisposeAsync();
		await stream.DisposeAsync();

		_logger.LogInformation("Mod aliases saved successfully");
	}

	private async Task LoadAsync(string filePath)
	{
		try
		{
			var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			var document = await JsonDocument.ParseAsync(stream, new JsonDocumentOptions
			{
				AllowTrailingCommas = true,
				CommentHandling = JsonCommentHandling.Skip
			});

			var root = document.RootElement;
			_aliases = new Dictionary<Guid, string>();

			foreach (var property in root.EnumerateObject())
			{
				if (Guid.TryParse(property.Name, out var guid) && property.Value.ValueKind == JsonValueKind.String)
				{
					var alias = property.Value.GetString();
					if (!string.IsNullOrWhiteSpace(alias))
					{
						_aliases[guid] = alias;
					}
				}
			}

			document.Dispose();
			await stream.DisposeAsync();

			_logger.LogInformation("Loaded {} mod aliases", _aliases.Count);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load aliases file, starting with empty dictionary");
			_aliases = new Dictionary<Guid, string>();
		}
	}

	[MemberNotNull(nameof(_aliases))]
	private void GuardInitialized()
	{
		if (!Initialized)
			throw new InvalidOperationException("Object not initialized!");
	}
}
