using System.Diagnostics;

namespace Bg3HonourRecovery;

public sealed class MainForm : Form
{
    private static readonly Color Background = Color.FromArgb(20, 22, 27);
    private static readonly Color Surface = Color.FromArgb(31, 34, 41);
    private static readonly Color SurfaceRaised = Color.FromArgb(42, 46, 55);
    private static readonly Color TextPrimary = Color.FromArgb(239, 235, 222);
    private static readonly Color TextSecondary = Color.FromArgb(172, 175, 184);
    private static readonly Color Gold = Color.FromArgb(202, 158, 78);
    private static readonly Color Danger = Color.FromArgb(190, 82, 75);

    private readonly ProfileRecoveryService _service;
    private readonly TextBox _pathBox = new();
    private readonly Button _browseButton = new();
    private readonly Button _autoLocateButton = new();
    private readonly SessionListControl _sessionList = new();
    private readonly Label _summaryLabel = new();
    private readonly Label _statusLabel = new();
    private readonly ProgressBar _progress = new();
    private readonly Button _selectAllButton = new();
    private readonly Button _clearButton = new();
    private readonly ModernCheckBox _autoBackupCheckBox = new()
    {
        Checked = true,
        AccessibleName = "自动备份"
    };
    private readonly Button _recoverSelectedButton = new();
    private readonly Button _recoverAllButton = new();

    public MainForm(ProfileRecoveryService service)
    {
        _service = service;
        InitializeWindow();
        BuildLayout();
        WireEvents();
    }

    private void InitializeWindow()
    {
        Text = "BG3 荣誉模式存档恢复器————by Alobal";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(960, 680);
        ClientSize = new Size(1100, 760);
        BackColor = Background;
        ForeColor = TextPrimary;
        Font = new Font("Microsoft YaHei UI", 9F);
        AutoScaleMode = AutoScaleMode.Dpi;
        AllowDrop = true;
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 24, 28, 20),
            RowCount = 5,
            ColumnCount = 1,
            BackColor = Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var title = new Label
        {
            AutoSize = true,
            Text = "BG3 荣誉模式存档恢复器————by Alobal",
            Font = new Font("Microsoft YaHei UI", 21F, FontStyle.Bold),
            ForeColor = TextPrimary,
            Margin = new Padding(0, 0, 0, 18)
        };
        root.Controls.Add(title, 0, 0);

        root.Controls.Add(BuildFilePanel(), 0, 1);
        root.Controls.Add(BuildSessionPanel(), 0, 2);
        root.Controls.Add(BuildActionPanel(), 0, 3);
        root.Controls.Add(BuildStatusPanel(), 0, 4);

