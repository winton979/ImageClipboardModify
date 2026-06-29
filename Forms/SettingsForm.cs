using System;
using System.Drawing;
using System.Windows.Forms;
using ImageClipboardModify.Models;

namespace ImageClipboardModify.Forms;

public class SettingsForm : Form
{
    private readonly AppConfig _config;
    private TextBox _templateBox = null!;
    private TextBox _saveFolderBox = null!;
    private ComboBox _formatCombo = null!;
    private CheckBox _startupCheck = null!;

    public event Action? SettingsChanged;

    public SettingsForm(AppConfig config)
    {
        _config = config;
        InitializeUI();
        LoadValues();
    }

    private void InitializeUI()
    {
        Text = "Settings - ImageClipboardModify";
        ClientSize = new Size(520, 480);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var margin = 15;
        var right = ClientSize.Width - margin;
        var inputW = right - margin;
        var browseBtnW = 50;

        // Save Folder
        AddLabel("Save Folder:", margin, margin);
        _saveFolderBox = new TextBox { Location = new Point(margin, margin + 20), Size = new Size(inputW - browseBtnW - 8, 26) };
        Controls.Add(_saveFolderBox);
        var browseBtn = new Button { Text = "...", Location = new Point(right - browseBtnW, margin + 20), Size = new Size(browseBtnW, 26) };
        browseBtn.Click += (_, _) =>
        {
            using var dlg = new FolderBrowserDialog();
            dlg.SelectedPath = _saveFolderBox.Text;
            if (dlg.ShowDialog() == DialogResult.OK)
                _saveFolderBox.Text = dlg.SelectedPath;
        };
        Controls.Add(browseBtn);

        // Format
        var formatY = margin + 55;
        AddLabel("Format:", margin, formatY);
        _formatCombo = new ComboBox { Location = new Point(100, formatY - 3), Size = new Size(100, 26), DropDownStyle = ComboBoxStyle.DropDownList };
        _formatCombo.Items.AddRange(["png", "jpg", "bmp"]);
        Controls.Add(_formatCombo);

        // Template
        var tmplY = formatY + 35;
        AddLabel("Template:", margin, tmplY);
        var tmplInputY = tmplY + 20;
        _templateBox = new TextBox
        {
            Location = new Point(margin, tmplInputY),
            Size = new Size(inputW, 180),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9f)
        };
        Controls.Add(_templateBox);

        // hint
        var hintY = tmplInputY + 185;
        var hint = new Label
        {
            Text = "Variables: {path} {filename} {date} {time} {newline}",
            Location = new Point(margin, hintY),
            AutoSize = true,
            ForeColor = SystemColors.GrayText
        };
        Controls.Add(hint);

        // Startup
        var startupY = hintY + 22;
        _startupCheck = new CheckBox { Text = "Start with Windows", Location = new Point(margin, startupY), AutoSize = true };
        Controls.Add(_startupCheck);

        // Buttons
        var btnY = startupY + 30;
        var saveBtn = new Button { Text = "Save", Location = new Point(right - 165, btnY), Size = new Size(75, 28), DialogResult = DialogResult.OK };
        var cancelBtn = new Button { Text = "Cancel", Location = new Point(right - 80, btnY), Size = new Size(75, 28), DialogResult = DialogResult.Cancel };
        saveBtn.Click += (_, _) => SaveAndClose();
        Controls.Add(saveBtn);
        Controls.Add(cancelBtn);

        AcceptButton = saveBtn;
        CancelButton = cancelBtn;
    }

    private void AddLabel(string text, int x, int y)
    {
        Controls.Add(new Label { Text = text, Location = new Point(x, y), AutoSize = true });
    }

    private void LoadValues()
    {
        _saveFolderBox.Text = _config.SaveFolder;
        _formatCombo.SelectedItem = _config.SaveFormat;
        _templateBox.Text = _config.Template;
        _startupCheck.Checked = _config.AutoStartup;
    }

    private void SaveAndClose()
    {
        _config.SaveFolder = _saveFolderBox.Text;
        _config.SaveFormat = _formatCombo.SelectedItem?.ToString() ?? "png";
        _config.Template = _templateBox.Text;
        _config.AutoStartup = _startupCheck.Checked;
        _config.Save();

        StartupManager.SetEnabled(_config.AutoStartup);
        SettingsChanged?.Invoke();
    }
}
