using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace YoutubeDownloader;

internal sealed class WigglePictureBox : PictureBox
{
    private float _rotationAngle;

    public WigglePictureBox()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float RotationAngle
    {
        get => _rotationAngle;
        set
        {
            if (Math.Abs(_rotationAngle - value) < 0.05F) return;
            _rotationAngle = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Image == null)
        {
            base.OnPaint(e);
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        RectangleF destination = GetZoomRectangle(Image.Size, ClientSize, 2F);
        GraphicsState state = e.Graphics.Save();
        e.Graphics.TranslateTransform(ClientSize.Width / 2F, ClientSize.Height / 2F);
        e.Graphics.RotateTransform(_rotationAngle);
        e.Graphics.TranslateTransform(-ClientSize.Width / 2F, -ClientSize.Height / 2F);
        e.Graphics.DrawImage(Image, destination);
        e.Graphics.Restore(state);
    }

    private static RectangleF GetZoomRectangle(Size imageSize, Size clientSize, float padding)
    {
        float availableWidth = Math.Max(1F, clientSize.Width - (padding * 2F));
        float availableHeight = Math.Max(1F, clientSize.Height - (padding * 2F));
        float scale = Math.Min(availableWidth / imageSize.Width, availableHeight / imageSize.Height);
        float width = imageSize.Width * scale;
        float height = imageSize.Height * scale;
        return new RectangleF((clientSize.Width - width) / 2F, (clientSize.Height - height) / 2F, width, height);
    }
}
