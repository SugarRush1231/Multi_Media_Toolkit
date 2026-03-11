using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using System.Diagnostics;
using System.Globalization;
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

        private RoundButton btnBrowse;
        private TextBox txtFile;
        private Label lblCurrentTime, lblIn, lblOut, lblDuration;
        private RoundButton btnSetIn, btnSetOut, btnClearIn, btnClearOut, btnGoIn, btnGoOut;
        private RoundButton btnPrevFrame, btnNextFrame, btnPrevSec, btnNextSec;
        private RoundButton btnPlayPause;
        
        private ComboBox cmbSaveOption;
        private RoundButton btnSave;
        private ProgressBar pbProgress;
        private Label lblStatus;
        private RoundButton btnCancel;
        private System.Threading.CancellationTokenSource _saveCts;

        private TrackBar tbTimeline;
        private TextBox txtSeek;
        private RoundButton btnSeek;
        private bool _isDraggingTrackBar = false;

        private System.Windows.Forms.Timer _timer;

        public MiniEditor()
        {
            InitializeComponent();
            
            this.Load += (s, e) => {
                if (!DesignMode)
                {
                    Task.Run(() => {
                        InitializeVlc();
                    });
                }
            };

            _timer = new System.Windows.Forms.Timer { Interval = 33 }; // ~30fps update
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
                });

                _mediaPlayer.LengthChanged += (s, e) => {
                    _durationMs = _mediaPlayer.Length;
                    InvokeIfRequired(() => {
                        if (_durationMs > 0 && _durationMs <= int.MaxValue)
                            tbTimeline.Maximum = (int)_durationMs;
                        UpdateTimeLabels();
                    });
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
                if (!_isDraggingTrackBar && _currentMs >= tbTimeline.Minimum && _currentMs <= tbTimeline.Maximum)
                {
                    tbTimeline.Value = (int)_currentMs;
                }
                UpdateTimeLabels();
            }
        }

        private void UpdateTimeLabels()
        {
            lblCurrentTime.Text = $"Current Time: {FormatTime(_currentMs)}";
            lblDuration.Text = $"Duration: {FormatTime(_durationMs)}";
            lblIn.Text = $"In: {FormatTime(_inMs)}";
            lblOut.Text = $"Out: {FormatTime(_outMs)}";
        }

        private string FormatTime(long ms)
        {
            if (ms < 0) return "--:--:--.---";
            TimeSpan ts = TimeSpan.FromMilliseconds(ms);
            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        }

        // Handle keyboard shortcuts (Need to override ProcessCmdKey to capture shortcuts globally within the control)
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_mediaPlayer != null && _mediaPlayer.Media != null)
            {
                long stepMs = 0;

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
                    case Keys.Left: // 1 Frame back
                        if (txtSeek.Focused) return base.ProcessCmdKey(ref msg, keyData);
                        SeekDelta(- (int)(1000.0 / _fps));
                        return true;
                    case Keys.Right: // 1 Frame forward
                        if (txtSeek.Focused) return base.ProcessCmdKey(ref msg, keyData);
                        SeekDelta((int)(1000.0 / _fps));
                        return true;
                    case Keys.Left | Keys.Shift: // 1 Sec back
                        SeekDelta(-1000);
                        return true;
                    case Keys.Right | Keys.Shift: // 1 Sec forward
                        SeekDelta(1000);
                        return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
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
            long newTime = _mediaPlayer.Time + deltaMs;
            if (newTime < 0) newTime = 0;
            if (newTime > _durationMs) newTime = _durationMs;
            
            _mediaPlayer.Time = newTime;
            _currentMs = newTime;
            if (newTime >= tbTimeline.Minimum && newTime <= tbTimeline.Maximum)
                tbTimeline.Value = (int)newTime;
            UpdateTimeLabels();
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
                    input = "00:" + input; // Convert MM:SS to HH:MM:SS
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
                
                // Pause playback as requested after seek
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

        // ===================================
        // UI Layout Code
        // ===================================
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(250, 250, 250);
            this.Size = new Size(612, 551);

            // Title
            Label lblTitle = new Label { Text = "미니 편집", Font = new Font("Segoe UI", 16F, FontStyle.Bold), Location = new Point(20, 10), AutoSize = true };
            
            // File Selection
            txtFile = new TextBox { Location = new Point(20, 50), Size = new Size(450, 25), ReadOnly = true, Font = new Font("Segoe UI", 10F) };
            btnBrowse = new RoundButton { Text = "열기", Location = new Point(480, 48), Size = new Size(100, 30), BackColor = Color.LightGray, FlatStyle = FlatStyle.Flat, BorderRadius = 10 };
            btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.Click += (s, e) => {
                using var ofd = new OpenFileDialog { Filter = "Video Files|*.mp4;*.webm;*.avi;*.mkv|All Files|*.*" };
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtFile.Text = ofd.FileName;
                    LoadMedia(ofd.FileName);
                }
            };

            // Video Player Area
            _videoView = new VideoView { Location = new Point(20, 85), Size = new Size(570, 240), BackColor = Color.Black };
            _videoView.MouseClick += (s, e) => btnPlayPause.Focus(); // 배경이나 영상 클릭시 포커스 회수

            // Timeline and Seek
            tbTimeline = new TrackBar { Location = new Point(15, 330), Size = new Size(445, 30), TickStyle = TickStyle.None, Minimum = 0, Maximum = 1000 };
            tbTimeline.MouseDown += (s, e) => {
                _isDraggingTrackBar = true;
                if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Pause();
                    _timer.Stop();
                }
            };
            tbTimeline.MouseUp += (s, e) => {
                _isDraggingTrackBar = false;
                if (_mediaPlayer != null && _mediaPlayer.Media != null)
                {
                    _mediaPlayer.Time = tbTimeline.Value;
                    _currentMs = tbTimeline.Value;
                    
                    // Do not resume playing. Explicitly keep paused.
                    UpdateTimeLabels();
                }
            };
            tbTimeline.Scroll += (s, e) => {
                if (_mediaPlayer != null && _mediaPlayer.Media != null)
                {
                    _currentMs = tbTimeline.Value;
                    UpdateTimeLabels();
                }
            };

            txtSeek = new TextBox { Location = new Point(465, 330), Size = new Size(65, 25), Font = new Font("Segoe UI", 10F), PlaceholderText = "분:초", TextAlign = HorizontalAlignment.Center };
            txtSeek.KeyPress += (s, e) => { 
                if (e.KeyChar == (char)Keys.Enter) { 
                    e.Handled = true; 
                    SeekToInput(); 
                    btnPlayPause.Focus(); // 텍스트박스 포커스 해제해서 다시 단축키 동작하도록
                } 
            };

            btnSeek = new RoundButton { Text = "이동", Location = new Point(540, 328), Size = new Size(50, 28), BackColor = Color.LightGray, FlatStyle = FlatStyle.Flat, BorderRadius = 8 };
            btnSeek.FlatAppearance.BorderSize = 0;
            btnSeek.Click += (s, e) => { SeekToInput(); btnPlayPause.Focus(); };

            // Controls
            int yCtl = 365;
            btnPlayPause = new RoundButton { Text = "재생/일시정지 (Space)", Location = new Point(20, yCtl), Size = new Size(160, 30), BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, BorderRadius=10 };
            btnPlayPause.FlatAppearance.BorderSize = 0;
            btnPlayPause.Click += (s, e) => TogglePlayPause();

            btnPrevSec = new RoundButton { Text = "◀ 1 Sec", Location = new Point(190, yCtl), Size = new Size(75, 30), BackColor = Color.LightGray, FlatStyle = FlatStyle.Flat, BorderRadius=10 };
            btnPrevFrame = new RoundButton { Text = "◀ 1 Frame", Location = new Point(275, yCtl), Size = new Size(85, 30), BackColor = Color.LightGray, FlatStyle = FlatStyle.Flat, BorderRadius=10 };
            btnNextFrame = new RoundButton { Text = "1 Frame ▶", Location = new Point(370, yCtl), Size = new Size(85, 30), BackColor = Color.LightGray, FlatStyle = FlatStyle.Flat, BorderRadius=10 };
            btnNextSec = new RoundButton { Text = "1 Sec ▶", Location = new Point(465, yCtl), Size = new Size(75, 30), BackColor = Color.LightGray, FlatStyle = FlatStyle.Flat, BorderRadius=10 };

            btnPrevSec.FlatAppearance.BorderSize = 0; btnPrevFrame.FlatAppearance.BorderSize = 0;
            btnNextFrame.FlatAppearance.BorderSize = 0; btnNextSec.FlatAppearance.BorderSize = 0;

            btnPrevSec.Click += (s, e) => SeekDelta(-1000);
            btnPrevFrame.Click += (s, e) => SeekDelta(-33);
            btnNextFrame.Click += (s, e) => SeekDelta(33);
            btnNextSec.Click += (s, e) => SeekDelta(1000);

            int ySet = 405;
            btnSetIn = new RoundButton { Text = "Mark In (I)", Location = new Point(20, ySet), Size = new Size(110, 30), BackColor = Color.FromArgb(200, 200, 200), FlatStyle = FlatStyle.Flat, BorderRadius=8 };
            btnClearIn = new RoundButton { Text = "Clear In", Location = new Point(140, ySet), Size = new Size(80, 30), BackColor = Color.FromArgb(200, 200, 200), FlatStyle = FlatStyle.Flat, BorderRadius=8 };
            btnGoIn = new RoundButton { Text = "Go to In", Location = new Point(230, ySet), Size = new Size(80, 30), BackColor = Color.FromArgb(200, 200, 200), FlatStyle = FlatStyle.Flat, BorderRadius=8 };

            btnSetOut = new RoundButton { Text = "Mark Out (O)", Location = new Point(320, ySet), Size = new Size(110, 30), BackColor = Color.FromArgb(200, 200, 200), FlatStyle = FlatStyle.Flat, BorderRadius=8 };
            btnClearOut = new RoundButton { Text = "Clear Out", Location = new Point(440, ySet), Size = new Size(80, 30), BackColor = Color.FromArgb(200, 200, 200), FlatStyle = FlatStyle.Flat, BorderRadius=8 };
            btnGoOut = new RoundButton { Text = "Go to Out", Location = new Point(530, ySet), Size = new Size(80, 30), BackColor = Color.FromArgb(200, 200, 200), FlatStyle = FlatStyle.Flat, BorderRadius=8 };

            btnSetIn.FlatAppearance.BorderSize = 0; btnClearIn.FlatAppearance.BorderSize = 0; btnGoIn.FlatAppearance.BorderSize = 0;
            btnSetOut.FlatAppearance.BorderSize = 0; btnClearOut.FlatAppearance.BorderSize = 0; btnGoOut.FlatAppearance.BorderSize = 0;

            btnSetIn.Click += (s, e) => SetIn();
            btnClearIn.Click += (s, e) => { _inMs = -1; UpdateTimeLabels(); };
            btnGoIn.Click += (s, e) => { if (_inMs != -1 && _mediaPlayer != null) { _mediaPlayer.Time = _inMs; _currentMs = _inMs; UpdateTimeLabels(); } };

            btnSetOut.Click += (s, e) => SetOut();
            btnClearOut.Click += (s, e) => { _outMs = -1; UpdateTimeLabels(); };
            btnGoOut.Click += (s, e) => { if (_outMs != -1 && _mediaPlayer != null) { _mediaPlayer.Time = _outMs; _currentMs = _outMs; UpdateTimeLabels(); } };

            int yLbl = 445;
            lblCurrentTime = new Label { Location = new Point(20, yLbl), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            lblDuration = new Label { Location = new Point(170, yLbl), AutoSize = true };
            lblIn = new Label { Location = new Point(320, yLbl), AutoSize = true, ForeColor = Color.DarkGreen, Font = new Font("Segoe UI", 9F, FontStyle.Bold)  };
            lblOut = new Label { Location = new Point(470, yLbl), AutoSize = true, ForeColor = Color.DarkRed, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            cmbSaveOption = new ComboBox { Location = new Point(20, 480), Size = new Size(200, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F) };
            cmbSaveOption.Items.AddRange(new string[] { "정확 저장 (원본 코덱 및 해상도 재인코딩)", "Premiere 호환 저장", "오디오만 추출" });
            cmbSaveOption.SelectedIndex = 0;

            btnSave = new RoundButton { Text = "저장/출력", Location = new Point(230, 475), Size = new Size(130, 35), BackColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, BorderRadius = 15, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
 
            btnCancel = new RoundButton { Text = "취소", Location = new Point(530, 475), Size = new Size(60, 35), BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, BorderRadius = 10, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Visible = false };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => _saveCts?.Cancel();

            lblStatus = new Label { Location = new Point(380, 485), AutoSize = true, Text = "대기 중" };
            pbProgress = new ProgressBar { Location = new Point(20, 520), Size = new Size(570, 15), Style = ProgressBarStyle.Continuous };

            this.Controls.Add(btnCancel);

            this.Controls.Add(lblTitle);
            this.Controls.Add(txtFile);
            this.Controls.Add(btnBrowse);
            this.Controls.Add(_videoView);
            this.Controls.Add(btnPlayPause);
            this.Controls.Add(btnPrevSec);
            this.Controls.Add(btnPrevFrame);
            this.Controls.Add(btnNextFrame);
            this.Controls.Add(btnNextSec);
            this.Controls.Add(btnSetIn);
            this.Controls.Add(btnClearIn);
            this.Controls.Add(btnGoIn);
            this.Controls.Add(btnSetOut);
            this.Controls.Add(btnClearOut);
            this.Controls.Add(btnGoOut);
            this.Controls.Add(lblCurrentTime);
            this.Controls.Add(lblDuration);
            this.Controls.Add(lblIn);
            this.Controls.Add(lblOut);
            this.Controls.Add(cmbSaveOption);
            this.Controls.Add(btnSave);
            this.Controls.Add(lblStatus);
            this.Controls.Add(pbProgress);
            
            this.Controls.Add(tbTimeline);
            this.Controls.Add(txtSeek);
            this.Controls.Add(btnSeek);

            // 입력칸 외 빈 공간이나 다른 요소 클릭 시 포커스 해제해서 단축키 정상 작동하도록
            this.Click += (s, e) => btnPlayPause.Focus();
            foreach (Control c in this.Controls)
            {
                if (c != txtSeek && c != txtFile && c != cmbSaveOption)
                {
                    c.Click += (s, e) => btnPlayPause.Focus();
                }
            }

            UpdateTimeLabels();
            this.ResumeLayout(false);
        }

        private void LoadMedia(string path)
        {
            if (_mediaPlayer == null) return;
            var media = new Media(_libvlc, new Uri(path));
            media.Parse(MediaParseOptions.ParseNetwork);
            
            // Get FPS
            Task.Run(async () => {
                var procD = new Process();
                procD.StartInfo.FileName = "ffprobe.exe";
                procD.StartInfo.Arguments = $"-v error -select_streams v -of default=noprint_wrappers=1:nokey=1 -show_entries stream=r_frame_rate \"{path}\"";
                procD.StartInfo.UseShellExecute = false;
                procD.StartInfo.CreateNoWindow = true;
                procD.StartInfo.RedirectStandardOutput = true;
                procD.Start();
                string fpsStr = await procD.StandardOutput.ReadToEndAsync();
                try {
                    var parts = fpsStr.Trim().Split('/');
                    if (parts.Length == 2) {
                        _fps = double.Parse(parts[0]) / double.Parse(parts[1]);
                    }
                } catch { _fps = 30.0; }
            });

            _mediaPlayer.Play(media);
            
            // Wait briefly to load the first frame, then pause automatically
            Task.Run(async () => {
                await Task.Delay(200);
                InvokeIfRequired(() => {
                    if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
                    {
                        _mediaPlayer.Pause();
                    }
                });
            });

            _inMs = -1;
            _outMs = -1;
            _currentMs = 0;
            tbTimeline.Value = 0;
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            string inputFile = txtFile.Text;
            if (!File.Exists(inputFile)) return;

            long start = _inMs == -1 ? 0 : _inMs;
            long end = _outMs == -1 ? _durationMs : _outMs;
            if (start >= end && end != 0) { MessageBox.Show("In/Out 설정이 잘못되었습니다."); return; }

            string outDir = SettingsManager.Settings.DefaultDownloadFolder;
            if (string.IsNullOrWhiteSpace(outDir) || !Directory.Exists(outDir))
            {
                outDir = Path.GetDirectoryName(inputFile) ?? "";
            }
            string baseName = Path.GetFileNameWithoutExtension(inputFile);
            
            int opt = cmbSaveOption.SelectedIndex;
            string ext = opt == 2 ? "mp3" : "mp4";
            string suffix = opt == 0 ? "_exact" : (opt == 1 ? "_pr" : "_audio");
            string outputFile = Path.Combine(outDir, $"{baseName}{suffix}.{ext}");

            string ssArgs = $"-ss {start / 1000.0:F3}";
            string toArgs = _outMs != -1 ? $"-to {end / 1000.0:F3}" : "";

            string vcodecOpts = opt switch {
                0 => "-c:v libx264 -crf 18 -preset fast -c:a aac -b:a 192k", // Exact save -> fully re-encode for accuracy
                1 => "-c:v libx264 -crf 18 -preset fast -pix_fmt yuv420p -c:a aac -b:a 192k", // Premiere compatible
                _ => "-vn -c:a libmp3lame -b:a 320k" // Audio extraction
            };

            string args = $"-y -i \"{inputFile}\" {ssArgs} {toArgs} {vcodecOpts} \"{outputFile}\"";
            
            CleanupManager.RegisterFile(outputFile);
            
            btnSave.Enabled = false;
            btnCancel.Visible = true;
            lblStatus.Text = "변환 중...";
            pbProgress.Value = 0;
            
            // Temporary pause playing while saving
            bool wasPlaying = _mediaPlayer.IsPlaying;
            if (wasPlaying) _mediaPlayer.Pause();

            try {
                _saveCts = new System.Threading.CancellationTokenSource();
                await RunFFmpegWithProgress(args, (end - start), pbProgress, lblStatus, _saveCts.Token);
                
                CleanupManager.UnregisterFile(outputFile);

                InvokeIfRequired(() => {
                    lblStatus.Text = "변환 완료";
                    pbProgress.Value = 100;
                    lblStatus.Update();
                    pbProgress.Update();
                });

                MessageBox.Show($"저장되었습니다:\n{outputFile}", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } catch (OperationCanceledException) {
                InvokeIfRequired(() => {
                    lblStatus.Text = "취소됨";
                    pbProgress.Value = 0;
                });
                
                // Delete partial file with retry
                await Task.Run(async () => {
                    for (int i = 0; i < 10; i++) {
                        try {
                            if (File.Exists(outputFile)) {
                                File.Delete(outputFile);
                                break;
                            } else break;
                        } catch { await Task.Delay(300); }
                    }
                });
            } catch (Exception ex) {
                MessageBox.Show($"오류 발생:\n{ex.Message}");
            } finally {
                btnSave.Enabled = true;
                btnCancel.Visible = false;
                _saveCts?.Dispose();
                _saveCts = null;
                if (wasPlaying) _mediaPlayer.Play();
            }
        }

        private async Task RunFFmpegWithProgress(string args, double expectedDurationMs, ProgressBar pb, Label lbl, System.Threading.CancellationToken token)
        {
            var proc = new Process();
            proc.StartInfo.FileName = "ffmpeg.exe";
            proc.StartInfo.Arguments = args;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.RedirectStandardError = true;
            
            proc.Start();
            CleanupManager.RegisterProcess(proc);

            try {
                var regex = new System.Text.RegularExpressions.Regex(@"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})");

                long lastUpdate = 0;
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        proc.Kill(true);
                        token.ThrowIfCancellationRequested();
                    }

                    string? line = await proc.StandardError.ReadLineAsync();
                    if (line == null) break;

                    if (expectedDurationMs > 0)
                    {
                        var match = regex.Match(line);
                        if (match.Success)
                        {
                            double h = double.Parse(match.Groups[1].Value);
                            double m = double.Parse(match.Groups[2].Value);
                            double s = double.Parse(match.Groups[3].Value);
                            double ms = double.Parse(match.Groups[4].Value) * 10;
                            double currentMs = (h * 3600 + m * 60 + s) * 1000 + ms;

                            int pct = (int)((currentMs / expectedDurationMs) * 100);
                            if (pct > 100) pct = 100;
                            if (pct < 0) pct = 0;

                            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            if (now - lastUpdate > 100 || pct == 100)
                            {
                                lastUpdate = now;
                                InvokeIfRequired(() => {
                                    pb.Value = pct;
                                    lbl.Text = $"변환 중... {pct}%";
                                });
                            }
                        }
                    }
                }
                await proc.WaitForExitAsync();
                InvokeIfRequired(() => {
                    pb.Value = 100;
                    lbl.Text = "변환 완료!";
                    pb.Update();
                    lbl.Update();
                });
            }
            finally
            {
                if (!proc.HasExited)
                {
                    try { proc.Kill(true); } catch { }
                }
                await proc.WaitForExitAsync();
                CleanupManager.UnregisterProcess(proc);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Stop();
                _timer?.Dispose();
                _mediaPlayer?.Stop();
                _mediaPlayer?.Dispose();
                _libvlc?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
