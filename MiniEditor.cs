using System;
using System.Drawing;
using System.IO;
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

        private RoundButton btnBrowse;
        private TextBox txtFile;
        private Label lblCurrentTime, lblIn, lblOut, lblDuration;
        private RoundButton btnSetIn, btnSetOut, btnClearAll;
        private RoundButton btnPrevFrame, btnNextFrame, btnPrevSec, btnNextSec;
        private RoundButton btnPlayPause, btnStop;
        
        private ComboBox cmbSaveOption;
        private RoundButton btnSave;
        private ProgressBar pbProgress;
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

        public MiniEditor()
        {
            this.DoubleBuffered = true;
            InitializeComponent();
            
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
                    _durationMs = _mediaPlayer.Length;
                    InvokeIfRequired(() => {
                        if (_durationMs > 0 && _durationMs <= int.MaxValue)
                            tbTimeline.Maximum = (int)_durationMs;
                        
                        // Try to get actual FPS
                        if (_mediaPlayer != null && _mediaPlayer.Fps > 1)
                            _fps = _mediaPlayer.Fps;
                        
                        // Try to get actual Video size to adjust aspect ratio
                        var track = _mediaPlayer.Media?.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Video);
                        if (track.HasValue)
                        {
                            var videoTrack = track.Value.Data.Video;
                            if (videoTrack.Width > 0 && videoTrack.Height > 0)
                            {
                                _videoWidth = (int)videoTrack.Width;
                                _videoHeight = (int)videoTrack.Height;
                                CenterControls(); 
                            }
                        }
                            
                        UpdateTimeLabels();
                    });
                };

                // Smoother time tracking
                _mediaPlayer.TimeChanged += (s, e) => {
                    _currentMs = e.Time;
                    // Background sync, UI is mostly handled by 60fps timer for smoothness
                };

                // Handle pause on load
                _mediaPlayer.Playing += (s, e) => {
                    if (_shouldPauseOnLoad)
                    {
                        _shouldPauseOnLoad = false;
                        Task.Run(() => {
                            Task.Delay(50).ContinueWith(_ => _mediaPlayer.Pause());
                        });
                        InvokeIfRequired(() => _timer.Stop());
                    }
                    else
                    {
                        InvokeIfRequired(() => _timer.Start());
                    }
                };

                _mediaPlayer.Paused += (s, e) => {
                    InvokeIfRequired(() => _timer.Stop());
                };
            }
        }

        private void InvokeIfRequired(Action action)
        {
            if (this.InvokeRequired) this.Invoke(new MethodInvoker(action));
            else action();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
            {
                _currentMs = _mediaPlayer.Time;
                
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
                        _mediaPlayer.Time = _inMs;
                        _currentMs = _inMs;
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
            
            double fps = (_mediaPlayer != null && _mediaPlayer.Fps > 1) ? _mediaPlayer.Fps : _fps;
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
                // Only handle on KeyDown (prevent multiples from other messages)
                if (msg.Msg != 0x100 && msg.Msg != 0x104) 
                    return base.ProcessCmdKey(ref msg, keyData);

                double currentFps = (_mediaPlayer.Fps > 1) ? _mediaPlayer.Fps : _fps;
                int frameMs = (int)Math.Max(1, 1000.0 / currentFps);

                switch (keyData)
                {
                    case Keys.Space:
                        TogglePlayPause();
                        return true;
                    case Keys.I:
                        SetIn();
                        return true;
                    case Keys.O:
                        SetOut();
                        return true;
                    case Keys.Left:
                        if (txtSeek.Focused) return base.ProcessCmdKey(ref msg, keyData);
                        SeekDelta(-frameMs);
                        return true;
                    case Keys.Right:
                        if (txtSeek.Focused) return base.ProcessCmdKey(ref msg, keyData);
                        SeekDelta(frameMs);
                        return true;
                    case Keys.Shift | Keys.Left:
                        SeekDelta(-5 * frameMs);
                        return true;
                    case Keys.Shift | Keys.Right:
                        SeekDelta(5 * frameMs);
                        return true;
                    case Keys.Enter:
                        ToggleFullScreen();
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
            if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                _timer.Stop();
                InvokeIfRequired(() => UpdateTimeLabels());
            }
        }

        private void TogglePlayPause()
        {
            if (_mediaPlayer == null || _mediaPlayer.Media == null) return;
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                _timer.Stop();
                _currentMs = _mediaPlayer.Time;
                UpdateTimeLabels();
            }
            else
            {
                _mediaPlayer.Play();
                _timer.Start();
            }
        }

        private void SeekDelta(long deltaMs)
        {
            if (_mediaPlayer == null || _mediaPlayer.Media == null) return;
            
            // If it's a small jump (frame-by-frame), pause first to ensure visual update
            if (Math.Abs(deltaMs) < 1000 && _mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                _timer.Stop();
            }

            long newTime = _mediaPlayer.Time + deltaMs;
            if (newTime < 0) newTime = 0;
            if (newTime > _mediaPlayer.Length) newTime = _mediaPlayer.Length;

            _mediaPlayer.Time = newTime;
            _currentMs = newTime;
            
            // Visual feedback
            InvokeIfRequired(() => {
                tbTimeline.Value = (int)Math.Min(tbTimeline.Maximum, Math.Max(0, _currentMs));
                UpdateTimeLabels();
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
                _mediaPlayer.Time = newMs;
                _currentMs = newMs;
                if (newMs >= tbTimeline.Minimum && newMs <= tbTimeline.Maximum)
                    tbTimeline.Value = (int)newMs;
                
                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Pause();
                    _timer.Stop();
                }

                UpdateTimeLabels();
            }
        }

        private void SetIn()
        {
            if (_mediaPlayer == null || _mediaPlayer.Media == null) return;
            _inMs = _mediaPlayer.Time;
            if (_outMs != -1 && _inMs > _outMs) _outMs = -1;
            UpdateTimeLabels();
        }

        private void SetOut()
        {
            if (_mediaPlayer == null || _mediaPlayer.Media == null) return;
            _outMs = _mediaPlayer.Time;
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
                if (_mediaPlayer?.IsPlaying == true) { _mediaPlayer.Pause(); _timer.Stop(); }
                
                // Click-to-Seek logic
                if (tbTimeline.Width > 0) {
                    double percent = (double)e.X / tbTimeline.Width;
                    int newValue = (int)(percent * (tbTimeline.Maximum - tbTimeline.Minimum));
                    if (newValue >= tbTimeline.Minimum && newValue <= tbTimeline.Maximum) {
                        tbTimeline.Value = newValue;
                        if (_mediaPlayer?.Media != null) {
                            _mediaPlayer.Time = newValue;
                            _currentMs = newValue;
                            UpdateTimeLabels();
                        }
                    }
                }
            };
            
            tbTimeline.MouseUp += (s, e) => { 
                _isDraggingTrackBar = false; 
                if (_mediaPlayer?.Media != null) { 
                    _mediaPlayer.Time = tbTimeline.Value; 
                    _currentMs = tbTimeline.Value; 
                    UpdateTimeLabels(); 
                } 
            };
            
            tbTimeline.Scroll += (s, e) => { 
                if (_mediaPlayer?.Media != null) { 
                    _currentMs = tbTimeline.Value; 
                    _mediaPlayer.Time = _currentMs; // Smooth scrubbing
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
                double fps = (_mediaPlayer != null && _mediaPlayer.Fps > 1) ? _mediaPlayer.Fps : _fps;
                SeekDelta(-5 * (int)(1000.0 / fps));
            };
            btnPrevFrame.Click += (s, e) => {
                double fps = (_mediaPlayer != null && _mediaPlayer.Fps > 1) ? _mediaPlayer.Fps : _fps;
                SeekDelta(-(int)(1000.0 / fps));
            };
            btnNextFrame.Click += (s, e) => {
                double fps = (_mediaPlayer != null && _mediaPlayer.Fps > 1) ? _mediaPlayer.Fps : _fps;
                SeekDelta((int)(1000.0 / fps));
            };
            btnNextSec.Click += (s, e) => {
                double fps = (_mediaPlayer != null && _mediaPlayer.Fps > 1) ? _mediaPlayer.Fps : _fps;
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
            lblCurrentTime = new Label { Location = new Point(20, yLabel), Size = new Size(140, 20), Text = "Playhead: --:--:--", Font = new Font("Segoe UI", 8F), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            lblDuration = new Label { Location = new Point(160, yLabel), Size = new Size(140, 20), Text = "Duration: --:--:--", Font = new Font("Segoe UI", 8F), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            lblIn = new Label { Location = new Point(310, yLabel), Size = new Size(130, 20), ForeColor = Color.DarkGreen, Font = new Font("Segoe UI", 8F), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            lblOut = new Label { Location = new Point(450, yLabel), Size = new Size(130, 20), ForeColor = Color.DarkRed, Font = new Font("Segoe UI", 8F), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };

            // Export Actions
            int yExport = 505;
            cmbSaveOption = new ComboBox { Location = new Point(20, yExport + 5), Size = new Size(200, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            cmbSaveOption.Items.AddRange(new string[] { "원본 저장", "Premiere 호환 (H.264)", "오디오만 추출 (MP3)" });
            cmbSaveOption.SelectedIndex = 0;

            btnSave = new RoundButton { Text = " 저장 🎞️", Location = new Point(230, yExport), Size = new Size(180, 40), BackColor = Color.FromArgb(2, 132, 199), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, BorderRadius = 15, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            btnSave.Click += BtnSave_Click;

            lblStatus = new Label { Location = new Point(420, yExport + 10), AutoSize = true, Text = "준비", Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            btnCancel = new RoundButton { Text = "취소", Location = new Point(510, yExport), Size = new Size(80, 40), BackColor = Color.FromArgb(255, 71, 87), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, BorderRadius = 15, Visible = false, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            btnCancel.Click += (s, e) => _saveCts?.Cancel();

            pbProgress = new ProgressBar { Location = new Point(20, 555), Size = new Size(570, 10), Style = ProgressBarStyle.Continuous, Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };

            this.Controls.AddRange(new Control[] { lblTitle, txtFile, btnBrowse, _videoView, lblTimecode, tbTimeline, pnlInMarker, pnlOutMarker, pnlRangeHighlight, txtSeek, btnSeek, 
                btnPrevSec, btnPrevFrame, btnPlayPause, btnStop, btnNextFrame, btnNextSec, btnLoop,
                btnSetIn, btnSetOut, btnClearAll, lblVolIcon, tbVolume, lblAudioGain,
                lblCurrentTime, lblDuration, lblIn, lblOut, cmbSaveOption, btnSave, btnCancel, lblStatus, pbProgress });

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
            int labelWidth = 580;
            int labelStartX = mid - (labelWidth / 2);
            if (labelStartX < 20) labelStartX = 20;
            lblCurrentTime.Location = new Point(labelStartX, yLabel);
            lblDuration.Location = new Point(labelStartX + 150, yLabel);
            lblIn.Location = new Point(labelStartX + 300, yLabel);
            lblOut.Location = new Point(labelStartX + 440, yLabel);

            // Export Actions
            cmbSaveOption.Location = new Point(20, yExport + 5);
            btnSave.Location = new Point(230, yExport);
            lblStatus.Location = new Point(420, yExport + 10);
            btnCancel.Location = new Point(this.Width - 100, yExport);

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
            _shouldPauseOnLoad = true; // Use event to pause correctly
            
            var media = new Media(_libvlc, new Uri(path));
            _mediaPlayer.Media = media;
            _mediaPlayer.Play();

            InvokeIfRequired(() => {
                tbTimeline.Value = 0;
                UpdateTimeLabels();
            });
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtFile.Text)) { MessageBox.Show("파일을 먼저 선택하세요."); return; }
            if (_inMs == -1 || _outMs == -1) { MessageBox.Show("In/Out 지점을 먼저 설정하세요."); return; }
            if (_inMs >= _outMs) { MessageBox.Show("In 지점이 Out 지점보다 늦을 수 없습니다."); return; }

            using var sfd = new SaveFileDialog { Filter = "Video Files|*.mp4;*.mkv|Audio Files|*.mp3" };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            string inputFile = txtFile.Text;
            string outputFile = sfd.FileName;
            _saveCts = new System.Threading.CancellationTokenSource();

            btnSave.Enabled = false;
            btnCancel.Visible = true;
            lblStatus.Text = "Saving...";
            pbProgress.Value = 0;

            try
            {
                string startTs = FormatTimeFFmpeg(_inMs);
                string durationTs = FormatTimeFFmpeg(_outMs - _inMs);
                double volFactor = tbVolume.Value / 100.0;
                string volFilter = $"-filter:a \"volume={volFactor:F2}\"";

                string args = "";
                if (cmbSaveOption.SelectedIndex == 0) // Accurate (High Quality)
                    args = $"-ss {startTs} -t {durationTs} -i \"{inputFile}\" -c:v libx264 -crf 18 -preset slower {volFilter} -c:a aac -b:a 192k \"{outputFile}\" -y";
                else if (cmbSaveOption.SelectedIndex == 1) // Premiere Compatible
                    args = $"-ss {startTs} -t {durationTs} -i \"{inputFile}\" -c:v libx264 -pix_fmt yuv420p -profile:v high -level 4.1 {volFilter} -c:a aac \"{outputFile}\" -y";
                else // Audio Only
                    args = $"-ss {startTs} -t {durationTs} -i \"{inputFile}\" -vn {volFilter} -c:a libmp3lame -b:a 192k \"{outputFile}\" -y";

                await RunFFmpegWithProgress(args, _saveCts.Token);
                lblStatus.Text = "저장 성공!";
                MessageBox.Show("영상이 성공적으로 저장되었습니다.");
            }
            catch (OperationCanceledException) { lblStatus.Text = "취소됨"; }
            catch (Exception ex) { lblStatus.Text = "Error"; MessageBox.Show("Export Error: " + ex.Message); }
            finally
            {
                btnSave.Enabled = true;
                btnCancel.Visible = false;
                _saveCts = null;
            }
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
                FileName = "ffmpeg.exe",
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
    }
}
