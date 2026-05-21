using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YoutubeDownloader;

public sealed class DownloadWidgetForm : Form
{
    private readonly Func<Task> _downloadAsync;
    private readonly Action _openMain;
    private readonly Action _turnOff;
    private readonly Action _exitApp;
    private readonly Action<Point> _saveLocation;
    private readonly ToolTip _toolTip;
    private readonly ProgressRingControl _downloadButton;
    private readonly System.Windows.Forms.Timer _progressResetTimer;
    private readonly ContextMenuStrip _contextMenu;
    private readonly WidgetMenuMessageFilter _menuMessageFilter;
    private readonly System.Windows.Forms.Timer _contextMenuCloseTimer;
    private WidgetToastForm? _toastForm;
    private bool _dragging;
    private Point _dragOffset;
    private Point _dragStartScreen;
    private bool _wasDragged;
    private bool _clickInProgress;
    private const int DragThresholdPixels = 6;

    public DownloadWidgetForm(
        Func<Task> downloadAsync,
        Action openMain,
        Action turnOff,
        Action exitApp,
        Action<Point> saveLocation)
    {
        _downloadAsync = downloadAsync;
        _openMain = openMain;
        _turnOff = turnOff;
        _exitApp = exitApp;
        _saveLocation = saveLocation;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(64, 64);
        BackColor = Color.FromArgb(1, 2, 3);
        TransparencyKey = Color.FromArgb(1, 2, 3);

        Image? widgetIcon = LoadWidgetIcon();
        _downloadButton = new ProgressRingControl
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand,
            Icon = widgetIcon
        };
        _downloadButton.MouseDown += Widget_MouseDown;
        _downloadButton.MouseMove += Widget_MouseMove;
        _downloadButton.MouseUp += Widget_MouseUp;
        Controls.Add(_downloadButton);

        _progressResetTimer = new System.Windows.Forms.Timer { Interval = 900 };
        _progressResetTimer.Tick += (s, e) =>
        {
            _progressResetTimer.Stop();
            _downloadButton.Progress = null;
        };

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add("\uC571 \uC5F4\uAE30", null, (s, e) => _openMain());
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("\uC885\uB8CC", null, (s, e) => _exitApp());
        _menuMessageFilter = new WidgetMenuMessageFilter(_contextMenu, this);
        _contextMenuCloseTimer = new System.Windows.Forms.Timer { Interval = 30 };
        _contextMenuCloseTimer.Tick += (s, e) => CloseContextMenuOnOutsideClick();
        _contextMenu.Opened += (s, e) =>
        {
            Application.AddMessageFilter(_menuMessageFilter);
            _contextMenuCloseTimer.Start();
        };
        _contextMenu.Closed += (s, e) =>
        {
            _contextMenuCloseTimer.Stop();
            Application.RemoveMessageFilter(_menuMessageFilter);
        };
        ContextMenuStrip = _contextMenu;
        _downloadButton.ContextMenuStrip = _contextMenu;

