using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ImageClipboardModify.Models;

namespace ImageClipboardModify
{
    public static class ImageSaver
    {
        public static string Save(Image image)
        {
            var config = AppConfig.Load();
            var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            var dir = Path.Combine(config.SaveFolder, dateFolder);
            Directory.CreateDirectory(dir);

            var fileName = DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + "." + config.SaveFormat;
            var path = Path.Combine(dir, fileName);

            ImageFormat format;
            switch (config.SaveFormat.ToLowerInvariant())
            {
                case "png":
                    format = ImageFormat.Png;
                    break;
                case "jpg":
                case "jpeg":
                    format = ImageFormat.Jpeg;
                    break;
                case "bmp":
                    format = ImageFormat.Bmp;
                    break;
                default:
                    format = ImageFormat.Png;
                    break;
            }

            image.Save(path, format);
            return path;
        }
    }
}
