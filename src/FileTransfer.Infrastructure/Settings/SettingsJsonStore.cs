using System.Text.Json;
using FileTransfer.Core.Contracts;
using FileTransfer.Core.Models;

namespace FileTransfer.Infrastructure.Settings;

public sealed class SettingsJsonStore : ISettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _settingsPath;

    public SettingsJsonStore()
    {
        _settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
    }

    public async Task<AppSettings?> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_settingsPath))
        {
            return null;
        }

        await using FileStream stream = File.OpenRead(_settingsPath);
        return await JsonSerializer.DeserializeAsync<AppSettings>(stream, SerializerOptions, cancellationToken);
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);

        await using FileStream stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}