        _toolTip = new ToolTip
        {
            AutoPopDelay = 5000,
            InitialDelay = 400,
            ReshowDelay = 100,
            ShowAlways = true
        };
        _toolTip.SetToolTip(_downloadButton, "\uD604\uC7AC \uD398\uC774\uC9C0 \uC601\uC0C1 \uB2E4\uC6B4\uB85C\uB4DC");
        SetWidgetRegion();
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_NOACTIVATE = 0x08000000;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    public void SetStatus(string status)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetStatus(status)));
            return;
        }

        _toolTip.SetToolTip(_downloadButton, string.IsNullOrWhiteSpace(status)
            ? "\uD604\uC7AC \uD398\uC774\uC9C0 \uC601\uC0C1 \uB2E4\uC6B4\uB85C\uB4DC"
            : status);
    }

    public void SetBusy(bool busy)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetBusy(busy)));
            return;
        }

        _downloadButton.Cursor = busy ? Cursors.WaitCursor : Cursors.Hand;
    }

    public void SetProgress(int? percent)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetProgress(percent)));
            return;
        }

        if (!percent.HasValue)
        {
            _progressResetTimer.Stop();
            _downloadButton.Progress = null;
            return;
        }

        int value = Math.Max(0, Math.Min(100, percent.Value));
        _downloadButton.Progress = value;
        if (value >= 100)
        {
            _progressResetTimer.Stop();
            _progressResetTimer.Start();
        }
        else
        {
            _progressResetTimer.Stop();
        }
    }

    public void ShowToast(string message, int durationMs = 1600)
    {
        if (IsDisposed || !Visible || string.IsNullOrWhiteSpace(message)) return;
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => ShowToast(message, durationMs)));
            return;
        }

        _toastForm ??= new WidgetToastForm();
        _toastForm.ShowToast(message, durationMs);
        PositionToast();
        _toastForm.ResetStartLocation();
    }

    public static Point GetDefaultLocation(Size widgetSize)
    {
        Rectangle area = Screen.PrimaryScreen?.WorkingArea ?? Screen.GetWorkingArea(Point.Empty);
        return new Point(area.Right - widgetSize.Width - 14, area.Bottom - widgetSize.Height - 18);
    }

    public static bool IsLocationVisible(Point location, Size widgetSize)
    {
        Rectangle widgetBounds = new Rectangle(location, widgetSize);
        return Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(widgetBounds));
    }

    private void Widget_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _dragging = true;
        _wasDragged = false;
        _dragStartScreen = sender is Control startControl
            ? startControl.PointToScreen(e.Location)
            : PointToScreen(e.Location);
        _dragOffset = sender is Control control
            ? PointToClient(control.PointToScreen(e.Location))
            : e.Location;
    }

    private void Widget_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        Point next = sender is Control control
            ? control.PointToScreen(e.Location)
            : PointToScreen(e.Location);
        if (Math.Abs(next.X - _dragStartScreen.X) >= DragThresholdPixels ||
            Math.Abs(next.Y - _dragStartScreen.Y) >= DragThresholdPixels)
        {
            _wasDragged = true;
        }

        if (!_wasDragged) return;
        next.Offset(-_dragOffset.X, -_dragOffset.Y);
        Location = ClampToVisibleArea(next);
    }

    private async void Widget_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || !_dragging) return;
        _dragging = false;
        if (_wasDragged)
        {
            _wasDragged = false;
            _saveLocation(Location);
            PositionToast();
            return;
        }

        if (_clickInProgress) return;
        _clickInProgress = true;
        try
        {
            await _downloadAsync();
        }
        finally
        {
            _clickInProgress = false;
        }
    }

    private Point ClampToVisibleArea(Point location)
    {
        Rectangle area = Screen.FromPoint(location).WorkingArea;
        int x = Math.Min(Math.Max(location.X, area.Left), area.Right - Width);
        int y = Math.Min(Math.Max(location.Y, area.Top), area.Bottom - Height);
        return new Point(x, y);
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        PositionToast();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        SetWidgetRegion();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible) _toastForm?.Hide();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Application.RemoveMessageFilter(_menuMessageFilter);
            _contextMenuCloseTimer.Dispose();
            _contextMenu.Dispose();
            _toastForm?.Dispose();
            _toastForm = null;
        }

        base.Dispose(disposing);
    }

    private void PositionToast()
    {
        if (_toastForm == null || _toastForm.IsDisposed) return;

        Rectangle area = Screen.FromPoint(Location).WorkingArea;
        int x = Left + (Width - _toastForm.Width) / 2;
        int y = Top - _toastForm.Height - 8;

        x = Math.Min(Math.Max(x, area.Left + 6), area.Right - _toastForm.Width - 6);
        y = Math.Max(y, area.Top + 6);
        _toastForm.Location = new Point(x, y);
    }

    private void SetWidgetRegion()
    {
        using var path = new GraphicsPath();
        path.AddEllipse(ClientRectangle);
        Region?.Dispose();
        Region = new Region(path);
    }

    private static Image? LoadWidgetIcon()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "MMT_down.png");
        if (!File.Exists(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, "MMT_down.png");
        }

        if (!File.Exists(path)) return null;

        using var source = new Bitmap(path);
        Rectangle bounds = GetAlphaBounds(source);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return new Bitmap(source);
        }

        bounds.Inflate(18, 18);
        bounds.Intersect(new Rectangle(Point.Empty, source.Size));
        return source.Clone(bounds, PixelFormat.Format32bppArgb);
    }

    private static Rectangle GetAlphaBounds(Bitmap bitmap)
    {
        int minX = bitmap.Width;
        int minY = bitmap.Height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).A <= 10) continue;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        return maxX < minX || maxY < minY
            ? Rectangle.Empty
            : Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
    }

    private sealed class ProgressRingControl : Control
    {
        private float _displayProgress;
        private int _targetProgress;
        private Image? _icon;
        private readonly System.Windows.Forms.Timer _animationTimer;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image? Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                Invalidate();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int? Progress
        {
            get => _targetProgress;
            set
            {
                if (!value.HasValue)
                {
                    _targetProgress = 0;
                    _displayProgress = 0;
                    _animationTimer.Stop();
                    Invalidate();
                    return;
                }

                int next = Math.Max(0, Math.Min(100, value.Value));
                if (_targetProgress == next) return;
                _targetProgress = next;
                if (next < _displayProgress)
                {
                    _displayProgress = next;
                    Invalidate();
                }
                if (!_animationTimer.Enabled) _animationTimer.Start();
            }
        }

        public ProgressRingControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            _animationTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _animationTimer.Tick += (s, e) =>
            {
                float diff = _targetProgress - _displayProgress;
                if (Math.Abs(diff) <= 0.6f)
                {
                    _displayProgress = _targetProgress;
                    _animationTimer.Stop();
                }
                else
                {
                    _displayProgress += diff * 0.28f;
                }

                Invalidate();
            };
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(Color.FromArgb(1, 2, 3));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (_icon != null)
            {
                Rectangle imageArea = ClientRectangle;
                imageArea.Inflate(-7, -7);
                Rectangle imageBounds = GetZoomBounds(_icon.Size, imageArea);
                e.Graphics.DrawImage(_icon, imageBounds);
            }

            Rectangle bounds = ClientRectangle;
            bounds.Inflate(-5, -5);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            using var progressPen = new Pen(Color.FromArgb(0, 214, 118), 3.2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            if (_displayProgress > 0)
            {
                e.Graphics.DrawArc(progressPen, bounds, -90, 360f * _displayProgress / 100f);
            }
        }

        private static Rectangle GetZoomBounds(Size imageSize, Rectangle bounds)
        {
            if (imageSize.Width <= 0 || imageSize.Height <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
            {
                return bounds;
            }

            float ratio = Math.Min((float)bounds.Width / imageSize.Width, (float)bounds.Height / imageSize.Height);
            int width = Math.Max(1, (int)(imageSize.Width * ratio));
            int height = Math.Max(1, (int)(imageSize.Height * ratio));
            int x = bounds.Left + (bounds.Width - width) / 2;
            int y = bounds.Top + (bounds.Height - height) / 2;
            return new Rectangle(x, y, width, height);
        }
    }

    private void CloseContextMenuOnOutsideClick()
    {
        if (!_contextMenu.Visible) return;
        if (Control.MouseButtons == MouseButtons.None) return;

        Point cursor = Cursor.Position;
        if (_contextMenu.Bounds.Contains(cursor) || Bounds.Contains(cursor)) return;

        _contextMenu.Close(ToolStripDropDownCloseReason.AppClicked);
    }

    private sealed class WidgetMenuMessageFilter : IMessageFilter
    {
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;
        private readonly ContextMenuStrip _menu;
        private readonly Form _owner;

        public WidgetMenuMessageFilter(ContextMenuStrip menu, Form owner)
        {
            _menu = menu;
            _owner = owner;
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (!_menu.Visible) return false;
            if (m.Msg != WM_LBUTTONDOWN && m.Msg != WM_RBUTTONDOWN && m.Msg != WM_MBUTTONDOWN) return false;

            Point cursor = Cursor.Position;
            if (_menu.Bounds.Contains(cursor) || _owner.Bounds.Contains(cursor)) return false;

            _menu.Close(ToolStripDropDownCloseReason.AppClicked);
            return false;
        }
    }

    private sealed class WidgetToastForm : Form
    {
        private readonly ToastTextControl _label;
        private readonly System.Windows.Forms.Timer _timer;
        private Point _startLocation;
        private DateTime _startedAt;
        private int _durationMs = 1600;

        public WidgetToastForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Lime;
            TransparencyKey = Color.Lime;
            Padding = Padding.Empty;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            _label = new ToastTextControl
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                MaximumSize = new Size(240, 0)
            };
            Controls.Add(_label);

            _timer = new System.Windows.Forms.Timer { Interval = 16 };
            _timer.Tick += (s, e) =>
            {
                double elapsed = (DateTime.UtcNow - _startedAt).TotalMilliseconds;
                double progress = Math.Min(1d, Math.Max(0d, elapsed / Math.Max(1, _durationMs)));
                Location = new Point(_startLocation.X, _startLocation.Y - (int)(26 * progress));
                Opacity = Math.Max(0d, 1d - progress);
                if (progress < 1d) return;
                _timer.Stop();
                Opacity = 1d;
                Hide();
            };
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TOOLWINDOW = 0x00000080;
                const int WS_EX_NOACTIVATE = 0x08000000;
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        public void ShowToast(string message, int durationMs)
        {
            _label.Text = message;
            Size = PreferredSize;
            _durationMs = Math.Max(700, durationMs);
            _startLocation = Location;
            _startedAt = DateTime.UtcNow;
            Opacity = 1d;
            _timer.Stop();
            Show();
            BringToFront();
            _timer.Start();
        }

        public void ResetStartLocation()
        {
            _startLocation = Location;
        }

        private sealed class ToastTextControl : Control
        {
            public override string Text
            {
                get => base.Text;
                set
                {
                    base.Text = value;
                    Size = GetPreferredSize(Size.Empty);
                    Invalidate();
                }
            }

            public ToastTextControl()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw |
                         ControlStyles.UserPaint |
                         ControlStyles.SupportsTransparentBackColor, true);
            }

            public override Size GetPreferredSize(Size proposedSize)
            {
                using var g = CreateGraphics();
                SizeF size = g.MeasureString(Text, Font, MaximumSize.Width > 0 ? MaximumSize.Width : 240);
                return new Size((int)Math.Ceiling(size.Width) + 8, (int)Math.Ceiling(size.Height) + 6);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                using var shadow = new SolidBrush(Color.FromArgb(170, 15, 23, 42));
                using var fill = new SolidBrush(ForeColor);
                e.Graphics.DrawString(Text, Font, shadow, new RectangleF(5, 4, Width - 8, Height - 6));
                e.Graphics.DrawString(Text, Font, fill, new RectangleF(4, 3, Width - 8, Height - 6));
            }
        }
    }
}
