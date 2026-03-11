using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Converter;
using Xabe.FFmpeg.Downloader;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections.Generic;
using YoutubeExplode.Videos;
using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using System.Net.Http;
using System.Text.Json;

namespace YoutubeDownloader;

public partial class Form1 : Form
{
    private YoutubeClient _youtube;
    private StreamManifest _streamManifest;
    private Video _currentVideo;
    private string _customTitle = "";
    
    // Download Queue
    private ConcurrentQueue<DownloadJob> _downloadQueue;
    private List<DownloadJob> _activeJobs;
    private bool _isDownloading = false;
    
    // GitHub Update Settings (리포지토리 생성 후 아래 정보를 수정하세요)
    private const string GITHUB_USER = "SugarRush1231"; // 본인의 GitHub 아이디
    private const string REPO_NAME = "Multi_Media_Toolkit"; // 리포지토리 이름
    private static readonly HttpClient _httpClient = new HttpClient();
    
    // Conversion Cancellation
    private CancellationTokenSource? _webmCts;
    private CancellationTokenSource? _codecCts;
    private CancellationTokenSource? _audioCts;
    private CancellationTokenSource? _ytDlpCts;
    private int _lastWidth = 800;
    private int _lastHeight = 600;
    private const string CURR_VERSION = "1.0.7";

    public Form1()
    {
        InitializeComponent();
        SettingsManager.Load();

        if (File.Exists("mmt.ico"))
        {
            try 
            { 
                var icon = new Icon("mmt.ico");
                this.Icon = icon; 
                this.notifyIconApp.Icon = icon;
            } 
            catch { }
        }
        this.notifyIconApp.Text = "Multi Media Toolkit";
        
        _youtube = new YoutubeClient();
        _downloadQueue = new ConcurrentQueue<DownloadJob>();
        _activeJobs = new List<DownloadJob>();

        // 저장 경로 초기 표시
        string initialPath = SettingsManager.Settings?.DefaultDownloadFolder ?? "";
        if (string.IsNullOrEmpty(initialPath)) initialPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        lblYtDlpSavePath.Text = "현재 저장 위치: " + initialPath;
        txtDownloadFolder.Text = SettingsManager.Settings?.DefaultDownloadFolder ?? "";

        // Enable DoubleBuffering
        typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(lvQueue, true, null);

        AppDomain.CurrentDomain.ProcessExit += (s, e) => CleanupManager.FullSystemCleanup();
        AppDomain.CurrentDomain.UnhandledException += (s, e) => CleanupManager.FullSystemCleanup();

        this.Text = $"Multi Media Toolkit (v{CURR_VERSION})";
        lblAbout.Text = $"Multi Media Toolkit v{CURR_VERSION}\r\nCreated by 김병석\r\n© {DateTime.Now.Year} all rights reserved.\r\n(kbs318@naver.com)";
    }
    
    private void Form1_Load(object sender, EventArgs e)
    {
        // Load Settings into UI
        string defaultDir = SettingsManager.Settings.DefaultDownloadFolder;
        txtDownloadFolder.Text = defaultDir;
        chkShowNotifications.Checked = SettingsManager.Settings.ShowNotifications;

        // Populate other tabs with the default path initially if it exists
        if (!string.IsNullOrWhiteSpace(defaultDir) && Directory.Exists(defaultDir))
        {
            txtWebMOutput.Text = defaultDir;
            txtCodecOutput.Text = defaultDir;
            txtAudioOutput.Text = defaultDir;
        }

        // Initialize Tab Active Styles
        UpdateTabStyles(btnTabYoutube);

        // [속도 최적화] 프로그램 시작 시 WebView2 엔진 미리 예열 (비공개 모드 클릭 시 렉 방지)
        _ = PreInitializeWebView2Async();

        cmbQuality.IntegralHeight = false;
        cmbQuality.MaxDropDownItems = 10;

        this.FormClosing += Form1_FormClosing;

        // 시작 시 조용히 업데이트 확인
        _ = CheckForUpdateAsync(false);
        
        // 필수 도구(ffmpeg, yt-dlp 등) 체크 및 설치 가이드
        _ = EnsureRequiredToolsAsync();
    }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        // Cancel all active conversions
        _webmCts?.Cancel();
        _codecCts?.Cancel();
        _audioCts?.Cancel();
        foreach (var job in _activeJobs) job.JobCts?.Cancel();

