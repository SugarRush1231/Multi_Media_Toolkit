using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using System.Globalization;
using System.Linq;
using YoutubeDownloader;

namespace YoutubeDownloader
{
    public partial class MiniEditor : UserControl
    {
        private LibVLC _libvlc;
        private MediaPlayer _mediaPlayer;
        private VideoView _videoView;

        private long _durationMs = 0;
        private long _currentMs = 0;
        private long _inMs = -1;
        private long _outMs = -1;
        private double _fps = 30.0;
        private int _videoWidth = 16, _videoHeight = 9; // Default 16:9
        private bool _shouldPauseOnLoad = false;
        private bool _isPlaying = false; // Local flag to avoid blocking getter
        private bool _isProcessing = false; // Prevent concurrent commands
        private long _lastSeekTime = 0; // Throttle arrow keys
        private Stopwatch _interpolationTimer = new Stopwatch();
        private long _lastSyncMs = 0;

        private RoundButton btnBrowse;
        private TextBox txtFile;
        private Label lblCurrentTime, lblIn, lblOut, lblDuration, lblVideoInfo;
        private RoundButton btnSetIn, btnSetOut, btnClearAll;
        private RoundButton btnPrevFrame, btnNextFrame, btnPrevSec, btnNextSec;
        private RoundButton btnPlayPause, btnStop;
        
        private ComboBox cmbSaveOption;
        private RoundButton btnSave;
        private ProgressBar pbProgress;
        private Label lblSavePath;
        private RoundButton btnOpenFolder;
        private Label lblStatus;
        private Label lblTimecode;
        private RoundButton btnCancel;
        private System.Threading.CancellationTokenSource _saveCts;

        private TrackBar tbTimeline;
        private Panel pnlInMarker, pnlOutMarker, pnlRangeHighlight;
        private TrackBar tbVolume;
        private Label lblAudioGain, lblVolIcon;
        private bool _isMuted = false;
        private int _lastVolumeBeforeMute = 80;
        private RoundButton btnLoop;
        private bool _isLooping = false;
        private TextBox txtSeek;
        private RoundButton btnSeek;
        private bool _isDraggingTrackBar = false;

        private System.Windows.Forms.Timer _timer;

