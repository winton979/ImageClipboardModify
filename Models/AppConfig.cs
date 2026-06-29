using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageClipboardModify.Models;

public class AppConfig
{
    public string SaveFolder { get; set; } = GetDefaultSaveFolder();
    public string Template { get; set; } = "请查看图片：\r\n\r\n{path}";
    public bool AutoStartup { get; set; } = true;
    public string SaveFormat { get; set; } = "png";

    private static readonly string ConfigPath =
        Path.Combine(AppContext.BaseDirectory, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static string GetDefaultSaveFolder()
    {
        return Directory.Exists("D:\\") ? "D:\\ClipboardImages" : "C:\\ClipboardImages";
    }

    public static AppConfig Load()
    {
        if (!File.Exists(ConfigPath))
            return new AppConfig();

        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }

    public void Reload()
    {
        var loaded = Load();
        SaveFolder = loaded.SaveFolder;
        Template = loaded.Template;
        AutoStartup = loaded.AutoStartup;
        SaveFormat = loaded.SaveFormat;
    }
}
