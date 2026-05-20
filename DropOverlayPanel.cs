using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace YoutubeDownloader;

public class DropOverlayPanel : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string DropText { get; set; } = "\uD30C\uC77C\uC744 \uC5EC\uAE30\uC5D0 \uB193\uC73C\uC138\uC694.";

    public DropOverlayPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(61, 74, 92);
        ForeColor = Color.White;
        AllowDrop = true;
        Visible = false;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.FromArgb(147, 197, 253), 1.5f)
        {
            DashStyle = DashStyle.Dash
        };

        var rect = new Rectangle(8, 8, Math.Max(1, Width - 17), Math.Max(1, Height - 17));
        e.Graphics.DrawRectangle(pen, rect);

        TextRenderer.DrawText(
            e.Graphics,
            DropText,
            new Font("Segoe UI", 11F, FontStyle.Bold),
            ClientRectangle,
            Color.FromArgb(226, 232, 240),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
