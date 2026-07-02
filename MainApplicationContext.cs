using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using ImageClipboardModify.Forms;
using ImageClipboardModify.Models;

namespace ImageClipboardModify
{
    public class MainApplicationContext : ApplicationContext
    {
        private readonly MainForm _mainForm;
        private readonly ClipboardWatcher _clipboardWatcher;
        private readonly NotifyIcon _trayIcon;
        private readonly AppConfig _config;

        private bool _disposed;

        public MainApplicationContext()
        {
            _config = AppConfig.Load();

            _mainForm = new MainForm();
            _mainForm.ShowSettingsRequested += OpenSettings;

            _clipboardWatcher = new ClipboardWatcher(_mainForm);
            _clipboardWatcher.ClipboardImageChanged += OnClipboardImageChanged;

            _mainForm.CopyRequested += OnCopyRequested;
            _mainForm.PasteAndCopyRequested += OnPasteAndCopyRequested;
            _mainForm.ClearHistoryRequested += OnClearHistory;

            _trayIcon = CreateTrayIcon();

            if (_config.AutoStartup && !StartupManager.IsEnabled())
                StartupManager.SetEnabled(true);

            // load history into right panel
            _mainForm.LoadHistory(_config.SaveFolder);

            _mainForm.Show();
        }

        private void OnClipboardImageChanged(Image image)
        {
            _mainForm.ShowClipboardImage(image);
        }

        private void OnCopyRequested()
        {
            var image = _mainForm.CurrentImage;
            if (image == null)
            {
                _trayIcon.ShowBalloonTip(2000, "No Image", "No image to copy.", ToolTipIcon.Warning);
                return;
            }

            DoCopy(image);
        }

        // 主动从剪切板拉取最新图片再 copy,兜底 watcher 未侦测到的场景
        private void OnPasteAndCopyRequested()
        {
            var image = ClipboardWatcher.TryReadClipboardImage();
            if (image == null)
            {
                _trayIcon.ShowBalloonTip(2000, "No Image", "Clipboard has no image.", ToolTipIcon.Warning);
                return;
            }

            using (image)
            {
                _mainForm.ShowClipboardImage(image);
                DoCopy(_mainForm.CurrentImage);
            }
        }

        private void DoCopy(Image image)
        {
            if (image == null)
            {
                _trayIcon.ShowBalloonTip(2000, "No Image", "No image to copy.", ToolTipIcon.Warning);
                return;
            }

            try
            {
                var path = SaveImageToDisk(image);
                var text = TemplateEngine.Render(path);

                Clipboard.SetText(text);

                _mainForm.SetStatus($"Copied: {Path.GetFileName(path)}");
                _mainForm.AddToHistory(path);
                _trayIcon.ShowBalloonTip(2000, "Copied", Path.GetFileName(path), ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                _mainForm.SetStatus($"Copy failed: {ex.Message}");
                _trayIcon.ShowBalloonTip(2000, "Copy Failed", ex.Message, ToolTipIcon.Error);
            }
        }

        private string SaveImageToDisk(Image image)
        {
            var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            var dir = Path.Combine(_config.SaveFolder, dateFolder);
            Directory.CreateDirectory(dir);

            var fileName = DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + "." + _config.SaveFormat;
            var path = Path.Combine(dir, fileName);

            ImageFormat format;
            switch (_config.SaveFormat.ToLowerInvariant())
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

        private NotifyIcon CreateTrayIcon()
        {
            var icon = new NotifyIcon
            {
                Icon = LoadTrayIcon(),
                Text = "Image Clipboard Modify",
                Visible = true
            };

            var menu = new ContextMenuStrip();

            var showItem = new ToolStripMenuItem("Show Window");
            showItem.Click += (_, _) => ShowMainWindow();
            menu.Items.Add(showItem);

            menu.Items.Add(new ToolStripSeparator());

            var openDirItem = new ToolStripMenuItem("Open Image Folder");
            openDirItem.Click += (_, _) => OpenImageFolder();
            menu.Items.Add(openDirItem);

            var settingsItem = new ToolStripMenuItem("Settings...");
            settingsItem.Click += (_, _) => OpenSettings();
            menu.Items.Add(settingsItem);

            menu.Items.Add(new ToolStripSeparator());

            var startupItem = new ToolStripMenuItem("Start with Windows")
            {
                CheckOnClick = true,
                Checked = _config.AutoStartup
            };
            startupItem.Click += (_, _) =>
            {
                _config.AutoStartup = startupItem.Checked;
                StartupManager.SetEnabled(_config.AutoStartup);
                _config.Save();
            };
            menu.Items.Add(startupItem);

            menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (_, _) => ExitThread();
            menu.Items.Add(exitItem);

            icon.ContextMenuStrip = menu;
            icon.DoubleClick += (_, _) => ShowMainWindow();

            return icon;
        }

        private void ShowMainWindow()
        {
            _mainForm.Show();
            _mainForm.WindowState = FormWindowState.Normal;
            _mainForm.BringToFront();
            _mainForm.Activate();
        }

        private static Icon LoadTrayIcon()
        {
            var stream = typeof(MainApplicationContext).Assembly
                .GetManifestResourceStream("ImageClipboardModify.Resources.tray.ico");
            return stream != null ? new Icon(stream) : SystemIcons.Application;
        }

        private void OpenImageFolder()
        {
            var dir = _config.SaveFolder;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            Process.Start("explorer.exe", dir);
        }

        private void OpenSettings()
        {
            using (var form = new SettingsForm(_config))
            {
                if (form.ShowDialog() == DialogResult.OK)
                    _config.Reload();
            }
        }

        private void OnClearHistory()
        {
            var dir = _config.SaveFolder;
            if (!Directory.Exists(dir))
            {
                _mainForm.ClearHistory();
                return;
            }

            var result = MessageBox.Show(
                "Delete all saved images from disk?\n\nThis cannot be undone.",
                "Clear All History",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);
            }
            catch { }

            _mainForm.ClearHistory();
            _mainForm.SetStatus("History cleared");
            _trayIcon.ShowBalloonTip(2000, "Cleared", "All images deleted.", ToolTipIcon.Info);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) { base.Dispose(disposing); return; }
            _disposed = true;

            if (disposing)
            {
                _clipboardWatcher.Dispose();
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _mainForm.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