        UpdateActionState();
    }

    private Control BuildFilePanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 106,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = Surface,
            ColumnCount = 3,
            RowCount = 3,
            Margin = new Padding(0, 0, 0, 16)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var label = new Label
        {
            Text = "选择存档目录下的profile8.lsf文件",
            AutoSize = true,
            ForeColor = TextPrimary,
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 7)
        };
        panel.Controls.Add(label, 0, 0);
        panel.SetColumnSpan(label, 3);

        _pathBox.Dock = DockStyle.Fill;
        _pathBox.BackColor = SurfaceRaised;
        _pathBox.ForeColor = TextPrimary;
        _pathBox.BorderStyle = BorderStyle.FixedSingle;
        _pathBox.Margin = new Padding(0, 0, 8, 4);
        panel.Controls.Add(_pathBox, 0, 1);

        ConfigureButton(_browseButton, "浏览…", SurfaceRaised, 82);
        _browseButton.Margin = new Padding(0, 0, 8, 4);
        panel.Controls.Add(_browseButton, 1, 1);

        ConfigureButton(_autoLocateButton, "自动定位", SurfaceRaised, 92);
        _autoLocateButton.Margin = new Padding(0, 0, 0, 4);
        panel.Controls.Add(_autoLocateButton, 2, 1);

        var hint = new Label
        {
            AutoSize = true,
            ForeColor = TextSecondary,
            Text = @"参考路径：C:\Users\<用户名>\AppData\Local\Larian Studios\Baldur's Gate 3\PlayerProfiles\Public\profile8.lsf",
            Margin = new Padding(0, 1, 0, 0)
        };
        panel.Controls.Add(hint, 0, 2);
        panel.SetColumnSpan(hint, 3);
        return panel;
    }

    private Control BuildSessionPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            Padding = new Padding(16, 12, 16, 14),
            RowCount = 2,
            ColumnCount = 1,
            Margin = new Padding(0, 0, 0, 14)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _summaryLabel.AutoSize = true;
        _summaryLabel.Text = "尚未读取文件";
        _summaryLabel.Font = new Font(Font, FontStyle.Bold);
        _summaryLabel.ForeColor = TextPrimary;
        _summaryLabel.Margin = new Padding(0, 0, 0, 10);
        panel.Controls.Add(_summaryLabel, 0, 0);

        _sessionList.Dock = DockStyle.Fill;
        panel.Controls.Add(_sessionList, 0, 1);
        return panel;
    }

    private Control BuildActionPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 14)
        };
        ConfigureButton(_selectAllButton, "全选", SurfaceRaised, 74);
        ConfigureButton(_clearButton, "清空", SurfaceRaised, 74);
        ConfigureButton(_recoverSelectedButton, "恢复选中项", Gold, 120, Color.FromArgb(28, 27, 23));
        ConfigureButton(_recoverAllButton, "一键全部恢复", Danger, 132);
        _clearButton.Margin = new Padding(0, 0, 18, 0);
        panel.Controls.AddRange([
            _selectAllButton,
            _clearButton,
            BuildBackupOption(),
            _recoverSelectedButton,
            _recoverAllButton
        ]);
        return panel;
    }

    private Control BuildBackupOption()
    {
        var panel = new Panel
        {
            Width = 128,
            Height = 34,
            Margin = new Padding(0, 0, 18, 0),
            BackColor = Color.Transparent
        };
        _autoBackupCheckBox.SetBounds(0, 1, 32, 32);
        panel.Controls.Add(_autoBackupCheckBox);

        var label = new Label
        {
            Text = "自动备份",
            AutoSize = false,
            Left = 38,
            Top = 0,
            Width = 88,
            Height = 34,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = TextPrimary,
            Cursor = Cursors.Hand
        };
        label.Click += (_, _) =>
        {
            if (_autoBackupCheckBox.Enabled)
            {
                _autoBackupCheckBox.Checked = !_autoBackupCheckBox.Checked;
            }
        };
        panel.Controls.Add(label);
        return panel;
    }

    private Control BuildStatusPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 28,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));

        _statusLabel.AutoEllipsis = true;
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.Text = "就绪";
        _statusLabel.ForeColor = TextSecondary;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        panel.Controls.Add(_statusLabel, 0, 0);

        _progress.Dock = DockStyle.Fill;
        _progress.Style = ProgressBarStyle.Marquee;
        _progress.MarqueeAnimationSpeed = 24;
        _progress.Visible = false;
        _progress.Margin = new Padding(8, 5, 0, 5);
        panel.Controls.Add(_progress, 1, 0);
        return panel;
    }

    private void WireEvents()
    {
        Shown += async (_, _) =>
        {
            if (File.Exists(ProfileRecoveryService.DefaultProfilePath))
            {
                _pathBox.Text = ProfileRecoveryService.DefaultProfilePath;
                await LoadProfileAsync();
            }
        };
        _browseButton.Click += async (_, _) => await BrowseAsync();
        _autoLocateButton.Click += async (_, _) => await AutoLocateAsync();
        _pathBox.KeyDown += async (_, args) =>
        {
            if (args.KeyCode == Keys.Enter)
            {
                args.SuppressKeyPress = true;
                await LoadProfileAsync();
            }
        };
        _sessionList.SelectionChanged += (_, _) => UpdateActionState();
        _sessionList.ImageOpenRequested += (_, path) => OpenHonourModeImage(path);
        _selectAllButton.Click += (_, _) => SetAllChecked(true);
        _clearButton.Click += (_, _) => SetAllChecked(false);
        _recoverSelectedButton.Click += async (_, _) => await RecoverAsync(GetCheckedGuids());
        _recoverAllButton.Click += async (_, _) => await RecoverAsync(GetAllGuids());
        DragEnter += (_, args) =>
        {
            if (args.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                args.Effect = DragDropEffects.Copy;
            }
        };
        DragDrop += async (_, args) =>
        {
            if (args.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
            {
                _pathBox.Text = files[0];
                await LoadProfileAsync();
            }
        };
    }

    private async Task BrowseAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择 BG3 profile8.lsf",
            Filter = "BG3 玩家配置 (profile8.lsf)|profile8.lsf|LSF 文件 (*.lsf)|*.lsf|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = GetInitialDirectory()
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _pathBox.Text = dialog.FileName;
            await LoadProfileAsync();
        }
    }

    private async Task AutoLocateAsync()
    {
        _pathBox.Text = ProfileRecoveryService.DefaultProfilePath;
        if (!File.Exists(_pathBox.Text))
        {
            ShowError("没有在默认位置找到 profile8.lsf。\n\n请使用“浏览”手动选择文件。", "未找到文件");
            return;
        }

        await LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        var path = _pathBox.Text.Trim().Trim('"');
        await RunBusyAsync("正在解码并扫描 profile8.lsf…", async () =>
        {
            var scan = await Task.Run(() => _service.Analyze(path));
            _sessionList.SetSessions(scan.Sessions);

            _pathBox.Text = scan.ProfilePath;
            _summaryLabel.Text = scan.Sessions.Count == 0
                ? "未发现失败的荣誉模式记录"
                : $"发现 {scan.Sessions.Count} 个可恢复战役 GUID";
            _statusLabel.Text = $"已读取 · {FormatSize(scan.FileSize)} · 修改时间 {scan.LastWriteTime:yyyy-MM-dd HH:mm:ss}";
            UpdateActionState();
        });
    }

    private async Task RecoverAsync(IReadOnlyList<string> guids)
    {
        if (guids.Count == 0)
        {
            ShowError("请先勾选至少一个 GUID。", "没有选择记录");
            return;
        }

        var path = _pathBox.Text.Trim();
        var createBackup = _autoBackupCheckBox.Checked;
        await RunBusyAsync(createBackup ? "正在备份、写回并校验…" : "正在写回并校验…", async () =>
        {
            var result = await Task.Run(() => _service.Recover(path, guids, createBackup));
            await LoadProfileAsync();
            var backupMessage = result.BackupPath is null
                ? string.Empty
                : $"\n\n备份文件：\n{result.BackupPath}";
            MessageBox.Show(
                this,
                $"执行成功，已恢复 {result.RemovedEntries} 个荣誉存档。{backupMessage}",
                "恢复成功",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        });
    }

    private async Task RunBusyAsync(string message, Func<Task> operation)
    {
        SetBusy(true, message);
        try
        {
            await operation();
        }
        catch (Exception exception)
        {
            var details = exception.InnerException?.Message;
            ShowError(
                string.IsNullOrWhiteSpace(details)
                    ? exception.Message
                    : $"{exception.Message}\n\n详细信息：{details}",
                "操作失败");
            _statusLabel.Text = "操作失败";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy, string? message = null)
    {
        UseWaitCursor = busy;
        _progress.Visible = busy;
        _browseButton.Enabled = !busy;
        _autoLocateButton.Enabled = !busy;
        _pathBox.Enabled = !busy;
        _sessionList.Enabled = !busy;
        _autoBackupCheckBox.Enabled = !busy;
        if (busy && message is not null)
        {
            _statusLabel.Text = message;
        }

        UpdateActionState(busy);
    }

    private void UpdateActionState() => UpdateActionState(UseWaitCursor);

    private void UpdateActionState(bool busy)
    {
        var hasItems = _sessionList.ItemCount > 0;
        var hasChecked = _sessionList.CheckedCount > 0;
        _selectAllButton.Enabled = !busy && hasItems;
        _clearButton.Enabled = !busy && hasChecked;
        _recoverSelectedButton.Enabled = !busy && hasChecked;
        _recoverAllButton.Enabled = !busy && hasItems;
    }

    private void SetAllChecked(bool value)
    {
        _sessionList.SetAllChecked(value);
        UpdateActionState();
    }

    private IReadOnlyList<string> GetCheckedGuids() => _sessionList.GetCheckedGuids();

    private IReadOnlyList<string> GetAllGuids() => _sessionList.GetAllGuids();

    private void OpenHonourModeImage(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(imagePath) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            ShowError(exception.Message, "无法打开图片");
        }
    }

    private string GetInitialDirectory()
    {
        var current = _pathBox.Text.Trim();
        if (File.Exists(current))
        {
            return Path.GetDirectoryName(current)!;
        }

        var defaultDirectory = Path.GetDirectoryName(ProfileRecoveryService.DefaultProfilePath)!;
        return Directory.Exists(defaultDirectory)
            ? defaultDirectory
            : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }

    private static void ConfigureButton(
        Button button,
        string text,
        Color backColor,
        int width,
        Color? foreColor = null)
    {
        button.Text = text;
        button.Width = width;
        button.Height = 34;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = backColor;
        button.ForeColor = foreColor ?? TextPrimary;
        button.Cursor = Cursors.Hand;
        button.Margin = new Padding(0, 0, 10, 0);
    }

    private void ShowError(string message, string title)
    {
        MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static string FormatSize(long bytes)
    {
        return bytes >= 1024 * 1024
            ? $"{bytes / 1024d / 1024d:0.##} MB"
            : $"{bytes / 1024d:0.##} KB";
    }

}
