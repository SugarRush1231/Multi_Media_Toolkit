using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.ComponentModel;

namespace YoutubeDownloader;

public class RoundButton : Button
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int BorderRadius { get; set; } = 15;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Point TextOffset { get; set; } = Point.Empty;

    public RoundButton()
    {
        this.DoubleBuffered = true;
        this.FlatStyle = FlatStyle.Flat;
        this.FlatAppearance.BorderSize = 0;
        this.BackColor = Color.FromArgb(46, 204, 113);
        this.ForeColor = Color.White;
        this.Cursor = Cursors.Hand;
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        // Custom draw completely to avoid text shifting on click
        pevent.Graphics.SmoothingMode = SmoothingMode.HighQuality;
        pevent.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        pevent.Graphics.Clear(this.Parent?.BackColor ?? Color.White);

        // Deflate by 0.5 - 1.0 to avoid edge clipping on some DPI settings
        RectangleF rectSurface = new RectangleF(0.5f, 0.5f, this.Width - 1.5f, this.Height - 1.5f);
        
        using (GraphicsPath pathSurface = GetFigurePath(rectSurface, BorderRadius - 0.5f))
        {
            Color bgColor = this.Enabled ? this.BackColor : Color.FromArgb(200, 200, 200);
            using (SolidBrush brushSurface = new SolidBrush(bgColor))
            {
                pevent.Graphics.FillPath(brushSurface, pathSurface);
            }

            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak;
            Rectangle textBounds = this.ClientRectangle;
            textBounds.Offset(TextOffset);
            TextRenderer.DrawText(pevent.Graphics, this.Text, this.Font, textBounds, this.ForeColor, flags);
        }
    }

    private GraphicsPath GetFigurePath(RectangleF rect, float radius)
    {
        GraphicsPath path = new GraphicsPath();
        float curveSize = radius * 2F;
        if (curveSize <= 0) curveSize = 1; // Prevent crash
        path.StartFigure();
        path.AddArc(rect.X, rect.Y, curveSize, curveSize, 180, 90);
        path.AddArc(rect.Right - curveSize, rect.Y, curveSize, curveSize, 270, 90);
        path.AddArc(rect.Right - curveSize, rect.Bottom - curveSize, curveSize, curveSize, 0, 90);
        path.AddArc(rect.X, rect.Bottom - curveSize, curveSize, curveSize, 90, 90);
        path.CloseFigure();
        return path;
    }
}
