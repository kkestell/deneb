using System.Runtime.InteropServices;
using System.Text.Json;

namespace Deneb.Configuration;

public class DenebConfig
{
    public string LibraryPath { get; set; }
    
    public bool Verbose { get; set; }
    
    public static DenebConfig Load()
    {
        if (!File.Exists(ConfigPath))
            throw new FileNotFoundException($"Config file not found at path: {ConfigPath}");

        var jsonContent = File.ReadAllText(ConfigPath);

        var config = JsonSerializer.Deserialize<DenebConfig>(jsonContent);

        if (config is null)
            throw new Exception($"Failed to deserialize config file at path: {ConfigPath}");

        return config;
    }

    public static string ConfigPath
    {
        get
        {
            string defaultConfigPath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                defaultConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "deneb", "config.json");
            }
            else
            {
                defaultConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "deneb", "config.json");
            }
        
            var configPath = Environment.GetEnvironmentVariable("DENEB_CONFIG") ?? defaultConfigPath;
            return configPath;
        }
    }
}