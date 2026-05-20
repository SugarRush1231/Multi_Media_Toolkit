using System;
using System.Drawing;
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
    private readonly PictureBox _downloadButton;
    private readonly Panel _activityDot;
    private bool _dragging;
    private Point _dragOffset;
    private bool _wasDragged;
    private bool _clickInProgress;

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
        Size = new Size(58, 58);
        BackColor = Color.White;
        TransparencyKey = Color.White;

        _downloadButton = new PictureBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand,
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = LoadWidgetIcon()
        };
        _downloadButton.MouseDown += Widget_MouseDown;
        _downloadButton.MouseMove += Widget_MouseMove;
        _downloadButton.MouseUp += Widget_MouseUp;
        _downloadButton.Click += async (s, e) =>
        {
            if (_wasDragged)
            {
                _wasDragged = false;
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
        };
        Controls.Add(_downloadButton);

        _activityDot = new Panel
        {
            Size = new Size(12, 12),
            Location = new Point(42, 42),
            BackColor = Color.FromArgb(30, 144, 255),
            Visible = false
        };
        Controls.Add(_activityDot);
        _activityDot.BringToFront();

        var menu = new ContextMenuStrip();
        menu.Items.Add("\uC571 \uC5F4\uAE30", null, (s, e) => _openMain());
        menu.Items.Add("\uC704\uC82F \uBAA8\uB4DC \uB044\uAE30", null, (s, e) => _turnOff());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("\uC885\uB8CC", null, (s, e) => _exitApp());
        ContextMenuStrip = menu;
        _downloadButton.ContextMenuStrip = menu;

        _toolTip = new ToolTip
        {
            AutoPopDelay = 5000,
            InitialDelay = 400,
            ReshowDelay = 100,
            ShowAlways = true
        };
        _toolTip.SetToolTip(_downloadButton, "\uD604\uC7AC \uD398\uC774\uC9C0 \uC601\uC0C1 \uB2E4\uC6B4\uB85C\uB4DC");
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

        _activityDot.Visible = busy;
        _downloadButton.Cursor = busy ? Cursors.WaitCursor : Cursors.Hand;
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
        _dragOffset = e.Location;
    }

    private void Widget_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        Point next = PointToScreen(e.Location);
        next.Offset(-_dragOffset.X, -_dragOffset.Y);
        if (Math.Abs(next.X - Left) > 2 || Math.Abs(next.Y - Top) > 2) _wasDragged = true;
        Location = ClampToVisibleArea(next);
    }

    private void Widget_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        _saveLocation(Location);
    }

    private Point ClampToVisibleArea(Point location)
    {
        Rectangle area = Screen.FromPoint(location).WorkingArea;
        int x = Math.Min(Math.Max(location.X, area.Left), area.Right - Width);
        int y = Math.Min(Math.Max(location.Y, area.Top), area.Bottom - Height);
        return new Point(x, y);
    }

    private static Image? LoadWidgetIcon()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "MMT_down.png");
        if (!File.Exists(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, "MMT_down.png");
        }

        return File.Exists(path) ? Image.FromFile(path) : null;
    }
}
