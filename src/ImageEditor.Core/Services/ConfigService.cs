using System.Text.Json;
using ImageEditor.Core.Models;

namespace ImageEditor.Core.Services;

public sealed class ConfigService
{
    public const string DefaultConfigFileName = "nametag.config.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public TemplateConfig LoadOrCreateDefault(string configPath)
    {
        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<TemplateConfig>(json, JsonOptions) ?? new TemplateConfig();
        }

        var defaultConfig = new TemplateConfig();
        Save(defaultConfig, configPath);
        return defaultConfig;
    }

    public TemplateConfig Load(string configPath)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Config file not found: {configPath}", configPath);
        }

        string json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<TemplateConfig>(json, JsonOptions) ?? new TemplateConfig();
    }

    public void Save(TemplateConfig config, string configPath)
    {
        string? dir = Path.GetDirectoryName(Path.GetFullPath(configPath));
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(configPath, json);
    }

    public static string ResolveWorkspaceRoot(string? startDirectory = null)
    {
        string current = Path.GetFullPath(startDirectory ?? Directory.GetCurrentDirectory());
        var dir = new DirectoryInfo(current);

        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, DefaultConfigFileName)) ||
                File.Exists(Path.Combine(dir.FullName, "ImageEditor.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return current;
    }

    public static string ResolvePath(string path, string baseDirectory)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }
}
