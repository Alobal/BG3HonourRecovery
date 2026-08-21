using System.Drawing.Drawing2D;

namespace Bg3HonourRecovery;

internal sealed class SessionListControl : UserControl
{
    private const int HeaderHeight = 42;
    private const int RowHeight = 159;
    private const int RowGap = 8;
    private const int RowPitch = RowHeight + RowGap;
    private readonly Panel _header = new();
    private readonly Panel _viewport = new();
    private readonly ModernScrollBar _scrollBar = new();
    private readonly List<SessionRowControl> _rows = [];

    public event EventHandler? SelectionChanged;
    public event EventHandler<string>? ImageOpenRequested;

    public int ItemCount => _rows.Count;
    public int CheckedCount => _rows.Count(row => row.Checked);

    public SessionListControl()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(31, 34, 41);
        BuildHeader();

        _viewport.BackColor = Color.FromArgb(31, 34, 41);
        _viewport.TabStop = true;
        _viewport.MouseWheel += (_, args) => ScrollBy(-Math.Sign(args.Delta) * 72);
        _viewport.MouseEnter += (_, _) => _viewport.Focus();
        Controls.Add(_viewport);

        _scrollBar.ValueChanged += (_, _) => LayoutRows();
        Controls.Add(_scrollBar);

        Resize += (_, _) => LayoutList();
        LayoutList();
    }

    public void SetSessions(IReadOnlyList<DisabledSession> sessions)
    {
        SuspendLayout();
        try
        {
            foreach (var row in _rows)
            {
                row.Dispose();
            }
            _rows.Clear();
            _viewport.Controls.Clear();

            foreach (var session in sessions)
            {
                Bitmap? thumbnail = null;
                if (session.ImagePath is not null)
                {
                    try
                    {
                        thumbnail = HonourModeImageLoader.LoadThumbnail(session.ImagePath, 240, 135);
                    }
                    catch
                    {
                        thumbnail = null;
                    }
                }

                var row = new SessionRowControl(session, thumbnail);
                row.CheckedChanged += (_, _) => SelectionChanged?.Invoke(this, EventArgs.Empty);
                row.ImageOpenRequested += (_, path) => ImageOpenRequested?.Invoke(this, path);
                row.MouseWheel += (_, args) => ScrollBy(-Math.Sign(args.Delta) * 72);
                _rows.Add(row);
                _viewport.Controls.Add(row);
            }

            _scrollBar.Value = 0;
            LayoutList();
        }
        finally
        {
            ResumeLayout(performLayout: true);
        }
    }

    public IReadOnlyList<string> GetCheckedGuids() => _rows
        .Where(row => row.Checked)
        .Select(row => row.Guid)
        .ToArray();

    public IReadOnlyList<string> GetAllGuids() => _rows
        .Select(row => row.Guid)
        .ToArray();

    public void SetAllChecked(bool value)
    {
        foreach (var row in _rows)
        {
            row.Checked = value;
        }
    }

    private void BuildHeader()
    {
        _header.Height = HeaderHeight;
        _header.BackColor = Color.FromArgb(37, 40, 48);
        Controls.Add(_header);

        var preview = CreateHeaderLabel("预览");
        preview.Left = 60;
        preview.Width = 240;
        _header.Controls.Add(preview);

        var guid = CreateHeaderLabel("GUID");
        guid.Left = 324;
        guid.Width = 420;
        _header.Controls.Add(guid);
    }

    private static Label CreateHeaderLabel(string text) => new()
    {
        Text = text,
        Top = 0,
        Height = HeaderHeight,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Color.FromArgb(181, 184, 193),
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
        BackColor = Color.Transparent
    };

    private void LayoutList()
    {
        _header.SetBounds(0, 0, Width, HeaderHeight);
        var viewportHeight = Math.Max(0, Height - HeaderHeight);
        var contentHeight = _rows.Count == 0 ? 0 : _rows.Count * RowPitch - RowGap;
        var maximum = Math.Max(0, contentHeight - viewportHeight);
        _scrollBar.SetRange(maximum, viewportHeight);

        var scrollWidth = _scrollBar.Visible ? 14 : 0;
        _viewport.SetBounds(0, HeaderHeight, Math.Max(0, Width - scrollWidth), viewportHeight);
        _scrollBar.SetBounds(Math.Max(0, Width - 14), HeaderHeight, 14, viewportHeight);
        LayoutRows();
    }

    private void LayoutRows()
    {
        var offset = _scrollBar.Value;
        for (var index = 0; index < _rows.Count; index++)
        {
            var row = _rows[index];
            var top = index * RowPitch - offset;
            row.SetBounds(0, top, _viewport.ClientSize.Width, RowHeight);
            row.Visible = top < _viewport.ClientSize.Height && top + RowHeight > 0;
        }
    }

    private void ScrollBy(int amount)
    {
        _scrollBar.Value = Math.Clamp(_scrollBar.Value + amount, 0, _scrollBar.Maximum);
    }
}

