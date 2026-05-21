using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using YoutubeExplode;
using YoutubeExplode.Videos.ClosedCaptions;
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
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing.Drawing2D;
using System.ComponentModel;

 
namespace YoutubeDownloader;

public partial class Form1 : Form
{
    private YoutubeClient _youtube;
    private StreamManifest _streamManifest;
    private Video _currentVideo;
    private string _customTitle = "";
    private string _currentUrl = "";
    
    // Download Queue
    private ConcurrentQueue<DownloadJob> _downloadQueue;
    private List<DownloadJob> _activeJobs;
    private bool _isDownloading = false;
    private ConcurrentQueue<YtDlpDownloadJob> _ytDlpDownloadQueue;
    private List<YtDlpDownloadJob> _activeYtDlpJobs;
    private YtDlpDownloadJob? _currentYtDlpQueueJob;
    private bool _isYtDlpQueueRunning = false;
    private bool _isInternalYtDlpRun = false;
    private readonly object _ytDlpQueueLock = new object();
    private readonly SemaphoreSlim _ytDlpBrowserSemaphore = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim _ytDlpInstallSemaphore = new SemaphoreSlim(1, 1);
    private const int MaxYtDlpParallelDownloads = 3;
    private const string YtDlpStartButtonText = "다운로드";
    private const string YtDlpQueueButtonText = "대기열에 추가";
    
