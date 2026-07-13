using System;
using System.Drawing;
using System.Windows.Forms;

namespace YoutubeDownloader;

internal sealed class UpdateProgressForm : Form
{
    private readonly Label _statusLabel;
    private readonly Label _progressLabel;
    private readonly ProgressBar _progressBar;
    private int _lastPercent = -1;
    private long _lastReportedBytes = -1;

    public UpdateProgressForm()
    {
        Text = "업데이트 진행 중";
        ClientSize = new Size(420, 142);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(248, 249, 251);
        Font = new Font("맑은 고딕", 9F);

        var titleLabel = new Label
        {
            AutoSize = true,
            Location = new Point(24, 20),
            Text = "업데이트 진행 중",
            Font = new Font("맑은 고딕", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 33, 40)
        };

        _statusLabel = new Label
        {
            AutoEllipsis = true,
            Location = new Point(26, 55),
            Size = new Size(368, 22),
            Text = "업데이트를 확인하고 있습니다...",
            ForeColor = Color.FromArgb(77, 85, 97)
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(26, 88),
            Size = new Size(320, 16),
            Minimum = 0,
            Maximum = 100,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 28
        };

        _progressLabel = new Label
        {
            Location = new Point(352, 86),
            Size = new Size(42, 22),
            Text = "",
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(77, 85, 97)
        };

        Controls.Add(titleLabel);
        Controls.Add(_statusLabel);
        Controls.Add(_progressBar);
        Controls.Add(_progressLabel);
    }

    public void ShowCentered(Form owner, string status)
    {
        Icon = owner.Icon;
        Rectangle bounds = owner.WindowState == FormWindowState.Maximized
            ? Screen.FromControl(owner).WorkingArea
            : owner.WindowState == FormWindowState.Minimized ? owner.RestoreBounds : owner.Bounds;

        Location = new Point(
            bounds.Left + Math.Max(0, (bounds.Width - Width) / 2),
            bounds.Top + Math.Max(0, (bounds.Height - Height) / 2));

        SetIndeterminate(status);
        Show(owner);
        BringToFront();
        Refresh();
    }

    public void SetIndeterminate(string status)
    {
        RunOnUiThread(() =>
        {
            _statusLabel.Text = status;
            _progressBar.Style = ProgressBarStyle.Marquee;
            _progressBar.MarqueeAnimationSpeed = 28;
            _progressLabel.Text = "";
            _lastPercent = -1;
            _lastReportedBytes = -1;
        });
    }

    public void SetDownloadProgress(long downloadedBytes, long? totalBytes)
    {
        RunOnUiThread(() =>
        {
            if (totalBytes is > 0)
            {
                int percent = (int)Math.Clamp(downloadedBytes * 100L / totalBytes.Value, 0, 100);
                if (_progressBar.Style != ProgressBarStyle.Continuous)
                {
                    _progressBar.MarqueeAnimationSpeed = 0;
                    _progressBar.Style = ProgressBarStyle.Continuous;
                }

                if (percent != _lastPercent)
                {
                    _progressBar.Value = percent;
                    _progressLabel.Text = $"{percent}%";
                    _statusLabel.Text = $"업데이트 파일 다운로드 중... {FormatMegabytes(downloadedBytes)} / {FormatMegabytes(totalBytes.Value)}";
                    _lastPercent = percent;
                }
            }
            else
            {
                if (_progressBar.Style != ProgressBarStyle.Marquee)
                {
                    _progressBar.Style = ProgressBarStyle.Marquee;
                    _progressBar.MarqueeAnimationSpeed = 28;
                }

                if (_lastReportedBytes < 0 || downloadedBytes - _lastReportedBytes >= 1024 * 1024)
                {
                    _statusLabel.Text = $"업데이트 파일 다운로드 중... {FormatMegabytes(downloadedBytes)}";
                    _progressLabel.Text = "";
                    _lastReportedBytes = downloadedBytes;
                }
            }
        });
    }

    private static string FormatMegabytes(long bytes)
    {
        return $"{bytes / 1024d / 1024d:0.0} MB";
    }

    private void RunOnUiThread(Action action)
    {
        if (IsDisposed || Disposing) return;
        if (InvokeRequired)
        {
            BeginInvoke(action);
            return;
        }

        action();
    }
}