internal sealed class SessionRowControl : Control
{
    private readonly ModernCheckBox _checkBox = new();
    private readonly ThumbnailControl _thumbnail;
    private readonly Label _guidLabel = new();

    public event EventHandler? CheckedChanged;
    public event EventHandler<string>? ImageOpenRequested;

    public string Guid { get; }

    public bool Checked
    {
        get => _checkBox.Checked;
        set => _checkBox.Checked = value;
    }

    public SessionRowControl(DisabledSession session, Bitmap? thumbnail)
    {
        Guid = session.Guid;
        DoubleBuffered = true;
        BackColor = Color.FromArgb(42, 46, 55);

        _checkBox.AccessibleName = $"选择 {session.Guid}";
        _checkBox.CheckedChanged += (_, _) => CheckedChanged?.Invoke(this, EventArgs.Empty);
        Controls.Add(_checkBox);

        _thumbnail = new ThumbnailControl(thumbnail, session.ImagePath);
        _thumbnail.ImageOpenRequested += (_, path) => ImageOpenRequested?.Invoke(this, path);
        Controls.Add(_thumbnail);

        _guidLabel.Text = session.Guid;
        _guidLabel.ForeColor = Color.FromArgb(239, 235, 222);
        _guidLabel.BackColor = Color.Transparent;
        _guidLabel.Font = new Font("Cascadia Mono", 11F, FontStyle.Regular);
        _guidLabel.TextAlign = ContentAlignment.MiddleLeft;
        _guidLabel.AutoEllipsis = true;
        Controls.Add(_guidLabel);

        Resize += (_, _) => LayoutRow();
        Click += (_, _) => _checkBox.Checked = !_checkBox.Checked;
        _guidLabel.Click += (_, _) => _checkBox.Checked = !_checkBox.Checked;
        LayoutRow();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Color.FromArgb(58, 63, 74));
        e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
    }

    private void LayoutRow()
    {
        _checkBox.SetBounds(16, (Height - 32) / 2, 32, 32);
        _thumbnail.SetBounds(60, 12, 240, 135);
        _guidLabel.SetBounds(324, 0, Math.Max(0, Width - 348), Height);
    }
}

internal sealed class ThumbnailControl : Control
{
    private readonly Bitmap? _image;
    private readonly string? _imagePath;
    private bool _hovered;

    public event EventHandler<string>? ImageOpenRequested;

