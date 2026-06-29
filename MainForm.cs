using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ImageClipboardModify;

public class MainForm : Form
{
    public event Action? ClipboardUpdate;
    public event Action? CopyRequested;
    public event Action? ClearHistoryRequested;

    private const int WM_CLIPBOARDUPDATE = 0x031D;

    private PictureBox _preview = null!;
    private Label _infoLabel = null!;
    private Button _copyButton = null!;
    private Button _openButton = null!;
    private StatusStrip _statusBar = null!;
    private ToolStripStatusLabel _statusLabel = null!;
    private ListBox _historyList = null!;
    private PictureBox _historyPreview = null!;
    private Label _historyInfoLabel = null!;

    private Image? _currentImage;
    private readonly List<string> _historyPaths = new();

    public Image? CurrentImage => _currentImage;

    public MainForm()
    {
        InitializeComponent();
        var _ = Handle;
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        Text = "Image Clipboard Modify";
        ClientSize = new Size(1100, 680);
        MinimumSize = new Size(750, 480);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = LoadAppIcon();

        // Menu bar
        var menuBar = new MenuStrip();
        var fileMenu = new ToolStripMenuItem("File");
        var copyItem = new ToolStripMenuItem("Copy to Clipboard") { ShortcutKeys = Keys.Control | Keys.C };
        copyItem.Click += (_, _) => CopyRequested?.Invoke();
        fileMenu.DropDownItems.Add(copyItem);
        var openItem = new ToolStripMenuItem("Open in Viewer") { ShortcutKeys = Keys.Control | Keys.O };
        openItem.Click += (_, _) => OpenInViewer();
        fileMenu.DropDownItems.Add(openItem);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        var clearItem = new ToolStripMenuItem("Clear All History");
        clearItem.Click += (_, _) => ClearHistoryRequested?.Invoke();
        fileMenu.DropDownItems.Add(clearItem);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => Close();
        fileMenu.DropDownItems.Add(exitItem);
        menuBar.Items.Add(fileMenu);

        var editMenu = new ToolStripMenuItem("Edit");
        var settingsItem = new ToolStripMenuItem("Settings...");
        settingsItem.Click += (_, _) => ShowSettingsRequested?.Invoke();
        editMenu.DropDownItems.Add(settingsItem);
        menuBar.Items.Add(editMenu);

        MainMenuStrip = menuBar;
        Controls.Add(menuBar);

        // Right panel (fixed width)
        var rightPanel = new Panel
        {
            Dock = DockStyle.Right,
            Width = 300
        };

        // history list (top half)
        _historyList = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9f)
        };
        _historyList.SelectedIndexChanged += OnHistorySelect;
        _historyList.DoubleClick += OnHistoryDoubleClick;
        rightPanel.Controls.Add(_historyList);

        // history preview (bottom half, fixed height)
        var rightBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 250
        };

        _historyInfoLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            TextAlign = ContentAlignment.TopLeft,
            Font = new Font("Segoe UI", 8f),
            ForeColor = SystemColors.GrayText,
            Padding = new Padding(4)
        };
        rightBottom.Controls.Add(_historyInfoLabel);

        _historyPreview = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(40, 40, 40)
        };
        rightBottom.Controls.Add(_historyPreview);

        rightPanel.Controls.Add(rightBottom);

        // === LEFT: preview + buttons ===
        var leftPanel = new Panel { Dock = DockStyle.Fill };

        _preview = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        leftPanel.Controls.Add(_preview);

        var leftBottom = new Panel { Dock = DockStyle.Bottom, Height = 55 };

        _infoLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f),
            Text = "No image in clipboard"
        };
        leftBottom.Controls.Add(_infoLabel);

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            FlowDirection = FlowDirection.LeftToRight
        };

        _copyButton = new Button
        {
            Text = "Copy to Clipboard",
            Width = 140,
            Height = 28,
            Enabled = false
        };
        _copyButton.Click += (_, _) => CopyRequested?.Invoke();
        btnPanel.Controls.Add(_copyButton);

        _openButton = new Button
        {
            Text = "Open in Viewer",
            Width = 120,
            Height = 28,
            Enabled = false
        };
        _openButton.Click += (_, _) => OpenInViewer();
        btnPanel.Controls.Add(_openButton);

        leftBottom.Controls.Add(btnPanel);
        leftPanel.Controls.Add(leftBottom);

        Controls.Add(leftPanel);
        Controls.Add(rightPanel);

        // Status bar
        _statusBar = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel
        {
            Text = "Ready",
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _statusBar.Items.Add(_statusLabel);
        Controls.Add(_statusBar);

        ClipboardUpdate += OnClipboardUpdate;
        FormClosing += OnFormClosing;
        Resize += OnResize;

        ResumeLayout(false);
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            WindowState = FormWindowState.Minimized;
            Hide();
        }
    }

    private void OnResize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
            Hide();
    }

    public void ShowClipboardImage(Image image)
    {
        try
        {
            _currentImage?.Dispose();
            _currentImage = (Image)image.Clone();

            _preview.Image?.Dispose();
            _preview.Image = (Image)_currentImage.Clone();

            _infoLabel.Text = $"Clipboard image | {DateTime.Now:yyyy-MM-dd HH:mm:ss} | {_currentImage.Width}x{_currentImage.Height}";
            _copyButton.Enabled = true;
            _openButton.Enabled = true;
            _statusLabel.Text = $"Image ready | {_currentImage.Width}x{_currentImage.Height}";
        }
        catch
        {
            ClearImage();
        }
    }

    public void ClearImage()
    {
        _currentImage?.Dispose();
        _currentImage = null;
        _preview.Image?.Dispose();
        _preview.Image = null;
        _infoLabel.Text = "No image in clipboard";
        _copyButton.Enabled = false;
        _openButton.Enabled = false;
        _statusLabel.Text = "Ready";
    }

    public void SetStatus(string text)
    {
        _statusLabel.Text = text;
    }

    public void AddToHistory(string path)
    {
        if (_historyPaths.Contains(path)) return;
        _historyPaths.Insert(0, path);
        _historyList.Items.Insert(0, Path.GetFileName(path));
        if (_historyList.Items.Count > 100)
        {
            _historyPaths.RemoveAt(_historyPaths.Count - 1);
            _historyList.Items.RemoveAt(_historyList.Items.Count - 1);
        }
    }

    public void ClearHistory()
    {
        _historyPaths.Clear();
        _historyList.Items.Clear();
        _historyPreview.Image?.Dispose();
        _historyPreview.Image = null;
        _historyInfoLabel.Text = "";
    }

    public void LoadHistory(string folder)
    {
        if (!Directory.Exists(folder)) return;

        _historyPaths.Clear();
        _historyList.Items.Clear();

        var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
        Array.Sort(files, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));

        var count = Math.Min(files.Length, 100);
        for (int i = 0; i < count; i++)
        {
            _historyPaths.Add(files[i]);
            _historyList.Items.Add(Path.GetFileName(files[i]));
        }

        if (_historyList.Items.Count > 0)
            _historyList.SelectedIndex = 0;
    }

    private void OnHistorySelect(object? sender, EventArgs e)
    {
        var idx = _historyList.SelectedIndex;
        if (idx < 0 || idx >= _historyPaths.Count) return;

        var path = _historyPaths[idx];
        try
        {
            _historyPreview.Image?.Dispose();
            _historyPreview.Image = Image.FromFile(path);
            var fi = new FileInfo(path);
            _historyInfoLabel.Text = $"{Path.GetFileName(path)}\r\n{fi.LastWriteTime:yyyy-MM-dd HH:mm:ss}\r\n{fi.Length / 1024} KB";
        }
        catch
        {
            _historyPreview.Image = null;
            _historyInfoLabel.Text = "";
        }
    }

    private void OnHistoryDoubleClick(object? sender, EventArgs e)
    {
        var idx = _historyList.SelectedIndex;
        if (idx < 0 || idx >= _historyPaths.Count) return;

        var path = _historyPaths[idx];
        try
        {
            var img = Image.FromFile(path);
            ShowClipboardImage(img);
            SetStatus($"Loaded: {Path.GetFileName(path)}");
        }
        catch { }
    }

    private string? GetTargetImage()
    {
        // from current preview or selected history
        if (_currentImage != null)
        {
            // find the most recent path that matches current display
            if (_historyPaths.Count > 0)
                return _historyPaths[0];
        }
        var idx = _historyList.SelectedIndex;
        if (idx >= 0 && idx < _historyPaths.Count)
            return _historyPaths[idx];
        return null;
    }

    private void OpenInViewer()
    {
        var path = GetTargetImage();
        if (path == null || !File.Exists(path))
        {
            SetStatus("No image to open");
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            SetStatus($"Opened: {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            SetStatus($"Open failed: {ex.Message}");
        }
    }

    public event Action? ShowSettingsRequested;

    private void OnClipboardUpdate()
    {
        // ClipboardWatcher handles getting the image and calling ShowClipboardImage
    }

    private static Icon LoadAppIcon()
    {
        var stream = typeof(MainForm).Assembly
            .GetManifestResourceStream("ImageClipboardModify.Resources.tray.ico");
        return stream != null ? new Icon(stream) : SystemIcons.Application;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_CLIPBOARDUPDATE)
            ClipboardUpdate?.Invoke();

        base.WndProc(ref m);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _currentImage?.Dispose();
            _preview.Image?.Dispose();
            _historyPreview.Image?.Dispose();
        }
        base.Dispose(disposing);
    }
}