        private DialogResult ShowCenteredMessage(string text)
        {
            return ShowCenteredMessage(text, FindForm()?.Text ?? "Multi Media Toolkit", MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        private DialogResult ShowCenteredMessage(string text, string caption)
        {
            return ShowCenteredMessage(text, caption, MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        private DialogResult ShowCenteredMessage(string text, string caption, MessageBoxButtons buttons)
        {
            return ShowCenteredMessage(text, caption, buttons, MessageBoxIcon.None);
        }

        private DialogResult ShowCenteredMessage(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            Form? owner = FindForm();
            if (owner == null)
            {
                return MessageBox.Show(text, caption, buttons, icon);
            }

            if (owner.InvokeRequired)
            {
                return (DialogResult)owner.Invoke(new Func<DialogResult>(() => ShowCenteredMessage(text, caption, buttons, icon)));
            }

            Rectangle ownerBounds = owner.WindowState == FormWindowState.Minimized ? owner.RestoreBounds : owner.Bounds;
            if (owner.WindowState == FormWindowState.Maximized)
            {
                ownerBounds = Screen.FromControl(owner).WorkingArea;
            }
            if (ownerBounds.Width <= 0 || ownerBounds.Height <= 0)
            {
                ownerBounds = Screen.FromControl(owner).WorkingArea;
            }

            Point center = new Point(
                ownerBounds.Left + ownerBounds.Width / 2,
                ownerBounds.Top + ownerBounds.Height / 2);

            using var anchor = new Form
            {
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                Size = new Size(1, 1),
                Location = center,
                Opacity = 0
            };

            try
            {
                anchor.Show(owner);
                return MessageBox.Show(anchor, text, caption, buttons, icon);
            }
            finally
            {
                anchor.Close();
            }
        }

        public MiniEditor()
        {
            this.DoubleBuffered = true;
            InitializeComponent();
            SetupFileDropTarget();
            NormalizeMiniEditorText();
            
            this.Load += (s, e) => {
                if (!DesignMode)
                {
                    Task.Run(() => {
                        InitializeVlc();
                    });
                }
            };

            _timer = new System.Windows.Forms.Timer { Interval = 16 }; // High precision 60fps update
            _timer.Tick += Timer_Tick;
        }

        private void NormalizeMiniEditorText()
        {
            var title = this.Controls.OfType<Label>().FirstOrDefault(l => l.Location.X == 20 && l.Location.Y == 10);
            if (title != null) title.Text = "\uBBF8\uB2C8 \uD3B8\uC9D1\uAE30";

            txtFile.PlaceholderText = "\uC601\uC0C1 \uD30C\uC77C \uC5F4\uAE30...";
            btnBrowse.Text = "\uC5F4\uAE30";
            txtSeek.PlaceholderText = "\uBD84:\uCD08";
            btnSeek.Text = "\uC774\uB3D9";
            btnPrevSec.Text = "-5s";
            btnPrevFrame.Text = "<";
            btnPlayPause.Text = "\uC7AC\uC0DD/\uC815\uC9C0";
            btnStop.Text = "\uC815\uC9C0";
            btnNextFrame.Text = ">";
            btnNextSec.Text = "+5s";
            lblVolIcon.Text = "Vol";
            cmbSaveOption.Items.Clear();
            cmbSaveOption.Items.AddRange(new string[] { "\uC6D0\uBCF8 \uC800\uC7A5", "Premiere \uD638\uD658 (H.264)", "\uC624\uB514\uC624\uB9CC \uCD94\uCD9C (MP3)" });
            cmbSaveOption.SelectedIndex = 0;
            btnSave.Text = "\uC800\uC7A5";
            lblStatus.Text = "\uC900\uBE44";
            btnCancel.Text = "\uCDE8\uC18C";
            lblSavePath.Text = "\uD604\uC7AC \uC800\uC7A5 \uC704\uCE58: " + (SettingsManager.Settings?.DefaultDownloadFolder ?? "");
            btnOpenFolder.Text = "\uD3F4\uB354 \uC5F4\uAE30";

            btnSave.Click -= BtnSave_Click;
            btnSave.Click += BtnSaveClean_Click;
            btnOpenFolder.Click -= BtnOpenFolder_Click;
            btnOpenFolder.Click += BtnOpenFolderClean_Click;
        }

        private void InitializeVlc()
        {
            if (!DesignMode)
            {
                Core.Initialize();
                _libvlc = new LibVLC();
                _mediaPlayer = new MediaPlayer(_libvlc);
                
                InvokeIfRequired(() => {
                    _videoView.MediaPlayer = _mediaPlayer;
                    _mediaPlayer.Volume = tbVolume.Value;
                    // Ensure default aspect ratio is used, but allow stretching if needed
                    _mediaPlayer.AspectRatio = null; 
                });

                _mediaPlayer.LengthChanged += (s, e) => {
                    _durationMs = e.Length;
                    this.BeginInvoke(new Action(() => {
                        if (_durationMs > 0 && _durationMs <= int.MaxValue)
                            tbTimeline.Maximum = (int)_durationMs;
                        
                        // Try to get actual video metadata (FPS, Size)
                        try { 
                            if (_mediaPlayer != null && _mediaPlayer.Fps > 1) 
                                _fps = _mediaPlayer.Fps;

                            var tracks = _mediaPlayer.Media?.Tracks;
                            if (tracks != null)
                            {
                                foreach (var track in tracks)
                                {
                                    if (track.TrackType == TrackType.Video)
                                    {
                                        var v = track.Data.Video;
                                        if (v.Width > 0 && v.Height > 0)
                                        {
                                            _videoWidth = (int)v.Width;
                                            _videoHeight = (int)v.Height;
                                        }
                                        if (v.FrameRateNum > 0)
                                        {
                                            _fps = (double)v.FrameRateNum / Math.Max(1, v.FrameRateDen);
                                        }
                                        break;
                                    }
                                }
                            }
                            CenterControls();
                        } catch {}
                            
                        UpdateTimeLabels();
                    }));
                };

                // Smoother time tracking
                _mediaPlayer.TimeChanged += (s, e) => {
                    _lastSyncMs = e.Time;
                    _currentMs = e.Time;
                    _interpolationTimer.Restart();
                };

                // Handle pause on load
                _mediaPlayer.Playing += (s, e) => {
                    _isPlaying = true;
                    if (_shouldPauseOnLoad)
                    {
                        _shouldPauseOnLoad = false;
                        Task.Run(() => {
                            Task.Delay(50).ContinueWith(_ => _mediaPlayer.Pause());
                        });
                        InvokeIfRequired(() => {
                            _interpolationTimer.Stop();
                            _timer.Stop();
                        });
                    }
                    else
                    {
                        InvokeIfRequired(() => {
                            _interpolationTimer.Start();
                            _timer.Start();
                        });
                    }
                };

                _mediaPlayer.Paused += (s, e) => {
                    _isPlaying = false;
                    this.BeginInvoke(new Action(() => {
                        _interpolationTimer.Stop();
                        _timer.Stop();
                    }));
                };

                _mediaPlayer.EndReached += (s, e) => {
                    _isPlaying = false;
                    // Reset UI from background thread without blocking
                    this.BeginInvoke(new Action(() => {
                        _interpolationTimer.Stop();
                        _timer.Stop();
                        _currentMs = 0;
                        _lastSyncMs = 0;
                        if (tbTimeline.Maximum > 0) tbTimeline.Value = 0;
                        UpdateTimeLabels();
                        lblStatus.Text = "영상 재생 완료";
                    }));
                };

                _mediaPlayer.EncounteredError += (s, e) => {
                    _isPlaying = false;
                    this.BeginInvoke(new Action(() => {
                        _timer.Stop();
                        lblStatus.Text = "재생 오류 발생";
                        ShowCenteredMessage("영상을 재생하는 중 오류가 발생했습니다.");
                    }));
                };
            }
        }

        private void InvokeIfRequired(Action action)
        {
            if (this.InvokeRequired) this.Invoke(new MethodInvoker(action));
            else action();
        }

        private void SetupFileDropTarget()
        {
            txtFile.PlaceholderText = "\uC601\uC0C1 \uD30C\uC77C \uACBD\uB85C\uB97C \uC120\uD0DD \uD558\uC138\uC694.";

            var overlay = new DropOverlayPanel
            {
                Parent = this,
                Dock = DockStyle.Fill,
                DropText = "\uD3B8\uC9D1\uD560 \uC601\uC0C1\uC744 \uC5EC\uAE30\uC5D0 \uB193\uC73C\uC138\uC694."
            };

            this.Controls.Add(overlay);
            overlay.BringToFront();

            void ShowOverlay(DragEventArgs e)
            {
                if (!TryGetDroppedFile(e, out _))
                {
                    e.Effect = DragDropEffects.None;
                    return;
                }

                e.Effect = DragDropEffects.Copy;
                overlay.Visible = true;
                overlay.BringToFront();
            }

            void ApplyDrop(DragEventArgs e)
            {
                overlay.Visible = false;
                if (!TryGetDroppedFile(e, out var filePath)) return;

                txtFile.Text = filePath;
                LoadMedia(filePath);
            }

            void Wire(Control control)
            {
                control.AllowDrop = true;
                control.DragEnter += (s, e) => ShowOverlay(e);
                control.DragOver += (s, e) => ShowOverlay(e);
                control.DragDrop += (s, e) => ApplyDrop(e);

                foreach (Control child in control.Controls)
                {
                    if (child == overlay) continue;
                    Wire(child);
                }
            }

            Wire(this);
            overlay.DragEnter += (s, e) => ShowOverlay(e);
            overlay.DragOver += (s, e) => ShowOverlay(e);
            overlay.DragDrop += (s, e) => ApplyDrop(e);
            overlay.DragLeave += (s, e) => overlay.Visible = false;
        }

        private static bool TryGetDroppedFile(DragEventArgs e, out string filePath)
        {
            filePath = "";
            if (!(e.Data?.GetDataPresent(DataFormats.FileDrop) ?? false)) return false;
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0) return false;
            if (!File.Exists(files[0])) return false;

            filePath = files[0];
            return true;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer != null && _isPlaying)
            {
                // High-precision interpolation
                long interpolatedMs = _lastSyncMs + _interpolationTimer.ElapsedMilliseconds;
                _currentMs = interpolatedMs;

                InvokeIfRequired(() => {
                    string currentTime = FormatTime(_currentMs);
                    if (lblTimecode.Text != currentTime) lblTimecode.Text = currentTime;
                    lblCurrentTime.Text = $"Playhead: {currentTime}";
                    
                    if (!_isDraggingTrackBar && _currentMs >= tbTimeline.Minimum && _currentMs <= tbTimeline.Maximum)
                    {
                        tbTimeline.Value = (int)_currentMs;
                    }
                });

                // Loop Logic (Timer handles this for fast checking)
                if (_isLooping && _inMs != -1 && _outMs != -1)
                {
                    if (_currentMs >= _outMs || _currentMs < _inMs - 100)
                    {
                        long target = _inMs;
                        Task.Run(() => _mediaPlayer.Time = target);
                        _lastSyncMs = target;
                        _currentMs = target;
                        _interpolationTimer.Restart();
                    }
                }
            }
        }

        private void UpdateTimeLabels()
        {
            string currentTime = FormatTime(_currentMs);
            lblTimecode.Text = currentTime;
            lblCurrentTime.Text = $"Playhead: {currentTime}";
            lblDuration.Text = $"Duration: {FormatTime(_durationMs)}";
            lblVideoInfo.Text = $"Video: {_videoWidth}x{_videoHeight} | {_fps:F2} FPS";
            lblIn.Text = $"[ IN ]:  {FormatTime(_inMs)}";
            lblOut.Text = $"[ OUT ]: {FormatTime(_outMs)}";
            UpdateMarkerPositions();
        }

        private void UpdateMarkerPositions()
        {
            if (_durationMs <= 0 || tbTimeline.Width <= 0) 
            {
                pnlInMarker.Visible = false;
                pnlOutMarker.Visible = false;
                return;
            }

            // Standard TrackBar margin is approx 12px on both sides
            int barRange = tbTimeline.Width - 24;
            int startX = tbTimeline.Left + 12;
            int yPos = tbTimeline.Top + 14; 

            if (_inMs != -1)
            {
                double ratio = (double)_inMs / _durationMs;
                int x = startX + (int)(ratio * barRange) - 2;
                pnlInMarker.Location = new Point(x, yPos);
                pnlInMarker.Visible = true;
                pnlInMarker.BringToFront();
            }
            else pnlInMarker.Visible = false;

            if (_outMs != -1)
            {
                double ratio = (double)_outMs / _durationMs;
                int x = startX + (int)(ratio * barRange) - 2;
                pnlOutMarker.Location = new Point(x, yPos);
                pnlOutMarker.Visible = true;
                pnlOutMarker.BringToFront();
            }
            else pnlOutMarker.Visible = false;

            if (_inMs != -1 && _outMs != -1)
            {
                double ratioIn = (double)_inMs / _durationMs;
                double ratioOut = (double)_outMs / _durationMs;
                int xIn = startX + (int)(ratioIn * barRange);
                int xOut = startX + (int)(ratioOut * barRange);
                
                pnlRangeHighlight.Location = new Point(xIn, yPos + 4);
                pnlRangeHighlight.Width = xOut - xIn;
                pnlRangeHighlight.Visible = true;
                pnlRangeHighlight.BringToFront();
                pnlInMarker.BringToFront();
                pnlOutMarker.BringToFront();
            }
            else pnlRangeHighlight.Visible = false;
        }

        private string FormatTime(long ms)
        {
            if (ms < 0) return "--:--:--:--";
            
            double fps = _fps;
            if (fps <= 0) fps = 30.0;

            TimeSpan ts = TimeSpan.FromMilliseconds(ms);
            
            // Calculate current frame within the second
            // Using a tiny epsilon (0.1ms) to avoid floating point precision issues during floor
            int frames = (int)Math.Floor(((ms % 1000) * fps / 1000.0) + 0.001);
            
            // Ensure frames don't exceed FPS due to floating point quirks
            if (frames >= (int)Math.Ceiling(fps)) frames = (int)Math.Ceiling(fps) - 1;

            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}:{frames:D2}";
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_mediaPlayer != null && _mediaPlayer.Media != null)
            {
                // msg.LParam: bit 30 is 1 if the key was already down before the message (repeat)
                bool isRepeat = (msg.LParam.ToInt64() & 0x40000000) != 0;

                // Only handle on KeyDown (prevent multiples from other messages)
                if (msg.Msg != 0x100 && msg.Msg != 0x104) 
                    return base.ProcessCmdKey(ref msg, keyData);

                switch (keyData)
                {
                    case Keys.Space:
                        if (!isRepeat) TogglePlayPause();
                        return true;
                    case Keys.I:
                        if (!isRepeat) SetIn();
                        return true;
                    case Keys.O:
                        if (!isRepeat) SetOut();
                        return true;
                    case Keys.Left:
                    case Keys.Right:
                    case Keys.Shift | Keys.Left:
                    case Keys.Shift | Keys.Right:
                        if (txtSeek.Focused) return base.ProcessCmdKey(ref msg, keyData);
                        
                        // Calculate frameMs only when needed to avoid blocking on _mediaPlayer.Fps
                        double currentFps = _fps;
                        int fMs = (int)Math.Max(1, 1000.0 / currentFps);

                        // Throttle rapid seeking (max 20 requests per second)
                        long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        if (now - _lastSeekTime < 50) return true;
                        _lastSeekTime = now;

                        if (keyData == Keys.Left) SeekDelta(-fMs);
                        else if (keyData == Keys.Right) SeekDelta(fMs);
                        else if (keyData == (Keys.Shift | Keys.Left)) SeekDelta(-5 * fMs);
                        else if (keyData == (Keys.Shift | Keys.Right)) SeekDelta(5 * fMs);
                        return true;

                    case Keys.Enter:
                        if (!isRepeat) ToggleFullScreen();
                        return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private Form _fullScreenForm;
        private Control _originalParent;
        private Point _originalLocation;
        private Size _originalSize;
        private AnchorStyles _originalAnchor;

        private void ToggleFullScreen()
        {
            if (_videoView == null) return;

            if (_fullScreenForm == null)
            {
                // Enter Full Screen
                _originalParent = _videoView.Parent;
                _originalLocation = _videoView.Location;
                _originalSize = _videoView.Size;
                _originalAnchor = _videoView.Anchor;

                _fullScreenForm = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    WindowState = FormWindowState.Maximized,
                    BackColor = Color.Black,
                    ShowInTaskbar = false,
                    TopMost = true,
                    KeyPreview = true
                };

                _fullScreenForm.KeyDown += (s, e) => {
                    if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter)
                    {
                        ToggleFullScreen();
                    }
                    else if (e.KeyCode == Keys.Space)
                    {
                        TogglePlayPause();
                    }
                    else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
                    {
                        double fps = _fps;
                        int fMs = (int)Math.Max(1, 1000.0 / fps);
                        int delta = (e.Shift ? 5 : 1) * fMs;
                        if (e.KeyCode == Keys.Left) SeekDelta(-delta);
                        else SeekDelta(delta);
                    }
                };

                _videoView.Parent = _fullScreenForm;
                _videoView.Dock = DockStyle.Fill;
                _fullScreenForm.Show();
                _fullScreenForm.Focus();
            }
            else
            {
                // Exit Full Screen
                _videoView.Parent = _originalParent;
                _videoView.Dock = DockStyle.None;
                _videoView.Location = _originalLocation;
                _videoView.Size = _originalSize;
                _videoView.Anchor = _originalAnchor;

                _fullScreenForm.Close();
                _fullScreenForm = null;
                
                CenterControls();
                this.Focus();
                btnPlayPause.Focus();
            }
        }

        public void PauseVideo()
        {
            if (_mediaPlayer != null && _isPlaying)
            {
                Task.Run(() => _mediaPlayer.Pause());
                _timer.Stop();
                InvokeIfRequired(() => UpdateTimeLabels());
            }
        }

        private void TogglePlayPause()
        {
            if (_mediaPlayer == null || _mediaPlayer.Media == null || _isProcessing) return;

            _isProcessing = true;
            Task.Run(() => {
                try {
                    var state = _mediaPlayer.State;
                    if (state == VLCState.Ended || state == VLCState.Stopped)
                    {
                        _mediaPlayer.Stop(); 
                        _mediaPlayer.Play();
                        _isPlaying = true;
                        this.BeginInvoke(new Action(() => _timer.Start()));
                    }
                    else if (_isPlaying)
                    {
                        _mediaPlayer.Pause();
                        _isPlaying = false;
                        this.BeginInvoke(new Action(() => {
                            _timer.Stop();
                            UpdateTimeLabels();
                        }));
                    }
                    else
                    {
                        _mediaPlayer.Play();
                        _isPlaying = true;
                        this.BeginInvoke(new Action(() => _timer.Start()));
                    }
                } finally {
                    _isProcessing = false;
                }
            });
        }

        private void SeekDelta(long deltaMs)
        {
            if (_mediaPlayer == null || _mediaPlayer.Media == null || _isProcessing) return;
            
            _isProcessing = true;
            Task.Run(() => {
                try {
                    // If it's a small jump (frame-by-frame), pause first to ensure visual update
                    if (Math.Abs(deltaMs) < 1000 && _isPlaying)
                    {
                        _mediaPlayer.Pause();
                        _isPlaying = false;
                        this.BeginInvoke(new Action(() => _timer.Stop()));
                    }

                    long newTime = _currentMs + deltaMs;
                    if (newTime < 0) newTime = 0;
                    if (newTime > _durationMs) newTime = _durationMs;

                    _currentMs = newTime;
                    _lastSyncMs = newTime;
                    _interpolationTimer.Restart();
                    _mediaPlayer.Time = newTime;
                    
                    this.BeginInvoke(new Action(() => {
                        if (newTime >= tbTimeline.Minimum && newTime <= tbTimeline.Maximum)
                            tbTimeline.Value = (int)newTime;
                        UpdateTimeLabels();
                    }));
                } finally {
                    _isProcessing = false;
                }
            });
        }

        private void SeekToInput()
        {
            if (_mediaPlayer == null || _mediaPlayer.Media == null) return;
            string input = txtSeek.Text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            long newMs = -1;

            if (input.Contains(':'))
            {
                var parts = input.Split(':');
                if (parts.Length == 2) {
                    input = "00:" + input; 
                }
                if (TimeSpan.TryParse(input, out TimeSpan parsedTs)) {
                    newMs = (long)parsedTs.TotalMilliseconds;
                }
            }
            else if (double.TryParse(input, out double sec))
            {
                newMs = (long)(sec * 1000);
            }

            if (newMs != -1)
            {
                if (newMs < 0) newMs = 0;
                if (newMs > _durationMs) newMs = _durationMs;
                _currentMs = newMs;
                _lastSyncMs = newMs;
                _interpolationTimer.Restart();
                _mediaPlayer.Time = newMs;

                if (newMs >= tbTimeline.Minimum && newMs <= tbTimeline.Maximum)
                    tbTimeline.Value = (int)newMs;
                
                if (_isPlaying)
                {
                    Task.Run(() => _mediaPlayer.Pause());
                    _isPlaying = false;
                    _timer.Stop();
                }

                UpdateTimeLabels();
            }
        }

        private void SetIn()
        {
            if (_mediaPlayer == null || _mediaPlayer.Media == null) return;
            _inMs = _currentMs;
            if (_outMs != -1 && _inMs > _outMs) _outMs = -1;
            UpdateTimeLabels();
        }

        private void SetOut()
        {
            if (_mediaPlayer == null || _mediaPlayer.Media == null) return;
            _outMs = _currentMs;
            if (_inMs != -1 && _outMs < _inMs) _inMs = -1;
            UpdateTimeLabels();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(250, 250, 250);
            this.Size = new Size(612, 600);

            // Title & File selection
            Label lblTitle = new Label { Text = "미니 편집기", Font = new Font("Segoe UI", 14F, FontStyle.Bold), Location = new Point(20, 10), AutoSize = true };
            txtFile = new TextBox { Location = new Point(20, 45), Size = new Size(450, 25), Font = new Font("Segoe UI", 10F), ReadOnly = true, PlaceholderText = "비디오 파일 열기...", Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.White, TabStop = false };
            btnBrowse = new RoundButton { Text = "열기 📂", Location = new Point(480, 42), Size = new Size(110, 32), BackColor = Color.FromArgb(71, 85, 105), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, BorderRadius = 16, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.Click += (s, e) => {
                using var ofd = new OpenFileDialog { Filter = "Video Files|*.mp4;*.webm;*.avi;*.mkv|All Files|*.*" };
                if (ofd.ShowDialog() == DialogResult.OK) { txtFile.Text = ofd.FileName; LoadMedia(ofd.FileName); }
            };

            // Monitor Area - Initial size (CenterControls will handle responsiveness)
            _videoView = new VideoView { Location = new Point(20, 80), Size = new Size(570, 260), BackColor = Color.Black, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };

            // Timeline (Under Monitor)
            tbTimeline = new TrackBar { Location = new Point(15, 345), Size = new Size(470, 45), TickStyle = TickStyle.None, Minimum = 0, Maximum = 1000, Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            
            pnlInMarker = new Panel { Width = 4, Height = 14, BackColor = Color.FromArgb(34, 197, 94), Visible = false };
            pnlOutMarker = new Panel { Width = 4, Height = 14, BackColor = Color.FromArgb(239, 68, 68), Visible = false };
            pnlRangeHighlight = new Panel { Height = 6, BackColor = Color.FromArgb(120, 100, 116, 139), Visible = false }; // Indigo-gray semi-transparent
            
            tbTimeline.MouseDown += (s, e) => { 
                _isDraggingTrackBar = true; 
                if (_isPlaying) { Task.Run(() => _mediaPlayer.Pause()); _isPlaying = false; _timer.Stop(); }
                
                // Click-to-Seek logic
                if (tbTimeline.Width > 0) {
                    double percent = (double)e.X / tbTimeline.Width;
                    int newValue = (int)(percent * (tbTimeline.Maximum - tbTimeline.Minimum));
                    if (newValue >= tbTimeline.Minimum && newValue <= tbTimeline.Maximum) {
                        tbTimeline.Value = newValue;
                        if (_mediaPlayer?.Media != null) {
                            Task.Run(() => _mediaPlayer.Time = newValue);
                            _currentMs = newValue;
                            UpdateTimeLabels();
                        }
                    }
                }
            };
            
            tbTimeline.MouseUp += (s, e) => { 
                _isDraggingTrackBar = false; 
                if (_mediaPlayer?.Media != null) { 
                    int target = tbTimeline.Value;
                    Task.Run(() => _mediaPlayer.Time = target); 
                    _currentMs = target; 
                    UpdateTimeLabels(); 
                } 
            };
            
            tbTimeline.Scroll += (s, e) => { 
                if (_mediaPlayer?.Media != null) { 
                    int target = tbTimeline.Value;
                    _currentMs = target; 
                    Task.Run(() => _mediaPlayer.Time = target); // Smooth scrubbing
                    UpdateTimeLabels(); 
                } 
            };

            txtSeek = new TextBox { Location = new Point(490, 350), Size = new Size(55, 25), Font = new Font("Segoe UI", 9F), PlaceholderText = "분:초", TextAlign = HorizontalAlignment.Center, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, BackColor = Color.White };
            btnSeek = new RoundButton { Text = "이동", Location = new Point(550, 348), Size = new Size(40, 28), BackColor = Color.FromArgb(226, 232, 240), ForeColor = Color.FromArgb(71, 85, 105), FlatStyle = FlatStyle.Flat, BorderRadius = 14, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            btnSeek.FlatAppearance.BorderSize = 0;
            btnSeek.Click += (s, e) => { SeekToInput(); btnPlayPause.Focus(); };

            // Big Timecode Display - Now centered above Transport (HH:MM:SS:FF)
            lblTimecode = new Label { Text = "00:00:00:00", Font = new Font("Consolas", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 120, 215), Location = new Point(220, 385), Size = new Size(180, 40), TextAlign = ContentAlignment.MiddleCenter, Anchor = AnchorStyles.Bottom };

            // Transport Control Group (Centered)
            int yTransport = 420;
            btnPrevSec = new RoundButton { Text = "⏪", Location = new Point(150, yTransport), Size = new Size(40, 40), BackColor = Color.FromArgb(226, 232, 240), ForeColor = Color.FromArgb(71, 85, 105), FlatStyle = FlatStyle.Flat, BorderRadius = 20, Anchor = AnchorStyles.Bottom };
            btnPrevFrame = new RoundButton { Text = "◀", Location = new Point(200, yTransport), Size = new Size(40, 40), BackColor = Color.FromArgb(226, 232, 240), ForeColor = Color.FromArgb(71, 85, 105), FlatStyle = FlatStyle.Flat, BorderRadius = 20, Anchor = AnchorStyles.Bottom };
            btnPlayPause = new RoundButton { Text = "▶/||", Location = new Point(250, yTransport), Size = new Size(80, 40), BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, BorderRadius = 15, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Anchor = AnchorStyles.Bottom };
            btnStop = new RoundButton { Text = "■", Location = new Point(340, yTransport), Size = new Size(40, 40), BackColor = Color.FromArgb(226, 232, 240), ForeColor = Color.FromArgb(71, 85, 105), FlatStyle = FlatStyle.Flat, BorderRadius = 20, Anchor = AnchorStyles.Bottom };
            btnNextFrame = new RoundButton { Text = "▶", Location = new Point(390, yTransport), Size = new Size(40, 40), BackColor = Color.FromArgb(226, 232, 240), ForeColor = Color.FromArgb(71, 85, 105), FlatStyle = FlatStyle.Flat, BorderRadius = 20, Anchor = AnchorStyles.Bottom };
            btnNextSec = new RoundButton { Text = "⏩", Location = new Point(440, yTransport), Size = new Size(40, 40), BackColor = Color.FromArgb(226, 232, 240), ForeColor = Color.FromArgb(71, 85, 105), FlatStyle = FlatStyle.Flat, BorderRadius = 20, Anchor = AnchorStyles.Bottom };
            btnLoop = new RoundButton { Text = "Loop", Location = new Point(490, yTransport), Size = new Size(40, 40), BackColor = Color.FromArgb(226, 232, 240), ForeColor = Color.FromArgb(71, 85, 105), FlatStyle = FlatStyle.Flat, BorderRadius = 20, Anchor = AnchorStyles.Bottom };

            btnPrevSec.FlatAppearance.BorderSize = 0; btnPrevFrame.FlatAppearance.BorderSize = 0;
            btnNextFrame.FlatAppearance.BorderSize = 0; btnNextSec.FlatAppearance.BorderSize = 0;
            btnPlayPause.FlatAppearance.BorderSize = 0; btnStop.FlatAppearance.BorderSize = 0; btnLoop.FlatAppearance.BorderSize = 0;

            btnPlayPause.Click += (s, e) => TogglePlayPause();
            btnStop.Click += (s, e) => {
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Pause();
                    _mediaPlayer.Time = 0;
                    _currentMs = 0;
                    _timer.Stop();
                    InvokeIfRequired(() => {
                        tbTimeline.Value = 0;
                        UpdateTimeLabels();
                    });
                }
            };
            btnPrevSec.Click += (s, e) => {
                double fps = _fps > 1 ? _fps : 30.0;
                SeekDelta(-5 * (int)(1000.0 / fps));
            };
            btnPrevFrame.Click += (s, e) => {
                double fps = _fps > 1 ? _fps : 30.0;
                SeekDelta(-(int)(1000.0 / fps));
            };
            btnNextFrame.Click += (s, e) => {
                double fps = _fps > 1 ? _fps : 30.0;
                SeekDelta((int)(1000.0 / fps));
            };
            btnNextSec.Click += (s, e) => {
                double fps = _fps > 1 ? _fps : 30.0;
                SeekDelta(5 * (int)(1000.0 / fps));
            };
            btnLoop.Click += (s, e) => {
                _isLooping = !_isLooping;
                btnLoop.BackColor = _isLooping ? Color.FromArgb(0, 120, 215) : Color.FromArgb(226, 232, 240);
                btnLoop.ForeColor = _isLooping ? Color.White : Color.FromArgb(71, 85, 105);
            };

            // Marking Group (In / Out)
            btnSetIn = new RoundButton { Text = "{ IN", Size = new Size(55, 30), BackColor = Color.FromArgb(34, 197, 94), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, BorderRadius = 5, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Anchor = AnchorStyles.Bottom };
            btnSetOut = new RoundButton { Text = "OUT }", Size = new Size(55, 30), BackColor = Color.FromArgb(239, 68, 68), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, BorderRadius = 5, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Anchor = AnchorStyles.Bottom };
            btnClearAll = new RoundButton { Text = "Clear All", Size = new Size(70, 30), BackColor = Color.FromArgb(226, 232, 240), ForeColor = Color.FromArgb(71, 85, 105), FlatStyle = FlatStyle.Flat, BorderRadius = 5, Font = new Font("Segoe UI", 8F), Anchor = AnchorStyles.Bottom };

            btnSetIn.FlatAppearance.BorderSize = 0; btnSetOut.FlatAppearance.BorderSize = 0; btnClearAll.FlatAppearance.BorderSize = 0;

            btnSetIn.Click += (s, e) => SetIn();
            btnSetOut.Click += (s, e) => SetOut();
            btnClearAll.Click += (s, e) => { _inMs = -1; _outMs = -1; UpdateTimeLabels(); };

            // Volume Control
            tbVolume = new TrackBar { Minimum = 0, Maximum = 100, Value = 50, Size = new Size(100, 45), TickStyle = TickStyle.None, Anchor = AnchorStyles.Bottom };
            lblAudioGain = new Label { Text = "Audio Gain: 50%", Font = new Font("Segoe UI", 7F), AutoSize = true, ForeColor = Color.Gray, Anchor = AnchorStyles.Bottom };
            lblVolIcon = new Label { Text = "🔊", AutoSize = true, Font = new Font("Segoe UI", 12F), Cursor = Cursors.Hand, Anchor = AnchorStyles.Bottom };

            Action updateVolume = () => {
                if (_mediaPlayer != null) 
                {
                    _mediaPlayer.Volume = _isMuted ? 0 : tbVolume.Value;
                }
                lblVolIcon.Text = _isMuted || tbVolume.Value == 0 ? "🔇" : "🔊";
                lblAudioGain.Text = $"Audio Gain: {(_isMuted ? 0 : tbVolume.Value)}%";
            };

            tbVolume.Scroll += (s, e) => {
                _isMuted = false;
                updateVolume();
            };
            
            tbVolume.MouseDown += (s, e) => {
                if (tbVolume.Width > 0) {
                    double percent = (double)e.X / tbVolume.Width;
                    int newValue = (int)(percent * (tbVolume.Maximum - tbVolume.Minimum));
                    if (newValue >= tbVolume.Minimum && newValue <= tbVolume.Maximum) {
                        tbVolume.Value = newValue;
                        _isMuted = false;
                        updateVolume();
                    }
                }
            };

            lblVolIcon.Click += (s, e) => {
                if (!_isMuted) {
                    _isMuted = true;
                    _lastVolumeBeforeMute = tbVolume.Value;
                    tbVolume.Value = 0;
                } else {
                    _isMuted = false;
                    tbVolume.Value = Math.Max(10, _lastVolumeBeforeMute); // Don't restore to 0 if we were muted at 0
                }
                updateVolume();
            };

            // Info Labels
            int yLabel = 470;
            lblCurrentTime = new Label { Location = new Point(20, yLabel), Size = new Size(130, 20), Text = "Playhead: --:--:--", Font = new Font("Segoe UI", 8F), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            lblDuration = new Label { Location = new Point(140, yLabel), Size = new Size(130, 20), Text = "Duration: --:--:--", Font = new Font("Segoe UI", 8F), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            lblVideoInfo = new Label { Location = new Point(265, yLabel), Size = new Size(150, 20), Text = "Video: 0x0 | 0 FPS", Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(0, 120, 215), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            lblIn = new Label { Location = new Point(410, yLabel), Size = new Size(110, 20), ForeColor = Color.DarkGreen, Font = new Font("Segoe UI", 8F), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            lblOut = new Label { Location = new Point(515, yLabel), Size = new Size(110, 20), ForeColor = Color.DarkRed, Font = new Font("Segoe UI", 8F), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };

            // Export Actions
            int yExport = 505;
            cmbSaveOption = new ComboBox { Location = new Point(20, yExport + 5), Size = new Size(200, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            cmbSaveOption.Items.AddRange(new string[] { "원본 저장", "Premiere 호환 (H.264)", "오디오만 추출 (MP3)" });
            cmbSaveOption.SelectedIndex = 0;

            btnSave = new RoundButton { Text = " 저장 🎞️", Location = new Point(230, yExport), Size = new Size(130, 40), BackColor = Color.FromArgb(2, 132, 199), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, BorderRadius = 15, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            btnSave.Click += BtnSave_Click;

            lblStatus = new Label { Location = new Point(20, 535), AutoSize = true, Text = "준비", Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            btnCancel = new RoundButton { Text = "취소 ✖", Location = new Point(230, yExport), Size = new Size(130, 40), BackColor = Color.FromArgb(255, 71, 87), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, BorderRadius = 15, Visible = false, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            btnCancel.Click += (s, e) => _saveCts?.Cancel();

            pbProgress = new ProgressBar { Location = new Point(20, 555), Size = new Size(570, 10), Style = ProgressBarStyle.Continuous, Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            
            lblSavePath = new Label { Anchor = AnchorStyles.Bottom | AnchorStyles.Left, AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(80, 80, 80), Text = "현재 저장 위치: " + (SettingsManager.Settings?.DefaultDownloadFolder ?? "") };
            btnOpenFolder = new RoundButton { Anchor = AnchorStyles.Bottom | AnchorStyles.Left, BackColor = Color.FromArgb(226, 232, 240), BorderRadius = 13, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(51, 65, 85), Size = new Size(80, 26), Text = "폴더 열기 📂" };
            btnOpenFolder.FlatAppearance.BorderSize = 0;
            btnOpenFolder.Click += BtnOpenFolder_Click;
            lblSavePath.SizeChanged += (s, e) => {
                btnOpenFolder.Left = lblSavePath.Right + 5;
                btnOpenFolder.Top = lblSavePath.Top + (lblSavePath.Height - btnOpenFolder.Height) / 2;
            };

            this.Controls.AddRange(new Control[] { lblTitle, txtFile, btnBrowse, _videoView, lblTimecode, tbTimeline, pnlInMarker, pnlOutMarker, pnlRangeHighlight, txtSeek, btnSeek, 
                btnPrevSec, btnPrevFrame, btnPlayPause, btnStop, btnNextFrame, btnNextSec, btnLoop,
                btnSetIn, btnSetOut, btnClearAll, lblVolIcon, tbVolume, lblAudioGain,
                lblCurrentTime, lblDuration, lblVideoInfo, lblIn, lblOut, cmbSaveOption, btnSave, btnCancel, lblStatus, pbProgress, lblSavePath, btnOpenFolder });

            this.Click += (s, e) => btnPlayPause.Focus();
            foreach (Control c in this.Controls) if (c != txtSeek && c != txtFile && c != cmbSaveOption) c.Click += (s, e) => btnPlayPause.Focus();

            this.Resize += (s, e) => CenterControls();
            
            UpdateTimeLabels();
            CenterControls(); // Initial centering
            this.ResumeLayout(false);
        }

        private void CenterControls()
        {
            int mid = this.Width / 2;
            int h = this.Height;

            // Vertical offsets from bottom (Spread out to avoid overlap)
            int yProgress = h - 35;
            int yExport = h - 90;
            int yLabel = h - 135;
            int yTransport = h - 190;
            int yMark = h - 185; // Aligned with transport buttons
            int yTimecode = h - 240;
            int yTimeline = h - 300;
            int ySeekText = h - 295;
            int ySeekBtn = h - 297;

            // Total width for the main interaction row
            // [SetIn] -- [Transport] -- [SetOut] -- [ClearAll] -- [Volume]
            int transportWidth = 380;
            int totalMainWidth = 55 + 20 + transportWidth + 20 + 55 + 10 + 70; 
            int startX = mid - (totalMainWidth / 2);
            if (startX < 20) startX = 20;

            // IN Group
            btnSetIn.Location = new Point(startX, yMark);

            // Transport Group (Gap 10px)
            int tX = startX + 75;
            btnPrevSec.Location = new Point(tX, yTransport);
            btnPrevFrame.Location = new Point(tX + 50, yTransport);
            btnPlayPause.Location = new Point(tX + 100, yTransport);
            btnStop.Location = new Point(tX + 190, yTransport);
            btnNextFrame.Location = new Point(tX + 240, yTransport);
            btnNextSec.Location = new Point(tX + 290, yTransport);
            btnLoop.Location = new Point(tX + 340, yTransport);

            // OUT Group
            int oX = tX + 400;
            btnSetOut.Location = new Point(oX, yMark);
            
            // Clear All
            btnClearAll.Location = new Point(oX + 65, yMark);

            // Volume Group
            int vX = oX + 160;
            lblVolIcon.Location = new Point(vX, yMark - 2);
            tbVolume.Location = new Point(vX + 30, yMark);
            lblAudioGain.Location = new Point(vX + 42, yMark + 35);

            // Center Info Labels - Spread them out slightly more
            int labelWidth = this.Width - 40;
            int labelStartX = 20;
            int step = (this.Width - 40) / 5;
            lblCurrentTime.Location = new Point(labelStartX, yLabel);
            lblDuration.Location = new Point(labelStartX + step, yLabel);
            lblVideoInfo.Location = new Point(labelStartX + step * 2, yLabel);
            lblIn.Location = new Point(labelStartX + step * 3, yLabel);
            lblOut.Location = new Point(labelStartX + step * 4, yLabel);

            // Export Actions
            cmbSaveOption.Location = new Point(20, yExport + 5);
            btnSave.Location = new Point(230, yExport);
            btnCancel.Location = new Point(230, yExport);
            lblStatus.Location = new Point(20, yProgress - 20);

            // Timeline & Seek (These also need Y update)
            tbTimeline.Location = new Point(15, yTimeline);
            txtSeek.Location = new Point(this.Width - 122, ySeekText);
            btnSeek.Location = new Point(this.Width - 62, ySeekBtn);

            // Dynamic VideoView Sizing: Follow the video's actual aspect ratio
            int maxVideoW = this.Width - 40;
            int maxVideoH = yTimeline - 80 - 10; // Space between top and timeline
            
            if (maxVideoH > 50 && _videoWidth > 0 && _videoHeight > 0)
            {
                double ratio = (double)_videoWidth / _videoHeight;
                int targetW = maxVideoW;
                int targetH = (int)(targetW / ratio);
                
                if (targetH > maxVideoH)
                {
                    targetH = maxVideoH;
                    targetW = (int)(targetH * ratio);
                }
                
                _videoView.Size = new Size(targetW, targetH);
                // Center the video view horizontally in the available space
                _videoView.Location = new Point(mid - (targetW / 2), 80 + (maxVideoH - targetH) / 2);
            }

            // Center Timecode above transport (Fixed overlap)
            lblTimecode.Location = new Point(mid - (lblTimecode.Width / 2), yTimecode);

            UpdateMarkerPositions();

            // Save Location (Next to btnSave)
            lblSavePath.Location = new Point(370, yExport + 12);
            btnOpenFolder.Left = lblSavePath.Right + 5;
            btnOpenFolder.Top = lblSavePath.Top + (lblSavePath.Height - btnOpenFolder.Height) / 2;

            // Progress Bar
            pbProgress.Location = new Point(20, yProgress);
            pbProgress.Width = this.Width - 40;
        }

        private void LoadMedia(string path)
        {
            if (_mediaPlayer == null) return;
            
            // Reset markers for new video
            _inMs = -1;
            _outMs = -1;
            _currentMs = 0;
            _lastSyncMs = 0;
            _interpolationTimer.Reset();
            _shouldPauseOnLoad = true; // Use event to pause correctly
            
            var media = new Media(_libvlc, new Uri(path));
            _mediaPlayer.Media = media;
            _mediaPlayer.Play();

            InvokeIfRequired(() => {
                tbTimeline.Value = 0;
                UpdateTimeLabels();
            });
        }

        private async void BtnSaveClean_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtFile.Text)) { ShowCenteredMessage("\uD30C\uC77C\uC744 \uBA3C\uC800 \uC120\uD0DD\uD558\uC138\uC694."); return; }
            if (_inMs == -1 || _outMs == -1) { ShowCenteredMessage("In/Out \uC9C0\uC810\uC744 \uBA3C\uC800 \uC124\uC815\uD558\uC138\uC694."); return; }
            if (_inMs >= _outMs) { ShowCenteredMessage("In \uC9C0\uC810\uC774 Out \uC9C0\uC810\uBCF4\uB2E4 \uB2A6\uC744 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4."); return; }

            using var sfd = new SaveFileDialog { Filter = "Video Files|*.mp4;*.mkv|Audio Files|*.mp3" };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            string inputFile = txtFile.Text;
            string outputFile = sfd.FileName;
            _saveCts = new System.Threading.CancellationTokenSource();

            btnSave.Enabled = false;
            btnCancel.Visible = true;
            btnCancel.BringToFront();
            lblStatus.Text = "\uC800\uC7A5 \uC911...";
            pbProgress.Value = 0;

            try
            {
                string startTs = FormatTimeFFmpeg(_inMs);
                string durationTs = FormatTimeFFmpeg(_outMs - _inMs);
                double volFactor = tbVolume.Value / 100.0;
                string volFilter = $"-filter:a \"volume={volFactor:F2}\"";

                string args = "";
                if (cmbSaveOption.SelectedIndex == 0)
                    args = $"-ss {startTs} -t {durationTs} -i \"{inputFile}\" -c:v libx264 -crf 18 -preset slower {volFilter} -c:a aac -b:a 192k \"{outputFile}\" -y";
                else if (cmbSaveOption.SelectedIndex == 1)
                    args = $"-ss {startTs} -t {durationTs} -i \"{inputFile}\" -c:v libx264 -pix_fmt yuv420p -profile:v high -level 4.1 {volFilter} -c:a aac \"{outputFile}\" -y";
                else
                    args = $"-ss {startTs} -t {durationTs} -i \"{inputFile}\" -vn {volFilter} -c:a libmp3lame -b:a 192k \"{outputFile}\" -y";

                await RunFFmpegWithProgress(args, _saveCts.Token);
                lblStatus.Text = "\uC800\uC7A5 \uC131\uACF5!";
                if (SettingsManager.Settings.AutoOpenFolder)
                {
                    try { Process.Start("explorer.exe", Path.GetDirectoryName(outputFile)); } catch { }
                }
                ShowCenteredMessage("\uC601\uC0C1\uC774 \uC131\uACF5\uC801\uC73C\uB85C \uC800\uC7A5\uB418\uC5C8\uC2B5\uB2C8\uB2E4.");
            }
            catch (OperationCanceledException) { lblStatus.Text = "\uCDE8\uC18C\uB428"; }
            catch (Exception ex) { lblStatus.Text = "\uC624\uB958"; ShowCenteredMessage("\uB0B4\uBCF4\uB0B4\uAE30 \uC624\uB958: " + ex.Message); }
            finally
            {
                btnSave.Enabled = true;
                btnCancel.Visible = false;
                _saveCts = null;
            }
        }

        private void BtnOpenFolderClean_Click(object? sender, EventArgs e)
        {
            string path = SettingsManager.Settings?.DefaultDownloadFolder ?? "";
            if (string.IsNullOrEmpty(path)) path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            if (System.IO.Directory.Exists(path))
            {
                try { Process.Start("explorer.exe", path); } catch { }
            }
            else
            {
                ShowCenteredMessage("\uD3F4\uB354\uAC00 \uC874\uC7AC\uD558\uC9C0 \uC54A\uC2B5\uB2C8\uB2E4: " + path);
            }
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            await Task.CompletedTask;
            BtnSaveClean_Click(sender, e);
        }

        private string FormatTimeFFmpeg(long ms)
        {
            TimeSpan ts = TimeSpan.FromMilliseconds(ms);
            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        }

        private async Task RunFFmpegWithProgress(string args, System.Threading.CancellationToken token)
        {
            var tcs = new TaskCompletionSource<bool>();
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = SettingsManager.GetFFmpegPath(),
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            process.ErrorDataReceived += (s, e) => {
                if (e.Data != null && e.Data.Contains("time="))
                {
                    try {
                        var match = System.Text.RegularExpressions.Regex.Match(e.Data, @"time=(\d+:\d+:\d+\.\d+)");
                        if (match.Success) {
                            TimeSpan current = TimeSpan.Parse(match.Groups[1].Value);
                            long totalMs = _outMs - _inMs;
                            if (totalMs > 0) {
                                int progress = (int)((current.TotalMilliseconds / totalMs) * 100);
                                InvokeIfRequired(() => { if (progress >= 0 && progress <= 100) pbProgress.Value = progress; });
                            }
                        }
                    } catch {}
                }
            };

            process.Start();
            process.BeginErrorReadLine();

            using (token.Register(() => { try { process.Kill(); } catch {} }))
            {
                await Task.Run(() => process.WaitForExit());
            }

            if (process.ExitCode != 0 && !token.IsCancellationRequested)
                throw new Exception("FFmpeg failed with code " + process.ExitCode);
        }

        private void BtnOpenFolder_Click(object sender, EventArgs e)
        {
            string path = SettingsManager.Settings?.DefaultDownloadFolder ?? "";
            if (string.IsNullOrEmpty(path)) path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            
            if (System.IO.Directory.Exists(path))
            {
                try { Process.Start("explorer.exe", path); } catch { }
            }
            else
            {
                ShowCenteredMessage("폴더가 존재하지 않습니다: " + path);
            }
        }

        public void UpdateSavePath(string newPath)
        {
            if (lblSavePath != null)
            {
                lblSavePath.Text = "현재 저장 위치: " + newPath;
                return;
            }

            if (lblSavePath != null)
                lblSavePath.Text = "현재 저장 위치: " + newPath;
        }
    }
}