    // GitHub Update Settings 
    private const string GITHUB_USER = "SugarRush1231";
    private const string REPO_NAME = "Multi_Media_Toolkit";
    private const string VersionArchiveUrl = "https://mmt-web.sgr970318.workers.dev/";
    private static readonly HttpClient _httpClient = new HttpClient();
    private ToolTip? _loginDownloadToolTip;
    private CheckBox? chkKeepLoginSession;
    private CheckBox? chkUseSiteFolderRules;
    private CheckBox? chkUseCustomSiteFolders;
    private ComboBox? cmbSiteFolderSite;
    private TextBox? txtSiteFolderOverride;
    private RoundButton? btnBrowseSiteFolderOverride;
    private ComboBox? cmbFileNamePreset;
    private TextBox? txtCustomFileNameTemplate;
    private ComboBox? cmbDefaultVideoQuality;
    private ComboBox? cmbYoutubeSubtitleLanguage;
    private ComboBox? cmbYtDlpSubtitleLanguage;
    private CheckBox? chkEnableWidgetMode;
    private RoundButton? btnOpenVersionArchive;
    private Label? lblTopWidgetMode;
    private WidgetModeToggleButton? btnTopWidgetMode;
    private DownloadWidgetForm? _downloadWidgetForm;
    private TabPage? tabVideoPicker;
    private Label? lblVideoPickerTitle;
    private Label? lblVideoPickerHint;
    private ListView? lvVideoPicker;
    private ImageList? imgVideoPickerThumbs;
    private RoundButton? btnVideoPickerDownloadAll;
    private RoundButton? btnVideoPickerDownloadSelected;
    private RoundButton? btnVideoPickerCancel;
    private List<DetectedVideoItem> _detectedVideoItems = new();
    private bool _returnToWidgetAfterVideoPick;
    private bool _updatingWidgetModeCheckbox;
    private IntPtr _lastWidgetTargetWindow = IntPtr.Zero;
    private static readonly HashSet<string> WidgetBrowserProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome", "msedge", "firefox", "brave", "whale", "opera", "opera_gx", "vivaldi", "iexplore"
    };
    private bool _loginKeepNoticeShown;
    private bool _initializingKeepLoginCheckbox;
    private bool _ytDlpForceUpdateChecked;
    
    // Conversion Cancellation
    private CancellationTokenSource? _webmCts;
    private CancellationTokenSource? _codecCts;
    private CancellationTokenSource? _audioCts;
    private CancellationTokenSource? _ytDlpCts;
    private int _lastWidth = 800;
    private int _lastHeight = 600;
    private const string CURR_VERSION = "1.3.0";

    // [Twitter/X Private Extraction] Captured Data
    private string _capturedM3u8Url = "";
    private string _capturedAuthToken = "";
    private string _capturedCsrfToken = "";
    private string _capturedUserAgent = "";

    // X Private Mode UI Controls
    private Panel? panelXTopBar;
    private Panel? panelXBottomBar;
    private TableLayoutPanel? tableLayoutX;
    private Label? lblXGuide;
    private FlowLayoutPanel? panelLoginSites;
    private Panel? panelLoginFolderOverlay;
    private Panel? panelLoginFolder;
    private FlowLayoutPanel? panelLoginFolderApps;
    private RoundButton? btnLoginFolderClose;
    private Label? lblLoginFolderTitle;
    private Label? lblLoginFolderHint;
    private RoundButton? btnLoginBrowserBack;
    private TextBox? txtLoginBrowserAddress;
    private RoundButton? btnLoginBrowserGo;
    private System.Windows.Forms.Timer? _loginFolderAnimationTimer;
    private bool _loginFolderOpening;
    private int _loginFolderAnimationFrame;
    private const int LoginFolderAnimationFrames = 10;
    private static readonly Size LoginFolderStartSize = new Size(220, 160);
    private static readonly Size LoginFolderTargetSize = new Size(470, 365);
    private bool _isLoginBrowserMode = false;
    private string _loginBrowserDownloadTitle = "";
    private bool _webViewUsingXProfile = false;
    private static readonly string XWebViewDataFolder = Path.Combine(SettingsManager.UserDataFolder, "BrowserData_X");
    private static readonly HashSet<string> VideoInputExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm", ".mkv", ".mov", ".avi", ".flv", ".m4v", ".wmv", ".ts", ".mts", ".m2ts", ".mpeg", ".mpg"
    };
    private static readonly HashSet<string> AudioInputExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".m4a", ".aac", ".ogg", ".flac", ".wma", ".opus"
    };
    private static readonly HashSet<string> AudioConverterInputExtensions = new(VideoInputExtensions.Concat(AudioInputExtensions), StringComparer.OrdinalIgnoreCase);
    
    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);
    [DllImport("user32.dll")]
    private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);
    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, MessageBoxHookProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    private delegate IntPtr MessageBoxHookProc(int nCode, IntPtr wParam, IntPtr lParam);
    private const int SB_HORZ = 0;
    private const int WH_CBT = 5;
    private const int HCBT_ACTIVATE = 5;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private static readonly IntPtr HWND_TOP = IntPtr.Zero;
    private static IntPtr _messageBoxHook = IntPtr.Zero;
    private static Rectangle _messageBoxOwnerBounds;
    private static MessageBoxHookProc? _messageBoxHookProc;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public Form1()
    {
        // 
        try { SetCurrentProcessExplicitAppUserModelID("Multi Media Toolkit"); } catch { }

        InitializeComponent();
        SettingsManager.Load();
        if (SettingsManager.Settings.EnableWidgetMode)
        {
            SettingsManager.Settings.EnableWidgetMode = false;
            SettingsManager.Save();
        }
        ShowInTaskbar = true;
        this.DoubleBuffered = true;
        LoadAppIcon();
        this.notifyIconApp.Text = "Multi Media Toolkit";
        
        // Program Files)
        Task.Run(() => CleanupOldInstallation());

        _youtube = new YoutubeClient();
        _downloadQueue = new ConcurrentQueue<DownloadJob>();
        _activeJobs = new List<DownloadJob>();
        _ytDlpDownloadQueue = new ConcurrentQueue<YtDlpDownloadJob>();
        _activeYtDlpJobs = new List<YtDlpDownloadJob>();

        // 
        lblYtDlpSavePath.SizeChanged += (s, e) => {
            btnOpenYtDlpFolder.Left = lblYtDlpSavePath.Right + 5;
            btnOpenYtDlpFolder.Top = lblYtDlpSavePath.Top + (lblYtDlpSavePath.Height - btnOpenYtDlpFolder.Height) / 2;
        };
        
        _ = SendHeartbeatReportAsync("App Launched");
        _ = StartHeartbeatLoopAsync();
        lblYoutubeSavePath.SizeChanged += (s, e) => {
            btnOpenYoutubeFolder.Left = lblYoutubeSavePath.Right + 5;
            btnOpenYoutubeFolder.Top = lblYoutubeSavePath.Top + (lblYoutubeSavePath.Height - btnOpenYoutubeFolder.Height) / 2;
        };
        lblCodecSavePath.SizeChanged += (s, e) => {
            btnOpenCodecFolder.Left = lblCodecSavePath.Right + 5;
            btnOpenCodecFolder.Top = lblCodecSavePath.Top + (lblCodecSavePath.Height - btnOpenCodecFolder.Height) / 2;
        };
        lblWebMSavePath.SizeChanged += (s, e) => {
            btnOpenWebMFolder.Left = lblWebMSavePath.Right + 5;
            btnOpenWebMFolder.Top = lblWebMSavePath.Top + (lblWebMSavePath.Height - btnOpenWebMFolder.Height) / 2;
        };
        lblAudioSavePath.SizeChanged += (s, e) => {
            btnOpenAudioFolder.Left = lblAudioSavePath.Right + 5;
            btnOpenAudioFolder.Top = lblAudioSavePath.Top + (lblAudioSavePath.Height - btnOpenAudioFolder.Height) / 2;
        };

        
        string initialPath = SettingsManager.Settings?.DefaultDownloadFolder ?? "";
        if (string.IsNullOrEmpty(initialPath)) initialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        SetSavePathLabels(initialPath);
        txtDownloadFolder.Text = SettingsManager.Settings?.DefaultDownloadFolder ?? "";
        miniEditorControl.UpdateSavePath(initialPath);
        ConfigureFileDropTargets();
        ConfigureYtDlpQueueColumns();
        HideLegacyPrivateModeToggles();
        ConfigureMiniEditorVisibility();
        ConfigureLoginDownloadHelp();
        ConfigureDownloadRuleSettings();
        ConfigureWidgetModeSettings();
        NormalizeKoreanUiText(initialPath);
        ConfigureVersionArchiveButton();
        ConfigureTopWidgetModeButton();
        ConfigureVideoPickerTab();

        // Ensure initially correct positions
        btnOpenYtDlpFolder.Left = lblYtDlpSavePath.Right + 5;
        btnOpenYtDlpFolder.Top = lblYtDlpSavePath.Top + (lblYtDlpSavePath.Height - btnOpenYtDlpFolder.Height) / 2;
        btnOpenYoutubeFolder.Left = lblYoutubeSavePath.Right + 5;
        btnOpenYoutubeFolder.Top = lblYoutubeSavePath.Top + (lblYoutubeSavePath.Height - btnOpenYoutubeFolder.Height) / 2;
        btnOpenCodecFolder.Left = lblCodecSavePath.Right + 5;
        btnOpenCodecFolder.Top = lblCodecSavePath.Top + (lblCodecSavePath.Height - btnOpenCodecFolder.Height) / 2;
        btnOpenWebMFolder.Left = lblWebMSavePath.Right + 5;
        btnOpenWebMFolder.Top = lblWebMSavePath.Top + (lblWebMSavePath.Height - btnOpenWebMFolder.Height) / 2;
        btnOpenAudioFolder.Left = lblAudioSavePath.Right + 5;
        btnOpenAudioFolder.Top = lblAudioSavePath.Top + (lblAudioSavePath.Height - btnOpenAudioFolder.Height) / 2;

        // [UI] 
        btnTabSettings.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        btnTabMiniEdit.Anchor = AnchorStyles.Top | AnchorStyles.Left;

        // Enable DoubleBuffering for ListView and Panel to prevent flickering
        var doubleBufferProp = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        doubleBufferProp?.SetValue(lvQueue, true, null);
        doubleBufferProp?.SetValue(lvYtDlpQueue, true, null);
        doubleBufferProp?.SetValue(panelMain, true, null);


        doubleBufferProp?.SetValue(tabControlMain, true, null);
        doubleBufferProp?.SetValue(tabYoutube, true, null);
        doubleBufferProp?.SetValue(tabYtDlp, true, null);

        AppDomain.CurrentDomain.ProcessExit += (s, e) => CleanupManager.FullSystemCleanup(SettingsManager.Settings.KeepLoginSession);
        AppDomain.CurrentDomain.UnhandledException += (s, e) => CleanupManager.FullSystemCleanup(SettingsManager.Settings.KeepLoginSession);

        this.Text = $"Multi Media Toolkit (v{CURR_VERSION})";
        lblAbout.Text = $"Multi Media Toolkit v{CURR_VERSION}\r\nCreated by 김병석\r\n(c) {DateTime.Now.Year} all rights reserved.\r\n(kbs318@naver.com)";

        lblAbout.Click += async (s, e) => {
            _secretClickCount++;
            if (_secretClickCount >= 10)
            {
                _secretClickCount = 0;
                
                // UX
                string originalText = lblAbout.Text;
                lblAbout.Text = "현황 보고 중...";
                bool success = await SendHeartbeatReportAsync("\uC218\uB3D9 \uD604\uD669 \uD655\uC778");
                
                if (success) 
                {
                    ShowCenteredMessage("현재 전체 가동 현황 보고가 완료되었습니다.", "관리자 보고", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    ShowCenteredMessage("보고 전송에 실패했습니다. 네트워크를 확인해 주세요.", "관리자 보고", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                lblAbout.Text = originalText;
            }
        };

        // 
        if (SettingsManager.Settings.LastSeenVersion != CURR_VERSION)
        {

            if (string.IsNullOrEmpty(SettingsManager.Settings.LastSeenVersion))
            {

                SettingsManager.Settings.LastSeenVersion = CURR_VERSION;
                SettingsManager.Save();
            }
            else
            {
                SettingsManager.Settings.LastSeenVersion = CURR_VERSION;
                SettingsManager.Save();

                _ = Task.Run(async () => {

                    string changelog = await GetServerChangelogAsync();
                    
                    this.Invoke((MethodInvoker)delegate {
                        string title = $"Multi Media Toolkit [v{CURR_VERSION}]";
                        ShowCenteredMessage(changelog, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        // 버전 공지 확인 후 다음 실행부터 숨김
                    });
                });
            }
        }
        
        CleanupOldInstallation();
    }

    private void SetSavePathLabels(string path)
    {
        string text = "현재 저장 위치: " + path;
        lblYtDlpSavePath.Text = text;
        lblYoutubeSavePath.Text = text;
        lblCodecSavePath.Text = text;
        lblWebMSavePath.Text = text;
        lblAudioSavePath.Text = text;
    }

    private void NormalizeKoreanUiText(string savePath)
    {
        btnTabYoutube.Text = "유튜브 다운로더";
        btnTabYtDlp.Text = "웹 사이트 영상 다운";
        btnTabCodec.Text = "Pr/AE 코덱 해결";
        btnTabWebM.Text = "포맷 변환기";
        btnTabAudio.Text = "오디오 변환기";
        btnTabMiniEdit.Text = "미니 편집기";
        btnTabSettings.Text = "설정";

        lblUrl.Text = "유튜브 URL 입력";
        btnLoad.Text = "영상 확인";
        lblVideoTitle.Text = "위 입력칸에 유튜브 URL을 붙여넣고 영상 확인을 클릭하세요.";
        lblQuality.Text = "화질 옵션";
        chkYoutubeDownloadSubtitles.Text = "자막도 함께 다운로드";
        btnAddQueue.Text = "다운로드";
        colTitle.Text = "영상 제목";
        colQuality.Text = "화질/포맷";
        colStatus.Text = "상태";
        btnRemoveSelected.Text = "선택 취소";
        menuRemoveSelected.Text = "선택 항목 취소";
        lblStatus.Text = "* 대기열 항목 우클릭으로도 취소가 가능합니다.";
        btnOpenYoutubeFolder.Text = "폴더 열기";

        lblYtDlpTitle.Text = "웹 사이트 영상 다운로드";
        lblYtDlpDesc.Text = "치지직, Instagram, SOOP, Pinterest, X, Vimeo, Anilife, Linkkf 등 다양한 사이트를 지원합니다.\n로그인이 필요한 일부공개/나이제한 영상은 로그인 후 좌측 상단 즉시 다운로드 또는 URL 입력으로 받을 수 있습니다.";
        txtYtDlpUrl.PlaceholderText = "다운로드할 영상의 URL을 입력하세요...";
        chkYtDlpDownloadSubtitles.Text = "자막도 함께 다운로드";
        btnYtDlpRun.Text = _isYtDlpQueueRunning ? YtDlpQueueButtonText : YtDlpStartButtonText;
        btnYtDlpCancel.Text = "중단";
        lblYtDlpStatus.Text = "대기 중...";
        colYtDlpUrl.Text = "URL";
        colYtDlpSubtitles.Text = "자막";
        colYtDlpStatus.Text = "상태";
        btnRemoveSelectedYtDlp.Text = "선택 취소";
        menuYtDlpRemoveSelected.Text = "선택 항목 취소";
        btnYtDlpLoginBrowser.Text = "로그인 후 다운";
        btnOpenYtDlpFolder.Text = "폴더 열기";
        lblXPrivateMode.Text = "X 비공개 모드";
        lblInstaPrivateMode.Text = "Instagram 로그인";
        btnXCapture.Text = "즉시 다운로드";
        btnXDownload.Text = "다운로드";
        btnXClose.Text = "닫기";
        lblXStatus.Text = "대기 중...";
        if (chkKeepLoginSession != null) chkKeepLoginSession.Text = "로그인 유지";

        lblWebMTitle.Text = "포맷 변환기";
        txtWebMInput.PlaceholderText = "영상 파일 경로를 선택하세요.";
        txtWebMOutput.PlaceholderText = "저장 위치 (기본: 원본 폴더)...";
        btnBrowseWebM.Text = "파일 선택";
        btnBrowseWebMOutput.Text = "위치 설정";
        btnConvertWebM.Text = "포맷 변환 시작";
        btnCancelWebM.Text = "취소";
        lblWebMStatus.Text = "대기 중...";
        btnOpenWebMFolder.Text = "폴더 열기";

        lblCodecTitle.Text = "Pr/AE 코덱 해결";
        lblCodecDesc.Text = "Premiere Pro 또는 After Effects에서 영상만 나오고 소리가 안 나오는 문제를 해결합니다.\nH.264 / AAC 코덱으로 다시 인코딩합니다.";
        txtCodecInput.PlaceholderText = "영상 파일 경로를 선택하세요.";
        txtCodecOutput.PlaceholderText = "저장 위치 (기본: 원본 폴더)...";
        btnBrowseCodec.Text = "파일 선택";
        btnBrowseCodecOutput.Text = "위치 설정";
        btnConvertCodec.Text = "코덱 문제 해결 시작";
        btnCancelCodec.Text = "취소";
        lblCodecStatus.Text = "대기 중...";
        btnOpenCodecFolder.Text = "폴더 열기";

        lblAudioTitle.Text = "오디오 변환기";
        txtAudioInput.PlaceholderText = "영상이나 mp3 파일 경로를 선택하세요.";
        txtAudioOutput.PlaceholderText = "저장 위치 (기본: 원본 폴더)...";
        btnBrowseAudio.Text = "파일 선택";
        btnBrowseAudioOutput.Text = "위치 설정";
        btnConvertAudio.Text = "오디오 추출/변환 시작";
        btnCancelAudio.Text = "취소";
        lblAudioStatus.Text = "대기 중...";
        btnOpenAudioFolder.Text = "폴더 열기";

        lblSettingsTitle.Text = "프로그램 설정";
        chkShowNotifications.Text = "알림 표시 (다운로드 완료/실패)";
        chkAutoOpenFolder.Text = "다운로드 완료 시 저장 폴더 열기";
        chkAutoUpdateCheck.Text = "시작 시 자동으로 업데이트 확인";
        lblDownloadFolder.Text = "기본 저장 경로";
        btnBrowseFolder.Text = "경로 변경";
        btnSaveSettings.Text = "설정 저장";
        btnCheckUpdate.Text = "새 버전 업데이트 확인";
        lblAbout.Text = $"Multi Media Toolkit v{CURR_VERSION}\r\nCreated by 김병석\r\n(c) {DateTime.Now.Year} all rights reserved.\r\n(kbs318@naver.com)";

        SetSavePathLabels(savePath);
    }

    private async Task<string> GetServerChangelogAsync()
    {
        try
        {

            string changelogUrl = "https://raw.githubusercontent.com/SugarRush1231/Multi_Media_Toolkit/main/changelog.txt";
            
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync(changelogUrl);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
        } catch { }

        try
        {
            string localChangelogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "changelog.txt");
            if (File.Exists(localChangelogPath))
            {
                return await File.ReadAllTextAsync(localChangelogPath);
            }
        } catch { }

        return $"Multi Media Toolkit v{CURR_VERSION} 업데이트가 완료되었습니다.";
    }

    private void CleanupOldInstallation()
    {
        try
        {
            // (Program Files (x86)  Program Files)
            string[] oldPaths = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Multi Media Toolkit"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Multi Media Toolkit")
            };

            string currentDir = AppDomain.CurrentDomain.BaseDirectory.ToLower();

            foreach (var path in oldPaths)
            {
                if (Directory.Exists(path) && !currentDir.StartsWith(path.ToLower()))
                {

                    Directory.Delete(path, true);
                }
            }
        }
        catch {  }
    }

    private int _secretClickCount = 0;
    
    private void Form1_Load(object sender, EventArgs e)
    {
        // Load Settings into UI
        ReloadSettingsUI();

        // Populate other tabs with the default path initially if it exists
        string defaultDir = SettingsManager.Settings.DefaultDownloadFolder;
        if (!string.IsNullOrWhiteSpace(defaultDir) && Directory.Exists(defaultDir))
        {
            txtWebMOutput.Text = defaultDir;
            txtCodecOutput.Text = defaultDir;
            txtAudioOutput.Text = defaultDir;
        }

        // Initialize Tab Active Styles
        UpdateTabStyles(btnTabYoutube);


        _ = PreInitializeWebView2Async();

        cmbQuality.IntegralHeight = false;
        cmbQuality.MaxDropDownItems = 10;

        this.FormClosing += Form1_FormClosing;


        if (SettingsManager.Settings.AutoUpdateCheck)
        {
            _ = CheckForUpdateAsync(false);
        }
        

        _ = EnsureRequiredToolsAsync();
    }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        // Cancel all active conversions
        _webmCts?.Cancel();
        _codecCts?.Cancel();
        _audioCts?.Cancel();
        foreach (var job in _activeJobs) job.JobCts?.Cancel();
        foreach (var job in _activeYtDlpJobs) job.JobCts?.Cancel();

        try { _downloadWidgetForm?.Dispose(); } catch { }
        ClearLoginBrowserDataBeforeExit();

        // Perform final cleanup of processes and partial files
        CleanupManager.FullSystemCleanup(SettingsManager.Settings.KeepLoginSession);
    }

    private void ClearLoginBrowserDataBeforeExit()
    {
        try
        {
            if (webViewX.CoreWebView2 != null)
            {
                if (!SettingsManager.Settings.KeepLoginSession)
                {
                    webViewX.CoreWebView2.CookieManager.DeleteAllCookies();
                }
                webViewX.CoreWebView2.Stop();
            }
        }
        catch { }

        try { webViewX.Dispose(); } catch { }
        if (!SettingsManager.Settings.KeepLoginSession)
        {
            try { if (Directory.Exists(XWebViewDataFolder)) Directory.Delete(XWebViewDataFolder, true); } catch { }
        }
    }

    private void Panel_Paint(object sender, PaintEventArgs e)
    {

    }

    private void ConfigureFileDropTargets()
    {
        string videoFilePlaceholder = "\uC601\uC0C1 \uD30C\uC77C \uACBD\uB85C\uB97C \uC120\uD0DD\uD558\uC138\uC694.";

        txtWebMInput.PlaceholderText = videoFilePlaceholder;
        txtCodecInput.PlaceholderText = videoFilePlaceholder;
        txtAudioInput.PlaceholderText = "\uC601\uC0C1\uC774\uB098 mp3 \uD30C\uC77C \uACBD\uB85C\uB97C \uC120\uD0DD\uD558\uC138\uC694.";

        panelWebMInput.BorderStyle = BorderStyle.FixedSingle;
        panelWebMOutput.BorderStyle = BorderStyle.FixedSingle;
        panelCodecInput.BorderStyle = BorderStyle.FixedSingle;
        panelCodecOutput.BorderStyle = BorderStyle.FixedSingle;
        panelAudioInput.BorderStyle = BorderStyle.FixedSingle;
        panelAudioOutput.BorderStyle = BorderStyle.FixedSingle;

        AddDropHintLabel(tabWebM, "dropHintWebM", "\uD30C\uC77C \uC120\uD0DD \uB610\uB294 \uB4DC\uB798\uADF8 \uC564 \uB4DC\uB78D \uAC00\uB2A5", new Point(20, 55));
        AddDropHintLabel(tabCodec, "dropHintCodec", "\uD30C\uC77C \uC120\uD0DD \uB610\uB294 \uB4DC\uB798\uADF8 \uC564 \uB4DC\uB78D \uAC00\uB2A5", new Point(20, 105));
        AddDropHintLabel(tabAudio, "dropHintAudio", "\uD30C\uC77C \uC120\uD0DD \uB610\uB294 \uB4DC\uB798\uADF8 \uC564 \uB4DC\uB78D \uAC00\uB2A5", new Point(20, 55));

        RegisterFileDropTarget(tabWebM, "\uD3EC\uB9F7 \uBCC0\uD658\uD560 \uC601\uC0C1\uC744 \uC5EC\uAE30\uC5D0 \uB193\uC73C\uC138\uC694.", path => SetSelectedInputFile(path, txtWebMInput, txtWebMOutput, VideoInputExtensions, "\uC601\uC0C1 \uD30C\uC77C", lblWebMStatus));
        RegisterFileDropTarget(tabCodec, "Pr/AE\uC6A9\uC73C\uB85C \uACE0\uCE60 \uC601\uC0C1\uC744 \uC5EC\uAE30\uC5D0 \uB193\uC73C\uC138\uC694.", path => SetSelectedInputFile(path, txtCodecInput, txtCodecOutput, VideoInputExtensions, "\uC601\uC0C1 \uD30C\uC77C", lblCodecStatus));
        RegisterFileDropTarget(tabAudio, "\uC624\uB514\uC624\uB85C \uBCC0\uD658\uD560 \uD30C\uC77C\uC744 \uC5EC\uAE30\uC5D0 \uB193\uC73C\uC138\uC694.", path => SetSelectedInputFile(path, txtAudioInput, txtAudioOutput, AudioConverterInputExtensions, "\uC601\uC0C1\uC774\uB098 \uC624\uB514\uC624 \uD30C\uC77C", lblAudioStatus));
    }

    private void ConfigureYtDlpQueueColumns()
    {
        ResizeYtDlpQueueColumns();
        lvYtDlpQueue.Resize += (s, e) => ResizeYtDlpQueueColumns();
    }

    private void ResizeYtDlpQueueColumns()
    {
        if (lvYtDlpQueue.ClientSize.Width <= 0) return;

        int totalWidth = Math.Max(320, lvYtDlpQueue.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 28);
        int subtitlesWidth = 52;
        int statusWidth = Math.Max(200, Math.Min(340, totalWidth - subtitlesWidth - 230));
        int urlWidth = Math.Max(120, totalWidth - subtitlesWidth - statusWidth);

        if (urlWidth + subtitlesWidth + statusWidth > totalWidth)
        {
            statusWidth = Math.Max(150, totalWidth - subtitlesWidth - urlWidth);
        }

        colYtDlpUrl.Width = urlWidth;
        colYtDlpSubtitles.Width = subtitlesWidth;
        colYtDlpStatus.Width = statusWidth;
        HideYtDlpQueueHorizontalScrollBar();
    }

    private void HideYtDlpQueueHorizontalScrollBar()
    {
        if (!lvYtDlpQueue.IsHandleCreated) return;

        ShowScrollBar(lvYtDlpQueue.Handle, SB_HORZ, false);
    }

    private void HideLegacyPrivateModeToggles()
    {
        lblXPrivateMode.Visible = false;
        tglXPrivateMode.Visible = false;
        lblInstaPrivateMode.Visible = false;
        tglInstaPrivateMode.Visible = false;
    }

    private void ConfigureLoginDownloadHelp()
    {
        btnYtDlpLoginBrowser.Text = "\uB85C\uADF8\uC778 \uD6C4 \uB2E4\uC6B4";
        lblYtDlpDesc.Text = "치지직, Instagram, SOOP, Pinterest, X, Vimeo, Anilife, Linkkf 등 다양한 사이트를 지원합니다.\n로그인이 필요한 일부공개/나이제한 영상은 로그인 후 좌측 상단 즉시 다운로드 또는 URL 입력으로 받을 수 있습니다.";

        btnYtDlpLoginBrowser.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnYtDlpLoginBrowser.Location = new Point(180, 182);
        btnYtDlpLoginBrowser.Size = new Size(180, 38);
        btnYtDlpCancel.Location = new Point(398, 182);

        _loginDownloadToolTip = new ToolTip
        {
            AutoPopDelay = 9000,
            InitialDelay = 350,
            ReshowDelay = 100,
            ShowAlways = true
        };

        string helpText =
            "로그인이 필요한 일부공개, 나이제한, 회원전용, 팔로워/구독자 공개 영상을 받을 때 사용합니다.\n\n" +
            "1. [로그인 후 다운]을 누른 뒤 사이트를 선택하고 로그인합니다.\n" +
            "2. 영상 페이지에서 좌측 상단 [즉시 다운로드]를 누르면 바로 받을 수 있습니다.\n" +
            "3. 브라우저를 닫은 뒤 URL 입력칸에 주소를 넣고 다운로드할 수도 있습니다.\n\n" +
            "YouTube 회원전용 영상은 이 브라우저에서 Google/YouTube 로그인 후 다시 시도하세요(업데이트 예정).\n" +
            "로그인 유지를 켜도 사이트 로그인 화면의 '로그인 유지', '로그인 상태 유지' 등의 옵션을 함께 체크해야 유지됩니다.\n" +
            "DRM 보호 영상이나 계정 권한이 없는 영상은 로그인해도 받을 수 없습니다. T.T";

        var helpIcon = tabYtDlp.Controls.Find("btnYtDlpLoginHelp", false).FirstOrDefault() as RoundButton;
        if (helpIcon == null)
        {
            helpIcon = new RoundButton
            {
                Name = "btnYtDlpLoginHelp",
                Text = "i",
                Size = new Size(26, 26),
                BorderRadius = 13,
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Help
            };
            helpIcon.FlatAppearance.BorderSize = 0;
            tabYtDlp.Controls.Add(helpIcon);
        }

        helpIcon.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        helpIcon.Location = new Point(btnYtDlpLoginBrowser.Right + 8, btnYtDlpLoginBrowser.Top + 6);
        helpIcon.BringToFront();
        btnYtDlpCancel.BringToFront();

        _loginDownloadToolTip.SetToolTip(helpIcon, helpText);

        chkKeepLoginSession = tabYtDlp.Controls.Find("chkKeepLoginSession", false).FirstOrDefault() as CheckBox;
        if (chkKeepLoginSession == null)
        {
            chkKeepLoginSession = new CheckBox
            {
                Name = "chkKeepLoginSession",
                Text = "\uB85C\uADF8\uC778 \uC720\uC9C0",
                AutoSize = true,
                Location = new Point(btnYtDlpLoginBrowser.Left, btnYtDlpLoginBrowser.Bottom + 9),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            tabYtDlp.Controls.Add(chkKeepLoginSession);
        }

        _initializingKeepLoginCheckbox = true;
        chkKeepLoginSession.Checked = SettingsManager.Settings.KeepLoginSession;
        _initializingKeepLoginCheckbox = false;
        chkKeepLoginSession.CheckedChanged -= ChkKeepLoginSession_CheckedChanged;
        chkKeepLoginSession.CheckedChanged += ChkKeepLoginSession_CheckedChanged;
        _loginDownloadToolTip.SetToolTip(chkKeepLoginSession, "켜면 프로그램을 껐다 켜도 로그인 브라우저 쿠키를 유지합니다.\n각 사이트의 '로그인 유지' 옵션도 함께 체크해야 오래 유지됩니다.\n방문 기록과 캐시는 정리하고, 로그인에 필요한 쿠키와 사이트 저장소만 유지합니다.\n끄면 저장된 로그인 쿠키를 즉시 삭제합니다.");
        chkKeepLoginSession.BringToFront();
    }

    private async void ChkKeepLoginSession_CheckedChanged(object? sender, EventArgs e)
    {
        if (_initializingKeepLoginCheckbox || chkKeepLoginSession == null) return;

        SettingsManager.Settings.KeepLoginSession = chkKeepLoginSession.Checked;
        SettingsManager.Save();

        if (chkKeepLoginSession.Checked)
        {
            if (!_loginKeepNoticeShown)
            {
                _loginKeepNoticeShown = true;
                ShowCenteredMessage(
                    "로그인 유지 기능을 켰습니다.\n\n프로그램을 껐다 켜도 로그인 브라우저의 쿠키가 유지됩니다.\n각 사이트 로그인 화면에 있는 '로그인 유지', '로그인 상태 유지', 'Remember me' 같은 옵션도 함께 체크해야 오래 유지됩니다.\n\n사이트 자체 로그인 유지 옵션을 체크하지 않으면 프로그램이 쿠키를 보관해도 사이트 세션이 만료되어 다시 로그인이 필요할 수 있습니다.\n\n속도 저하를 막기 위해 방문 기록과 캐시는 시작/종료 때 정리하고, 로그인에 필요한 쿠키와 사이트 저장소만 유지합니다.\n\n공용 PC에서는 계정 정보가 남을 수 있으니 사용하지 않는 것을 권장합니다.\n체크를 해제하면 저장된 로그인 쿠키를 즉시 삭제합니다.",
                    "로그인 유지 안내",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            return;
        }

        await ClearLoginBrowserSessionAsync(showMessage: true);
    }

    private async Task ClearLoginBrowserSessionAsync(bool showMessage)
    {
        try
        {
            if (webViewX.CoreWebView2 != null)
            {
                webViewX.CoreWebView2.CookieManager.DeleteAllCookies();
                webViewX.CoreWebView2.Stop();
                webViewX.CoreWebView2.Navigate("about:blank");
            }
        }
        catch { }

        _capturedM3u8Url = "";
        _capturedAuthToken = "";
        _capturedCsrfToken = "";

        try
        {
            await Task.Run(() => CleanupManager.CleanupWebViewData(force: true));
            await Task.Run(() =>
            {
                try { if (Directory.Exists(XWebViewDataFolder)) Directory.Delete(XWebViewDataFolder, true); } catch { }
            });
        }
        catch { }

        if (showMessage)
        {
            ShowCenteredMessage(
                "로그인 유지 기능을 껐습니다.\n\n저장된 로그인 쿠키를 삭제했습니다. 다시 로그인하려면 [로그인 후 다운]에서 사이트를 열어주세요.",
                "로그인 정보 삭제",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    private void ConfigureDownloadRuleSettings()
    {
        tabSettings.AutoScroll = false;
        tabSettings.AutoScrollMinSize = Size.Empty;

        var group = tabSettings.Controls.Find("grpDownloadRules", false).FirstOrDefault() as GroupBox;
        if (group == null)
        {
            group = new GroupBox
            {
                Name = "grpDownloadRules",
                Text = "다운로드 저장 규칙",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(20, 285),
                Size = new Size(570, 240),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tabSettings.Controls.Add(group);
        }

        group.Controls.Clear();
        group.Size = new Size(570, 240);

        chkUseSiteFolderRules = new CheckBox
        {
            Text = "사이트별 하위 폴더 사용",
            AutoSize = true,
            Location = new Point(15, 30),
            Font = new Font("Segoe UI", 9F)
        };
        group.Controls.Add(chkUseSiteFolderRules);

        chkUseCustomSiteFolders = new CheckBox
        {
            Text = "사이트별 직접 폴더 지정",
            AutoSize = true,
            Location = new Point(15, 58),
            Font = new Font("Segoe UI", 9F)
        };
        group.Controls.Add(chkUseCustomSiteFolders);

        group.Controls.Add(new Label { Text = "사이트", AutoSize = true, Location = new Point(28, 92), Font = new Font("Segoe UI", 9F) });
        cmbSiteFolderSite = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(80, 88),
            Size = new Size(120, 23),
            Font = new Font("Segoe UI", 9F)
        };
        cmbSiteFolderSite.Items.AddRange(GetKnownSiteNames());
        cmbSiteFolderSite.SelectedIndexChanged += (s, e) => LoadSelectedSiteFolderOverride();
        group.Controls.Add(cmbSiteFolderSite);

        txtSiteFolderOverride = new TextBox
        {
            Location = new Point(210, 88),
            Size = new Size(250, 23),
            Font = new Font("Segoe UI", 9F)
        };
        txtSiteFolderOverride.Leave += (s, e) => SaveSelectedSiteFolderOverride();
        group.Controls.Add(txtSiteFolderOverride);

        btnBrowseSiteFolderOverride = new RoundButton
        {
            Text = "찾기",
            Location = new Point(470, 86),
            Size = new Size(70, 27),
            BorderRadius = 12,
            BackColor = Color.FromArgb(226, 232, 240),
            ForeColor = Color.FromArgb(51, 65, 85),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold)
        };
        btnBrowseSiteFolderOverride.FlatAppearance.BorderSize = 0;
        btnBrowseSiteFolderOverride.Click += (s, e) => BrowseSelectedSiteFolderOverride();
        group.Controls.Add(btnBrowseSiteFolderOverride);

        group.Controls.Add(new Label { Text = "파일명 규칙", AutoSize = true, Location = new Point(15, 130), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
        cmbFileNamePreset = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(110, 126),
            Size = new Size(150, 23),
            Font = new Font("Segoe UI", 9F)
        };
        cmbFileNamePreset.Items.AddRange(new object[] { "제목만", "채널-제목", "날짜-제목", "사이트-제목", "직접 입력" });
        cmbFileNamePreset.SelectedIndexChanged += (s, e) => UpdateCustomFileNameTemplateVisibility();
        group.Controls.Add(cmbFileNamePreset);

        txtCustomFileNameTemplate = new TextBox
        {
            Location = new Point(270, 126),
            Size = new Size(270, 23),
            Font = new Font("Segoe UI", 9F),
            PlaceholderText = "{site}_{title}_{date}"
        };
        group.Controls.Add(txtCustomFileNameTemplate);

        group.Controls.Add(new Label
        {
            Text = "토큰: {site}, {channel}, {title}, {date}, {quality}",
            AutoSize = true,
            Location = new Point(110, 154),
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8F)
        });

        group.Controls.Add(new Label { Text = "기본 품질", AutoSize = true, Location = new Point(15, 188), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
        cmbDefaultVideoQuality = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(110, 184),
            Size = new Size(150, 23),
            Font = new Font("Segoe UI", 9F)
        };
        cmbDefaultVideoQuality.Items.AddRange(new object[] { "최고화질", "1080p", "720p", "MP3" });
        group.Controls.Add(cmbDefaultVideoQuality);

        ConfigureInlineSubtitleLanguageControls();

        chkYoutubeDownloadSubtitles.CheckedChanged -= SubtitleCheckbox_CheckedChanged;
        chkYtDlpDownloadSubtitles.CheckedChanged -= SubtitleCheckbox_CheckedChanged;
        chkYoutubeDownloadSubtitles.CheckedChanged += SubtitleCheckbox_CheckedChanged;
        chkYtDlpDownloadSubtitles.CheckedChanged += SubtitleCheckbox_CheckedChanged;

        btnSaveSettings.Location = new Point(20, 545);
        btnCheckUpdate.Location = new Point(20, 605);
        lblAbout.Location = new Point(20, 660);
        tabSettings.AutoScrollMinSize = Size.Empty;

        ReloadDownloadRuleSettingsUI();
    }

    private void ConfigureWidgetModeSettings()
    {
        tabSettings.AutoScroll = false;
        tabSettings.AutoScrollMinSize = Size.Empty;

        var group = tabSettings.Controls.Find("grpWidgetMode", false).FirstOrDefault() as GroupBox;
        if (group == null)
        {
            group = new GroupBox
            {
                Name = "grpWidgetMode",
                Text = "위젯 모드",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(20, 535),
                Size = new Size(570, 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            tabSettings.Controls.Add(group);
        }

        group.Controls.Clear();

        chkEnableWidgetMode = new CheckBox
        {
            Text = "시계 위 미니 다운로드 버튼 사용",
            AutoSize = true,
            Location = new Point(15, 28),
            Font = new Font("Segoe UI", 9F)
        };
        chkEnableWidgetMode.CheckedChanged += ChkEnableWidgetMode_CheckedChanged;
        group.Controls.Add(chkEnableWidgetMode);

        var hint = new Label
        {
            Text = "브라우저에서 URL을 복사한 뒤 작은 버튼을 누르면 바로 다운로드 대기열에 추가됩니다.",
            AutoSize = true,
            Location = new Point(35, 52),
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8.5F)
        };
        group.Controls.Add(hint);

        btnSaveSettings.Location = new Point(20, 545);
        btnCheckUpdate.Location = new Point(20, 605);
        lblAbout.Location = new Point(20, 660);
        tabSettings.AutoScrollMinSize = Size.Empty;

        group.Visible = false;
        ReloadWidgetModeSettingsUI();
    }

    private void ConfigureTopWidgetModeButton()
    {
        lblTopWidgetMode = new Label
        {
            Name = "lblTopWidgetMode",
            Text = "위젯 모드",
            Size = new Size(78, 24),
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("맑은 고딕", 8.5F, FontStyle.Regular),
            ForeColor = Color.Silver,
            BackColor = panelSidebar.BackColor,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };
        panelSidebar.Controls.Add(lblTopWidgetMode);

        btnTopWidgetMode = new WidgetModeToggleButton
        {
            Name = "btnTopWidgetMode",
            Size = new Size(46, 24),
            Checked = false,
            BackColor = panelSidebar.BackColor,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };
        btnTopWidgetMode.CheckedChanged += (s, e) =>
        {
            if (btnTopWidgetMode.Checked)
            {
                EnterWidgetMode(saveSetting: false);
            }
        };
        panelSidebar.Controls.Add(btnTopWidgetMode);
        lblTopWidgetMode.BringToFront();
        btnTopWidgetMode.BringToFront();

        var tip = new ToolTip { ShowAlways = true };
        tip.SetToolTip(lblTopWidgetMode, "위젯 모드 ON/OFF");
        tip.SetToolTip(btnTopWidgetMode, "위젯 모드로 전환");

        panelSidebar.Resize += (s, e) => PositionTopWidgetModeButton();
        PositionTopWidgetModeButton();
    }

    private void ConfigureVideoPickerTab()
    {
        if (tabVideoPicker != null) return;

        tabVideoPicker = new TabPage
        {
            Name = "tabVideoPicker",
            Text = "\uC601\uC0C1 \uC120\uD0DD",
            BackColor = Color.FromArgb(250, 250, 250),
            Padding = new Padding(24)
        };

        lblVideoPickerTitle = new Label
        {
            Text = "\uBC1B\uC744 \uC601\uC0C1\uC744 \uC120\uD0DD\uD574 \uC8FC\uC138\uC694",
            AutoSize = true,
            Location = new Point(30, 30),
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = Color.Black
        };

        lblVideoPickerHint = new Label
        {
            Text = "\uD55C \uD398\uC774\uC9C0\uC5D0 \uC5EC\uB7EC \uC601\uC0C1\uC774 \uAC10\uC9C0\uB418\uC5C8\uC2B5\uB2C8\uB2E4. \uC6D0\uD558\uB294 \uC601\uC0C1\uC744 \uACE0\uB974\uAC70\uB098 \uBAA8\uB450 \uB2E4\uC6B4\uB85C\uB4DC\uD558\uC138\uC694.",
            AutoSize = false,
            Location = new Point(32, 72),
            Size = new Size(760, 36),
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(70, 80, 95)
        };

        btnVideoPickerDownloadAll = new RoundButton
        {
            Text = "\uBAA8\uB450 \uB2E4\uC6B4\uB85C\uB4DC",
            Size = new Size(150, 38),
            Location = new Point(32, 122),
            BackColor = Color.FromArgb(18, 148, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            BorderRadius = 14
        };
        btnVideoPickerDownloadAll.FlatAppearance.BorderSize = 0;
        btnVideoPickerDownloadAll.Click += BtnVideoPickerDownloadAll_Click;

        btnVideoPickerDownloadSelected = new RoundButton
        {
            Text = "\uC120\uD0DD \uB2E4\uC6B4\uB85C\uB4DC",
            Size = new Size(150, 38),
            Location = new Point(192, 122),
            BackColor = Color.FromArgb(16, 132, 118),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            BorderRadius = 14
        };
        btnVideoPickerDownloadSelected.FlatAppearance.BorderSize = 0;
        btnVideoPickerDownloadSelected.Click += BtnVideoPickerDownloadSelected_Click;

        btnVideoPickerCancel = new RoundButton
        {
            Text = "\uB3CC\uC544\uAC00\uAE30",
            Size = new Size(120, 38),
            Location = new Point(352, 122),
            BackColor = Color.FromArgb(239, 244, 248),
            ForeColor = Color.FromArgb(51, 65, 85),
            FlatStyle = FlatStyle.Flat,
            BorderRadius = 14
        };
        btnVideoPickerCancel.FlatAppearance.BorderSize = 0;
        btnVideoPickerCancel.Click += (s, e) => CloseVideoPicker(returnToWidget: _returnToWidgetAfterVideoPick);

        imgVideoPickerThumbs = new ImageList
        {
            ColorDepth = ColorDepth.Depth32Bit,
            ImageSize = new Size(96, 54)
        };

        lvVideoPicker = new ListView
        {
            Location = new Point(32, 178),
            Size = new Size(820, 390),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = true,
            HideSelection = false,
            SmallImageList = imgVideoPickerThumbs,
            Font = new Font("Segoe UI", 9.5F),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        lvVideoPicker.Columns.Add("\uC378\uB124\uC77C / \uC81C\uBAA9", 500);
        lvVideoPicker.Columns.Add("\uAE38\uC774", 90);
        lvVideoPicker.Columns.Add("URL", 220);
        lvVideoPicker.DoubleClick += BtnVideoPickerDownloadSelected_Click;

        tabVideoPicker.Controls.Add(lblVideoPickerTitle);
        tabVideoPicker.Controls.Add(lblVideoPickerHint);
        tabVideoPicker.Controls.Add(btnVideoPickerDownloadAll);
        tabVideoPicker.Controls.Add(btnVideoPickerDownloadSelected);
        tabVideoPicker.Controls.Add(btnVideoPickerCancel);
        tabVideoPicker.Controls.Add(lvVideoPicker);
        tabControlMain.Controls.Add(tabVideoPicker);
    }

    private void ConfigureVersionArchiveButton()
    {
        if (btnOpenVersionArchive == null)
        {
            btnOpenVersionArchive = new RoundButton
            {
                Name = "btnOpenVersionArchive",
                Text = "\uB2E4\uB978 \uBC84\uC804 \uBC1B\uAE30",
                Size = new Size(145, 34),
                BorderRadius = 14,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };
            btnOpenVersionArchive.FlatAppearance.BorderSize = 0;
            btnOpenVersionArchive.Click += BtnOpenVersionArchive_Click;
            tabSettings.Controls.Add(btnOpenVersionArchive);

            var tip = new ToolTip { ShowAlways = true };
            tip.SetToolTip(btnOpenVersionArchive, "\uC774\uC804 \uBC84\uC804\uC774\uB098 \uD14C\uC2A4\uD2B8 \uBC84\uC804\uC744 \uB2E4\uC6B4\uB85C\uB4DC \uD398\uC774\uC9C0\uC5D0\uC11C \uC120\uD0DD\uD574 \uBC1B\uC2B5\uB2C8\uB2E4.");
        }

        btnOpenVersionArchive.Location = new Point(20, Math.Max(420, tabSettings.ClientSize.Height - 54));
        btnOpenVersionArchive.BringToFront();
    }

    private void BtnOpenVersionArchive_Click(object? sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(VersionArchiveUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ShowCenteredMessage(
                "\uBC84\uC804 \uBCF4\uAD00\uD568\uC744 \uC5F4\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4.\n\n" + ex.Message,
                "\uBC84\uC804 \uBCF4\uAD00\uD568",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void PositionTopWidgetModeButton()
    {
        if (btnTopWidgetMode == null) return;
        int top = Math.Max(395, btnTabSettings.Top - 40);
        btnTopWidgetMode.Location = new Point(108, top);
        if (lblTopWidgetMode != null)
        {
            lblTopWidgetMode.Location = new Point(15, top);
            lblTopWidgetMode.BackColor = panelSidebar.BackColor;
            lblTopWidgetMode.BringToFront();
        }
        btnTopWidgetMode.BackColor = panelSidebar.BackColor;
        btnTopWidgetMode.BringToFront();
    }

    private void ReloadWidgetModeSettingsUI()
    {
        if (chkEnableWidgetMode == null) return;
        _updatingWidgetModeCheckbox = true;
        chkEnableWidgetMode.Checked = SettingsManager.Settings.EnableWidgetMode;
        _updatingWidgetModeCheckbox = false;
    }

    private void ChkEnableWidgetMode_CheckedChanged(object? sender, EventArgs e)
    {
        if (_updatingWidgetModeCheckbox || chkEnableWidgetMode == null) return;

        if (chkEnableWidgetMode.Checked)
        {
            EnterWidgetMode(saveSetting: true);
        }
        else
        {
            ExitWidgetMode(saveSetting: true);
        }
    }

    private ComboBox CreateSubtitlePresetCombo(Point location)
    {
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = location,
            Size = new Size(150, 23),
            Font = new Font("Segoe UI", 9F)
        };
        combo.Items.AddRange(new object[] { "한국어", "한국어 + 영어", "영어", "모든 자막" });
        return combo;
    }

    private void ConfigureInlineSubtitleLanguageControls()
    {
        cmbYoutubeSubtitleLanguage = CreateSubtitlePresetCombo(new Point(chkYoutubeDownloadSubtitles.Right + 18, chkYoutubeDownloadSubtitles.Top - 3));
        cmbYoutubeSubtitleLanguage.Name = "cmbYoutubeSubtitleLanguage";
        cmbYoutubeSubtitleLanguage.Width = 140;
        tabYoutube.Controls.Add(cmbYoutubeSubtitleLanguage);
        cmbYoutubeSubtitleLanguage.BringToFront();

        cmbYtDlpSubtitleLanguage = CreateSubtitlePresetCombo(new Point(chkYtDlpDownloadSubtitles.Left + 18, chkYtDlpDownloadSubtitles.Bottom + 8));
        cmbYtDlpSubtitleLanguage.Name = "cmbYtDlpSubtitleLanguage";
        cmbYtDlpSubtitleLanguage.Width = 140;
        tabYtDlp.Controls.Add(cmbYtDlpSubtitleLanguage);
        cmbYtDlpSubtitleLanguage.BringToFront();
    }

    private static object[] GetKnownSiteNames()
    {
        return new object[] { "YouTube", "SOOP", "Chzzk", "Instagram", "X", "Anilife", "Linkkf", "WebSite", "Audio" };
    }

    private void ReloadDownloadRuleSettingsUI()
    {
        if (chkUseSiteFolderRules == null) return;

        chkUseSiteFolderRules.Checked = SettingsManager.Settings.UseSiteFolderRules;
        chkUseCustomSiteFolders!.Checked = SettingsManager.Settings.UseCustomSiteFolders;
        cmbSiteFolderSite!.SelectedIndex = 0;
        SelectComboText(cmbFileNamePreset!, GetFileNamePresetDisplay(SettingsManager.Settings.FileNamePreset));
        txtCustomFileNameTemplate!.Text = string.IsNullOrWhiteSpace(SettingsManager.Settings.CustomFileNameTemplate)
            ? "{title}"
            : SettingsManager.Settings.CustomFileNameTemplate;
        SelectComboText(cmbDefaultVideoQuality!, GetQualityDisplay(SettingsManager.Settings.DefaultVideoQuality));
        string subtitlePreset = string.IsNullOrWhiteSpace(SettingsManager.Settings.SubtitleLanguagePreset)
            ? "Ko"
            : SettingsManager.Settings.SubtitleLanguagePreset;
        if (subtitlePreset == "KoEn") subtitlePreset = "Ko";
        SelectComboText(cmbYoutubeSubtitleLanguage!, GetSubtitlePresetDisplay(subtitlePreset));
        SelectComboText(cmbYtDlpSubtitleLanguage!, GetSubtitlePresetDisplay(subtitlePreset));
        LoadSelectedSiteFolderOverride();
        UpdateCustomFileNameTemplateVisibility();
        UpdateSubtitleOptionVisibility();
    }

    private static void SelectComboText(ComboBox combo, string text)
    {
        int index = combo.Items.IndexOf(text);
        combo.SelectedIndex = index >= 0 ? index : 0;
    }

    private void UpdateCustomFileNameTemplateVisibility()
    {
        if (txtCustomFileNameTemplate == null || cmbFileNamePreset == null) return;
        txtCustomFileNameTemplate.Enabled = cmbFileNamePreset.Text == "직접 입력";
    }

    private void SubtitleCheckbox_CheckedChanged(object? sender, EventArgs e) => UpdateSubtitleOptionVisibility();

    private void UpdateSubtitleOptionVisibility()
    {
        if (cmbYoutubeSubtitleLanguage != null)
        {
            cmbYoutubeSubtitleLanguage.Location = new Point(chkYoutubeDownloadSubtitles.Right + 18, chkYoutubeDownloadSubtitles.Top - 3);
            cmbYoutubeSubtitleLanguage.Visible = chkYoutubeDownloadSubtitles.Checked;
            if (cmbYoutubeSubtitleLanguage.Visible) cmbYoutubeSubtitleLanguage.BringToFront();
        }

        if (cmbYtDlpSubtitleLanguage != null)
        {
            cmbYtDlpSubtitleLanguage.Location = new Point(chkYtDlpDownloadSubtitles.Left + 18, chkYtDlpDownloadSubtitles.Bottom + 8);
            cmbYtDlpSubtitleLanguage.Visible = chkYtDlpDownloadSubtitles.Checked;
            if (cmbYtDlpSubtitleLanguage.Visible) cmbYtDlpSubtitleLanguage.BringToFront();
        }
    }

    private async Task PopulateYoutubeSubtitleOptionsAsync(VideoId videoId)
    {
        if (cmbYoutubeSubtitleLanguage == null) return;

        string previousValue = GetSelectedSubtitlePreset(cmbYoutubeSubtitleLanguage);
        cmbYoutubeSubtitleLanguage.BeginUpdate();
        try
        {
            cmbYoutubeSubtitleLanguage.Items.Clear();
            cmbYoutubeSubtitleLanguage.Enabled = false;

            var captionManifest = await _youtube.Videos.ClosedCaptions.GetManifestAsync(videoId);
            var tracks = captionManifest.Tracks
                .GroupBy(t => t.Language.Code, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.FirstOrDefault(t => !t.IsAutoGenerated) ?? g.First())
                .OrderBy(t => IsKoreanCaptionTrack(t) ? 0 : IsEnglishCaptionTrack(t) ? 1 : 2)
                .ThenBy(t => t.Language.Name)
                .ToList();

            if (tracks.Count == 0)
            {
                cmbYoutubeSubtitleLanguage.Items.Add(new SubtitleOption("지원 자막 없음", ""));
                cmbYoutubeSubtitleLanguage.SelectedIndex = 0;
                return;
            }

            foreach (var track in tracks)
            {
                string code = track.Language.Code;
                string name = string.IsNullOrWhiteSpace(track.Language.Name) ? code : track.Language.Name;
                string autoSuffix = track.IsAutoGenerated ? " 자동" : "";
                cmbYoutubeSubtitleLanguage.Items.Add(new SubtitleOption($"{name} ({code}){autoSuffix}", "Lang:" + code));
            }

            if (tracks.Count > 1)
            {
                cmbYoutubeSubtitleLanguage.Items.Add(new SubtitleOption("영상의 모든 자막", "All"));
            }

            int selectedIndex = FindSubtitleOptionIndex(cmbYoutubeSubtitleLanguage, previousValue);
            if (selectedIndex < 0) selectedIndex = FindSubtitleOptionIndex(cmbYoutubeSubtitleLanguage, "Ko");
            if (selectedIndex < 0) selectedIndex = 0;
            cmbYoutubeSubtitleLanguage.SelectedIndex = selectedIndex;
            cmbYoutubeSubtitleLanguage.Enabled = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[YouTubeSubtitleManifest] {ex.Message}");
            cmbYoutubeSubtitleLanguage.Items.Clear();
            cmbYoutubeSubtitleLanguage.Items.Add(new SubtitleOption("지원 자막 확인 실패", ""));
            cmbYoutubeSubtitleLanguage.SelectedIndex = 0;
            cmbYoutubeSubtitleLanguage.Enabled = false;
        }
        finally
        {
            cmbYoutubeSubtitleLanguage.EndUpdate();
            UpdateSubtitleOptionVisibility();
        }
    }

    private static int FindSubtitleOptionIndex(ComboBox combo, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return -1;

        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is not SubtitleOption option) continue;
            if (SubtitleOptionMatches(option, value)) return i;
        }

        return -1;
    }

    private static bool SubtitleOptionMatches(SubtitleOption option, string value)
    {
        if (option.Value.Equals(value, StringComparison.OrdinalIgnoreCase)) return true;
        if (value == "Ko") return option.Value.StartsWith("Lang:ko", StringComparison.OrdinalIgnoreCase);
        if (value == "KoEn") return option.Value.StartsWith("Lang:ko", StringComparison.OrdinalIgnoreCase)
            || option.Value.StartsWith("Lang:en", StringComparison.OrdinalIgnoreCase);
        if (value == "En") return option.Value.StartsWith("Lang:en", StringComparison.OrdinalIgnoreCase);
        return false;
    }

    private void LoadSelectedSiteFolderOverride()
    {
        if (cmbSiteFolderSite == null || txtSiteFolderOverride == null) return;
        string site = cmbSiteFolderSite.Text;
        SettingsManager.Settings.SiteFolderOverrides ??= new Dictionary<string, string>();
        SettingsManager.Settings.SiteFolderOverrides.TryGetValue(site, out string? path);
        txtSiteFolderOverride.Text = path ?? "";
    }

    private void SaveSelectedSiteFolderOverride()
    {
        if (cmbSiteFolderSite == null || txtSiteFolderOverride == null) return;
        string site = cmbSiteFolderSite.Text;
        string path = txtSiteFolderOverride.Text.Trim();
        SettingsManager.Settings.SiteFolderOverrides ??= new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(path))
            SettingsManager.Settings.SiteFolderOverrides.Remove(site);
        else
            SettingsManager.Settings.SiteFolderOverrides[site] = path;
    }

    private void BrowseSelectedSiteFolderOverride()
    {
        if (txtSiteFolderOverride == null) return;
        using var fbd = new FolderBrowserDialog();
        fbd.Description = "사이트별 저장 폴더를 선택하세요.";
        if (Directory.Exists(txtSiteFolderOverride.Text)) fbd.SelectedPath = txtSiteFolderOverride.Text;
        if (fbd.ShowDialog() != DialogResult.OK) return;
        txtSiteFolderOverride.Text = fbd.SelectedPath;
        SaveSelectedSiteFolderOverride();
    }

    private static string GetFileNamePresetValue(string? display) => display switch
    {
        "채널-제목" => "ChannelTitle",
        "날짜-제목" => "DateTitle",
        "사이트-제목" => "SiteTitle",
        "직접 입력" => "Custom",
        _ => "Title"
    };

    private static string GetFileNamePresetDisplay(string? value) => value switch
    {
        "ChannelTitle" => "채널-제목",
        "DateTitle" => "날짜-제목",
        "SiteTitle" => "사이트-제목",
        "Custom" => "직접 입력",
        _ => "제목만"
    };

    private static string GetQualityValue(string? display) => display switch
    {
        "1080p" => "1080p",
        "720p" => "720p",
        "MP3" => "MP3",
        _ => "Best"
    };

    private static string GetQualityDisplay(string? value) => value switch
    {
        "1080p" => "1080p",
        "720p" => "720p",
        "MP3" => "MP3",
        _ => "최고화질"
    };

    private static string GetSubtitlePresetValue(string? display) => display switch
    {
        "한국어" => "Ko",
        "한국어 우선" => "Ko",
        "한국어 + 영어" => "KoEn",
        "영어" => "En",
        "모든 자막" => "All",
        "영상의 모든 자막" => "All",
        _ => "Ko"
    };

    private static string GetSelectedSubtitlePreset(ComboBox? combo)
    {
        if (combo?.SelectedItem is SubtitleOption option) return option.Value;
        return GetSubtitlePresetValue(combo?.Text);
    }

    private static string GetSubtitlePresetDisplay(string? value) => value switch
    {
        "Ko" => "한국어",
        "En" => "영어",
        "All" => "모든 자막",
        _ => "한국어"
    };

    private static string GetSubtitleLanguageArgument(string? value) => value switch
    {
        "Ko" => "ko.*,ko",
        "En" => "en.*,en",
        "All" => "all",
        _ when !string.IsNullOrWhiteSpace(value) && value.StartsWith("Lang:", StringComparison.OrdinalIgnoreCase) => value.Substring("Lang:".Length),
        _ => "ko.*,ko"
    };

    private static string GetSiteNameFromUrl(string url)
    {
        string lower = url.ToLowerInvariant();
        if (LooksLikeYouTubeInput(url)) return "YouTube";
        if (lower.Contains("sooplive")) return "SOOP";
        if (lower.Contains("chzzk.naver.com") || lower.Contains("pstatic.net")) return "Chzzk";
        if (lower.Contains("instagram.com")) return "Instagram";
        if (lower.Contains("x.com") || lower.Contains("twitter.com")) return "X";
        if (lower.Contains("anilife") || lower.Contains("gcdn.app")) return "Anilife";
        if (lower.Contains("linkkf")) return "Linkkf";
        return "WebSite";
    }

    private static string GetDownloadSaveDirectory(string url, bool audioOnly)
    {
        string basePath = SettingsManager.Settings.DefaultDownloadFolder;
        if (string.IsNullOrWhiteSpace(basePath))
            basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        string site = audioOnly ? "Audio" : GetSiteNameFromUrl(url);
        SettingsManager.Settings.SiteFolderOverrides ??= new Dictionary<string, string>();

        if (SettingsManager.Settings.UseCustomSiteFolders &&
            SettingsManager.Settings.SiteFolderOverrides.TryGetValue(site, out string? overridePath) &&
            !string.IsNullOrWhiteSpace(overridePath))
        {
            Directory.CreateDirectory(overridePath);
            return overridePath;
        }

        if (SettingsManager.Settings.UseSiteFolderRules)
        {
            string sitePath = Path.Combine(basePath, site);
            Directory.CreateDirectory(sitePath);
            return sitePath;
        }

        Directory.CreateDirectory(basePath);
        return basePath;
    }

    private static string BuildFileNameFromSettings(string site, string channel, string title, string quality)
    {
        string preset = SettingsManager.Settings.FileNamePreset;
        string template = preset switch
        {
            "ChannelTitle" => "{channel} - {title}",
            "DateTitle" => "{date} - {title}",
            "SiteTitle" => "{site} - {title}",
            "Custom" => string.IsNullOrWhiteSpace(SettingsManager.Settings.CustomFileNameTemplate) ? "{title}" : SettingsManager.Settings.CustomFileNameTemplate,
            _ => "{title}"
        };

        string fileName = template
            .Replace("{site}", site)
            .Replace("{channel}", string.IsNullOrWhiteSpace(channel) ? site : channel)
            .Replace("{title}", title)
            .Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"))
            .Replace("{quality}", quality);

        fileName = MakeValidFileName(fileName.Trim());
        return string.IsNullOrWhiteSpace(fileName) ? MakeValidFileName(title) : fileName;
    }

    private static string BuildYtDlpOutputNameTemplate(string site)
    {
        string preset = SettingsManager.Settings.FileNamePreset;
        string template = preset switch
        {
            "ChannelTitle" => "{channel} - {title}",
            "DateTitle" => "{date} - {title}",
            "SiteTitle" => "{site} - {title}",
            "Custom" => string.IsNullOrWhiteSpace(SettingsManager.Settings.CustomFileNameTemplate) ? "{title}" : SettingsManager.Settings.CustomFileNameTemplate,
            _ => "{title}"
        };

        return template
            .Replace("{site}", site)
            .Replace("{channel}", "%(uploader)s")
            .Replace("{title}", "%(title)s")
            .Replace("{date}", "%(upload_date>%Y-%m-%d)s")
            .Replace("{quality}", "%(height)sp");
    }

    private static string GetYtDlpFormatForDefaultQuality()
    {
        return SettingsManager.Settings.DefaultVideoQuality switch
        {
            "1080p" => "bestvideo[height<=1080]+bestaudio/best[height<=1080]",
            "720p" => "bestvideo[height<=720]+bestaudio/best[height<=720]",
            "MP3" => "best_mp3",
            _ => "bestvideo*+bestaudio/best"
        };
    }

    private void ApplyDefaultQualitySelection()
    {
        if (cmbQuality.Items.Count == 0) return;

        string quality = SettingsManager.Settings.DefaultVideoQuality;
        int target = 0;

        if (quality == "MP3")
        {
            target = FindQualityIndex(item => item.Id.Equals("best_mp3", StringComparison.OrdinalIgnoreCase));
        }
        else if (quality == "1080p")
        {
            target = FindQualityIndex(item => item.IsVideo && ExtractQualityHeight(item) <= 1080);
        }
        else if (quality == "720p")
        {
            target = FindQualityIndex(item => item.IsVideo && ExtractQualityHeight(item) <= 720);
        }

        cmbQuality.SelectedIndex = target >= 0 ? target : 0;
    }

    private int FindQualityIndex(Func<QualityOption, bool> predicate)
    {
        for (int i = 0; i < cmbQuality.Items.Count; i++)
        {
            if (cmbQuality.Items[i] is QualityOption option && predicate(option)) return i;
        }

        return -1;
    }

    private static int ExtractQualityHeight(QualityOption option)
    {
        var match = System.Text.RegularExpressions.Regex.Match(option.Title + " " + option.Id, @"(?<height>\d+)p", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups["height"].Value, out int height) ? height : int.MaxValue;
    }

    private static void AddDropHintLabel(Control parent, string name, string text, Point location)
    {
        if (parent.Controls.Find(name, false).Length > 0) return;

        var label = new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = location,
            Name = name,
            Text = text
        };

        parent.Controls.Add(label);
        label.BringToFront();
    }

    private void RegisterFileDropTarget(Control parent, string dropText, Action<string> onFileDropped)
    {
        var overlay = new DropOverlayPanel
        {
            Parent = parent,
            Dock = DockStyle.Fill,
            DropText = dropText
        };

        parent.Controls.Add(overlay);
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
            onFileDropped(filePath);
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

        Wire(parent);
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

    private bool SetSelectedInputFile(string filePath, TextBox input, TextBox output, HashSet<string> allowedExtensions, string fileTypeName, Label statusLabel)
    {
        if (!TryValidateMediaInput(filePath, allowedExtensions, fileTypeName, out string message))
        {
            statusLabel.Text = message;
            ShowCenteredMessage(message, "\uC9C0\uC6D0\uD558\uC9C0 \uC54A\uB294 \uD30C\uC77C", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        SetDroppedInputFile(filePath, input, output);
        return true;
    }

    private static bool TryValidateMediaInput(string filePath, HashSet<string> allowedExtensions, string fileTypeName, out string message)
    {
        message = "";

        if (!File.Exists(filePath))
        {
            message = "\uD30C\uC77C\uC744 \uCC3E\uC744 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.";
            return false;
        }

        string extension = Path.GetExtension(filePath);
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
        {
            string allowed = string.Join(", ", allowedExtensions.OrderBy(x => x));
            message = $"{fileTypeName}\uB9CC \uB123\uC744 \uC218 \uC788\uC2B5\uB2C8\uB2E4. ({allowed})";
            return false;
        }

        return true;
    }

    private void SetDroppedInputFile(string filePath, TextBox input, TextBox output)
    {
        input.Text = filePath;

        string defaultDir = SettingsManager.Settings.DefaultDownloadFolder;
        if (!string.IsNullOrWhiteSpace(defaultDir) && Directory.Exists(defaultDir))
        {
            output.Text = defaultDir;
        }
        else
        {
            output.Text = Path.GetDirectoryName(filePath) ?? "";
        }
    }

    private async Task EnsureRequiredToolsAsync()
    {
        string[] toolNames = { "ffmpeg.exe", "ffprobe.exe", "yt-dlp.exe" };
        bool anyMissing = false;

        foreach (var tool in toolNames)
        {
            string toolPath = SettingsManager.GetToolPath(tool);

            // Validate and clean toolPath
            if (File.Exists(toolPath) && (IsGitLfsPointer(toolPath) || !HasMzHeader(toolPath)))
            {
                try { File.Delete(toolPath); } catch { }
            }

            if (!File.Exists(toolPath)) anyMissing = true;
        }

        if (anyMissing)
        {
            var result = ShowCenteredMessage(
                "프로그램 실행에 필요한 필수 도구(FFmpeg, yt-dlp)가 설치되어 있지 않습니다.\n" +
                "자동으로 다운로드하여 설치하시겠습니까?\n\n" +
                "약 1~2분 정도 걸릴 수 있으며, 완료 후 프로그램이 정상 작동합니다.",
                "필수 도구 설치 안내",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                lblYtDlpStatus.Text = "필수 도구 다운로드 중... (창을 닫지 마세요)";
                try
                {
                    string ffmpegPath = SettingsManager.GetFFmpegPath();
                    string ytDlpPath = SettingsManager.GetYtDlpPath();
                    string toolsDir = Path.GetDirectoryName(ffmpegPath) ?? SettingsManager.UserDataFolder;
                    if (!Directory.Exists(toolsDir)) Directory.CreateDirectory(toolsDir);

                    lblYtDlpStatus.Text = "FFmpeg 다운로드 중... (창을 닫지 마세요)";
                    if (!File.Exists(ffmpegPath) || !File.Exists(SettingsManager.GetFFprobePath()))
                    {
                        // FFmpegDownloader doesn't support downloading to AppData directly if we pass directory. 
                        // But we will ensure the directory is writable.
                        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, toolsDir);
                    }

                    // 3. yt-dlp.exe
                    if (!File.Exists(ytDlpPath))
                    {
                        using var client = new HttpClient();
                        lblYtDlpStatus.Text = "yt-dlp 다운로드 중...";
                        var res = await client.GetAsync("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe");
                        res.EnsureSuccessStatusCode();
                        await using var fs = new FileStream(ytDlpPath, FileMode.Create);
                        await res.Content.CopyToAsync(fs);
                    }

                    Xabe.FFmpeg.FFmpeg.SetExecutablesPath(toolsDir);
                    lblYtDlpStatus.Text = "모든 도구 준비 완료!";
                    ShowCenteredMessage("모든 필수 도구가 성공적으로 설치되었습니다.\n이제 정상적으로 사용할 수 있습니다.", "설치 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    ShowCenteredMessage($"다운로드 중 오류 발생: {ex.Message}\n\n권한 문제가 있을 수 있습니다. 프로그램을 관리자 권한으로 실행하거나 AppData 폴더 쓰기 권한을 확인해 주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblYtDlpStatus.Text = "도구 설치 실패";
                }
            }
        }
    }

    private void SetYtDlpToolStatus(string status)
    {
        if (this.IsDisposed || this.Disposing) return;

        void Apply()
        {
            lblYtDlpStatus.Text = status;
            lblStatus.Text = status;
        }

        try
        {
            if (InvokeRequired) Invoke((MethodInvoker)Apply);
            else Apply();
        }
        catch { }
    }

    private async Task EnsureYtDlpAsync(CancellationToken ct = default, bool forceUpdate = false)
    {
        string ytdlpPath = SettingsManager.GetYtDlpPath();
        bool hadExistingYtDlp = File.Exists(ytdlpPath);
        if (hadExistingYtDlp && forceUpdate && _ytDlpForceUpdateChecked) return;
        if (hadExistingYtDlp && !forceUpdate) return;

        await _ytDlpInstallSemaphore.WaitAsync(ct);
        try
        {
            hadExistingYtDlp = File.Exists(ytdlpPath);
            if (hadExistingYtDlp && forceUpdate && _ytDlpForceUpdateChecked) return;
            if (hadExistingYtDlp && !forceUpdate) return;

            if (forceUpdate) SetYtDlpToolStatus("yt-dlp update check...");

            Directory.CreateDirectory(Path.GetDirectoryName(ytdlpPath) ?? AppDomain.CurrentDomain.BaseDirectory);
            string tempPath = ytdlpPath + ".download";
            string backupPath = ytdlpPath + ".backup";

            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }

            using var client = new HttpClient();
            if (forceUpdate) SetYtDlpToolStatus("yt-dlp downloading latest file...");
            var response = await client.GetAsync("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe", ct);
            response.EnsureSuccessStatusCode();

            await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs, ct);
            }

            if (forceUpdate) SetYtDlpToolStatus("yt-dlp validating new file...");
            await ValidateYtDlpExecutableAsync(tempPath, ct);

            if (forceUpdate) SetYtDlpToolStatus("yt-dlp applying latest file...");
            if (File.Exists(ytdlpPath))
            {
                try { if (File.Exists(backupPath)) File.Delete(backupPath); } catch { }
                File.Replace(tempPath, ytdlpPath, backupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, ytdlpPath);
            }

            if (forceUpdate)
            {
                _ytDlpForceUpdateChecked = true;
                SetYtDlpToolStatus("yt-dlp updated. Starting download...");
            }
        }
        catch (Exception ex)
        {
            try { File.Delete(ytdlpPath + ".download"); } catch { }

            if (forceUpdate && hadExistingYtDlp && File.Exists(ytdlpPath))
            {
                _ytDlpForceUpdateChecked = true;
                SetYtDlpToolStatus("yt-dlp update failed. Continuing with existing file...");
                ReportError($"yt-dlp update failed, existing file kept | URL: {txtYtDlpUrl.Text}", ex);
                return;
            }

            SetYtDlpToolStatus($"yt-dlp prepare failed: {ex.Message}");
            ReportError($"tool download failed | URL: {txtYtDlpUrl.Text}", ex);
            throw;
        }
        finally
        {
            _ytDlpInstallSemaphore.Release();
        }
    }
    private static async Task ValidateYtDlpExecutableAsync(string ytdlpPath, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = ytdlpPath,
            Arguments = "--version",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = psi };
        process.Start();
        string output = await process.StandardOutput.ReadToEndAsync(ct);
        string error = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException("Downloaded yt-dlp.exe validation failed: " + error.Trim());
        }
    }
    private void BtnTab_Click(object sender, EventArgs e)
    {
        var btn = sender as RoundButton;
        if (btn == null) return;

        if (btn == btnTabMiniEdit && !btnTabMiniEdit.Visible)
        {
            SelectMainTab(btnTabYoutube, tabYoutube);
            return;
        }


        if (tabControlMain.SelectedTab == tabMiniEdit && btn != btnTabMiniEdit)
        {
            miniEditorControl.PauseVideo();
        }


        if (tabControlMain.SelectedTab == tabSettings && btn != btnTabSettings)
        {
            ReloadSettingsUI();
        }


        if (tglXPrivateMode.Checked)
        {
            tglXPrivateMode.Checked = false;
        }

        panelMain.SuspendLayout();
        UpdateTabStyles(btn);
        
        if (btn == btnTabYoutube) tabControlMain.SelectedTab = tabYoutube;
        else if (btn == btnTabYtDlp) tabControlMain.SelectedTab = tabYtDlp;
        else if (btn == btnTabCodec) tabControlMain.SelectedTab = tabCodec;
        else if (btn == btnTabWebM) tabControlMain.SelectedTab = tabWebM;
        else if (btn == btnTabAudio) tabControlMain.SelectedTab = tabAudio;
        else if (btn == btnTabMiniEdit) tabControlMain.SelectedTab = tabMiniEdit;
        else if (btn == btnTabSettings) tabControlMain.SelectedTab = tabSettings;
        
        panelMain.ResumeLayout(true);
    }

    private void ConfigureMiniEditorVisibility()
    {
        btnTabMiniEdit.Visible = false;
        btnTabMiniEdit.Enabled = false;

        tabControlMain.SelectedIndexChanged += (s, e) =>
        {
            if (tabControlMain.SelectedTab == tabMiniEdit)
            {
                miniEditorControl.PauseVideo();
                SelectMainTab(btnTabYoutube, tabYoutube);
            }
        };
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

    private static bool LooksLikeYouTubeInput(string url)
    {
        string input = url.Trim();
        if (input.Length == 11 && input.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
        {
            return true;
        }

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            string host = uri.Host.ToLowerInvariant();
            return host == "youtu.be"
                || host == "youtube.com"
                || host.EndsWith(".youtube.com")
                || host == "youtube-nocookie.com"
                || host.EndsWith(".youtube-nocookie.com");
        }

        string lower = input.ToLowerInvariant();
        return lower.Contains("youtube.com/") || lower.Contains("youtu.be/");
    }

    private static string NormalizeYouTubeSingleVideoUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        string input = url.Trim();
        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri)) return input;

        string host = uri.Host.ToLowerInvariant();
        bool isYouTubeHost = host == "youtube.com"
            || host.EndsWith(".youtube.com")
            || host == "youtube-nocookie.com"
            || host.EndsWith(".youtube-nocookie.com");

        if (host == "youtu.be")
        {
            string videoId = uri.AbsolutePath.Trim('/');
            if (string.IsNullOrWhiteSpace(videoId)) return input;
            return new UriBuilder(uri.Scheme, uri.Host, uri.Port, "/" + videoId) { Query = "" }.Uri.ToString();
        }

        if (!isYouTubeHost || !uri.AbsolutePath.Equals("/watch", StringComparison.OrdinalIgnoreCase)) return input;

        string videoIdFromQuery = GetQueryParameterValue(uri.Query, "v");
        if (string.IsNullOrWhiteSpace(videoIdFromQuery)) return input;

        return new UriBuilder(uri.Scheme, uri.Host, uri.Port, uri.AbsolutePath)
        {
            Query = "v=" + Uri.EscapeDataString(videoIdFromQuery)
        }.Uri.ToString();
    }

    private static string GetQueryParameterValue(string query, string key)
    {
        if (string.IsNullOrWhiteSpace(query)) return "";
        string trimmed = query.TrimStart('?');
        foreach (string part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] pair = part.Split('=', 2);
            string name = Uri.UnescapeDataString(pair[0].Replace("+", " "));
            if (!name.Equals(key, StringComparison.OrdinalIgnoreCase)) continue;

            return pair.Length > 1 ? Uri.UnescapeDataString(pair[1].Replace("+", " ")) : "";
        }

        return "";
    }

    private bool IsYoutubeUrlInFlight(string url)
    {
        string normalizedUrl = NormalizeYouTubeSingleVideoUrl(url);
        bool SameUrl(DownloadJob job) => string.Equals(NormalizeYouTubeSingleVideoUrl(job.Url), normalizedUrl, StringComparison.OrdinalIgnoreCase);

        if (_downloadQueue.Any(SameUrl)) return true;
        lock (_activeJobs)
        {
            return _activeJobs.Any(SameUrl);
        }
    }

    private bool IsYtDlpUrlInFlight(string url, int playlistItemIndex = 0)
    {
        string normalizedUrl = NormalizeYouTubeSingleVideoUrl(url);
        bool SameUrl(YtDlpDownloadJob job) =>
            job.PlaylistItemIndex == playlistItemIndex &&
            string.Equals(NormalizeYouTubeSingleVideoUrl(job.Url), normalizedUrl, StringComparison.OrdinalIgnoreCase);

        if (_ytDlpDownloadQueue.Any(SameUrl)) return true;
        lock (_ytDlpQueueLock)
        {
            return _activeYtDlpJobs.Any(SameUrl);
        }
    }

    private static bool IsInvalidYouTubeInputError(Exception ex)
    {
        return ex.Message.Contains("Invalid YouTube video ID or URL", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldLoadYouTubeInfoWithYtDlp(Exception ex)
    {
        string message = FlattenExceptionMessage(ex);
        string lower = message.ToLowerInvariant();

        return lower.Contains("cipher manifest") ||
               lower.Contains("failed to extract") ||
               lower.Contains("signature") ||
               lower.Contains("player") ||
               lower.Contains("manifest") ||
               lower.Contains("video is unavailable") ||
               lower.Contains("sign in to confirm your age") ||
               lower.Contains("requested format is not available") ||
               lower.Contains("no video formats");
    }

    private void GuideToWebsiteDownloadTab(string url)
    {
        txtYtDlpUrl.Text = url;
        lblVideoTitle.Text = "유튜브 탭은 유튜브 주소만 사용할 수 있습니다.";
        lblYtDlpStatus.Text = "웹 사이트 영상 다운로드 탭에서 주소를 확인해보세요.";

        panelMain.SuspendLayout();
        UpdateTabStyles(btnTabYtDlp);
        tabControlMain.SelectedTab = tabYtDlp;
        panelMain.ResumeLayout(true);
        txtYtDlpUrl.Focus();

        ShowCenteredMessage(
            "이 주소는 유튜브 주소가 아닌 것 같습니다.\n\n웹 사이트 영상은 [웹 사이트 영상 다운] 탭에서 다운로드를 시도해 보세요.\n\n주소는 해당 탭에 미리 넣어두었습니다.",
            "웹사이트 다운로드 안내",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ShowYoutubeInputGuide()
    {
        cmbQuality.Enabled = false;
        lblVideoTitle.Text = "주소를 다시 확인해 주세요.";

        ShowCenteredMessage(
            "유튜브 주소를 확인해 주세요.\n\n유튜브가 아닌 웹사이트 영상 주소라면 [웹 사이트 영상 다운] 탭에서 시도해 보세요.",
            "주소 확인 안내",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void SelectMainTab(RoundButton button, TabPage tabPage)
    {
        panelMain.SuspendLayout();
        UpdateTabStyles(button);
        tabControlMain.SelectedTab = tabPage;
        panelMain.ResumeLayout(true);
    }

    private void RouteToWebsiteDownload(string url, bool startDownload)
    {
        txtYtDlpUrl.Text = url;
        chkYtDlpDownloadSubtitles.Checked = chkYoutubeDownloadSubtitles.Checked;
        if (cmbYtDlpSubtitleLanguage != null && cmbYoutubeSubtitleLanguage != null)
        {
            SelectComboText(cmbYtDlpSubtitleLanguage, cmbYoutubeSubtitleLanguage.Text);
        }
        lblVideoTitle.Text = "유튜브가 아닌 주소라 웹 사이트 영상 다운 탭으로 이동합니다.";
        lblYtDlpStatus.Text = startDownload
            ? "유튜브가 아닌 주소라 웹 사이트 영상 다운로드를 바로 시작합니다."
            : "웹 사이트 영상 다운 탭에서 주소를 확인해 보세요.";

        SelectMainTab(btnTabYtDlp, tabYtDlp);
        txtYtDlpUrl.Focus();

        if (startDownload)
        {
            BeginInvoke(new Action(() => BtnYtDlpRun_Click(this, EventArgs.Empty)));
        }
    }

    private async Task RouteToYoutubeDownloadAsync(string url)
    {
        txtUrl.Text = url;
        chkYoutubeDownloadSubtitles.Checked = chkYtDlpDownloadSubtitles.Checked;
        if (cmbYoutubeSubtitleLanguage != null && cmbYtDlpSubtitleLanguage != null)
        {
            SelectComboText(cmbYoutubeSubtitleLanguage, cmbYtDlpSubtitleLanguage.Text);
        }
        lblYtDlpStatus.Text = "유튜브 주소는 유튜브 다운로더 탭으로 이동합니다.";
        lblStatus.Text = "유튜브 영상 정보를 확인한 뒤 다운로드 대기열에 추가합니다.";

        SelectMainTab(btnTabYoutube, tabYoutube);
        txtUrl.Focus();
        _currentVideo = null!;
        _streamManifest = null!;
        cmbQuality.Items.Clear();
        cmbQuality.SelectedIndex = -1;
        cmbQuality.Enabled = false;
        picThumbnail.Image = null;
        BtnLoad_Click(this, EventArgs.Empty);

        for (int i = 0; i < 150; i++)
        {
            await Task.Delay(200);
            if (_currentVideo != null && _streamManifest != null && cmbQuality.SelectedItem != null)
            {
                BtnAddQueue_Click(this, EventArgs.Empty);
                return;
            }

            if (btnLoad.Enabled && (_currentVideo == null || _streamManifest == null))
            {
                return;
            }
        }
    }

    private void EnterWidgetMode(bool saveSetting)
    {
        SettingsManager.Settings.EnableWidgetMode = true;
        if (saveSetting) SettingsManager.Save();
        if (btnTopWidgetMode != null) btnTopWidgetMode.Checked = true;

        if (chkEnableWidgetMode != null && !chkEnableWidgetMode.Checked)
        {
            _updatingWidgetModeCheckbox = true;
            chkEnableWidgetMode.Checked = true;
            _updatingWidgetModeCheckbox = false;
        }

        try
        {
            _loginFolderAnimationTimer?.Stop();
            if (_isLoginBrowserMode || panelXBrowser.Visible)
            {
                CloseLoginBrowserPanel();
            }

            if (webViewX.CoreWebView2 != null)
            {
                webViewX.CoreWebView2.Stop();
                webViewX.CoreWebView2.Navigate("about:blank");
            }
        }
        catch { }

        ShowDownloadWidget();
        ShowInTaskbar = false;
        Hide();
        BeginInvoke(new Action(async () =>
        {
            await Task.Delay(250);
            RememberWidgetTargetWindow();
        }));
    }

    private void ExitWidgetMode(bool saveSetting)
    {
        SettingsManager.Settings.EnableWidgetMode = false;
        if (saveSetting) SettingsManager.Save();

        if (chkEnableWidgetMode != null && chkEnableWidgetMode.Checked)
        {
            _updatingWidgetModeCheckbox = true;
            chkEnableWidgetMode.Checked = false;
            _updatingWidgetModeCheckbox = false;
        }

        HideDownloadWidget();
        if (btnTopWidgetMode != null) btnTopWidgetMode.Checked = false;
        ShowInTaskbar = true;
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Show();
        Activate();
    }

    private void ShowMainFromWidget()
    {
        SettingsManager.Settings.EnableWidgetMode = false;
        if (chkEnableWidgetMode != null && chkEnableWidgetMode.Checked)
        {
            _updatingWidgetModeCheckbox = true;
            chkEnableWidgetMode.Checked = false;
            _updatingWidgetModeCheckbox = false;
        }

        HideDownloadWidget();
        if (btnTopWidgetMode != null) btnTopWidgetMode.Checked = false;
        ShowInTaskbar = true;
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Show();
        Activate();
    }

    private void ShowDownloadWidget()
    {
        if (_downloadWidgetForm == null || _downloadWidgetForm.IsDisposed)
        {
            _downloadWidgetForm = new DownloadWidgetForm(
                HandleWidgetDownloadAsync,
                ShowMainFromWidget,
                () => ExitWidgetMode(saveSetting: false),
                Application.Exit,
                SaveWidgetLocation);
        }

        Point location = new Point(SettingsManager.Settings.WidgetLocationX, SettingsManager.Settings.WidgetLocationY);
        if (!DownloadWidgetForm.IsLocationVisible(location, _downloadWidgetForm.Size))
        {
            location = DownloadWidgetForm.GetDefaultLocation(_downloadWidgetForm.Size);
        }

        _downloadWidgetForm.Location = location;
        _downloadWidgetForm.Show();
        _downloadWidgetForm.BringToFront();
    }

    private void HideDownloadWidget()
    {
        if (_downloadWidgetForm == null || _downloadWidgetForm.IsDisposed) return;
        _downloadWidgetForm.Hide();
    }

    private void SaveWidgetLocation(Point location)
    {
        SettingsManager.Settings.WidgetLocationX = location.X;
        SettingsManager.Settings.WidgetLocationY = location.Y;
        SettingsManager.Save();
    }

    private void ShowVideoPickerFromWidget(string sourceUrl, List<DetectedVideoItem> items)
    {
        _downloadWidgetForm?.SetProgress(null);
        _downloadWidgetForm?.ShowToast("\uC5EC\uB7EC \uC601\uC0C1\uC744 \uCC3E\uC558\uC2B5\uB2C8\uB2E4.");
        ShowMainFromWidget();
        _returnToWidgetAfterVideoPick = true;
        ShowVideoPicker(sourceUrl, items);
    }

    private void ShowVideoPicker(string sourceUrl, List<DetectedVideoItem> items)
    {
        ConfigureVideoPickerTab();
        _detectedVideoItems = items;

        if (lblVideoPickerHint != null)
        {
            lblVideoPickerHint.Text = $"{items.Count}\uAC1C\uC758 \uC601\uC0C1\uC774 \uAC10\uC9C0\uB418\uC5C8\uC2B5\uB2C8\uB2E4. \uC6D0\uD558\uB294 \uC601\uC0C1\uC744 \uACE0\uB974\uAC70\uB098 \uBAA8\uB450 \uB2E4\uC6B4\uB85C\uB4DC\uD558\uC138\uC694.";
        }

        imgVideoPickerThumbs?.Images.Clear();
        lvVideoPicker?.Items.Clear();

        if (lvVideoPicker != null && imgVideoPickerThumbs != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                imgVideoPickerThumbs.Images.Add(CreateVideoPickerPlaceholder(i + 1));

                var row = new ListViewItem(item.Title, i)
                {
                    Tag = item
                };
                row.SubItems.Add(item.Duration);
                row.SubItems.Add(item.Url);
                lvVideoPicker.Items.Add(row);
            }

            if (lvVideoPicker.Items.Count > 0) lvVideoPicker.Items[0].Selected = true;
        }

        tabControlMain.SelectedTab = tabVideoPicker;
        UpdateTabStyles(btnTabYtDlp);
        _ = LoadVideoPickerThumbnailsAsync(items);
    }

    private async Task LoadVideoPickerThumbnailsAsync(List<DetectedVideoItem> items)
    {
        if (imgVideoPickerThumbs == null || lvVideoPicker == null) return;

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        for (int i = 0; i < items.Count; i++)
        {
            string thumbnail = items[i].ThumbnailUrl;
            if (string.IsNullOrWhiteSpace(thumbnail)) continue;

            try
            {
                byte[] bytes = await client.GetByteArrayAsync(thumbnail);
                using var ms = new MemoryStream(bytes);
                using var image = Image.FromStream(ms);
                var thumb = new Bitmap(imgVideoPickerThumbs.ImageSize.Width, imgVideoPickerThumbs.ImageSize.Height);
                using (var g = Graphics.FromImage(thumb))
                {
                    g.Clear(Color.FromArgb(241, 245, 249));
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle bounds = GetZoomBounds(image.Size, new Rectangle(Point.Empty, thumb.Size));
                    g.DrawImage(image, bounds);
                }

                if (IsDisposed || imgVideoPickerThumbs.Images.Count <= i) return;
                imgVideoPickerThumbs.Images[i] = thumb;
                lvVideoPicker.Invalidate();
            }
            catch { }
        }
    }

    private static Bitmap CreateVideoPickerPlaceholder(int index)
    {
        var bitmap = new Bitmap(96, 54);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.FromArgb(226, 232, 240));
        using var brush = new SolidBrush(Color.FromArgb(100, 116, 139));
        using var font = new Font("Segoe UI", 10F, FontStyle.Bold);
        string text = index.ToString();
        SizeF size = g.MeasureString(text, font);
        g.DrawString(text, font, brush, (bitmap.Width - size.Width) / 2, (bitmap.Height - size.Height) / 2);
        return bitmap;
    }

    private static Rectangle GetZoomBounds(Size imageSize, Rectangle bounds)
    {
        if (imageSize.Width <= 0 || imageSize.Height <= 0 || bounds.Width <= 0 || bounds.Height <= 0) return bounds;
        float ratio = Math.Min((float)bounds.Width / imageSize.Width, (float)bounds.Height / imageSize.Height);
        int width = Math.Max(1, (int)(imageSize.Width * ratio));
        int height = Math.Max(1, (int)(imageSize.Height * ratio));
        return new Rectangle(
            bounds.Left + (bounds.Width - width) / 2,
            bounds.Top + (bounds.Height - height) / 2,
            width,
            height);
    }

    private async Task<List<DetectedVideoItem>> DetectVideoCandidatesForSelectionAsync(string url)
    {
        var result = new List<DetectedVideoItem>();

        if (!IsHttpUrl(url) || LooksLikeYouTubeInput(url))
            return result;

        string ytDlpPath = SettingsManager.GetYtDlpPath();
        if (string.IsNullOrWhiteSpace(ytDlpPath) || !File.Exists(ytDlpPath))
            return result;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(18));
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            process.StartInfo.ArgumentList.Add("--ignore-config");
            process.StartInfo.ArgumentList.Add("--flat-playlist");
            process.StartInfo.ArgumentList.Add("--dump-single-json");
            process.StartInfo.ArgumentList.Add("--no-warnings");
            process.StartInfo.ArgumentList.Add("--encoding");
            process.StartInfo.ArgumentList.Add("utf-8");
            process.StartInfo.ArgumentList.Add(url);

            process.Start();
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(cts.Token);

            string json = await outputTask;
            _ = await errorTask;
            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(json))
                return result;

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("entries", out var entries) || entries.ValueKind != JsonValueKind.Array)
                return result;

            int index = 1;
            foreach (var entry in entries.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object) continue;

                string itemUrl = GetDetectedVideoUrl(entry);
                if (!IsHttpUrl(itemUrl)) continue;

                string title = GetJsonString(entry, "title", "fulltitle", "id") ?? $"\uC601\uC0C1 {index}";
                string duration = GetJsonString(entry, "duration_string") ?? FormatDetectedDuration(entry);
                string thumbnail = GetJsonString(entry, "thumbnail") ?? GetLastThumbnailUrl(entry);

                result.Add(new DetectedVideoItem
                {
                    Title = title.Trim(),
                    Url = itemUrl.Trim(),
                    Duration = duration,
                    ThumbnailUrl = thumbnail,
                    SourcePageUrl = url,
                    PlaylistIndex = GetJsonInt(entry, "playlist_index")
                });
                index++;
            }
        }
        catch
        {
            return new List<DetectedVideoItem>();
        }

        return result
            .GroupBy(x => x.Url, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .Take(50)
            .ToList();
    }

    private static string? GetJsonString(JsonElement element, params string[] names)
    {
        foreach (string name in names)
        {
            if (!element.TryGetProperty(name, out var value)) continue;
            if (value.ValueKind == JsonValueKind.String)
            {
                string? text = value.GetString();
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
            else if (value.ValueKind == JsonValueKind.Number)
            {
                return value.ToString();
            }
        }

        return null;
    }

    private static int GetJsonInt(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value)) return 0;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number)) return number;
        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number)) return number;
        return 0;
    }

    private static string GetDetectedVideoUrl(JsonElement entry)
    {
        string itemUrl = GetJsonString(entry, "url") ?? "";
        if (IsHttpUrl(itemUrl)) return itemUrl;

        itemUrl = GetFirstNestedUrl(entry, "requested_downloads");
        if (IsHttpUrl(itemUrl)) return itemUrl;

        itemUrl = GetFirstNestedUrl(entry, "formats");
        if (IsHttpUrl(itemUrl)) return itemUrl;

        itemUrl = GetJsonString(entry, "webpage_url") ?? "";
        return IsHttpUrl(itemUrl) ? itemUrl : "";
    }

    private static string GetFirstNestedUrl(JsonElement entry, string propertyName)
    {
        if (!entry.TryGetProperty(propertyName, out var items) || items.ValueKind != JsonValueKind.Array)
            return "";

        foreach (var item in items.EnumerateArray())
        {
            string? url = GetJsonString(item, "url");
            if (IsHttpUrl(url ?? "")) return url!;
        }

        return "";
    }

    private static string GetLastThumbnailUrl(JsonElement entry)
    {
        if (!entry.TryGetProperty("thumbnails", out var thumbnails) || thumbnails.ValueKind != JsonValueKind.Array)
            return "";

        string last = "";
        foreach (var thumbnail in thumbnails.EnumerateArray())
        {
            string? url = GetJsonString(thumbnail, "url");
            if (!string.IsNullOrWhiteSpace(url)) last = url;
        }

        return last;
    }

    private static string FormatDetectedDuration(JsonElement entry)
    {
        if (!entry.TryGetProperty("duration", out var duration) || duration.ValueKind != JsonValueKind.Number)
            return "-";

        double seconds = duration.TryGetDouble(out double value) ? value : 0;
        if (seconds <= 0) return "-";

        var time = TimeSpan.FromSeconds(seconds);
        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{time.Minutes:00}:{time.Seconds:00}";
    }

    private void BtnVideoPickerDownloadAll_Click(object? sender, EventArgs e)
    {
        QueueDetectedVideoItems(_detectedVideoItems);
    }

    private void BtnVideoPickerDownloadSelected_Click(object? sender, EventArgs e)
    {
        if (lvVideoPicker == null || lvVideoPicker.SelectedItems.Count == 0)
        {
            ShowCenteredMessage(
                "\uB2E4\uC6B4\uB85C\uB4DC\uD560 \uC601\uC0C1\uC744 \uC120\uD0DD\uD574 \uC8FC\uC138\uC694.",
                "\uC601\uC0C1 \uC120\uD0DD",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var selected = lvVideoPicker.SelectedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag as DetectedVideoItem)
            .Where(item => item != null)
            .Cast<DetectedVideoItem>()
            .ToList();

        QueueDetectedVideoItems(selected);
    }

    private void QueueDetectedVideoItems(IEnumerable<DetectedVideoItem> items)
    {
        var list = items
            .Where(item => item != null && IsHttpUrl(item.Url))
            .GroupBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        if (list.Count == 0)
        {
            ShowCenteredMessage(
                "\uB2E4\uC6B4\uB85C\uB4DC\uD560 \uC601\uC0C1 \uC8FC\uC18C\uB97C \uCC3E\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4.",
                "\uC601\uC0C1 \uC120\uD0DD",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        int added = 0;
        string lastRejectReason = "";
        foreach (var item in list)
        {
            string queueUrl = item.PlaylistIndex > 0 && IsHttpUrl(item.SourcePageUrl) ? item.SourcePageUrl : item.Url;
            if (EnqueueYtDlpDownload(queueUrl, allowFolderPrompt: false, out string rejectReason, item.PlaylistIndex, item.Title))
            {
                added++;
            }
            else if (!string.IsNullOrWhiteSpace(rejectReason))
            {
                lastRejectReason = rejectReason;
            }
        }

        if (added > 0)
        {
            lblYtDlpStatus.Text = $"{added}\uAC1C\uC758 \uC601\uC0C1\uC744 \uB300\uAE30\uC5F4\uC5D0 \uCD94\uAC00\uD588\uC2B5\uB2C8\uB2E4.";
            CloseVideoPicker(returnToWidget: _returnToWidgetAfterVideoPick);
            return;
        }

        ShowCenteredMessage(
            string.IsNullOrWhiteSpace(lastRejectReason)
                ? "\uB300\uAE30\uC5F4\uC5D0 \uCD94\uAC00\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4."
                : lastRejectReason,
            "\uC601\uC0C1 \uC120\uD0DD",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private void CloseVideoPicker(bool returnToWidget)
    {
        _returnToWidgetAfterVideoPick = false;
        _detectedVideoItems.Clear();
        lvVideoPicker?.Items.Clear();
        imgVideoPickerThumbs?.Images.Clear();

        SelectMainTab(btnTabYtDlp, tabYtDlp);

        if (returnToWidget)
        {
            BeginInvoke(new Action(() => EnterWidgetMode(saveSetting: false)));
        }
    }

    private async Task HandleWidgetDownloadAsync()
    {
        try
        {
            await HandleWidgetDownloadAsyncCore();
        }
        finally
        {
            _downloadWidgetForm?.SetBusy(false);
        }
    }

    private async Task HandleWidgetDownloadAsyncCore()
    {
        _downloadWidgetForm?.SetBusy(true);
        _downloadWidgetForm?.SetProgress(null);
        _downloadWidgetForm?.SetStatus("현재 브라우저 주소 확인 중...");
        RememberWidgetTargetWindow();
        string text = await TryGetActivePageUrlAsync();
        if (!IsHttpUrl(text)) text = GetClipboardText();

        if (!IsHttpUrl(text))
        {
            _downloadWidgetForm?.SetProgress(null);
            _downloadWidgetForm?.SetStatus("다운로드할 페이지를 브라우저에서 연 뒤 다시 눌러 주세요. 감지가 안 되면 URL을 복사한 뒤 눌러 주세요.");
            return;
        }

        _downloadWidgetForm?.SetProgress(0);
        _downloadWidgetForm?.SetStatus("다운로드 대기열에 추가 중...");

        try
        {
            if (LooksLikeYouTubeInput(text))
            {
                bool youtubeWasRunning = _isDownloading || !_downloadQueue.IsEmpty;
                int itemCountBefore = lvQueue.Items.Count;
                await RouteToYoutubeDownloadAsync(text);
                if (lvQueue.Items.Count <= itemCountBefore)
                {
                    _downloadWidgetForm?.SetProgress(null);
                    _downloadWidgetForm?.SetStatus("\uC601\uC0C1 \uC815\uBCF4\uB97C \uAC00\uC838\uC624\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4. \uC2E4\uC81C \uC601\uC0C1 \uD398\uC774\uC9C0\uC5D0\uC11C \uB2E4\uC2DC \uB20C\uB7EC\uC8FC\uC138\uC694.");
                    _downloadWidgetForm?.ShowToast("\uC601\uC0C1\uC744 \uCC3E\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4.");
                    return;
                }
                _downloadWidgetForm?.ShowToast("\uB300\uAE30\uC5F4\uC5D0 \uCD94\uAC00\uB418\uC5C8\uC2B5\uB2C8\uB2E4.");
                _downloadWidgetForm?.SetStatus(youtubeWasRunning
                    ? "\uB300\uAE30\uC5F4\uC5D0 \uCD94\uAC00\uB418\uC5C8\uC2B5\uB2C8\uB2E4. \uC55E \uC791\uC5C5\uC774 \uB05D\uB098\uBA74 \uC21C\uC11C\uB300\uB85C \uB2E4\uC6B4\uB85C\uB4DC\uD569\uB2C8\uB2E4."
                    : "\uC720\uD29C\uBE0C \uB2E4\uC6B4\uB85C\uB4DC\uB97C \uC2DC\uC791\uD588\uC2B5\uB2C8\uB2E4.");
                return;
            }

            var detectedItems = await DetectVideoCandidatesForSelectionAsync(text);
            if (detectedItems.Count > 1)
            {
                ShowVideoPickerFromWidget(text, detectedItems);
                return;
            }

            bool wasRunning = _isYtDlpQueueRunning || !_ytDlpDownloadQueue.IsEmpty;
            if (!EnqueueYtDlpDownload(text, allowFolderPrompt: false, out string rejectReason))
            {
                if (!string.IsNullOrWhiteSpace(rejectReason))
                {
                    _downloadWidgetForm?.SetProgress(null);
                    _downloadWidgetForm?.SetStatus(rejectReason);
                    return;
                }
                _downloadWidgetForm?.SetProgress(null);
                _downloadWidgetForm?.SetStatus("대기열에 추가하지 못했습니다. 앱을 열어 저장 위치를 확인해 주세요.");
                return;
            }

            _downloadWidgetForm?.ShowToast("\uB300\uAE30\uC5F4\uC5D0 \uCD94\uAC00\uB418\uC5C8\uC2B5\uB2C8\uB2E4.");
            _downloadWidgetForm?.SetStatus(wasRunning
                ? "대기열에 추가했습니다. 앞 작업이 끝나면 순서대로 다운로드합니다."
                : "다운로드를 시작했습니다.");
        }
        catch (Exception ex)
        {
            ReportError($"위젯 다운로드 추가 실패 | URL: {text}", ex);
            _downloadWidgetForm?.SetProgress(null);
            _downloadWidgetForm?.SetStatus("대기열 추가 실패. 앱을 열어 오류 내용을 확인해 주세요.");
        }
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Tab) || keyData == (Keys.Control | Keys.Shift | Keys.Tab))
        {
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private static string GetClipboardText()
    {
        try
        {
            return Clipboard.GetText(TextDataFormat.Text).Trim();
        }
        catch
        {
            return "";
        }
    }

    private void RememberWidgetTargetWindow()
    {
        IntPtr foreground = GetForegroundWindow();
        if (IsWidgetBrowserWindow(foreground))
        {
            _lastWidgetTargetWindow = foreground;
            return;
        }

        IntPtr browserWindow = FindWidgetBrowserWindow();
        if (browserWindow != IntPtr.Zero)
        {
            _lastWidgetTargetWindow = browserWindow;
        }
    }

    private void FocusWidgetTargetWindow()
    {
        if (!IsWidgetBrowserWindow(_lastWidgetTargetWindow))
        {
            _lastWidgetTargetWindow = FindWidgetBrowserWindow();
        }

        if (_lastWidgetTargetWindow == IntPtr.Zero) return;

        try
        {
            if (IsIconic(_lastWidgetTargetWindow)) ShowWindow(_lastWidgetTargetWindow, SW_RESTORE);
            SetForegroundWindow(_lastWidgetTargetWindow);
        }
        catch { }
    }

    private bool IsWidgetBrowserWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || hwnd == Handle || (_downloadWidgetForm != null && hwnd == _downloadWidgetForm.Handle))
            return false;

        try
        {
            if (!IsWindow(hwnd) || !IsWindowVisible(hwnd) || GetWindowTextLength(hwnd) <= 0) return false;

            GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == 0 || processId == (uint)Environment.ProcessId) return false;

            using Process process = Process.GetProcessById((int)processId);
            return WidgetBrowserProcessNames.Contains(process.ProcessName);
        }
        catch
        {
            return false;
        }
    }

    private IntPtr FindWidgetBrowserWindow()
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows((hwnd, lParam) =>
        {
            if (!IsWidgetBrowserWindow(hwnd)) return true;

            found = hwnd;
            return false;
        }, IntPtr.Zero);

        return found;
    }

    private async Task<string> TryGetActivePageUrlAsync()
    {
        IDataObject? previousClipboard = null;
        try
        {
            previousClipboard = Clipboard.GetDataObject();
        }
        catch { }

        try
        {
            FocusWidgetTargetWindow();
            await Task.Delay(120);
            SendKeys.SendWait("^l");
            await Task.Delay(140);
            SendKeys.SendWait("^c");
            await Task.Delay(220);

            string captured = GetClipboardText();
            if (IsHttpUrl(captured))
            {
                try
                {
                    if (previousClipboard != null) Clipboard.SetDataObject(previousClipboard, true);
                }
                catch { }

                return captured;
            }

            SendKeys.SendWait("%d");
            await Task.Delay(140);
            SendKeys.SendWait("^c");
            await Task.Delay(220);

            captured = GetClipboardText();
            if (IsHttpUrl(captured))
            {
                try
                {
                    if (previousClipboard != null) Clipboard.SetDataObject(previousClipboard, true);
                }
                catch { }

                return captured;
            }
        }
        catch { }

        try
        {
            if (previousClipboard != null) Clipboard.SetDataObject(previousClipboard, true);
        }
        catch { }

        return "";
    }

    private void Notify(string title, string text)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(() => Notify(title, text)));
            return;
        }

        if (SettingsManager.Settings.ShowNotifications)
        {
            notifyIconApp.ShowBalloonTip(3000, title, text, ToolTipIcon.None);
        }
    }

    private DialogResult ShowCenteredMessage(string text)
    {
        return ShowCenteredMessage(text, this.Text, MessageBoxButtons.OK, MessageBoxIcon.None);
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
        if (InvokeRequired)
        {
            return (DialogResult)Invoke(new Func<DialogResult>(() => ShowCenteredMessage(text, caption, buttons, icon)));
        }

        IWin32Window owner = this;
        bool useWidgetOwner = _downloadWidgetForm != null && !_downloadWidgetForm.IsDisposed && _downloadWidgetForm.Visible && !Visible;
        Rectangle ownerBounds;

        if (useWidgetOwner)
        {
            owner = _downloadWidgetForm!;
            ownerBounds = Screen.FromControl(_downloadWidgetForm!).WorkingArea;
        }
        else
        {
            ownerBounds = WindowState == FormWindowState.Minimized ? RestoreBounds : Bounds;
        }

        if (!useWidgetOwner && WindowState == FormWindowState.Maximized)
        {
            ownerBounds = Screen.FromControl(this).WorkingArea;
        }
        if (ownerBounds.Width <= 0 || ownerBounds.Height <= 0)
        {
            ownerBounds = useWidgetOwner && _downloadWidgetForm != null
                ? Screen.FromControl(_downloadWidgetForm).WorkingArea
                : Screen.FromControl(this).WorkingArea;
        }

        Point? widgetLocationBeforeMessage = null;
        if (useWidgetOwner && _downloadWidgetForm != null && !_downloadWidgetForm.IsDisposed)
        {
            widgetLocationBeforeMessage = _downloadWidgetForm.Location;
        }

        _messageBoxOwnerBounds = ownerBounds;
        _messageBoxHookProc = CenterMessageBoxHook;
        _messageBoxHook = SetWindowsHookEx(WH_CBT, _messageBoxHookProc, IntPtr.Zero, GetCurrentThreadId());
        try
        {
            if (useWidgetOwner)
            {
                _downloadWidgetForm?.BringToFront();
            }

            return MessageBox.Show(owner, text, caption, buttons, icon);
        }
        finally
        {
            if (_messageBoxHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_messageBoxHook);
                _messageBoxHook = IntPtr.Zero;
            }

            if (widgetLocationBeforeMessage.HasValue && _downloadWidgetForm != null && !_downloadWidgetForm.IsDisposed)
            {
                _downloadWidgetForm.Location = widgetLocationBeforeMessage.Value;
                SaveWidgetLocation(widgetLocationBeforeMessage.Value);
                _downloadWidgetForm.BringToFront();
            }
        }
    }

    private static IntPtr CenterMessageBoxHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode == HCBT_ACTIVATE && wParam != IntPtr.Zero)
        {
            try
            {
                if (GetWindowRect(wParam, out RECT rect))
                {
                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;
                    int x = _messageBoxOwnerBounds.Left + (_messageBoxOwnerBounds.Width - width) / 2;
                    int y = _messageBoxOwnerBounds.Top + (_messageBoxOwnerBounds.Height - height) / 2;

                    Rectangle screen = Screen.FromRectangle(_messageBoxOwnerBounds).WorkingArea;
                    x = Math.Max(screen.Left, Math.Min(x, screen.Right - width));
                    y = Math.Max(screen.Top, Math.Min(y, screen.Bottom - height));

                    SetWindowPos(wParam, HWND_TOP, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
                }
            }
            catch { }

            if (_messageBoxHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_messageBoxHook);
                _messageBoxHook = IntPtr.Zero;
            }
        }

        return CallNextHookEx(_messageBoxHook, nCode, wParam, lParam);
    }

    private static string FlattenExceptionMessage(Exception ex)
    {
        var sb = new StringBuilder();
        Exception? current = ex;
        while (current != null)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(current.Message);
            }
            current = current.InnerException;
        }

        return sb.ToString();
    }

    private static string GetDownloadFailureCause(Exception ex)
    {
        string message = FlattenExceptionMessage(ex);
        string lower = message.ToLowerInvariant();

        if (lower.Contains("403") || lower.Contains("forbidden"))
            return "403 차단";

        if (lower.Contains("401") || lower.Contains("login") || lower.Contains("sign in") ||
            lower.Contains("cookies") || message.Contains("로그인") || message.Contains("비공개") || message.Contains("인증"))
            return "로그인 필요";

        if (lower.Contains("ffmpeg") || lower.Contains("conversion failed") ||
            message.Contains("변환 실패") || message.Contains("MP4 변환 실패"))
            return "ffmpeg 변환 실패";

        if (lower.Contains("requested format is not available"))
            return "사용 가능한 영상 포맷 없음";

        if (lower.Contains("m3u8") || lower.Contains("manifest") || lower.Contains("hls") ||
            message.Contains("스트림 URL") || message.Contains("영상 정보를 자동") || message.Contains("재생 정보"))
            return "m3u8 추출 실패";

        if (lower.Contains("unsupported url") || lower.Contains("unable to extract") ||
            lower.Contains("extractor") || lower.Contains("no video formats") || lower.Contains("not supported"))
            return "사이트 구조 변경 가능성";

        if (lower.Contains("timed out") || lower.Contains("timeout") || message.Contains("시간"))
            return "네트워크 시간 초과";

        if (lower.Contains("404") || lower.Contains("not found"))
            return "영상 삭제 또는 주소 오류";

        return "원인 확인 필요";
    }

    private static string GetDownloadFailureHint(string cause)
    {
        return cause switch
        {
            "m3u8 추출 실패" => "영상 주소를 찾지 못했습니다. 사이트 구조가 바뀌었거나 플레이어 로딩이 막혔을 수 있습니다.",
            "403 차단" => "사이트가 다운로드 요청을 차단했습니다. 로그인/쿠키, Referer, 지역 제한 또는 봇 차단 가능성이 있습니다.",
            "ffmpeg 변환 실패" => "영상 조각은 받았지만 MP4 병합 또는 변환에 실패했습니다. ffmpeg, 파일 경로, 원본 스트림 상태를 확인해야 합니다.",
            "로그인 필요" => "로그인이 필요한 영상일 수 있습니다. 로그인 후 다운 화면에서 로그인한 뒤 다시 시도해 보세요.",
            "사용 가능한 영상 포맷 없음" => "영상에서 받을 수 있는 포맷을 찾지 못했습니다. 로그인 권한, 라이브 상태, yt-dlp 최신 구조 대응 여부를 확인해야 합니다.",
            "사이트 구조 변경 가능성" => "현재 추출 방식이 사이트의 최신 구조와 맞지 않을 수 있습니다.",
            "네트워크 시간 초과" => "사이트 응답이 느리거나 네트워크가 불안정합니다. 잠시 뒤 다시 시도해 보세요.",
            "영상 삭제 또는 주소 오류" => "영상이 삭제되었거나 주소가 잘못되었을 수 있습니다.",
            _ => "상세 오류를 보고 원인을 확인해야 합니다. (제작자에게 문의해 주세요)"
        };
    }

    private static string BuildDownloadFailureMessage(string url, Exception ex)
    {
        string cause = GetDownloadFailureCause(ex);
        string detail = CleanErrorDetailForDisplay(FlattenExceptionMessage(ex).Trim());
        if (detail.Length > 1200)
        {
            detail = detail.Substring(0, 1200) + "...";
        }

        return $"다운로드 실패 원인: {cause}\n\n{GetDownloadFailureHint(cause)}\n\nURL: {url}\n\n상세 오류:\n{detail}";
    }

    private static string CleanErrorDetailForDisplay(string detail)
    {
        if (string.IsNullOrWhiteSpace(detail)) return "상세 오류가 비어 있습니다.";
        if (!LooksLikeBrokenKorean(detail)) return detail;

        return "오류 원문이 깨져 표시될 수 있어 생략했습니다. 위의 실패 원인과 안내를 기준으로 확인해 주세요.";
    }
    private static bool LooksLikeBrokenKorean(string text)
    {
        foreach (char ch in text)
        {
            if ((ch >= '\u4E00' && ch <= '\u9FFF') || (ch >= '\uF900' && ch <= '\uFAFF'))
                return true;
        }

        for (int i = 0; i < text.Length - 1; i++)
        {
            if (text[i] != '?') continue;
            char next = text[i + 1];
            if ((next >= '\u3130' && next <= '\u318F') || (next >= '\uAC00' && next <= '\uD7AF'))
                return true;
        }

        return false;
    }

    private void LoadAppIcon()
    {
        try 
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            string[] paths = { 
                Path.Combine(baseDir, "mmt.ico"), 
                Path.Combine(projectRoot, "mmt.ico"), 
                "mmt.ico",
                Path.Combine(baseDir, "MMT.ico")
            };

            foreach (var p in paths) {
                if (File.Exists(p)) {

                    byte[] iconBytes = File.ReadAllBytes(p);
                    using (MemoryStream ms = new MemoryStream(iconBytes))
                    {
                        var icon = new Icon(ms);
                        this.Icon = icon;
                        this.notifyIconApp.Icon = icon;
                        break;
                    }
                }
            }
        } catch { }
        
        if (this.notifyIconApp.Icon == null) this.notifyIconApp.Icon = SystemIcons.Application;
        this.notifyIconApp.Visible = true;
    }



    // ============================================
    // YOUTUBE DOWNLOADER LOGIC
    // ============================================

    private async void BtnLoad_Click(object sender, EventArgs e)
    {
        string url = NormalizeYouTubeSingleVideoUrl(txtUrl.Text.Trim());
        if (string.IsNullOrEmpty(url)) return;
        _currentUrl = url;

        if (!LooksLikeYouTubeInput(url))
        {
            RouteToWebsiteDownload(url, startDownload: true);
            return;
        }

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
            await PopulateYoutubeSubtitleOptionsAsync(_currentVideo.Id);
            
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
                cmbQuality.Enabled = true;
                ApplyDefaultQualitySelection();
                // Force drop-down height recalculation to avoid 2-row glitch
                cmbQuality.DropDownHeight = 300; 
            }
            else
            {
                cmbQuality.Enabled = false;
                lblVideoTitle.Text = "지원하는 스트림을 찾지 못했습니다.";
            }
            cmbQuality.EndUpdate();
        }
        catch (Exception ex) when (IsInvalidYouTubeInputError(ex))
        {
            ShowYoutubeInputGuide();
        }
        catch (Exception ex) when (ShouldLoadYouTubeInfoWithYtDlp(ex))
        {
            await LoadYoutubeInfoWithYtDlpAsync(url);
        }
        catch (Exception ex)
        {
            cmbQuality.Enabled = false;
            ShowCenteredMessage($"오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblVideoTitle.Text = "오류가 발생했습니다.";
            ReportError($"유튜브 정보 로드 실패 | URL: {url}", ex);
        }
        finally
        {
            btnLoad.Enabled = true;
        }
    }

    private void BtnAddQueue_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_customTitle) || cmbQuality.SelectedItem == null)
        {
            ShowCenteredMessage("영상을 먼저 불러오세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedOption = (QualityOption)cmbQuality.SelectedItem;
        
        string outputPath = "";

        if (IsYoutubeUrlInFlight(_currentUrl))
        {
            ShowCenteredMessage("이미 다운로드 중인 영상입니다. 완료된 뒤 다시 누르면 새 파일로 받을 수 있습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Check if Default Folder is set
        if (!string.IsNullOrWhiteSpace(SettingsManager.Settings.DefaultDownloadFolder) || SettingsManager.Settings.UseSiteFolderRules || SettingsManager.Settings.UseCustomSiteFolders)
        {
            string ext = selectedOption.IsVideo ? "mp4" : selectedOption.Id.Replace("best_", "");
            string saveDirectory = GetDownloadSaveDirectory(_currentUrl, !selectedOption.IsVideo);
            string channel = _currentVideo?.Author.ChannelTitle ?? "";
            string baseFileName = BuildFileNameFromSettings("YouTube", channel, _customTitle, selectedOption.Title);
            outputPath = Path.Combine(saveDirectory, $"{baseFileName}.{ext}");
            
            // To avoid overwrite, add numbers to filename if exists
            int count = 1;
            while(File.Exists(outputPath)) {
                outputPath = Path.Combine(saveDirectory, $"{baseFileName} ({count}).{ext}");
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
            Url = _currentUrl,
            Video = _currentVideo,
            Manifest = _streamManifest,
            Option = selectedOption,
            OutputPath = outputPath,
            ListViewItem = item,
            JobCts = new CancellationTokenSource(),
            CustomFileName = _customTitle,
            DownloadSubtitles = chkYoutubeDownloadSubtitles.Checked,
            SubtitleLanguagePreset = GetSelectedSubtitlePreset(cmbYoutubeSubtitleLanguage)
        };
        
        item.Tag = job;

        _downloadQueue.Enqueue(job);
        
        txtUrl.Text = "";
        lblVideoTitle.Text = "URL을 입력하고 '영상 확인' 버튼을 눌러주세요.";
        picThumbnail.Image = null;
        cmbQuality.Items.Clear();
        cmbQuality.Enabled = false;
        _currentVideo = null!;
        _streamManifest = null!;
        _customTitle = "";
        _currentUrl = "";
        
        lblStatus.Text = $"{lvQueue.Items.Count}개의 작업이 대기열에 있습니다.";

        if (!_isDownloading)
        {
            _ = ProcessDownloadQueueAsync();
        }

    }

    private bool EnqueueYtDlpDownload(string url, bool allowFolderPrompt = true)
    {
        return EnqueueYtDlpDownload(url, allowFolderPrompt, out _);
    }

    private bool EnqueueYtDlpDownload(string url, bool allowFolderPrompt, out string rejectReason, int playlistItemIndex = 0, string preferredTitle = "")
    {
        rejectReason = "";
        url = NormalizeYouTubeSingleVideoUrl(url);
        if (IsYtDlpUrlInFlight(url, playlistItemIndex))
        {
            rejectReason = "이미 다운로드 중인 URL입니다. 완료된 뒤 다시 누르면 새 파일로 받을 수 있습니다.";
            lblYtDlpStatus.Text = rejectReason;
            return false;
        }

        string savePath = GetDownloadSaveDirectory(url, SettingsManager.Settings.DefaultVideoQuality == "MP3");
        if (string.IsNullOrWhiteSpace(savePath) || !Directory.Exists(savePath))
        {
            if (allowFolderPrompt)
            {
                using var fbd = new FolderBrowserDialog();
                fbd.Description = "영상을 저장할 폴더를 선택하세요.";
                if (fbd.ShowDialog() != DialogResult.OK)
                {
                    rejectReason = "저장 위치 선택이 취소되었습니다.";
                    return false;
                }
                savePath = fbd.SelectedPath;
            }
            else
            {
                savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                Directory.CreateDirectory(savePath);
            }
        }

        var item = new ListViewItem(url);
        item.SubItems.Add(chkYtDlpDownloadSubtitles.Checked ? "켜짐" : "꺼짐");
        item.SubItems.Add("대기 중");
        lvYtDlpQueue.Items.Add(item);
        ResizeYtDlpQueueColumns();

        var job = new YtDlpDownloadJob
        {
            Id = Guid.NewGuid().ToString("N"),
            Url = url,
            SavePath = savePath,
            DownloadSubtitles = chkYtDlpDownloadSubtitles.Checked,
            SubtitleLanguagePreset = GetSelectedSubtitlePreset(cmbYtDlpSubtitleLanguage),
            FormatSelector = GetYtDlpFormatForDefaultQuality(),
            OutputNameTemplate = BuildYtDlpOutputNameTemplate(GetSiteNameFromUrl(url)),
            PreferredTitle = string.IsNullOrWhiteSpace(preferredTitle) ? _loginBrowserDownloadTitle : preferredTitle,
            PlaylistItemIndex = playlistItemIndex,
            UseXPrivateMode = tglXPrivateMode.Checked,
            UseInstaPrivateMode = tglInstaPrivateMode.Checked,
            UseLoginBrowserCookies = _isLoginBrowserMode || ShouldUseLoginBrowserCookiesForUrl(url),
            ListViewItem = item,
            JobCts = new CancellationTokenSource()
        };
        item.Tag = job;

        _ytDlpDownloadQueue.Enqueue(job);
        _loginBrowserDownloadTitle = "";
        txtYtDlpUrl.Text = "";
        lblYtDlpStatus.Text = $"{lvYtDlpQueue.Items.Count}개의 웹사이트 작업이 대기열에 있습니다.";

        if (!_isYtDlpQueueRunning)
        {
            _ = ProcessYtDlpDownloadQueueAsync();
        }

        return true;
    }

    private async Task ProcessYtDlpDownloadQueueAsync()
    {
        if (_isYtDlpQueueRunning) return;

        _isYtDlpQueueRunning = true;
        btnYtDlpRun.Text = YtDlpQueueButtonText;
        btnYtDlpCancel.Visible = true;
        btnYtDlpCancel.Enabled = true;

        try
        {
            while (true)
            {
                var workers = Enumerable.Range(0, MaxYtDlpParallelDownloads)
                    .Select(_ => ProcessYtDlpQueueWorkerAsync())
                    .ToArray();

                await Task.WhenAll(workers);

                if (_ytDlpDownloadQueue.IsEmpty)
                    break;
            }
        }
        finally
        {
            _isYtDlpQueueRunning = false;
            btnYtDlpCancel.Visible = false;
            pbYtDlp.Value = 0;

            if (_ytDlpDownloadQueue.IsEmpty)
            {
                btnYtDlpRun.Text = YtDlpStartButtonText;
                _downloadWidgetForm?.SetBusy(false);
                lblYtDlpStatus.Text = "웹사이트 다운로드 대기열 처리가 완료되었습니다.";
            }
            else
            {
                _ = ProcessYtDlpDownloadQueueAsync();
            }
        }
    }

    private async Task ProcessYtDlpQueueWorkerAsync()
    {
        while (_ytDlpDownloadQueue.TryDequeue(out var job))
        {
            if (job.JobCts.IsCancellationRequested || !lvYtDlpQueue.Items.Contains(job.ListViewItem))
            {
                job.JobCts.Dispose();
                continue;
            }

            lock (_ytDlpQueueLock)
            {
                _activeYtDlpJobs.Add(job);
                _currentYtDlpQueueJob ??= job;
            }

            try
            {
                _downloadWidgetForm?.SetBusy(true);
                _downloadWidgetForm?.SetProgress(0);
                _downloadWidgetForm?.ShowToast("\uB2E4\uC6B4\uB85C\uB4DC\uAC00 \uC2DC\uC791\uB418\uC5C8\uC2B5\uB2C8\uB2E4.");
                btnYtDlpCancel.Enabled = true;
                UpdateYtDlpJobStatus(job, "준비 중...");
                await DownloadYtDlpQueueJobAsync(job);
            }
            finally
            {
                lock (_ytDlpQueueLock)
                {
                    _activeYtDlpJobs.Remove(job);
                    _currentYtDlpQueueJob = _activeYtDlpJobs.FirstOrDefault();
                }

                job.JobCts.Dispose();
            }
        }
    }

    private async Task DownloadYtDlpQueueJobAsync(YtDlpDownloadJob job)
    {
        string url = job.Url;
        string cookieFile = "";

        try
        {
            _lastYtDlpPct = -1;
            _ytDlpCts = job.JobCts;
            UpdateYtDlpJobStatus(job, "yt-dlp 확인 중...");
            lblYtDlpStatus.Text = "yt-dlp 확인 중...";
            await EnsureYtDlpAsync(job.JobCts.Token);

            UpdateYtDlpJobStatus(job, "다운로드 준비 중...");
            lblYtDlpStatus.Text = "다운로드 준비 중...";
            pbYtDlp.Value = 0;
            if (job.UseXPrivateMode || job.UseInstaPrivateMode || job.UseLoginBrowserCookies)
            {
                lblXStatus.Text = "다운로드 준비 중...";
                pbXDownload.Value = 0;
            }

            YtDlpDownloader downloader = new YtDlpDownloader
            {
                DownloadSubtitles = job.DownloadSubtitles,
                SubtitleLanguages = GetSubtitleLanguageArgument(job.SubtitleLanguagePreset),
                FormatSelector = job.FormatSelector,
                OutputNameTemplate = job.OutputNameTemplate,
                PreferredTitle = job.PreferredTitle,
                PlaylistItemIndex = job.PlaylistItemIndex
            };

            downloader.OnProgressChanged += progress =>
            {
                int pct = (int)Math.Min(progress, 100);
                string msg = $"다운로드 진행 중... {progress:F1}%";
                UpdateYtDlpJobStatus(job, $"{pct}%");
                MirrorProgress(pct, msg);
            };

            downloader.WebViewResolver = async targetUrl =>
            {
                await _ytDlpBrowserSemaphore.WaitAsync(job.JobCts.Token);
                try
                {
                    string capturedUrl = "";
                    bool isAnilife = targetUrl.Contains("anilife.app", StringComparison.OrdinalIgnoreCase);
                    bool needsBrowserCapture = isAnilife ||
                        targetUrl.Contains("chzzk.naver.com", StringComparison.OrdinalIgnoreCase) ||
                        targetUrl.Contains("sooplive", StringComparison.OrdinalIgnoreCase);
                    try
                    {
                        if (webViewX.CoreWebView2 == null) await PreInitializeWebView2Async();
                    }
                    catch { }

                    this.Invoke((MethodInvoker)(() =>
                    {
                        _capturedM3u8Url = "";
                        if (webViewX.CoreWebView2 == null) return;
                        webViewX.CoreWebView2.Navigate(targetUrl);
                    }));

                    int maxAttempts = isAnilife ? 90 : needsBrowserCapture ? 60 : 40;
                    for (int i = 0; i < maxAttempts; i++)
                    {
                        await Task.Delay(500, job.JobCts.Token);
                        if (isAnilife && i == 10)
                        {
                            this.Invoke((MethodInvoker)(async () =>
                            {
                                try
                                {
                                    if (webViewX.CoreWebView2 != null)
                                    {
                                        await webViewX.CoreWebView2.ExecuteScriptAsync("window.dispatchEvent(new Event('mousemove')); window.dispatchEvent(new Event('scroll')); document.querySelector('video')?.load?.();");
                                    }
                                }
                                catch { }
                            }));
                        }

                        if (!string.IsNullOrEmpty(_capturedM3u8Url) && LooksLikeCapturedMediaUrl(_capturedM3u8Url))
                        {
                            capturedUrl = _capturedM3u8Url;
                            break;
                        }
                    }

                    this.Invoke((MethodInvoker)(() =>
                    {
                        if (webViewX.CoreWebView2 != null)
                        {
                            webViewX.CoreWebView2.Stop();
                            webViewX.CoreWebView2.Navigate("about:blank");
                        }
                    }));

                    return capturedUrl;
                }
                finally
                {
                    _ytDlpBrowserSemaphore.Release();
                }
            };

            if (url.Contains("chzzk.naver.com") && !url.Contains("/video/") && !url.Contains("/clips/"))
            {
                throw new Exception("치지직은 video 또는 clips 주소만 다운로드할 수 있습니다.");
            }

            string browser = "none";
            Dictionary<string, string>? customHeaders = null;
            string lowerUrlSuccess = url.ToLower();
            bool isTargetPlatform = ShouldUseLoginBrowserCookiesForUrl(lowerUrlSuccess);

            bool shouldUseWebViewCookies = job.UseLoginBrowserCookies || job.UseXPrivateMode || job.UseInstaPrivateMode || isTargetPlatform;

            if (shouldUseWebViewCookies)
            {
                await _ytDlpBrowserSemaphore.WaitAsync(job.JobCts.Token);
                try
                {
                    bool useXCookieProfile = job.UseXPrivateMode ||
                        lowerUrlSuccess.Contains("x.com") ||
                        lowerUrlSuccess.Contains("twitter.com");
                    await EnsureLoginWebViewProfileAsync(useXCookieProfile);

                    string exportedCookieFile = await ExportWebViewCookiesAsync(url);

                    if (string.IsNullOrEmpty(exportedCookieFile) && (job.UseXPrivateMode || job.UseInstaPrivateMode))
                    {
                        throw new Exception("브라우저에서 로그인 정보를 찾을 수 없습니다. 먼저 로그인 후 다운 화면에서 로그인을 완료해 주세요.");
                    }

                    if (!string.IsNullOrEmpty(exportedCookieFile) && File.Exists(exportedCookieFile))
                    {
                        cookieFile = Path.Combine(SettingsManager.UserDataFolder, $"temp_x_cookies_{job.Id}.txt");
                        File.Copy(exportedCookieFile, cookieFile, true);
                        try { File.Delete(exportedCookieFile); } catch { }
                    }

                    customHeaders = new Dictionary<string, string>();
                    bool isYouTubeLoginDownload = LooksLikeYouTubeInput(url);
                    if (!isYouTubeLoginDownload)
                    {
                        if (!string.IsNullOrEmpty(_capturedAuthToken)) customHeaders["authorization"] = _capturedAuthToken;
                        if (!string.IsNullOrEmpty(_capturedCsrfToken)) customHeaders["x-csrf-token"] = _capturedCsrfToken;
                        if (!string.IsNullOrEmpty(_capturedUserAgent)) customHeaders["User-Agent"] = _capturedUserAgent;
                        string cookieHeader = await BuildWebViewCookieHeaderAsync();
                        if (!string.IsNullOrWhiteSpace(cookieHeader)) customHeaders["Cookie"] = cookieHeader;
                    }
                }
                finally
                {
                    _ytDlpBrowserSemaphore.Release();
                }
            }

            string finalFilePath = await downloader.DownloadVideoAsync(url, job.SavePath, browser, job.JobCts.Token, cookieFile, customHeaders);

            UpdateYtDlpJobStatus(job, downloader.LastSubtitleDownloaded ? "완료 + 자막" : "완료");
            Notify("다운로드 완료", "웹사이트 영상 다운로드가 완료되었습니다.");

            string successMsg = "다운로드 완료! 아래 '폴더 열기'를 눌러 폴더를 여세요.";
            lblYtDlpStatus.Text = successMsg;
            pbYtDlp.Value = 100;
            _downloadWidgetForm?.SetProgress(100);
            _downloadWidgetForm?.SetStatus("\uB2E4\uC6B4\uB85C\uB4DC \uC644\uB8CC");
            _downloadWidgetForm?.ShowToast("\uB2E4\uC6B4\uB85C\uB4DC\uAC00 \uC644\uB8CC\uB418\uC5C8\uC2B5\uB2C8\uB2E4.");
            if (job.UseXPrivateMode || job.UseInstaPrivateMode || job.UseLoginBrowserCookies)
            {
                lblXStatus.Text = successMsg;
                pbXDownload.Value = 100;
            }

            if (SettingsManager.Settings.AutoOpenFolder) OpenFolder(job.SavePath);

            string platformSuccess = "웹 브라우저";
            try
            {
                var uri = new Uri(url);
                platformSuccess = uri.Host.Replace("www.", "");
            }
            catch { }

            if (lowerUrlSuccess.Contains("x.com") || lowerUrlSuccess.Contains("twitter.com"))
                platformSuccess = job.UseXPrivateMode ? "X(비공개)" : "X";
            else if (lowerUrlSuccess.Contains("chzzk")) platformSuccess = "치지직";
            else if (lowerUrlSuccess.Contains("soop") || lowerUrlSuccess.Contains("afreeca")) platformSuccess = "SOOP";
            else if (lowerUrlSuccess.Contains("instagram")) platformSuccess = "Instagram";
            else if (lowerUrlSuccess.Contains("pinterest")) platformSuccess = "Pinterest";
            else if (lowerUrlSuccess.Contains("anilife")) platformSuccess = "Anilife";
            else if (lowerUrlSuccess.Contains("linkkf")) platformSuccess = "Linkkf";
            else if (lowerUrlSuccess.Contains("youtube") || lowerUrlSuccess.Contains("youtu.be")) platformSuccess = "YouTube(범용)";

            LogDownload(BuildDownloadHistoryEntry(platformSuccess, job.PreferredTitle, finalFilePath));
            LogUsage(platformSuccess);
        }
        catch (OperationCanceledException)
        {
            _downloadWidgetForm?.SetProgress(null);
            _downloadWidgetForm?.SetBusy(false);
            _downloadWidgetForm?.ShowToast("\uB2E4\uC6B4\uB85C\uB4DC\uAC00 \uCDE8\uC18C\uB418\uC5C8\uC2B5\uB2C8\uB2E4.");
            UpdateYtDlpJobStatus(job, "취소됨");
            lblYtDlpStatus.Text = "다운로드가 취소되었습니다.";
            if (job.UseXPrivateMode || job.UseInstaPrivateMode || job.UseLoginBrowserCookies) lblXStatus.Text = "다운로드가 취소되었습니다.";
            Notify("다운로드 취소", "웹사이트 영상 다운로드가 취소되었습니다.");
        }
        catch (Exception ex)
        {
            _downloadWidgetForm?.SetProgress(null);
            _downloadWidgetForm?.SetBusy(false);
            _downloadWidgetForm?.ShowToast("\uB2E4\uC6B4\uB85C\uB4DC\uAC00 \uC2E4\uD328\uD588\uC2B5\uB2C8\uB2E4.");
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            bool ytDlpExists = File.Exists(Path.Combine(baseDir, "yt-dlp.exe"));
            bool ffmpegExists = File.Exists(Path.Combine(baseDir, "ffmpeg.exe"));

            string cause = GetDownloadFailureCause(ex);
            string errorMsg = BuildDownloadFailureMessage(url, ex);
            if (!ytDlpExists || !ffmpegExists)
            {
                errorMsg += "\n\n실행 폴더에 yt-dlp.exe 또는 ffmpeg.exe가 없습니다.";
            }

            UpdateYtDlpJobStatus(job, BuildYtDlpQueueFailureStatus(cause));
            lblYtDlpStatus.Text = "다운로드 실패: " + cause;
            if (job.UseXPrivateMode || job.UseInstaPrivateMode || job.UseLoginBrowserCookies) lblXStatus.Text = "다운로드 실패: " + cause;
            pbYtDlp.Value = 0;
            if (job.UseXPrivateMode || job.UseInstaPrivateMode || job.UseLoginBrowserCookies) pbXDownload.Value = 0;
            ShowCenteredMessage(errorMsg, "다운로드 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            ReportError($"yt-dlp 다운로드 실패 ({cause}) | URL: {url}", ex);
        }
        finally
        {
            if (!string.IsNullOrEmpty(cookieFile) && File.Exists(cookieFile))
            {
                try { File.Delete(cookieFile); } catch { }
            }

            if (_ytDlpCts == job.JobCts)
            {
                _ytDlpCts = null;
            }
        }
    }

    private void UpdateYtDlpJobStatus(YtDlpDownloadJob job, string status)
    {
        if (this.IsDisposed || this.Disposing) return;

        void Apply()
        {
            try
            {
                if (lvYtDlpQueue.Items.Contains(job.ListViewItem))
                {
                    job.ListViewItem.SubItems[2].Text = status;
                    HideYtDlpQueueHorizontalScrollBar();
                }
            }
            catch { }
        }

        if (InvokeRequired)
            BeginInvoke((MethodInvoker)Apply);
        else
            Apply();
    }

    private static string BuildYtDlpQueueFailureStatus(string cause)
    {
        if (string.IsNullOrWhiteSpace(cause) || cause.Contains("원인 확인 필요"))
        {
            return "실패: 원인 확인 필요 (제작자에게 문의해 주세요)";
        }

        return "실패: " + cause;
    }

    private async void BtnYtDlpRun_Click(object sender, EventArgs e)
    {
        string url = txtYtDlpUrl.Text.Trim();
        if (!_isInternalYtDlpRun)
        {
            if (!string.IsNullOrEmpty(url) && LooksLikeYouTubeInput(url) && !_isLoginBrowserMode)
            {
                await RouteToYoutubeDownloadAsync(url);
                return;
            }

            if (string.IsNullOrEmpty(url))
            {
                ShowCenteredMessage("다운로드할 URL을 입력해 주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            EnqueueYtDlpDownload(url);
            return;
        }

        if (!string.IsNullOrEmpty(url) && LooksLikeYouTubeInput(url) && !_isLoginBrowserMode)
        {
            await RouteToYoutubeDownloadAsync(url);
            return;
        }
        if (string.IsNullOrEmpty(url))
        {
            ShowCenteredMessage("다운로드할 URL을 입력해 주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            // Use default folder from settings if available
            string savePath = GetDownloadSaveDirectory(url, SettingsManager.Settings.DefaultVideoQuality == "MP3");
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
            if (tglXPrivateMode.Checked || tglInstaPrivateMode.Checked || _isLoginBrowserMode) lblXStatus.Text = "yt-dlp 확인 중...";
            
            _ytDlpCts = new CancellationTokenSource();
            



            _lastYtDlpPct = -1; // Reset progress tracking for new download
            await EnsureYtDlpAsync(_ytDlpCts.Token);

            lblYtDlpStatus.Text = "다운로드 준비 중...";
            pbYtDlp.Value = 0;
            if (tglXPrivateMode.Checked || tglInstaPrivateMode.Checked || _isLoginBrowserMode) {
                lblXStatus.Text = "다운로드 준비 중...";
                pbXDownload.Value = 0;
            }

            YtDlpDownloader downloader = new YtDlpDownloader
            {
                DownloadSubtitles = chkYtDlpDownloadSubtitles.Checked,
                SubtitleLanguages = GetSubtitleLanguageArgument(GetSelectedSubtitlePreset(cmbYtDlpSubtitleLanguage)),
                FormatSelector = GetYtDlpFormatForDefaultQuality(),
                OutputNameTemplate = BuildYtDlpOutputNameTemplate(GetSiteNameFromUrl(url)),
                PreferredTitle = _loginBrowserDownloadTitle
            };
            downloader.OnProgressChanged += (progress) =>
            {
                int pct = (int)Math.Min(progress, 100);
                string msg = $"다운로드 진행 중... {progress:F1}%";
                MirrorProgress(pct, msg);
            };

            downloader.WebViewResolver = async (targetUrl) =>
            {
                string capturedUrl = "";
                bool isAnilife = targetUrl.Contains("anilife.app", StringComparison.OrdinalIgnoreCase);
                bool needsBrowserCapture = isAnilife ||
                    targetUrl.Contains("chzzk.naver.com", StringComparison.OrdinalIgnoreCase) ||
                    targetUrl.Contains("sooplive", StringComparison.OrdinalIgnoreCase);
                try
                {
                    if (webViewX.CoreWebView2 == null) await PreInitializeWebView2Async();
                }
                catch { }
                this.Invoke((MethodInvoker)(() =>
                {
                    _capturedM3u8Url = "";
                    if (webViewX.CoreWebView2 == null) return;
                    webViewX.CoreWebView2.Navigate(targetUrl);
                }));

                int maxAttempts = isAnilife ? 90 : needsBrowserCapture ? 60 : 40;
                for (int i = 0; i < maxAttempts; i++) // anilife can delay hydration before the player requests HLS.
                {
                    await Task.Delay(500);
                    if (isAnilife && i == 10)
                    {
                        this.Invoke((MethodInvoker)(async () =>
                        {
                            try
                            {
                                if (webViewX.CoreWebView2 != null)
                                {
                                    await webViewX.CoreWebView2.ExecuteScriptAsync("window.dispatchEvent(new Event('mousemove')); window.dispatchEvent(new Event('scroll')); document.querySelector('video')?.load?.();");
                                }
                            }
                            catch { }
                        }));
                    }
                    if (!string.IsNullOrEmpty(_capturedM3u8Url) && LooksLikeCapturedMediaUrl(_capturedM3u8Url))
                    {
                        capturedUrl = _capturedM3u8Url;
                        break;
                    }
                }

                this.Invoke((MethodInvoker)(() =>
                {
                    if (webViewX.CoreWebView2 != null)
                    {
                        webViewX.CoreWebView2.Stop();
                        webViewX.CoreWebView2.Navigate("about:blank");
                    }
                }));

                return capturedUrl;
            };


            if (url.Contains("chzzk.naver.com") && !url.Contains("/video/") && !url.Contains("/clips/"))
            {
                ShowCenteredMessage("치지직은 video 또는 clips 주소만 다운로드할 수 있습니다.\n\n라이브는 다운로드 시작 후 5초 이상 시청한 뒤 다시 시도해 주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string browser = "none";
            string cookieFile = "";
            Dictionary<string, string> customHeaders = null;
            
            string lowerUrlSuccess = url.ToLower();
            bool isTargetPlatform = ShouldUseLoginBrowserCookiesForUrl(lowerUrlSuccess);

            if (tglXPrivateMode.Checked || tglInstaPrivateMode.Checked || _isLoginBrowserMode || isTargetPlatform)
            {
                try
                {

                    bool useXCookieProfile = tglXPrivateMode.Checked ||
                        lowerUrlSuccess.Contains("x.com") ||
                        lowerUrlSuccess.Contains("twitter.com");
                    await EnsureLoginWebViewProfileAsync(useXCookieProfile);

                    cookieFile = await ExportWebViewCookiesAsync(url);
                    
                    if (string.IsNullOrEmpty(cookieFile) && (tglXPrivateMode.Checked || tglInstaPrivateMode.Checked))
                    {
                        ShowCenteredMessage("브라우저에서 로그인 정보를 찾을 수 없습니다.\n먼저 로그인 후 다운 화면에서 로그인을 완료해 주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }


                    bool isYouTubeLoginDownload = LooksLikeYouTubeInput(url);
                    if (!isYouTubeLoginDownload)
                    {
                        if (!string.IsNullOrEmpty(_capturedAuthToken)) customHeaders["authorization"] = _capturedAuthToken;
                        if (!string.IsNullOrEmpty(_capturedCsrfToken)) customHeaders["x-csrf-token"] = _capturedCsrfToken;
                        if (!string.IsNullOrEmpty(_capturedUserAgent)) customHeaders["User-Agent"] = _capturedUserAgent;
                        string cookieHeader = await BuildWebViewCookieHeaderAsync();
                        if (!string.IsNullOrWhiteSpace(cookieHeader)) customHeaders["Cookie"] = cookieHeader;
                    }
                }
                catch (Exception ex)
                {
                    if (tglXPrivateMode.Checked || tglInstaPrivateMode.Checked)
                    {
                        ShowCenteredMessage($"로그인 정보를 가져오지 못했습니다: {ex.Message}", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        ReportError($"yt-dlp 로그인 정보 가져오기 실패 | URL: {url}", ex);
                        return;
                    }
                }
            }
            
            string finalFilePath = await downloader.DownloadVideoAsync(url, savePath, browser, _ytDlpCts.Token, cookieFile, customHeaders);
            _loginBrowserDownloadTitle = "";

            Notify("다운로드 완료", "영상 다운로드가 완료되었습니다.");
            ShowCenteredMessage($"다운로드 완료!\n저장 위치: {finalFilePath}", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            txtYtDlpUrl.Text = "";
            string successMsg = "다운로드 완료! 아래 '폴더 열기'를 눌러 폴더를 여세요.";
            lblYtDlpStatus.Text = successMsg;
            pbYtDlp.Value = 100;
            if (tglXPrivateMode.Checked || tglInstaPrivateMode.Checked || _isLoginBrowserMode)
            {
                lblXStatus.Text = successMsg;
                pbXDownload.Value = 100;
            }

            if (SettingsManager.Settings.AutoOpenFolder) OpenFolder(savePath);


            string platformSuccess = "웹 브라우저";
            try 
            {
                var uri = new Uri(url);
                platformSuccess = uri.Host.Replace("www.", "");
            } catch { }

            if (lowerUrlSuccess.Contains("x.com") || lowerUrlSuccess.Contains("twitter.com")) 
                platformSuccess = tglXPrivateMode.Checked ? "X(비공개)" : "X";
            else if (lowerUrlSuccess.Contains("chzzk")) platformSuccess = "치지직";
            else if (lowerUrlSuccess.Contains("soop") || lowerUrlSuccess.Contains("afreeca")) platformSuccess = "SOOP";
            else if (lowerUrlSuccess.Contains("instagram")) platformSuccess = "Instagram";
            else if (lowerUrlSuccess.Contains("pinterest")) platformSuccess = "Pinterest";
            else if (lowerUrlSuccess.Contains("anilife")) platformSuccess = "Anilife";
            else if (lowerUrlSuccess.Contains("linkkf")) platformSuccess = "Linkkf";
            else if (lowerUrlSuccess.Contains("youtube") || lowerUrlSuccess.Contains("youtu.be")) platformSuccess = "YouTube(범용)";
            
            LogUsage(platformSuccess);
        }
        catch (OperationCanceledException)
        {
            lblYtDlpStatus.Text = "다운로드가 취소되었습니다.";
            if (tglXPrivateMode.Checked || tglInstaPrivateMode.Checked || _isLoginBrowserMode) lblXStatus.Text = "다운로드가 취소되었습니다.";
            ShowCenteredMessage("다운로드가 취소되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            bool ytDlpExists = File.Exists(Path.Combine(baseDir, "yt-dlp.exe"));
            bool ffmpegExists = File.Exists(Path.Combine(baseDir, "ffmpeg.exe"));

            string cause = GetDownloadFailureCause(ex);
            string errorMsg = BuildDownloadFailureMessage(url, ex);
            if (!ytDlpExists || !ffmpegExists)
            {
                errorMsg += "\n\n실행 폴더에 yt-dlp.exe 또는 ffmpeg.exe가 없습니다.";
            }
            
            lblYtDlpStatus.Text = "다운로드 실패: " + cause;
            if (tglXPrivateMode.Checked || tglInstaPrivateMode.Checked || _isLoginBrowserMode) lblXStatus.Text = "다운로드 실패: " + cause;
            pbYtDlp.Value = 0;
            if (tglXPrivateMode.Checked || tglInstaPrivateMode.Checked || _isLoginBrowserMode) pbXDownload.Value = 0;
            ShowCenteredMessage(errorMsg, "다운로드 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            ReportError($"yt-dlp 다운로드 실패 ({cause}) | URL: {url}", ex);
        }
        finally
        {

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

    private int _lastYtDlpPct = -1;
    private void MirrorProgress(int pct, string msg)
    {
        if (this.IsDisposed || this.Disposing) return;
        if (pct == _lastYtDlpPct) return;
        _lastYtDlpPct = pct;

        this.Invoke((MethodInvoker)delegate {
            pbYtDlp.Value = pct;
            lblYtDlpStatus.Text = msg;
            _downloadWidgetForm?.SetProgress(pct);
            _downloadWidgetForm?.SetStatus(msg);
            
            if (tglXPrivateMode.Checked || tglInstaPrivateMode.Checked || _isLoginBrowserMode)
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

    private void BtnRemoveSelectedYtDlp_Click(object sender, EventArgs e)
    {
        if (lvYtDlpQueue.SelectedItems.Count == 0) return;

        var selectedItems = lvYtDlpQueue.SelectedItems.Cast<ListViewItem>().ToList();
        foreach (var item in selectedItems)
        {
            if (item.Tag is not YtDlpDownloadJob job)
            {
                lvYtDlpQueue.Items.Remove(item);
                continue;
            }

            bool isActive;
            lock (_ytDlpQueueLock)
            {
                isActive = _activeYtDlpJobs.Contains(job);
            }

            try
            {
                if (!job.JobCts.IsCancellationRequested)
                {
                    job.JobCts.Cancel();
                }
            }
            catch (ObjectDisposedException) { }

            if (isActive)
            {
                UpdateYtDlpJobStatus(job, "취소 중...");
            }
            else
            {
                lvYtDlpQueue.Items.Remove(item);
            }
        }

        lblYtDlpStatus.Text = "선택한 웹사이트 작업을 취소했습니다.";
    }

    private async Task<List<string>> TryDownloadYoutubeSubtitleAsync(DownloadJob job)
    {
        var downloaded = new List<string>();
        try
        {
            var captionManifest = await _youtube.Videos.ClosedCaptions.GetManifestAsync(job.Video.Id, job.JobCts.Token);
            var tracks = SelectCaptionTracks(captionManifest, job.SubtitleLanguagePreset);
            if (tracks.Count == 0) return downloaded;

            foreach (var track in tracks)
            {
                string subtitlePath = GetUniqueSubtitlePath(job.OutputPath, track.Language.Code, ".srt");
                await _youtube.Videos.ClosedCaptions.DownloadAsync(track, subtitlePath, new Progress<double>(), job.JobCts.Token);
                downloaded.Add(subtitlePath);
            }

            return downloaded;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[YouTubeSubtitle] {ex.Message}");
            return downloaded;
        }
    }

    private static List<ClosedCaptionTrackInfo> SelectCaptionTracks(ClosedCaptionManifest manifest, string preset)
    {
        if (preset.StartsWith("Lang:", StringComparison.OrdinalIgnoreCase))
        {
            string code = preset.Substring("Lang:".Length);
            var track = manifest.Tracks.FirstOrDefault(t => !t.IsAutoGenerated && t.Language.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
                ?? manifest.Tracks.FirstOrDefault(t => t.Language.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            return track == null ? new List<ClosedCaptionTrackInfo>() : new List<ClosedCaptionTrackInfo> { track };
        }

        if (preset == "All")
            return manifest.Tracks.ToList();

        var selected = new List<ClosedCaptionTrackInfo>();
        if (preset == "Ko" || preset == "KoEn")
        {
            var ko = manifest.Tracks.FirstOrDefault(t => !t.IsAutoGenerated && IsKoreanCaptionTrack(t))
                ?? manifest.Tracks.FirstOrDefault(IsKoreanCaptionTrack);
            if (ko != null) selected.Add(ko);
        }

        if (preset == "En" || preset == "KoEn")
        {
            var en = manifest.Tracks.FirstOrDefault(t => !t.IsAutoGenerated && IsEnglishCaptionTrack(t))
                ?? manifest.Tracks.FirstOrDefault(IsEnglishCaptionTrack);
            if (en != null && !selected.Any(t => t.Language.Code.Equals(en.Language.Code, StringComparison.OrdinalIgnoreCase))) selected.Add(en);
        }

        if (selected.Count == 0)
        {
            var fallback = manifest.Tracks.FirstOrDefault(t => !t.IsAutoGenerated)
                ?? manifest.Tracks.FirstOrDefault();
            if (fallback != null) selected.Add(fallback);
        }

        return selected;
    }

    private static bool IsKoreanCaptionTrack(ClosedCaptionTrackInfo track)
    {
        string code = track.Language.Code.ToLowerInvariant();
        string name = track.Language.Name.ToLowerInvariant();
        return code == "ko" || code.StartsWith("ko-") || name.Contains("korean") || name.Contains("\uD55C\uAD6D");
    }

    private static bool IsEnglishCaptionTrack(ClosedCaptionTrackInfo track)
    {
        string code = track.Language.Code.ToLowerInvariant();
        string name = track.Language.Name.ToLowerInvariant();
        return code == "en" || code.StartsWith("en-") || name.Contains("english");
    }

    private static string GetUniqueSubtitlePath(string mediaPath, string languageCode, string extension)
    {
        string directory = Path.GetDirectoryName(mediaPath) ?? "";
        string baseName = Path.GetFileNameWithoutExtension(mediaPath);
        string safeLanguage = MakeValidFileName(languageCode);
        string suffix = string.IsNullOrWhiteSpace(safeLanguage) ? "" : "." + safeLanguage;
        string subtitlePath = Path.Combine(directory, $"{baseName}{suffix}{extension}");

        int counter = 1;
        while (File.Exists(subtitlePath))
        {
            subtitlePath = Path.Combine(directory, $"{baseName}{suffix}_{counter++}{extension}");
        }

        return subtitlePath;
    }

    private async Task ProcessDownloadQueueAsync()
    {
        _isDownloading = true;

        await EnsureFFmpegAsync();
        Xabe.FFmpeg.FFmpeg.SetExecutablesPath(Path.GetDirectoryName(SettingsManager.GetFFmpegPath()));

        while (_downloadQueue.TryDequeue(out var job))
        {
            try
            {
                if (job.JobCts.IsCancellationRequested || !lvQueue.Items.Contains(job.ListViewItem))
                    continue;
                
                _activeJobs.Add(job);
                _downloadWidgetForm?.SetBusy(true);
                _downloadWidgetForm?.SetProgress(0);
                _downloadWidgetForm?.SetStatus("\uC720\uD29C\uBE0C \uB2E4\uC6B4\uB85C\uB4DC \uC2DC\uC791");
                _downloadWidgetForm?.ShowToast("\uB2E4\uC6B4\uB85C\uB4DC\uAC00 \uC2DC\uC791\uB418\uC5C8\uC2B5\uB2C8\uB2E4.");
                job.ListViewItem.SubItems[2].Text = "준비 중...";
                lblStatus.Text = $"진행 중: {job.Video.Title}";

                int lastPct = -1;
                var progress = new Progress<double>(p =>
                {
                    if (!lvQueue.Items.Contains(job.ListViewItem))
                        job.JobCts.Cancel();

                    int pct = (int)(p * 100);
                    if (pct == lastPct) return; // Only update on actual change
                    lastPct = pct;

                    if (!this.IsDisposed && !this.Disposing)
                    {
                        try {
                            this.Invoke((MethodInvoker)delegate {
                                job.ListViewItem.SubItems[2].Text = $"{pct}%";
                                pbYoutube.Value = pct;
                                _downloadWidgetForm?.SetProgress(pct);
                                _downloadWidgetForm?.SetStatus($"{pct}%");
                            });
                        } catch { }
                    }
                });

                if (job.Manifest == null)
                {
                    // yt-dlp fallback job
                    await DownloadYoutubeWithYtDlpFallbackAsync(job);
                    continue;
                }

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

                List<string> subtitlePaths = new List<string>();
                if (job.DownloadSubtitles)
                {
                    if (!this.IsDisposed && !this.Disposing && lvQueue.Items.Contains(job.ListViewItem))
                    {
                        this.Invoke((MethodInvoker)delegate {
                            job.ListViewItem.SubItems[2].Text = "\uC790\uB9C9 \uC800\uC7A5 \uC911...";
                        });
                    }

                    subtitlePaths = await TryDownloadYoutubeSubtitleAsync(job);
                }
                
                if (!this.IsDisposed && !this.Disposing && lvQueue.Items.Contains(job.ListViewItem))
                {
                    this.Invoke((MethodInvoker)delegate {
                        job.ListViewItem.SubItems[2].Text = "완료";
                        if (subtitlePaths.Count > 0)
                        {
                            job.ListViewItem.SubItems[2].Text = "\uC644\uB8CC + \uC790\uB9C9";
                        }
                        pbYoutube.Value = 100;
                        _downloadWidgetForm?.SetProgress(100);
                        _downloadWidgetForm?.SetStatus("\uB2E4\uC6B4\uB85C\uB4DC \uC644\uB8CC");
                        _downloadWidgetForm?.ShowToast("\uB2E4\uC6B4\uB85C\uB4DC\uAC00 \uC644\uB8CC\uB418\uC5C8\uC2B5\uB2C8\uB2E4.");
                    });
                }
                
                Notify("다운로드 성공", $"{job.Video.Title} 다운로드가 완료되었습니다.");
                

                LogDownload(BuildDownloadHistoryEntry("YouTube", job.CustomFileName, job.OutputPath));
                LogUsage("YouTube");

                if (SettingsManager.Settings.AutoOpenFolder)
                {
                    string folder = Path.GetDirectoryName(job.OutputPath);
                    OpenFolder(folder);
                }
            }
            catch (OperationCanceledException)
            {
                if (!this.IsDisposed && !this.Disposing && lvQueue.Items.Contains(job.ListViewItem))
                {
                    this.Invoke((MethodInvoker)delegate {
                        job.ListViewItem.SubItems[2].Text = "취소됨";
                        pbYoutube.Value = 0;
                        _downloadWidgetForm?.SetProgress(null);
                        _downloadWidgetForm?.SetBusy(false);
                        _downloadWidgetForm?.ShowToast("\uB2E4\uC6B4\uB85C\uB4DC\uAC00 \uCDE8\uC18C\uB418\uC5C8\uC2B5\uB2C8\uB2E4.");
                    });
                }
                Notify("다운로드 취소", $"{job.Video.Title} 다운로드가 취소되었습니다.");
            }
            catch (Exception ex)
            {
                if (job.Manifest != null && ShouldLoadYouTubeInfoWithYtDlp(ex))
                {
                    try
                    {
                        if (!this.IsDisposed && !this.Disposing && lvQueue.Items.Contains(job.ListViewItem))
                        {
                            this.Invoke((MethodInvoker)delegate {
                                job.ListViewItem.SubItems[2].Text = "yt-dlp로 재시도 중...";
                                lblStatus.Text = "YouTubeExplode 다운로드 실패: yt-dlp로 재시도 중...";
                                pbYoutube.Value = 0;
                            });
                        }

                        job.Manifest = null!;
                        await DownloadYoutubeWithYtDlpFallbackAsync(job);
                        continue;
                    }
                    catch (Exception fallbackEx)
                    {
                        ex = fallbackEx;
                    }
                }

                string cause = GetDownloadFailureCause(ex);
                string failedUrl = !string.IsNullOrWhiteSpace(job.Url) ? job.Url : job.Video?.Url ?? "";
                string failedTitle = !string.IsNullOrWhiteSpace(job.CustomFileName) ? job.CustomFileName : job.Video?.Title ?? "영상";
                string failureMessage = BuildDownloadFailureMessage(failedUrl, ex);
                if (!this.IsDisposed && !this.Disposing && lvQueue.Items.Contains(job.ListViewItem))
                {
                    this.Invoke((MethodInvoker)delegate {
                        job.ListViewItem.SubItems[2].Text = "실패: " + cause;
                        lblStatus.Text = "다운로드 실패: " + cause;
                        pbYoutube.Value = 0;
                    });
                }
                Notify("다운로드 실패", $"{failedTitle} 다운로드 실패: {cause}");
                ShowCenteredMessage(failureMessage, "다운로드 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                ReportError($"유튜브 다운로드 실패 ({cause}) | URL: {failedUrl}", ex);
            }            finally
            {
                _activeJobs.Remove(job);
            }
        }

        lblStatus.Text = "모든 처리가 완료되었습니다.";
        pbYoutube.Value = 0;
        _isDownloading = false;
        _downloadWidgetForm?.SetBusy(false);
    }

    private async Task LoadYoutubeInfoWithYtDlpAsync(string url)
    {
        try
        {
            _currentUrl = url;
            lblVideoTitle.Text = "유튜브 보안 업데이트 우회 중... (잠시만 기다려주세요)";
            
            string ytDlpPath = SettingsManager.GetYtDlpPath();
            await EnsureYtDlpAsync(forceUpdate: true);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = $"--print \"%(title)s|%(thumbnail)s\" --no-warnings \"{url}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi };
            string output = "";
            process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) output = e.Data; };
            process.Start();
            process.BeginOutputReadLine();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
            {
                throw new Exception(
                    "우회 방식을 통해서도 영상 정보를 가져오지 못했습니다.\n\n" +
                    "이 경우 보통 유튜브 보안 구조 변경, 비공개/회원 전용/나이 제한 영상, 라이브 영상, 지역 제한, 삭제된 영상 때문에 발생합니다.\n\n" +
                    "해볼 수 있는 방법:\n" +
                    "1. 로그인이 필요한 영상이면 [로그인 후 다운]에서 먼저 로그인한 뒤 다시 시도해 주세요.\n" +
                    "2. 일반 공개 영상도 같은 오류가 나면 yt-dlp 최신 버전 대응이 아직 안 된 영상일 수 있습니다.\n" +
                    "3. 같은 URL이 계속 실패하면 영상 URL과 오류 내용을 제작자에게 보내 주세요.\n\n" +
                    "DRM 보호 영상이나 권한이 없는 비공개 영상은 로그인해도 받을 수 없습니다.");
            }

            var parts = output.Split('|', 2);
            _customTitle = parts[0].Trim();
            string thumbUrl = parts.Length > 1 ? parts[1].Trim() : "";

            lblVideoTitle.Text = _customTitle;
            
            if (!string.IsNullOrEmpty(thumbUrl))
            {
                try { picThumbnail.LoadAsync(thumbUrl); } catch { }
            }

            _currentVideo = null!; 
            _streamManifest = null!;
            
            cmbQuality.BeginUpdate();
            cmbQuality.Items.Clear();
            cmbQuality.Items.Add(new QualityOption("최고 화질 (MP4)", "bestvideo+bestaudio/best", true));
            cmbQuality.Items.Add(new QualityOption("1080p (MP4)", "bestvideo[height<=1080]+bestaudio/best", true));
            cmbQuality.Items.Add(new QualityOption("720p (MP4)", "bestvideo[height<=720]+bestaudio/best", true));
            cmbQuality.Items.Add(new QualityOption("오디오 전용 (MP3 320kbps)", "best_mp3", false));
            cmbQuality.Items.Add(new QualityOption("오디오 전용 (WAV)", "best_wav", false));
            cmbQuality.Items.Add(new QualityOption("오디오 전용 (M4A)", "best_m4a", false));
            cmbQuality.Items.Add(new QualityOption("오디오 전용 (FLAC)", "best_flac", false));
            cmbQuality.Enabled = true;
            ApplyDefaultQualitySelection();
            cmbQuality.DropDownHeight = 300;
            cmbQuality.EndUpdate();
        }
        catch (Exception ex)
        {
            cmbQuality.Enabled = false;
            ShowCenteredMessage($"오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblVideoTitle.Text = "오류가 발생했습니다.";
        }
    }

    private static string GetYtDlpFallbackFormat(QualityOption option)
    {
        if (!option.IsVideo) return option.Id;
        if (option.Id.StartsWith("bestvideo", StringComparison.OrdinalIgnoreCase)) return option.Id;

        var heightMatch = System.Text.RegularExpressions.Regex.Match(option.Id, @"(?<height>\d+)p", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (heightMatch.Success)
        {
            string height = heightMatch.Groups["height"].Value;
            return $"bestvideo[height<={height}]+bestaudio/best[height<={height}]";
        }

        return "bestvideo+bestaudio/best";
    }

    private async Task DownloadYoutubeWithYtDlpFallbackAsync(DownloadJob job)
    {
        if (!this.IsDisposed && !this.Disposing && lvQueue.Items.Contains(job.ListViewItem))
        {
            this.Invoke((MethodInvoker)delegate {
                job.ListViewItem.SubItems[2].Text = "yt-dlp preparing...";
                lblStatus.Text = "yt-dlp preparing...";
                pbYoutube.Value = 0;
            });
        }

        await EnsureYtDlpAsync(forceUpdate: true);
        if (!this.IsDisposed && !this.Disposing && lvQueue.Items.Contains(job.ListViewItem))
        {
            this.Invoke((MethodInvoker)delegate {
                job.ListViewItem.SubItems[2].Text = "yt-dlp download starting...";
                lblStatus.Text = "yt-dlp download starting...";
            });
        }

        string ytDlpPath = SettingsManager.GetYtDlpPath();
        string ffmpegDir = Path.GetDirectoryName(SettingsManager.GetFFmpegPath()) ?? AppDomain.CurrentDomain.BaseDirectory;
        
        string formatArg = GetYtDlpFallbackFormat(job.Option);
        bool isAudioOnly = !job.Option.IsVideo;
        string ext = isAudioOnly ? job.Option.Id.Replace("best_", "") : "mp4";
        string outputTemplate = job.OutputPath;
        string sourceUrl = !string.IsNullOrWhiteSpace(job.Url) ? job.Url : job.Video?.Url ?? "";

        string arguments = $"--newline --encoding utf-8 --no-warnings --ffmpeg-location \"{ffmpegDir}\" ";

        if (isAudioOnly)
        {
            arguments += $"-x --audio-format {ext} ";
        }
        else
        {
            arguments += $"-f \"{formatArg}\" --merge-output-format mp4 ";
        }

        if (job.DownloadSubtitles)
        {
            arguments += $"--write-auto-subs --write-subs --sub-langs \"{GetSubtitleLanguageArgument(job.SubtitleLanguagePreset)}\" --embed-subs ";
        }

        arguments += $"-o \"{outputTemplate}\" \"{sourceUrl}\"";

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        using var process = new Process { StartInfo = psi };
        var processLog = new StringBuilder();
        
        System.Text.RegularExpressions.Regex progressRegex = new System.Text.RegularExpressions.Regex(@"\[download\]\s+(?<percent>\d+(\.\d+)?)%");
        
        process.OutputDataReceived += (s, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            processLog.AppendLine(e.Data);
            var m = progressRegex.Match(e.Data);
            if (m.Success && double.TryParse(m.Groups["percent"].Value, out double p))
            {
                int pct = (int)p;
                if (!this.IsDisposed && !this.Disposing && lvQueue.Items.Contains(job.ListViewItem))
                {
                    try {
                        this.Invoke((MethodInvoker)delegate {
                            job.ListViewItem.SubItems[2].Text = $"{pct}%";
                            pbYoutube.Value = pct;
                        });
                    } catch { }
                }
            }
        };
        process.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data)) processLog.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using (job.JobCts.Token.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch { } }))
        {
            await process.WaitForExitAsync();
        }

        job.JobCts.Token.ThrowIfCancellationRequested();

        if (process.ExitCode != 0 && !File.Exists(outputTemplate))
        {
            string detail = processLog.ToString().Trim();
            if (string.IsNullOrWhiteSpace(detail)) detail = "yt-dlp did not return detail.";
            throw new Exception("yt-dlp download failed\n" + detail);
        }

        if (!this.IsDisposed && !this.Disposing && lvQueue.Items.Contains(job.ListViewItem))
        {
            this.Invoke((MethodInvoker)delegate {
                job.ListViewItem.SubItems[2].Text = "완료" + (HasSubtitleOutputForJob(job, processLog.ToString()) ? " + 자막" : "");
                pbYoutube.Value = 100;
            });
        }
        
        Notify("다운로드 성공", $"{job.CustomFileName} 다운로드가 완료되었습니다.");
        LogUsage("YouTube");

        LogDownload(BuildDownloadHistoryEntry("YouTube", job.CustomFileName, job.OutputPath));

        if (SettingsManager.Settings.AutoOpenFolder)
        {
            string folder = Path.GetDirectoryName(job.OutputPath);
            OpenFolder(folder);
        }
    }

    private static bool HasSubtitleOutputForJob(DownloadJob job, string processLog)
    {
        if (!job.DownloadSubtitles) return false;

        if (!string.IsNullOrWhiteSpace(processLog)
            && (processLog.Contains(".srt", StringComparison.OrdinalIgnoreCase)
                || processLog.Contains(".vtt", StringComparison.OrdinalIgnoreCase)
                || processLog.Contains("Writing video subtitles to:", StringComparison.OrdinalIgnoreCase)
                || processLog.Contains("Writing video automatic captions to:", StringComparison.OrdinalIgnoreCase)
                || processLog.Contains("Embedding subtitles in", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        try
        {
            string directory = Path.GetDirectoryName(job.OutputPath) ?? "";
            string baseName = Path.GetFileNameWithoutExtension(job.OutputPath);
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(baseName) || !Directory.Exists(directory)) return false;

            foreach (string file in Directory.EnumerateFiles(directory, baseName + "*.*"))
            {
                string ext = Path.GetExtension(file);
                if (ext.Equals(".srt", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".vtt", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".ass", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".ssa", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch { }

        return false;
    }

    // ============================================
    // WEBM TO MP4 CONVERTER LOGIC
    // ============================================

    private void BtnBrowseWebM_Click(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog();
        ofd.Filter = "영상 파일|*.mp4;*.webm;*.mkv;*.mov;*.avi;*.flv;*.m4v;*.wmv;*.ts;*.mts;*.m2ts;*.mpeg;*.mpg|모든 파일|*.*";
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            SetSelectedInputFile(ofd.FileName, txtWebMInput, txtWebMOutput, VideoInputExtensions, "영상 파일", lblWebMStatus);
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
        lock (_ytDlpQueueLock)
        {
            foreach (var job in _activeYtDlpJobs)
            {
                job.JobCts.Cancel();
            }
        }

        while (_ytDlpDownloadQueue.TryDequeue(out var pendingJob))
        {
            pendingJob.JobCts.Cancel();
            UpdateYtDlpJobStatus(pendingJob, "취소됨");
            pendingJob.JobCts.Dispose();
        }

        btnYtDlpCancel.Enabled = false;
        lblYtDlpStatus.Text = "취소 중...";
        if (tglXPrivateMode.Checked || tglInstaPrivateMode.Checked || _isLoginBrowserMode) lblXStatus.Text = "취소 중...";
    }

    private void BtnYtDlpLoginBrowser_Click(object sender, EventArgs e)
    {
        ShowLoginFolder();
    }

    private async void BtnConvertWebM_Click(object sender, EventArgs e)
    {
        string inputFile = txtWebMInput.Text;
        if (!File.Exists(inputFile))
        {
            ShowCenteredMessage("영상 파일 경로를 선택하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (!TryValidateMediaInput(inputFile, VideoInputExtensions, "영상 파일", out string validationMessage))
        {
            lblWebMStatus.Text = validationMessage;
            ShowCenteredMessage(validationMessage, "지원하지 않는 파일", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            Xabe.FFmpeg.FFmpeg.SetExecutablesPath(Path.GetDirectoryName(SettingsManager.GetFFmpegPath()));
 
            CleanupManager.RegisterFile(isSequence ? sequenceDir : outputFile);

            await RunFFmpegWithProgress(args, inputFile, pbWebM, lblWebMStatus, _webmCts.Token);
  
            CleanupManager.UnregisterFile(isSequence ? sequenceDir : outputFile);

            Notify("변환 성공", "포맷 변환이 완료되었습니다.");
            string showPath = isSequence ? sequenceDir : outputFile;
            if (SettingsManager.Settings.AutoOpenFolder)
            {
                OpenFolder(isSequence ? sequenceDir : Path.GetDirectoryName(outputFile));
            }
            ShowCenteredMessage($"저장 위치:\n{showPath}", "변환 성공", MessageBoxButtons.OK, MessageBoxIcon.Information);


            LogUsage("FormatConv");
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
            ShowCenteredMessage($"변환 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ReportError($"WebM 변환 실패 | Input: {inputFile}, Format: {format}", ex);
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
        ofd.Filter = "영상 파일|*.mp4;*.webm;*.mkv;*.mov;*.avi;*.flv;*.m4v;*.wmv;*.ts;*.mts;*.m2ts;*.mpeg;*.mpg|모든 파일|*.*";
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            SetSelectedInputFile(ofd.FileName, txtCodecInput, txtCodecOutput, VideoInputExtensions, "영상 파일", lblCodecStatus);
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
            ShowCenteredMessage("영상 파일 경로를 선택하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (!TryValidateMediaInput(inputFile, VideoInputExtensions, "영상 파일", out string validationMessage))
        {
            lblCodecStatus.Text = validationMessage;
            ShowCenteredMessage(validationMessage, "지원하지 않는 파일", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            lblCodecStatus.Text = "코덱 변환 중... 시간이 오래 걸릴 수 있습니다.";
            pbCodec.Value = 0;
 
            CleanupManager.RegisterFile(outputFile);

            await EnsureFFmpegAsync();
            Xabe.FFmpeg.FFmpeg.SetExecutablesPath(Path.GetDirectoryName(SettingsManager.GetFFmpegPath()));
 
            string args = $"-i \"{inputFile}\" -c:v libx264 -preset fast -crf 18 -r 30 -pix_fmt yuv420p -c:a aac -b:a 192k \"{outputFile}\" -y";
            await RunFFmpegWithProgress(args, inputFile, pbCodec, lblCodecStatus, _codecCts.Token);
  
            CleanupManager.UnregisterFile(outputFile);

            Notify("변환 성공", "Pr/AE용 코덱 변환이 완료되었습니다.");
            if (SettingsManager.Settings.AutoOpenFolder) OpenFolder(outDir);
            ShowCenteredMessage($"저장 위치:\n{outputFile}", "변환 성공", MessageBoxButtons.OK, MessageBoxIcon.Information);


            LogUsage("CodecFix");
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
            ShowCenteredMessage($"변환 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ReportError($"코덱 변환 실패 | Input: {inputFile}", ex);
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
        ofd.Filter = "미디어 파일|*.mp4;*.webm;*.mkv;*.mov;*.avi;*.flv;*.m4v;*.wmv;*.ts;*.mts;*.m2ts;*.mpeg;*.mpg;*.mp3;*.wav;*.m4a;*.aac;*.ogg;*.flac;*.wma;*.opus|모든 파일|*.*";
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            SetSelectedInputFile(ofd.FileName, txtAudioInput, txtAudioOutput, AudioConverterInputExtensions, "영상이나 오디오 파일", lblAudioStatus);
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
            ShowCenteredMessage("영상이나 mp3 파일 경로를 선택하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (!TryValidateMediaInput(inputFile, AudioConverterInputExtensions, "영상이나 오디오 파일", out string validationMessage))
        {
            lblAudioStatus.Text = validationMessage;
            ShowCenteredMessage(validationMessage, "지원하지 않는 파일", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            lblAudioStatus.Text = $"{format}로 변환 중... 잠시만 기다려주세요.";
            pbAudio.Value = 0;
 
            CleanupManager.RegisterFile(outputFile);

            await EnsureFFmpegAsync();
            Xabe.FFmpeg.FFmpeg.SetExecutablesPath(Path.GetDirectoryName(SettingsManager.GetFFmpegPath()));
 
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
            if (SettingsManager.Settings.AutoOpenFolder) OpenFolder(outDir);
            ShowCenteredMessage($"저장 위치:\n{outputFile}", "변환 성공", MessageBoxButtons.OK, MessageBoxIcon.Information);


            LogUsage("AudioConv");
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
            ShowCenteredMessage($"변환 중 오류가 발생했습니다.\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ReportError($"오디오 변환 실패 | Input: {inputFile}, Format: {format}", ex);
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
            SetSavePathLabels(fbd.SelectedPath);
            miniEditorControl.UpdateSavePath(fbd.SelectedPath);
        }
    }

    private void BtnSaveSettings_Click(object sender, EventArgs e)
    {
        SaveCurrentSettings(true);
    }

    private void SaveCurrentSettings(bool showSuccessMsg)
    {
        string newPath = txtDownloadFolder.Text.Trim();
        SettingsManager.Settings.DefaultDownloadFolder = newPath;
        SettingsManager.Settings.ShowNotifications = chkShowNotifications.Checked;
        SettingsManager.Settings.AutoOpenFolder = chkAutoOpenFolder.Checked;
        SettingsManager.Settings.AutoUpdateCheck = chkAutoUpdateCheck.Checked;
        if (chkEnableWidgetMode != null)
        {
            SettingsManager.Settings.EnableWidgetMode = chkEnableWidgetMode.Checked;
        }
        SaveSelectedSiteFolderOverride();
        if (chkUseSiteFolderRules != null)
        {
            SettingsManager.Settings.UseSiteFolderRules = chkUseSiteFolderRules.Checked;
            SettingsManager.Settings.UseCustomSiteFolders = chkUseCustomSiteFolders?.Checked ?? false;
            SettingsManager.Settings.FileNamePreset = GetFileNamePresetValue(cmbFileNamePreset?.Text);
            SettingsManager.Settings.CustomFileNameTemplate = string.IsNullOrWhiteSpace(txtCustomFileNameTemplate?.Text)
                ? "{title}"
                : txtCustomFileNameTemplate.Text.Trim();
            SettingsManager.Settings.DefaultVideoQuality = GetQualityValue(cmbDefaultVideoQuality?.Text);
            string subtitlePreset = chkYtDlpDownloadSubtitles.Checked
                ? GetSelectedSubtitlePreset(cmbYtDlpSubtitleLanguage)
                : GetSelectedSubtitlePreset(cmbYoutubeSubtitleLanguage);
            SettingsManager.Settings.SubtitleLanguagePreset = subtitlePreset;
        }
        SettingsManager.Save();

        // Update all conversion tab output paths in real-time
        if (!string.IsNullOrWhiteSpace(newPath) && Directory.Exists(newPath))
        {
            txtWebMOutput.Text = newPath;
            txtCodecOutput.Text = newPath;
            txtAudioOutput.Text = newPath;
            SetSavePathLabels(newPath);
            miniEditorControl.UpdateSavePath(newPath);
        }

        if (showSuccessMsg)
        {
            ShowCenteredMessage("설정이 안전하게 저장되었습니다.", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void ReloadSettingsUI()
    {
        txtDownloadFolder.Text = SettingsManager.Settings.DefaultDownloadFolder;
        chkShowNotifications.Checked = SettingsManager.Settings.ShowNotifications;
        chkAutoOpenFolder.Checked = SettingsManager.Settings.AutoOpenFolder;
        chkAutoUpdateCheck.Checked = SettingsManager.Settings.AutoUpdateCheck;
        ReloadDownloadRuleSettingsUI();
        ReloadWidgetModeSettingsUI();
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
                    var result = ShowCenteredMessage($"새로운 업데이트(v{latestVersion})가 있습니다.\n지금 다운로드하여 설치하시겠습니까?\n\n설치 중 프로그램이 자동으로 종료 후 다시 시작될 수 있습니다.", 
                        "업데이트 알림", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        var assets = root.GetProperty("assets");
                        string downloadUrl = "";
                        foreach (var asset in assets.EnumerateArray())
                        {
                            string fileName = asset.GetProperty("name").GetString() ?? "";
                            if (fileName.EndsWith(".exe"))
                            {
                                downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(downloadUrl))
                        {
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
                                            

                                        }
                                    }
                                }


                                await Task.Delay(500);

                                var startInfo = new ProcessStartInfo(tempFile)
                                {



                                    UseShellExecute = true
                                };
                                
                                Process.Start(startInfo);
                                

                                Application.Exit();
                            }
                            catch (Exception ex)
                            {
                                ShowCenteredMessage($"업데이트 다운로드 중 오류가 발생했습니다: {ex.Message}", "업데이트 실패");
                            }
                        }
                    }
                }
                else if (manual)
                {
                    ShowCenteredMessage("현재 최신 버전을 사용 중입니다.", "업데이트 확인");
                }
            }
        }
        catch (Exception ex)
        {
            if (manual) ShowCenteredMessage($"업데이트 확인 중 오류가 발생했습니다: {ex.Message}", "오류");
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
            procD.StartInfo.FileName = SettingsManager.GetFFprobePath();
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
        proc.StartInfo.FileName = SettingsManager.GetFFmpegPath();
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

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private const int SW_RESTORE = 9;

    private void OpenFolder(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
        
        string targetPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLower();

        try 
        {

            Type? shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType != null)
            {
                object? shellInstance = Activator.CreateInstance(shellType);
                if (shellInstance != null)
                {
                    dynamic shell = shellInstance;
                    dynamic windows = shell.Windows();
                
                for (int i = 0; i < windows.Count; i++)
                {
                    dynamic window = windows.Item(i);

                    string fullExePath = "";
                    try { fullExePath = window.FullName; } catch { continue; }
                    
                    if (!string.IsNullOrEmpty(fullExePath) && fullExePath.ToLower().EndsWith("explorer.exe"))
                    {
                        string windowPath = "";
                        try {
                            string url = window.LocationURL;
                            if (!string.IsNullOrEmpty(url))
                            {
                                var uri = new Uri(url);
                                if (uri.IsFile) windowPath = uri.LocalPath;
                            }


                            if (string.IsNullOrEmpty(windowPath))
                            {
                                windowPath = window.Document.Folder.Self.Path;
                            }
                        } catch { continue; }


                        if (!string.IsNullOrEmpty(windowPath) && Path.IsPathRooted(windowPath))
                        {
                            try
                            {
                                string fullWindowPath = Path.GetFullPath(windowPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLower();
                                if (fullWindowPath == targetPath)
                                {
                                    IntPtr hwnd = (IntPtr)window.HWND;
                                    if (IsIconic(hwnd)) ShowWindow(hwnd, SW_RESTORE);
                                    SetForegroundWindow(hwnd);
                                    return;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
        }
    }
        catch {  }

        // 열려있지 않은 경우 새로 열기
        try { 
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); 
        } 
        catch { 
            try { Process.Start("explorer.exe", $"\"{path}\""); } catch { }
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
        string ffmpegPath = SettingsManager.GetFFmpegPath();
        string ffprobePath = SettingsManager.GetFFprobePath();


        if (File.Exists(ffmpegPath) && !IsGitLfsPointer(ffmpegPath) && HasMzHeader(ffmpegPath) &&
            File.Exists(ffprobePath) && !IsGitLfsPointer(ffprobePath) && HasMzHeader(ffprobePath))
        {
            Xabe.FFmpeg.FFmpeg.SetExecutablesPath(Path.GetDirectoryName(ffmpegPath));
            return;
        }


        if (File.Exists(ffmpegPath) && (IsGitLfsPointer(ffmpegPath) || !HasMzHeader(ffmpegPath))) try { File.Delete(ffmpegPath); } catch { }
        if (File.Exists(ffprobePath) && (IsGitLfsPointer(ffprobePath) || !HasMzHeader(ffprobePath))) try { File.Delete(ffprobePath); } catch { }


        if (File.Exists(ffmpegPath) && File.Exists(ffprobePath))
        {
            Xabe.FFmpeg.FFmpeg.SetExecutablesPath(Path.GetDirectoryName(ffmpegPath));
            return;
        }


        string toolsDir = Path.GetDirectoryName(ffmpegPath) ?? SettingsManager.UserDataFolder;
        if (!Directory.Exists(toolsDir)) Directory.CreateDirectory(toolsDir);
        
        try
        {
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, toolsDir);
        }
        catch (Exception ex)
        {
            // Fallback for download failure
            ShowCenteredMessage($"FFmpeg 다운로드 중 오류가 발생했습니다: {ex.Message}\n프로그램을 관리자 권한으로 실행하거나 AppData 폴더 쓰기 권한을 확인해 주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        
        Xabe.FFmpeg.FFmpeg.SetExecutablesPath(toolsDir);
    }

    private void UpdateVideoInfoDisplay()
    {
        if (_currentVideo == null) return;
        lblVideoTitle.Text = $"{_customTitle}\n채널: {_currentVideo.Author.ChannelTitle}\n길이: {_currentVideo.Duration}";
    }

    private async Task PreInitializeWebView2Async(string? userDataPathOverride = null)
    {
        try
        {
            string userDataPath = userDataPathOverride ?? SettingsManager.WebViewDataFolder;
            bool useXProfile = string.Equals(userDataPath, XWebViewDataFolder, StringComparison.OrdinalIgnoreCase);
            if (webViewX.CoreWebView2 != null)
            {
                if (_webViewUsingXProfile == useXProfile) return;
                RecreateLoginWebViewControl();
            }


            if (string.Equals(userDataPath, SettingsManager.WebViewDataFolder, StringComparison.OrdinalIgnoreCase))
            {
                if (!SettingsManager.Settings.KeepLoginSession)
                {
                    CleanupManager.CleanupWebViewData();
                }
                else
                {
                    CleanupManager.CleanupWebViewNonLoginData();
                }
            }
            else if (!SettingsManager.Settings.KeepLoginSession)
            {
                try { if (Directory.Exists(userDataPath)) Directory.Delete(userDataPath, true); } catch { }
            }
            if (!Directory.Exists(userDataPath)) Directory.CreateDirectory(userDataPath);

            var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, userDataPath);
            await webViewX.EnsureCoreWebView2Async(env);
            _webViewUsingXProfile = useXProfile;
            

            // webViewYoutube.Source = new Uri("https://www.youtube.com"); // This line was commented out as it refers to webViewYoutube, not webViewX.
            
            if (webViewX.CoreWebView2 != null)
            {
                _capturedUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome Safari/537.36";
                webViewX.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webViewX.CoreWebView2.NewWindowRequested += WebViewX_NewWindowRequested;
                try
                {
                    string userAgentJson = await webViewX.CoreWebView2.ExecuteScriptAsync("navigator.userAgent");
                    string? detectedUserAgent = JsonSerializer.Deserialize<string>(userAgentJson);
                    if (!string.IsNullOrWhiteSpace(detectedUserAgent)) _capturedUserAgent = detectedUserAgent;
                }
                catch { }
                webViewX.CoreWebView2.NavigationCompleted += async (s, e) =>
                {
                    UpdateLoginBrowserNavigationState();
                    await ApplySameWindowBrowserPatchAsync();
                };
                webViewX.CoreWebView2.HistoryChanged += (s, e) => UpdateLoginBrowserNavigationState();
                await webViewX.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
(() => {
    const rewriteBlankTargets = () => {
        document.querySelectorAll('a[target=""_blank""], form[target=""_blank""]').forEach(el => el.setAttribute('target', '_self'));
    };
    document.addEventListener('click', event => {
        const target = event.target && event.target.closest ? event.target.closest('a[target=""_blank""]') : null;
        if (target) target.setAttribute('target', '_self');
    }, true);
    rewriteBlankTargets();
    new MutationObserver(rewriteBlankTargets).observe(document.documentElement, { childList: true, subtree: true });
})();
");

                webViewX.CoreWebView2.NavigationStarting += (s, e) => {
                    _capturedM3u8Url = "";
                    if (TryBuildGoogleAccountChooserUrl(e.Uri, out string accountChooserUrl))
                    {
                        e.Cancel = true;
                        webViewX.CoreWebView2.Navigate(accountChooserUrl);
                        return;
                    }

                    Debug.WriteLine("[X-Browser] Navigation Starting, clearing captured URL.");
                };

                await webViewX.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.enable", "{}");
                
                webViewX.CoreWebView2.GetDevToolsProtocolEventReceiver("Network.requestWillBeSent").DevToolsProtocolEventReceived += (sender, args) =>
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(args.ParameterObjectAsJson);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("request", out var request))
                        {
                            string url = request.GetProperty("url").GetString() ?? "";
                            if (LooksLikeCapturedMediaUrl(url))
                            {
                                _capturedM3u8Url = NormalizeCapturedMediaUrl(url);
                                Debug.WriteLine($"[MMT-Intercept] Video Found: {_capturedM3u8Url}");
                            }
                            if (request.TryGetProperty("headers", out var headers))
                            {
                                if (headers.TryGetProperty("authorization", out var auth)) _capturedAuthToken = auth.GetString() ?? _capturedAuthToken;
                                if (headers.TryGetProperty("x-csrf-token", out var csrf)) _capturedCsrfToken = csrf.GetString() ?? _capturedCsrfToken;
                            }
                        }
                    }
                    catch { }
                };
            }
        }
        catch { }
    }

    private async Task EnsureLoginWebViewProfileAsync(bool useXProfile)
    {
        await PreInitializeWebView2Async(useXProfile ? XWebViewDataFolder : SettingsManager.WebViewDataFolder);
    }

    private void RecreateLoginWebViewControl()
    {
        var oldWebView = webViewX;
        Control? parent = oldWebView.Parent;
        bool inTableLayout = tableLayoutX != null && parent == tableLayoutX;

        try
        {
            parent?.Controls.Remove(oldWebView);
            oldWebView.Dispose();
        }
        catch { }

        webViewX = new Microsoft.Web.WebView2.WinForms.WebView2
        {
            AllowExternalDrop = true,
            CreationProperties = null,
            DefaultBackgroundColor = Color.White,
            Dock = DockStyle.Fill,
            Name = "webViewX",
            ZoomFactor = 1D
        };

        if (inTableLayout && tableLayoutX != null)
        {
            tableLayoutX.Controls.Add(webViewX, 0, 1);
        }
        else if (parent != null)
        {
            parent.Controls.Add(webViewX);
            webViewX.SendToBack();
        }
    }

    private void WebViewX_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        string url = e.Uri;
        e.Handled = true;
        if (ShouldOpenLoginPopup(url))
        {
            ShowLoginPopupWindow(e);
            return;
        }

        if (string.IsNullOrWhiteSpace(url) || url.Equals("about:blank", StringComparison.OrdinalIgnoreCase) || webViewX.CoreWebView2 == null)
        {
            return;
        }

        webViewX.CoreWebView2.Navigate(url);
        if (txtLoginBrowserAddress?.Visible == true) txtLoginBrowserAddress.Text = url;
    }

    private static bool ShouldOpenLoginPopup(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Equals("about:blank", StringComparison.OrdinalIgnoreCase)) return true;

        return url.Contains("accounts.google.com", StringComparison.OrdinalIgnoreCase) ||
               url.Contains("google.com/accounts", StringComparison.OrdinalIgnoreCase) ||
               url.Contains("oauth", StringComparison.OrdinalIgnoreCase) ||
               url.Contains("authorize", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryBuildGoogleAccountChooserUrl(string url, out string accountChooserUrl)
    {
        accountChooserUrl = "";
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (!uri.Host.EndsWith("accounts.google.com", StringComparison.OrdinalIgnoreCase)) return false;

        string lowerUrl = url.ToLowerInvariant();
        bool isOAuthLogin =
            lowerUrl.Contains("/o/oauth", StringComparison.OrdinalIgnoreCase) ||
            lowerUrl.Contains("/signin/oauth", StringComparison.OrdinalIgnoreCase) ||
            lowerUrl.Contains("client_id=", StringComparison.OrdinalIgnoreCase);

        if (!isOAuthLogin) return false;
        if (lowerUrl.Contains("prompt=select_account", StringComparison.OrdinalIgnoreCase)) return false;

        accountChooserUrl = System.Text.RegularExpressions.Regex.Replace(
            url,
            @"([?&])prompt=[^&]*",
            "$1prompt=select_account",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (accountChooserUrl == url)
        {
            accountChooserUrl = url + (url.Contains("?") ? "&" : "?") + "prompt=select_account";
        }

        return true;
    }

    private async void ShowLoginPopupWindow(CoreWebView2NewWindowRequestedEventArgs e)
    {
        var deferral = e.GetDeferral();
        try
        {
            var popupForm = new Form
            {
                Text = "로그인",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(520, 720),
                MinimizeBox = false,
                ShowIcon = false
            };

            var popupWebView = new Microsoft.Web.WebView2.WinForms.WebView2
            {
                Dock = DockStyle.Fill
            };
            popupForm.Controls.Add(popupWebView);

            string userDataPath = _webViewUsingXProfile ? XWebViewDataFolder : SettingsManager.WebViewDataFolder;
            if (!Directory.Exists(userDataPath)) Directory.CreateDirectory(userDataPath);
            var env = await CoreWebView2Environment.CreateAsync(null, userDataPath);
            await popupWebView.EnsureCoreWebView2Async(env);

            popupWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            popupWebView.CoreWebView2.NavigationStarting += (s, args) =>
            {
                if (TryBuildGoogleAccountChooserUrl(args.Uri, out string accountChooserUrl))
                {
                    args.Cancel = true;
                    popupWebView.CoreWebView2.Navigate(accountChooserUrl);
                }
            };
            popupWebView.CoreWebView2.WindowCloseRequested += (s, args) => popupForm.Close();
            popupWebView.CoreWebView2.NewWindowRequested += (s, args) =>
            {
                args.Handled = true;
                if (!string.IsNullOrWhiteSpace(args.Uri) && !args.Uri.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
                {
                    popupWebView.CoreWebView2.Navigate(args.Uri);
                }
            };
            popupWebView.CoreWebView2.NavigationCompleted += (s, args) =>
            {
                try
                {
                    string source = popupWebView.CoreWebView2.Source ?? "";
                    if (source.Contains("x.com", StringComparison.OrdinalIgnoreCase) ||
                        source.Contains("twitter.com", StringComparison.OrdinalIgnoreCase))
                    {
                        popupForm.Close();
                        webViewX.CoreWebView2?.Reload();
                    }
                }
                catch { }
            };

            e.NewWindow = popupWebView.CoreWebView2;
            e.Handled = true;
            popupForm.FormClosed += (s, args) => popupWebView.Dispose();
            popupForm.Show(this);
        }
        catch
        {
            e.Handled = false;
        }
        finally
        {
            deferral.Complete();
        }
    }

    private async Task ApplySameWindowBrowserPatchAsync()
    {
        if (webViewX.CoreWebView2 == null) return;

        try
        {
            await webViewX.CoreWebView2.ExecuteScriptAsync(@"
document.querySelectorAll('a[target=""_blank""], form[target=""_blank""]').forEach(el => el.setAttribute('target', '_self'));
");
        }
        catch { }
    }

    private void UpdateLoginBrowserNavigationState()
    {
        bool canGoBack = webViewX.CoreWebView2?.CanGoBack == true;
        if (btnLoginBrowserBack != null)
        {
            btnLoginBrowserBack.Enabled = canGoBack;
            btnLoginBrowserBack.BackColor = canGoBack ? Color.FromArgb(51, 65, 85) : Color.FromArgb(148, 163, 184);
        }

        if (txtLoginBrowserAddress?.Visible == true && webViewX.CoreWebView2 != null)
        {
            string source = webViewX.CoreWebView2.Source;
            if (!string.IsNullOrWhiteSpace(source) && source != "about:blank") txtLoginBrowserAddress.Text = source;
        }
    }

    private void NavigateLoginBrowserBack()
    {
        if (webViewX.CoreWebView2?.CanGoBack == true)
        {
            webViewX.CoreWebView2.GoBack();
        }
    }

    private static bool LooksLikeCapturedMediaUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        string lower = url.ToLowerInvariant();

        return lower.Contains(".m3u8") ||
               lower.Contains(".mp4") && (lower.Contains("video.twimg.com") || lower.Contains("scontent") || lower.Contains("fbcdn") || lower.Contains("cdninstagram.com")) ||
               lower.Contains("gcdn.app") ||
               lower.Contains("anilife.app") ||
               lower.Contains("sooplive") && (lower.Contains("manifest") || lower.Contains(".m3u8")) ||
               lower.Contains("pstatic.net") && (lower.Contains(".m3u8") || lower.Contains(".ts"));
    }

    private static string NormalizeCapturedMediaUrl(string url)
    {
        if (url.Contains("pstatic.net", StringComparison.OrdinalIgnoreCase) && url.Contains(".ts", StringComparison.OrdinalIgnoreCase))
        {
            string normalized = System.Text.RegularExpressions.Regex.Replace(url, @"-\d+\.ts(?=[?#]|$)", ".m3u8", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return normalized.Replace("%7E", "~", StringComparison.OrdinalIgnoreCase);
        }

        return url;
    }

    private static bool ShouldUseLoginBrowserCookiesForUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        string lower = url.ToLowerInvariant();

        return lower.Contains("x.com") ||
               lower.Contains("twitter.com") ||
               lower.Contains("instagram.com") ||
               lower.Contains("youtube.com") ||
               lower.Contains("youtu.be") ||
               lower.Contains("youtube-nocookie.com") ||
               lower.Contains("chzzk.naver.com") ||
               lower.Contains("pstatic.net") ||
               lower.Contains("sooplive") ||
               lower.Contains("afreecatv.com");
    }

    private async void TglXPrivateMode_CheckedChanged(object sender, EventArgs e)
    {
        if (tglXPrivateMode.Checked)
        {
            _isLoginBrowserMode = false;
            _lastWidth = this.Width;
            _lastHeight = this.Height;

            SetupXPrivateUI();
            SetLoginBrowserControlsVisible(false);
            this.WindowState = FormWindowState.Maximized;

            panelXBrowser.Parent = this; 
            panelXBrowser.BringToFront();
            panelXBrowser.Visible = true;
            panelXBrowser.Dock = DockStyle.Fill;
            
            // 테이블 레이아웃 보이기
            if (tableLayoutX != null) tableLayoutX.Visible = true;
            if (btnLoginBrowserBack != null) btnLoginBrowserBack.Visible = false;
            
            await EnsureLoginWebViewProfileAsync(useXProfile: true);
            if (webViewX.CoreWebView2 != null)
            {
                webViewX.CoreWebView2.Stop();
                webViewX.CoreWebView2.Navigate("https://x.com/login");
            }

            if (lblXGuide != null) lblXGuide.Text = "영상 게시물 페이지로 이동한 뒤 즉시 다운로드를 눌러주세요.";
            if (lblXStatus != null) lblXStatus.Text = "영상 페이지로 이동해 주세요.";
            
            this.PerformLayout();
            this.Refresh();
        }
        else
        {
            if (webViewX.CoreWebView2 != null) webViewX.CoreWebView2.Stop();

            this.WindowState = FormWindowState.Normal;
            this.MinimumSize = new Size(800, 600);
            panelXBrowser.Visible = false;
            panelXBrowser.Parent = tabYtDlp; 
            panelXBrowser.Dock = DockStyle.Fill;
            
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
    private async void TglInstaPrivateMode_CheckedChanged(object sender, EventArgs e)
    {
        if (tglInstaPrivateMode.Checked)
        {
            if (tglXPrivateMode.Checked) tglXPrivateMode.Checked = false;
            _isLoginBrowserMode = false;


            if (_isInstaLoggedIn)
            {
                lblInstaPrivateMode.Text = "Instagram 로그인됨";
                lblYtDlpStatus.Text = "Instagram 로그인 상태입니다. 주소를 넣고 다운로드하세요.";
                return;
            }

            _lastWidth = this.Width;
            _lastHeight = this.Height;

            SetupXPrivateUI();
            SetLoginBrowserControlsVisible(false);
            this.WindowState = FormWindowState.Maximized;

            panelXBrowser.Parent = this; 
            panelXBrowser.BringToFront();
            panelXBrowser.Visible = true;
            panelXBrowser.Dock = DockStyle.Fill;
            
            if (tableLayoutX != null) tableLayoutX.Visible = true;
            if (btnLoginBrowserBack != null) btnLoginBrowserBack.Visible = false;
            
            await EnsureLoginWebViewProfileAsync(useXProfile: false);
            if (webViewX.CoreWebView2 != null) 
            {
                webViewX.CoreWebView2.Stop();

                webViewX.CoreWebView2.NavigationCompleted -= InstaLoginWatcher;
                webViewX.CoreWebView2.NavigationCompleted += InstaLoginWatcher;
                webViewX.CoreWebView2.Navigate("https://www.instagram.com/accounts/login/");
            }
            else
            {
                await PreInitializeWebView2Async();
                if (webViewX.CoreWebView2 != null)
                {
                    webViewX.CoreWebView2.NavigationCompleted -= InstaLoginWatcher;
                    webViewX.CoreWebView2.NavigationCompleted += InstaLoginWatcher;
                    webViewX.CoreWebView2.Navigate("https://www.instagram.com/accounts/login/");
                }
            }

            if (lblXGuide != null) lblXGuide.Text = "Instagram에 로그인하면 자동으로 닫힙니다.";
            this.PerformLayout();
            this.Refresh();
        }
        else
        {


            if (webViewX.CoreWebView2 != null)
            {
                webViewX.CoreWebView2.NavigationCompleted -= InstaLoginWatcher;
                webViewX.CoreWebView2.Stop();
                webViewX.CoreWebView2.Navigate("about:blank");
            }


            this.WindowState = FormWindowState.Normal;
            this.MinimumSize = new Size(800, 600);
            panelXBrowser.Visible = false;
            panelXBrowser.Parent = tabYtDlp; 
            panelXBrowser.Dock = DockStyle.Fill;
            
            if (_lastWidth > 0 && _lastHeight > 0)
            {
                this.Width = _lastWidth;
                this.Height = _lastHeight;
            }


            _ = ClearInstagramCookiesAsync();
            
            lblInstaPrivateMode.Text = "Instagram 로그인";
            lblYtDlpStatus.Text = "Instagram 로그아웃 완료.";
            this.PerformLayout();
            this.Refresh();
        }
    }


    private void InstaLoginWatcher(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
    {
        if (webViewX.CoreWebView2 == null) return;
        string currentUrl = webViewX.CoreWebView2.Source;
        

        if (currentUrl.Contains("instagram.com") && !currentUrl.Contains("/accounts/login"))
        {
            // 로그인 감지 후 중복 이벤트 제거
            webViewX.CoreWebView2.NavigationCompleted -= InstaLoginWatcher;
            webViewX.CoreWebView2.Stop();
            webViewX.CoreWebView2.Navigate("about:blank");

            this.Invoke((Action)(() =>
            {

                _isInstaLoggedIn = true;


                this.WindowState = FormWindowState.Normal;
                this.MinimumSize = new Size(800, 600);
                panelXBrowser.Visible = false;
                panelXBrowser.Parent = tabYtDlp;
                panelXBrowser.Dock = DockStyle.Fill;

                if (_lastWidth > 0 && _lastHeight > 0)
                {
                    this.Width = _lastWidth;
                    this.Height = _lastHeight;
                }

                lblInstaPrivateMode.Text = "Instagram 로그인됨";
                lblYtDlpStatus.Text = "Instagram 로그인 성공! 이제 주소를 넣고 다운로드하세요.";
                ShowCenteredMessage("Instagram 로그인 성공!\n\n이제 Instagram 영상 주소를 넣고 [다운로드]를 누르면 됩니다.\n\n해제하면 로그아웃됩니다.", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.PerformLayout();
                this.Refresh();
            }));
        }
    }

    private bool _isInstaLoggedIn = false;

    private async Task ClearInstagramCookiesAsync()
    {
        try
        {
            if (webViewX.CoreWebView2 == null) await PreInitializeWebView2Async();
            if (webViewX.CoreWebView2 != null)
            {
                var cookieManager = webViewX.CoreWebView2.CookieManager;
                var cookies = await cookieManager.GetCookiesAsync("https://www.instagram.com");
                foreach (var cookie in cookies) cookieManager.DeleteCookie(cookie);
            }
            _isInstaLoggedIn = false;
        }
        catch { }
    }

    private void ShowLoginFolder()
    {
        EnsureLoginFolderOverlay();
        if (panelLoginFolderOverlay == null || panelLoginFolder == null) return;

        panelLoginFolderOverlay.SuspendLayout();
        try
        {
            panelLoginFolder.Size = LoginFolderStartSize;
            PositionLoginFolder();
            SetLoginFolderContentVisible(false);
            panelLoginFolderOverlay.Visible = true;
            panelLoginFolderOverlay.BringToFront();
            lblYtDlpStatus.Text = "\uB85C\uADF8\uC778\uD560 \uC0AC\uC774\uD2B8\uB97C \uC120\uD0DD\uD558\uC138\uC694.";
        }
        finally
        {
            panelLoginFolderOverlay.ResumeLayout(true);
        }

        StartLoginFolderAnimation(opening: true);
    }

    private void HideLoginFolder(bool immediate = false)
    {
        if (panelLoginFolderOverlay == null) return;

        if (immediate || !panelLoginFolderOverlay.Visible)
        {
            _loginFolderAnimationTimer?.Stop();
            panelLoginFolderOverlay.Visible = false;
            if (panelLoginFolder != null) panelLoginFolder.Size = LoginFolderTargetSize;
            SetLoginFolderContentVisible(true);
            return;
        }

        StartLoginFolderAnimation(opening: false);
    }

    private void EnsureLoginFolderOverlay()
    {
        if (panelLoginFolderOverlay != null) return;

        panelLoginFolderOverlay = new Panel
        {
            Parent = tabYtDlp,
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(241, 245, 249),
            Visible = false
        };
        panelLoginFolderOverlay.Click += (s, e) => HideLoginFolder();
        panelLoginFolderOverlay.Resize += (s, e) => PositionLoginFolder();

        panelLoginFolder = new RoundPanel
        {
            Parent = panelLoginFolderOverlay,
            Size = LoginFolderTargetSize,
            BackColor = Color.FromArgb(250, 252, 255),
            BorderRadius = 32,
            BorderColor = Color.FromArgb(226, 232, 240),
            BorderThickness = 1
        };

        lblLoginFolderTitle = new Label
        {
            Parent = panelLoginFolder,
            Text = "\uB85C\uADF8\uC778 \uBE0C\uB77C\uC6B0\uC800",
            Location = new Point(24, 22),
            Size = new Size(280, 30),
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        lblLoginFolderHint = new Label
        {
            Parent = panelLoginFolder,
            Text = "\uB85C\uADF8\uC778\uC774 \uD544\uC694\uD55C \uC0AC\uC774\uD2B8\uB97C \uC120\uD0DD\uD55C \uB4A4, \uC601\uC0C1 \uD398\uC774\uC9C0\uC5D0\uC11C \uC989\uC2DC \uB2E4\uC6B4\uB85C\uB4DC\uB97C \uB204\uB974\uC138\uC694.",
            Location = new Point(25, 53),
            Size = new Size(400, 38),
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Segoe UI", 9F),
            TextAlign = ContentAlignment.MiddleLeft
        };

        btnLoginFolderClose = new RoundButton
        {
            Parent = panelLoginFolder,
            Text = "X",
            Size = new Size(36, 36),
            Location = new Point(panelLoginFolder.Width - 54, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BorderRadius = 18,
            BackColor = Color.FromArgb(226, 232, 240),
            ForeColor = Color.FromArgb(71, 85, 105),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            UseVisualStyleBackColor = false
        };
        btnLoginFolderClose.FlatAppearance.BorderSize = 0;
        btnLoginFolderClose.Click += (s, e) => HideLoginFolder();

        panelLoginFolderApps = new FlowLayoutPanel
        {
            Parent = panelLoginFolder,
            Location = new Point(25, 106),
            Size = new Size(420, 238),
            BackColor = Color.FromArgb(250, 252, 255),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        AddLoginFolderApp("Instagram", "IG", Color.FromArgb(190, 24, 93), "instagram.png");
        AddLoginFolderApp("\uCE58\uC9C0\uC9C1", "CZ", Color.FromArgb(22, 163, 74), "chzzk.png");
        AddLoginFolderApp("SOOP", "SOOP", Color.FromArgb(2, 132, 199), "soop.png");
        AddLoginFolderApp("\uC6F9 \uBE0C\uB77C\uC6B0\uC800", "...", Color.FromArgb(71, 85, 105), "etc.png");

        _loginFolderAnimationTimer = new System.Windows.Forms.Timer
        {
            Interval = 12
        };
        _loginFolderAnimationTimer.Tick += LoginFolderAnimationTimer_Tick;

        var doubleBufferProp = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        doubleBufferProp?.SetValue(panelLoginFolderOverlay, true, null);
        doubleBufferProp?.SetValue(panelLoginFolder, true, null);
        doubleBufferProp?.SetValue(panelLoginFolderApps, true, null);

        PositionLoginFolder();
    }

    private void SetLoginFolderContentVisible(bool visible)
    {
        if (lblLoginFolderTitle != null) lblLoginFolderTitle.Visible = visible;
        if (lblLoginFolderHint != null) lblLoginFolderHint.Visible = visible;
        if (btnLoginFolderClose != null) btnLoginFolderClose.Visible = false;
        if (panelLoginFolderApps != null) panelLoginFolderApps.Visible = visible;
    }

    private void StartLoginFolderAnimation(bool opening)
    {
        if (panelLoginFolderOverlay == null || panelLoginFolder == null) return;

        _loginFolderAnimationTimer?.Stop();
        _loginFolderOpening = opening;
        _loginFolderAnimationFrame = 0;

        if (opening)
        {
            panelLoginFolderOverlay.Visible = true;
            panelLoginFolder.Size = LoginFolderStartSize;
            PositionLoginFolder();
            SetLoginFolderContentVisible(false);
        }
        else
        {
            SetLoginFolderContentVisible(false);
        }

        _loginFolderAnimationTimer?.Start();
    }

    private void LoginFolderAnimationTimer_Tick(object? sender, EventArgs e)
    {
        if (panelLoginFolderOverlay == null || panelLoginFolder == null)
        {
            _loginFolderAnimationTimer?.Stop();
            return;
        }

        _loginFolderAnimationFrame++;
        double progress = Math.Min(1.0, _loginFolderAnimationFrame / (double)LoginFolderAnimationFrames);
        double eased = _loginFolderOpening ? EaseOutCubic(progress) : 1.0 - EaseOutCubic(progress);

        int width = LoginFolderStartSize.Width + (int)((LoginFolderTargetSize.Width - LoginFolderStartSize.Width) * eased);
        int height = LoginFolderStartSize.Height + (int)((LoginFolderTargetSize.Height - LoginFolderStartSize.Height) * eased);

        panelLoginFolder.SuspendLayout();
        try
        {
            panelLoginFolder.Size = new Size(width, height);
            PositionLoginFolder();
        }
        finally
        {
            panelLoginFolder.ResumeLayout(false);
        }

        if (progress < 1.0) return;

        _loginFolderAnimationTimer?.Stop();
        if (_loginFolderOpening)
        {
            panelLoginFolder.Size = LoginFolderTargetSize;
            PositionLoginFolder();
            SetLoginFolderContentVisible(true);
        }
        else
        {
            panelLoginFolderOverlay.Visible = false;
            panelLoginFolder.Size = LoginFolderTargetSize;
            PositionLoginFolder();
            SetLoginFolderContentVisible(true);
        }
    }

    private static double EaseOutCubic(double value)
    {
        double t = 1.0 - value;
        return 1.0 - (t * t * t);
    }

    private void PositionLoginFolder()
    {
        if (panelLoginFolderOverlay == null || panelLoginFolder == null) return;

        int x = Math.Max(12, (panelLoginFolderOverlay.ClientSize.Width - panelLoginFolder.Width) / 2);
        int y = Math.Max(12, (panelLoginFolderOverlay.ClientSize.Height - panelLoginFolder.Height) / 2);
        panelLoginFolder.Location = new Point(x, y);
    }

    private void AddLoginFolderApp(string siteName, string iconText, Color iconColor, string? iconFileName = null)
    {
        if (panelLoginFolderApps == null) return;

        var appTile = new Panel
        {
            Size = new Size(126, 104),
            Margin = new Padding(6),
            BackColor = Color.FromArgb(250, 252, 255),
            Cursor = Cursors.Hand
        };

        Control appIcon;
        Image? iconImage = string.IsNullOrWhiteSpace(iconFileName) ? null : LoadLoginIconImage(iconFileName);
        if (iconImage != null)
        {
            appIcon = new PictureBox
            {
                Parent = appTile,
                Image = iconImage,
                Size = new Size(64, 64),
                Location = new Point(31, 4),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
        }
        else
        {
            var fallbackIcon = new RoundButton
            {
                Parent = appTile,
                Text = iconText,
                Size = new Size(64, 64),
                Location = new Point(31, 4),
                BorderRadius = 18,
                BackColor = iconColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", iconText.Length > 2 ? 10F : 13F, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand
            };
            fallbackIcon.FlatAppearance.BorderSize = 0;
            appIcon = fallbackIcon;
        }

        var appLabel = new Label
        {
            Parent = appTile,
            Text = siteName,
            Location = new Point(0, 73),
            Size = new Size(126, 24),
            ForeColor = Color.FromArgb(51, 65, 85),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };

        EventHandler openHandler = async (s, e) => await OpenLoginSiteFromFolderAsync(siteName);
        appTile.Click += openHandler;
        appIcon.Click += openHandler;
        appLabel.Click += openHandler;

        panelLoginFolderApps.Controls.Add(appTile);
    }

    private static Image? LoadLoginIconImage(string fileName)
    {
        string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "LoginIcons", fileName);
        if (!File.Exists(iconPath))
        {
            iconPath = Path.Combine(Application.StartupPath, "Assets", "LoginIcons", fileName);
        }

        if (!File.Exists(iconPath))
        {
            iconPath = Path.Combine(Environment.CurrentDirectory, "Assets", "LoginIcons", fileName);
        }

        if (!File.Exists(iconPath)) return null;

        try
        {
            using var stream = new FileStream(iconPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var image = Image.FromStream(stream);
            return new Bitmap(image);
        }
        catch
        {
            return null;
        }
    }

    private async Task OpenLoginSiteFromFolderAsync(string siteName)
    {
        HideLoginFolder(immediate: true);
        if (siteName == "X")
        {
            _isLoginBrowserMode = false;
            if (tglInstaPrivateMode.Checked) tglInstaPrivateMode.Checked = false;
            if (!tglXPrivateMode.Checked) tglXPrivateMode.Checked = true;
            return;
        }

        if (siteName == "Instagram")
        {
            _isLoginBrowserMode = false;
            if (tglXPrivateMode.Checked) tglXPrivateMode.Checked = false;
            if (!tglInstaPrivateMode.Checked) tglInstaPrivateMode.Checked = true;
            return;
        }

        _isLoginBrowserMode = true;
        _lastWidth = this.Width;
        _lastHeight = this.Height;

        await ShowLoginBrowserAsync(siteName, GetLoginBrowserUrl(siteName));
    }

    private async Task ShowLoginBrowserAsync(string siteName, string url, bool navigate = true)
    {
        SetupXPrivateUI();
        SetLoginBrowserControlsVisible(false);
        bool showAddressBar = siteName == "\uC6F9 \uBE0C\uB77C\uC6B0\uC800" || siteName == "\uAE30\uD0C0";

        this.SuspendLayout();
        panelXBrowser.SuspendLayout();
        try
        {
            this.WindowState = FormWindowState.Maximized;
            panelXBrowser.Parent = this;
            panelXBrowser.Dock = DockStyle.Fill;
            panelXBrowser.Visible = true;
            panelXBrowser.BringToFront();

            if (tableLayoutX != null) tableLayoutX.Visible = true;
            if (btnLoginBrowserBack != null) btnLoginBrowserBack.Visible = true;
            if (lblXGuide != null)
            {
                lblXGuide.Text = showAddressBar
                    ? "\uC8FC\uC18C\uB97C \uBD99\uC5EC\uB123\uACE0 \uC774\uB3D9\uD558\uAC70\uB098 \uAD6C\uAE00\uC5D0\uC11C \uAC80\uC0C9\uD558\uC138\uC694."
                    : navigate
                    ? $"{siteName} 로그인 후 영상 페이지에서 즉시 다운로드를 눌러주세요."
                    : "사이트 버튼을 선택하면 해당 로그인 페이지로 이동합니다.";
            }
            if (lblXStatus != null) lblXStatus.Text = $"{siteName} 로그인 브라우저 열림";
            SetLoginBrowserAddressBarVisible(showAddressBar, url);
        }
        finally
        {
            panelXBrowser.ResumeLayout(true);
            this.ResumeLayout(true);
        }

        await EnsureLoginWebViewProfileAsync(useXProfile: false);
        if (webViewX.CoreWebView2 != null)
        {
            webViewX.CoreWebView2.NavigationCompleted -= InstaLoginWatcher;
            webViewX.CoreWebView2.Stop();
            if (navigate) webViewX.CoreWebView2.Navigate(url);
        }
        else
        {
            await PreInitializeWebView2Async();
            if (webViewX.CoreWebView2 != null)
            {
                webViewX.CoreWebView2.NavigationCompleted -= InstaLoginWatcher;
                if (navigate) webViewX.CoreWebView2.Navigate(url);
            }
        }

        if (showAddressBar && txtLoginBrowserAddress != null)
        {
            BeginInvoke(new Action(() =>
            {
                txtLoginBrowserAddress.Focus();
                txtLoginBrowserAddress.SelectAll();
            }));
        }
    }

    private void SetLoginBrowserControlsVisible(bool visible)
    {
        if (panelLoginSites != null) panelLoginSites.Visible = visible;
    }

    private void SetLoginBrowserAddressBarVisible(bool visible, string url = "")
    {
        if (txtLoginBrowserAddress != null)
        {
            txtLoginBrowserAddress.Visible = visible;
            if (visible) txtLoginBrowserAddress.Text = url;
        }

        if (btnLoginBrowserGo != null) btnLoginBrowserGo.Visible = visible;
        LayoutLoginBrowserAddressBar();
    }

    private void LayoutLoginBrowserAddressBar()
    {
        if (panelXTopBar == null || txtLoginBrowserAddress == null || btnLoginBrowserGo == null) return;

        int left = 222;
        int gap = 8;
        int goWidth = 58;
        int rightLimit = Math.Max(left + 260, panelXTopBar.ClientSize.Width - 110);
        int addressWidth = Math.Max(180, rightLimit - left - goWidth - gap);

        txtLoginBrowserAddress.Location = new Point(left, 16);
        txtLoginBrowserAddress.Size = new Size(addressWidth, 24);
        btnLoginBrowserGo.Location = new Point(left + addressWidth + gap, 12);
        btnLoginBrowserGo.Size = new Size(goWidth, 36);
    }

    private async Task NavigateLoginBrowserAddressAsync()
    {
        if (txtLoginBrowserAddress == null) return;

        string url = txtLoginBrowserAddress.Text.Trim();
        if (string.IsNullOrWhiteSpace(url)) return;
        if (!url.Contains("://")) url = "https://" + url;

        txtLoginBrowserAddress.Text = url;
        if (webViewX.CoreWebView2 == null) await PreInitializeWebView2Async();
        webViewX.CoreWebView2?.Navigate(url);
    }

    private string GetLoginBrowserUrl(string siteName)
    {
        return siteName switch
        {
            "X" => "https://x.com/login",
            "Instagram" => "https://www.instagram.com/accounts/login/",
            "\uCE58\uC9C0\uC9C1" => "https://chzzk.naver.com/",
            "SOOP" => "https://www.sooplive.co.kr/",
            _ => "https://www.google.com/"
        };
    }

    private void CloseLoginBrowserPanel()
    {
        if (webViewX.CoreWebView2 != null)
        {
            webViewX.CoreWebView2.NavigationCompleted -= InstaLoginWatcher;
            webViewX.CoreWebView2.Stop();
            webViewX.CoreWebView2.Navigate("about:blank");
        }

        this.SuspendLayout();
        panelXBrowser.SuspendLayout();
        try
        {
            this.WindowState = FormWindowState.Normal;
            this.MinimumSize = new Size(800, 600);
            panelXBrowser.Visible = false;
            panelXBrowser.Parent = tabYtDlp;
            panelXBrowser.Dock = DockStyle.Fill;

            if (_lastWidth > 0 && _lastHeight > 0)
            {
                this.Width = _lastWidth;
                this.Height = _lastHeight;
            }

            _isLoginBrowserMode = false;
            SetLoginBrowserControlsVisible(false);
            if (btnLoginBrowserBack != null) btnLoginBrowserBack.Visible = false;
            SetLoginBrowserAddressBarVisible(false);
        }
        finally
        {
            panelXBrowser.ResumeLayout(true);
            this.ResumeLayout(true);
        }
    }

    private void AddLoginSiteButton(string siteName, Color backColor)
    {
        if (panelLoginSites == null) return;

        var siteButton = new RoundButton
        {
            Text = siteName,
            Size = new Size(104, 30),
            Margin = new Padding(4),
            BorderRadius = 14,
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            UseVisualStyleBackColor = false
        };
        siteButton.FlatAppearance.BorderSize = 0;
        siteButton.Click += async (s, e) => await ShowLoginBrowserAsync(siteName, GetLoginBrowserUrl(siteName));
        panelLoginSites.Controls.Add(siteButton);
    }

    private void SetupXPrivateUI()
    {
        if (tableLayoutX == null)
        {
            panelXBrowser.Padding = new Padding(0);
            panelXBrowser.Size = tabYtDlp.Size;


            tableLayoutX = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            panelXBrowser.Controls.Add(tableLayoutX);


            tableLayoutX.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            tableLayoutX.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutX.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

            // 1. 상단 바
            panelXTopBar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(15, 10, 15, 10)
            };
            tableLayoutX.Controls.Add(panelXTopBar, 0, 0);

            lblXGuide = new Label
            {
                Text = "영상 페이지로 이동한 뒤 즉시 다운로드를 눌러주세요.",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            panelXTopBar.Controls.Add(lblXGuide);
            lblXGuide.SendToBack();

            // 2. 하단 바
            panelXBottomBar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(20, 5, 20, 5)
            };
            tableLayoutX.Controls.Add(panelXBottomBar, 0, 2);


            btnXCapture.Visible = false;

            btnLoginBrowserBack = new RoundButton
            {
                Parent = panelXTopBar,
                Text = "\u2190",
                Visible = false,
                Size = new Size(38, 36),
                Location = new Point(15, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                BorderRadius = 18,
                BackColor = Color.FromArgb(148, 163, 184),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand
            };
            btnLoginBrowserBack.FlatAppearance.BorderSize = 0;
            btnLoginBrowserBack.Click += (s, e) => NavigateLoginBrowserBack();

            btnXDownload.Parent = panelXTopBar;
            btnXDownload.Dock = DockStyle.None;
            btnXDownload.Size = new Size(150, 36);
            btnXDownload.Location = new Point(61, 12);
            btnXDownload.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnXDownload.BorderRadius = 18;
            btnXDownload.Text = "즉시 다운로드";
            btnXDownload.BackColor = Color.FromArgb(2, 132, 199);
            btnXDownload.ForeColor = Color.White;
            btnXDownload.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnXDownload.BringToFront();

            txtLoginBrowserAddress = new TextBox
            {
                Parent = panelXTopBar,
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(15, 23, 42),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            txtLoginBrowserAddress.KeyDown += async (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                await NavigateLoginBrowserAddressAsync();
            };

            btnLoginBrowserGo = new RoundButton
            {
                Parent = panelXTopBar,
                Text = "\uC774\uB3D9",
                Visible = false,
                BorderRadius = 18,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnLoginBrowserGo.FlatAppearance.BorderSize = 0;
            btnLoginBrowserGo.Click += async (s, e) => await NavigateLoginBrowserAddressAsync();
            panelXTopBar.Resize += (s, e) => LayoutLoginBrowserAddressBar();
            LayoutLoginBrowserAddressBar();

            panelLoginSites = new FlowLayoutPanel
            {
                Parent = panelXTopBar,
                Location = new Point(175, 8),
                Size = new Size(345, 78),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Visible = false
            };

            AddLoginSiteButton("Instagram", Color.FromArgb(190, 24, 93));
            AddLoginSiteButton("\uCE58\uC9C0\uC9C1", Color.FromArgb(22, 163, 74));
            AddLoginSiteButton("SOOP", Color.FromArgb(2, 132, 199));
            AddLoginSiteButton("\uC6F9 \uBE0C\uB77C\uC6B0\uC800", Color.FromArgb(71, 85, 105));

            panelLoginSites.BringToFront();
            btnLoginBrowserBack.BringToFront();
            txtLoginBrowserAddress.BringToFront();
            btnLoginBrowserGo.BringToFront();
            
            btnXClose.Parent = panelXTopBar;
            btnXClose.Dock = DockStyle.None;
            btnXClose.Size = new Size(80, 36);

            int closeX = panelXTopBar.Width > 100 ? panelXTopBar.Width - 95 : 515;
            btnXClose.Location = new Point(closeX, 12); 
            btnXClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnXClose.BorderRadius = 18;
            btnXClose.Text = "닫기";
            btnXClose.BackColor = Color.FromArgb(241, 245, 249); 
            btnXClose.ForeColor = Color.FromArgb(71, 85, 105);
            btnXClose.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnXClose.BringToFront();

            // 4. 프로그레스 바
            pbXDownload.Parent = panelXBottomBar;
            pbXDownload.Dock = DockStyle.Fill;
            lblXStatus.Visible = false;
            pbXDownload.BringToFront();


            webViewX.Parent = tableLayoutX;
            tableLayoutX.Controls.Add(webViewX, 0, 1);
            webViewX.Dock = DockStyle.Fill;
            
        }

        tableLayoutX.BringToFront();
        panelXBrowser.PerformLayout();
    }

    private void BtnXCapture_Click(object sender, EventArgs e)
    {
        string currentUrl = webViewX.Source.ToString();
        string lowerUrl = currentUrl.ToLower();
        

        bool isTweetPage = lowerUrl.Contains("x.com") || lowerUrl.Contains("twitter.com");
        bool isInstaPage = lowerUrl.Contains("instagram.com");

        if (isTweetPage || isInstaPage)
        {
            txtYtDlpUrl.Text = currentUrl;
            string platformName = isInstaPage ? "인스타그램" : "트윗";
            string statusMsg = $"{platformName} 주소 인식 완료";
            
            if (!string.IsNullOrEmpty(_capturedM3u8Url))
            {
                statusMsg = $"{platformName} 영상 스트림 포착 성공";
                

                string finalUrl = _capturedM3u8Url;
                if (isInstaPage && finalUrl.Contains("bytestart="))
                {

                }
                
                txtYtDlpUrl.Text = finalUrl; 
            }

            lblYtDlpStatus.Text = statusMsg + ": " + currentUrl;
            ShowCenteredMessage($"{statusMsg}!\n\n이제 '즉시 다운로드' 버튼을 눌러주세요.", "포착 성공");
        }
        else
        {
            ShowCenteredMessage($"현재 페이지: {webViewX.Source}\n\n영상이 있는 본문 페이지로 이동한 뒤 눌러주세요.", "알림");
        }
    }

    private async void BtnXDownload_Click(object sender, EventArgs e)
    {
        string currentUrl = webViewX.Source.ToString();
        _loginBrowserDownloadTitle = _isLoginBrowserMode
            ? await GetLoginBrowserDownloadTitleAsync()
            : "";

        if (_isLoginBrowserMode && !string.IsNullOrWhiteSpace(_capturedM3u8Url) && LooksLikeCapturedMediaUrl(_capturedM3u8Url))
        {
            txtYtDlpUrl.Text = NormalizeCapturedMediaUrl(_capturedM3u8Url);
        }
        else
        {
            txtYtDlpUrl.Text = currentUrl;
        }
        

        BtnYtDlpRun_Click(this, EventArgs.Empty);
    }

    private async Task<string> GetLoginBrowserDownloadTitleAsync()
    {
        try
        {
            if (webViewX.CoreWebView2 == null) return "";

            string scriptResult = await webViewX.CoreWebView2.ExecuteScriptAsync(
                "(() => document.querySelector('meta[property=\"og:title\"]')?.content || document.title || '')()");
            string title = JsonSerializer.Deserialize<string>(scriptResult) ?? "";
            title = title.Trim();

            if (string.IsNullOrWhiteSpace(title) || title.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }

            foreach (string suffix in new[] { " - YouTube", " | YouTube", " - NAVER TV", " | NAVER TV" })
            {
                if (title.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    title = title[..^suffix.Length].Trim();
                    break;
                }
            }

            return title;
        }
        catch
        {
            return "";
        }
    }

    private void BtnXClose_Click(object sender, EventArgs e)
    {
        if (_isLoginBrowserMode)
        {
            CloseLoginBrowserPanel();
            return;
        }


        if (webViewX.CoreWebView2 != null)
        {
            webViewX.CoreWebView2.NavigationCompleted -= InstaLoginWatcher;
            webViewX.CoreWebView2.Stop();
            webViewX.CoreWebView2.Navigate("about:blank");
        }
        
        if (tglInstaPrivateMode.Checked)
            tglInstaPrivateMode.Checked = false;
        else
            tglXPrivateMode.Checked = false;
    }

    private async Task<string> ExportWebViewCookiesAsync(string targetUrl = "")
    {
        if (webViewX.CoreWebView2 == null) return "";
        
        var cookieManager = webViewX.CoreWebView2.CookieManager;
        // 모든 쿠키를 모은 뒤 지원 도메인만 저장한다.
        var cookies = new List<CoreWebView2Cookie>();
        var seenCookies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        async Task AddCookiesAsync(string? uri)
        {
            try
            {
                var found = await cookieManager.GetCookiesAsync(uri);
                if (found == null) return;

                foreach (var cookie in found)
                {
                    string key = $"{cookie.Domain}|{cookie.Path}|{cookie.Name}";
                    if (seenCookies.Add(key)) cookies.Add(cookie);
                }
            }
            catch { }
        }

        await AddCookiesAsync(null);

        if (LooksLikeYouTubeInput(targetUrl))
        {
            await AddCookiesAsync("https://www.youtube.com/");
            await AddCookiesAsync("https://youtube.com/");
            await AddCookiesAsync("https://m.youtube.com/");
            await AddCookiesAsync("https://studio.youtube.com/");
            await AddCookiesAsync("https://accounts.google.com/");
            await AddCookiesAsync("https://myaccount.google.com/");
            await AddCookiesAsync("https://www.google.com/");
        }
        else if (Uri.TryCreate(targetUrl, UriKind.Absolute, out var targetUri))
        {
            await AddCookiesAsync($"{targetUri.Scheme}://{targetUri.Host}/");
        }
        
        if (cookies == null || cookies.Count == 0) return "";

        string cookiePath = Path.Combine(SettingsManager.UserDataFolder, "temp_x_cookies.txt");
        int count = 0;
        using (var sw = new StreamWriter(cookiePath, false, new System.Text.UTF8Encoding(false)))
        {
            sw.WriteLine("# Netscape HTTP Cookie File");
            sw.WriteLine("# This file is generated by YoutubeDownloader");
            
            foreach (var c in cookies)
            {

                string cookieDomain = c.Domain.ToLowerInvariant();
                if (!IsSupportedLoginCookieDomain(cookieDomain)) continue;

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

    private static bool IsSupportedLoginCookieDomain(string domain)
    {
        string lower = domain.ToLowerInvariant();
        return lower.Contains("x.com") ||
               lower.Contains("twitter.com") ||
               lower.Contains("instagram.com") ||
               lower.Contains("youtube.com") ||
               lower.Contains("youtube-nocookie.com") ||
               lower.Contains("google.com") ||
               lower.Contains("naver.com") ||
               lower.Contains("pstatic.net") ||
               lower.Contains("sooplive.com") ||
               lower.Contains("sooplive.co.kr") ||
               lower.Contains("afreecatv.com");
    }

    private async Task<string> BuildWebViewCookieHeaderAsync()
    {
        if (webViewX.CoreWebView2 == null) return "";

        try
        {
            var cookies = await webViewX.CoreWebView2.CookieManager.GetCookiesAsync(null);
            if (cookies == null || cookies.Count == 0) return "";

            var parts = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cookie in cookies)
            {
                string domain = cookie.Domain.ToLowerInvariant();
                bool isSupportedLoginDomain =
                    domain.Contains("x.com") ||
                    domain.Contains("twitter.com") ||
                    domain.Contains("instagram.com") ||
                    domain.Contains("youtube.com") ||
                    domain.Contains("google.com") ||
                    domain.Contains("youtube-nocookie.com") ||
                    domain.Contains("naver.com") ||
                    domain.Contains("pstatic.net") ||
                    domain.Contains("sooplive") ||
                    domain.Contains("afreecatv.com");

                if (!isSupportedLoginDomain) continue;
                string key = $"{cookie.Domain}|{cookie.Path}|{cookie.Name}";
                if (!seen.Add(key)) continue;
                parts.Add($"{cookie.Name}={cookie.Value}");
            }

            return string.Join("; ", parts);
        }
        catch
        {
            return "";
        }
    }

    private class DownloadJob
    {
        public string Id { get; set; } = "";
        public string Url { get; set; } = "";
        public Video Video { get; set; } = null!;
        public StreamManifest Manifest { get; set; } = null!;
        public QualityOption Option { get; set; } = null!;
        public string OutputPath { get; set; } = "";
        public ListViewItem ListViewItem { get; set; } = null!;
        public CancellationTokenSource JobCts { get; set; } = null!;
        public string CustomFileName { get; set; } = "";
        public bool DownloadSubtitles { get; set; }
        public string SubtitleLanguagePreset { get; set; } = "Ko";
    }

    private class YtDlpDownloadJob
    {
        public string Id { get; set; } = "";
        public string Url { get; set; } = "";
        public string SavePath { get; set; } = "";
        public bool DownloadSubtitles { get; set; }
        public string SubtitleLanguagePreset { get; set; } = "Ko";
        public string FormatSelector { get; set; } = "";
        public string OutputNameTemplate { get; set; } = "%(title)s";
        public string PreferredTitle { get; set; } = "";
        public int PlaylistItemIndex { get; set; }
        public bool UseXPrivateMode { get; set; }
        public bool UseInstaPrivateMode { get; set; }
        public bool UseLoginBrowserCookies { get; set; }
        public ListViewItem ListViewItem { get; set; } = null!;
        public CancellationTokenSource JobCts { get; set; } = null!;
    }

    private void MenuTrayOpen_Click(object sender, EventArgs e)
    {
        HideDownloadWidget();
        SettingsManager.Settings.EnableWidgetMode = false;
        SettingsManager.Save();
        if (chkEnableWidgetMode != null && chkEnableWidgetMode.Checked)
        {
            _updatingWidgetModeCheckbox = true;
            chkEnableWidgetMode.Checked = false;
            _updatingWidgetModeCheckbox = false;
        }
        if (btnTopWidgetMode != null) btnTopWidgetMode.Checked = false;
        ShowInTaskbar = true;
        this.Show();
        this.WindowState = FormWindowState.Normal;
        this.Activate();
    }

    private void MenuTrayExit_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }

    private void BtnOpenYtDlpFolder_Click(object sender, EventArgs e)
    {
        string path = SettingsManager.Settings?.DefaultDownloadFolder ?? "";
        if (string.IsNullOrEmpty(path)) path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        
        if (Directory.Exists(path))
        {
            try { Process.Start("explorer.exe", path); } catch { }
        }
        else
        {
            ShowCenteredMessage("폴더가 존재하지 않습니다: " + path);
        }
    }

    private void BtnOpenYoutubeFolder_Click(object sender, EventArgs e)
    {
        string path = SettingsManager.Settings?.DefaultDownloadFolder ?? "";
        if (string.IsNullOrEmpty(path)) path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        
        if (Directory.Exists(path))
        {
            try { Process.Start("explorer.exe", path); } catch { }
        }
        else
        {
            ShowCenteredMessage("폴더가 존재하지 않습니다: " + path);
        }
    }

    private void BtnOpenCodecFolder_Click(object sender, EventArgs e)
    {
        string path = SettingsManager.Settings?.DefaultDownloadFolder ?? "";
        if (string.IsNullOrEmpty(path)) path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        
        if (Directory.Exists(path))
        {
            try { Process.Start("explorer.exe", path); } catch { }
        }
        else
        {
            ShowCenteredMessage("폴더가 존재하지 않습니다: " + path);
        }
    }

    private void BtnOpenWebMFolder_Click(object sender, EventArgs e)
    {
        string path = SettingsManager.Settings?.DefaultDownloadFolder ?? "";
        if (string.IsNullOrEmpty(path)) path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        
        if (Directory.Exists(path))
        {
            try { Process.Start("explorer.exe", path); } catch { }
        }
        else
        {
            ShowCenteredMessage("폴더가 존재하지 않습니다: " + path);
        }
    }

    private void BtnOpenAudioFolder_Click(object sender, EventArgs e)
    {
        string path = SettingsManager.Settings?.DefaultDownloadFolder ?? "";
        if (string.IsNullOrEmpty(path)) path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        
        if (Directory.Exists(path))
        {
            try { Process.Start("explorer.exe", path); } catch { }
        }
        else
        {
            ShowCenteredMessage("폴더가 존재하지 않습니다: " + path);
        }
    }

    private sealed class WidgetModeToggleButton : Control
    {
        private bool _checked;

        public event EventHandler? CheckedChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public WidgetModeToggleButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            Cursor = Cursors.Hand;
            TabStop = false;
            BackColor = Color.Transparent;
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush backgroundBrush = new SolidBrush(BackColor == Color.Transparent ? Parent?.BackColor ?? SystemColors.Control : BackColor))
            {
                e.Graphics.FillRectangle(backgroundBrush, ClientRectangle);
            }

            Rectangle track = new Rectangle(1, 2, Width - 2, Height - 4);
            using (GraphicsPath trackPath = RoundedRectangle(track, track.Height / 2))
            using (SolidBrush trackBrush = new SolidBrush(Checked ? Color.FromArgb(35, 150, 216) : Color.FromArgb(180, 187, 196)))
            {
                e.Graphics.FillPath(trackBrush, trackPath);
            }

            int knobSize = Math.Max(14, track.Height - 4);
            int knobX = Checked ? track.Right - knobSize - 2 : track.Left + 2;
            Rectangle knob = new Rectangle(knobX, track.Top + 2, knobSize, knobSize);
            using (SolidBrush knobBrush = new SolidBrush(Color.FromArgb(237, 242, 246)))
            {
                e.Graphics.FillEllipse(knobBrush, knob);
            }
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
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

    private class SubtitleOption
    {
        public string Title { get; }
        public string Value { get; }

        public SubtitleOption(string title, string value)
        {
            Title = title;
            Value = value;
        }

        public override string ToString() => Title;
    }

    private void LogUsage(string feature)
    {
        try
        {
            if (SettingsManager.Settings.UsageStats == null)
                SettingsManager.Settings.UsageStats = new System.Collections.Generic.Dictionary<string, int>();

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string key = $"{today}_{feature}";

            if (SettingsManager.Settings.UsageStats.ContainsKey(key))
                SettingsManager.Settings.UsageStats[key]++;
            else
                SettingsManager.Settings.UsageStats[key] = 1;

            SettingsManager.Save();
        }
        catch { }
    }
    private static string BuildDownloadHistoryEntry(string site, string title, string filePath)
    {
        string cleanSite = string.IsNullOrWhiteSpace(site) ? "Download" : site.Trim();
        string cleanTitle = string.IsNullOrWhiteSpace(title) ? "" : title.Trim();

        if (string.IsNullOrWhiteSpace(cleanTitle) && !string.IsNullOrWhiteSpace(filePath))
        {
            try
            {
                cleanTitle = Path.GetFileNameWithoutExtension(filePath);
            }
            catch
            {
                cleanTitle = filePath;
            }
        }

        if (string.IsNullOrWhiteSpace(cleanTitle)) cleanTitle = "unknown";

        cleanSite = cleanSite.Replace("\r", " ").Replace("\n", " ");
        cleanTitle = cleanTitle.Replace("\r", " ").Replace("\n", " ");
        return $"{cleanSite} - {cleanTitle}";
    }

    private void LogDownload(string title)
    {
        try
        {
            if (SettingsManager.Settings.DailyDownloadHistory == null)
                SettingsManager.Settings.DailyDownloadHistory = new System.Collections.Generic.List<string>();

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string entry = $"{today}|{title}";

            if (!SettingsManager.Settings.DailyDownloadHistory.Contains(entry))
            {
                SettingsManager.Settings.DailyDownloadHistory.Add(entry);
                if (SettingsManager.Settings.DailyDownloadHistory.Count > 100)
                    SettingsManager.Settings.DailyDownloadHistory.RemoveAt(0);
                SettingsManager.Save();
            }
        }
        catch { }
    }

    private async Task StartHeartbeatLoopAsync()
    {
        while (true)
        {
            try
            {
                DateTime now = DateTime.Now;
                DateTime nextHour = now.AddHours(1).Date.AddHours(now.Hour + 1);
                TimeSpan delay = nextHour - now;
                if (delay.TotalMinutes < 1) delay = delay.Add(TimeSpan.FromHours(1));

                await Task.Delay(delay);
                await SendHeartbeatReportAsync("\uC815\uAE30 \uBCF4\uACE0");
            }
            catch { }
        }
    }

    private async Task<bool> SendHeartbeatReportAsync(string action, string errorMsg = "")
    {
        try
        {
            string a1 = "ht"; string a2 = "tps://"; string a3 = "discord.com/"; string a4 = "api/webho";
            string a5 = "oks/1482430548432519230/Zvwo0goRNckPROWjP6X9_DkBv";
            string a6 = "xM21SQ-OLFLOUHtbvFIiAWcA8bdihgEreonb2jcHL1U";
            string secretUrl = a1 + a2 + a3 + a4 + a5 + a6;

            if (string.IsNullOrEmpty(secretUrl)) return false;

            string todayStr = DateTime.Now.ToString("yyyy-MM-dd");
            string currentStatusKey = $"{todayStr} {DateTime.Now.Hour}";
            string periodicReport = "\uC815\uAE30 \uBCF4\uACE0";
            if (action == periodicReport && SettingsManager.Settings.LastHeartbeatDate == currentStatusKey) return false;

            string statsStr = BuildUsageStatsReport(todayStr);

            string reportTitle = action;
            bool isError = action.Equals("Error", StringComparison.OrdinalIgnoreCase);
            bool isManual = action.Equals("\uC218\uB3D9 \uD604\uD669 \uD655\uC778", StringComparison.OrdinalIgnoreCase);

            if (isError) reportTitle = "\uC624\uB958 \uBCF4\uACE0";
            else if (isManual) reportTitle = "\uC804\uCCB4 \uC0AC\uC6A9 \uD1B5\uACC4";
            else reportTitle = "\uC790\uB3D9 \uC0C1\uD0DC \uBCF4\uACE0";

            string installId = string.IsNullOrWhiteSpace(SettingsManager.Settings.InstallId)
                ? "\uBBF8\uC0DD\uC131"
                : SettingsManager.Settings.InstallId.Trim();
            string safeErrorMsg = isError ? BuildErrorReportContent(errorMsg) : SanitizeReportText(errorMsg);
            var payload = new
            {
                username = "MMT \uB370\uC774\uD130 \uC9D1\uACC4\uAE30",
                content = isError
                    ? $"**[\uC624\uB958 \uBCF4\uACE0]** v{CURR_VERSION}\n**\uAE30\uAE30 ID**: `{installId}`\n**\uB0B4\uC6A9**\n{safeErrorMsg}\n**\uC2DC\uAC01**: `{DateTime.Now:yyyy-MM-dd HH:mm:ss}`"
                    : $"**[{reportTitle}]** v{CURR_VERSION}\n**\uAE30\uAE30 ID**: `{installId}`\n\n**\uAE30\uB2A5 \uC0AC\uC6A9 \uD1B5\uACC4**\n{statsStr}\n\n**\uBCF4\uACE0 \uAE30\uC900**: `{todayStr}`\n**\uBCF4\uACE0 \uC2DC\uAC01**: `{DateTime.Now:yyyy-MM-dd HH:mm}`"
            };

            var result = await _httpClient.PostAsync(secretUrl, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
            if (result.IsSuccessStatusCode)
            {
                SettingsManager.Settings.LastHeartbeatDate = currentStatusKey;
                SettingsManager.Save();
                return true;
            }
        } catch { }
        return false;
    }

    private static string BuildUsageStatsReport(string todayStr)
    {
        var stats = SettingsManager.Settings.UsageStats;
        if (stats == null || stats.Count == 0) return "- \uD1B5\uACC4 \uC5C6\uC74C";

        var dailyStats = stats
            .Where(x => x.Key.StartsWith(todayStr + "_", StringComparison.Ordinal))
            .Select(x => new
            {
                Feature = SanitizeReportText(x.Key.Replace(todayStr + "_", "")),
                Count = Math.Max(0, x.Value)
            })
            .Where(x => x.Count > 0 && !string.IsNullOrWhiteSpace(x.Feature))
            .OrderByDescending(x => x.Count)
            .Take(12)
            .ToList();

        int total = dailyStats.Sum(x => x.Count);
        if (total <= 0) return "- \uD1B5\uACC4 \uC5C6\uC74C";

        var lines = new List<string>
        {
            $"- \uC624\uB298 \uCD1D \uC0AC\uC6A9\uB7C9: {total}"
        };

        foreach (var item in dailyStats)
        {
            double percent = item.Count * 100.0 / total;
            int filled = Math.Clamp((int)Math.Round(percent / 10.0), 0, 10);
            string bar = new string('\u25A0', filled).PadRight(10, '\u25A1');
            lines.Add($"- {item.Feature}: {item.Count}회 ({percent:F1}%) {bar}");
        }

        return string.Join("\n", lines);
    }

    private static string BuildErrorReportContent(string rawError)
    {
        string text = SanitizeReportText(rawError);
        string url = ExtractReportValue(text, "URL:");
        string cause = "";

        int causeStart = text.IndexOf("(\uC6D0\uC778:", StringComparison.Ordinal);
        if (causeStart >= 0)
        {
            cause = text[(causeStart + "(\uC6D0\uC778:".Length)..].Trim();
            if (cause.EndsWith(")", StringComparison.Ordinal)) cause = cause[..^1].Trim();
        }

        if (string.IsNullOrWhiteSpace(cause))
        {
            cause = text;
        }

        if (cause.Length > 1600) cause = cause[..1600] + "\n... \uC774\uD558 \uC0DD\uB7B5";

        return string.Join("\n", new[]
        {
            $"**URL**: {SanitizeReportText(string.IsNullOrWhiteSpace(url) ? "\uD655\uC778 \uBD88\uAC00" : url)}",
            $"**\uC624\uB958 \uB0B4\uC6A9**: {SanitizeReportText(cause)}"
        });
    }

    private static string ExtractReportValue(string text, string marker)
    {
        int index = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return "";

        string value = text[(index + marker.Length)..].Trim();
        int newline = value.IndexOf('\n');
        if (newline >= 0) value = value[..newline].Trim();

        int paren = value.IndexOf("(\uC6D0\uC778:", StringComparison.Ordinal);
        if (paren >= 0) value = value[..paren].Trim();

        return value.Trim();
    }

    private static string SanitizeReportText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "\uC54C \uC218 \uC5C6\uB294 \uC624\uB958\uC785\uB2C8\uB2E4.";

        string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        string[] lines = normalized.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (LooksLikeBrokenKorean(lines[i]))
            {
                lines[i] = "\uC624\uB958 \uC6D0\uBB38\uC774 \uAE68\uC838 \uD45C\uC2DC\uB420 \uC218 \uC788\uC5B4 \uC0DD\uB7B5\uD588\uC2B5\uB2C8\uB2E4.";
            }
        }

        return string.Join("\n", lines);
    }

    // 전역 오류 보고용 헬퍼
    private void ReportError(string msg, Exception? ex = null)
    {
        string fullMsg = ex != null
            ? $"{SanitizeReportText(msg)} (\uC6D0\uC778: {SanitizeReportText(ex.Message)})"
            : SanitizeReportText(msg);
        Task.Run(async () => await SendHeartbeatReportAsync("Error", fullMsg));
    }

    private static bool IsGitLfsPointer(string path)
    {
        if (!File.Exists(path)) return false;
        try
        {
            using var sr = new StreamReader(path);
            string? first = sr.ReadLine();
            return first != null && first.StartsWith("version https://git-lfs.github.com/spec/v1", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private static bool HasMzHeader(string path)
    {
        if (!File.Exists(path)) return false;
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Length < 2) return false;
            return fs.ReadByte() == 'M' && fs.ReadByte() == 'Z';
        }
        catch { return false; }
    }

    private sealed class DetectedVideoItem
    {
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public string Duration { get; set; } = "";
        public string ThumbnailUrl { get; set; } = "";
        public string SourcePageUrl { get; set; } = "";
        public int PlaylistIndex { get; set; }
    }
}
