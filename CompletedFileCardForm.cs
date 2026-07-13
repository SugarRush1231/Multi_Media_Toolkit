using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace YoutubeDownloader;

internal sealed class CompletedFileCardForm : Form
{
    private const int AutoCloseDelayMs = 30000;
    private readonly string _filePath;
    private readonly System.Windows.Forms.Timer _autoCloseTimer;
    private long _remainingAutoCloseMs = AutoCloseDelayMs;
    private long _lastAutoCloseTick;
    private Point _mouseDownPoint;
    private bool _mouseDown;
    private bool _dragStarted;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WsExToolWindow = 0x00000080;
            const int WsExNoActivate = 0x08000000;
            CreateParams parameters = base.CreateParams;
            parameters.ExStyle |= WsExToolWindow | WsExNoActivate;
            return parameters;
        }
    }

    public CompletedFileCardForm(string filePath)
    {
        _filePath = filePath;

        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.White;
        ClientSize = new Size(380, 96);
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;

        var iconBox = new PictureBox
        {
            Location = new Point(16, 20),
            Size = new Size(48, 48),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        try
        {
            using Icon? icon = Icon.ExtractAssociatedIcon(filePath);
            iconBox.Image = icon?.ToBitmap();
        }
        catch
        {
        }

        var title = new Label
        {
            Text = Path.GetFileName(filePath),
            Location = new Point(78, 14),
            Size = new Size(252, 26),
            AutoEllipsis = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var status = new Label
        {
            Text = "다운로드 완료",
            Location = new Point(78, 40),
            Size = new Size(170, 20),
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(2, 132, 199),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var hint = new Label
        {
            Text = "원하는 프로그램이나 폴더로 끌어놓으세요.",
            Location = new Point(78, 62),
            Size = new Size(270, 20),
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(100, 116, 139),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var closeButton = new RoundButton
        {
            Text = "×",
            Location = new Point(338, 6),
            Size = new Size(34, 34),
            BorderRadius = 8,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(254, 242, 242),
            ForeColor = Color.FromArgb(220, 38, 38),
            Font = new Font("Segoe UI", 13F, FontStyle.Regular),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.MouseEnter += (s, e) =>
        {
            closeButton.BackColor = Color.FromArgb(254, 226, 226);
            closeButton.Invalidate();
        };
        closeButton.MouseLeave += (s, e) =>
        {
            closeButton.BackColor = Color.FromArgb(254, 242, 242);
            closeButton.Invalidate();
        };
        closeButton.MouseDown += (s, e) =>
        {
            closeButton.BackColor = Color.FromArgb(254, 202, 202);
            closeButton.Invalidate();
        };
        closeButton.Click += (s, e) => Close();

        Controls.Add(iconBox);
        Controls.Add(title);
        Controls.Add(status);
        Controls.Add(hint);
        Controls.Add(closeButton);

        RegisterDragSurface(this);
        RegisterDragSurface(iconBox);
        RegisterDragSurface(title);
        RegisterDragSurface(status);
        RegisterDragSurface(hint);

        _autoCloseTimer = new System.Windows.Forms.Timer { Interval = 200 };
        _autoCloseTimer.Tick += AutoCloseTimer_Tick;
        Shown += (s, e) =>
        {
            _remainingAutoCloseMs = AutoCloseDelayMs;
            _lastAutoCloseTick = Environment.TickCount64;
            _autoCloseTimer.Start();
        };

    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using var path = CreateRoundedPath(ClientRectangle, 12);
        Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = CreateRoundedPath(new Rectangle(1, 1, Width - 3, Height - 3), 11);
        using var pen = new Pen(Color.FromArgb(203, 213, 225), 1);
        e.Graphics.DrawPath(pen, path);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _autoCloseTimer.Stop();
            _autoCloseTimer.Dispose();
            foreach (Control control in Controls)
            {
                if (control is PictureBox pictureBox)
                {
                    pictureBox.Image?.Dispose();
                }
            }
        }

        base.Dispose(disposing);
    }

    private void RegisterDragSurface(Control control)
    {
        control.Cursor = Cursors.Hand;
        control.MouseDown += DragSurface_MouseDown;
        control.MouseMove += DragSurface_MouseMove;
        control.MouseUp += DragSurface_MouseUp;
    }

    private void DragSurface_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || sender is not Control control) return;

        _mouseDown = true;
        _dragStarted = false;
        _mouseDownPoint = PointToClient(control.PointToScreen(e.Location));
    }

    private void DragSurface_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_mouseDown || _dragStarted || e.Button != MouseButtons.Left || sender is not Control control) return;

        Point currentPoint = PointToClient(control.PointToScreen(e.Location));
        Size dragSize = SystemInformation.DragSize;
        var dragBounds = new Rectangle(
            _mouseDownPoint.X - dragSize.Width / 2,
            _mouseDownPoint.Y - dragSize.Height / 2,
            dragSize.Width,
            dragSize.Height);

        if (dragBounds.Contains(currentPoint)) return;

        _dragStarted = true;
        var data = new DataObject(DataFormats.FileDrop, new[] { _filePath });
        DragDropEffects result = DoDragDrop(data, DragDropEffects.Copy);
        _mouseDown = false;
        if (result != DragDropEffects.None)
        {
            Close();
        }
    }

    private void DragSurface_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;

        bool openFile = _mouseDown && !_dragStarted;
        _mouseDown = false;
        if (!openFile) return;

        try
        {
            Process.Start(new ProcessStartInfo(_filePath) { UseShellExecute = true });
        }
        catch
        {
        }
    }

    private void AutoCloseTimer_Tick(object? sender, EventArgs e)
    {
        long now = Environment.TickCount64;
        long elapsed = Math.Max(0, now - _lastAutoCloseTick);
        _lastAutoCloseTick = now;

        bool mouseInside = ClientRectangle.Contains(PointToClient(Cursor.Position));
        if (mouseInside) return;

        _remainingAutoCloseMs -= elapsed;
        if (_remainingAutoCloseMs <= 0)
        {
            Close();
        }
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
