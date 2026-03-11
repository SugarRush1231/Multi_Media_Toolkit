using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace YoutubeDownloader
{
    public class ToggleSwitch : CheckBox
    {
        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            Padding = new Padding(6);
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            this.OnPaintBackground(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            var checkRect = new Rectangle(1, 1, Height - 3, Height - 3);
            
            if (Checked)
            {
                e.Graphics.FillPath(new SolidBrush(Color.FromArgb(155, 89, 182)), GetRoundedRect(rect)); // Purple
                checkRect.X = Width - Height + 1;
            }
            else
            {
                e.Graphics.FillPath(new SolidBrush(Color.LightGray), GetRoundedRect(rect));
            }
            
            e.Graphics.FillEllipse(Brushes.White, checkRect);
            
            // Text on the left if checked, right if unchecked? or just focus on the toggle.
        }

        private GraphicsPath GetRoundedRect(Rectangle rect)
        {
            float radius = Height / 2f;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.X + rect.Width - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.X + rect.Width - radius * 2, rect.Y + rect.Height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
