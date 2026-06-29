using System;
using System.IO;
using Newtonsoft.Json;

namespace ImageClipboardModify.Models
{
    public class AppConfig
    {
        public string SaveFolder { get; set; } = GetDefaultSaveFolder();
        public string Template { get; set; } = "请查看图片：\r\n\r\n{path}";
        public bool AutoStartup { get; set; } = true;
        public string SaveFormat { get; set; } = "png";

        private static readonly string ConfigPath =
            Path.Combine(AppContext.BaseDirectory, "config.json");

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
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
                return JsonConvert.DeserializeObject<AppConfig>(json, JsonSettings) ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }

        public void Save()
        {
            var json = JsonConvert.SerializeObject(this, JsonSettings);
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
}
