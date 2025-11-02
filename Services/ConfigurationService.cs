namespace Lab4.Services;

using System.Text.Json;
using Lab4.Models;

public class ConfigurationService
{
    private static readonly string AppName = "DailyMealPlanner";
    private static readonly string ConfigFileName = "user-config.json";

    public static string GetConfigDirectory()
    {
        var configDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appConfigDir = Path.Combine(configDir, AppName);

        // Ensure directory exists
        if (!Directory.Exists(appConfigDir))
        {
            Directory.CreateDirectory(appConfigDir);
        }

        return appConfigDir;
    }

    public static string GetConfigFilePath()
    {
        return Path.Combine(GetConfigDirectory(), ConfigFileName);
    }

    public static void SaveUserConfig(User user)
    {
        try
        {
            var configPath = GetConfigFilePath();
            var json = JsonSerializer.Serialize(user, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(configPath, json);
            Logger.Instance.Information("User configuration saved to {Path}", configPath);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to save user configuration");
        }
    }

    public static User? LoadUserConfig()
    {
        try
        {
            var configPath = GetConfigFilePath();

            if (!File.Exists(configPath))
            {
                Logger.Instance.Information("No existing configuration found, using defaults");
                return null;
            }

            var json = File.ReadAllText(configPath);
            var user = JsonSerializer.Deserialize<User>(json);

            if (user != null)
            {
                Logger.Instance.Information("User configuration loaded from {Path}", configPath);
            }

            return user;
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, "Failed to load user configuration");
            return null;
        }
    }
}