        // Perform final cleanup of processes and partial files
        CleanupManager.FullSystemCleanup();
    }

    private void Panel_Paint(object sender, PaintEventArgs e)
    {
        var panel = sender as Panel;
        if (panel == null) return;
        ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle,
            Color.FromArgb(200, 200, 200), 1, ButtonBorderStyle.Solid,
            Color.FromArgb(200, 200, 200), 1, ButtonBorderStyle.Solid,
            Color.FromArgb(200, 200, 200), 1, ButtonBorderStyle.Solid,
            Color.FromArgb(200, 200, 200), 1, ButtonBorderStyle.Solid);
    }

    private async Task EnsureRequiredToolsAsync()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
        
        string[] toolNames = { "ffmpeg.exe", "ffprobe.exe", "yt-dlp.exe" };
        bool anyMissing = false;

        foreach (var tool in toolNames)
        {
            string baseToolPath = Path.Combine(baseDir, tool);
            string rootToolPath = Path.Combine(projectRoot, tool);

            // 1. 프로젝트 루트에 있으면 실행 폴더로 복사
            if (!File.Exists(baseToolPath) && File.Exists(rootToolPath))
            {
                try { File.Copy(rootToolPath, baseToolPath, true); } catch { }
            }
            // 2. 실행 폴더에 있으면 프로젝트 루트로 복사 (사용자 가시성 위함)
            else if (File.Exists(baseToolPath) && !File.Exists(rootToolPath) && Directory.Exists(projectRoot))
            {
                try { File.Copy(baseToolPath, rootToolPath, true); } catch { }
            }

            if (!File.Exists(baseToolPath)) anyMissing = true;
        }

        if (anyMissing)
        {
            var result = MessageBox.Show(
                "프로그램 운영에 필요한 필수 도구(FFmpeg, yt-dlp)가 설치되어 있지 않습니다.\n" +
                "자동으로 다운로드하여 설치하시겠습니까?\n\n" +
                "(약 1~2분 정도 소요되며, 완료 후 프로젝트 폴더에도 나타납니다.)",
                "필수 도구 설치 안내",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                lblYtDlpStatus.Text = "도구 다운로드 중... (창을 끄지 마세요)";
                try
                {
                    const string RAW_URL_BASE = "https://github.com/SugarRush1231/Multi_Media_Toolkit/raw/main/";
                    using var client = new HttpClient();

                    // 1. ffmpeg.exe
                    if (!File.Exists(Path.Combine(baseDir, "ffmpeg.exe")))
                    {
                        lblYtDlpStatus.Text = "FFmpeg 다운로드 중... (1/3)";
                        var res = await client.GetAsync(RAW_URL_BASE + "ffmpeg.exe");
                        res.EnsureSuccessStatusCode();
                        await using var fs = new FileStream(Path.Combine(baseDir, "ffmpeg.exe"), FileMode.Create);
                        await res.Content.CopyToAsync(fs);
                    }

                    // 2. ffprobe.exe
                    if (!File.Exists(Path.Combine(baseDir, "ffprobe.exe")))
                    {
                        lblYtDlpStatus.Text = "ffprobe 다운로드 중... (2/3)";
                        var res = await client.GetAsync(RAW_URL_BASE + "ffprobe.exe");
                        res.EnsureSuccessStatusCode();
                        await using var fs = new FileStream(Path.Combine(baseDir, "ffprobe.exe"), FileMode.Create);
                        await res.Content.CopyToAsync(fs);
                    }

                    // 3. yt-dlp.exe
                    if (!File.Exists(Path.Combine(baseDir, "yt-dlp.exe")))
                    {
                        lblYtDlpStatus.Text = "yt-dlp 다운로드 중... (3/3)";
                        var res = await client.GetAsync(RAW_URL_BASE + "yt-dlp.exe");
                        res.EnsureSuccessStatusCode();
                        await using var fs = new FileStream(Path.Combine(baseDir, "yt-dlp.exe"), FileMode.Create);
                        await res.Content.CopyToAsync(fs);
                    }

                    // 다운로드 후 프로젝트 루트로도 복사 (사용자가 바로 확인할 수 있게)
                    foreach (var tool in toolNames)
                    {
                        string baseToolPath = Path.Combine(baseDir, tool);
                        string rootToolPath = Path.Combine(projectRoot, tool);
                        if (File.Exists(baseToolPath) && !File.Exists(rootToolPath) && Directory.Exists(projectRoot))
                        {
                            try { File.Copy(baseToolPath, rootToolPath, true); } catch { }
                        }
                    }

                    lblYtDlpStatus.Text = "모든 도구 준비 완료!";
                    MessageBox.Show("모든 필수 도구가 성공적으로 설치되었습니다.\n이제 정상적으로 이용 가능합니다.", "설치 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"다운로드 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblYtDlpStatus.Text = "도구 설치 실패";
                }
            }
        }
    }

    private async Task EnsureYtDlpAsync()
    {
        string ytdlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yt-dlp.exe");
        try
        {
            using var client = new HttpClient();
            // yt-dlp 공식 릴리즈 페이지에서 최신 exe 다운로드
            var response = await client.GetAsync("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe");
            response.EnsureSuccessStatusCode();
            
            await using var fs = new FileStream(ytdlpPath, FileMode.Create);
            await response.Content.CopyToAsync(fs);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"yt-dlp 다운로드 실패: {ex.Message}");
            throw;
        }
    }

    private void BtnTab_Click(object sender, EventArgs e)
    {
        var btn = sender as RoundButton;
        if (btn == null) return;

        // [UI 직관성] 사이드바 탭을 클릭하면 켜져있는 비공개 모드를 자동으로 해제
        if (tglXPrivateMode.Checked)
        {
            tglXPrivateMode.Checked = false;
        }

        UpdateTabStyles(btn);
        
        if (btn == btnTabYoutube) tabControlMain.SelectedTab = tabYoutube;
        else if (btn == btnTabYtDlp) tabControlMain.SelectedTab = tabYtDlp;
        else if (btn == btnTabCodec) tabControlMain.SelectedTab = tabCodec;
        else if (btn == btnTabWebM) tabControlMain.SelectedTab = tabWebM;
        else if (btn == btnTabAudio) tabControlMain.SelectedTab = tabAudio;
        else if (btn == btnTabMiniEdit) tabControlMain.SelectedTab = tabMiniEdit;
        else if (btn == btnTabSettings) tabControlMain.SelectedTab = tabSettings;
    }

    private void UpdateTabStyles(RoundButton activeBtn)
    {
        RoundButton[] tabs = { btnTabYoutube, btnTabYtDlp, btnTabCodec, btnTabWebM, btnTabAudio, btnTabMiniEdit, btnTabSettings };
        foreach (var t in tabs)
        {
            if (t == activeBtn)
            {
                t.BackColor = Color.FromArgb(255, 71, 87);
                t.ForeColor = Color.White;
            }
            else
            {
                t.BackColor = Color.FromArgb(50, 50, 50);
                t.ForeColor = Color.Silver;
            }
        }
    }

    private void Notify(string title, string text)
    {
        if (SettingsManager.Settings.ShowNotifications)
        {
            notifyIconApp.ShowBalloonTip(3000, title, text, ToolTipIcon.Info);
        }
    }

    // ============================================
    // YOUTUBE DOWNLOADER LOGIC
    // ============================================

    private async void BtnLoad_Click(object sender, EventArgs e)
    {
        string url = txtUrl.Text.Trim();
        if (string.IsNullOrEmpty(url)) return;

        try
        {
            btnLoad.Enabled = false;
            lblVideoTitle.Text = "영상 정보를 불러오는 중...";
            
            _currentVideo = await _youtube.Videos.GetAsync(url);
            _customTitle = _currentVideo.Title;
            UpdateVideoInfoDisplay();

            // Clear previous thumbnail
            picThumbnail.Image = null;

            // Prefer JPG if possible (System.Drawing might not support WebP easily)
            var thumb = _currentVideo.Thumbnails
                            .OrderByDescending(t => t.Resolution.Area)
                            .FirstOrDefault(t => t.Url.Contains(".jpg") || t.Url.Contains(".jpeg"))
                            ?? _currentVideo.Thumbnails.OrderByDescending(t => t.Resolution.Area).FirstOrDefault();

            if (thumb != null)
            {
                try
                {
                    using var client = new System.Net.Http.HttpClient();
                    var bytes = await client.GetByteArrayAsync(thumb.Url);
                    using var ms = new MemoryStream(bytes);
                    picThumbnail.Image = Image.FromStream(ms);
                }
                catch
                {
                    // Fallback to simpler loading if manual fails
                    try { picThumbnail.LoadAsync(thumb.Url); } catch { }
                }
            }

            _streamManifest = await _youtube.Videos.Streams.GetManifestAsync(url);
            
            cmbQuality.BeginUpdate();
            cmbQuality.Items.Clear();
            cmbQuality.SelectedIndex = -1;
            
            var videoStreams = _streamManifest.GetVideoOnlyStreams()
                .OrderByDescending(s => s.VideoQuality.MaxHeight)
                .GroupBy(s => s.VideoQuality.Label)
                .Select(g => g.First())
                .ToList();

            foreach (var stream in videoStreams)
            {
                cmbQuality.Items.Add(new QualityOption($"{stream.VideoQuality.Label} (MP4)", stream.VideoQuality.Label, true));
            }

            cmbQuality.Items.Add(new QualityOption("오디오 전용 (MP3 320kbps)", "best_mp3", false));
            cmbQuality.Items.Add(new QualityOption("오디오 전용 (WAV)", "best_wav", false));
            cmbQuality.Items.Add(new QualityOption("오디오 전용 (M4A)", "best_m4a", false));
            cmbQuality.Items.Add(new QualityOption("오디오 전용 (FLAC)", "best_flac", false));

            if (cmbQuality.Items.Count > 0)
            {
                cmbQuality.SelectedIndex = 0;
                // Force drop-down height recalculation to avoid 2-row glitch
                cmbQuality.DropDownHeight = 300; 
            }
            else
            {
                lblVideoTitle.Text = "지원하는 스트림을 찾지 못했습니다.";
            }
            cmbQuality.EndUpdate();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"오류: {ex.Message}", "에러", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblVideoTitle.Text = "오류가 발생했습니다.";
        }
        finally
        {
            btnLoad.Enabled = true;
        }
    }

    private void BtnAddQueue_Click(object sender, EventArgs e)
    {
        if (_streamManifest == null || _currentVideo == null || cmbQuality.SelectedItem == null)
        {
            MessageBox.Show("영상을 먼저 불러오세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedOption = (QualityOption)cmbQuality.SelectedItem;
        
        string outputPath = "";

        // Check if Default Folder is set
        if (!string.IsNullOrWhiteSpace(SettingsManager.Settings.DefaultDownloadFolder) && Directory.Exists(SettingsManager.Settings.DefaultDownloadFolder))
        {
            string ext = selectedOption.IsVideo ? "mp4" : selectedOption.Id.Replace("best_", "");
            outputPath = Path.Combine(SettingsManager.Settings.DefaultDownloadFolder, $"{MakeValidFileName(_customTitle)}.{ext}");
            
            // To avoid overwrite, add numbers to filename if exists
            int count = 1;
            while(File.Exists(outputPath)) {
                outputPath = Path.Combine(SettingsManager.Settings.DefaultDownloadFolder, $"{MakeValidFileName(_customTitle)} ({count}).{ext}");
                count++;
            }
        }
        else
        {
            using var sfd = new SaveFileDialog();
            sfd.FileName = MakeValidFileName(_customTitle);
            
            if (selectedOption.IsVideo)
            {
                sfd.Filter = "MP4 파일|*.mp4";
                sfd.DefaultExt = "mp4";
            }
            else
            {
                string ext = selectedOption.Id.Replace("best_", "");
                sfd.Filter = $"{ext.ToUpper()} 파일|*.{ext}";
                sfd.DefaultExt = ext;
            }

            if (sfd.ShowDialog() != DialogResult.OK) return;
            outputPath = sfd.FileName;
        }

        var item = new ListViewItem(_customTitle);
        item.SubItems.Add(selectedOption.Title);
        item.SubItems.Add("대기 중");
        lvQueue.Items.Add(item);

        var job = new DownloadJob
        {
            Id = Guid.NewGuid().ToString(),
            Video = _currentVideo,
            Manifest = _streamManifest,
            Option = selectedOption,
            OutputPath = outputPath,
            ListViewItem = item,
            JobCts = new CancellationTokenSource(),
            CustomFileName = _customTitle
        };
        
        item.Tag = job;

        _downloadQueue.Enqueue(job);
        
        txtUrl.Text = "";
        lblVideoTitle.Text = "URL을 입력하고 '영상 확인' 버튼을 눌러주세요.";
        picThumbnail.Image = null;
        cmbQuality.Items.Clear();
        _currentVideo = null!;
        _streamManifest = null!;
        _customTitle = "";
        
        lblStatus.Text = $"{lvQueue.Items.Count}개의 작업이 큐에 있습니다.";

        if (!_isDownloading)
        {
            _ = ProcessDownloadQueueAsync();
        }
    }

    private async void BtnYtDlpRun_Click(object sender, EventArgs e)
    {
        string url = txtYtDlpUrl.Text.Trim();
        if (string.IsNullOrEmpty(url))
        {
            MessageBox.Show("다운로드할 URL을 입력해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            // Use default folder from settings if available
            string savePath = SettingsManager.Settings.DefaultDownloadFolder;
            if (string.IsNullOrWhiteSpace(savePath) || !Directory.Exists(savePath))
            {
                using var fbd = new FolderBrowserDialog();
                fbd.Description = "영상을 저장할 폴더를 선택하세요.";
                if (fbd.ShowDialog() != DialogResult.OK) return;
                savePath = fbd.SelectedPath;
            }

            btnYtDlpRun.Enabled = false;
            btnYtDlpCancel.Visible = true;
            btnYtDlpCancel.Enabled = true;
            lblYtDlpStatus.Text = "yt-dlp 확인 중...";
            if (tglXPrivateMode.Checked) lblXStatus.Text = "yt-dlp 확인 중...";
            
            _ytDlpCts = new CancellationTokenSource();
            
            await EnsureYtDlpAsync(_ytDlpCts.Token);

            lblYtDlpStatus.Text = "다운로드 준비 중...";
            pbYtDlp.Value = 0;
            if (tglXPrivateMode.Checked) {
                lblXStatus.Text = "다운로드 준비 중...";
                pbXDownload.Value = 0;
            }

            YtDlpDownloader downloader = new YtDlpDownloader();
            downloader.OnProgressChanged += (progress) =>
            {
                int pct = (int)Math.Min(progress, 100);
                string msg = $"다운로드 진행 중... {progress:F1}%";
                MirrorProgress(pct, msg);
            };

            // [치지직 정책] video(VOD) 및 clips 허용
            if (url.Contains("chzzk.naver.com") && !url.Contains("/video/") && !url.Contains("/clips/"))
            {
                MessageBox.Show("치지직은 'video' 또는 'clips' 주소만 다운로드할 수 있습니다.\n(라이브는 지원하지 않습니다.)", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string browser = "none";
            string cookieFile = "";
            
            if (tglXPrivateMode.Checked)
            {
                try
                {
                    // WebView2에서 쿠키를 추출하여 임시 파일로 저장
                    cookieFile = await ExportWebViewCookiesAsync();
                    
                    if (string.IsNullOrEmpty(cookieFile))
                    {
                        MessageBox.Show("브라우저에서 로그인 정보를 찾을 수 없습니다.\n먼저 비공개 모드 브라우저에서 로그인을 완료해 주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return; // 정지하거나 경고 후 진행
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"로그인 정보를 가져오지 못했습니다: {ex.Message}", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            
            string finalFilePath = await downloader.DownloadVideoAsync(url, savePath, browser, _ytDlpCts.Token, cookieFile);

            Notify("다운로드 완료", "영상 다운로드가 완료되었습니다.");
            MessageBox.Show($"다운로드 완료!\n저장 위치: {finalFilePath}", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            txtYtDlpUrl.Text = ""; // 성공 시 주소창 초기화
            string successMsg = "다운로드 완료! (아래 파란색 '저장 위치'를 눌러 폴더를 여세요)";
            lblYtDlpStatus.Text = successMsg;
            pbYtDlp.Value = 100;

            if (tglXPrivateMode.Checked)
            {
                lblXStatus.Text = successMsg;
                pbXDownload.Value = 100;
            }
        }
        catch (OperationCanceledException)
        {
            lblYtDlpStatus.Text = "다운로드가 취소되었습니다.";
            if (tglXPrivateMode.Checked) lblXStatus.Text = "다운로드가 취소되었습니다.";
            MessageBox.Show("다운로드가 취소되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            bool ytDlpExists = File.Exists(Path.Combine(baseDir, "yt-dlp.exe"));
            bool ffmpegExists = File.Exists(Path.Combine(baseDir, "ffmpeg.exe"));

            string errorMsg = "에러 발생: " + ex.Message;
            if (!ytDlpExists || !ffmpegExists)
            {
                errorMsg += "\n\n(실행 폴더에 yt-dlp.exe 또는 ffmpeg.exe가 없습니다.)";
            }
            
            lblYtDlpStatus.Text = "에러 발생: " + ex.Message;
            if (tglXPrivateMode.Checked) lblXStatus.Text = "오류: " + ex.Message;
            pbYtDlp.Value = 0;
            if (tglXPrivateMode.Checked) pbXDownload.Value = 0;
        }
        finally
        {
            // 임시 쿠키 파일 삭제
            string cookiePath = Path.Combine(SettingsManager.UserDataFolder, "temp_x_cookies.txt");
            if (File.Exists(cookiePath)) 
            {
                try { File.Delete(cookiePath); } catch {}
            }

            btnYtDlpRun.Enabled = true;
            btnYtDlpCancel.Visible = false;
            _ytDlpCts?.Dispose();
            _ytDlpCts = null;
        }
    }

    private void MirrorProgress(int pct, string msg)
    {
        if (this.IsDisposed || this.Disposing) return;
        this.Invoke((MethodInvoker)delegate {
            pbYtDlp.Value = pct;
            lblYtDlpStatus.Text = msg;
            
            if (tglXPrivateMode.Checked)
            {
                pbXDownload.Value = pct;
                lblXStatus.Text = msg;
            }
        });
    }


    private void BtnRemoveSelected_Click(object sender, EventArgs e)
    {
        if (lvQueue.SelectedItems.Count == 0) return;

        foreach (ListViewItem item in lvQueue.SelectedItems)
        {
            if (item.Tag is DownloadJob job)
            {
                if (job.JobCts != null && !job.JobCts.IsCancellationRequested)
                {
                    job.JobCts.Cancel();
                }
                lvQueue.Items.Remove(item);
                _activeJobs.Remove(job);
            }
        }
        
        lblStatus.Text = "* 대기열 항목 우클릭으로도 취소가 가능합니다.";
    }

    private async Task ProcessDownloadQueueAsync()
    {
        _isDownloading = true;

        await EnsureFFmpegAsync();
        Xabe.FFmpeg.FFmpeg.SetExecutablesPath(AppDomain.CurrentDomain.BaseDirectory);

        while (_downloadQueue.TryDequeue(out var job))
        {
            try
            {
                if (job.JobCts.IsCancellationRequested || !lvQueue.Items.Contains(job.ListViewItem))
                    continue;
                
                _activeJobs.Add(job);
                job.ListViewItem.SubItems[2].Text = "준비 중...";
                lblStatus.Text = $"진행 중: {job.Video.Title}";

                var progress = new Progress<double>(p =>
                {
                    if (!lvQueue.Items.Contains(job.ListViewItem))
                        job.JobCts.Cancel();

                    int pct = (int)(p * 100);
                    if (!this.IsDisposed && !this.Disposing)
                    {
                        try {
                            this.Invoke((MethodInvoker)delegate {
                                job.ListViewItem.SubItems[2].Text = $"{pct}%";
                                pbYoutube.Value = pct;
                            });
                        } catch { }
                    }
                });

                IStreamInfo[] targetStreams;
                var audioStream = job.Manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                if (!job.Option.IsVideo)
                {
                    targetStreams = new IStreamInfo[] { audioStream };
                }
                else
                {
                    var videoStream = job.Manifest.GetVideoOnlyStreams().FirstOrDefault(s => s.VideoQuality.Label == job.Option.Id) 
                                      ?? job.Manifest.GetVideoOnlyStreams().GetWithHighestVideoQuality();
                    targetStreams = new IStreamInfo[] { audioStream, videoStream };
                }

                var builder = new ConversionRequestBuilder(job.OutputPath).SetPreset(ConversionPreset.UltraFast);
                
                if (!job.Option.IsVideo) 
                {
                    string ext = job.Option.Id.Replace("best_", "");
                    builder.SetContainer(ext);
                }

                CleanupManager.RegisterFile(job.OutputPath);
                await _youtube.Videos.DownloadAsync(targetStreams, builder.Build(), progress, job.JobCts.Token);
                CleanupManager.UnregisterFile(job.OutputPath);
                
                if (!this.IsDisposed && !this.Disposing && lvQueue.Items.Contains(job.ListViewItem))
                {
                    this.Invoke((MethodInvoker)delegate {
                        job.ListViewItem.SubItems[2].Text = "완료";
                        pbYoutube.Value = 100;
                    });
                }
                
                Notify("다운로드 성공", $"{job.Video.Title} 다운로드가 완료되었습니다.");
            }
            catch (OperationCanceledException)
            {
                if (!this.IsDisposed && !this.Disposing && lvQueue.Items.Contains(job.ListViewItem))
                {
                    this.Invoke((MethodInvoker)delegate {
                        job.ListViewItem.SubItems[2].Text = "취소됨";
                        pbYoutube.Value = 0;
                    });
                }
                Notify("다운로드 취소", $"{job.Video.Title} 다운로드가 취소되었습니다.");
            }
            catch (Exception ex)
            {
                if (!this.IsDisposed && !this.Disposing && lvQueue.Items.Contains(job.ListViewItem))
                {
                    this.Invoke((MethodInvoker)delegate {
                        job.ListViewItem.SubItems[2].Text = "오류";
                        pbYoutube.Value = 0;
                    });
                }
                Notify("다운로드 실패", $"{job.Video.Title} 다운로드 중 오류가 발생했습니다.");
            }
            finally
            {
                _activeJobs.Remove(job);
            }
        }

        lblStatus.Text = "모든 처리가 완료되었습니다.";
        pbYoutube.Value = 0;
        _isDownloading = false;
    }

    // ============================================
    // WEBM TO MP4 CONVERTER LOGIC
    // ============================================

    private void BtnBrowseWebM_Click(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog();
        ofd.Filter = "MP4 파일|*.mp4|모든 영상 파일|*.*";
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            txtWebMInput.Text = ofd.FileName;
            // Use default folder from settings if available
            string defaultDir = SettingsManager.Settings.DefaultDownloadFolder;
            if (!string.IsNullOrWhiteSpace(defaultDir) && Directory.Exists(defaultDir))
            {
                txtWebMOutput.Text = defaultDir;
            }
            else
            {
                txtWebMOutput.Text = Path.GetDirectoryName(ofd.FileName);
            }
        }
    }

    private void BtnBrowseWebMOutput_Click(object sender, EventArgs e)
    {
        using var fbd = new FolderBrowserDialog();
        if (fbd.ShowDialog() == DialogResult.OK)
        {
            txtWebMOutput.Text = fbd.SelectedPath;
        }
    }

    private void BtnYtDlpCancel_Click(object sender, EventArgs e)
    {
        _ytDlpCts?.Cancel();
        btnYtDlpCancel.Enabled = false;
        lblYtDlpStatus.Text = "취소 중...";
        if (tglXPrivateMode.Checked) lblXStatus.Text = "취소 중...";
    }

    private async void BtnConvertWebM_Click(object sender, EventArgs e)
    {
        string inputFile = txtWebMInput.Text;
        if (!File.Exists(inputFile))
        {
            MessageBox.Show("정확한 입력 파일을 지정하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string format = cmbWebMFormat.SelectedItem?.ToString() ?? "WebM (.webm)";
        // Priority: UI Box > Default Settings > Input File Dir
        string outDir = txtWebMOutput.Text;
        if (string.IsNullOrWhiteSpace(outDir)) outDir = SettingsManager.Settings.DefaultDownloadFolder;
        if (string.IsNullOrWhiteSpace(outDir)) outDir = Path.GetDirectoryName(inputFile) ?? "";
        
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        string fileNameNoExt = Path.GetFileNameWithoutExtension(inputFile);
        string outputFile = "";
        string args = "";
        bool isSequence = false;
        string sequenceDir = "";

        if (format.Contains("WebM"))
        {
            outputFile = Path.Combine(outDir, fileNameNoExt + "_converted.webm");
            args = $"-i \"{inputFile}\" -c:v libvpx-vp9 -crf 30 -b:v 0 -c:a libopus \"{outputFile}\" -y";
        }
        else if (format.Contains("MOV"))
        {
            outputFile = Path.Combine(outDir, fileNameNoExt + ".mov");
            args = $"-i \"{inputFile}\" -c:v libx264 -crf 18 -c:a aac -b:a 192k \"{outputFile}\" -y";
        }
        else if (format.Contains("MKV"))
        {
            outputFile = Path.Combine(outDir, fileNameNoExt + ".mkv");
            args = $"-i \"{inputFile}\" -c:v libx264 -crf 18 -c:a aac -b:a 192k \"{outputFile}\" -y";
        }
        else if (format.Contains("AVI"))
        {
            outputFile = Path.Combine(outDir, fileNameNoExt + ".avi");
            // AVI prefers mp3 or pcm common for old players, but h264/mp3 is a good modern balance
            args = $"-i \"{inputFile}\" -c:v libx264 -crf 18 -c:a libmp3lame -b:a 192k \"{outputFile}\" -y";
        }
        else if (format.Contains("WMV"))
        {
            outputFile = Path.Combine(outDir, fileNameNoExt + ".wmv");
            args = $"-i \"{inputFile}\" -c:v wmv2 -b:v 5M -c:a wmav2 -b:a 128k \"{outputFile}\" -y";
        }
        else if (format.Contains("GIF"))
        {
            outputFile = Path.Combine(outDir, fileNameNoExt + ".gif");
            // Reduced scale to 480p and fps to 12 to prevent hangs on large files
            args = $"-i \"{inputFile}\" -vf \"fps=12,scale=480:-1:flags=lanczos,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\" \"{outputFile}\" -y";
        }
        else if (format.Contains("JPG Sequence"))
        {
            isSequence = true;
            sequenceDir = Path.Combine(outDir, fileNameNoExt + "_jpg_seq");
            if (!Directory.Exists(sequenceDir)) Directory.CreateDirectory(sequenceDir);
            outputFile = Path.Combine(sequenceDir, "frame_%04d.jpg");
            args = $"-i \"{inputFile}\" -qscale:v 2 \"{outputFile}\" -y";
        }
        else if (format.Contains("PNG Sequence"))
        {
            isSequence = true;
            sequenceDir = Path.Combine(outDir, fileNameNoExt + "_png_seq");
            if (!Directory.Exists(sequenceDir)) Directory.CreateDirectory(sequenceDir);
            outputFile = Path.Combine(sequenceDir, "frame_%04d.png");
            args = $"-i \"{inputFile}\" \"{outputFile}\" -y";
        }

        try
        {
            _webmCts = new CancellationTokenSource();
            btnConvertWebM.Enabled = false;
            btnCancelWebM.Visible = true;
            lblWebMStatus.Text = "변환 중... 잠시만 기다려주세요.";
            pbWebM.Value = 0;
 
            await EnsureFFmpegAsync();
            Xabe.FFmpeg.FFmpeg.SetExecutablesPath(AppDomain.CurrentDomain.BaseDirectory);
 
            CleanupManager.RegisterFile(isSequence ? sequenceDir : outputFile);

            await RunFFmpegWithProgress(args, inputFile, pbWebM, lblWebMStatus, _webmCts.Token);
  
            CleanupManager.UnregisterFile(isSequence ? sequenceDir : outputFile);

            Notify("변환 성공", "포맷 변환이 완료되었습니다.");
            string showPath = isSequence ? sequenceDir : outputFile;
            MessageBox.Show($"저장 위치:\n{showPath}", "변환 성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            this.Invoke((MethodInvoker)delegate {
                lblWebMStatus.Text = "변환이 취소되었습니다.";
                pbWebM.Value = 0;
            });

            // Delete partial file/folder with retry logic
            await Task.Run(async () => {
                for (int i = 0; i < 10; i++) {
                    try {
                        if (isSequence) {
                            if (Directory.Exists(sequenceDir)) Directory.Delete(sequenceDir, true);
                            break;
                        } else {
                            if (File.Exists(outputFile)) {
                                File.Delete(outputFile);
                                break;
                            } else break;
                        }
                    } catch { await Task.Delay(300); }
                }
            });

            Notify("변환 취소", "사용자에 의해 변환이 취소되었습니다.");
        }
        catch (Exception ex)
        {
            this.Invoke((MethodInvoker)delegate {
                lblWebMStatus.Text = "변환 실패.";
                pbWebM.Value = 0;
            });
            Notify("변환 실패", "변환 중 오류가 발생했습니다.");
            MessageBox.Show($"변환 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            this.Invoke((MethodInvoker)delegate {
                btnConvertWebM.Enabled = true;
                btnCancelWebM.Visible = false;
                _webmCts?.Dispose();
                _webmCts = null;
            });
        }
    }

    private void BtnCancelWebM_Click(object sender, EventArgs e)
    {
        _webmCts?.Cancel();
    }

    // ============================================
    // CODEC FIX LOGIC (Premiere Pro)
    // ============================================

    private void BtnBrowseCodec_Click(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog();
        ofd.Filter = "MP4 파일|*.mp4|모든 영상 파일|*.*";
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            txtCodecInput.Text = ofd.FileName;
            // Use default folder from settings if available
            string defaultDir = SettingsManager.Settings.DefaultDownloadFolder;
            if (!string.IsNullOrWhiteSpace(defaultDir) && Directory.Exists(defaultDir))
            {
                txtCodecOutput.Text = defaultDir;
            }
            else
            {
                txtCodecOutput.Text = Path.GetDirectoryName(ofd.FileName);
            }
        }
    }

    private void BtnBrowseCodecOutput_Click(object sender, EventArgs e)
    {
        using var fbd = new FolderBrowserDialog();
        if (fbd.ShowDialog() == DialogResult.OK)
        {
            txtCodecOutput.Text = fbd.SelectedPath;
        }
    }

    private async void BtnConvertCodec_Click(object sender, EventArgs e)
    {
        string inputFile = txtCodecInput.Text;
        if (!File.Exists(inputFile))
        {
            MessageBox.Show("정확한 MP4 입력 파일을 지정하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Priority: UI Box > Default Settings > Input File Dir
        string outDir = txtCodecOutput.Text;
        if (string.IsNullOrWhiteSpace(outDir)) outDir = SettingsManager.Settings.DefaultDownloadFolder;
        if (string.IsNullOrWhiteSpace(outDir)) outDir = Path.GetDirectoryName(inputFile) ?? "";
        
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        string outputFile = Path.Combine(outDir, Path.GetFileNameWithoutExtension(inputFile) + "_fixed.mp4");

        try
        {
            _codecCts = new CancellationTokenSource();
            btnConvertCodec.Enabled = false;
            btnCancelCodec.Visible = true;
            lblCodecStatus.Text = "코덱 변환 중... (시간이 오래 걸릴 수 있습니다.)";
            pbCodec.Value = 0;
 
            CleanupManager.RegisterFile(outputFile);

            await EnsureFFmpegAsync();
            Xabe.FFmpeg.FFmpeg.SetExecutablesPath(AppDomain.CurrentDomain.BaseDirectory);
 
            string args = $"-i \"{inputFile}\" -c:v libx264 -preset fast -crf 18 -r 30 -pix_fmt yuv420p -c:a aac -b:a 192k \"{outputFile}\" -y";
            await RunFFmpegWithProgress(args, inputFile, pbCodec, lblCodecStatus, _codecCts.Token);
  
            CleanupManager.UnregisterFile(outputFile);

            Notify("변환 성공", "프리미어 프로용 코덱 변환이 완료되었습니다.");
            MessageBox.Show($"저장 위치:\n{outputFile}", "변환 성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            this.Invoke((MethodInvoker)delegate {
                lblCodecStatus.Text = "변환이 취소되었습니다.";
                pbCodec.Value = 0;
            });

            // Delete partial file with retry logic
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

            Notify("변환 취소", "사용자에 의해 변환이 취소되었습니다.");
        }
        catch (Exception ex)
        {
            this.Invoke((MethodInvoker)delegate {
                lblCodecStatus.Text = "변환 실패.";
                pbCodec.Value = 0;
            });
            Notify("변환 실패", "코덱 변환 중 오류가 발생했습니다.");
            MessageBox.Show($"변환 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            this.Invoke((MethodInvoker)delegate {
                btnConvertCodec.Enabled = true;
                btnCancelCodec.Visible = false;
                _codecCts?.Dispose();
                _codecCts = null;
            });
        }
    }

    private void BtnCancelCodec_Click(object sender, EventArgs e)
    {
        _codecCts?.Cancel();
    }

    // ============================================
    // AUDIO CONVERTER LOGIC

    // ============================================

    private void BtnBrowseAudio_Click(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog();
        ofd.Filter = "미디어 파일|*.mp4;*.webm;*.mkv;*.avi;*.flv;*.mp3;*.wav;*.m4a;*.ogg;*.flac|모든 파일|*.*";
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            txtAudioInput.Text = ofd.FileName;
            // Use default folder from settings if available
            string defaultDir = SettingsManager.Settings.DefaultDownloadFolder;
            if (!string.IsNullOrWhiteSpace(defaultDir) && Directory.Exists(defaultDir))
            {
                txtAudioOutput.Text = defaultDir;
            }
            else
            {
                txtAudioOutput.Text = Path.GetDirectoryName(ofd.FileName);
            }
        }
    }

    private void BtnBrowseAudioOutput_Click(object sender, EventArgs e)
    {
        using var fbd = new FolderBrowserDialog();
        if (fbd.ShowDialog() == DialogResult.OK)
        {
            txtAudioOutput.Text = fbd.SelectedPath;
        }
    }

    private async void BtnConvertAudio_Click(object sender, EventArgs e)
    {
        string inputFile = txtAudioInput.Text;
        if (!File.Exists(inputFile))
        {
            MessageBox.Show("정확한 입력 파일을 지정하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string format = cmbAudioFormat.SelectedItem?.ToString() ?? "MP3";
        string ext = format.ToLower();
        
        // Priority: UI Box > Default Settings > Input File Dir
        string outDir = txtAudioOutput.Text;
        if (string.IsNullOrWhiteSpace(outDir)) outDir = SettingsManager.Settings.DefaultDownloadFolder;
        if (string.IsNullOrWhiteSpace(outDir)) outDir = Path.GetDirectoryName(inputFile) ?? "";

        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        string outputFile = Path.Combine(outDir, Path.GetFileNameWithoutExtension(inputFile) + $"_converted.{ext}");

        try
        {
            _audioCts = new CancellationTokenSource();
            btnConvertAudio.Enabled = false;
            btnCancelAudio.Visible = true;
            lblAudioStatus.Text = $"{format} (으)로 변환 중... 잠시만 기다려주세요.";
            pbAudio.Value = 0;
 
            CleanupManager.RegisterFile(outputFile);

            await EnsureFFmpegAsync();
            Xabe.FFmpeg.FFmpeg.SetExecutablesPath(AppDomain.CurrentDomain.BaseDirectory);
 
            string args = ext switch
            {
                "mp3" => $"-i \"{inputFile}\" -vn -ar 44100 -ac 2 -b:a 320k \"{outputFile}\" -y",
                "wav" => $"-i \"{inputFile}\" -vn -acodec pcm_s16le -ar 44100 -ac 2 \"{outputFile}\" -y",
                "flac" => $"-i \"{inputFile}\" -vn -c:a flac \"{outputFile}\" -y",
                "ogg" => $"-i \"{inputFile}\" -vn -c:a libvorbis -q:a 4 \"{outputFile}\" -y",
                "m4a" => $"-i \"{inputFile}\" -vn -c:a aac -b:a 192k \"{outputFile}\" -y",
                _ => $"-i \"{inputFile}\" -vn \"{outputFile}\" -y"
            };
 
            await RunFFmpegWithProgress(args, inputFile, pbAudio, lblAudioStatus, _audioCts.Token);
  
            CleanupManager.UnregisterFile(outputFile);

            Notify("변환 성공", $"{format} 변환이 완료되었습니다.");
            MessageBox.Show($"저장 위치:\n{outputFile}", "변환 성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            this.Invoke((MethodInvoker)delegate {
                lblAudioStatus.Text = "변환이 취소되었습니다.";
                pbAudio.Value = 0;
            });

            // Delete partial file with retry logic
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

            Notify("변환 취소", "사용자에 의해 변환이 취소되었습니다.");
        }
        catch (Exception ex)
        {
            this.Invoke((MethodInvoker)delegate {
                lblAudioStatus.Text = "변환 실패.";
                pbAudio.Value = 0;
            });
            Notify("변환 실패", "변환 중 오류가 발생했습니다.");
            MessageBox.Show($"변환 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            this.Invoke((MethodInvoker)delegate {
                btnConvertAudio.Enabled = true;
                btnCancelAudio.Visible = false;
                _audioCts?.Dispose();
                _audioCts = null;
            });
        }
    }

    private void BtnCancelAudio_Click(object sender, EventArgs e)
    {
        _audioCts?.Cancel();
    }


    // ============================================
    // SETTINGS LOGIC
    // ============================================

    private void BtnBrowseFolder_Click(object sender, EventArgs e)
    {
        using var fbd = new FolderBrowserDialog();
        if (fbd.ShowDialog() == DialogResult.OK)
        {
            txtDownloadFolder.Text = fbd.SelectedPath;
            lblYtDlpSavePath.Text = "현재 저장 위치: " + fbd.SelectedPath;
        }
    }

    private void BtnSaveSettings_Click(object sender, EventArgs e)
    {
        string newPath = txtDownloadFolder.Text.Trim();
        SettingsManager.Settings.DefaultDownloadFolder = newPath;
        SettingsManager.Settings.ShowNotifications = chkShowNotifications.Checked;
        SettingsManager.Save();

        // Update all conversion tab output paths in real-time
        if (!string.IsNullOrWhiteSpace(newPath) && Directory.Exists(newPath))
        {
            txtWebMOutput.Text = newPath;
            txtCodecOutput.Text = newPath;
            txtAudioOutput.Text = newPath;
            lblYtDlpSavePath.Text = "현재 저장 위치: " + newPath;
        }

        MessageBox.Show("설정이 안전하게 저장되었습니다.", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BtnFullCleanup_Click(object sender, EventArgs e)
    {
        try
        {
            CleanupManager.FullSystemCleanup();
            MessageBox.Show("임시 파일 및 메모리 정리가 완료되었습니다.", "정리 완료");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"정리 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnCheckUpdate_Click(object sender, EventArgs e)
    {
        await CheckForUpdateAsync(true);
    }

    private async Task CheckForUpdateAsync(bool manual)
    {
        try
        {
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "MMT-Updater");

            string url = $"https://api.github.com/repos/{GITHUB_USER}/{REPO_NAME}/releases/latest";
            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            string latestVersion = root.GetProperty("tag_name").GetString()?.Replace("v", "").Trim() ?? "";
            string currentVersion = CURR_VERSION.Trim(); 

            if (Version.TryParse(latestVersion, out var latest) && Version.TryParse(currentVersion, out var current))
            {
                if (latest.CompareTo(current) > 0)
                {
                    var result = MessageBox.Show($"새로운 업데이트(v{latestVersion})가 존재합니다.\n지금 다운로드하여 설치하시겠습니까?\n\n(설치 중 프로그램이 자동으로 종료 및 재시작됩니다.)", 
                        "업데이트 알림", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        var assets = root.GetProperty("assets");
                        string downloadUrl = "";
                        foreach (var asset in assets.EnumerateArray())
                        {
                            string fileName = asset.GetProperty("name").GetString();
                            if (fileName.EndsWith(".exe"))
                            {
                                downloadUrl = asset.GetProperty("browser_download_url").GetString();
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(downloadUrl))
                        {
                            // 1. 업데이트 전용 팝업 창 즉석 생성
                            Form updateForm = new Form
                            {
                                Text = "소프트웨어 업데이트",
                                Size = new Size(400, 180),
                                StartPosition = FormStartPosition.CenterParent,
                                FormBorderStyle = FormBorderStyle.FixedDialog,
                                MaximizeBox = false,
                                MinimizeBox = false,
                                BackColor = Color.White
                            };

                            Label lblStatus = new Label
                            {
                                Text = $"신규 버전(v{latestVersion})을 준비 중입니다...",
                                Location = new Point(20, 25),
                                Size = new Size(360, 20),
                                Font = new Font("Segoe UI", 10, FontStyle.Bold)
                            };

                            ProgressBar pbUpdate = new ProgressBar
                            {
                                Location = new Point(20, 60),
                                Size = new Size(345, 25),
                                Style = ProgressBarStyle.Continuous
                            };

                            Label lblPercent = new Label
                            {
                                Text = "기다려 주세요... 0%",
                                Location = new Point(20, 95),
                                Size = new Size(360, 20),
                                ForeColor = Color.Gray
                            };

                            updateForm.Controls.AddRange(new Control[] { lblStatus, pbUpdate, lblPercent });
                            updateForm.Show(); // 창 띄우기
                            updateForm.Refresh();

                            string tempFile = Path.Combine(Path.GetTempPath(), $"MMT_Setup_v{latestVersion}.exe");
                            
                            try
                            {
                                using (var downloadResponse = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                                {
                                    downloadResponse.EnsureSuccessStatusCode();
                                    var totalBytes = downloadResponse.Content.Headers.ContentLength ?? -1L;
                                    
                                    using (var contentStream = await downloadResponse.Content.ReadAsStreamAsync())
                                    using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                                    {
                                        var buffer = new byte[8192];
                                        var totalRead = 0L;
                                        int read;
                                        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                        {
                                            await fileStream.WriteAsync(buffer.AsMemory(0, read));
                                            totalRead += read;
                                            
                                            if (totalBytes != -1)
                                            {
                                                int progress = (int)((totalRead * 100) / totalBytes);
                                                
                                                // UI 스레드에서 안전하게 업데이트
                                                this.Invoke((MethodInvoker)delegate {
                                                    pbUpdate.Value = progress;
                                                    lblPercent.Text = $"다운로드 중... {progress}% ({totalRead / 1024 / 1024}MB / {totalBytes / 1024 / 1024}MB)";
                                                    updateForm.Refresh();
                                                });
                                            }
                                        }
                                    }
                                }

                                updateForm.Close(); // 다운로드 완료 시 자동 닫기

                                MessageBox.Show("업데이트 준비가 완료되었습니다.\n확인을 누르면 설치를 시작하고 최신 버전으로 다시 시작합니다.", 
                                    "준비 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                var startInfo = new ProcessStartInfo(tempFile)
                                {
                                    Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-",
                                    UseShellExecute = true,
                                    Verb = "runas"
                                };
                                
                                Process.Start(startInfo);
                                Application.Exit();
                            }
                            catch (Exception ex)
                            {
                                updateForm.Close();
                                MessageBox.Show($"다운로드 중 오류가 발생했습니다: {ex.Message}", "업데이트 실패");
                            }
                        }
                    }
                }
                else if (manual)
                {
                    MessageBox.Show("현재 최신 버전을 사용 중입니다.", "업데이트 확인");
                }
            }
        }
        catch (Exception ex)
        {
            if (manual) MessageBox.Show($"업데이트 확인 중 오류가 발생했습니다: {ex.Message}", "오류");
        }
    }

    // ============================================
    // HELPER CLASSES AND METHODS
    // ============================================

    private async Task RunFFmpegWithProgress(string args, string inputFile, ProgressBar pb, Label lbl, CancellationToken token)
    {
        double totalMilliseconds = 0;

        try {
            var procD = new Process();
            procD.StartInfo.FileName = "ffprobe.exe";
            procD.StartInfo.Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{inputFile}\"";
            procD.StartInfo.UseShellExecute = false;
            procD.StartInfo.CreateNoWindow = true;
            procD.StartInfo.RedirectStandardOutput = true;
            procD.Start();
            string durationStr = await procD.StandardOutput.ReadToEndAsync();
            if (double.TryParse(durationStr.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d)) {
                totalMilliseconds = d * 1000;
            }
        } catch { }

        var proc = new Process();
        proc.StartInfo.FileName = "ffmpeg.exe";
        proc.StartInfo.Arguments = args;
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.CreateNoWindow = true;
        proc.StartInfo.RedirectStandardError = true;
        
        proc.Start();
        CleanupManager.RegisterProcess(proc);

        var regex = new System.Text.RegularExpressions.Regex(@"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})");
        long lastUpdate = 0; // Performance throttle

        using (token.Register(() => { try { if (!proc.HasExited) proc.Kill(true); } catch { } }))
        {
            try {
                while (true)
                {
                    if (token.IsCancellationRequested) break;
                    string? line = await proc.StandardError.ReadLineAsync();
                    if (line == null) break;

                    if (totalMilliseconds > 0)
                    {
                        var match = regex.Match(line);
                        if (match.Success)
                        {
                            double h = double.Parse(match.Groups[1].Value);
                            double m = double.Parse(match.Groups[2].Value);
                            double s = double.Parse(match.Groups[3].Value);
                            double ms = double.Parse(match.Groups[4].Value) * 10;
                            double currentMs = (h * 3600 + m * 60 + s) * 1000 + ms;

                            int pct = (int)((currentMs / totalMilliseconds) * 100);
                            if (pct > 100) pct = 100;
                            if (pct < 0) pct = 0;

                            // Throttle UI updates to once every 100ms
                            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            if (now - lastUpdate > 100 || pct == 100)
                            {
                                lastUpdate = now;
                                if (!this.IsDisposed && !this.Disposing)
                                {
                                    try {
                                        this.Invoke((MethodInvoker)delegate {
                                            pb.Value = pct;
                                            lbl.Text = $"변환 중... {pct}%";
                                        });
                                    } catch { }
                                }
                            }
                        }
                    }
                }
            } finally {
                if (!proc.HasExited) {
                    try { proc.Kill(true); } catch { }
                }
                await proc.WaitForExitAsync();
                CleanupManager.UnregisterProcess(proc);
            }
            token.ThrowIfCancellationRequested();
        }
        
        if (!this.IsDisposed && !this.Disposing)
        {
            this.Invoke((MethodInvoker)delegate {
                pb.Value = 100;
                lbl.Text = "변환 완료!";
                pb.Update();
                lbl.Update();
            });
        }
    }

    public static string MakeValidFileName(string name)
    {
        string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
        string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}])", invalidChars);
        return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
    }

    private void LblVideoTitle_DoubleClick(object sender, EventArgs e)
    {
        if (_currentVideo == null) return;
        
        txtEditTitle.Text = _customTitle;
        txtEditTitle.Visible = true;
        lblVideoTitle.Visible = false;
        txtEditTitle.Focus();
        txtEditTitle.SelectAll();
    }

    private void TxtEditTitle_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            FinishEditingTitle();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            txtEditTitle.Visible = false;
            lblVideoTitle.Visible = true;
        }
    }

    private void TxtEditTitle_LostFocus(object sender, EventArgs e)
    {
        if (txtEditTitle.Visible) FinishEditingTitle();
    }

    private void FinishEditingTitle()
    {
        _customTitle = txtEditTitle.Text.Trim();
        if (string.IsNullOrEmpty(_customTitle) && _currentVideo != null) _customTitle = _currentVideo.Title;
        
        txtEditTitle.Visible = false;
        lblVideoTitle.Visible = true;
        UpdateVideoInfoDisplay();
    }

    private async Task EnsureFFmpegAsync()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string ffmpegPath = Path.Combine(baseDir, "ffmpeg.exe");
        string ffprobePath = Path.Combine(baseDir, "ffprobe.exe");

        if (File.Exists(ffmpegPath) && File.Exists(ffprobePath)) return;

        // Fallback: Check project root
        string projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
        foreach (string file in new[] { "ffmpeg.exe", "ffprobe.exe" })
        {
            string src = Path.Combine(projectRoot, file);
            string dest = Path.Combine(baseDir, file);
            if (File.Exists(src) && !File.Exists(dest))
            {
                try { File.Copy(src, dest, true); } catch { }
            }
        }

        if (File.Exists(ffmpegPath) && File.Exists(ffprobePath)) return;

        try
        {
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"FFmpeg 다운로드 중 오류가 발생했습니다: {ex.Message}\n프로그램을 다시 실행하거나 수동으로 설치해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task EnsureYtDlpAsync(CancellationToken token = default)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string ytDlpPath = Path.Combine(baseDir, "yt-dlp.exe");
        
        if (File.Exists(ytDlpPath)) return;

        // Fallback: Check project root (useful during development/dotnet run)
        // Usually 3 levels up from bin/Debug/net10.0-windows
        string projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
        string rootYtDlp = Path.Combine(projectRoot, "yt-dlp.exe");

        if (File.Exists(rootYtDlp))
        {
            try {
                File.Copy(rootYtDlp, ytDlpPath, true);
                lblYtDlpStatus.Text = "yt-dlp를 실행 폴더로 복사했습니다.";
                if (tglXPrivateMode.Checked) lblXStatus.Text = "yt-dlp를 실행 폴더로 복사했습니다.";
                return;
            } catch { /* ignored, will try download */ }
        }

        try
        {
            lblYtDlpStatus.Text = "yt-dlp가 없습니다. 다운로드 중 (15MB)...";
            if (tglXPrivateMode.Checked) lblXStatus.Text = "yt-dlp가 없습니다. 다운로드 중 (15MB)...";
            using var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "YoutubeDownloader-Antigravity");
            // CancellationToken 반영
            var response = await client.GetAsync("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe", token);
            response.EnsureSuccessStatusCode();
            var bytes = await response.Content.ReadAsByteArrayAsync(token);
            await File.WriteAllBytesAsync(ytDlpPath, bytes, token);
            lblYtDlpStatus.Text = "yt-dlp 다운로드 완료.";
            if (tglXPrivateMode.Checked) lblXStatus.Text = "yt-dlp 다운로드 완료.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"yt-dlp 다운로드 중 오류가 발생했습니다: {ex.Message}\n\n직접 설치하시려면 아래 파일을 다운로드하여 실행 파일(.exe)과 같은 폴더에 넣어주세요:\nhttps://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void UpdateVideoInfoDisplay()
    {
        if (_currentVideo == null) return;
        lblVideoTitle.Text = $"{_customTitle}\n채널: {_currentVideo.Author.ChannelTitle}\n길이: {_currentVideo.Duration}";
    }

    private async Task PreInitializeWebView2Async()
    {
        try
        {
            if (webViewX.CoreWebView2 != null) return;

            string webViewDataPath = Path.Combine(SettingsManager.UserDataFolder, "WebView2_Cache");
            if (!Directory.Exists(webViewDataPath)) Directory.CreateDirectory(webViewDataPath);

            var env = await CoreWebView2Environment.CreateAsync(null, webViewDataPath);
            await webViewX.EnsureCoreWebView2Async(env);
            
            if (webViewX.CoreWebView2 != null)
            {
                webViewX.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";
                webViewX.CoreWebView2.Settings.AreDevToolsEnabled = false;
            }
        }
        catch { /* 비공개 모드 진입 시 다시 시도하므로 여기서는 무시 */ }
    }

    private async void TglXPrivateMode_CheckedChanged(object sender, EventArgs e)
    {
        if (tglXPrivateMode.Checked)
        {
            // 현재 크기 저장
            _lastWidth = this.Width;
            _lastHeight = this.Height;

            this.MinimumSize = new Size(1100, 750);
            if (this.Width < 1100) this.Width = 1100;
            if (this.Height < 750) this.Height = 750;

            panelXBrowser.Parent = this; 
            panelXBrowser.BringToFront();
            panelXBrowser.Visible = true;
            panelXBrowser.Dock = DockStyle.Fill;
            webViewX.Dock = DockStyle.Fill;
            
            // 이미 초기화되어 있다면 바로 이동 (초고속)
            if (webViewX.CoreWebView2 != null) 
            {
                webViewX.CoreWebView2.Stop();
                webViewX.CoreWebView2.Navigate("https://x.com/login");
                return;
            }

            // 만약 예열이 안 됐을 경우에만 여기서 초기화
            await PreInitializeWebView2Async();
            
            if (webViewX.CoreWebView2 != null)
            {
                webViewX.CoreWebView2.Navigate("https://x.com/login");
            }
            
            this.PerformLayout();
            this.Refresh();
        }
        else
        {
            if (webViewX.CoreWebView2 != null)
            {
                webViewX.CoreWebView2.Stop(); // 꺼질 때 통신 즉시 중단
            }

            // 비공개 모드 해제 시 UI 및 최소 크기 복원
            this.MinimumSize = new Size(800, 600);
            panelXBrowser.Visible = false;
            panelXBrowser.Parent = tabYtDlp; // 부모를 다시 탭 내부로 복원
            panelXBrowser.Dock = DockStyle.Fill;
            webViewX.Dock = DockStyle.None; // 원래 스타일로 복구
            webViewX.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            
            // 크기 원복
            if (_lastWidth > 0 && _lastHeight > 0)
            {
                this.Width = _lastWidth;
                this.Height = _lastHeight;
            }
            
            lblYtDlpStatus.Text = "비공개 모드 종료";
            this.PerformLayout();
            this.Refresh();
        }
    }

    private void BtnXCapture_Click(object sender, EventArgs e)
    {
        string currentUrl = webViewX.Source.ToString();
        string lowerUrl = currentUrl.ToLower();
        
        // x.com 이나 twitter.com 의 status 페이지인지 확인 (조금 더 유연하게)
        bool isTweetPage = lowerUrl.Contains("x.com") || lowerUrl.Contains("twitter.com");

        if (isTweetPage)
        {
            txtYtDlpUrl.Text = currentUrl;
            lblYtDlpStatus.Text = "영상 주소 인식 완료: " + currentUrl;
            // 이제 바로 닫지 않고 사용자에게 선택권을 줌
            MessageBox.Show("영상을 포착했습니다! 이제 '바로 다운' 버튼을 눌러 다운 받아주세요.", "알림");
        }
        else
        {
            MessageBox.Show($"현재 페이지: {webViewX.Source}\n\n영상이 있는 트윗 본문 페이지로 이동한 후 눌러주세요.", "알림");
        }
    }

    private async void BtnXDownload_Click(object sender, EventArgs e)
    {
        string currentUrl = webViewX.Source.ToString();
        txtYtDlpUrl.Text = currentUrl;
        
        // 바로 다운로드 시작 로직 호출
        BtnYtDlpRun_Click(null, null);
    }

    private void BtnXClose_Click(object sender, EventArgs e)
    {
        tglXPrivateMode.Checked = false; // CheckedChanged에서 panelXBrowser.Visible = false 처리됨
    }

    private async Task<string> ExportWebViewCookiesAsync()
    {
        if (webViewX.CoreWebView2 == null) return "";
        
        var cookieManager = webViewX.CoreWebView2.CookieManager;
        // 모든 쿠키를 가져와서 x.com 및 twitter.com 관련 쿠키만 필터링
        var cookies = await cookieManager.GetCookiesAsync(null); 
        
        if (cookies == null || cookies.Count == 0) return "";

        string cookiePath = Path.Combine(SettingsManager.UserDataFolder, "temp_x_cookies.txt");
        int count = 0;
        using (var sw = new StreamWriter(cookiePath, false, new System.Text.UTF8Encoding(false)))
        {
            sw.WriteLine("# Netscape HTTP Cookie File");
            sw.WriteLine("# This file is generated by YoutubeDownloader");
            
            foreach (var c in cookies)
            {
                // x.com 또는 twitter.com 관련 쿠키만 포함
                if (!c.Domain.Contains("x.com") && !c.Domain.Contains("twitter.com")) continue;

                count++;
                string domain = c.Domain;
                string flag = domain.StartsWith(".") ? "TRUE" : "FALSE";
                string secure = c.IsSecure ? "TRUE" : "FALSE";
                
                long expires = 0;
                try {
                    if (c.Expires == default(DateTime) || c.Expires.Year > 2050) {
                        expires = 2147483647; // Session or far future
                    } else {
                        expires = new DateTimeOffset(c.Expires).ToUnixTimeSeconds();
                    }
                } catch { expires = 2147483647; }
                
                sw.WriteLine($"{domain}\t{flag}\t{c.Path}\t{secure}\t{expires}\t{c.Name}\t{c.Value}");
            }
        }
        
        return count > 0 ? cookiePath : "";
    }

    private class DownloadJob
    {
        public string Id { get; set; } = "";
        public Video Video { get; set; } = null!;
        public StreamManifest Manifest { get; set; } = null!;
        public QualityOption Option { get; set; } = null!;
        public string OutputPath { get; set; } = "";
        public ListViewItem ListViewItem { get; set; } = null!;
        public CancellationTokenSource JobCts { get; set; } = null!;
        public string CustomFileName { get; set; } = "";
    }

    private void MenuTrayOpen_Click(object sender, EventArgs e)
    {
        this.Show();
        this.WindowState = FormWindowState.Normal;
        this.Activate();
    }

    private void MenuTrayExit_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }

    private void LblYtDlpSavePath_Click(object sender, EventArgs e)
    {
        string path = SettingsManager.Settings?.DefaultDownloadFolder ?? "";
        if (string.IsNullOrEmpty(path)) path = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        
        if (Directory.Exists(path))
        {
            try { Process.Start("explorer.exe", path); } catch { }
        }
        else
        {
            MessageBox.Show("폴더가 존재하지 않습니다: " + path);
        }
    }

    private class QualityOption
    {
        public string Title { get; set; }
        public string Id { get; set; }
        public bool IsVideo { get; set; }

        public QualityOption(string title, string id, bool isVideo)
        {
            Title = title;
            Id = id;
            IsVideo = isVideo;
        }
        public override string ToString() => Title;
    }
}
