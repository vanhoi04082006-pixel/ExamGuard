using System.Text.Json;

namespace ExamGuard.Core.Configuration;

public sealed class ConfigStore
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public ConfigStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, "examguard.json");
    }

    public string FilePath => _filePath;

    public AppConfig Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_filePath))
                return new AppConfig();
            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<AppConfig>(json)
                    ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }
    }

    public void Save(AppConfig config)
    {
        lock (_lock)
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            string? dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(_filePath, json);
        }
    }
}
