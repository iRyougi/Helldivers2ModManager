namespace Helldivers2ModManager.Models;

internal sealed class ConflictInfo
{
	public required string PatchPrefix { get; init; }
	public required ModData[] ConflictingMods { get; init; }
}