    public ThumbnailControl(Bitmap? image, string? imagePath)
    {
        _image = image;
        _imagePath = imagePath;
        DoubleBuffered = true;
        Cursor = image is null ? Cursors.Default : Cursors.Hand;

        MouseEnter += (_, _) =>
        {
            if (_image is not null)
            {
                _hovered = true;
                Invalidate();
            }
        };
        MouseLeave += (_, _) =>
        {
            _hovered = false;
            Invalidate();
        };
        MouseClick += (_, _) =>
        {
            if (_image is not null && _imagePath is not null)
            {
                ImageOpenRequested?.Invoke(this, _imagePath);
            }
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedRectangle(new Rectangle(0, 0, Width - 1, Height - 1), 8);
        using var background = new SolidBrush(Color.FromArgb(29, 32, 39));
        e.Graphics.FillPath(background, path);

        if (_image is not null)
        {
            var state = e.Graphics.Save();
            e.Graphics.SetClip(path);
            e.Graphics.DrawImage(_image, ClientRectangle);
            e.Graphics.Restore(state);
        }

        using var border = new Pen(
            _hovered ? Color.FromArgb(220, 169, 78) : Color.FromArgb(67, 72, 83),
            _hovered ? 2F : 1F);
        e.Graphics.DrawPath(border, path);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _image?.Dispose();
        }
        base.Dispose(disposing);
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class ModernCheckBox : Control
{
    private bool _checked;
    private bool _hovered;

    public event EventHandler? CheckedChanged;

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
            {
                return;
            }
            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ModernCheckBox()
    {
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleRole = AccessibleRole.CheckButton;
        MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
        MouseLeave += (_, _) => { _hovered = false; Invalidate(); };
        Click += (_, _) => Checked = !Checked;
        KeyDown += (_, args) =>
        {
            if (args.KeyCode == Keys.Space)
            {
                Checked = !Checked;
                args.Handled = true;
            }
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var box = new Rectangle(2, 2, Width - 5, Height - 5);
        using var path = RoundedRectangle(box, 6);
        var fillColor = Checked
            ? Color.FromArgb(202, 158, 78)
            : _hovered
                ? Color.FromArgb(61, 66, 77)
                : Color.FromArgb(36, 39, 47);
        using var fill = new SolidBrush(fillColor);
        using var border = new Pen(
            Checked ? Color.FromArgb(223, 181, 100) : Color.FromArgb(105, 110, 122),
            1.5F);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);

        if (Checked)
        {
            using var check = new Pen(Color.FromArgb(28, 27, 23), 2.6F)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            e.Graphics.DrawLines(check,
            new Point[]
            {
                new Point((int)(Width * 0.25), (int)(Height * 0.52)),
                new Point((int)(Width * 0.43), (int)(Height * 0.70)),
                new Point((int)(Width * 0.76), (int)(Height * 0.30))
            });
        }
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class ModernScrollBar : Control
{
    private int _maximum;
    private int _value;
    private int _largeChange;
    private bool _dragging;
    private int _dragStartY;
    private int _dragStartValue;

    public event EventHandler? ValueChanged;

    public int Maximum => _maximum;

    public int Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, 0, _maximum);
            if (_value == clamped)
            {
                return;
            }
            _value = clamped;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ModernScrollBar()
    {
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        BackColor = Color.FromArgb(31, 34, 41);
    }

    public void SetRange(int maximum, int largeChange)
    {
        _maximum = Math.Max(0, maximum);
        _largeChange = Math.Max(1, largeChange);
        _value = Math.Clamp(_value, 0, _maximum);
        Visible = _maximum > 0;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var track = new SolidBrush(Color.FromArgb(37, 40, 48));
        e.Graphics.FillRectangle(track, ClientRectangle);

        if (!Visible || _maximum <= 0)
        {
            return;
        }

        var thumb = GetThumbRectangle();
        using var path = RoundedRectangle(thumb, Math.Max(2, thumb.Width / 2));
        using var brush = new SolidBrush(_dragging
            ? Color.FromArgb(202, 158, 78)
            : Color.FromArgb(105, 110, 122));
        e.Graphics.FillPath(brush, path);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        var thumb = GetThumbRectangle();
        if (thumb.Contains(e.Location))
        {
            _dragging = true;
            _dragStartY = e.Y;
            _dragStartValue = Value;
            Capture = true;
            Invalidate();
            return;
        }

        Value += e.Y < thumb.Top ? -_largeChange : _largeChange;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging)
        {
            return;
        }

        var thumb = GetThumbRectangle();
        var travel = Math.Max(1, Height - thumb.Height - 8);
        Value = _dragStartValue + (int)Math.Round((e.Y - _dragStartY) * (_maximum / (double)travel));
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
        Capture = false;
        Invalidate();
    }

    private Rectangle GetThumbRectangle()
    {
        var trackHeight = Math.Max(1, Height - 8);
        var contentHeight = _maximum + _largeChange;
        var minimumThumbHeight = Math.Min(36, trackHeight);
        var thumbHeight = Math.Clamp(
            (int)Math.Round(trackHeight * (_largeChange / (double)Math.Max(1, contentHeight))),
            minimumThumbHeight,
            trackHeight);
        var travel = Math.Max(0, trackHeight - thumbHeight);
        var top = 4 + (_maximum == 0 ? 0 : (int)Math.Round(travel * (_value / (double)_maximum)));
        return new Rectangle(3, top, Math.Max(4, Width - 6), thumbHeight);
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
