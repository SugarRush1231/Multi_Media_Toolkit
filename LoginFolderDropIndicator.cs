using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace YoutubeDownloader;

internal sealed class LoginFolderDropIndicator : Panel
{
    public LoginFolderDropIndicator()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle bounds = new Rectangle(7, 5, Width - 15, Height - 12);
        using GraphicsPath path = CreateRoundedPath(bounds, 12F);
        using var fill = new SolidBrush(Color.FromArgb(45, 14, 165, 233));
        using var border = new Pen(Color.FromArgb(150, 14, 165, 233), 1.5F)
        {
            DashStyle = DashStyle.Dash
        };
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
    }

    private static GraphicsPath CreateRoundedPath(Rectangle rectangle, float radius)
    {
        float diameter = radius * 2F;
        var path = new GraphicsPath();
        path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180F, 90F);
        path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270F, 90F);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0F, 90F);
        path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90F, 90F);
        path.CloseFigure();
        return path;
    }
}
