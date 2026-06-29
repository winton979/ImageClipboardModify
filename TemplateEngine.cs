using System;
using System.IO;
using ImageClipboardModify.Models;

namespace ImageClipboardModify
{
    public static class TemplateEngine
    {
        public static string Render(string imagePath)
        {
            var config = AppConfig.Load();
            var template = config.Template;

            var fileName = Path.GetFileName(imagePath);
            var fileNameNoExt = Path.GetFileNameWithoutExtension(imagePath);
            var dir = Path.GetDirectoryName(imagePath) ?? "";
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            var time = DateTime.Now.ToString("HH:mm:ss");

            return template
                .Replace("{path}", imagePath)
                .Replace("{filename}", fileName)
                .Replace("{filename_no_ext}", fileNameNoExt)
                .Replace("{dir}", dir)
                .Replace("{date}", date)
                .Replace("{time}", time)
                .Replace("{newline}", "\r\n");
        }
    }
}
