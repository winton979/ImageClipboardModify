using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ImageClipboardModify
{
    public static class StartupManager
    {
        private const string AppName = "ImageClipboardModify";
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static bool IsEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
            {
                return key?.GetValue(AppName) != null;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
            {
                if (key == null) return;

                if (enabled)
                {
                    var exePath = Application.ExecutablePath;
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
        }
    }
}
