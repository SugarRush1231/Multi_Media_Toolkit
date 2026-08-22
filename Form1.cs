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
using System.IO.Compression;
using System.Security.Cryptography;

 
namespace YoutubeDownloader;

public partial class Form1 : Form
{
    private YoutubeClient _youtube;
    private StreamManifest _streamManifest;
    private Video _currentVideo;
    private double? _currentVideoDurationSeconds;
    private string _customTitle = "";
    private string _currentUrl = "";
    private string _pendingYoutubeSourceFeature = "유튜브 다운로더";
    
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
    private readonly SemaphoreSlim _ffmpegInstallSemaphore = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim _denoInstallSemaphore = new SemaphoreSlim(1, 1);
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
    private ComboBox? cmbConcurrentDownloads;
    private ComboBox? cmbYoutubeSubtitleLanguage;
    private ComboBox? cmbYtDlpSubtitleLanguage;
    private CheckBox? chkYoutubeEmbedMetadata;
    private CheckBox? chkYoutubeSponsorBlock;
    private CheckBox? chkYoutubeDownloadSection;
    private CheckBox? chkYtDlpEmbedMetadata;
    private CheckBox? chkYtDlpDownloadSection;
    private CheckBox? chkEnableWidgetMode;
    private RoundButton? btnOpenVersionArchive;
    private RoundButton? btnExperimentalFeatures;
    private RoundButton? btnRepairTools;
    private RoundButton? btnInquiry;
    private DateTime _lastInquirySentAt = DateTime.MinValue;
    private Panel? panelExperimentalFeaturesOverlay;
    private RoundPanel? panelExperimentalFeaturesMenu;
    private CheckBox? chkEnableCompletedFileQuickUse;
    private CompletedFileCardForm? _completedFileCard;
    private bool _initializingExperimentalFeatures;
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
    private static readonly string[][] LoginCookieDomainGroups =
    {
        new[] { "youtube.com", "youtube-nocookie.com", "youtu.be", "google.com", "googlevideo.com" },
        new[] { "x.com", "twitter.com", "twimg.com" },
        new[] { "instagram.com", "cdninstagram.com", "fbcdn.net", "threads.com", "threads.net" },
        new[] { "kuaishou.com", "kwimgs.com", "yximgs.com", "wskwai.com" },
        new[] { "naver.com", "pstatic.net" },
        new[] { "sooplive.com", "sooplive.co.kr", "afreecatv.com" }
    };
    private bool _loginKeepNoticeShown;
    private bool _initializingKeepLoginCheckbox;
    private bool _ytDlpForceUpdateChecked;
    private bool _denoReadyChecked;
    
    // Conversion Cancellation
    private CancellationTokenSource? _webmCts;
    private CancellationTokenSource? _codecCts;
    private CancellationTokenSource? _audioCts;
    private CancellationTokenSource? _ytDlpCts;
    private int _lastWidth = 800;
    private int _lastHeight = 600;
    private const string CURR_VERSION = "1.3.6";
    private static readonly string UpdateFailureMarkerPath = Path.Combine(SettingsManager.UserDataFolder, "update-error.txt");

    // [Twitter/X Private Extraction] Captured Data
    private string _capturedM3u8Url = "";
    private readonly object _capturedMediaLock = new object();
    private readonly List<string> _capturedMediaUrls = new List<string>();
    private readonly HashSet<string> _pendingAnilifeMediaRequestIds = new(StringComparer.Ordinal);
    private string _capturedAnilifeManifestUrl = "";
    private string _capturedXStatusId = "";
    private int _genericMediaCaptureDepth;
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
    private RoundButton? btnLoginBrowserFavorite;
    private System.Windows.Forms.Timer? _loginFolderAnimationTimer;
    private System.Windows.Forms.Timer? _loginFolderLongPressTimer;
    private System.Windows.Forms.Timer? _loginFolderWiggleTimer;
    private Panel? _pressedLoginFolderTile;
    private Point _loginFolderPressScreenPoint;
    private bool _loginFolderEditMode;
    private bool _loginFolderDragging;
    private bool _suppressLoginFolderOpen;
    private int _loginFolderWigglePhase;
    private string _activeLoginBookmarkGroup = string.Empty;
    private LoginFolderDropIndicator? _loginFolderDropIndicator;
    private Panel? _loginFolderDropHighlightedTile;
    private Color _loginFolderDropHighlightedOriginalColor;
    private LoginFolderDisplayItem? _loginFolderDropTargetItem;
    private string _loginFolderDropTargetGroup = string.Empty;
    private bool _loginFolderDropInsertAfter;
    private bool _loginFolderDropCreatesGroup;
    private bool _loginFolderDropOnGroupTile;
    private bool _loginFolderDropPreviewActive;
    private bool _loginFolderOuterDropPreviewActive;
    private bool _loginFolderOpening;
    private int _loginFolderAnimationFrame;
    private const int LoginFolderAnimationFrames = 10;
    private static readonly Size LoginFolderStartSize = new Size(220, 160);
    private static readonly Size LoginFolderTargetSize = new Size(470, 365);
    private bool _isLoginBrowserMode = false;
    private string _loginBrowserDownloadTitle = "";
    private bool _webViewUsingXProfile = false;
    private static readonly string XWebViewDataFolder = Path.Combine(SettingsManager.UserDataFolder, "BrowserData_X");

    private sealed class LoginFolderTileInfo
    {
        public LoginFolderDisplayItem? Item { get; init; }
        public string GroupName { get; init; } = string.Empty;
        public bool IsGroup { get; init; }
    }

    private sealed class LoginFolderBuiltInDefinition
    {
        public LoginFolderBuiltInDefinition(string appId, string title, string iconText, Color iconColor, string iconFileName)
        {
            AppId = appId;
            Title = title;
            IconText = iconText;
            IconColor = iconColor;
            IconFileName = iconFileName;
        }

        public string AppId { get; }
        public string Title { get; }
        public string IconText { get; }
        public Color IconColor { get; }
        public string IconFileName { get; }
    }

    private sealed class LoginFolderDisplayItem
    {
        public LoginBrowserBookmark? Bookmark { get; init; }
        public LoginBrowserBuiltInAppLayout? BuiltInLayout { get; init; }
        public LoginFolderBuiltInDefinition? BuiltInDefinition { get; init; }
        public bool IsBuiltIn => BuiltInLayout != null && BuiltInDefinition != null;
        public string Title => Bookmark != null ? GetBookmarkDisplayTitle(Bookmark) : BuiltInDefinition?.Title ?? string.Empty;
        public string GroupName
        {
            get => Bookmark?.GroupName ?? BuiltInLayout?.GroupName ?? string.Empty;
            set
            {
                if (Bookmark != null) Bookmark.GroupName = value;
                if (BuiltInLayout != null) BuiltInLayout.GroupName = value;
            }
        }
        public int SortOrder
        {
            get => Bookmark?.SortOrder ?? BuiltInLayout?.SortOrder ?? int.MaxValue;
            set
            {
                if (Bookmark != null) Bookmark.SortOrder = value;
                if (BuiltInLayout != null) BuiltInLayout.SortOrder = value;
            }
        }
    }

    private static readonly LoginFolderBuiltInDefinition[] LoginFolderBuiltInApps =
    {
        new("instagram", "Instagram", "IG", Color.FromArgb(190, 24, 93), "instagram.png"),
        new("chzzk", "\uCE58\uC9C0\uC9C1", "CZ", Color.FromArgb(22, 163, 74), "chzzk.png"),
        new("soop", "SOOP", "SOOP", Color.FromArgb(2, 132, 199), "soop.png"),
        new("x", "X", "X", Color.Black, "x.png"),
        new("web", "\uC6F9 \uBE0C\uB77C\uC6B0\uC800", "...", Color.FromArgb(71, 85, 105), "etc.png")
    };

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
        CleanupManager.DeleteStaleCookieExports();
        ConfigureUpdatedAppForeground();
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
        Task.Run(async () =>
        {
            CleanupOldVersionedExecutables();
            await Task.Delay(TimeSpan.FromSeconds(15));
            CleanupStaleUpdateArtifacts();
        });

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
        ConfigureYoutubeQueueCopy();
        ConfigureYtDlpQueueColumns();
        HideLegacyPrivateModeToggles();
        ConfigureMiniEditorVisibility();
        ConfigureLoginDownloadHelp();
        ConfigureDownloadRuleSettings();
        ConfigureDownloadOptionControls();
        ConfigureBatchUrlPaste();
        ConfigureWidgetModeSettings();
        NormalizeKoreanUiText(initialPath);
        ConfigureVersionArchiveButton();
        ConfigureExperimentalFeaturesMenu();
        ConfigureToolRepairButton();
        ConfigureInquiryButton();
        ConfigureSettingsAboutCard();
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

        string lastSeenVersion = SettingsManager.Settings.LastSeenVersion?.Trim() ?? "";
        bool currentVersionValid = Version.TryParse(CURR_VERSION.Trim().TrimStart('v', 'V'), out Version? currentVersion);
        bool lastSeenVersionValid = Version.TryParse(lastSeenVersion.TrimStart('v', 'V'), out Version? previousVersion);

        if (string.IsNullOrEmpty(lastSeenVersion) || !currentVersionValid || !lastSeenVersionValid)
        {
            SettingsManager.Settings.LastSeenVersion = CURR_VERSION;
            SettingsManager.Save();
        }
        else if (currentVersion!.CompareTo(previousVersion) > 0)
        {
            _ = Task.Run(async () => {
                string changelog = await GetServerChangelogAsync();

                this.Invoke((MethodInvoker)delegate {
                    string title = $"Multi Media Toolkit [v{CURR_VERSION}]";
                    ShowCenteredMessage(changelog, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

                    SettingsManager.Settings.LastSeenVersion = CURR_VERSION;
                    SettingsManager.Save();
                });
            });
        }
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
        lblYtDlpDesc.Text = "치지직, Instagram, Threads, TikTok, Kuaishou, SOOP, Pinterest, X, Vimeo, Anilife, Linkkf 등 다양한 사이트를 지원합니다.\n로그인이 필요한 회원전용/나이제한 영상은 로그인 후 좌측 상단 즉시 다운로드 또는 URL 입력으로 받을 수 있습니다. YouTube 일부공개 영상은 URL만 있으면 유튜브 다운로더에서 받을 수 있습니다.";
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

            string currentDir = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            foreach (var path in oldPaths)
            {
                string oldExecutable = Path.Combine(path, "Multi Media Toolkit.exe");
                string oldDirectory = Path.GetFullPath(path)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (Directory.Exists(path) &&
                    File.Exists(oldExecutable) &&
                    !currentDir.StartsWith(oldDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    Directory.Delete(path, true);
                }
            }
        }
        catch {  }
    }

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


        if (SettingsManager.Settings.AutoUpdateCheck && !Program.StartedAfterFailedUpdate)
        {
            Shown += async (s, e) =>
            {
                await Task.Delay(1000);
                if (!IsDisposed && !Disposing)
                    await CheckForUpdateAsync(false);
            };
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

        var copyUrlMenuItem = new ToolStripMenuItem("URL \uC8FC\uC18C \uBCF5\uC0AC")
        {
            ShortcutKeyDisplayString = "Ctrl+C"
        };
        copyUrlMenuItem.Click += (s, e) => CopySelectedYtDlpUrls();
        contextMenuYtDlpRemove.Items.Insert(0, copyUrlMenuItem);

        lvYtDlpQueue.KeyDown += (s, e) =>
        {
            if (!e.Control || e.KeyCode != Keys.C) return;
            CopySelectedYtDlpUrls();
            e.SuppressKeyPress = true;
            e.Handled = true;
        };

        lvYtDlpQueue.MouseDown += (s, e) =>
        {
            if (e.Button != MouseButtons.Right) return;

            ListViewItem? clickedItem = lvYtDlpQueue.GetItemAt(e.X, e.Y);
            if (clickedItem == null)
            {
                foreach (ListViewItem item in lvYtDlpQueue.SelectedItems.Cast<ListViewItem>().ToList())
                    item.Selected = false;
                return;
            }

            if (!clickedItem.Selected)
            {
                foreach (ListViewItem item in lvYtDlpQueue.SelectedItems.Cast<ListViewItem>().ToList())
                    item.Selected = false;
                clickedItem.Selected = true;
            }
            clickedItem.Focused = true;
        };

        contextMenuYtDlpRemove.Opening += (s, e) =>
        {
            bool hasSelection = lvYtDlpQueue.SelectedItems.Count > 0;
            copyUrlMenuItem.Enabled = hasSelection;
            menuYtDlpRemoveSelected.Enabled = hasSelection;
        };
    }

    private void ConfigureYoutubeQueueCopy()
    {
        var copyUrlMenuItem = new ToolStripMenuItem("URL 주소 복사")
        {
            ShortcutKeyDisplayString = "Ctrl+C"
        };
        copyUrlMenuItem.Click += (s, e) => CopySelectedYoutubeUrls();
        contextMenuRemove.Items.Insert(0, copyUrlMenuItem);

        lvQueue.KeyDown += (s, e) =>
        {
            if (!e.Control || e.KeyCode != Keys.C) return;
            CopySelectedYoutubeUrls();
            e.SuppressKeyPress = true;
            e.Handled = true;
        };

        lvQueue.MouseDown += (s, e) =>
        {
            if (e.Button != MouseButtons.Right) return;

            ListViewItem? clickedItem = lvQueue.GetItemAt(e.X, e.Y);
            if (clickedItem == null)
            {
                foreach (ListViewItem item in lvQueue.SelectedItems.Cast<ListViewItem>().ToList())
                    item.Selected = false;
                return;
            }

            if (!clickedItem.Selected)
            {
                foreach (ListViewItem item in lvQueue.SelectedItems.Cast<ListViewItem>().ToList())
                    item.Selected = false;
                clickedItem.Selected = true;
            }
            clickedItem.Focused = true;
        };

        contextMenuRemove.Opening += (s, e) =>
        {
            bool hasSelection = lvQueue.SelectedItems.Count > 0;
            copyUrlMenuItem.Enabled = hasSelection;
            menuRemoveSelected.Enabled = hasSelection;
        };
    }

    private void CopySelectedYoutubeUrls()
    {
        string[] urls = lvQueue.SelectedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag is DownloadJob job ? job.Url.Trim() : string.Empty)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (urls.Length == 0) return;

        string text = string.Join(Environment.NewLine, urls);
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                Clipboard.SetText(text);
                lblStatus.Text = urls.Length == 1
                    ? "URL 주소를 복사했습니다."
                    : $"{urls.Length}개 URL 주소를 복사했습니다.";
                return;
            }
            catch (System.Runtime.InteropServices.ExternalException) when (attempt < 2)
            {
                Thread.Sleep(30);
            }
        }

        ShowCenteredMessage("URL을 클립보드에 복사하지 못했습니다.", "URL 복사", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void CopySelectedYtDlpUrls()
    {
        string[] urls = lvYtDlpQueue.SelectedItems
            .Cast<ListViewItem>()
            .Select(item => item.SubItems.Count > 0 ? item.SubItems[0].Text.Trim() : item.Text.Trim())
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (urls.Length == 0) return;

        string text = string.Join(Environment.NewLine, urls);
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                Clipboard.SetText(text);
                lblYtDlpStatus.Text = urls.Length == 1
                    ? "URL \uC8FC\uC18C\uB97C \uBCF5\uC0AC\uD588\uC2B5\uB2C8\uB2E4."
                    : $"{urls.Length}\uAC1C URL \uC8FC\uC18C\uB97C \uBCF5\uC0AC\uD588\uC2B5\uB2C8\uB2E4.";
                return;
            }
            catch (System.Runtime.InteropServices.ExternalException) when (attempt < 2)
            {
                Thread.Sleep(30);
            }
        }
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
        lblYtDlpDesc.Text = "치지직, Instagram, Threads, TikTok, Kuaishou, SOOP, Pinterest, X, Vimeo, Anilife, Linkkf 등 다양한 사이트를 지원합니다.\n로그인이 필요한 회원전용/나이제한 영상은 로그인 후 좌측 상단 즉시 다운로드 또는 URL 입력으로 받을 수 있습니다. YouTube 일부공개 영상은 URL만 있으면 유튜브 다운로더에서 받을 수 있습니다.";

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
            "로그인이 필요한 나이제한, 회원전용, 팔로워/구독자 공개 영상을 받을 때 사용합니다. YouTube 일부공개 영상은 로그인 없이 URL만으로 유튜브 다운로더에서 받을 수 있습니다.\n\n" +
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

        group.Controls.Add(new Label { Text = "동시 다운로드", AutoSize = true, Location = new Point(285, 188), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
        cmbConcurrentDownloads = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(390, 184),
            Size = new Size(150, 23),
            Font = new Font("Segoe UI", 9F)
        };
        cmbConcurrentDownloads.Items.AddRange(new object[] { "안정적 (1개)", "균형 (3개)", "빠르게 (5개)" });
        group.Controls.Add(cmbConcurrentDownloads);

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

    private void ConfigureDownloadOptionControls()
    {
        chkYoutubeEmbedMetadata = CreateDownloadOptionCheckBox("썸네일·게시 정보 포함", Point.Empty);
        chkYoutubeSponsorBlock = CreateDownloadOptionCheckBox("스폰서 구간 제거", Point.Empty);
        chkYoutubeDownloadSection = CreateDownloadOptionCheckBox("구간만 받기", Point.Empty);
        chkYtDlpEmbedMetadata = CreateDownloadOptionCheckBox("썸네일·게시 정보 포함", Point.Empty);
        chkYtDlpDownloadSection = CreateDownloadOptionCheckBox("구간만 받기", Point.Empty);

        tabYoutube.Controls.Add(chkYoutubeEmbedMetadata);
        tabYoutube.Controls.Add(chkYoutubeSponsorBlock);
        tabYoutube.Controls.Add(chkYoutubeDownloadSection);
        tabYtDlp.Controls.Add(chkYtDlpEmbedMetadata);
        tabYtDlp.Controls.Add(chkYtDlpDownloadSection);

        chkYoutubeEmbedMetadata.Checked = SettingsManager.Settings.YoutubeEmbedMetadata;
        chkYoutubeSponsorBlock.Checked = SettingsManager.Settings.YoutubeRemoveSponsorSegments;
        chkYtDlpEmbedMetadata.Checked = SettingsManager.Settings.WebsiteEmbedMetadata;
        chkYoutubeEmbedMetadata.CheckedChanged += DownloadOptionSettings_CheckedChanged;
        chkYoutubeSponsorBlock.CheckedChanged += DownloadOptionSettings_CheckedChanged;
        chkYtDlpEmbedMetadata.CheckedChanged += DownloadOptionSettings_CheckedChanged;

        var tip = new ToolTip { ShowAlways = true };
        tip.SetToolTip(chkYoutubeEmbedMetadata, "파일에 썸네일, 제목, 게시자와 챕터 정보를 함께 저장합니다.");
        tip.SetToolTip(chkYtDlpEmbedMetadata, "사이트가 제공하는 경우 썸네일과 게시 정보를 파일에 함께 저장합니다.");
        tip.SetToolTip(chkYoutubeSponsorBlock, "YouTube의 스폰서 구간이 있으면 제거합니다. 구간이 없으면 원본 영상을 그대로 저장합니다.");
        tip.SetToolTip(chkYoutubeDownloadSection, "다운로드할 시작과 종료 시간을 지정합니다.");
        tip.SetToolTip(chkYtDlpDownloadSection, "다운로드할 시작과 종료 시간을 지정합니다.");

        LayoutDownloadOptionControls();
        chkYoutubeEmbedMetadata.BringToFront();
        chkYoutubeSponsorBlock.BringToFront();
        chkYoutubeDownloadSection.BringToFront();
        chkYtDlpEmbedMetadata.BringToFront();
        chkYtDlpDownloadSection.BringToFront();
    }

    private void DownloadOptionSettings_CheckedChanged(object? sender, EventArgs e)
    {
        SettingsManager.Settings.YoutubeEmbedMetadata = chkYoutubeEmbedMetadata?.Checked == true;
        SettingsManager.Settings.YoutubeRemoveSponsorSegments = chkYoutubeSponsorBlock?.Checked == true;
        SettingsManager.Settings.WebsiteEmbedMetadata = chkYtDlpEmbedMetadata?.Checked == true;
        SettingsManager.Save();
    }

    private void LayoutDownloadOptionControls()
    {
        if (chkYoutubeEmbedMetadata != null && chkYoutubeSponsorBlock != null && chkYoutubeDownloadSection != null)
        {
            int left = chkYoutubeDownloadSubtitles.Right + 18;
            if (cmbYoutubeSubtitleLanguage?.Visible == true)
                left = cmbYoutubeSubtitleLanguage.Right + 18;

            chkYoutubeEmbedMetadata.Location = new Point(left, chkYoutubeDownloadSubtitles.Top);
            chkYoutubeSponsorBlock.Location = new Point(chkYoutubeEmbedMetadata.Right + 18, chkYoutubeDownloadSubtitles.Top);
            chkYoutubeDownloadSection.Location = new Point(chkYoutubeSponsorBlock.Right + 18, chkYoutubeDownloadSubtitles.Top);
        }

        if (chkYtDlpEmbedMetadata != null && chkYtDlpDownloadSection != null)
        {
            int left = chkKeepLoginSession?.Right + 18 ?? chkYtDlpDownloadSubtitles.Right + 18;
            chkYtDlpEmbedMetadata.Location = new Point(left, chkYtDlpDownloadSubtitles.Top);
            chkYtDlpDownloadSection.Location = new Point(chkYtDlpEmbedMetadata.Right + 18, chkYtDlpDownloadSubtitles.Top);
        }
    }

    private static CheckBox CreateDownloadOptionCheckBox(string text, Point location)
    {
        return new CheckBox
        {
            Text = text,
            AutoSize = true,
            Location = location,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 80, 80),
            BackColor = Color.Transparent,
            UseVisualStyleBackColor = true
        };
    }

    private bool TryPromptDownloadSection(
        out double startSeconds,
        out double endSeconds,
        double? maximumSeconds = null)
    {
        startSeconds = 0;
        endSeconds = 0;
        if (maximumSeconds is > 0)
            maximumSeconds = Math.Floor(maximumSeconds.Value);

        using var dialog = new Form
        {
            Text = "구간만 받기",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(430, 205),
            Font = new Font("Segoe UI", 9F)
        };

        dialog.Controls.Add(new Label
        {
            Text = "받을 영상 구간을 입력해 주세요.",
            AutoSize = true,
            Location = new Point(20, 18),
            Font = new Font("Segoe UI", 11F, FontStyle.Bold)
        });
        dialog.Controls.Add(new Label
        {
            Text = maximumSeconds is > 0
                ? $"시간 형식: 01:30  ·  영상 최대: {FormatSectionTime(maximumSeconds.Value)}"
                : "시간 형식: 01:30 또는 00:01:30",
            AutoSize = true,
            Location = new Point(20, 48),
            ForeColor = Color.Gray
        });

        var startBox = new TextBox
        {
            Location = new Point(95, 82),
            Size = new Size(300, 25),
            Text = "00:00"
        };
        var endBox = new TextBox
        {
            Location = new Point(95, 118),
            Size = new Size(300, 25),
            Text = maximumSeconds is > 0
                ? FormatSectionTime(maximumSeconds.Value)
                : ""
        };
        dialog.Controls.Add(new Label { Text = "시작", AutoSize = true, Location = new Point(20, 86) });
        dialog.Controls.Add(new Label { Text = "종료", AutoSize = true, Location = new Point(20, 122) });
        dialog.Controls.Add(startBox);
        dialog.Controls.Add(endBox);

        var okButton = new RoundButton
        {
            Text = "적용",
            DialogResult = DialogResult.OK,
            Location = new Point(235, 158),
            Size = new Size(75, 32),
            BackColor = Color.FromArgb(2, 132, 199),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            BorderRadius = 14
        };
        okButton.FlatAppearance.BorderSize = 0;
        var cancelButton = new RoundButton
        {
            Text = "취소",
            DialogResult = DialogResult.Cancel,
            Location = new Point(320, 158),
            Size = new Size(75, 32),
            BackColor = Color.FromArgb(255, 241, 242),
            ForeColor = Color.FromArgb(225, 29, 72),
            FlatStyle = FlatStyle.Flat,
            BorderRadius = 14
        };
        cancelButton.FlatAppearance.BorderSize = 0;
        dialog.Controls.Add(okButton);
        dialog.Controls.Add(cancelButton);
        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;

        while (dialog.ShowDialog(this) == DialogResult.OK)
        {
            if (TryParseSectionTime(startBox.Text, out startSeconds) &&
                TryParseSectionTime(endBox.Text, out endSeconds) &&
                endSeconds > startSeconds)
            {
                if (maximumSeconds is > 0 &&
                    (startSeconds >= maximumSeconds.Value || endSeconds > maximumSeconds.Value))
                {
                    ShowCenteredMessage(
                        $"이 영상은 최대 {FormatSectionTime(maximumSeconds.Value)}까지 입력할 수 있습니다.",
                        "구간 확인",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    dialog.DialogResult = DialogResult.None;
                    continue;
                }

                return true;
            }

            ShowCenteredMessage(
                "종료 시간은 시작 시간보다 뒤에 있어야 합니다.\n예: 시작 01:20, 종료 02:10",
                "구간 확인",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            dialog.DialogResult = DialogResult.None;
        }

        startSeconds = 0;
        endSeconds = 0;
        return false;
    }

    private static bool TryParseSectionTime(string value, out double totalSeconds)
    {
        totalSeconds = 0;
        string[] parts = (value ?? "").Trim().Split(':');
        if (parts.Length is < 1 or > 3) return false;

        double multiplier = 1;
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            if (!double.TryParse(
                    parts[i],
                    System.Globalization.NumberStyles.AllowDecimalPoint,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double part) || part < 0)
            {
                return false;
            }

            if (i > 0 && part >= 60) return false;
            totalSeconds += part * multiplier;
            multiplier *= 60;
        }

        return double.IsFinite(totalSeconds);
    }

    private static string FormatSectionTime(double seconds)
    {
        TimeSpan value = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return value.TotalHours >= 1
            ? $"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}"
            : $"{value.Minutes:00}:{value.Seconds:00}";
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024) return $"{bytes / (1024d * 1024 * 1024):0.0} GB";
        return $"{bytes / (1024d * 1024):0} MB";
    }

    private bool ConfirmDownloadSpace(string path, long estimatedBytes = 0)
    {
        try
        {
            string fullPath = Path.GetFullPath(path);
            string root = Path.GetPathRoot(fullPath) ?? "";
            if (string.IsNullOrWhiteSpace(root)) return true;

            var drive = new DriveInfo(root);
            if (!drive.IsReady) return true;

            long available = drive.AvailableFreeSpace;
            long reserve = Math.Max(512L * 1024 * 1024, estimatedBytes / 20);
            bool insufficient = estimatedBytes > 0 && available < estimatedBytes + reserve;
            bool criticallyLow = estimatedBytes == 0 && available < 512L * 1024 * 1024;
            if (!insufficient && !criticallyLow) return true;

            string estimateText = estimatedBytes > 0
                ? $"\n예상 다운로드 용량: 약 {FormatFileSize(estimatedBytes)}"
                : "";
            return ShowCenteredMessage(
                $"저장 공간이 부족할 수 있습니다.{estimateText}\n현재 사용 가능 공간: {FormatFileSize(available)}\n\n그대로 대기열에 추가할까요?",
                "저장 공간 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.Yes;
        }
        catch
        {
            return true;
        }
    }

    private void ConfigureBatchUrlPaste()
    {
        txtUrl.KeyDown += BatchUrlTextBox_KeyDown;
        txtYtDlpUrl.KeyDown += BatchUrlTextBox_KeyDown;
    }

    private async void BatchUrlTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!e.Control || e.KeyCode != Keys.V) return;

        List<string> urls = ExtractHttpUrls(GetClipboardText());
        if (urls.Count < 2) return;

        e.Handled = true;
        e.SuppressKeyPress = true;
        await ConfirmAndQueueMultipleUrlsAsync(urls, ReferenceEquals(sender, txtUrl));
    }

    private static List<string> ExtractHttpUrls(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        var starts = System.Text.RegularExpressions.Regex.Matches(
            text,
            @"https?://",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var urls = new List<string>();
        for (int i = 0; i < starts.Count; i++)
        {
            int start = starts[i].Index;
            int end = i + 1 < starts.Count ? starts[i + 1].Index : text.Length;
            string candidate = text[start..end].Trim();
            int whitespace = candidate.IndexOfAny(new[] { ' ', '\t', '\r', '\n' });
            if (whitespace >= 0) candidate = candidate[..whitespace];
            candidate = candidate.TrimEnd(',', ';', ')', ']', '}', '>', '"', '\'', '.', '。');

            if (TryNormalizeHttpUrl(candidate, out string normalized) &&
                !urls.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                urls.Add(normalized);
            }
        }

        return urls;
    }

    private async Task ConfirmAndQueueMultipleUrlsAsync(IReadOnlyList<string> urls, bool fromYoutubeTab)
    {
        using var dialog = new Form
        {
            Text = "여러 URL 확인",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(660, 390),
            Font = new Font("Segoe UI", 9F),
            ShowInTaskbar = false
        };
        var title = new Label
        {
            Text = $"다운로드할 URL {urls.Count}개를 확인해 주세요.",
            AutoSize = true,
            Location = new Point(20, 18),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold)
        };
        var hint = new Label
        {
            Text = "잘못 나뉜 주소가 있다면 체크를 해제하세요.",
            AutoSize = true,
            Location = new Point(20, 50),
            ForeColor = Color.Gray
        };
        var list = new CheckedListBox
        {
            Location = new Point(20, 80),
            Size = new Size(620, 245),
            CheckOnClick = true,
            HorizontalScrollbar = true
        };
        foreach (string url in urls) list.Items.Add(url, true);

        var addButton = new RoundButton
        {
            Text = "선택 항목 대기열 추가",
            Location = new Point(390, 340),
            Size = new Size(160, 34),
            BackColor = Color.FromArgb(2, 132, 199),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            BorderRadius = 14,
            DialogResult = DialogResult.OK
        };
        addButton.FlatAppearance.BorderSize = 0;
        var cancelButton = new RoundButton
        {
            Text = "취소",
            Location = new Point(560, 340),
            Size = new Size(80, 34),
            BackColor = Color.FromArgb(255, 241, 242),
            ForeColor = Color.FromArgb(225, 29, 72),
            FlatStyle = FlatStyle.Flat,
            BorderRadius = 14,
            DialogResult = DialogResult.Cancel
        };
        cancelButton.FlatAppearance.BorderSize = 0;
        dialog.Controls.AddRange(new Control[] { title, hint, list, addButton, cancelButton });
        dialog.AcceptButton = addButton;
        dialog.CancelButton = cancelButton;

        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var selectedUrls = list.CheckedItems.Cast<string>().ToList();
        if (selectedUrls.Count == 0) return;

        bool downloadSubtitles = fromYoutubeTab ? chkYoutubeDownloadSubtitles.Checked : chkYtDlpDownloadSubtitles.Checked;
        string subtitlePreset = fromYoutubeTab
            ? GetSelectedSubtitlePreset(cmbYoutubeSubtitleLanguage)
            : GetSelectedSubtitlePreset(cmbYtDlpSubtitleLanguage);
        bool embedMetadata = fromYoutubeTab
            ? chkYoutubeEmbedMetadata?.Checked == true
            : chkYtDlpEmbedMetadata?.Checked == true;
        bool removeSponsors = fromYoutubeTab && chkYoutubeSponsorBlock?.Checked == true;
        bool useSection = fromYoutubeTab
            ? chkYoutubeDownloadSection?.Checked == true
            : chkYtDlpDownloadSection?.Checked == true;
        double sectionStartSeconds = 0;
        double sectionEndSeconds = 0;
        if (useSection && !TryPromptDownloadSection(out sectionStartSeconds, out sectionEndSeconds)) return;

        SelectMainTab(btnTabYtDlp, tabYtDlp);
        int queued = 0;
        foreach (string url in selectedUrls)
        {
            if (EnqueueYtDlpDownload(
                url,
                allowFolderPrompt: true,
                out _,
                sourceFeature: "여러 URL 다운로드",
                downloadSubtitles: downloadSubtitles,
                subtitlePreset: subtitlePreset,
                embedMetadata: embedMetadata,
                removeSponsorSegments: removeSponsors && LooksLikeYouTubeInput(url),
                sectionStartSeconds: sectionStartSeconds,
                sectionEndSeconds: sectionEndSeconds))
            {
                queued++;
            }
        }

        lblYtDlpStatus.Text = $"선택한 URL 중 {queued}개를 대기열에 추가했습니다.";
        await Task.CompletedTask;
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
                BackColor = Color.FromArgb(226, 232, 240),
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

    private void ConfigureInquiryButton()
    {
        if (btnInquiry != null) return;

        btnInquiry = new RoundButton
        {
            Name = "btnInquiry",
            Text = "문의하기",
            Size = new Size(104, 34),
            Location = new Point(Math.Max(20, tabSettings.ClientSize.Width - 124), 16),
            BorderRadius = 14,
            BackColor = Color.FromArgb(226, 232, 240),
            ForeColor = Color.FromArgb(51, 65, 85),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            UseVisualStyleBackColor = false
        };
        btnInquiry.FlatAppearance.BorderSize = 0;
        btnInquiry.Click += BtnInquiry_Click;
        tabSettings.Controls.Add(btnInquiry);
        btnInquiry.BringToFront();

        var tip = new ToolTip { ShowAlways = true };
        tip.SetToolTip(btnInquiry, "문제나 의견을 제작자에게 바로 보냅니다.");
    }

    private async void BtnInquiry_Click(object? sender, EventArgs e)
    {
        await ShowInquiryDialogAsync();
    }

    private async Task ShowInquiryDialogAsync(string initialTitle = "", string initialMessage = "")
    {
        if (DateTime.Now - _lastInquirySentAt < TimeSpan.FromSeconds(30))
        {
            ShowCenteredMessage("문의는 30초 후에 다시 보낼 수 있습니다.", "문의하기", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new InquiryForm(initialTitle, initialMessage);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        string originalText = btnInquiry?.Text ?? "문의하기";
        if (btnInquiry != null)
        {
            btnInquiry.Enabled = false;
            btnInquiry.Text = "전송 중...";
        }
        try
        {
            bool sent = await SendInquiryAsync(dialog.InquiryTitle, dialog.Message);
            if (!sent)
            {
                ShowCenteredMessage(
                    "문의를 전송하지 못했습니다. 인터넷 연결을 확인한 뒤 다시 시도해 주세요.",
                    "문의 전송 실패",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _lastInquirySentAt = DateTime.Now;
            ShowCenteredMessage("문의가 전송되었습니다.", "문의 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch
        {
            ShowCenteredMessage(
                "문의를 전송하지 못했습니다. 잠시 후 다시 시도해 주세요.",
                "문의 전송 실패",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        finally
        {
            if (btnInquiry != null)
            {
                btnInquiry.Text = originalText;
                btnInquiry.Enabled = true;
            }
        }
    }

    private async Task<bool> SendInquiryAsync(string inquiryTitle, string message)
    {
        string p1 = "ht";
        string p2 = "tps://discord.com/api/webhooks/1526570399943491744/";
        string p3 = "j9x37o55fFAtf5TeJ1A28RDnkJ8aaUhhnWCAJ9cdlnisnYrUwZzS-1ncm94dW2sI_2Xu";
        string webhookUrl = p1 + p2 + p3;
        string safeTitle = SanitizeInquiryText(inquiryTitle, 100, "제목 없음");
        string safeMessage = SanitizeInquiryText(message, 1000, "내용 없음");
        string installId = string.IsNullOrWhiteSpace(SettingsManager.Settings.InstallId)
            ? "미생성"
            : SettingsManager.Settings.InstallId.Trim();

        var payload = new
        {
            username = "MMT 문의 접수",
            allowed_mentions = new { parse = Array.Empty<string>() },
            content =
                "**[사용자 문의]**\n" +
                $"**기기 ID**: `{installId}`\n" +
                $"**제목**: {safeTitle}\n\n" +
                $"**문의 내용**\n{safeMessage}\n\n" +
                $"**시각**: `{DateTime.Now:yyyy-MM-dd HH:mm:ss}`"
        };

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using HttpResponseMessage response = await _httpClient.PostAsync(
            webhookUrl,
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            timeout.Token);
        return response.IsSuccessStatusCode;
    }

    private static string SanitizeInquiryText(string? text, int maxLength, string emptyValue)
    {
        if (string.IsNullOrWhiteSpace(text)) return emptyValue;
        string value = text.Trim()
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace("```", "'''", StringComparison.Ordinal)
            .Replace('@', '＠');
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private void ConfigureExperimentalFeaturesMenu()
    {
        if (btnExperimentalFeatures == null)
        {
            btnExperimentalFeatures = new RoundButton
            {
                Name = "btnExperimentalFeatures",
                Text = "실험 기능",
                Size = new Size(120, 34),
                BorderRadius = 14,
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };
            btnExperimentalFeatures.FlatAppearance.BorderSize = 0;
            btnExperimentalFeatures.Click += (s, e) => ShowExperimentalFeaturesMenu();
            tabSettings.Controls.Add(btnExperimentalFeatures);

            var tip = new ToolTip { ShowAlways = true };
            tip.SetToolTip(btnExperimentalFeatures, "정식 적용 전 기능을 선택해서 시험해 볼 수 있습니다.");
        }

        int left = btnOpenVersionArchive?.Right + 10 ?? 175;
        btnExperimentalFeatures.Location = new Point(left, Math.Max(420, tabSettings.ClientSize.Height - 54));
        btnExperimentalFeatures.BringToFront();
    }

    private void ConfigureToolRepairButton()
    {
        if (btnRepairTools == null)
        {
            btnRepairTools = new RoundButton
            {
                Name = "btnRepairTools",
                Text = "필수 도구 복구",
                Size = new Size(145, 34),
                BorderRadius = 14,
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };
            btnRepairTools.FlatAppearance.BorderSize = 0;
            btnRepairTools.Click += BtnRepairTools_Click;
            tabSettings.Controls.Add(btnRepairTools);

            var tip = new ToolTip { ShowAlways = true };
            tip.SetToolTip(btnRepairTools, "FFmpeg, ffprobe, yt-dlp와 YouTube 실행 환경을 확인하고 손상된 도구만 복구합니다.");
        }

        int left = btnExperimentalFeatures?.Right + 10 ?? 305;
        btnRepairTools.Location = new Point(left, Math.Max(420, tabSettings.ClientSize.Height - 54));
        btnRepairTools.BringToFront();
    }

    private async void BtnRepairTools_Click(object? sender, EventArgs e)
    {
        if (btnRepairTools == null || !btnRepairTools.Enabled) return;

        string originalText = btnRepairTools.Text;
        btnRepairTools.Enabled = false;
        btnRepairTools.Text = "확인 중...";
        try
        {
            bool repairNeeded =
                !IsValidWindowsExecutable(SettingsManager.GetFFmpegPath()) ||
                !IsValidWindowsExecutable(SettingsManager.GetFFprobePath()) ||
                !IsValidWindowsExecutable(SettingsManager.GetYtDlpPath()) ||
                !IsValidWindowsExecutable(SettingsManager.GetDenoPath());

            if (!repairNeeded)
            {
                ShowCenteredMessage("필수 도구가 모두 정상입니다.\n별도의 복구가 필요하지 않습니다.", "필수 도구 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _ytDlpForceUpdateChecked = false;
            _denoReadyChecked = false;
            await EnsureFFmpegAsync();
            await EnsureYtDlpAsync(forceUpdate: true);
            await EnsureDenoAsync();

            if (!IsValidWindowsExecutable(SettingsManager.GetFFmpegPath()) ||
                !IsValidWindowsExecutable(SettingsManager.GetFFprobePath()) ||
                !IsValidWindowsExecutable(SettingsManager.GetYtDlpPath()) ||
                !IsValidWindowsExecutable(SettingsManager.GetDenoPath()))
            {
                throw new InvalidDataException("일부 필수 도구를 확인할 수 없습니다.");
            }

            ShowCenteredMessage("필수 도구 확인과 복구가 완료되었습니다.", "필수 도구 복구", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            ShowCenteredMessage(
                UserErrorFormatter.Format("필수 도구를 복구하지 못했습니다. 인터넷 연결을 확인한 뒤 다시 시도해 주세요.", ex),
                "필수 도구 복구 실패",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            ReportError("설정", "필수 도구 복구", "필수 도구 수동 복구 실패", ex);
        }
        finally
        {
            if (!IsDisposed && btnRepairTools != null)
            {
                btnRepairTools.Text = originalText;
                btnRepairTools.Enabled = true;
            }
        }
    }

    private void ConfigureSettingsAboutCard()
    {
        lblAbout.Visible = true;
        lblAbout.Enabled = true;
        lblAbout.AutoSize = true;
        lblAbout.MaximumSize = Size.Empty;
        lblAbout.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        lblAbout.TextAlign = ContentAlignment.BottomRight;
        lblAbout.BackColor = tabSettings.BackColor;
        lblAbout.Cursor = Cursors.Default;
        lblAbout.Size = lblAbout.GetPreferredSize(Size.Empty);

        tabSettings.Resize -= TabSettings_ResizeAboutCard;
        tabSettings.Resize += TabSettings_ResizeAboutCard;
        PositionSettingsAboutCard();
    }

    private void TabSettings_ResizeAboutCard(object? sender, EventArgs e)
    {
        PositionSettingsAboutCard();
    }

    private void PositionSettingsAboutCard()
    {
        int left = Math.Max(310, tabSettings.ClientSize.Width - lblAbout.Width - 18);
        int top = Math.Max(0, tabSettings.ClientSize.Height - lblAbout.Height - 12);
        lblAbout.Location = new Point(left, top);
        lblAbout.BringToFront();
    }

    private void ShowExperimentalFeaturesMenu()
    {
        EnsureExperimentalFeaturesMenu();
        if (panelExperimentalFeaturesOverlay == null) return;

        ReloadExperimentalFeaturesUI();
        panelExperimentalFeaturesOverlay.Visible = true;
        panelExperimentalFeaturesOverlay.BringToFront();
        PositionExperimentalFeaturesMenu();
    }

    private void HideExperimentalFeaturesMenu()
    {
        if (panelExperimentalFeaturesOverlay != null)
        {
            panelExperimentalFeaturesOverlay.Visible = false;
        }
    }

    private void EnsureExperimentalFeaturesMenu()
    {
        if (panelExperimentalFeaturesOverlay != null) return;

        panelExperimentalFeaturesOverlay = new Panel
        {
            Parent = tabSettings,
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(241, 245, 249),
            Visible = false
        };
        panelExperimentalFeaturesOverlay.Click += (s, e) => HideExperimentalFeaturesMenu();
        panelExperimentalFeaturesOverlay.Resize += (s, e) => PositionExperimentalFeaturesMenu();

        panelExperimentalFeaturesMenu = new RoundPanel
        {
            Parent = panelExperimentalFeaturesOverlay,
            Size = new Size(500, 320),
            BackColor = Color.FromArgb(250, 252, 255),
            BorderRadius = 28,
            BorderColor = Color.FromArgb(203, 213, 225),
            BorderThickness = 1
        };

        var title = new Label
        {
            Parent = panelExperimentalFeaturesMenu,
            Text = "실험 기능",
            Location = new Point(28, 24),
            Size = new Size(300, 32),
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var hint = new Label
        {
            Parent = panelExperimentalFeaturesMenu,
            Text = "필요한 기능만 켜서 사용해 보세요. 언제든 다시 끌 수 있습니다.",
            Location = new Point(29, 58),
            Size = new Size(420, 24),
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Segoe UI", 9F),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var closeButton = new RoundButton
        {
            Parent = panelExperimentalFeaturesMenu,
            Text = "X",
            Size = new Size(34, 34),
            Location = new Point(442, 22),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BorderRadius = 17,
            BackColor = Color.FromArgb(226, 232, 240),
            ForeColor = Color.FromArgb(71, 85, 105),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            UseVisualStyleBackColor = false
        };
        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.Click += (s, e) => HideExperimentalFeaturesMenu();

        var featureCard = new RoundPanel
        {
            Parent = panelExperimentalFeaturesMenu,
            Location = new Point(28, 102),
            Size = new Size(444, 126),
            BackColor = Color.White,
            BorderRadius = 14,
            BorderColor = Color.FromArgb(226, 232, 240),
            BorderThickness = 1
        };

        chkEnableCompletedFileQuickUse = new CheckBox
        {
            Parent = featureCard,
            Text = "완료 파일 바로 사용",
            AutoSize = true,
            Location = new Point(20, 19),
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 11F, FontStyle.Bold)
        };
        chkEnableCompletedFileQuickUse.CheckedChanged += CompletedFileQuickUse_CheckedChanged;

        var badge = new Label
        {
            Parent = featureCard,
            Text = "실험",
            Location = new Point(342, 17),
            Size = new Size(72, 28),
            BackColor = Color.FromArgb(224, 242, 254),
            ForeColor = Color.FromArgb(3, 105, 161),
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var featureDescription = new Label
        {
            Parent = featureCard,
            Text = "다운로드가 끝나면 파일 카드를 표시합니다.\n카드를 다른 프로그램이나 폴더로 끌어 바로 사용할 수 있습니다.",
            Location = new Point(22, 55),
            Size = new Size(392, 52),
            ForeColor = Color.FromArgb(71, 85, 105),
            Font = new Font("Segoe UI", 9F),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var footer = new Label
        {
            Parent = panelExperimentalFeaturesMenu,
            Text = "실험 기능은 기본적으로 꺼져 있으며 기존 기능에는 영향을 주지 않습니다.",
            Location = new Point(29, 249),
            Size = new Size(420, 38),
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Segoe UI", 8.5F),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var doubleBufferProp = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        doubleBufferProp?.SetValue(panelExperimentalFeaturesOverlay, true, null);
        doubleBufferProp?.SetValue(panelExperimentalFeaturesMenu, true, null);
        doubleBufferProp?.SetValue(featureCard, true, null);

        PositionExperimentalFeaturesMenu();
    }

    private void PositionExperimentalFeaturesMenu()
    {
        if (panelExperimentalFeaturesOverlay == null || panelExperimentalFeaturesMenu == null) return;

        int x = Math.Max(12, (panelExperimentalFeaturesOverlay.ClientSize.Width - panelExperimentalFeaturesMenu.Width) / 2);
        int y = Math.Max(12, (panelExperimentalFeaturesOverlay.ClientSize.Height - panelExperimentalFeaturesMenu.Height) / 2);
        panelExperimentalFeaturesMenu.Location = new Point(x, y);
    }

    private void ReloadExperimentalFeaturesUI()
    {
        if (chkEnableCompletedFileQuickUse == null) return;

        _initializingExperimentalFeatures = true;
        chkEnableCompletedFileQuickUse.Checked = SettingsManager.Settings.EnableCompletedFileQuickUse;
        _initializingExperimentalFeatures = false;
    }

    private void CompletedFileQuickUse_CheckedChanged(object? sender, EventArgs e)
    {
        if (_initializingExperimentalFeatures || chkEnableCompletedFileQuickUse == null) return;

        SettingsManager.Settings.EnableCompletedFileQuickUse = chkEnableCompletedFileQuickUse.Checked;
        SettingsManager.Save();

        if (!chkEnableCompletedFileQuickUse.Checked)
        {
            _completedFileCard?.Close();
            _completedFileCard = null;
        }
    }

    private void ShowCompletedFileQuickUse(string? filePath)
    {
        if (!SettingsManager.Settings.EnableCompletedFileQuickUse ||
            string.IsNullOrWhiteSpace(filePath) ||
            !File.Exists(filePath))
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => ShowCompletedFileQuickUse(filePath)));
            return;
        }

        _completedFileCard?.Close();

        var card = new CompletedFileCardForm(filePath);
        Rectangle workArea = Screen.FromControl(this).WorkingArea;
        Point location;

        if (_downloadWidgetForm != null && !_downloadWidgetForm.IsDisposed && _downloadWidgetForm.Visible)
        {
            int x = _downloadWidgetForm.Left + (_downloadWidgetForm.Width - card.Width) / 2;
            int y = _downloadWidgetForm.Top - card.Height - 10;
            location = new Point(
                Math.Clamp(x, workArea.Left + 8, workArea.Right - card.Width - 8),
                Math.Clamp(y, workArea.Top + 8, workArea.Bottom - card.Height - 8));
        }
        else
        {
            Rectangle hostBounds;
            if (Visible && WindowState != FormWindowState.Minimized && panelMain.Visible)
            {
                hostBounds = panelMain.RectangleToScreen(panelMain.ClientRectangle);
            }
            else
            {
                hostBounds = workArea;
            }
            int x = hostBounds.Left + (hostBounds.Width - card.Width) / 2;
            int y = hostBounds.Top + (hostBounds.Height - card.Height) / 2;
            location = new Point(
                Math.Clamp(x, workArea.Left + 8, workArea.Right - card.Width - 8),
                Math.Clamp(y, workArea.Top + 8, workArea.Bottom - card.Height - 8));
        }

        card.Location = location;
        card.FormClosed += (s, e) =>
        {
            if (ReferenceEquals(_completedFileCard, card)) _completedFileCard = null;
        };
        _completedFileCard = card;
        card.Show();
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
                UserErrorFormatter.Format("버전 보관함을 열지 못했습니다.", ex),
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
        return new object[] { "YouTube", "SOOP", "Chzzk", "Instagram", "Threads", "TikTok", "Kuaishou", "X", "Anilife", "Linkkf", "WebSite", "Audio" };
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
        SelectComboText(cmbConcurrentDownloads!, GetConcurrentDownloadsDisplay(SettingsManager.Settings.ConcurrentDownloads));
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

        LayoutDownloadOptionControls();
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

    private static int GetConcurrentDownloadsValue(string? display)
    {
        if (display?.Contains("5개", StringComparison.Ordinal) == true) return 5;
        if (display?.Contains("1개", StringComparison.Ordinal) == true) return 1;
        return 3;
    }

    private static string GetConcurrentDownloadsDisplay(int value) => value switch
    {
        <= 1 => "안정적 (1개)",
        >= 5 => "빠르게 (5개)",
        _ => "균형 (3개)"
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
        if (IsThreadsPlatformUrl(url)) return "Threads";
        if (lower.Contains("tiktok.com")) return "TikTok";
        if (lower.Contains("snapchat.com")) return "Snapchat";
        if (IsKuaishouPlatformUrl(url)) return "Kuaishou";
        if (IsXPlatformUrl(url)) return "X";
        if (lower.Contains("anilife") || lower.Contains("gcdn.app")) return "Anilife";
        if (IsLinkkfPlatformUrl(url)) return "Linkkf";
        return "WebSite";
    }

    private static bool IsXPlatformUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return false;
        string host = uri.Host;
        return host.Equals("x.com", StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith(".x.com", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("twitter.com", StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith(".twitter.com", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("video.twimg.com", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsThreadsPlatformUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return false;
        return HostMatchesAnyDomain(uri.Host, "threads.com", "threads.net");
    }

    private static bool IsThreadsPostUrl(string url)
    {
        if (!IsThreadsPlatformUrl(url) || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(
            uri.AbsolutePath,
            @"/@[^/]+/post/[^/?#]+",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static bool IsKuaishouPlatformUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
               HostMatchesAnyDomain(uri.Host, "kuaishou.com");
    }

    private static bool IsKuaishouShortVideoUrl(string url)
    {
        return IsKuaishouPlatformUrl(url) &&
               Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
               System.Text.RegularExpressions.Regex.IsMatch(
                   uri.AbsolutePath,
                   @"^/short-video/[^/?#]+",
                   System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static bool IsLinkkfPlatformUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return false;
        string host = uri.Host;
        return host.Equals("linkkf.drewpx.xyz", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("linkkf.tckopke.com", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("kf.carsstore365.com", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("play.sub2.top", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("play.sub3.top", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("playv2.sub3.top", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("hlz3.top", StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith(".hlz3.top", StringComparison.OrdinalIgnoreCase);
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
            return EnsureWritableDownloadDirectory(overridePath);
        }

        if (SettingsManager.Settings.UseSiteFolderRules)
        {
            string sitePath = Path.Combine(basePath, site);
            return EnsureWritableDownloadDirectory(sitePath);
        }

        return EnsureWritableDownloadDirectory(basePath);
    }

    private static string EnsureWritableDownloadDirectory(string path)
    {
        if (TryPrepareWritableDirectory(path, out string preparedPath, out string errorMessage))
            return preparedPath;

        throw new InvalidOperationException(errorMessage);
    }

    private static bool TryPrepareWritableDirectory(string path, out string preparedPath, out string errorMessage)
    {
        preparedPath = "";
        errorMessage = "";

        if (string.IsNullOrWhiteSpace(path))
        {
            errorMessage = "저장 폴더가 지정되지 않았습니다. 다른 저장 위치를 선택해 주세요.";
            return false;
        }

        try
        {
            preparedPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()));
            Directory.CreateDirectory(preparedPath);

            string probePath = Path.Combine(preparedPath, $".mmt_write_test_{Guid.NewGuid():N}.tmp");
            using (var probe = new FileStream(
                       probePath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       1,
                       FileOptions.DeleteOnClose))
            {
                probe.WriteByte(0);
            }

            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or IOException or UnauthorizedAccessException)
        {
            preparedPath = "";
            string reason = ex is UnauthorizedAccessException
                ? "이 폴더에 파일을 저장할 권한이 없습니다."
                : ex is DriveNotFoundException or DirectoryNotFoundException
                    ? "지정한 드라이브나 폴더를 찾을 수 없습니다."
                    : "이 폴더를 저장 위치로 사용할 수 없습니다.";
            errorMessage = $"{reason}\n다른 저장 위치를 선택해 주세요.\n\n경로: {path}";
            return false;
        }
    }

    private static string GetUniqueOutputPath(string desiredPath, bool directory = false)
    {
        string? parent = Path.GetDirectoryName(desiredPath);
        string fileName = Path.GetFileName(desiredPath);
        if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(fileName)) return desiredPath;

        string extension = directory ? "" : Path.GetExtension(fileName);
        string baseName = directory ? fileName : Path.GetFileNameWithoutExtension(fileName);
        string candidate = desiredPath;
        int counter = 2;

        while (File.Exists(candidate) || Directory.Exists(candidate))
        {
            candidate = Path.Combine(parent, $"{baseName}_{counter++}{extension}");
        }

        return candidate;
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
                    lblYtDlpStatus.Text = "FFmpeg 다운로드 중... (창을 닫지 마세요)";
                    await EnsureFFmpegAsync();

                    lblYtDlpStatus.Text = "yt-dlp 다운로드 중...";
                    await EnsureYtDlpAsync();

                    lblYtDlpStatus.Text = "모든 도구 준비 완료!";
                    ShowCenteredMessage("모든 필수 도구가 성공적으로 설치되었습니다.\n이제 정상적으로 사용할 수 있습니다.", "설치 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    ShowCenteredMessage(
                        UserErrorFormatter.Format("필수 도구를 설치하지 못했습니다.", ex),
                        "필수 도구 설치 오류",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
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

            if (forceUpdate) SetYtDlpToolStatus("yt-dlp 업데이트 확인 중...");

            Directory.CreateDirectory(Path.GetDirectoryName(ytdlpPath) ?? AppDomain.CurrentDomain.BaseDirectory);
            string tempPath = ytdlpPath + ".download";
            string backupPath = ytdlpPath + ".backup";

            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }

            using var client = new HttpClient();
            if (forceUpdate) SetYtDlpToolStatus("최신 yt-dlp 다운로드 중...");
            var response = await client.GetAsync("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe", ct);
            response.EnsureSuccessStatusCode();

            await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs, ct);
            }

            if (forceUpdate) SetYtDlpToolStatus("새 yt-dlp 파일 확인 중...");
            await ValidateYtDlpExecutableAsync(tempPath, ct);

            if (forceUpdate) SetYtDlpToolStatus("최신 yt-dlp 적용 중...");
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
                SetYtDlpToolStatus("yt-dlp 업데이트 완료. 다운로드를 시작합니다...");
            }
        }
        catch (Exception ex)
        {
            try { File.Delete(ytdlpPath + ".download"); } catch { }

            if (forceUpdate && hadExistingYtDlp && File.Exists(ytdlpPath))
            {
                _ytDlpForceUpdateChecked = true;
                SetYtDlpToolStatus("yt-dlp 업데이트 실패. 기존 파일로 계속 진행합니다...");
                ReportError("다운로드 엔진", "yt-dlp 자동 업데이트", $"yt-dlp update failed, existing file kept | URL: {txtYtDlpUrl.Text}", ex);
                return;
            }

            SetYtDlpToolStatus($"yt-dlp 준비 실패: {UserErrorFormatter.GetCause(FlattenExceptionMessage(ex))}");
            ReportError("다운로드 엔진", "yt-dlp 설치", $"tool download failed | URL: {txtYtDlpUrl.Text}", ex);
            throw;
        }
        finally
        {
            _ytDlpInstallSemaphore.Release();
        }
    }

    private async Task EnsureYouTubeExtractionToolsAsync(CancellationToken ct = default)
    {
        await EnsureYtDlpAsync(ct, forceUpdate: true);

        try
        {
            await EnsureDenoAsync(ct);
        }
        catch (Exception ex)
        {
            // Some YouTube formats still work without Deno, so keep the existing fallback available.
            ReportError("유튜브 다운로더", "Deno 보안 엔진 준비", "YouTube JavaScript runtime setup failed", ex);
        }
    }

    private async Task EnsureDenoAsync(CancellationToken ct = default)
    {
        string denoPath = SettingsManager.GetDenoPath();
        if (_denoReadyChecked && File.Exists(denoPath) && HasMzHeader(denoPath)) return;

        await _denoInstallSemaphore.WaitAsync(ct);
        string tempSuffix = "." + Guid.NewGuid().ToString("N") + ".download";
        string zipPath = denoPath + tempSuffix + ".zip";
        string tempExePath = denoPath + tempSuffix;
        try
        {
            if (File.Exists(denoPath) && HasMzHeader(denoPath))
            {
                _denoReadyChecked = true;
                return;
            }
            else if (File.Exists(denoPath))
            {
                try { File.Delete(denoPath); } catch { }
            }

            string toolsDir = Path.GetDirectoryName(denoPath) ?? SettingsManager.UserDataFolder;
            Directory.CreateDirectory(toolsDir);

            SetYtDlpToolStatus("YouTube 보안 엔진 준비 중...");
            using (var client = new HttpClient())
            using (var response = await client.GetAsync(
                "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip",
                HttpCompletionOption.ResponseHeadersRead,
                ct))
            {
                response.EnsureSuccessStatusCode();
                await using var output = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(output, ct);
            }

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                ZipArchiveEntry? denoEntry = archive.Entries.FirstOrDefault(entry =>
                    entry.Name.Equals("deno.exe", StringComparison.OrdinalIgnoreCase));
                if (denoEntry == null)
                    throw new InvalidDataException("Deno archive does not contain deno.exe.");

                denoEntry.ExtractToFile(tempExePath, overwrite: true);
            }

            try
            {
                await ValidateDenoExecutableAsync(tempExePath, ct);
            }
            catch (Exception ex) when (IsTemporaryFileLock(ex))
            {
                await Task.Delay(1000, ct);
                await ValidateDenoExecutableAsync(tempExePath, ct);
            }

            if (File.Exists(denoPath))
                File.Replace(tempExePath, denoPath, null, ignoreMetadataErrors: true);
            else
                File.Move(tempExePath, denoPath);

            _denoReadyChecked = true;
            SetYtDlpToolStatus("YouTube 보안 엔진 준비 완료");

            try { File.Delete(zipPath); } catch { }
        }
        finally
        {
            try { File.Delete(zipPath); } catch { }
            try { File.Delete(tempExePath); } catch { }
            _denoInstallSemaphore.Release();
        }
    }

    private static async Task ValidateDenoExecutableAsync(string denoPath, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = denoPath,
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

        if (process.ExitCode != 0 || !output.Contains("deno", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Downloaded deno.exe validation failed: " + error.Trim());
    }

    private static bool IsTemporaryFileLock(Exception ex)
    {
        return ex is IOException ||
               ex is Win32Exception win32 && (win32.NativeErrorCode == 32 || win32.NativeErrorCode == 33);
    }

    private static string GetYtDlpJavaScriptRuntimeArguments()
    {
        string denoPath = SettingsManager.GetDenoPath();
        return File.Exists(denoPath)
            ? $"--js-runtimes \"deno:{denoPath}\" "
            : "";
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
            HideExperimentalFeaturesMenu();
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

    private static bool IsYouTubeSingleVideoInput(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        string input = value.Trim();
        if (IsValidYouTubeVideoId(input)) return true;
        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri)) return false;

        string host = uri.Host.ToLowerInvariant();
        if (host == "youtu.be")
            return IsValidYouTubeVideoId(uri.AbsolutePath.Trim('/').Split('/')[0]);

        bool isYouTubeHost = host == "youtube.com"
            || host.EndsWith(".youtube.com")
            || host == "youtube-nocookie.com"
            || host.EndsWith(".youtube-nocookie.com");
        if (!isYouTubeHost) return false;

        string path = uri.AbsolutePath.Trim('/');
        if (path.Equals("watch", StringComparison.OrdinalIgnoreCase))
            return IsValidYouTubeVideoId(GetQueryParameterValue(uri.Query, "v"));

        string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2
            && (segments[0].Equals("shorts", StringComparison.OrdinalIgnoreCase)
                || segments[0].Equals("live", StringComparison.OrdinalIgnoreCase)
                || segments[0].Equals("embed", StringComparison.OrdinalIgnoreCase)
                || segments[0].Equals("v", StringComparison.OrdinalIgnoreCase))
            && IsValidYouTubeVideoId(segments[1]);
    }

    private static bool IsValidYouTubeVideoId(string value)
    {
        return value.Length == 11
            && value.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
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
               lower.Contains("object reference not set") ||
               lower.Contains("video is unavailable") ||
               lower.Contains(" is not available") ||
               lower.Contains("sign in to confirm your age") ||
               lower.Contains("requested format is not available") ||
               lower.Contains("no video formats");
    }

    private static bool IsExplicitYouTubeLoginRequiredError(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;

        string lower = message.ToLowerInvariant();
        return lower.Contains("private video")
            || lower.Contains("members-only")
            || lower.Contains("members only")
            || lower.Contains("join this channel")
            || lower.Contains("sign in to confirm your age")
            || lower.Contains("age-restricted")
            || lower.Contains("login required")
            || lower.Contains("cookies")
            || lower.Contains("--cookies")
            || lower.Contains("not a bot")
            || lower.Contains("confirm you're not a bot");
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

    private async Task RouteToYoutubeDownloadAsync(
        string url,
        string sourceFeature = "웹사이트 영상 다운 → 유튜브 다운로더")
    {
        _pendingYoutubeSourceFeature = sourceFeature;
        try
        {
            if (!IsYouTubeSingleVideoInput(url))
            {
                const string message = "YouTube 영상 페이지를 연 뒤 다시 시도해 주세요. YouTube 홈, 채널, 추천 화면은 다운로드하지 않습니다.";
                lblYtDlpStatus.Text = message;
                lblStatus.Text = message;
                _downloadWidgetForm?.SetProgress(null);
                _downloadWidgetForm?.SetStatus(message);
                _downloadWidgetForm?.ShowToast("YouTube 영상 페이지를 찾지 못했습니다.");
                return;
            }

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
                if (!string.IsNullOrWhiteSpace(_customTitle) && cmbQuality.SelectedItem != null)
                {
                    _pendingYoutubeSourceFeature = sourceFeature;
                    BtnAddQueue_Click(this, EventArgs.Empty);
                    return;
                }

                if (btnLoad.Enabled && (_currentVideo == null || _streamManifest == null))
                {
                    return;
                }
            }
        }
        finally
        {
            _pendingYoutubeSourceFeature = "유튜브 다운로더";
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

    public void ActivateFromSecondLaunch()
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(new Action(ActivateFromSecondLaunch));
            return;
        }

        ShowMainFromWidget();
        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;

        bool restoreTopMost = TopMost;
        TopMost = true;
        BringToFront();
        Activate();
        SetForegroundWindow(Handle);

        var releaseTopMostTimer = new System.Windows.Forms.Timer { Interval = 500 };
        releaseTopMostTimer.Tick += (_, _) =>
        {
            releaseTopMostTimer.Stop();
            releaseTopMostTimer.Dispose();
            if (!IsDisposed) TopMost = restoreTopMost;
        };
        releaseTopMostTimer.Start();
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
        string sourceFeature = _returnToWidgetAfterVideoPick
            ? "위젯 모드 / 영상 선택 다운로드"
            : "웹사이트 영상 다운 / 영상 선택";
        foreach (var item in list)
        {
            string queueUrl = item.PlaylistIndex > 0 && IsHttpUrl(item.SourcePageUrl) ? item.SourcePageUrl : item.Url;
            if (EnqueueYtDlpDownload(
                    queueUrl,
                    allowFolderPrompt: false,
                    out string rejectReason,
                    item.PlaylistIndex,
                    item.Title,
                    sourceFeature))
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
                await RouteToYoutubeDownloadAsync(text, "위젯 모드 / YouTube 다운로드");
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
            if (!EnqueueYtDlpDownload(
                    text,
                    allowFolderPrompt: false,
                    out string rejectReason,
                    sourceFeature: "위젯 모드 / 웹사이트 다운로드"))
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
            ReportError("위젯 모드", "URL 감지 및 대기열 추가", $"위젯 다운로드 추가 실패 | URL: {text}", ex);
            _downloadWidgetForm?.SetProgress(null);
            _downloadWidgetForm?.SetStatus("대기열 추가 실패. 앱을 열어 오류 내용을 확인해 주세요.");
        }
    }

    private static bool IsHttpUrl(string value)
    {
        return TryNormalizeHttpUrl(value, out _);
    }

    private static bool TryNormalizeHttpUrl(string value, out string normalizedUrl)
    {
        normalizedUrl = "";
        if (string.IsNullOrWhiteSpace(value)) return false;

        string candidate = value.Trim().Trim('"', '\'', ' ');
        if (candidate.Any(char.IsControl)) return false;
        if (IsValidYouTubeVideoId(candidate))
        {
            candidate = $"https://www.youtube.com/watch?v={candidate}";
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        normalizedUrl = uri.AbsoluteUri;
        return true;
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

        if (!SettingsManager.Settings.ShowNotifications) return;

        bool widgetModeActive = _downloadWidgetForm != null &&
                                !_downloadWidgetForm.IsDisposed &&
                                _downloadWidgetForm.Visible &&
                                !Visible;
        if (widgetModeActive) return;

        notifyIconApp.ShowBalloonTip(3000, title, text, ToolTipIcon.None);
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

    private (DialogResult Result, bool SkipThisVersion) ShowUpdateAvailableDialog(string latestVersion)
    {
        if (InvokeRequired)
        {
            return (ValueTuple<DialogResult, bool>)Invoke(
                new Func<(DialogResult, bool)>(() => ShowUpdateAvailableDialog(latestVersion)));
        }

        bool useWidgetOwner = _downloadWidgetForm != null &&
                              !_downloadWidgetForm.IsDisposed &&
                              _downloadWidgetForm.Visible &&
                              !Visible;
        IWin32Window owner = useWidgetOwner ? _downloadWidgetForm! : this;
        Rectangle ownerBounds = useWidgetOwner
            ? Screen.FromControl(_downloadWidgetForm!).WorkingArea
            : WindowState == FormWindowState.Maximized
                ? Screen.FromControl(this).WorkingArea
                : WindowState == FormWindowState.Minimized ? RestoreBounds : Bounds;

        if (ownerBounds.Width <= 0 || ownerBounds.Height <= 0)
            ownerBounds = Screen.FromControl(useWidgetOwner ? _downloadWidgetForm! : this).WorkingArea;

        using var dialog = new Form
        {
            Text = "업데이트 알림",
            ClientSize = new Size(500, 225),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            BackColor = Color.White,
            Font = Font,
            TopMost = useWidgetOwner
        };

        var title = new Label
        {
            Text = $"새 버전 v{latestVersion}가 있습니다.",
            Location = new Point(28, 24),
            Size = new Size(444, 30),
            Font = new Font(Font, FontStyle.Bold)
        };
        var description = new Label
        {
            Text = "지금 업데이트 파일을 다운로드하시겠습니까?\n다운로드가 끝난 뒤 설치 시점을 선택할 수 있습니다.",
            Location = new Point(28, 62),
            Size = new Size(444, 52)
        };
        var skipCheckBox = new CheckBox
        {
            Text = "이 버전 알림 다시 보지 않기",
            Location = new Point(28, 128),
            AutoSize = true
        };
        var updateButton = new Button
        {
            Text = "다운로드",
            DialogResult = DialogResult.Yes,
            Location = new Point(252, 172),
            Size = new Size(105, 34),
            BackColor = Color.FromArgb(14, 149, 220),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        updateButton.FlatAppearance.BorderSize = 0;

        var laterButton = new Button
        {
            Text = "나중에",
            DialogResult = DialogResult.No,
            Location = new Point(367, 172),
            Size = new Size(105, 34),
            BackColor = Color.FromArgb(230, 235, 241),
            ForeColor = Color.FromArgb(35, 48, 65),
            FlatStyle = FlatStyle.Flat
        };
        laterButton.FlatAppearance.BorderSize = 0;

        dialog.Controls.AddRange(new Control[] { title, description, skipCheckBox, updateButton, laterButton });
        dialog.AcceptButton = updateButton;
        dialog.CancelButton = laterButton;
        dialog.Shown += (_, _) =>
        {
            int x = ownerBounds.Left + (ownerBounds.Width - dialog.Width) / 2;
            int y = ownerBounds.Top + (ownerBounds.Height - dialog.Height) / 2;
            Rectangle workingArea = Screen.FromRectangle(ownerBounds).WorkingArea;
            dialog.Location = new Point(
                Math.Max(workingArea.Left, Math.Min(x, workingArea.Right - dialog.Width)),
                Math.Max(workingArea.Top, Math.Min(y, workingArea.Bottom - dialog.Height)));
            dialog.Activate();
        };

        DialogResult result = dialog.ShowDialog(owner);
        return (result, skipCheckBox.Checked);
    }

    private bool ShowUpdateReadyDialog(string latestVersion)
    {
        if (InvokeRequired)
        {
            return (bool)Invoke(new Func<bool>(() => ShowUpdateReadyDialog(latestVersion)));
        }

        bool useWidgetOwner = _downloadWidgetForm != null &&
                              !_downloadWidgetForm.IsDisposed &&
                              _downloadWidgetForm.Visible &&
                              !Visible;
        IWin32Window owner = useWidgetOwner ? _downloadWidgetForm! : this;
        Rectangle ownerBounds = useWidgetOwner
            ? Screen.FromControl(_downloadWidgetForm!).WorkingArea
            : WindowState == FormWindowState.Maximized
                ? Screen.FromControl(this).WorkingArea
                : WindowState == FormWindowState.Minimized ? RestoreBounds : Bounds;

        if (ownerBounds.Width <= 0 || ownerBounds.Height <= 0)
            ownerBounds = Screen.FromControl(useWidgetOwner ? _downloadWidgetForm! : this).WorkingArea;

        using var dialog = new Form
        {
            Text = "업데이트 다운로드 완료",
            ClientSize = new Size(520, 205),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            BackColor = Color.White,
            Font = Font,
            TopMost = useWidgetOwner
        };

        var title = new Label
        {
            Text = $"v{latestVersion} 설치 파일이 준비되었습니다.",
            Location = new Point(28, 26),
            Size = new Size(464, 30),
            Font = new Font(Font, FontStyle.Bold)
        };
        var description = new Label
        {
            Text = "설치하고 다시 시작을 누르면 프로그램이 종료된 후 업데이트됩니다.\n진행 중인 다운로드가 있다면 먼저 완료해 주세요.",
            Location = new Point(28, 66),
            Size = new Size(464, 52)
        };
        var installButton = new Button
        {
            Text = "설치하고 다시 시작",
            DialogResult = DialogResult.Yes,
            Location = new Point(235, 150),
            Size = new Size(155, 36),
            BackColor = Color.FromArgb(14, 149, 220),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        installButton.FlatAppearance.BorderSize = 0;

        var laterButton = new Button
        {
            Text = "나중에",
            DialogResult = DialogResult.No,
            Location = new Point(400, 150),
            Size = new Size(92, 36),
            BackColor = Color.FromArgb(230, 235, 241),
            ForeColor = Color.FromArgb(35, 48, 65),
            FlatStyle = FlatStyle.Flat
        };
        laterButton.FlatAppearance.BorderSize = 0;

        dialog.Controls.AddRange(new Control[] { title, description, installButton, laterButton });
        dialog.AcceptButton = installButton;
        dialog.CancelButton = laterButton;
        dialog.Shown += (_, _) =>
        {
            int x = ownerBounds.Left + (ownerBounds.Width - dialog.Width) / 2;
            int y = ownerBounds.Top + (ownerBounds.Height - dialog.Height) / 2;
            Rectangle workingArea = Screen.FromRectangle(ownerBounds).WorkingArea;
            dialog.Location = new Point(
                Math.Max(workingArea.Left, Math.Min(x, workingArea.Right - dialog.Width)),
                Math.Max(workingArea.Top, Math.Min(y, workingArea.Bottom - dialog.Height)));
            dialog.Activate();
        };

        return dialog.ShowDialog(owner) == DialogResult.Yes;
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
        return UserErrorFormatter.GetCause(FlattenExceptionMessage(ex));
    }

    private static bool IsLoginRequiredDownloadError(string url, Exception ex)
    {
        string lower = FlattenExceptionMessage(ex).ToLowerInvariant();
        bool xVideoMayRequireLogin = IsXPlatformUrl(url) &&
            lower.Contains("no video could be found in this tweet");
        bool kuaishouVerificationRequired = IsKuaishouPlatformUrl(url) &&
            (lower.Contains("kuaishou") || lower.Contains("콰이쇼우") || lower.Contains("보안 확인"));

        return xVideoMayRequireLogin ||
               kuaishouVerificationRequired ||
               lower.Contains("instagram sent an empty media response") ||
               lower.Contains("use --cookies-from-browser") ||
               lower.Contains("use --cookies for the authentication") ||
               lower.Contains("login required") ||
               lower.Contains("sign in") ||
               lower.Contains("members-only") ||
               lower.Contains("members only") ||
               lower.Contains("private video") ||
               lower.Contains("age-restricted") ||
               lower.Contains("confirm you're not a bot");
    }

    private static string GetLoginSiteNameForUrl(string url)
    {
        string lower = url.ToLowerInvariant();
        if (lower.Contains("instagram.com")) return "Instagram";
        if (IsThreadsPlatformUrl(url)) return "Threads";
        if (lower.Contains("tiktok.com")) return "TikTok";
        if (IsKuaishouPlatformUrl(url)) return "콰이쇼우";
        if (lower.Contains("youtube.com") || lower.Contains("youtu.be")) return "YouTube";
        if (lower.Contains("chzzk.naver.com")) return "치지직";
        if (lower.Contains("sooplive") || lower.Contains("afreecatv.com")) return "SOOP";
        if (IsXPlatformUrl(url)) return "X";
        return "웹 브라우저";
    }

    private static bool IsInstagramPlatformUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
               HostMatchesAnyDomain(uri.Host, "instagram.com", "cdninstagram.com", "fbcdn.net");
    }

    private async Task<bool> HandleLoginRequiredDownloadErrorAsync(string url, Exception ex, bool alreadyInLoginBrowser)
    {
        if (!IsLoginRequiredDownloadError(url, ex)) return false;

        string siteName = GetLoginSiteNameForUrl(url);
        if (alreadyInLoginBrowser)
        {
            if (siteName == "콰이쇼우")
            {
                ShowCenteredMessage(
                    "콰이쇼우 보안 확인을 마친 뒤 좌측 상단 [즉시 다운로드]를 다시 눌러 주세요.\n대부분 영상이 표시되지만, 간혹 영상이 보이지 않아도 다운로드는 가능할 수 있습니다.\n\n계속 실패하면 [설정 > 문의하기]에 영상 URL과 오류 내용을 남겨 주세요. 더 빠르게 확인할 수 있으며, 수정 가능하면 빠르면 다음 날 업데이트에 반영할 수 있습니다.",
                    "콰이쇼우 다운로드 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return true;
            }

            if (siteName == "Instagram")
            {
                DialogResult retryLogin = ShowCenteredMessage(
                    "프로그램 내부 Instagram 로그인 쿠키를 확인하지 못했습니다.\n\nChrome이나 Edge의 로그인 정보는 이 프로그램과 별개입니다. [예]를 누르면 프로그램 내부 Instagram 로그인 화면을 다시 엽니다.",
                    "Instagram 로그인 다시 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                if (retryLogin == DialogResult.Yes)
                {
                    await RestartInstagramLoginAsync(url);
                }
                return true;
            }

            string message = siteName == "X"
                ? "X에 로그인된 상태에서도 게시물의 영상을 찾지 못했습니다.\n\n브라우저에서 해당 영상이 실제로 재생되는지 확인해 주세요. 삭제됐거나 영상이 없는 게시물은 다운로드할 수 없습니다."
                : $"{siteName} 로그인 또는 시청 권한을 확인해 주세요.\n\n브라우저에서 해당 영상이 정상 재생되는지 확인한 뒤 다시 다운로드해 주세요.";
            ShowCenteredMessage(
                message,
                "로그인 확인 필요",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return true;
        }

        if (siteName == "콰이쇼우")
        {
            await ShowKuaishouSecurityVerificationAsync(url);
            return true;
        }

        string prompt = siteName switch
        {
            "Instagram" => "Instagram 로그인이 필요한 영상입니다.\n\n로그인하시면 다운로드할 수 있습니다.\n지금 [로그인 후 다운]을 열어 로그인하시겠습니까?",
            "X" => "X에서 로그인해야 확인할 수 있는 영상일 수 있습니다.\n\n영상이 삭제되지 않았고 계정에 시청 권한이 있다면 로그인 후 다운로드할 수 있습니다.\n지금 [로그인 후 다운]에서 X에 로그인하시겠습니까?",
            _ => $"이 영상은 {siteName} 로그인이 필요한 것으로 보입니다.\n\n로그인하시면 다운로드할 수 있습니다.\n지금 [로그인 후 다운]을 열어 로그인하시겠습니까?"
        };

        DialogResult result = ShowCenteredMessage(
            prompt,
            "로그인 필요",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (result == DialogResult.Yes)
        {
            await OpenLoginDownloadForUrlAsync(siteName, url);
        }

        return true;
    }

    private async Task ShowKuaishouSecurityVerificationAsync(string url)
    {
        DialogResult verificationResult = ShowCenteredMessage(
            "콰이쇼우가 자동 접근을 제한하여 보안 확인이 필요합니다.\n\n[확인]을 누르면 프로그램 내부 콰이쇼우 페이지가 열립니다.\n1. 화면에 표시되는 보안 검사를 완료하세요.\n2. 영상 페이지에서 좌측 상단 [즉시 다운로드]를 누르세요.\n\n대부분 영상이 표시되지만, 간혹 영상이 보이지 않아도 다운로드는 가능할 수 있습니다.",
            "콰이쇼우 보안 확인",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Information);
        if (verificationResult == DialogResult.OK)
        {
            await OpenLoginDownloadForUrlAsync("콰이쇼우", url);
        }
    }

    private async Task<string> DownloadWithKuaishouRetryAsync(
        YtDlpDownloader downloader,
        string url,
        string savePath,
        string browser,
        CancellationToken token,
        string cookieFile,
        Dictionary<string, string>? customHeaders,
        Action beforeRetry)
    {
        if (!IsKuaishouShortVideoUrl(url))
        {
            return await downloader.DownloadVideoAsync(url, savePath, browser, token, cookieFile, customHeaders);
        }

        try
        {
            return await downloader.DownloadVideoAsync(url, savePath, browser, token, cookieFile, customHeaders);
        }
        catch (Exception) when (!token.IsCancellationRequested)
        {
            beforeRetry();
            await Task.Delay(350, token);
        }

        try
        {
            return await downloader.DownloadVideoAsync(url, savePath, browser, token, cookieFile, customHeaders);
        }
        catch (OperationCanceledException ex) when (!token.IsCancellationRequested)
        {
            throw new Exception(
                "콰이쇼우 보안 확인이 필요합니다. 프로그램 내부 브라우저에서 보안 검사를 완료한 뒤 다시 시도해 주세요.",
                ex);
        }
    }

    private async Task OpenLoginDownloadForUrlAsync(string siteName, string url)
    {
        if (!Visible || SettingsManager.Settings.EnableWidgetMode)
        {
            ShowMainFromWidget();
        }

        SelectMainTab(btnTabYtDlp, tabYtDlp);
        txtYtDlpUrl.Text = url;

        if (siteName == "Instagram")
        {
            _isLoginBrowserMode = false;
            if (tglXPrivateMode.Checked) tglXPrivateMode.Checked = false;
            if (!tglInstaPrivateMode.Checked) tglInstaPrivateMode.Checked = true;
            return;
        }

        _isLoginBrowserMode = true;
        _lastWidth = Math.Max(Width, 800);
        _lastHeight = Math.Max(Height, 600);

        await ShowLoginBrowserAsync(siteName, url);
        if (lblXGuide != null)
            lblXGuide.Text = siteName == "콰이쇼우"
                ? "보안 검사를 완료한 뒤 좌측 상단 즉시 다운로드를 눌러주세요. 간혹 영상이 보이지 않아도 다운로드는 가능할 수 있습니다."
                : $"{siteName}에 로그인한 뒤 영상이 재생되면 좌측 상단 즉시 다운로드를 눌러주세요.";
        if (lblXStatus != null)
            lblXStatus.Text = siteName == "콰이쇼우"
                ? "보안 확인 후 즉시 다운로드를 눌러주세요."
                : "로그인 후 영상을 확인해 주세요.";
    }

    private async Task RestartInstagramLoginAsync(string url)
    {
        txtYtDlpUrl.Text = url;
        _isInstaLoggedIn = false;
        if (tglInstaPrivateMode.Checked)
        {
            tglInstaPrivateMode.Checked = false;
            await ClearInstagramCookiesAsync();
            await Task.Delay(100);
        }

        tglInstaPrivateMode.Checked = true;
    }

    private static string GetDownloadFailureHint(string cause, string? relatedFilePath = null)
    {
        return UserErrorFormatter.GetHint(cause, relatedFilePath);
    }

    private static string BuildDownloadFailureMessage(string url, Exception ex, string? relatedFilePath = null)
    {
        string cause = GetDownloadFailureCause(ex);
        string detail = CleanErrorDetailForDisplay(FlattenExceptionMessage(ex).Trim());
        if (detail.Length > 1200)
        {
            detail = detail.Substring(0, 1200) + "...";
        }

        string inquiryGuidance = UserErrorFormatter.GetInquiryGuidance(cause);
        string inquirySection = string.IsNullOrWhiteSpace(inquiryGuidance)
            ? string.Empty
            : $"\n\n문의 안내:\n{inquiryGuidance}";
        return $"다운로드 실패 원인: {cause}\n\n{GetDownloadFailureHint(cause, relatedFilePath)}{inquirySection}\n\nURL: {url}\n\n상세 오류:\n{detail}";
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
        List<string> urls = ExtractHttpUrls(txtUrl.Text);
        if (urls.Count > 1)
        {
            await ConfirmAndQueueMultipleUrlsAsync(urls, fromYoutubeTab: true);
            return;
        }

        string url = NormalizeYouTubeSingleVideoUrl(txtUrl.Text.Trim());
        if (string.IsNullOrEmpty(url)) return;
        _currentUrl = url;
        _currentVideoDurationSeconds = null;

        if (!LooksLikeYouTubeInput(url))
        {
            RouteToWebsiteDownload(url, startDownload: true);
            return;
        }

        if (!IsYouTubeSingleVideoInput(url))
        {
            ShowCenteredMessage("YouTube 영상 주소를 입력해 주세요. 홈, 채널, 추천 화면 주소는 다운로드할 수 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            lblVideoTitle.Text = "YouTube 영상 페이지 주소를 확인해 주세요.";
            return;
        }

        try
        {
            btnLoad.Enabled = false;
            lblVideoTitle.Text = "영상 정보를 불러오는 중...";
            
            _currentVideo = await _youtube.Videos.GetAsync(url);
            _currentVideoDurationSeconds = _currentVideo.Duration?.TotalSeconds;
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
            var preferredAudio = GetPreferredYoutubeAudioStream(_streamManifest);

            foreach (var stream in videoStreams)
            {
                long estimatedBytes = stream.Size.Bytes + (preferredAudio?.Size.Bytes ?? 0);
                cmbQuality.Items.Add(new QualityOption($"{stream.VideoQuality.Label} (MP4)", stream.VideoQuality.Label, true, estimatedBytes));
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
            ShowCenteredMessage(
                UserErrorFormatter.Format("유튜브 영상 정보를 불러오지 못했습니다.", ex),
                "영상 정보 오류",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            lblVideoTitle.Text = "오류가 발생했습니다.";
            ReportError(_pendingYoutubeSourceFeature, "영상 정보 조회", $"유튜브 정보 로드 실패 | URL: {url}", ex);
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

        string downloadUrl = NormalizeYouTubeSingleVideoUrl(
            !string.IsNullOrWhiteSpace(txtUrl.Text) ? txtUrl.Text.Trim() : _currentUrl);
        if (!IsYouTubeSingleVideoInput(downloadUrl))
        {
            ShowCenteredMessage("다운로드할 YouTube 영상 주소를 다시 확인해 주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedOption = (QualityOption)cmbQuality.SelectedItem;
        double sectionStartSeconds = 0;
        double sectionEndSeconds = 0;
        bool useSection = chkYoutubeDownloadSection?.Checked == true &&
            !_pendingYoutubeSourceFeature.StartsWith("위젯 모드", StringComparison.OrdinalIgnoreCase);
        if (useSection && !TryPromptDownloadSection(
                out sectionStartSeconds,
                out sectionEndSeconds,
                _currentVideoDurationSeconds)) return;
        
        string outputPath = "";

        if (IsYoutubeUrlInFlight(downloadUrl))
        {
            ShowCenteredMessage("이미 다운로드 중인 영상입니다. 완료된 뒤 다시 누르면 새 파일로 받을 수 있습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Check if Default Folder is set
        if (!string.IsNullOrWhiteSpace(SettingsManager.Settings.DefaultDownloadFolder) || SettingsManager.Settings.UseSiteFolderRules || SettingsManager.Settings.UseCustomSiteFolders)
        {
            string ext = selectedOption.IsVideo ? "mp4" : selectedOption.Id.Replace("best_", "");
            string saveDirectory;
            try
            {
                saveDirectory = GetDownloadSaveDirectory(downloadUrl, !selectedOption.IsVideo);
            }
            catch (Exception ex)
            {
                ShowCenteredMessage(ex.Message, "저장 위치 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string channel = _currentVideo?.Author.ChannelTitle ?? "";
            string baseFileName = BuildFileNameFromSettings("YouTube", channel, _customTitle, selectedOption.Title);
            outputPath = Path.Combine(saveDirectory, $"{baseFileName}.{ext}");
            
            // To avoid overwrite, add numbers to filename if exists
            int count = 2;
            while(File.Exists(outputPath)) {
                outputPath = Path.Combine(saveDirectory, $"{baseFileName}_{count}.{ext}");
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

        long estimatedBytes = selectedOption.EstimatedBytes;
        if (estimatedBytes > 0 && useSection && _currentVideo?.Duration is TimeSpan duration && duration.TotalSeconds > 0)
        {
            double ratio = Math.Clamp((sectionEndSeconds - sectionStartSeconds) / duration.TotalSeconds, 0.01, 1);
            estimatedBytes = (long)(estimatedBytes * ratio);
        }
        if (!ConfirmDownloadSpace(outputPath, estimatedBytes)) return;

        var item = new ListViewItem(_customTitle);
        item.SubItems.Add(selectedOption.Title);
        item.SubItems.Add("대기 중");
        lvQueue.Items.Add(item);

        var job = new DownloadJob
        {
            Id = Guid.NewGuid().ToString(),
            Url = downloadUrl,
            Video = _currentVideo,
            Manifest = _streamManifest,
            Option = selectedOption,
            OutputPath = outputPath,
            ListViewItem = item,
            JobCts = new CancellationTokenSource(),
            CustomFileName = _customTitle,
            DownloadSubtitles = chkYoutubeDownloadSubtitles.Checked,
            SubtitleLanguagePreset = GetSelectedSubtitlePreset(cmbYoutubeSubtitleLanguage),
            EmbedMetadata = chkYoutubeEmbedMetadata?.Checked == true,
            RemoveSponsorSegments = chkYoutubeSponsorBlock?.Checked == true,
            SectionStartSeconds = sectionStartSeconds,
            SectionEndSeconds = sectionEndSeconds,
            SourceFeature = _pendingYoutubeSourceFeature
        };
        
        item.Tag = job;

        _downloadQueue.Enqueue(job);
        
        txtUrl.Text = "";
        lblVideoTitle.Text = "URL을 입력하고 '영상 확인' 버튼을 눌러주세요.";
        picThumbnail.Image = null;
        cmbQuality.Items.Clear();
        cmbQuality.Enabled = false;
        _currentVideo = null!;
        _currentVideoDurationSeconds = null;
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

    private bool EnqueueYtDlpDownload(
        string url,
        bool allowFolderPrompt,
        out string rejectReason,
        int playlistItemIndex = 0,
        string preferredTitle = "",
        string sourceFeature = "웹사이트 영상 다운",
        bool? downloadSubtitles = null,
        string? subtitlePreset = null,
        bool? embedMetadata = null,
        bool removeSponsorSegments = false,
        double sectionStartSeconds = 0,
        double sectionEndSeconds = 0)
    {
        rejectReason = "";
        if (!TryNormalizeHttpUrl(url, out url))
        {
            rejectReason = "http:// 또는 https://로 시작하는 올바른 영상 주소를 입력해 주세요.";
            lblYtDlpStatus.Text = rejectReason;
            return false;
        }

        url = NormalizeYouTubeSingleVideoUrl(url);
        if (LooksLikeYouTubeInput(url) && !IsYouTubeSingleVideoInput(url))
        {
            rejectReason = "YouTube 영상 페이지를 열어 주세요. 홈, 채널, 추천 화면은 다운로드하지 않습니다.";
            lblYtDlpStatus.Text = rejectReason;
            return false;
        }

        if (IsYtDlpUrlInFlight(url, playlistItemIndex))
        {
            rejectReason = "이미 다운로드 중인 URL입니다. 완료된 뒤 다시 누르면 새 파일로 받을 수 있습니다.";
            lblYtDlpStatus.Text = rejectReason;
            return false;
        }

        string savePath;
        try
        {
            savePath = GetDownloadSaveDirectory(url, SettingsManager.Settings.DefaultVideoQuality == "MP3");
        }
        catch (Exception ex)
        {
            rejectReason = ex.Message;
            lblYtDlpStatus.Text = rejectReason;
            return false;
        }
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
        if (!ConfirmDownloadSpace(savePath))
        {
            rejectReason = "저장 공간 확인이 취소되었습니다.";
            return false;
        }

        bool effectiveDownloadSubtitles = downloadSubtitles ?? chkYtDlpDownloadSubtitles.Checked;
        string effectiveSubtitlePreset = subtitlePreset ?? GetSelectedSubtitlePreset(cmbYtDlpSubtitleLanguage);
        bool effectiveEmbedMetadata = embedMetadata ?? (chkYtDlpEmbedMetadata?.Checked == true);

        var item = new ListViewItem(url);
        item.SubItems.Add(effectiveDownloadSubtitles ? "켜짐" : "꺼짐");
        item.SubItems.Add("대기 중");
        lvYtDlpQueue.Items.Add(item);
        ResizeYtDlpQueueColumns();

        bool usesExplicitLogin = _isLoginBrowserMode || tglXPrivateMode.Checked || tglInstaPrivateMode.Checked;
        string effectiveSourceFeature = sourceFeature;
        if (usesExplicitLogin)
        {
            effectiveSourceFeature = sourceFeature == "웹사이트 영상 다운"
                ? "로그인 후 다운"
                : sourceFeature + " / 로그인 쿠키";
        }

        var job = new YtDlpDownloadJob
        {
            Id = Guid.NewGuid().ToString("N"),
            Url = url,
            SavePath = savePath,
            DownloadSubtitles = effectiveDownloadSubtitles,
            SubtitleLanguagePreset = effectiveSubtitlePreset,
            EmbedMetadata = effectiveEmbedMetadata,
            RemoveSponsorSegments = removeSponsorSegments,
            SectionStartSeconds = sectionStartSeconds,
            SectionEndSeconds = sectionEndSeconds,
            FormatSelector = GetYtDlpFormatForDefaultQuality(),
            OutputNameTemplate = BuildYtDlpOutputNameTemplate(GetSiteNameFromUrl(url)),
            PreferredTitle = string.IsNullOrWhiteSpace(preferredTitle) ? _loginBrowserDownloadTitle : preferredTitle,
            PlaylistItemIndex = playlistItemIndex,
            UseXPrivateMode = tglXPrivateMode.Checked,
            UseInstaPrivateMode = tglInstaPrivateMode.Checked,
            UseLoginBrowserCookies = _isLoginBrowserMode,
            SourceFeature = effectiveSourceFeature,
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
                int workerCount = Math.Clamp(SettingsManager.Settings.ConcurrentDownloads, 1, 5);
                var workers = Enumerable.Range(0, workerCount)
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
            UpdateYtDlpJobStatus(job, "필수 도구 확인 중...");
            lblYtDlpStatus.Text = "필수 도구 확인 중...";
            await EnsureFFmpegAsync(job.JobCts.Token);
            if (LooksLikeYouTubeInput(url))
                await EnsureYouTubeExtractionToolsAsync(job.JobCts.Token);
            else
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
                PlaylistItemIndex = job.PlaylistItemIndex,
                EmbedMetadata = job.EmbedMetadata,
                RemoveSponsorSegments = job.RemoveSponsorSegments,
                SectionStartSeconds = job.SectionStartSeconds,
                SectionEndSeconds = job.SectionEndSeconds
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
                bool genericCaptureEnabled = false;
                try
                {
                    string capturedUrl = "";
                    string anilifeResolveDiagnostic = "";
                    bool anilifePageApiAttempted = false;
                    bool isAnilife = targetUrl.Contains("anilife.app", StringComparison.OrdinalIgnoreCase);
                    bool isXStatusTarget = IsXStatusPageUrl(targetUrl);
                    bool isThreads = IsThreadsPostUrl(targetUrl);
                    bool isKuaishou = IsKuaishouShortVideoUrl(targetUrl);
                    bool needsBrowserCapture = isAnilife ||
                        isThreads ||
                        isKuaishou ||
                        targetUrl.Contains("chzzk.naver.com", StringComparison.OrdinalIgnoreCase) ||
                        targetUrl.Contains("sooplive", StringComparison.OrdinalIgnoreCase) ||
                        IsLinkkfPlatformUrl(targetUrl);
                    genericCaptureEnabled = isKuaishou || !needsBrowserCapture;
                    if (genericCaptureEnabled) Interlocked.Increment(ref _genericMediaCaptureDepth);
                    try
                    {
                        if (webViewX.CoreWebView2 == null) await PreInitializeWebView2Async();
                    }
                    catch { }

                    this.Invoke((MethodInvoker)(() =>
                    {
                        ClearCapturedMediaCandidates();
                        if (webViewX.CoreWebView2 == null) return;
                        webViewX.CoreWebView2.Navigate(targetUrl);
                    }));

                    int maxAttempts = isAnilife ? 90 : needsBrowserCapture ? 60 : 24;
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
                                        await webViewX.CoreWebView2.ExecuteScriptAsync("window.dispatchEvent(new Event('mousemove')); window.dispatchEvent(new Event('scroll')); const video = document.querySelector('video'); if (video) { video.muted = true; video.play().catch(() => {}); }");
                                    }
                                }
                                catch { }
                            }));
                        }

                        if ((!needsBrowserCapture || isKuaishou) && i == 6)
                        {
                            this.Invoke((MethodInvoker)(async () =>
                            {
                                try
                                {
                                    if (webViewX.CoreWebView2 != null)
                                    {
                                        string activationScript = isXStatusTarget
                                            ? @"(() => {
    const videos = Array.from(document.querySelectorAll('video'));
    const visible = videos
        .map(video => ({ video, rect: video.getBoundingClientRect() }))
        .filter(item => item.rect.bottom > 0 && item.rect.right > 0 && item.rect.top < innerHeight && item.rect.left < innerWidth)
        .sort((a, b) => (b.rect.width * b.rect.height) - (a.rect.width * a.rect.height));
    const video = visible[0]?.video;
    if (video) { video.muted = true; video.play().catch(() => {}); }
})()"
                                            : "document.querySelectorAll('video').forEach(v => { v.muted = true; v.play().catch(() => {}); }); window.scrollTo(0, Math.max(document.body.scrollHeight / 3, 1));";
                                        await webViewX.CoreWebView2.ExecuteScriptAsync(activationScript);
                                    }
                                }
                                catch { }
                            }));
                        }

                        string bestCapturedUrl = isThreads || isKuaishou ? "" : GetBestCapturedMediaUrl();
                        if (isThreads && i >= 2)
                        {
                            var threadsMedia = await TryReadThreadsMediaFromWebViewAsync();
                            bestCapturedUrl = threadsMedia.Url;
                            if (!string.IsNullOrWhiteSpace(threadsMedia.Title))
                                downloader.PreferredTitle = threadsMedia.Title;
                            if (!string.IsNullOrWhiteSpace(bestCapturedUrl))
                                RememberCapturedMediaUrl(bestCapturedUrl);
                        }
                        if (isKuaishou && i >= 2)
                        {
                            var kuaishouMedia = await TryReadKuaishouMediaFromWebViewAsync();
                            bestCapturedUrl = kuaishouMedia.Url;
                            if (!string.IsNullOrWhiteSpace(kuaishouMedia.Title))
                                downloader.PreferredTitle = kuaishouMedia.Title;
                            if (string.IsNullOrWhiteSpace(bestCapturedUrl))
                                bestCapturedUrl = GetBestCapturedMediaUrl();
                            if (!string.IsNullOrWhiteSpace(bestCapturedUrl))
                                RememberCapturedMediaUrl(bestCapturedUrl);
                        }
                        if (isAnilife && !IsAnilifeManifestCaptureUrl(bestCapturedUrl)) bestCapturedUrl = "";
                        if (isAnilife && string.IsNullOrWhiteSpace(bestCapturedUrl) && i >= 4 && i % 4 == 0)
                        {
                            bestCapturedUrl = await TryReadAnilifeManifestFromWebViewAsync();
                            if (!string.IsNullOrWhiteSpace(bestCapturedUrl)) RememberCapturedMediaUrl(bestCapturedUrl);
                        }
                        if (isAnilife && string.IsNullOrWhiteSpace(bestCapturedUrl) && !anilifePageApiAttempted && i >= 12)
                        {
                            anilifePageApiAttempted = true;
                            var pageApiResult = await TryResolveAnilifeManifestViaPageApiAsync(targetUrl);
                            bestCapturedUrl = pageApiResult.Url;
                            anilifeResolveDiagnostic = pageApiResult.Diagnostic;
                            if (!string.IsNullOrWhiteSpace(bestCapturedUrl)) RememberCapturedMediaUrl(bestCapturedUrl);
                        }
                        if (!string.IsNullOrEmpty(bestCapturedUrl) && (needsBrowserCapture || i >= 8))
                        {
                            capturedUrl = bestCapturedUrl;
                            break;
                        }
                    }

                    if (isAnilife && !string.IsNullOrWhiteSpace(capturedUrl) && string.IsNullOrWhiteSpace(downloader.PreferredTitle))
                    {
                        string pageTitle = await TryReadAnilifePageTitleFromWebViewAsync();
                        if (!string.IsNullOrWhiteSpace(pageTitle)) downloader.PreferredTitle = pageTitle;
                    }

                    if (isAnilife && string.IsNullOrWhiteSpace(capturedUrl) && !string.IsNullOrWhiteSpace(anilifeResolveDiagnostic))
                    {
                        throw new Exception($"애니라이프 재생 정보를 가져오지 못했습니다.\n{anilifeResolveDiagnostic}");
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
                    if (genericCaptureEnabled) Interlocked.Decrement(ref _genericMediaCaptureDepth);
                    try
                    {
                        this.Invoke((MethodInvoker)(() =>
                        {
                            if (webViewX.CoreWebView2 != null)
                            {
                                webViewX.CoreWebView2.Stop();
                                webViewX.CoreWebView2.Navigate("about:blank");
                            }
                        }));
                    }
                    catch { }
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
                    bool useXCookieProfile = job.UseXPrivateMode;
                    await EnsureLoginWebViewProfileAsync(useXCookieProfile);

                    string exportedCookieFile = await ExportWebViewCookiesAsync(url);

                    if (job.UseInstaPrivateMode && IsInstagramPlatformUrl(url) &&
                        (string.IsNullOrEmpty(exportedCookieFile) || !CookieFileContainsCookie(exportedCookieFile, "sessionid")))
                    {
                        _isInstaLoggedIn = false;
                        throw new Exception("Instagram 로그인 세션을 확인하지 못했습니다. 프로그램 내부 Instagram 로그인 화면에서 홈 화면이 표시될 때까지 기다린 뒤 다시 시도해 주세요.");
                    }

                    if (string.IsNullOrEmpty(exportedCookieFile) && (job.UseXPrivateMode || job.UseInstaPrivateMode))
                    {
                        throw new Exception("브라우저에서 로그인 정보를 찾을 수 없습니다. 먼저 로그인 후 다운 화면에서 로그인을 완료해 주세요.");
                    }

                    if (!string.IsNullOrEmpty(exportedCookieFile) && File.Exists(exportedCookieFile))
                    {
                        cookieFile = Path.Combine(SettingsManager.UserDataFolder, $"temp_x_cookies_{job.Id}.txt");
                        File.Copy(exportedCookieFile, cookieFile, true);
                        CleanupManager.RegisterFile(cookieFile);
                        try { File.Delete(exportedCookieFile); } catch { }
                    }

                    customHeaders = new Dictionary<string, string>();
                    bool isYouTubeLoginDownload = LooksLikeYouTubeInput(url);
                    if (!isYouTubeLoginDownload)
                    {
                        bool isXAuthenticationTarget = IsXPlatformUrl(url) || IsXDirectMediaUrl(url);
                        if (isXAuthenticationTarget && !string.IsNullOrEmpty(_capturedAuthToken)) customHeaders["authorization"] = _capturedAuthToken;
                        if (isXAuthenticationTarget && !string.IsNullOrEmpty(_capturedCsrfToken)) customHeaders["x-csrf-token"] = _capturedCsrfToken;
                        if (!string.IsNullOrEmpty(_capturedUserAgent)) customHeaders["User-Agent"] = _capturedUserAgent;
                    }
                }
                finally
                {
                    _ytDlpBrowserSemaphore.Release();
                }
            }

            string finalFilePath = await DownloadWithKuaishouRetryAsync(
                downloader,
                url,
                job.SavePath,
                browser,
                job.JobCts.Token,
                cookieFile,
                customHeaders,
                () =>
                {
                    UpdateYtDlpJobStatus(job, "연결 다시 확인 중...");
                    lblYtDlpStatus.Text = "콰이쇼우 연결을 다시 확인하고 있습니다...";
                });

            string completedStatus = downloader.LastSubtitleDownloaded ? "완료 + 자막" : "완료";
            if (downloader.LastSponsorBlockFallback) completedStatus = "완료 (원본 저장)";
            else if (downloader.LastMetadataFallback) completedStatus = "완료 (게시 정보 제외)";
            UpdateYtDlpJobStatus(job, completedStatus);
            Notify("다운로드 완료", "웹사이트 영상 다운로드가 완료되었습니다.");
            ShowCompletedFileQuickUse(finalFilePath);

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

            if (IsXPlatformUrl(url))
                platformSuccess = job.UseXPrivateMode ? "X(비공개)" : "X";
            else if (lowerUrlSuccess.Contains("chzzk")) platformSuccess = "치지직";
            else if (lowerUrlSuccess.Contains("soop") || lowerUrlSuccess.Contains("afreeca")) platformSuccess = "SOOP";
            else if (lowerUrlSuccess.Contains("instagram")) platformSuccess = "Instagram";
            else if (IsThreadsPlatformUrl(url)) platformSuccess = "Threads";
            else if (lowerUrlSuccess.Contains("tiktok")) platformSuccess = "TikTok";
            else if (lowerUrlSuccess.Contains("snapchat")) platformSuccess = "Snapchat";
            else if (IsKuaishouPlatformUrl(url)) platformSuccess = "Kuaishou";
            else if (lowerUrlSuccess.Contains("pinterest")) platformSuccess = "Pinterest";
            else if (lowerUrlSuccess.Contains("anilife")) platformSuccess = "Anilife";
            else if (IsLinkkfPlatformUrl(url)) platformSuccess = "Linkkf";
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
            bool ytDlpExists = File.Exists(SettingsManager.GetYtDlpPath());
            bool ffmpegExists = File.Exists(SettingsManager.GetFFmpegPath());

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
            bool loginErrorHandled = await HandleLoginRequiredDownloadErrorAsync(
                url,
                ex,
                job.UseLoginBrowserCookies || job.UseXPrivateMode || job.UseInstaPrivateMode);
            if (!loginErrorHandled)
            {
                ShowCenteredMessage(errorMsg, "다운로드 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            ReportError(job.SourceFeature, "yt-dlp 영상 다운로드", $"yt-dlp 다운로드 실패 ({cause}) | URL: {url}", ex);
        }
        finally
        {
            if (!string.IsNullOrEmpty(cookieFile) && File.Exists(cookieFile))
            {
                try { File.Delete(cookieFile); } catch { }
            }
            CleanupManager.UnregisterFile(cookieFile);

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

    private string GetCurrentYtDlpErrorFeature()
    {
        return _isLoginBrowserMode || tglXPrivateMode.Checked || tglInstaPrivateMode.Checked
            ? "로그인 후 다운"
            : "웹사이트 영상 다운";
    }

    private async void BtnYtDlpRun_Click(object sender, EventArgs e)
    {
        string url = txtYtDlpUrl.Text.Trim();
        if (!_isInternalYtDlpRun)
        {
            List<string> urls = ExtractHttpUrls(url);
            if (urls.Count > 1)
            {
                await ConfirmAndQueueMultipleUrlsAsync(urls, fromYoutubeTab: false);
                return;
            }

            if (string.IsNullOrEmpty(url))
            {
                ShowCenteredMessage("다운로드할 URL을 입력해 주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!TryNormalizeHttpUrl(url, out url))
            {
                ShowCenteredMessage("http:// 또는 https://로 시작하는 올바른 영상 주소를 입력해 주세요.", "주소 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!string.IsNullOrEmpty(url) && LooksLikeYouTubeInput(url) && !_isLoginBrowserMode)
            {
                await RouteToYoutubeDownloadAsync(url);
                return;
            }

            double sectionStartSeconds = 0;
            double sectionEndSeconds = 0;
            if (chkYtDlpDownloadSection?.Checked == true &&
                !TryPromptDownloadSection(out sectionStartSeconds, out sectionEndSeconds))
            {
                return;
            }

            EnqueueYtDlpDownload(
                url,
                allowFolderPrompt: true,
                out _,
                sectionStartSeconds: sectionStartSeconds,
                sectionEndSeconds: sectionEndSeconds);
            return;
        }

        if (string.IsNullOrEmpty(url))
        {
            ShowCenteredMessage("다운로드할 URL을 입력해 주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!TryNormalizeHttpUrl(url, out url))
        {
            ShowCenteredMessage("http:// 또는 https://로 시작하는 올바른 영상 주소를 입력해 주세요.", "주소 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!string.IsNullOrEmpty(url) && LooksLikeYouTubeInput(url) && !_isLoginBrowserMode)
        {
            await RouteToYoutubeDownloadAsync(url);
            return;
        }

        double internalSectionStartSeconds = 0;
        double internalSectionEndSeconds = 0;
        if (chkYtDlpDownloadSection?.Checked == true &&
            !TryPromptDownloadSection(out internalSectionStartSeconds, out internalSectionEndSeconds))
        {
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
            if (LooksLikeYouTubeInput(url))
                await EnsureYouTubeExtractionToolsAsync(_ytDlpCts.Token);
            else
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
                PreferredTitle = _loginBrowserDownloadTitle,
                EmbedMetadata = chkYtDlpEmbedMetadata?.Checked == true,
                SectionStartSeconds = internalSectionStartSeconds,
                SectionEndSeconds = internalSectionEndSeconds
            };
            downloader.OnProgressChanged += (progress) =>
            {
                int pct = (int)Math.Min(progress, 100);
                string msg = $"다운로드 진행 중... {progress:F1}%";
                MirrorProgress(pct, msg);
            };

            downloader.WebViewResolver = async (targetUrl) =>
            {
                await _ytDlpBrowserSemaphore.WaitAsync();
                bool genericCaptureEnabled = false;
                try
                {
                    string capturedUrl = "";
                    string anilifeResolveDiagnostic = "";
                    bool anilifePageApiAttempted = false;
                    bool isAnilife = targetUrl.Contains("anilife.app", StringComparison.OrdinalIgnoreCase);
                    bool isXStatusTarget = IsXStatusPageUrl(targetUrl);
                    bool isThreads = IsThreadsPostUrl(targetUrl);
                    bool isKuaishou = IsKuaishouShortVideoUrl(targetUrl);
                    bool needsBrowserCapture = isAnilife ||
                        isThreads ||
                        isKuaishou ||
                        targetUrl.Contains("chzzk.naver.com", StringComparison.OrdinalIgnoreCase) ||
                        targetUrl.Contains("sooplive", StringComparison.OrdinalIgnoreCase) ||
                        IsLinkkfPlatformUrl(targetUrl);
                    genericCaptureEnabled = isKuaishou || !needsBrowserCapture;
                    if (genericCaptureEnabled) Interlocked.Increment(ref _genericMediaCaptureDepth);
                    try
                    {
                        if (webViewX.CoreWebView2 == null) await PreInitializeWebView2Async();
                    }
                    catch { }
                    this.Invoke((MethodInvoker)(() =>
                    {
                        ClearCapturedMediaCandidates();
                        if (webViewX.CoreWebView2 == null) return;
                        webViewX.CoreWebView2.Navigate(targetUrl);
                    }));

                    int maxAttempts = isAnilife ? 90 : needsBrowserCapture ? 60 : 24;
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
                                        await webViewX.CoreWebView2.ExecuteScriptAsync("window.dispatchEvent(new Event('mousemove')); window.dispatchEvent(new Event('scroll')); const video = document.querySelector('video'); if (video) { video.muted = true; video.play().catch(() => {}); }");
                                    }
                                }
                                catch { }
                            }));
                        }
                        if ((!needsBrowserCapture || isKuaishou) && i == 6)
                        {
                            this.Invoke((MethodInvoker)(async () =>
                            {
                                try
                                {
                                    if (webViewX.CoreWebView2 != null)
                                    {
                                        string activationScript = isXStatusTarget
                                            ? @"(() => {
    const videos = Array.from(document.querySelectorAll('video'));
    const visible = videos
        .map(video => ({ video, rect: video.getBoundingClientRect() }))
        .filter(item => item.rect.bottom > 0 && item.rect.right > 0 && item.rect.top < innerHeight && item.rect.left < innerWidth)
        .sort((a, b) => (b.rect.width * b.rect.height) - (a.rect.width * a.rect.height));
    const video = visible[0]?.video;
    if (video) { video.muted = true; video.play().catch(() => {}); }
})()"
                                            : "document.querySelectorAll('video').forEach(v => { v.muted = true; v.play().catch(() => {}); }); window.scrollTo(0, Math.max(document.body.scrollHeight / 3, 1));";
                                        await webViewX.CoreWebView2.ExecuteScriptAsync(activationScript);
                                    }
                                }
                                catch { }
                            }));
                        }

                        string bestCapturedUrl = isThreads || isKuaishou ? "" : GetBestCapturedMediaUrl();
                        if (isThreads && i >= 2)
                        {
                            var threadsMedia = await TryReadThreadsMediaFromWebViewAsync();
                            bestCapturedUrl = threadsMedia.Url;
                            if (!string.IsNullOrWhiteSpace(threadsMedia.Title))
                                downloader.PreferredTitle = threadsMedia.Title;
                            if (!string.IsNullOrWhiteSpace(bestCapturedUrl))
                                RememberCapturedMediaUrl(bestCapturedUrl);
                        }
                        if (isKuaishou && i >= 2)
                        {
                            var kuaishouMedia = await TryReadKuaishouMediaFromWebViewAsync();
                            bestCapturedUrl = kuaishouMedia.Url;
                            if (!string.IsNullOrWhiteSpace(kuaishouMedia.Title))
                                downloader.PreferredTitle = kuaishouMedia.Title;
                            if (string.IsNullOrWhiteSpace(bestCapturedUrl))
                                bestCapturedUrl = GetBestCapturedMediaUrl();
                            if (!string.IsNullOrWhiteSpace(bestCapturedUrl))
                                RememberCapturedMediaUrl(bestCapturedUrl);
                        }
                        if (isAnilife && !IsAnilifeManifestCaptureUrl(bestCapturedUrl)) bestCapturedUrl = "";
                        if (isAnilife && string.IsNullOrWhiteSpace(bestCapturedUrl) && i >= 4 && i % 4 == 0)
                        {
                            bestCapturedUrl = await TryReadAnilifeManifestFromWebViewAsync();
                            if (!string.IsNullOrWhiteSpace(bestCapturedUrl)) RememberCapturedMediaUrl(bestCapturedUrl);
                        }
                        if (isAnilife && string.IsNullOrWhiteSpace(bestCapturedUrl) && !anilifePageApiAttempted && i >= 12)
                        {
                            anilifePageApiAttempted = true;
                            var pageApiResult = await TryResolveAnilifeManifestViaPageApiAsync(targetUrl);
                            bestCapturedUrl = pageApiResult.Url;
                            anilifeResolveDiagnostic = pageApiResult.Diagnostic;
                            if (!string.IsNullOrWhiteSpace(bestCapturedUrl)) RememberCapturedMediaUrl(bestCapturedUrl);
                        }
                        if (!string.IsNullOrEmpty(bestCapturedUrl) && (needsBrowserCapture || i >= 8))
                        {
                            capturedUrl = bestCapturedUrl;
                            break;
                        }
                    }

                    if (isAnilife && !string.IsNullOrWhiteSpace(capturedUrl) && string.IsNullOrWhiteSpace(downloader.PreferredTitle))
                    {
                        string pageTitle = await TryReadAnilifePageTitleFromWebViewAsync();
                        if (!string.IsNullOrWhiteSpace(pageTitle)) downloader.PreferredTitle = pageTitle;
                    }

                    if (isAnilife && string.IsNullOrWhiteSpace(capturedUrl) && !string.IsNullOrWhiteSpace(anilifeResolveDiagnostic))
                    {
                        throw new Exception($"애니라이프 재생 정보를 가져오지 못했습니다.\n{anilifeResolveDiagnostic}");
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
                    if (genericCaptureEnabled) Interlocked.Decrement(ref _genericMediaCaptureDepth);
                    try
                    {
                        this.Invoke((MethodInvoker)(() =>
                        {
                            if (webViewX.CoreWebView2 != null)
                            {
                                webViewX.CoreWebView2.Stop();
                                webViewX.CoreWebView2.Navigate("about:blank");
                            }
                        }));
                    }
                    catch { }
                    _ytDlpBrowserSemaphore.Release();
                }
            };


            if (url.Contains("chzzk.naver.com") && !url.Contains("/video/") && !url.Contains("/clips/"))
            {
                ShowCenteredMessage("치지직은 video 또는 clips 주소만 다운로드할 수 있습니다.\n\n라이브는 다운로드 시작 후 5초 이상 시청한 뒤 다시 시도해 주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string browser = "none";
            string cookieFile = "";
            Dictionary<string, string>? customHeaders = null;
            
            string lowerUrlSuccess = url.ToLower();
            bool isTargetPlatform = ShouldUseLoginBrowserCookiesForUrl(lowerUrlSuccess);

            if (tglXPrivateMode.Checked || tglInstaPrivateMode.Checked || _isLoginBrowserMode || isTargetPlatform)
            {
                try
                {

                    customHeaders = new Dictionary<string, string>();

                    bool useXCookieProfile = tglXPrivateMode.Checked;
                    await EnsureLoginWebViewProfileAsync(useXCookieProfile);

                    cookieFile = await ExportWebViewCookiesAsync(url);

                    if (tglInstaPrivateMode.Checked && IsInstagramPlatformUrl(url) &&
                        (string.IsNullOrEmpty(cookieFile) || !CookieFileContainsCookie(cookieFile, "sessionid")))
                    {
                        _isInstaLoggedIn = false;
                        throw new Exception("Instagram 로그인 세션을 확인하지 못했습니다. 프로그램 내부 Instagram 로그인 화면에서 홈 화면이 표시될 때까지 기다린 뒤 다시 시도해 주세요.");
                    }
                    
                    if (string.IsNullOrEmpty(cookieFile) && (tglXPrivateMode.Checked || tglInstaPrivateMode.Checked))
                    {
                        ShowCenteredMessage("브라우저에서 로그인 정보를 찾을 수 없습니다.\n먼저 로그인 후 다운 화면에서 로그인을 완료해 주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }


                    bool isYouTubeLoginDownload = LooksLikeYouTubeInput(url);
                    if (!isYouTubeLoginDownload)
                    {
                        bool isXAuthenticationTarget = IsXPlatformUrl(url) || IsXDirectMediaUrl(url);
                        if (isXAuthenticationTarget && !string.IsNullOrEmpty(_capturedAuthToken)) customHeaders["authorization"] = _capturedAuthToken;
                        if (isXAuthenticationTarget && !string.IsNullOrEmpty(_capturedCsrfToken)) customHeaders["x-csrf-token"] = _capturedCsrfToken;
                        if (!string.IsNullOrEmpty(_capturedUserAgent)) customHeaders["User-Agent"] = _capturedUserAgent;
                    }
                }
                catch (Exception ex)
                {
                    if (tglXPrivateMode.Checked || tglInstaPrivateMode.Checked)
                    {
                        ShowCenteredMessage(
                            UserErrorFormatter.Format("로그인 정보를 가져오지 못했습니다.", ex),
                            "로그인 정보 오류",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        ReportError("로그인 후 다운", "로그인 쿠키 준비", $"yt-dlp 로그인 정보 가져오기 실패 | URL: {url}", ex);
                        return;
                    }
                }
            }
            
            string finalFilePath = await DownloadWithKuaishouRetryAsync(
                downloader,
                url,
                savePath,
                browser,
                _ytDlpCts.Token,
                cookieFile,
                customHeaders,
                () =>
                {
                    lblYtDlpStatus.Text = "콰이쇼우 연결을 다시 확인하고 있습니다...";
                    if (_isLoginBrowserMode) lblXStatus.Text = "콰이쇼우 연결을 다시 확인하고 있습니다...";
                });
            _loginBrowserDownloadTitle = "";

            Notify("다운로드 완료", "영상 다운로드가 완료되었습니다.");
            ShowCenteredMessage($"다운로드 완료!\n저장 위치: {finalFilePath}", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ShowCompletedFileQuickUse(finalFilePath);
            
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

            if (IsXPlatformUrl(url)) 
                platformSuccess = tglXPrivateMode.Checked ? "X(비공개)" : "X";
            else if (lowerUrlSuccess.Contains("chzzk")) platformSuccess = "치지직";
            else if (lowerUrlSuccess.Contains("soop") || lowerUrlSuccess.Contains("afreeca")) platformSuccess = "SOOP";
            else if (lowerUrlSuccess.Contains("instagram")) platformSuccess = "Instagram";
            else if (IsThreadsPlatformUrl(url)) platformSuccess = "Threads";
            else if (lowerUrlSuccess.Contains("tiktok")) platformSuccess = "TikTok";
            else if (lowerUrlSuccess.Contains("snapchat")) platformSuccess = "Snapchat";
            else if (IsKuaishouPlatformUrl(url)) platformSuccess = "Kuaishou";
            else if (lowerUrlSuccess.Contains("pinterest")) platformSuccess = "Pinterest";
            else if (lowerUrlSuccess.Contains("anilife")) platformSuccess = "Anilife";
            else if (IsLinkkfPlatformUrl(url)) platformSuccess = "Linkkf";
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
            bool ytDlpExists = File.Exists(SettingsManager.GetYtDlpPath());
            bool ffmpegExists = File.Exists(SettingsManager.GetFFmpegPath());

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
            bool loginErrorHandled = await HandleLoginRequiredDownloadErrorAsync(
                url,
                ex,
                tglXPrivateMode.Checked || tglInstaPrivateMode.Checked || _isLoginBrowserMode);
            if (!loginErrorHandled)
            {
                ShowCenteredMessage(errorMsg, "다운로드 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            ReportError(GetCurrentYtDlpErrorFeature(), "즉시 영상 다운로드", $"yt-dlp 다운로드 실패 ({cause}) | URL: {url}", ex);
        }
        finally
        {

            string cookiePath = Path.Combine(SettingsManager.UserDataFolder, "temp_x_cookies.txt");
            if (File.Exists(cookiePath)) 
            {
                try { File.Delete(cookiePath); } catch {}
            }
            CleanupManager.UnregisterFile(cookiePath);

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
        if (job.Video == null) return downloaded;
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

    private static string GetDownloadJobTitle(DownloadJob job)
    {
        if (!string.IsNullOrWhiteSpace(job.CustomFileName)) return job.CustomFileName;
        if (job.Video != null && !string.IsNullOrWhiteSpace(job.Video.Title)) return job.Video.Title;
        if (!string.IsNullOrWhiteSpace(job.Url)) return job.Url;
        return "YouTube 영상";
    }

    private static bool LooksLikeUnreadableTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return true;

        int replacementCount = title.Count(ch => ch == '\uFFFD' || ch == '�');
        if (replacementCount > 0) return true;

        int visibleCount = title.Count(ch => !char.IsWhiteSpace(ch));
        if (visibleCount == 0) return true;

        int questionCount = title.Count(ch => ch == '?');
        return visibleCount <= 12 && questionCount >= Math.Max(3, visibleCount / 2);
    }

    private static bool IsFallbackYouTubeTitle(string title)
    {
        return string.IsNullOrWhiteSpace(title)
            || title.StartsWith("YouTube_Video_", StringComparison.OrdinalIgnoreCase)
            || LooksLikeUnreadableTitle(title);
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

    private static AudioOnlyStreamInfo? GetPreferredYoutubeAudioStream(StreamManifest? manifest)
    {
        if (manifest == null) return null;

        return manifest.GetAudioOnlyStreams()
            .OrderByDescending(stream => stream.IsAudioLanguageDefault == true)
            .ThenByDescending(stream => stream.Bitrate.BitsPerSecond)
            .FirstOrDefault();
    }

    private async Task ProcessDownloadQueueAsync()
    {
        _isDownloading = true;

        try
        {
            await EnsureFFmpegAsync();
            Xabe.FFmpeg.FFmpeg.SetExecutablesPath(Path.GetDirectoryName(SettingsManager.GetFFmpegPath()));
        }
        catch (Exception ex)
        {
            while (_downloadQueue.TryDequeue(out var pendingJob))
            {
                if (lvQueue.Items.Contains(pendingJob.ListViewItem))
                    pendingJob.ListViewItem.SubItems[2].Text = "실패: 필수 도구 준비 오류";
                pendingJob.JobCts.Dispose();
            }

            lblStatus.Text = "다운로드를 시작하지 못했습니다. 필수 도구를 확인해 주세요.";
            pbYoutube.Value = 0;
            _isDownloading = false;
            _downloadWidgetForm?.SetProgress(null);
            _downloadWidgetForm?.SetBusy(false);
            _downloadWidgetForm?.SetStatus("필수 도구를 준비하지 못했습니다. 앱에서 오류 내용을 확인해 주세요.");
            Notify("다운로드 실패", "YouTube 다운로드 필수 도구를 준비하지 못했습니다.");
            ShowCenteredMessage(
                UserErrorFormatter.Format("YouTube 다운로드에 필요한 필수 도구를 준비하지 못했습니다.", ex),
                "필수 도구 오류",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            ReportError("유튜브 다운로더", "필수 도구 준비", "YouTube 다운로드 시작 전 FFmpeg 준비 실패", ex);
            return;
        }

        while (_downloadQueue.TryDequeue(out var job))
        {
            try
            {
                string jobTitle = GetDownloadJobTitle(job);
                if (job.JobCts.IsCancellationRequested || !lvQueue.Items.Contains(job.ListViewItem))
                    continue;
                
                _activeJobs.Add(job);
                _downloadWidgetForm?.SetBusy(true);
                _downloadWidgetForm?.SetProgress(0);
                _downloadWidgetForm?.SetStatus("\uC720\uD29C\uBE0C \uB2E4\uC6B4\uB85C\uB4DC \uC2DC\uC791");
                _downloadWidgetForm?.ShowToast("\uB2E4\uC6B4\uB85C\uB4DC\uAC00 \uC2DC\uC791\uB418\uC5C8\uC2B5\uB2C8\uB2E4.");
                job.ListViewItem.SubItems[2].Text = "준비 중...";
                lblStatus.Text = $"진행 중: {jobTitle}";

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

                if (job.Manifest == null || job.EmbedMetadata || job.RemoveSponsorSegments || job.SectionEndSeconds > job.SectionStartSeconds)
                {
                    // yt-dlp fallback job
                    await DownloadYoutubeWithYtDlpFallbackAsync(job);
                    continue;
                }

                IStreamInfo[] targetStreams;
                var audioStream = GetPreferredYoutubeAudioStream(job.Manifest);
                if (audioStream == null)
                    throw new InvalidOperationException("YouTube manifest contains no audio stream.");

                if (!job.Option.IsVideo)
                {
                    targetStreams = new IStreamInfo[] { audioStream };
                }
                else
                {
                    var videoStream = job.Manifest.GetVideoOnlyStreams().FirstOrDefault(s => s.VideoQuality.Label == job.Option.Id) 
                                      ?? job.Manifest.GetVideoOnlyStreams().GetWithHighestVideoQuality();
                    if (videoStream == null)
                        throw new InvalidOperationException("YouTube manifest contains no video stream.");
                    targetStreams = new IStreamInfo[] { audioStream, videoStream };
                }

                var builder = new ConversionRequestBuilder(job.OutputPath)
                    .SetFFmpegPath(SettingsManager.GetFFmpegPath())
                    .SetPreset(ConversionPreset.UltraFast);
                
                if (!job.Option.IsVideo) 
                {
                    string ext = job.Option.Id.Replace("best_", "");
                    builder.SetContainer(ext);
                }

                CleanupManager.RegisterFile(job.OutputPath);
                await _youtube.Videos.DownloadAsync(targetStreams, builder.Build(), progress, job.JobCts.Token);
                CleanupManager.UnregisterFile(job.OutputPath);
                job.MediaDownloadCompleted = true;

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
                
                Notify("다운로드 성공", $"{jobTitle} 다운로드가 완료되었습니다.");
                ShowCompletedFileQuickUse(job.OutputPath);
                

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
                CleanupManager.DeleteTemporaryPath(job.TemporaryDownloadDirectory);
                CleanupManager.DeleteCanceledDownloadArtifacts(job.OutputPath, deleteOutputFile: !job.MediaDownloadCompleted);
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
                Notify("다운로드 취소", $"{GetDownloadJobTitle(job)} 다운로드가 취소되었습니다.");
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
                string failureMessage = BuildDownloadFailureMessage(failedUrl, ex, job.OutputPath);
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

                ReportError(job.SourceFeature, "영상 다운로드", $"유튜브 다운로드 실패 ({cause}) | URL: {failedUrl}", ex);
            }            finally
            {
                CleanupManager.DeleteTemporaryPath(job.TemporaryDownloadDirectory);
                job.TemporaryDownloadDirectory = "";
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
            await EnsureYouTubeExtractionToolsAsync();

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = $"--ignore-config --encoding utf-8 --no-playlist --playlist-items 1 {GetYtDlpJavaScriptRuntimeArguments()}--print \"%(title)s\t%(thumbnail)s\t%(duration)s\" \"{url}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi };
            string output = "";
            string errorOutput = "";
            process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) output = e.Data; };
            process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) errorOutput += e.Data + Environment.NewLine; };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
            {
                string loginHint = IsExplicitYouTubeLoginRequiredError(errorOutput)
                    ? "이 영상은 비공개, 회원전용, 나이제한 또는 로그인 확인이 필요한 영상일 수 있습니다. [로그인 후 다운]에서 YouTube에 로그인한 뒤 다시 시도해 주세요."
                    : "일부공개 영상은 URL만 있으면 받을 수 있어야 하므로 일반/우회 방식으로 시도했습니다. 계속 실패하면 YouTube 구조 변경, 라이브/쇼츠 특수 포맷, yt-dlp 대응 지연 가능성이 큽니다.";
                if (!IsExplicitYouTubeLoginRequiredError(errorOutput))
                {
                    output = $"YouTube_Video_{DateTime.Now:yyyyMMdd_HHmmss}|";
                }
                else
                {
                throw new Exception(
                    "우회 방식을 통해서도 YouTube 영상 정보를 가져오지 못했습니다.\n\n" +
                    loginHint + "\n\n" +
                    "[오류 내용]\n" + (string.IsNullOrWhiteSpace(errorOutput) ? "yt-dlp가 상세 오류를 반환하지 않았습니다." : errorOutput.Trim()) + "\n\n" +
                    "우회 방식을 통해서도 영상 정보를 가져오지 못했습니다.\n\n" +
                    "이 경우 보통 유튜브 보안 구조 변경, 비공개/회원 전용/나이 제한 영상, 라이브 영상, 지역 제한, 삭제된 영상 때문에 발생합니다.\n\n" +
                    "해볼 수 있는 방법:\n" +
                    "1. 로그인이 필요한 영상이면 [로그인 후 다운]에서 먼저 로그인한 뒤 다시 시도해 주세요.\n" +
                    "2. 일반 공개 영상도 같은 오류가 나면 yt-dlp 최신 버전 대응이 아직 안 된 영상일 수 있습니다.\n" +
                    "3. 같은 URL이 계속 실패하면 영상 URL과 오류 내용을 제작자에게 보내 주세요.\n\n" +
                    "DRM 보호 영상이나 권한이 없는 비공개 영상은 로그인해도 받을 수 없습니다.");
            }

            }

            var parts = output.Split('\t', 3);
            _customTitle = parts[0].Trim();
            if (LooksLikeUnreadableTitle(_customTitle))
            {
                _customTitle = $"YouTube_Video_{DateTime.Now:yyyyMMdd_HHmmss}";
            }
            string thumbUrl = parts.Length > 1 ? parts[1].Trim() : "";
            _currentVideoDurationSeconds = parts.Length > 2 &&
                double.TryParse(
                    parts[2].Trim(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double durationSeconds) && durationSeconds > 0
                ? durationSeconds
                : null;

            lblVideoTitle.Text = _currentVideoDurationSeconds is > 0
                ? $"{_customTitle}\n길이: {FormatSectionTime(_currentVideoDurationSeconds.Value)}"
                : _customTitle;
            
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
            ShowCenteredMessage(
                UserErrorFormatter.Format("우회 방식으로도 유튜브 영상 정보를 불러오지 못했습니다.", ex),
                "영상 정보 오류",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            lblVideoTitle.Text = "오류가 발생했습니다.";
            ReportError(_pendingYoutubeSourceFeature, "yt-dlp 영상 정보 우회 조회", $"YouTube 정보 우회 조회 실패 | URL: {url}", ex);
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

        await EnsureYouTubeExtractionToolsAsync(job.JobCts.Token);
        if (!this.IsDisposed && !this.Disposing && lvQueue.Items.Contains(job.ListViewItem))
        {
            this.Invoke((MethodInvoker)delegate {
                job.ListViewItem.SubItems[2].Text = "yt-dlp download starting...";
                lblStatus.Text = "yt-dlp download starting...";
            });
        }

        string ytDlpPath = SettingsManager.GetYtDlpPath();
        string ffmpegDir = Path.GetDirectoryName(SettingsManager.GetFFmpegPath()) ?? AppDomain.CurrentDomain.BaseDirectory;
        job.TemporaryDownloadDirectory = Path.Combine(Path.GetTempPath(), "MMT", "Downloads", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(job.TemporaryDownloadDirectory);
        CleanupManager.RegisterFile(job.TemporaryDownloadDirectory);
        
        string formatArg = GetYtDlpFallbackFormat(job.Option);
        bool isAudioOnly = !job.Option.IsVideo;
        string ext = isAudioOnly ? job.Option.Id.Replace("best_", "") : "mp4";
            string outputTemplate = job.OutputPath;
            string sourceUrl = !string.IsNullOrWhiteSpace(job.Url) ? job.Url : job.Video?.Url ?? "";
        if (!IsYouTubeSingleVideoInput(sourceUrl))
            throw new InvalidOperationException("다운로드할 YouTube 영상 주소가 비어 있거나 올바르지 않습니다.");
        if (job.Video == null && IsFallbackYouTubeTitle(job.CustomFileName))
        {
            string directory = Path.GetDirectoryName(job.OutputPath) ?? GetDownloadSaveDirectory(sourceUrl, isAudioOnly);
            string siteTemplate = BuildYtDlpOutputNameTemplate("YouTube");
            outputTemplate = Path.Combine(directory, $"{siteTemplate}.%(ext)s");
        }

        string arguments = $"--ignore-config --newline --encoding utf-8 --no-playlist --playlist-items 1 -S \"lang\" --paths temp:\"{job.TemporaryDownloadDirectory}\" --print \"after_move:MMT_FILE:%(filepath)s\" {GetYtDlpJavaScriptRuntimeArguments()}--ffmpeg-location \"{ffmpegDir}\" ";

        if (job.SectionEndSeconds > job.SectionStartSeconds)
        {
            string section = $"*{FormatSectionTime(job.SectionStartSeconds)}-{FormatSectionTime(job.SectionEndSeconds)}";
            arguments += $"--download-sections \"{section}\" --force-keyframes-at-cuts ";
        }

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

        if (job.EmbedMetadata)
        {
            arguments += "--embed-metadata --embed-thumbnail --embed-chapters ";
        }

        if (job.RemoveSponsorSegments)
        {
            arguments += "--sponsorblock-remove sponsor ";
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
        string downloadedFilePath = "";
        
        System.Text.RegularExpressions.Regex progressRegex = new System.Text.RegularExpressions.Regex(@"\[download\]\s+(?<percent>\d+(\.\d+)?)%");
        
        process.OutputDataReceived += (s, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            processLog.AppendLine(e.Data);
            if (e.Data.StartsWith("MMT_FILE:", StringComparison.Ordinal))
            {
                downloadedFilePath = e.Data["MMT_FILE:".Length..].Trim().Trim('"');
            }
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

        if (process.ExitCode != 0 && job.RemoveSponsorSegments &&
            IsSponsorBlockProcessFailure(processLog.ToString()))
        {
            CleanupManager.DeleteTemporaryPath(job.TemporaryDownloadDirectory);
            CleanupManager.DeleteCanceledDownloadArtifacts(job.OutputPath, deleteOutputFile: true);
            job.RemoveSponsorSegments = false;
            await DownloadYoutubeWithYtDlpFallbackAsync(job);
            if (!this.IsDisposed && !this.Disposing && lvQueue.Items.Contains(job.ListViewItem))
                job.ListViewItem.SubItems[2].Text = "완료 (원본 저장)";
            return;
        }

        if (process.ExitCode != 0 && job.EmbedMetadata &&
            IsMetadataProcessFailure(processLog.ToString()))
        {
            CleanupManager.DeleteTemporaryPath(job.TemporaryDownloadDirectory);
            CleanupManager.DeleteCanceledDownloadArtifacts(job.OutputPath, deleteOutputFile: true);
            job.EmbedMetadata = false;
            await DownloadYoutubeWithYtDlpFallbackAsync(job);
            if (!this.IsDisposed && !this.Disposing && lvQueue.Items.Contains(job.ListViewItem))
                job.ListViewItem.SubItems[2].Text = "완료 (게시 정보 제외)";
            return;
        }

        if (process.ExitCode != 0 && !File.Exists(outputTemplate))
        {
            string detail = processLog.ToString().Trim();
            if (string.IsNullOrWhiteSpace(detail)) detail = "yt-dlp did not return detail.";
            throw new Exception("yt-dlp download failed\n" + detail);
        }

        job.MediaDownloadCompleted = true;

        if (!this.IsDisposed && !this.Disposing && lvQueue.Items.Contains(job.ListViewItem))
        {
            this.Invoke((MethodInvoker)delegate {
                job.ListViewItem.SubItems[2].Text = "완료" + (HasSubtitleOutputForJob(job, processLog.ToString()) ? " + 자막" : "");
                pbYoutube.Value = 100;
            });
        }
        
        string completedFilePath = File.Exists(downloadedFilePath) ? downloadedFilePath : job.OutputPath;
        Notify("다운로드 성공", $"{job.CustomFileName} 다운로드가 완료되었습니다.");
        ShowCompletedFileQuickUse(completedFilePath);
        LogUsage("YouTube");

        LogDownload(BuildDownloadHistoryEntry("YouTube", job.CustomFileName, completedFilePath));

        if (SettingsManager.Settings.AutoOpenFolder)
        {
            string folder = Path.GetDirectoryName(completedFilePath);
            OpenFolder(folder);
        }
    }

    private static bool IsSponsorBlockProcessFailure(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return false;
        return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Any(line =>
                (line.Contains("SponsorBlock", StringComparison.OrdinalIgnoreCase) ||
                 line.Contains("ModifyChapters", StringComparison.OrdinalIgnoreCase)) &&
                (line.Contains("ERROR", StringComparison.OrdinalIgnoreCase) ||
                 line.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
                 line.Contains("unable", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsMetadataProcessFailure(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return false;
        return output.Contains("EmbedThumbnail", StringComparison.OrdinalIgnoreCase) ||
               output.Contains("Metadata", StringComparison.OrdinalIgnoreCase) &&
               output.Contains("PostProcessingError", StringComparison.OrdinalIgnoreCase) ||
               output.Contains("Unable to embed thumbnail", StringComparison.OrdinalIgnoreCase) ||
               output.Contains("AtomicParsley", StringComparison.OrdinalIgnoreCase);
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

        if (!TryPrepareWritableDirectory(outDir, out string preparedOutDir, out string folderError))
        {
            lblWebMStatus.Text = "저장 위치를 확인해 주세요.";
            ShowCenteredMessage(folderError, "저장 위치 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        outDir = preparedOutDir;

        string fileNameNoExt = Path.GetFileNameWithoutExtension(inputFile);
        string outputFile = "";
        string args = "";
        bool isSequence = false;
        string sequenceDir = "";

        if (format.Contains("WebM"))
        {
            outputFile = GetUniqueOutputPath(Path.Combine(outDir, fileNameNoExt + "_converted.webm"));
            args = $"-i \"{inputFile}\" -c:v libvpx-vp9 -crf 30 -b:v 0 -c:a libopus \"{outputFile}\" -y";
        }
        else if (format.Contains("MOV"))
        {
            outputFile = GetUniqueOutputPath(Path.Combine(outDir, fileNameNoExt + ".mov"));
            args = $"-i \"{inputFile}\" -c:v libx264 -crf 18 -c:a aac -b:a 192k \"{outputFile}\" -y";
        }
        else if (format.Contains("MKV"))
        {
            outputFile = GetUniqueOutputPath(Path.Combine(outDir, fileNameNoExt + ".mkv"));
            args = $"-i \"{inputFile}\" -c:v libx264 -crf 18 -c:a aac -b:a 192k \"{outputFile}\" -y";
        }
        else if (format.Contains("AVI"))
        {
            outputFile = GetUniqueOutputPath(Path.Combine(outDir, fileNameNoExt + ".avi"));
            // AVI prefers mp3 or pcm common for old players, but h264/mp3 is a good modern balance
            args = $"-i \"{inputFile}\" -c:v libx264 -crf 18 -c:a libmp3lame -b:a 192k \"{outputFile}\" -y";
        }
        else if (format.Contains("WMV"))
        {
            outputFile = GetUniqueOutputPath(Path.Combine(outDir, fileNameNoExt + ".wmv"));
            args = $"-i \"{inputFile}\" -c:v wmv2 -b:v 5M -c:a wmav2 -b:a 128k \"{outputFile}\" -y";
        }
        else if (format.Contains("GIF"))
        {
            outputFile = GetUniqueOutputPath(Path.Combine(outDir, fileNameNoExt + ".gif"));
            // Reduced scale to 480p and fps to 12 to prevent hangs on large files
            args = $"-i \"{inputFile}\" -vf \"fps=12,scale=480:-1:flags=lanczos,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\" \"{outputFile}\" -y";
        }
        else if (format.Contains("JPG Sequence"))
        {
            isSequence = true;
            sequenceDir = GetUniqueOutputPath(Path.Combine(outDir, fileNameNoExt + "_jpg_seq"), directory: true);
            outputFile = Path.Combine(sequenceDir, "frame_%04d.jpg");
            args = $"-i \"{inputFile}\" -qscale:v 2 \"{outputFile}\" -y";
        }
        else if (format.Contains("PNG Sequence"))
        {
            isSequence = true;
            sequenceDir = GetUniqueOutputPath(Path.Combine(outDir, fileNameNoExt + "_png_seq"), directory: true);
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

            if (isSequence) Directory.CreateDirectory(sequenceDir);
 
            CleanupManager.RegisterFile(isSequence ? sequenceDir : outputFile);

            await RunFFmpegWithProgress(
                args,
                inputFile,
                pbWebM,
                lblWebMStatus,
                _webmCts.Token,
                isSequence ? sequenceDir : outputFile,
                isSequence);
  
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
            ShowCenteredMessage(
                UserErrorFormatter.Format("포맷 변환 중 오류가 발생했습니다.", ex, inputFile),
                "포맷 변환 오류",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            ReportError("포맷 변환기", $"{format} 변환", $"포맷 변환 실패 | Input: {inputFile}, Format: {format}", ex);
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

        if (!TryPrepareWritableDirectory(outDir, out string preparedOutDir, out string folderError))
        {
            lblCodecStatus.Text = "저장 위치를 확인해 주세요.";
            ShowCenteredMessage(folderError, "저장 위치 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        outDir = preparedOutDir;

        string outputFile = GetUniqueOutputPath(Path.Combine(outDir, Path.GetFileNameWithoutExtension(inputFile) + "_fixed.mp4"));

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
            await RunFFmpegWithProgress(args, inputFile, pbCodec, lblCodecStatus, _codecCts.Token, outputFile);
  
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
            ShowCenteredMessage(
                UserErrorFormatter.Format("Pr/AE 호환 코덱 변환 중 오류가 발생했습니다.", ex, inputFile),
                "코덱 변환 오류",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            ReportError("Pr/AE 코덱 해결", "호환 MP4 변환", $"코덱 변환 실패 | Input: {inputFile}", ex);
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

        if (!TryPrepareWritableDirectory(outDir, out string preparedOutDir, out string folderError))
        {
            lblAudioStatus.Text = "저장 위치를 확인해 주세요.";
            ShowCenteredMessage(folderError, "저장 위치 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        outDir = preparedOutDir;

        string outputFile = GetUniqueOutputPath(Path.Combine(outDir, Path.GetFileNameWithoutExtension(inputFile) + $"_converted.{ext}"));

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
 
            await RunFFmpegWithProgress(args, inputFile, pbAudio, lblAudioStatus, _audioCts.Token, outputFile);
  
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
            ShowCenteredMessage(
                UserErrorFormatter.Format("오디오 변환 중 오류가 발생했습니다.", ex, inputFile),
                "오디오 변환 오류",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            ReportError("오디오 변환기", $"{format} 변환", $"오디오 변환 실패 | Input: {inputFile}, Format: {format}", ex);
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
        if (!TryPrepareWritableDirectory(newPath, out string preparedPath, out string folderError))
        {
            ShowCenteredMessage(folderError, "저장 위치 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        newPath = preparedPath;
        txtDownloadFolder.Text = preparedPath;

        if (chkUseCustomSiteFolders?.Checked == true &&
            txtSiteFolderOverride != null &&
            !string.IsNullOrWhiteSpace(txtSiteFolderOverride.Text))
        {
            if (!TryPrepareWritableDirectory(txtSiteFolderOverride.Text, out string preparedOverride, out string overrideError))
            {
                ShowCenteredMessage(overrideError, "사이트별 저장 위치 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            txtSiteFolderOverride.Text = preparedOverride;
        }

        SettingsManager.Settings.DefaultDownloadFolder = newPath;
        SettingsManager.Settings.ShowNotifications = chkShowNotifications.Checked;
        SettingsManager.Settings.AutoOpenFolder = chkAutoOpenFolder.Checked;
        SettingsManager.Settings.AutoUpdateCheck = chkAutoUpdateCheck.Checked;
        if (chkEnableWidgetMode != null)
        {
            SettingsManager.Settings.EnableWidgetMode = chkEnableWidgetMode.Checked;
        }
        if (chkEnableCompletedFileQuickUse != null)
        {
            SettingsManager.Settings.EnableCompletedFileQuickUse = chkEnableCompletedFileQuickUse.Checked;
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
            SettingsManager.Settings.ConcurrentDownloads = GetConcurrentDownloadsValue(cmbConcurrentDownloads?.Text);
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
        ReloadExperimentalFeaturesUI();
    }


    private async void BtnCheckUpdate_Click(object sender, EventArgs e)
    {
        if (!btnCheckUpdate.Enabled) return;

        btnCheckUpdate.Enabled = false;
        try
        {
            await CheckForUpdateAsync(true);
        }
        finally
        {
            if (!IsDisposed && !btnCheckUpdate.IsDisposed)
                btnCheckUpdate.Enabled = true;
        }
    }

    private void ConfigureUpdatedAppForeground()
    {
        if (!Program.StartedAfterUpdate && !Program.StartedAfterFailedUpdate) return;

        TopMost = true;
        Shown += (s, e) =>
        {
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
            BringToFront();
            Activate();
            SetForegroundWindow(Handle);

            if (Program.StartedAfterFailedUpdate)
            {
                string reason = "설치된 프로그램 파일을 다른 프로그램이 사용 중이어서 교체하지 못했습니다.";
                try
                {
                    if (File.Exists(UpdateFailureMarkerPath))
                    {
                        string savedReason = File.ReadAllText(UpdateFailureMarkerPath, Encoding.UTF8).Trim();
                        if (!string.IsNullOrWhiteSpace(savedReason)) reason = savedReason;
                        File.Delete(UpdateFailureMarkerPath);
                    }
                }
                catch { }

                ShowCenteredMessage(
                    $"업데이트를 완료하지 못해 기존 버전을 다시 실행했습니다.\n\n{reason}\n\n안내된 프로그램을 종료한 뒤 업데이트를 다시 시도해 주세요.\n\n계속 안 되면 프로그램 설정의 [다른 버전 받기]에서 최신 버전을 내려받아 수동으로 설치할 수 있습니다.",
                    "업데이트 설치 실패",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            var releaseTopMostTimer = new System.Windows.Forms.Timer { Interval = 1500 };
            releaseTopMostTimer.Tick += (timerSender, timerEvent) =>
            {
                releaseTopMostTimer.Stop();
                releaseTopMostTimer.Dispose();
                if (!IsDisposed) TopMost = false;
            };
            releaseTopMostTimer.Start();
        };
    }

    private async Task CheckForUpdateAsync(bool manual)
    {
        UpdateProgressForm? updateProgress = null;

        try
        {
            if (manual)
            {
                updateProgress = new UpdateProgressForm();
                updateProgress.ShowCentered(this, "새 버전을 확인하고 있습니다...");
                await Task.Yield();
            }

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
                    if (!manual && string.Equals(
                            SettingsManager.Settings.SkippedUpdateVersion?.Trim(),
                            latestVersion,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    updateProgress?.Close();
                    updateProgress?.Dispose();
                    updateProgress = null;

                    var updatePrompt = ShowUpdateAvailableDialog(latestVersion);
                    var result = updatePrompt.Result;

                    if (result != DialogResult.Yes && updatePrompt.SkipThisVersion)
                    {
                        SettingsManager.Settings.SkippedUpdateVersion = latestVersion;
                        SettingsManager.Save();
                    }

                    if (result == DialogResult.Yes)
                    {
                        updateProgress = new UpdateProgressForm();
                        updateProgress.ShowCentered(this, "업데이트 파일을 준비하고 있습니다...");
                        await Task.Yield();

                        if (string.Equals(
                                SettingsManager.Settings.SkippedUpdateVersion?.Trim(),
                                latestVersion,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            SettingsManager.Settings.SkippedUpdateVersion = "";
                            SettingsManager.Save();
                        }

                        var assets = root.GetProperty("assets");
                        string expectedInstallerName = $"MMT_Setup_v{latestVersion}.exe";
                        string downloadUrl = "";
                        string expectedDigest = "";
                        foreach (var asset in assets.EnumerateArray())
                        {
                            string fileName = asset.GetProperty("name").GetString() ?? "";
                            if (fileName.Equals(expectedInstallerName, StringComparison.OrdinalIgnoreCase))
                            {
                                downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                                if (asset.TryGetProperty("digest", out JsonElement digestElement))
                                    expectedDigest = digestElement.GetString() ?? "";
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
                                    long? totalBytes = downloadResponse.Content.Headers.ContentLength;
                                    long downloadedBytes = 0;

                                    using (var contentStream = await downloadResponse.Content.ReadAsStreamAsync())
                                    using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                                    {
                                        var buffer = new byte[8192];
                                        int read;
                                        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                        {
                                            await fileStream.WriteAsync(buffer.AsMemory(0, read));
                                            downloadedBytes += read;
                                            updateProgress.SetDownloadProgress(downloadedBytes, totalBytes);
                                        }
                                    }
                                }

                                updateProgress.SetIndeterminate("다운로드한 업데이트 파일을 확인하고 있습니다...");
                                if (!IsValidWindowsExecutable(tempFile))
                                {
                                    try { File.Delete(tempFile); } catch { }
                                    throw new InvalidDataException("다운로드한 업데이트 설치 파일이 올바른 Windows EXE가 아닙니다.");
                                }

                                if (!string.IsNullOrWhiteSpace(expectedDigest))
                                {
                                    updateProgress.SetIndeterminate("업데이트 파일의 안전성을 검증하고 있습니다...");
                                    await using var installerStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                                    byte[] actualHash = await SHA256.HashDataAsync(installerStream);
                                    string actualDigest = "sha256:" + Convert.ToHexString(actualHash).ToLowerInvariant();
                                    if (!actualDigest.Equals(expectedDigest.Trim(), StringComparison.OrdinalIgnoreCase))
                                    {
                                        try { File.Delete(tempFile); } catch { }
                                        throw new InvalidDataException("업데이트 설치 파일의 SHA-256 검증에 실패했습니다. 설치를 중단합니다.");
                                    }
                                }

                                updateProgress?.Close();
                                updateProgress?.Dispose();
                                updateProgress = null;

                                if (!ShowUpdateReadyDialog(latestVersion))
                                    return;

                                updateProgress = new UpdateProgressForm();
                                updateProgress.ShowCentered(this, "설치 프로그램을 실행하고 있습니다...");
                                await Task.Delay(500);

                                LaunchUpdateInstallerAfterExit(tempFile);
                                await Task.Delay(300);
                                Application.Exit();
                            }
                            catch (Exception ex)
                            {
                                updateProgress?.Close();
                                updateProgress?.Dispose();
                                updateProgress = null;
                                ShowCenteredMessage(
                                    UserErrorFormatter.Format("업데이트 파일을 다운로드하거나 실행하지 못했습니다.", ex) +
                                    "\n\n계속 안 되면 프로그램 설정의 [다른 버전 받기]에서 최신 버전을 내려받아 수동으로 설치할 수 있습니다.",
                                    "업데이트 실패",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            updateProgress?.Close();
                            updateProgress?.Dispose();
                            updateProgress = null;
                            ShowCenteredMessage(
                                $"업데이트 설치 파일을 찾지 못했습니다.\n\n필요한 파일명: {expectedInstallerName}\n잠시 후 다시 확인하거나 [다른 버전 받기]를 이용해 주세요.",
                                "업데이트 파일 없음",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                        }
                    }
                }
                else if (manual)
                {
                    updateProgress?.Close();
                    updateProgress?.Dispose();
                    updateProgress = null;
                    ShowCenteredMessage("현재 최신 버전을 사용 중입니다.", "업데이트 확인");
                }
            }
        }
        catch (Exception ex)
        {
            if (manual)
            {
                updateProgress?.Close();
                updateProgress?.Dispose();
                updateProgress = null;
                ShowCenteredMessage(
                    UserErrorFormatter.Format("새 버전을 확인하지 못했습니다.", ex) +
                    "\n\n프로그램 설정의 [다른 버전 받기]에서 최신 버전을 직접 내려받을 수도 있습니다.",
                    "업데이트 확인 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        finally
        {
            if (updateProgress != null)
            {
                updateProgress.Close();
                updateProgress.Dispose();
            }
        }
    }

    // ============================================
    // HELPER CLASSES AND METHODS
    // ============================================

    private async Task RunFFmpegWithProgress(
        string args,
        string inputFile,
        ProgressBar pb,
        Label lbl,
        CancellationToken token,
        string expectedOutputPath,
        bool outputIsDirectory = false)
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
        var errorTail = new Queue<string>();
        long lastUpdate = 0; // Performance throttle

        using (token.Register(() => { try { if (!proc.HasExited) proc.Kill(true); } catch { } }))
        {
            try {
                while (true)
                {
                    if (token.IsCancellationRequested) break;
                    string? line = await proc.StandardError.ReadLineAsync();
                    if (line == null) break;

                    if (errorTail.Count >= 60) errorTail.Dequeue();
                    errorTail.Enqueue(line);

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

        if (proc.ExitCode != 0)
        {
            CleanupManager.DeleteTemporaryPath(expectedOutputPath);
            string detail = string.Join(Environment.NewLine, errorTail);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(detail)
                    ? $"FFmpeg 처리가 실패했습니다. 종료 코드: {proc.ExitCode}"
                    : $"FFmpeg 처리가 실패했습니다. 종료 코드: {proc.ExitCode}\n{detail}");
        }

        bool outputCreated = outputIsDirectory
            ? Directory.Exists(expectedOutputPath) && Directory.EnumerateFiles(expectedOutputPath).Any(file => new FileInfo(file).Length > 0)
            : File.Exists(expectedOutputPath) && new FileInfo(expectedOutputPath).Length > 0;
        if (!outputCreated)
        {
            CleanupManager.DeleteTemporaryPath(expectedOutputPath);
            throw new InvalidOperationException("FFmpeg 처리는 끝났지만 결과 파일이 생성되지 않았습니다.");
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

    private async Task EnsureFFmpegAsync(CancellationToken ct = default)
    {
        await _ffmpegInstallSemaphore.WaitAsync(ct);
        try
        {
            string ffmpegPath = SettingsManager.GetFFmpegPath();
            string ffprobePath = SettingsManager.GetFFprobePath();

            if (IsValidWindowsExecutable(ffmpegPath) && IsValidWindowsExecutable(ffprobePath))
            {
                Xabe.FFmpeg.FFmpeg.SetExecutablesPath(Path.GetDirectoryName(ffmpegPath));
                return;
            }

            if (File.Exists(ffmpegPath) && !IsValidWindowsExecutable(ffmpegPath)) try { File.Delete(ffmpegPath); } catch { }
            if (File.Exists(ffprobePath) && !IsValidWindowsExecutable(ffprobePath)) try { File.Delete(ffprobePath); } catch { }

            string toolsDir = Path.GetDirectoryName(ffmpegPath) ?? SettingsManager.UserDataFolder;
            Directory.CreateDirectory(toolsDir);
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, toolsDir);

            ffmpegPath = SettingsManager.GetFFmpegPath();
            ffprobePath = SettingsManager.GetFFprobePath();
            if (!IsValidWindowsExecutable(ffmpegPath) || !IsValidWindowsExecutable(ffprobePath))
            {
                throw new InvalidDataException("다운로드 후 ffmpeg.exe 또는 ffprobe.exe를 확인할 수 없습니다.");
            }

            Xabe.FFmpeg.FFmpeg.SetExecutablesPath(Path.GetDirectoryName(ffmpegPath));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "FFmpeg 필수 도구를 준비하지 못했습니다. 네트워크 연결을 확인한 뒤 다시 시도해 주세요.",
                ex);
        }
        finally
        {
            _ffmpegInstallSemaphore.Release();
        }
    }

    private static bool IsValidWindowsExecutable(string path)
    {
        return File.Exists(path) && !IsGitLfsPointer(path) && HasMzHeader(path);
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
                    await Task.Run(() => CleanupManager.CleanupWebViewData());
                }
                else
                {
                    await Task.Run(() => CleanupManager.CleanupWebViewNonLoginData());
                }
            }
            else if (!SettingsManager.Settings.KeepLoginSession)
            {
                await Task.Run(() =>
                {
                    try { if (Directory.Exists(userDataPath)) Directory.Delete(userDataPath, true); } catch { }
                });
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
                webViewX.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
                webViewX.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
                try
                {
                    await webViewX.CoreWebView2.Profile.ClearBrowsingDataAsync(
                        CoreWebView2BrowsingDataKinds.PasswordAutosave |
                        CoreWebView2BrowsingDataKinds.GeneralAutofill);
                }
                catch { }
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
                webViewX.CoreWebView2.HistoryChanged += (s, e) =>
                {
                    UpdateLoginBrowserNavigationState();
                    ResetCapturedXMediaWhenStatusChanges();
                };
                webViewX.CoreWebView2.SourceChanged += (s, e) =>
                {
                    ResetCapturedXMediaWhenStatusChanges();
                    UpdateLoginBrowserFavoriteButton();
                };
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
                    ClearCapturedMediaCandidates();
                    _capturedXStatusId = ExtractXStatusId(e.Uri);
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
                            string resourceType = root.TryGetProperty("type", out var typeValue) ? typeValue.GetString() ?? "" : "";
                            bool genericCaptureActive = Volatile.Read(ref _genericMediaCaptureDepth) > 0;
                            if (LooksLikeCapturedMediaUrl(url, genericCaptureActive ? resourceType : ""))
                            {
                                RememberCapturedMediaUrl(url);
                                Debug.WriteLine($"[MMT-Intercept] Video request found: {GetBestCapturedMediaUrl()}");
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

                webViewX.CoreWebView2.GetDevToolsProtocolEventReceiver("Network.responseReceived").DevToolsProtocolEventReceived += (sender, args) =>
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(args.ParameterObjectAsJson);
                        var root = doc.RootElement;
                        if (!root.TryGetProperty("response", out var response)) return;

                        string url = response.TryGetProperty("url", out var urlValue) ? urlValue.GetString() ?? "" : "";
                        if (IsAnilifeMediaApiUrl(url) && root.TryGetProperty("requestId", out var requestIdValue))
                        {
                            string requestId = requestIdValue.GetString() ?? "";
                            if (!string.IsNullOrWhiteSpace(requestId))
                            {
                                lock (_capturedMediaLock) _pendingAnilifeMediaRequestIds.Add(requestId);
                            }
                        }

                        if (Volatile.Read(ref _genericMediaCaptureDepth) <= 0) return;
                        string mimeType = response.TryGetProperty("mimeType", out var mimeValue) ? mimeValue.GetString() ?? "" : "";
                        string resourceType = root.TryGetProperty("type", out var typeValue) ? typeValue.GetString() ?? "" : "";
                        if (LooksLikeCapturedMediaUrl(url, resourceType, mimeType))
                        {
                            RememberCapturedMediaUrl(url);
                            Debug.WriteLine($"[MMT-Intercept] Video response found: {GetBestCapturedMediaUrl()} ({mimeType})");
                        }
                    }
                    catch { }
                };

                webViewX.CoreWebView2.GetDevToolsProtocolEventReceiver("Network.loadingFinished").DevToolsProtocolEventReceived += (sender, args) =>
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(args.ParameterObjectAsJson);
                        string requestId = doc.RootElement.TryGetProperty("requestId", out var requestIdValue)
                            ? requestIdValue.GetString() ?? ""
                            : "";
                        if (string.IsNullOrWhiteSpace(requestId)) return;

                        bool shouldCapture;
                        lock (_capturedMediaLock)
                        {
                            shouldCapture = _pendingAnilifeMediaRequestIds.Remove(requestId);
                        }

                        if (shouldCapture) _ = CaptureAnilifeMediaResponseAsync(requestId);
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
            popupWebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
            popupWebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
            try
            {
                await popupWebView.CoreWebView2.Profile.ClearBrowsingDataAsync(
                    CoreWebView2BrowsingDataKinds.PasswordAutosave |
                    CoreWebView2BrowsingDataKinds.GeneralAutofill);
            }
            catch { }
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

        if (btnLoginBrowserFavorite?.Visible == true) UpdateLoginBrowserFavoriteButton();
    }

    private void NavigateLoginBrowserBack()
    {
        if (webViewX.CoreWebView2?.CanGoBack == true)
        {
            webViewX.CoreWebView2.GoBack();
        }
    }

    private void ClearCapturedMediaCandidates()
    {
        lock (_capturedMediaLock)
        {
            _capturedMediaUrls.Clear();
            _capturedM3u8Url = "";
            _capturedAnilifeManifestUrl = "";
            _pendingAnilifeMediaRequestIds.Clear();
        }
    }

    private static bool IsAnilifeMediaApiUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               uri.Host.Equals("api.anilife.app", StringComparison.OrdinalIgnoreCase) &&
               uri.AbsolutePath.StartsWith("/v1/media/", StringComparison.OrdinalIgnoreCase);
    }

    private async Task CaptureAnilifeMediaResponseAsync(string requestId)
    {
        try
        {
            if (webViewX.CoreWebView2 == null || string.IsNullOrWhiteSpace(requestId)) return;

            string parameters = JsonSerializer.Serialize(new { requestId });
            string responseJson = await webViewX.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.getResponseBody", parameters);
            using var responseDoc = JsonDocument.Parse(responseJson);
            JsonElement root = responseDoc.RootElement;
            if (!root.TryGetProperty("body", out var bodyValue)) return;

            string body = bodyValue.GetString() ?? "";
            if (root.TryGetProperty("base64Encoded", out var encodedValue) && encodedValue.GetBoolean())
            {
                byte[] bytes = Convert.FromBase64String(body);
                if (bytes.Length > 4_000_000) return;
                body = Encoding.UTF8.GetString(bytes);
            }

            string manifestUrl = YtDlpDownloader.TryResolveAnilifeManifestFromMediaResponse(body);
            if (!LooksLikeCapturedMediaUrl(manifestUrl)) return;

            lock (_capturedMediaLock) _capturedAnilifeManifestUrl = manifestUrl;
            RememberCapturedMediaUrl(manifestUrl);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AnilifeWebViewResolver] response capture failed: {ex.Message}");
        }
    }

    private void ResetCapturedXMediaWhenStatusChanges()
    {
        string source = webViewX.CoreWebView2?.Source ?? "";
        string statusId = ExtractXStatusId(source);
        if (string.IsNullOrWhiteSpace(statusId) ||
            statusId.Equals(_capturedXStatusId, StringComparison.Ordinal))
        {
            return;
        }

        _capturedXStatusId = statusId;
        ClearCapturedMediaCandidates();
    }

    private static string ExtractXStatusId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(
            url,
            @"(?:x\.com|twitter\.com)/[^/?#]+/status/(\d+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "";
    }

    private static bool IsXStatusPageUrl(string url)
    {
        return !string.IsNullOrWhiteSpace(ExtractXStatusId(url));
    }

    private void RememberCapturedMediaUrl(string url)
    {
        string normalized = NormalizeCapturedMediaUrl(url);
        lock (_capturedMediaLock)
        {
            if (!_capturedMediaUrls.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                _capturedMediaUrls.Add(normalized);

            _capturedM3u8Url = _capturedMediaUrls
                .Select((candidate, index) => new { candidate, index, score = ScoreCapturedMediaUrl(candidate) })
                .OrderByDescending(item => item.score)
                .ThenByDescending(item => item.index)
                .Select(item => item.candidate)
                .FirstOrDefault() ?? normalized;
        }
    }

    private static void LaunchUpdateInstallerAfterExit(string installerPath)
    {
        int parentProcessId = Environment.ProcessId;
        string fallbackAppPath = Environment.ProcessPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Multi Media Toolkit",
            "Multi Media Toolkit.exe");
        string launcherPath = Path.Combine(Path.GetTempPath(), $"MMT_UpdateLauncher_{Guid.NewGuid():N}.ps1");
        string installerLogPath = Path.Combine(Path.GetTempPath(), $"MMT_Update_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        string launcherErrorPath = installerLogPath + ".launcher-error.txt";

        string launcherScript = """
param(
    [int]$ParentId,
    [string]$InstallerPath,
    [string]$LogPath,
    [string]$LauncherErrorPath,
    [string]$FallbackAppPath
)

try {
    Wait-Process -Id $ParentId -ErrorAction SilentlyContinue
    $setupArguments = @(
        '/VERYSILENT',
        '/SUPPRESSMSGBOXES',
        '/NORESTART',
        '/SP-',
        '/LANG=korean',
        '/CLOSEAPPLICATIONS',
        '/FORCECLOSEAPPLICATIONS',
        '/MERGETASKS="!desktopicon"',
        ('/LOG="' + $LogPath + '"')
    )
    $setup = Start-Process -FilePath $InstallerPath -ArgumentList $setupArguments -WindowStyle Hidden -PassThru -Wait
    if ($setup.ExitCode -ne 0) {
        throw "Update installer exited with code $($setup.ExitCode)."
    }
}
catch {
    $errorDetails = ($_ | Out-String).Trim()
    $errorDetails | Out-File -LiteralPath $LauncherErrorPath -Encoding utf8

    $failureMessage = '업데이트 설치 중 기존 프로그램 파일을 교체하지 못했습니다.'
    if (Test-Path -LiteralPath $LogPath) {
        $logText = Get-Content -LiteralPath $LogPath -Raw -ErrorAction SilentlyContinue
        $blockerMatch = [regex]::Match($logText, 'RestartManager found an application using one of our files:\s*([^\r\n]+)')
        if ($blockerMatch.Success) {
            $failureMessage = "'$($blockerMatch.Groups[1].Value.Trim())' 프로그램이 Multi Media Toolkit 파일을 사용 중입니다."
        }
        elseif ($logText -match 'DeleteFile.*(?:code 32|코드 32)') {
            $failureMessage = '다른 프로그램이 Multi Media Toolkit 실행 파일을 사용 중입니다.'
        }
    }

    $userDataFolder = Join-Path $env:LOCALAPPDATA 'YoutubeDownloader'
    $failureMarkerPath = Join-Path $userDataFolder 'update-error.txt'
    New-Item -ItemType Directory -Path $userDataFolder -Force | Out-Null
    $failureMessage | Out-File -LiteralPath $failureMarkerPath -Encoding utf8

    if (Test-Path -LiteralPath $FallbackAppPath) {
        Start-Process -FilePath $FallbackAppPath -ArgumentList '/updatefailed'
    }
}
finally {
    Remove-Item -LiteralPath $PSCommandPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $InstallerPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $LogPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $LauncherErrorPath -Force -ErrorAction SilentlyContinue
}
""";

        File.WriteAllText(launcherPath, launcherScript, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var launcherInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{launcherPath}\" -ParentId {parentProcessId} -InstallerPath \"{installerPath}\" -LogPath \"{installerLogPath}\" -LauncherErrorPath \"{launcherErrorPath}\" -FallbackAppPath \"{fallbackAppPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        _ = Process.Start(launcherInfo) ?? throw new InvalidOperationException("업데이트 설치 실행기를 시작하지 못했습니다.");
    }

    private static void CleanupOldVersionedExecutables()
    {
        try
        {
            string currentPath = Environment.ProcessPath ?? string.Empty;
            string installFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Multi Media Toolkit");

            if (!Directory.Exists(installFolder) ||
                !Path.GetFileName(currentPath).StartsWith("Multi Media Toolkit.v", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            foreach (string file in Directory.EnumerateFiles(installFolder, "Multi Media Toolkit.v*.exe"))
            {
                if (file.Equals(currentPath, StringComparison.OrdinalIgnoreCase)) continue;
                try { File.Delete(file); } catch { }
            }
        }
        catch { }
    }

    private static void CleanupStaleUpdateArtifacts()
    {
        try
        {
            string tempFolder = Path.GetTempPath();
            foreach (string pattern in new[]
                     {
                         "MMT_Setup_v*.exe",
                         "MMT_Update_*.log",
                         "MMT_Update_*.log.launcher-error.txt",
                         "MMT_UpdateLauncher_*.ps1"
                     })
            {
                foreach (string file in Directory.EnumerateFiles(tempFolder, pattern))
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }
        catch { }
    }

    private async Task<(string Url, string Title)> TryReadThreadsMediaFromWebViewAsync()
    {
        var completion = new TaskCompletionSource<(string Url, string Title)>(TaskCreationOptions.RunContinuationsAsynchronously);

        void BeginRead()
        {
            _ = ReadOnUiAsync();

            async Task ReadOnUiAsync()
            {
                try
                {
                    if (webViewX.CoreWebView2 == null)
                    {
                        completion.TrySetResult(("", ""));
                        return;
                    }

                    string resultJson = await webViewX.CoreWebView2.ExecuteScriptAsync("""
(() => {
    const viewportWidth = window.innerWidth || document.documentElement.clientWidth || 1;
    const viewportHeight = window.innerHeight || document.documentElement.clientHeight || 1;
    const candidates = Array.from(document.querySelectorAll('video'))
        .map(video => {
            const url = String(video.currentSrc || video.src || '');
            const rect = video.getBoundingClientRect();
            const visibleWidth = Math.max(0, Math.min(rect.right, viewportWidth) - Math.max(rect.left, 0));
            const visibleHeight = Math.max(0, Math.min(rect.bottom, viewportHeight) - Math.max(rect.top, 0));
            return {
                url,
                top: rect.top,
                visibleArea: visibleWidth * visibleHeight,
                area: Math.max(0, rect.width * rect.height)
            };
        })
        .filter(item => /^https?:\/\//i.test(item.url));

    const visible = candidates.filter(item => item.visibleArea > 0);
    const pool = visible.length > 0 ? visible : candidates;
    pool.sort((a, b) => {
        if (visible.length > 0 && b.visibleArea !== a.visibleArea) return b.visibleArea - a.visibleArea;
        if (b.area !== a.area) return b.area - a.area;
        return Math.abs(a.top) - Math.abs(b.top);
    });

    const title = document.querySelector('meta[property="og:description"]')?.content ||
        document.querySelector('meta[property="og:title"]')?.content ||
        document.title || '';
    return { url: pool[0]?.url || '', title: String(title) };
})()
""");

                    using JsonDocument document = JsonDocument.Parse(resultJson);
                    JsonElement root = document.RootElement;
                    string url = root.TryGetProperty("url", out JsonElement urlValue) ? urlValue.GetString() ?? "" : "";
                    string title = root.TryGetProperty("title", out JsonElement titleValue) ? titleValue.GetString() ?? "" : "";
                    title = System.Net.WebUtility.HtmlDecode(title)
                        .Replace("\r", " ")
                        .Replace("\n", " ")
                        .Trim();

                    completion.TrySetResult((LooksLikeCapturedMediaUrl(url) ? url : "", title));
                }
                catch
                {
                    completion.TrySetResult(("", ""));
                }
            }
        }

        try
        {
            if (IsDisposed || Disposing) return ("", "");
            if (InvokeRequired) BeginInvoke((Action)BeginRead);
            else BeginRead();

            Task completedTask = await Task.WhenAny(completion.Task, Task.Delay(2500));
            return completedTask == completion.Task ? await completion.Task : ("", "");
        }
        catch
        {
            return ("", "");
        }
    }

    private async Task<(string Url, string Title)> TryReadKuaishouMediaFromWebViewAsync()
    {
        var completion = new TaskCompletionSource<(string Url, string Title)>(TaskCreationOptions.RunContinuationsAsynchronously);

        void BeginRead()
        {
            _ = ReadOnUiAsync();

            async Task ReadOnUiAsync()
            {
                try
                {
                    if (webViewX.CoreWebView2 == null)
                    {
                        completion.TrySetResult(("", ""));
                        return;
                    }

                    string resultJson = await webViewX.CoreWebView2.ExecuteScriptAsync("""
(() => {
    const cache = window.__APOLLO_STATE__?.defaultClient || {};
    const photoId = location.pathname.match(/^\/short-video\/([^/?#]+)/i)?.[1] || '';
    const resolveRef = value => {
        if (value && typeof value === 'object' && value.type === 'id' && value.id && cache[value.id]) {
            return cache[value.id];
        }
        return value;
    };

    let detail = null;
    for (const [key, value] of Object.entries(cache)) {
        if (!key.toLowerCase().includes('visionvideodetail') || !key.includes(photoId)) continue;
        const resolved = resolveRef(value);
        if (resolved && resolveRef(resolved.photo)) {
            detail = resolved;
            break;
        }
    }

    const photo = resolveRef(detail?.photo) || null;
    const representations = [];
    for (const adaptation of photo?.manifest?.adaptationSet || []) {
        for (const representation of adaptation?.representation || []) {
            if (!representation || representation.hidden === true) continue;
            const urls = [];
            if (typeof representation.url === 'string') urls.push(representation.url);
            if (typeof representation.backupUrl === 'string') urls.push(representation.backupUrl);
            if (Array.isArray(representation.backupUrl)) urls.push(...representation.backupUrl);
            for (const url of urls) {
                if (!/^https?:\/\//i.test(String(url || ''))) continue;
                representations.push({
                    url: String(url),
                    pixels: Number(representation.width || 0) * Number(representation.height || 0),
                    bitrate: Math.max(Number(representation.avgBitrate || 0), Number(representation.maxBitrate || 0)),
                    frameRate: Number(representation.frameRate || 0)
                });
            }
        }
    }
    representations.sort((a, b) => b.pixels - a.pixels || b.bitrate - a.bitrate || b.frameRate - a.frameRate);

    const viewportWidth = window.innerWidth || document.documentElement.clientWidth || 1;
    const viewportHeight = window.innerHeight || document.documentElement.clientHeight || 1;
    const videos = Array.from(document.querySelectorAll('video')).map(video => {
        const rect = video.getBoundingClientRect();
        const visibleWidth = Math.max(0, Math.min(rect.right, viewportWidth) - Math.max(rect.left, 0));
        const visibleHeight = Math.max(0, Math.min(rect.bottom, viewportHeight) - Math.max(rect.top, 0));
        return {
            url: String(video.currentSrc || video.src || ''),
            visibleArea: visibleWidth * visibleHeight,
            area: Math.max(0, rect.width * rect.height)
        };
    }).filter(item => /^https?:\/\//i.test(item.url));
    videos.sort((a, b) => b.visibleArea - a.visibleArea || b.area - a.area);

    let videoResource = typeof photo?.videoResource === 'string' ? photo.videoResource : '';
    if (!/^https?:\/\//i.test(videoResource)) videoResource = '';
    const url = representations[0]?.url || photo?.photoUrl || videoResource || videos[0]?.url || '';
    let title = photo?.caption ||
        document.querySelector('.video-info-title')?.textContent ||
        document.querySelector('meta[property="og:title"]')?.content ||
        '';
    title = String(title || '').trim();
    if (title === '短视频-快手' || title === '快手') title = '';
    return { url: String(url || ''), title };
})()
""");

                    using JsonDocument document = JsonDocument.Parse(resultJson);
                    JsonElement root = document.RootElement;
                    string url = root.TryGetProperty("url", out JsonElement urlValue) ? urlValue.GetString() ?? "" : "";
                    string title = root.TryGetProperty("title", out JsonElement titleValue) ? titleValue.GetString() ?? "" : "";
                    title = System.Net.WebUtility.HtmlDecode(title)
                        .Replace("\r", " ")
                        .Replace("\n", " ")
                        .Trim();

                    completion.TrySetResult((LooksLikeCapturedMediaUrl(url) ? url : "", title));
                }
                catch
                {
                    completion.TrySetResult(("", ""));
                }
            }
        }

        try
        {
            if (IsDisposed || Disposing) return ("", "");
            if (InvokeRequired) BeginInvoke((Action)BeginRead);
            else BeginRead();

            Task completedTask = await Task.WhenAny(completion.Task, Task.Delay(2500));
            return completedTask == completion.Task ? await completion.Task : ("", "");
        }
        catch
        {
            return ("", "");
        }
    }

    private async Task<string> TryReadAnilifePageTitleFromWebViewAsync()
    {
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        void BeginRead()
        {
            _ = ReadOnUiAsync();

            async Task ReadOnUiAsync()
            {
                try
                {
                    if (webViewX.CoreWebView2 == null)
                    {
                        completion.TrySetResult("");
                        return;
                    }

                    string encodedTitle = await webViewX.CoreWebView2.ExecuteScriptAsync("document.title || ''");
                    string title = JsonSerializer.Deserialize<string>(encodedTitle) ?? "";
                    title = System.Net.WebUtility.HtmlDecode(title)
                        .Replace("\r", " ")
                        .Replace("\n", " ")
                        .Trim();

                    foreach (string suffix in new[] { " | 애니라이프", " | Anilife" })
                    {
                        if (title.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                            title = title[..^suffix.Length].Trim();
                    }

                    if (title.Equals("애니라이프", StringComparison.OrdinalIgnoreCase) ||
                        title.Equals("Anilife", StringComparison.OrdinalIgnoreCase))
                    {
                        title = "";
                    }

                    completion.TrySetResult(title);
                }
                catch
                {
                    completion.TrySetResult("");
                }
            }
        }

        try
        {
            if (IsDisposed || Disposing) return "";
            if (InvokeRequired) BeginInvoke((Action)BeginRead);
            else BeginRead();

            Task completedTask = await Task.WhenAny(completion.Task, Task.Delay(2000));
            return completedTask == completion.Task ? await completion.Task : "";
        }
        catch
        {
            return "";
        }
    }

    private async Task<(string Url, string Diagnostic)> TryResolveAnilifeManifestViaPageApiAsync(string targetUrl)
    {
        string videoId = "";
        string encodedReferer = "";
        try
        {
            var uri = new Uri(targetUrl);
            encodedReferer = Uri.EscapeDataString(uri.PathAndQuery);
            foreach (string parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = parameter.Split('=', 2);
                if (parts.Length == 2 && parts[0].Equals("id", StringComparison.OrdinalIgnoreCase))
                {
                    videoId = Uri.UnescapeDataString(parts[1]);
                    break;
                }
            }
        }
        catch { }

        if (string.IsNullOrWhiteSpace(videoId))
            return ("", "영상 ID를 주소에서 확인하지 못했습니다.");

        string deviceToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var completion = new TaskCompletionSource<(string Url, string Diagnostic)>(TaskCreationOptions.RunContinuationsAsynchronously);

        void BeginResolve()
        {
            _ = ResolveOnUiAsync();

            async Task ResolveOnUiAsync()
            {
                try
                {
                    CoreWebView2? core = webViewX.CoreWebView2;
                    if (core == null)
                    {
                        completion.TrySetResult(("", "로그인 브라우저가 준비되지 않았습니다."));
                        return;
                    }

                    if (!Uri.TryCreate(core.Source, UriKind.Absolute, out var sourceUri) ||
                        !sourceUri.Host.Equals("anilife.app", StringComparison.OrdinalIgnoreCase))
                    {
                        completion.TrySetResult(("", "Anilife 페이지가 브라우저에서 열리지 않았습니다."));
                        return;
                    }

                    string bootstrapScript = """
(() => {
    window.__mmtAnilifeResolve = { done: false };
    const finish = value => { window.__mmtAnilifeResolve = { done: true, ...value }; };
    (async () => {
        try {
            const videoId = __VIDEO_ID__;
            const encodedReferer = __ENCODED_REFERER__;
            const deviceToken = __DEVICE_TOKEN__;
            const buildId = String(window.__NUXT__?.config?.public?.buildVersion || window.__NUXT__?.config?.public?.buildId || '');
            if (!buildId) { finish({ ok: false, stage: 'build' }); return; }

            const tokenResponse = await fetch('https://api.anilife.app/v1/csrf/token', {
                credentials: 'include',
                headers: { Accept: 'application/json' }
            });
            if (!tokenResponse.ok) { finish({ ok: false, stage: 'csrf', status: tokenResponse.status }); return; }

            const tokenData = await tokenResponse.json();
            const csrfToken = String(tokenData.token || tokenData.csrfToken || '');
            const csrfHeader = String(tokenData.headerName || 'x-csrf-token');
            if (!csrfToken || !/^[A-Za-z0-9-]+$/.test(csrfHeader)) {
                finish({ ok: false, stage: 'csrf-data' });
                return;
            }

            const headers = {
                Accept: 'application/json',
                'Content-Type': 'application/json',
                'x-client-id': 'web',
                'x-build-id': buildId,
                'x-anilife-referer': encodedReferer,
                'x-device-token': deviceToken
            };
            headers[csrfHeader] = csrfToken;

            const mediaResponse = await fetch(`https://api.anilife.app/v1/media/${encodeURIComponent(videoId)}`, {
                credentials: 'include',
                headers
            });
            if (!mediaResponse.ok) { finish({ ok: false, stage: 'media', status: mediaResponse.status }); return; }

            const body = await mediaResponse.text();
            if (body.length > 4000000) { finish({ ok: false, stage: 'size' }); return; }
            finish({ ok: true, body });
        } catch (error) {
            finish({ ok: false, stage: 'exception', message: String(error || '') });
        }
    })();
})()
"""
                        .Replace("__VIDEO_ID__", JsonSerializer.Serialize(videoId), StringComparison.Ordinal)
                        .Replace("__ENCODED_REFERER__", JsonSerializer.Serialize(encodedReferer), StringComparison.Ordinal)
                        .Replace("__DEVICE_TOKEN__", JsonSerializer.Serialize(deviceToken), StringComparison.Ordinal);

                    await core.ExecuteScriptAsync(bootstrapScript);

                    for (int attempt = 0; attempt < 30; attempt++)
                    {
                        await Task.Delay(250);
                        string encodedState = await core.ExecuteScriptAsync("JSON.stringify(window.__mmtAnilifeResolve || { done: false })");
                        string stateJson = JsonSerializer.Deserialize<string>(encodedState) ?? "";
                        if (string.IsNullOrWhiteSpace(stateJson)) continue;

                        using var stateDoc = JsonDocument.Parse(stateJson);
                        JsonElement state = stateDoc.RootElement;
                        if (!state.TryGetProperty("done", out var doneValue) || !doneValue.GetBoolean()) continue;

                        bool ok = state.TryGetProperty("ok", out var okValue) && okValue.GetBoolean();
                        if (ok)
                        {
                            string body = state.TryGetProperty("body", out var bodyValue) ? bodyValue.GetString() ?? "" : "";
                            string manifestUrl = YtDlpDownloader.TryResolveAnilifeManifestFromMediaResponse(body);
                            completion.TrySetResult(string.IsNullOrWhiteSpace(manifestUrl)
                                ? ("", "Anilife 미디어 응답 형식이 변경되었습니다.")
                                : (manifestUrl, ""));
                            return;
                        }

                        string stage = state.TryGetProperty("stage", out var stageValue) ? stageValue.GetString() ?? "" : "";
                        int status = state.TryGetProperty("status", out var statusValue) && statusValue.TryGetInt32(out int parsedStatus) ? parsedStatus : 0;
                        string diagnostic = stage switch
                        {
                            "build" => "Anilife 페이지 설정을 읽지 못했습니다.",
                            "csrf" => $"Anilife 토큰 요청이 거부되었습니다 (HTTP {status}).",
                            "csrf-data" => "Anilife 토큰 응답 형식이 변경되었습니다.",
                            "media" => $"Anilife 미디어 요청이 거부되었습니다 (HTTP {status}).",
                            "size" => "Anilife 미디어 응답 크기가 비정상적으로 큽니다.",
                            "exception" => "Anilife 페이지 내부 요청이 차단되었습니다.",
                            _ => "Anilife 페이지 내부 요청에 실패했습니다."
                        };
                        completion.TrySetResult(("", diagnostic));
                        return;
                    }

                    completion.TrySetResult(("", "Anilife 페이지 내부 요청 시간이 초과되었습니다."));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AnilifePageApiResolver] error: {ex.Message}");
                    completion.TrySetResult(("", "Anilife 페이지 내부 요청을 실행하지 못했습니다."));
                }
                finally
                {
                    try
                    {
                        if (webViewX.CoreWebView2 != null)
                            await webViewX.CoreWebView2.ExecuteScriptAsync("delete window.__mmtAnilifeResolve");
                    }
                    catch { }
                }
            }
        }

        try
        {
            if (IsDisposed || Disposing) return ("", "프로그램이 종료 중입니다.");
            if (InvokeRequired) BeginInvoke((Action)BeginResolve);
            else BeginResolve();

            Task completedTask = await Task.WhenAny(completion.Task, Task.Delay(10_000));
            return completedTask == completion.Task
                ? await completion.Task
                : ("", "Anilife 페이지 내부 요청 시간이 초과되었습니다.");
        }
        catch
        {
            return ("", "Anilife 페이지 내부 요청을 시작하지 못했습니다.");
        }
    }

    private async Task<string> TryReadAnilifeManifestFromWebViewAsync()
    {
        lock (_capturedMediaLock)
        {
            if (!string.IsNullOrWhiteSpace(_capturedAnilifeManifestUrl))
                return _capturedAnilifeManifestUrl;
        }

        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        void BeginRead()
        {
            _ = ReadAsync();

            async Task ReadAsync()
            {
                try
                {
                    if (webViewX.CoreWebView2 == null)
                    {
                        completion.TrySetResult("");
                        return;
                    }

                    string resultJson = await webViewX.CoreWebView2.ExecuteScriptAsync(@"
(() => {
    const entries = performance.getEntriesByType('resource');
    for (let index = entries.length - 1; index >= 0; index--) {
        const url = String(entries[index].name || '');
        if (url.includes('api.gcdn.app/v1/manifest/a/') && url.includes('.m3u8')) return url;
    }
    return '';
})()");
                    string result = JsonSerializer.Deserialize<string>(resultJson) ?? "";
                    completion.TrySetResult(LooksLikeCapturedMediaUrl(result) ? result : "");
                }
                catch
                {
                    completion.TrySetResult("");
                }
            }
        }

        try
        {
            if (IsDisposed || Disposing) return "";
            if (InvokeRequired) BeginInvoke((Action)BeginRead);
            else BeginRead();

            Task completedTask = await Task.WhenAny(completion.Task, Task.Delay(2500));
            return completedTask == completion.Task ? await completion.Task : "";
        }
        catch
        {
            return "";
        }
    }

    private string GetBestCapturedMediaUrl(string preferredMediaId = "")
    {
        lock (_capturedMediaLock)
        {
            IEnumerable<string> candidates = _capturedMediaUrls;
            if (!string.IsNullOrWhiteSpace(preferredMediaId))
            {
                var matched = _capturedMediaUrls
                    .Where(url => url.Contains(preferredMediaId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (matched.Count == 0) return "";
                candidates = matched;
            }

            return candidates
                .Select((candidate, index) => new { candidate, index, score = ScoreCapturedMediaUrl(candidate) })
                .OrderByDescending(item => item.score)
                .ThenByDescending(item => item.index)
                .Select(item => item.candidate)
                .FirstOrDefault() ?? "";
        }
    }

    private static int ScoreCapturedMediaUrl(string url)
    {
        string lower = url.ToLowerInvariant();
        if (IsLikelyAdvertisingMediaUrl(lower)) return int.MinValue;

        int score = 0;
        if (lower.Contains(".m3u8")) score += 500;
        else if (lower.Contains(".mpd")) score += 450;
        else if (lower.Contains(".mp4")) score += 350;
        else score += 250;

        if (lower.Contains("master") || lower.Contains("playlist")) score += 80;
        if (lower.Contains("1080")) score += 30;
        else if (lower.Contains("720")) score += 20;
        System.Text.RegularExpressions.Match resolution = System.Text.RegularExpressions.Regex.Match(lower, @"(?<!\d)(\d{2,4})x(\d{2,4})(?!\d)");
        if (resolution.Success &&
            int.TryParse(resolution.Groups[1].Value, out int width) &&
            int.TryParse(resolution.Groups[2].Value, out int height))
        {
            score += Math.Min(180, width * height / 10000);
        }
        if (lower.Contains("preview") || lower.Contains("thumbnail") || lower.Contains("sprite")) score -= 200;
        return score;
    }

    private static bool LooksLikeCapturedMediaUrl(string url, string resourceType = "", string mimeType = "")
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
        if (uri.IsLoopback || IsPrivateNetworkHost(uri.Host)) return false;

        string lower = url.ToLowerInvariant();
        if (IsLikelyAdvertisingMediaUrl(lower)) return false;

        string lowerType = resourceType.ToLowerInvariant();
        string lowerMime = mimeType.ToLowerInvariant();
        bool mediaResponse = lowerType == "media" || lowerMime.StartsWith("video/");
        bool manifestResponse = lowerMime.Contains("mpegurl") ||
                                lowerMime.Contains("dash+xml") ||
                                lowerMime.Contains("application/mp4");

        return lower.Contains(".m3u8") ||
               lower.Contains(".mpd") ||
               lower.Contains(".mp4") && (mediaResponse ||
                                           lower.Contains("video.twimg.com") ||
                                           lower.Contains("scontent") ||
                                           lower.Contains("fbcdn") ||
                                           lower.Contains("cdninstagram.com") ||
                                           lower.Contains("kwimgs.com") ||
                                           lower.Contains("yximgs.com") ||
                                           lower.Contains("wskwai.com")) ||
               mediaResponse ||
               manifestResponse ||
               lower.Contains("sooplive") && (lower.Contains("manifest") || lower.Contains(".m3u8")) ||
               lower.Contains("pstatic.net") && (lower.Contains(".m3u8") || lower.Contains(".ts"));
    }

    private static bool IsAnilifeManifestCaptureUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               uri.Host.Equals("api.gcdn.app", StringComparison.OrdinalIgnoreCase) &&
               uri.AbsolutePath.Contains("/v1/manifest/a/", StringComparison.OrdinalIgnoreCase) &&
               uri.AbsolutePath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLikelyAdvertisingMediaUrl(string lowerUrl)
    {
        return lowerUrl.Contains("doubleclick.net") ||
               lowerUrl.Contains("googlesyndication.com") ||
               lowerUrl.Contains("googleadservices.com") ||
               lowerUrl.Contains("/preroll/") ||
               lowerUrl.Contains("/pre-roll/") ||
               lowerUrl.Contains("/advert/") ||
               lowerUrl.Contains("/advertisement/") ||
               lowerUrl.Contains("vast.xml");
    }

    private static bool IsPrivateNetworkHost(string host)
    {
        if (!System.Net.IPAddress.TryParse(host, out var address)) return false;
        byte[] bytes = address.GetAddressBytes();
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return bytes[0] == 10 ||
                   bytes[0] == 127 ||
                   bytes[0] == 169 && bytes[1] == 254 ||
                   bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31 ||
                   bytes[0] == 192 && bytes[1] == 168;
        }

        return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal;
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
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return false;
        return IsKnownLoginCookieHost(uri.Host);
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


    private async void InstaLoginWatcher(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
    {
        if (webViewX.CoreWebView2 == null) return;
        string currentUrl = webViewX.CoreWebView2.Source;
        

        if (currentUrl.Contains("instagram.com") && !currentUrl.Contains("/accounts/login"))
        {
            bool hasLoginSession = false;
            for (int attempt = 0; attempt < 3 && !hasLoginSession; attempt++)
            {
                hasLoginSession = await HasInstagramSessionCookieAsync();
                if (!hasLoginSession) await Task.Delay(400);
            }

            if (!hasLoginSession)
            {
                if (lblXStatus != null)
                    lblXStatus.Text = "Instagram 로그인 완료를 확인 중입니다. 홈 화면이 표시될 때까지 기다려 주세요.";
                return;
            }

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
                ShowCenteredMessage(
                    "Instagram 로그인 성공!\n\n이제 Instagram 영상 주소를 넣고 [다운로드]를 누르면 됩니다.\n\n'Instagram 로그인됨' 상태를 해제하면 프로그램 내부의 Instagram 로그인 쿠키가 삭제되어 로그아웃됩니다. Chrome이나 Edge의 로그인 상태에는 영향을 주지 않습니다.",
                    "성공",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                this.PerformLayout();
                this.Refresh();
            }));
        }
    }

    private bool _isInstaLoggedIn = false;

    private async Task<bool> HasInstagramSessionCookieAsync()
    {
        try
        {
            if (webViewX.CoreWebView2 == null) return false;
            var cookies = await webViewX.CoreWebView2.CookieManager.GetCookiesAsync("https://www.instagram.com/");
            return cookies.Any(cookie =>
                cookie.Name.Equals("sessionid", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(cookie.Value));
        }
        catch
        {
            return false;
        }
    }

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
        _activeLoginBookmarkGroup = string.Empty;
        RefreshLoginFolderApps();

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
        ExitLoginFolderEditMode();

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
            Visible = false,
            AllowDrop = true
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
            BorderThickness = 1,
            AllowDrop = true
        };
        foreach (Control dropTarget in new Control[] { panelLoginFolderOverlay, panelLoginFolder })
        {
            dropTarget.DragEnter += LoginFolderOuter_DragEnter;
            dropTarget.DragOver += LoginFolderOuter_DragOver;
            dropTarget.DragLeave += LoginFolderOuter_DragLeave;
            dropTarget.DragDrop += LoginFolderOuter_DragDrop;
        }

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
            Text = "← 뒤로",
            Size = new Size(78, 34),
            Location = new Point(20, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            BorderRadius = 15,
            BackColor = Color.FromArgb(226, 232, 240),
            ForeColor = Color.FromArgb(71, 85, 105),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            UseVisualStyleBackColor = false
        };
        btnLoginFolderClose.FlatAppearance.BorderSize = 0;
        btnLoginFolderClose.Click += LoginFolderClose_Click;
        foreach (Control dropTarget in new Control[] { lblLoginFolderTitle, lblLoginFolderHint, btnLoginFolderClose })
        {
            dropTarget.AllowDrop = true;
            dropTarget.DragEnter += LoginFolderOuter_DragEnter;
            dropTarget.DragOver += LoginFolderOuter_DragOver;
            dropTarget.DragLeave += LoginFolderOuter_DragLeave;
            dropTarget.DragDrop += LoginFolderOuter_DragDrop;
        }

        panelLoginFolderApps = new FlowLayoutPanel
        {
            Parent = panelLoginFolder,
            Location = new Point(25, 106),
            Size = new Size(420, 238),
            BackColor = Color.FromArgb(250, 252, 255),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true,
            AllowDrop = true,
            Padding = Padding.Empty
        };
        panelLoginFolderApps.Click += (s, e) => ExitLoginFolderEditMode();
        panelLoginFolderApps.DragEnter += LoginFolderApps_DragEnter;
        panelLoginFolderApps.DragOver += LoginFolderApps_DragOver;
        panelLoginFolderApps.DragLeave += LoginFolderApps_DragLeave;
        panelLoginFolderApps.DragDrop += LoginFolderApps_DragDrop;

        RefreshLoginFolderApps();

        _loginFolderAnimationTimer = new System.Windows.Forms.Timer
        {
            Interval = 12
        };
        _loginFolderAnimationTimer.Tick += LoginFolderAnimationTimer_Tick;

        _loginFolderLongPressTimer = new System.Windows.Forms.Timer { Interval = 550 };
        _loginFolderLongPressTimer.Tick += LoginFolderLongPressTimer_Tick;
        _loginFolderWiggleTimer = new System.Windows.Forms.Timer { Interval = 30 };
        _loginFolderWiggleTimer.Tick += LoginFolderWiggleTimer_Tick;

        var doubleBufferProp = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        doubleBufferProp?.SetValue(panelLoginFolderOverlay, true, null);
        doubleBufferProp?.SetValue(panelLoginFolder, true, null);
        doubleBufferProp?.SetValue(panelLoginFolderApps, true, null);

        PositionLoginFolder();
    }

    private List<LoginFolderDisplayItem> GetLoginFolderDisplayItems()
    {
        SettingsManager.Settings.LoginBrowserBookmarks ??= new List<LoginBrowserBookmark>();
        SettingsManager.Settings.LoginBrowserBuiltInAppLayouts ??= new List<LoginBrowserBuiltInAppLayout>();
        bool settingsChanged = false;

        foreach (LoginFolderBuiltInDefinition definition in LoginFolderBuiltInApps)
        {
            if (SettingsManager.Settings.LoginBrowserBuiltInAppLayouts.Any(item =>
                    string.Equals(item.AppId, definition.AppId, StringComparison.OrdinalIgnoreCase))) continue;

            SettingsManager.Settings.LoginBrowserBuiltInAppLayouts.Add(new LoginBrowserBuiltInAppLayout
            {
                AppId = definition.AppId
            });
            settingsChanged = true;
        }

        if (SettingsManager.Settings.LoginBrowserAppOrderVersion < 1)
        {
            List<int> existingOrderSlots = SettingsManager.Settings.LoginBrowserBuiltInAppLayouts
                .Where(layout => LoginFolderBuiltInApps.Any(definition =>
                    string.Equals(definition.AppId, layout.AppId, StringComparison.OrdinalIgnoreCase)))
                .Where(layout => layout.SortOrder != int.MaxValue)
                .Select(layout => layout.SortOrder)
                .OrderBy(order => order)
                .ToList();
            if (existingOrderSlots.Count == LoginFolderBuiltInApps.Length)
            {
                for (int index = 0; index < LoginFolderBuiltInApps.Length; index++)
                {
                    LoginBrowserBuiltInAppLayout? layout = SettingsManager.Settings.LoginBrowserBuiltInAppLayouts.FirstOrDefault(item =>
                        string.Equals(item.AppId, LoginFolderBuiltInApps[index].AppId, StringComparison.OrdinalIgnoreCase));
                    if (layout != null) layout.SortOrder = existingOrderSlots[index];
                }
            }

            SettingsManager.Settings.LoginBrowserAppOrderVersion = 1;
            settingsChanged = true;
        }

        var items = new List<LoginFolderDisplayItem>();
        foreach (LoginFolderBuiltInDefinition definition in LoginFolderBuiltInApps)
        {
            LoginBrowserBuiltInAppLayout? layout = SettingsManager.Settings.LoginBrowserBuiltInAppLayouts.FirstOrDefault(item =>
                string.Equals(item.AppId, definition.AppId, StringComparison.OrdinalIgnoreCase));
            if (layout != null)
                items.Add(new LoginFolderDisplayItem { BuiltInDefinition = definition, BuiltInLayout = layout });
        }

        items.AddRange(SettingsManager.Settings.LoginBrowserBookmarks
            .Where(item => IsBookmarkableBrowserUrl(item.Url))
            .Take(100)
            .Select(bookmark => new LoginFolderDisplayItem { Bookmark = bookmark }));

        int nextOrder = items.Where(item => item.SortOrder != int.MaxValue)
            .Select(item => item.SortOrder)
            .DefaultIfEmpty(-10)
            .Max() + 10;
        foreach (LoginFolderDisplayItem item in items.Where(item => item.SortOrder == int.MaxValue))
        {
            item.SortOrder = nextOrder;
            nextOrder += 10;
            settingsChanged = true;
        }

        if (settingsChanged) SettingsManager.Save();
        return items.OrderBy(item => item.SortOrder).ThenBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static bool IsSameLoginFolderItem(LoginFolderDisplayItem? first, LoginFolderDisplayItem? second)
    {
        if (first == null || second == null) return false;
        if (first.Bookmark != null || second.Bookmark != null)
            return ReferenceEquals(first.Bookmark, second.Bookmark);
        return ReferenceEquals(first.BuiltInLayout, second.BuiltInLayout);
    }

    private void RefreshLoginFolderApps()
    {
        if (panelLoginFolderApps == null) return;
        ExitLoginFolderEditMode();

        foreach (Control control in panelLoginFolderApps.Controls.Cast<Control>().ToArray())
        {
            control.ContextMenuStrip?.Dispose();
            if (control is Panel tile)
            {
                foreach (PictureBox picture in tile.Controls.OfType<PictureBox>())
                    picture.Image?.Dispose();
            }
            control.Dispose();
        }
        panelLoginFolderApps.Controls.Clear();

        List<LoginFolderDisplayItem> items = GetLoginFolderDisplayItems();
        if (DissolveSingleItemLoginFolderGroups(items))
            SettingsManager.Save();

        if (string.IsNullOrWhiteSpace(_activeLoginBookmarkGroup))
        {
            if (lblLoginFolderTitle != null) lblLoginFolderTitle.Text = "로그인 브라우저";
            if (lblLoginFolderHint != null)
                lblLoginFolderHint.Text = "사이트를 선택하거나 즐겨찾기를 길게 눌러 순서를 바꿀 수 있습니다.";

            foreach (IGrouping<string, LoginFolderDisplayItem> group in items
                         .Where(item => !string.IsNullOrWhiteSpace(item.GroupName))
                         .GroupBy(item => item.GroupName.Trim(), StringComparer.OrdinalIgnoreCase)
                         .OrderBy(group => group.Min(item => item.SortOrder)))
            {
                AddLoginFolderGroup(group.Key, group.ToList());
            }

            foreach (LoginFolderDisplayItem item in items.Where(item => string.IsNullOrWhiteSpace(item.GroupName)))
                AddLoginFolderDisplayItem(item);
        }
        else
        {
            if (lblLoginFolderTitle != null) lblLoginFolderTitle.Text = _activeLoginBookmarkGroup;
            if (lblLoginFolderHint != null)
                lblLoginFolderHint.Text = "즐겨찾기를 누르면 열리고, 길게 누르면 순서를 바꿀 수 있습니다.";

            foreach (LoginFolderDisplayItem item in items.Where(item =>
                         string.Equals(item.GroupName.Trim(), _activeLoginBookmarkGroup, StringComparison.OrdinalIgnoreCase)))
            {
                AddLoginFolderDisplayItem(item);
            }
        }

        int rows = Math.Max(1, (panelLoginFolderApps.Controls.Count + 2) / 3);
        panelLoginFolderApps.AutoScrollMinSize = new Size(0, rows * 112);
        panelLoginFolderApps.HorizontalScroll.Enabled = false;
        panelLoginFolderApps.HorizontalScroll.Visible = false;
        UpdateLoginFolderHeader(showContent: true);
    }

    private bool DissolveSingleItemLoginFolderGroups(List<LoginFolderDisplayItem> items)
    {
        HashSet<string> groupsToDissolve = items
            .Where(item => !string.IsNullOrWhiteSpace(item.GroupName))
            .GroupBy(item => item.GroupName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() <= 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (groupsToDissolve.Count == 0) return false;

        foreach (LoginFolderDisplayItem item in items.Where(item =>
                     groupsToDissolve.Contains(item.GroupName.Trim())))
        {
            item.GroupName = string.Empty;
        }

        if (groupsToDissolve.Contains(_activeLoginBookmarkGroup.Trim()))
            _activeLoginBookmarkGroup = string.Empty;

        return true;
    }

    private void UpdateLoginFolderHeader(bool showContent)
    {
        bool isGroupOpen = !string.IsNullOrWhiteSpace(_activeLoginBookmarkGroup);

        if (lblLoginFolderTitle != null)
        {
            lblLoginFolderTitle.Location = isGroupOpen ? new Point(112, 22) : new Point(24, 22);
            lblLoginFolderTitle.Size = isGroupOpen ? new Size(236, 30) : new Size(280, 30);
        }

        if (btnLoginFolderClose == null) return;

        btnLoginFolderClose.Text = "← 뒤로";
        btnLoginFolderClose.Location = new Point(20, 20);
        btnLoginFolderClose.Visible = showContent && isGroupOpen;
        btnLoginFolderClose.Enabled = btnLoginFolderClose.Visible;
        if (btnLoginFolderClose.Visible) btnLoginFolderClose.BringToFront();
    }

    private void SetLoginFolderContentVisible(bool visible)
    {
        if (lblLoginFolderTitle != null) lblLoginFolderTitle.Visible = visible;
        if (lblLoginFolderHint != null) lblLoginFolderHint.Visible = visible;
        UpdateLoginFolderHeader(visible);
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

    private void AddLoginFolderDisplayItem(LoginFolderDisplayItem item)
    {
        if (item.IsBuiltIn)
            AddLoginFolderBuiltInApp(item);
        else if (item.Bookmark != null)
            AddLoginFolderBookmark(item);
    }

    private void AddLoginFolderBuiltInApp(LoginFolderDisplayItem item)
    {
        LoginFolderBuiltInDefinition? definition = item.BuiltInDefinition;
        if (definition == null) return;

        EventHandler openBuiltInApp = async (s, e) =>
        {
            if (ConsumeSuppressedLoginFolderOpen()) return;
            await OpenLoginSiteFromFolderAsync(definition.Title);
        };
        Panel? tile = AddLoginFolderApp(
            definition.Title,
            definition.IconText,
            definition.IconColor,
            definition.IconFileName,
            customOpenHandler: openBuiltInApp);
        if (tile == null) return;

        tile.Tag = new LoginFolderTileInfo { Item = item };
        tile.ContextMenuStrip = BuildBuiltInAppContextMenu(item);
        ConfigureLoginFolderEditableTile(tile);
    }

    private void AddLoginFolderBookmark(LoginFolderDisplayItem item)
    {
        LoginBrowserBookmark? bookmark = item.Bookmark;
        if (bookmark == null) return;
        string trimmedTitle = bookmark.Title?.Trim() ?? string.Empty;
        string fallbackText = trimmedTitle.Length > 0 && char.IsLetterOrDigit(trimmedTitle[0])
            ? char.ToUpperInvariant(trimmedTitle[0]).ToString()
            : "★";
        EventHandler openBookmark = async (s, e) =>
        {
            if (ConsumeSuppressedLoginFolderOpen()) return;
            await OpenLoginBrowserBookmarkAsync(bookmark);
        };

        Panel? tile = AddLoginFolderApp(
            GetBookmarkDisplayTitle(bookmark),
            fallbackText,
            Color.FromArgb(51, 65, 85),
            customIconPath: bookmark.IconPath,
            customOpenHandler: openBookmark);
        if (tile == null) return;

        tile.Tag = new LoginFolderTileInfo { Item = item };
        tile.ContextMenuStrip = BuildBookmarkContextMenu(bookmark);
        AddLoginFolderBookmarkDeleteButton(tile, bookmark);
        ConfigureLoginFolderEditableTile(tile);
    }

    private void AddLoginFolderGroup(string groupName, List<LoginFolderDisplayItem> items)
    {
        EventHandler openGroup = (s, e) =>
        {
            if (ConsumeSuppressedLoginFolderOpen()) return;
            _activeLoginBookmarkGroup = groupName;
            RefreshLoginFolderApps();
        };

        Panel? tile = AddLoginFolderApp(
            groupName.Length <= 14 ? groupName : groupName[..13] + "…",
            "▦",
            Color.FromArgb(71, 85, 105),
            customOpenHandler: openGroup,
            customIconImage: CreateBookmarkGroupIcon(items));
        if (tile == null) return;

        tile.Tag = new LoginFolderTileInfo { GroupName = groupName, IsGroup = true };
        tile.ContextMenuStrip = BuildBookmarkGroupContextMenu(groupName);
        ConfigureLoginFolderEditableTile(tile);
    }

    private static Bitmap CreateBookmarkGroupIcon(IReadOnlyList<LoginFolderDisplayItem> items)
    {
        const int size = 64;
        var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using GraphicsPath path = CreateRoundedRectanglePath(new RectangleF(1.5F, 1.5F, 61F, 61F), 15F);
        using (var brush = new SolidBrush(Color.FromArgb(226, 232, 240))) graphics.FillPath(brush, path);
        using (var pen = new Pen(Color.FromArgb(148, 163, 184), 1F)) graphics.DrawPath(pen, path);

        Rectangle[] slots =
        {
            new Rectangle(10, 10, 20, 20), new Rectangle(34, 10, 20, 20),
            new Rectangle(10, 34, 20, 20), new Rectangle(34, 34, 20, 20)
        };
        Color[] colors =
        {
            Color.FromArgb(14, 165, 233), Color.FromArgb(99, 102, 241),
            Color.FromArgb(20, 184, 166), Color.FromArgb(244, 63, 94)
        };
        using var font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Pixel);

        for (int index = 0; index < slots.Length; index++)
        {
            Rectangle slot = slots[index];
            using GraphicsPath slotPath = CreateRoundedRectanglePath(slot, 5F);
            using var brush = new SolidBrush(colors[index]);
            graphics.FillPath(brush, slotPath);
            if (index >= items.Count) continue;

            string title = items[index].Title.Trim();
            string letter = title.Length > 0 && char.IsLetterOrDigit(title[0])
                ? char.ToUpperInvariant(title[0]).ToString()
                : "•";
            TextRenderer.DrawText(
                graphics,
                letter,
                font,
                slot,
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        return bitmap;
    }

    private void AddLoginFolderBookmarkDeleteButton(Panel tile, LoginBrowserBookmark bookmark)
    {
        var deleteButton = new RoundButton
        {
            Parent = tile,
            Name = "LoginFolderDeleteButton",
            Text = "X",
            Size = new Size(24, 24),
            Location = new Point(tile.Width - 28, 0),
            BorderRadius = 12,
            BackColor = Color.FromArgb(239, 68, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            Visible = _loginFolderEditMode
        };
        deleteButton.FlatAppearance.BorderSize = 0;
        deleteButton.Click += (s, e) => RemoveLoginBrowserBookmark(bookmark);
        deleteButton.BringToFront();
    }

    private ContextMenuStrip BuildBookmarkContextMenu(LoginBrowserBookmark bookmark)
    {
        var menu = new ContextMenuStrip { ShowImageMargin = false };
        menu.Items.Add("새 그룹 만들기...", null, (s, e) => CreateBookmarkGroupFor(bookmark));

        var moveMenu = new ToolStripMenuItem("그룹으로 이동");
        foreach (string groupName in GetLoginFolderGroupNames())
        {
            string targetGroup = groupName;
            moveMenu.DropDownItems.Add(targetGroup, null, (s, e) => MoveBookmarkToGroup(bookmark, targetGroup));
        }
        if (!string.IsNullOrWhiteSpace(bookmark.GroupName))
            moveMenu.DropDownItems.Add("그룹에서 빼기", null, (s, e) => MoveBookmarkToGroup(bookmark, string.Empty));
        moveMenu.Enabled = moveMenu.DropDownItems.Count > 0;
        menu.Items.Add(moveMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("즐겨찾기 삭제", null, (s, e) => RemoveLoginBrowserBookmark(bookmark));
        return menu;
    }

    private ContextMenuStrip BuildBuiltInAppContextMenu(LoginFolderDisplayItem item)
    {
        var menu = new ContextMenuStrip { ShowImageMargin = false };
        menu.Items.Add("새 그룹 만들기...", null, (s, e) => CreateLoginFolderGroupFor(item));

        var moveMenu = new ToolStripMenuItem("그룹으로 이동");
        foreach (string groupName in GetLoginFolderGroupNames())
        {
            string targetGroup = groupName;
            moveMenu.DropDownItems.Add(targetGroup, null, (s, e) => MoveLoginFolderItemToGroup(item, targetGroup));
        }
        if (!string.IsNullOrWhiteSpace(item.GroupName))
            moveMenu.DropDownItems.Add("그룹에서 빼기", null, (s, e) => MoveLoginFolderItemToGroup(item, string.Empty));
        moveMenu.Enabled = moveMenu.DropDownItems.Count > 0;
        menu.Items.Add(moveMenu);
        return menu;
    }

    private List<string> GetLoginFolderGroupNames()
    {
        return GetLoginFolderDisplayItems()
            .Select(item => item.GroupName.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private ContextMenuStrip BuildBookmarkGroupContextMenu(string groupName)
    {
        var menu = new ContextMenuStrip { ShowImageMargin = false };
        menu.Items.Add("그룹 이름 변경...", null, (s, e) => RenameBookmarkGroup(groupName));
        menu.Items.Add("그룹 해제", null, (s, e) => UngroupBookmarks(groupName));
        return menu;
    }

    private bool ConsumeSuppressedLoginFolderOpen()
    {
        if (!_suppressLoginFolderOpen) return false;
        _suppressLoginFolderOpen = false;
        return true;
    }

    private void CreateBookmarkGroupFor(LoginBrowserBookmark bookmark)
    {
        string? groupName = PromptForBookmarkGroupName("새 그룹", "그룹 이름을 입력하세요.");
        if (string.IsNullOrWhiteSpace(groupName)) return;
        MoveBookmarkToGroup(bookmark, groupName.Trim());
    }

    private void CreateLoginFolderGroupFor(LoginFolderDisplayItem item)
    {
        string? groupName = PromptForBookmarkGroupName("새 그룹", "그룹 이름을 입력하세요.");
        if (string.IsNullOrWhiteSpace(groupName)) return;
        MoveLoginFolderItemToGroup(item, groupName.Trim());
    }

    private void RenameBookmarkGroup(string oldName)
    {
        string? newName = PromptForBookmarkGroupName(oldName, "새 그룹 이름을 입력하세요.");
        if (string.IsNullOrWhiteSpace(newName) || string.Equals(oldName, newName.Trim(), StringComparison.OrdinalIgnoreCase)) return;

        SettingsManager.Settings.LoginBrowserBookmarks ??= new List<LoginBrowserBookmark>();
        foreach (LoginBrowserBookmark bookmark in SettingsManager.Settings.LoginBrowserBookmarks.Where(item =>
                     string.Equals(item.GroupName?.Trim(), oldName, StringComparison.OrdinalIgnoreCase)))
        {
            bookmark.GroupName = newName.Trim();
        }
        SettingsManager.Settings.LoginBrowserBuiltInAppLayouts ??= new List<LoginBrowserBuiltInAppLayout>();
        foreach (LoginBrowserBuiltInAppLayout layout in SettingsManager.Settings.LoginBrowserBuiltInAppLayouts.Where(item =>
                     string.Equals(item.GroupName?.Trim(), oldName, StringComparison.OrdinalIgnoreCase)))
        {
            layout.GroupName = newName.Trim();
        }

        if (string.Equals(_activeLoginBookmarkGroup, oldName, StringComparison.OrdinalIgnoreCase))
            _activeLoginBookmarkGroup = newName.Trim();
        SettingsManager.Save();
        RefreshLoginFolderApps();
    }

    private void UngroupBookmarks(string groupName)
    {
        SettingsManager.Settings.LoginBrowserBookmarks ??= new List<LoginBrowserBookmark>();
        foreach (LoginBrowserBookmark bookmark in SettingsManager.Settings.LoginBrowserBookmarks.Where(item =>
                     string.Equals(item.GroupName?.Trim(), groupName, StringComparison.OrdinalIgnoreCase)))
        {
            bookmark.GroupName = string.Empty;
        }
        SettingsManager.Settings.LoginBrowserBuiltInAppLayouts ??= new List<LoginBrowserBuiltInAppLayout>();
        foreach (LoginBrowserBuiltInAppLayout layout in SettingsManager.Settings.LoginBrowserBuiltInAppLayouts.Where(item =>
                     string.Equals(item.GroupName?.Trim(), groupName, StringComparison.OrdinalIgnoreCase)))
        {
            layout.GroupName = string.Empty;
        }
        _activeLoginBookmarkGroup = string.Empty;
        SettingsManager.Save();
        RefreshLoginFolderApps();
    }

    private void MoveBookmarkToGroup(LoginBrowserBookmark bookmark, string groupName)
    {
        bookmark.GroupName = groupName.Trim();
        SettingsManager.Save();
        RefreshLoginFolderApps();
    }

    private void MoveLoginFolderItemToGroup(LoginFolderDisplayItem item, string groupName)
    {
        item.GroupName = groupName.Trim();
        SettingsManager.Save();
        RefreshLoginFolderApps();
    }

    private void RemoveLoginBrowserBookmark(LoginBrowserBookmark bookmark)
    {
        SettingsManager.Settings.LoginBrowserBookmarks ??= new List<LoginBrowserBookmark>();
        if (!SettingsManager.Settings.LoginBrowserBookmarks.Remove(bookmark)) return;
        DeleteUnusedBookmarkIcon(bookmark.IconPath, SettingsManager.Settings.LoginBrowserBookmarks);
        SettingsManager.Save();
        RefreshLoginFolderApps();
    }

    private string? PromptForBookmarkGroupName(string initialValue, string message)
    {
        using var dialog = new Form
        {
            Text = "즐겨찾기 그룹",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            ClientSize = new Size(340, 145),
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9F)
        };
        var label = new Label
        {
            Parent = dialog,
            Text = message,
            Location = new Point(20, 18),
            Size = new Size(300, 24)
        };
        var input = new TextBox
        {
            Parent = dialog,
            Text = initialValue,
            Location = new Point(20, 48),
            Size = new Size(300, 25),
            MaxLength = 24
        };
        var ok = new Button
        {
            Parent = dialog,
            Text = "확인",
            DialogResult = DialogResult.OK,
            Location = new Point(164, 92),
            Size = new Size(75, 32)
        };
        var cancel = new Button
        {
            Parent = dialog,
            Text = "취소",
            DialogResult = DialogResult.Cancel,
            Location = new Point(245, 92),
            Size = new Size(75, 32)
        };
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;
        dialog.Shown += (s, e) =>
        {
            input.Focus();
            input.SelectAll();
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? input.Text.Trim() : null;
    }

    private void LoginFolderClose_Click(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_activeLoginBookmarkGroup))
        {
            _activeLoginBookmarkGroup = string.Empty;
            RefreshLoginFolderApps();
            return;
        }

        HideLoginFolder();
    }

    private void ConfigureLoginFolderEditableTile(Panel tile)
    {
        foreach (Control control in tile.Controls.Cast<Control>().Prepend(tile))
        {
            control.MouseDown += (s, e) => LoginFolderTile_MouseDown(tile, e);
            control.MouseMove += (s, e) => LoginFolderTile_MouseMove(tile, e);
            control.MouseUp += (s, e) => LoginFolderTile_MouseUp(tile, e);
            if (tile.ContextMenuStrip != null) control.ContextMenuStrip = tile.ContextMenuStrip;
        }
    }

    private void LoginFolderTile_MouseDown(Panel tile, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _pressedLoginFolderTile = tile;
        _loginFolderPressScreenPoint = Control.MousePosition;
        _loginFolderLongPressTimer?.Stop();
        _loginFolderLongPressTimer?.Start();
    }

    private void LoginFolderTile_MouseMove(Panel tile, MouseEventArgs e)
    {
        if (!_loginFolderEditMode || _loginFolderDragging || _pressedLoginFolderTile != tile ||
            (Control.MouseButtons & MouseButtons.Left) == 0) return;
        if (tile.Tag is not LoginFolderTileInfo { Item: not null } tileInfo) return;

        Point current = Control.MousePosition;
        if (Math.Abs(current.X - _loginFolderPressScreenPoint.X) < 6 &&
            Math.Abs(current.Y - _loginFolderPressScreenPoint.Y) < 6) return;

        _loginFolderLongPressTimer?.Stop();
        _loginFolderDragging = true;
        _suppressLoginFolderOpen = true;
        try
        {
            tile.DoDragDrop(tileInfo.Item, DragDropEffects.Move);
        }
        finally
        {
            ClearLoginFolderDropPreview();
            _loginFolderDragging = false;
            _pressedLoginFolderTile = null;
        }
    }

    private void LoginFolderTile_MouseUp(Panel tile, MouseEventArgs e)
    {
        _loginFolderLongPressTimer?.Stop();
        if (_loginFolderEditMode)
        {
            _suppressLoginFolderOpen = true;
            BeginInvoke(new Action(() => _suppressLoginFolderOpen = false));
        }
        _pressedLoginFolderTile = null;
    }

    private void LoginFolderLongPressTimer_Tick(object? sender, EventArgs e)
    {
        _loginFolderLongPressTimer?.Stop();
        if (_pressedLoginFolderTile == null || (Control.MouseButtons & MouseButtons.Left) == 0) return;

        _loginFolderEditMode = true;
        _suppressLoginFolderOpen = true;
        _loginFolderWigglePhase = 0;
        SetLoginFolderDeleteButtonsVisible(true);
        _loginFolderWiggleTimer?.Start();
    }

    private void LoginFolderWiggleTimer_Tick(object? sender, EventArgs e)
    {
        if (!_loginFolderEditMode || panelLoginFolderApps == null)
        {
            _loginFolderWiggleTimer?.Stop();
            return;
        }

        _loginFolderWigglePhase = (_loginFolderWigglePhase + 1) % 10000;
        foreach (Panel tile in panelLoginFolderApps.Controls.OfType<Panel>().Where(item =>
                     item.Tag is LoginFolderTileInfo && panelLoginFolderApps.ClientRectangle.IntersectsWith(item.Bounds)))
        {
            Control? icon = tile.Controls.Cast<Control>().FirstOrDefault(item => item.Name == "LoginFolderAppIcon");
            if (icon == null) continue;
            double phase = (_loginFolderWigglePhase * 0.32D) + ((tile.TabIndex & 1) == 0 ? 0D : Math.PI);
            float angle = (float)(Math.Sin(phase) * 1.7D);
            if (icon is WigglePictureBox picture)
            {
                picture.RotationAngle = angle;
                picture.Location = new Point(31, 4);
            }
            else
            {
                icon.Location = new Point(
                    31 + (int)Math.Round(Math.Sin(phase) * 1D),
                    4 + (int)Math.Round(Math.Cos(phase) * 0.7D));
            }
        }
    }

    private void ExitLoginFolderEditMode()
    {
        _loginFolderLongPressTimer?.Stop();
        _loginFolderWiggleTimer?.Stop();
        ClearLoginFolderDropPreview();
        _loginFolderEditMode = false;
        _loginFolderDragging = false;
        _pressedLoginFolderTile = null;
        _suppressLoginFolderOpen = false;

        if (panelLoginFolderApps == null) return;
        SetLoginFolderDeleteButtonsVisible(false);
        foreach (Panel tile in panelLoginFolderApps.Controls.OfType<Panel>())
        {
            Control? icon = tile.Controls.Cast<Control>().FirstOrDefault(item => item.Name == "LoginFolderAppIcon");
            if (icon is WigglePictureBox picture) picture.RotationAngle = 0F;
            if (icon != null) icon.Location = new Point(31, 4);
        }
    }

    private void SetLoginFolderDeleteButtonsVisible(bool visible)
    {
        if (panelLoginFolderApps == null) return;
        foreach (Control button in panelLoginFolderApps.Controls.OfType<Panel>()
                     .SelectMany(tile => tile.Controls.Cast<Control>())
                     .Where(control => control.Name == "LoginFolderDeleteButton"))
        {
            button.Visible = visible;
            if (visible) button.BringToFront();
        }
    }

    private void LoginFolderApps_DragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(typeof(LoginFolderDisplayItem)) == true
            ? DragDropEffects.Move
            : DragDropEffects.None;
    }

    private void LoginFolderOuter_DragEnter(object? sender, DragEventArgs e)
    {
        bool canMoveOut = !string.IsNullOrWhiteSpace(_activeLoginBookmarkGroup) &&
                          e.Data?.GetData(typeof(LoginFolderDisplayItem)) is LoginFolderDisplayItem item &&
                          string.Equals(item.GroupName.Trim(), _activeLoginBookmarkGroup, StringComparison.OrdinalIgnoreCase);
        e.Effect = canMoveOut ? DragDropEffects.Move : DragDropEffects.None;
    }

    private void LoginFolderOuter_DragOver(object? sender, DragEventArgs e)
    {
        LoginFolderOuter_DragEnter(sender, e);
        if (e.Effect == DragDropEffects.None) return;
        ClearLoginFolderInnerDropPreview();
        SetLoginFolderOuterDropPreview(true);
    }

    private void LoginFolderOuter_DragLeave(object? sender, EventArgs e)
    {
        SetLoginFolderOuterDropPreview(false);
    }

    private void LoginFolderOuter_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(typeof(LoginFolderDisplayItem)) is not LoginFolderDisplayItem source ||
            string.IsNullOrWhiteSpace(_activeLoginBookmarkGroup) ||
            !string.Equals(source.GroupName.Trim(), _activeLoginBookmarkGroup, StringComparison.OrdinalIgnoreCase)) return;

        string previousGroup = _activeLoginBookmarkGroup;
        source.GroupName = string.Empty;
        SetLoginFolderOuterDropPreview(false);
        SettingsManager.Save();

        if (!GetLoginFolderDisplayItems().Any(item =>
                string.Equals(item.GroupName.Trim(), previousGroup, StringComparison.OrdinalIgnoreCase)))
        {
            _activeLoginBookmarkGroup = string.Empty;
        }
        RefreshLoginFolderApps();
    }

    private void SetLoginFolderOuterDropPreview(bool visible)
    {
        if (_loginFolderOuterDropPreviewActive == visible) return;
        _loginFolderOuterDropPreviewActive = visible;
        if (btnLoginFolderClose != null)
        {
            btnLoginFolderClose.BackColor = visible ? Color.FromArgb(2, 132, 199) : Color.FromArgb(226, 232, 240);
            btnLoginFolderClose.ForeColor = visible ? Color.White : Color.FromArgb(71, 85, 105);
        }
        if (lblLoginFolderHint != null)
        {
            lblLoginFolderHint.Text = visible
                ? "여기에 놓으면 그룹 밖으로 이동합니다."
                : string.IsNullOrWhiteSpace(_activeLoginBookmarkGroup)
                    ? "사이트를 선택하거나 즐겨찾기를 길게 눌러 순서를 바꿀 수 있습니다."
                    : "앱을 누르면 열리고, 길게 누르면 순서를 바꿀 수 있습니다.";
            lblLoginFolderHint.BackColor = visible ? Color.FromArgb(224, 242, 254) : Color.Transparent;
            lblLoginFolderHint.ForeColor = visible ? Color.FromArgb(3, 105, 161) : Color.FromArgb(100, 116, 139);
            lblLoginFolderHint.TextAlign = visible ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft;
        }
    }

    private void LoginFolderApps_DragOver(object? sender, DragEventArgs e)
    {
        LoginFolderApps_DragEnter(sender, e);
        if (e.Effect == DragDropEffects.None || panelLoginFolderApps == null ||
            e.Data?.GetData(typeof(LoginFolderDisplayItem)) is not LoginFolderDisplayItem source) return;

        Point clientPoint = panelLoginFolderApps.PointToClient(new Point(e.X, e.Y));
        Control? targetControl = panelLoginFolderApps.GetChildAtPoint(clientPoint);
        if (ReferenceEquals(targetControl, _loginFolderDropIndicator)) return;

        LoginFolderTileInfo? targetInfo = targetControl?.Tag as LoginFolderTileInfo;
        if (targetControl == null || targetInfo == null)
        {
            ClearLoginFolderDropHighlight();
            ShowLoginFolderDropIndicator(null, insertAfter: true);
            SetLoginFolderDropPreview(null, _activeLoginBookmarkGroup, insertAfter: true, createsGroup: false, onGroupTile: false);
            return;
        }

        if (targetInfo.IsGroup)
        {
            RemoveLoginFolderDropIndicator();
            HighlightLoginFolderDropTarget((Panel)targetControl);
            SetLoginFolderDropPreview(null, targetInfo.GroupName, insertAfter: false, createsGroup: false, onGroupTile: true);
            return;
        }

        LoginFolderDisplayItem? targetItem = targetInfo.Item;
        if (targetItem == null || IsSameLoginFolderItem(source, targetItem))
        {
            ClearLoginFolderDropPreview();
            return;
        }

        Point targetPoint = targetControl.PointToClient(new Point(e.X, e.Y));
        int groupZoneLeft = (int)Math.Round(targetControl.Width * 0.35D);
        int groupZoneRight = (int)Math.Round(targetControl.Width * 0.65D);
        bool canCreateGroup = string.IsNullOrWhiteSpace(_activeLoginBookmarkGroup) &&
                              string.IsNullOrWhiteSpace(source.GroupName) &&
                              string.IsNullOrWhiteSpace(targetItem.GroupName);
        bool createsGroup = canCreateGroup &&
                            targetPoint.X >= groupZoneLeft && targetPoint.X <= groupZoneRight &&
                            targetPoint.Y >= 0 && targetPoint.Y <= 76;

        if (createsGroup)
        {
            RemoveLoginFolderDropIndicator();
            HighlightLoginFolderDropTarget((Panel)targetControl);
            SetLoginFolderDropPreview(targetItem, string.Empty, insertAfter: false, createsGroup: true, onGroupTile: false);
            return;
        }

        ClearLoginFolderDropHighlight();
        bool insertAfter = targetPoint.X > targetControl.Width / 2;
        ShowLoginFolderDropIndicator(targetControl, insertAfter);
        SetLoginFolderDropPreview(targetItem, targetItem.GroupName.Trim(), insertAfter, createsGroup: false, onGroupTile: false);
    }

    private void LoginFolderApps_DragLeave(object? sender, EventArgs e)
    {
        ClearLoginFolderDropPreview();
    }

    private void SetLoginFolderDropPreview(
        LoginFolderDisplayItem? targetItem,
        string targetGroup,
        bool insertAfter,
        bool createsGroup,
        bool onGroupTile)
    {
        _loginFolderDropTargetItem = targetItem;
        _loginFolderDropTargetGroup = targetGroup;
        _loginFolderDropInsertAfter = insertAfter;
        _loginFolderDropCreatesGroup = createsGroup;
        _loginFolderDropOnGroupTile = onGroupTile;
        _loginFolderDropPreviewActive = true;
    }

    private void ShowLoginFolderDropIndicator(Control? targetControl, bool insertAfter)
    {
        if (panelLoginFolderApps == null) return;
        _loginFolderDropIndicator ??= new LoginFolderDropIndicator
        {
            Size = new Size(126, 104),
            Margin = new Padding(4),
            Cursor = Cursors.SizeAll
        };

        panelLoginFolderApps.SuspendLayout();
        try
        {
            if (_loginFolderDropIndicator.Parent == panelLoginFolderApps)
                panelLoginFolderApps.Controls.Remove(_loginFolderDropIndicator);

            int insertIndex = targetControl == null
                ? panelLoginFolderApps.Controls.Count
                : panelLoginFolderApps.Controls.GetChildIndex(targetControl) + (insertAfter ? 1 : 0);
            panelLoginFolderApps.Controls.Add(_loginFolderDropIndicator);
            insertIndex = Math.Clamp(insertIndex, 0, panelLoginFolderApps.Controls.Count - 1);
            panelLoginFolderApps.Controls.SetChildIndex(_loginFolderDropIndicator, insertIndex);
        }
        finally
        {
            panelLoginFolderApps.ResumeLayout(true);
        }
    }

    private void RemoveLoginFolderDropIndicator()
    {
        if (panelLoginFolderApps == null || _loginFolderDropIndicator?.Parent != panelLoginFolderApps) return;
        panelLoginFolderApps.Controls.Remove(_loginFolderDropIndicator);
        panelLoginFolderApps.PerformLayout();
    }

    private void HighlightLoginFolderDropTarget(Panel tile)
    {
        if (ReferenceEquals(_loginFolderDropHighlightedTile, tile)) return;
        ClearLoginFolderDropHighlight();
        _loginFolderDropHighlightedTile = tile;
        _loginFolderDropHighlightedOriginalColor = tile.BackColor;
        tile.BackColor = Color.FromArgb(224, 242, 254);
    }

    private void ClearLoginFolderDropHighlight()
    {
        if (_loginFolderDropHighlightedTile != null && !_loginFolderDropHighlightedTile.IsDisposed)
            _loginFolderDropHighlightedTile.BackColor = _loginFolderDropHighlightedOriginalColor;
        _loginFolderDropHighlightedTile = null;
    }

    private void ClearLoginFolderDropPreview()
    {
        ClearLoginFolderInnerDropPreview();
        SetLoginFolderOuterDropPreview(false);
    }

    private void ClearLoginFolderInnerDropPreview()
    {
        RemoveLoginFolderDropIndicator();
        ClearLoginFolderDropHighlight();
        _loginFolderDropTargetItem = null;
        _loginFolderDropTargetGroup = string.Empty;
        _loginFolderDropInsertAfter = false;
        _loginFolderDropCreatesGroup = false;
        _loginFolderDropOnGroupTile = false;
        _loginFolderDropPreviewActive = false;
    }

    private void LoginFolderApps_DragDrop(object? sender, DragEventArgs e)
    {
        if (panelLoginFolderApps == null || e.Data?.GetData(typeof(LoginFolderDisplayItem)) is not LoginFolderDisplayItem source) return;

        bool previewActive = _loginFolderDropPreviewActive;
        LoginFolderDisplayItem? targetItem = _loginFolderDropTargetItem;
        string targetGroup = _loginFolderDropTargetGroup;
        bool insertAfter = _loginFolderDropInsertAfter;
        bool createsGroup = _loginFolderDropCreatesGroup;
        bool onGroupTile = _loginFolderDropOnGroupTile;
        ClearLoginFolderDropPreview();
        if (!previewActive) return;

        if (onGroupTile)
        {
            ReorderLoginFolderItem(source, null, targetGroup);
        }
        else if (createsGroup && targetItem != null)
        {
            string? groupName = PromptForBookmarkGroupName("새 그룹", "두 앱을 묶을 그룹 이름을 입력하세요.");
            if (!string.IsNullOrWhiteSpace(groupName))
            {
                source.GroupName = groupName.Trim();
                targetItem.GroupName = groupName.Trim();
                SettingsManager.Save();
                RefreshLoginFolderApps();
            }
            return;
        }
        else
        {
            ReorderLoginFolderItem(source, targetItem, targetGroup, insertAfter);
        }

        SettingsManager.Save();
        RefreshLoginFolderApps();
    }

    private void ReorderLoginFolderItem(
        LoginFolderDisplayItem source,
        LoginFolderDisplayItem? target,
        string targetGroup,
        bool insertAfterTarget = false)
    {
        List<LoginFolderDisplayItem> orderedItems = GetLoginFolderDisplayItems();
        LoginFolderDisplayItem? storedSource = orderedItems.FirstOrDefault(item => IsSameLoginFolderItem(item, source));
        if (storedSource == null) return;

        storedSource.GroupName = targetGroup.Trim();
        orderedItems.Remove(storedSource);

        int insertIndex = target == null
            ? orderedItems.FindLastIndex(item => string.Equals(item.GroupName.Trim(), targetGroup.Trim(), StringComparison.OrdinalIgnoreCase)) + 1
            : orderedItems.FindIndex(item => IsSameLoginFolderItem(item, target));
        if (insertAfterTarget && insertIndex >= 0) insertIndex++;
        if (insertIndex < 0 || insertIndex > orderedItems.Count) insertIndex = orderedItems.Count;
        orderedItems.Insert(insertIndex, storedSource);

        for (int index = 0; index < orderedItems.Count; index++)
            orderedItems[index].SortOrder = index * 10;
    }

    private Panel? AddLoginFolderApp(
        string siteName,
        string iconText,
        Color iconColor,
        string? iconFileName = null,
        string? customIconPath = null,
        EventHandler? customOpenHandler = null,
        Image? customIconImage = null)
    {
        if (panelLoginFolderApps == null) return null;

        var appTile = new Panel
        {
            Size = new Size(126, 104),
            Margin = new Padding(4),
            BackColor = Color.FromArgb(250, 252, 255),
            Cursor = Cursors.Hand
        };

        Control appIcon;
        Image? iconImage = customIconImage ?? (!string.IsNullOrWhiteSpace(customIconPath)
            ? LoadBookmarkIconImage(customIconPath, iconText)
            : string.IsNullOrWhiteSpace(iconFileName) ? null : LoadLoginIconImage(iconFileName));
        if (iconImage == null && customOpenHandler != null)
            iconImage = CreateBookmarkIcon(null, iconText);
        if (iconImage != null)
        {
            appIcon = new WigglePictureBox
            {
                Parent = appTile,
                Image = iconImage,
                Size = new Size(64, 64),
                Location = new Point(31, 4),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            appIcon.Name = "LoginFolderAppIcon";
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
            fallbackIcon.Name = "LoginFolderAppIcon";
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

        EventHandler openHandler = customOpenHandler ?? (async (s, e) => await OpenLoginSiteFromFolderAsync(siteName));
        appTile.Click += openHandler;
        appIcon.Click += openHandler;
        appLabel.Click += openHandler;

        panelLoginFolderApps.Controls.Add(appTile);
        return appTile;
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

        return LoadImageCopy(iconPath);
    }

    private static Image? LoadImageCopy(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath)) return null;

        try
        {
            using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var image = Image.FromStream(stream);
            return new Bitmap(image);
        }
        catch
        {
            return null;
        }
    }

    private static Image? LoadBookmarkIconImage(string? imagePath, string fallbackText)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath)) return null;
        if (!Path.GetFileName(imagePath).EndsWith("_raw_v2.png", StringComparison.OrdinalIgnoreCase))
            return CreateBookmarkIcon(null, fallbackText);

        try
        {
            using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var source = Image.FromStream(stream);
            if (source.Width < 4 || source.Height < 4) return null;
            return CreateBookmarkIcon(source, fallbackText);
        }
        catch
        {
            return null;
        }
    }

    private static Bitmap CreateBookmarkIcon(Image? source, string fallbackText)
    {
        const int canvasSize = 64;
        var canvas = new Bitmap(canvasSize, canvasSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(canvas);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        RectangleF background = new RectangleF(1.5F, 1.5F, 61F, 61F);
        using GraphicsPath backgroundPath = CreateRoundedRectanglePath(background, 15F);
        using (var backgroundBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
            graphics.FillPath(backgroundBrush, backgroundPath);
        using (var borderPen = new Pen(Color.FromArgb(203, 213, 225), 1F))
            graphics.DrawPath(borderPen, backgroundPath);

        bool hasLetter = !string.IsNullOrWhiteSpace(fallbackText) && char.IsLetterOrDigit(fallbackText.Trim()[0]);
        void DrawDefaultGlyph()
        {
            if (hasLetter)
            {
                string letter = char.ToUpperInvariant(fallbackText.Trim()[0]).ToString();
                using var font = new Font("Segoe UI", 23F, FontStyle.Bold, GraphicsUnit.Pixel);
                TextRenderer.DrawText(
                    graphics,
                    letter,
                    font,
                    new Rectangle(10, 9, 44, 44),
                    Color.FromArgb(51, 65, 85),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                return;
            }

            using var globePen = new Pen(Color.FromArgb(51, 65, 85), 2.2F);
            RectangleF globe = new RectangleF(18F, 18F, 28F, 28F);
            graphics.DrawEllipse(globePen, globe);
            graphics.DrawArc(globePen, new RectangleF(25F, 18F, 14F, 28F), 90F, 180F);
            graphics.DrawArc(globePen, new RectangleF(25F, 18F, 14F, 28F), 270F, 180F);
            graphics.DrawLine(globePen, 19F, 27F, 45F, 27F);
            graphics.DrawLine(globePen, 19F, 37F, 45F, 37F);
        }

        if (source == null)
        {
            DrawDefaultGlyph();
            return canvas;
        }

        int longestSide = Math.Max(source.Width, source.Height);
        if (longestSide <= 32)
        {
            DrawDefaultGlyph();
            int badgeSide = Math.Clamp(longestSide, 12, 20);
            int badgeX = canvasSize - badgeSide - 6;
            int badgeY = canvasSize - badgeSide - 6;
            using var badgeBrush = new SolidBrush(Color.White);
            graphics.FillEllipse(badgeBrush, badgeX - 2, badgeY - 2, badgeSide + 4, badgeSide + 4);
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.DrawImage(source, new Rectangle(badgeX, badgeY, badgeSide, badgeSide));
            return canvas;
        }

        const float maximumSide = 44F;
        float scale = Math.Min(maximumSide / source.Width, maximumSide / source.Height);
        scale = Math.Min(scale, 1F);
        int drawWidth = Math.Max(8, (int)Math.Round(source.Width * scale));
        int drawHeight = Math.Max(8, (int)Math.Round(source.Height * scale));
        int drawX = (canvasSize - drawWidth) / 2;
        int drawY = (canvasSize - drawHeight) / 2;

        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(source, new Rectangle(drawX, drawY, drawWidth, drawHeight));
        return canvas;
    }

    private static GraphicsPath CreateRoundedRectanglePath(RectangleF rectangle, float radius)
    {
        float diameter = Math.Max(1F, radius * 2F);
        var path = new GraphicsPath();
        path.StartFigure();
        path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180F, 90F);
        path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270F, 90F);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0F, 90F);
        path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90F, 90F);
        path.CloseFigure();
        return path;
    }

    private static bool IsBookmarkableBrowserUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string GetBookmarkDisplayTitle(LoginBrowserBookmark bookmark)
    {
        string title = bookmark.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title) && Uri.TryCreate(bookmark.Url, UriKind.Absolute, out Uri? uri))
            title = uri.Host;
        if (string.IsNullOrWhiteSpace(title)) title = "즐겨찾기";
        return title.Length <= 14 ? title : title[..13] + "…";
    }

    private async Task OpenLoginBrowserBookmarkAsync(LoginBrowserBookmark bookmark)
    {
        if (!IsBookmarkableBrowserUrl(bookmark.Url)) return;

        HideLoginFolder(immediate: true);
        _isLoginBrowserMode = true;
        _lastWidth = Width;
        _lastHeight = Height;
        await ShowLoginBrowserAsync("웹 브라우저", bookmark.Url);

        if (!Path.GetFileName(bookmark.IconPath ?? string.Empty).EndsWith("_raw_v2.png", StringComparison.OrdinalIgnoreCase))
        {
            await Task.Delay(1200);
            string currentUrl = webViewX.CoreWebView2?.Source ?? string.Empty;
            if (Uri.TryCreate(currentUrl, UriKind.Absolute, out Uri? currentUri) &&
                Uri.TryCreate(bookmark.Url, UriKind.Absolute, out Uri? bookmarkUri) &&
                string.Equals(currentUri.Host, bookmarkUri.Host, StringComparison.OrdinalIgnoreCase))
            {
                string refreshedIconPath = await SaveCurrentPageFaviconAsync(currentUrl);
                if (!string.IsNullOrWhiteSpace(refreshedIconPath))
                {
                    bookmark.IconPath = refreshedIconPath;
                    SettingsManager.Save();
                }
            }
        }
    }

    private async Task ToggleCurrentPageBookmarkAsync()
    {
        if (webViewX.CoreWebView2 == null) return;

        string url = webViewX.CoreWebView2.Source?.Trim() ?? string.Empty;
        if (!IsBookmarkableBrowserUrl(url))
        {
            ShowCenteredMessage("현재 페이지는 즐겨찾기에 추가할 수 없습니다.", "즐겨찾기", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SettingsManager.Settings.LoginBrowserBookmarks ??= new List<LoginBrowserBookmark>();
        List<LoginBrowserBookmark> bookmarks = SettingsManager.Settings.LoginBrowserBookmarks;
        LoginBrowserBookmark? existing = bookmarks.FirstOrDefault(item =>
            string.Equals(item.Url?.Trim(), url, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            bookmarks.Remove(existing);
            DeleteUnusedBookmarkIcon(existing.IconPath, bookmarks);
            SettingsManager.Save();
            UpdateLoginBrowserFavoriteButton();
            RefreshLoginFolderApps();
            if (lblXGuide != null) lblXGuide.Text = "즐겨찾기에서 삭제했습니다.";
            return;
        }

        if (bookmarks.Count >= 100)
        {
            ShowCenteredMessage("즐겨찾기는 최대 100개까지 저장할 수 있습니다.", "즐겨찾기", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string title = await GetLoginBrowserDownloadTitleAsync();
        if (string.IsNullOrWhiteSpace(title) && Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            title = uri.Host;
        if (string.IsNullOrWhiteSpace(title)) title = "즐겨찾기";
        if (title.Length > 80) title = title[..80];

        string iconPath = await SaveCurrentPageFaviconAsync(url);
        bookmarks.Add(new LoginBrowserBookmark
        {
            Title = title,
            Url = url,
            IconPath = iconPath
        });
        SettingsManager.Save();
        UpdateLoginBrowserFavoriteButton();
        RefreshLoginFolderApps();
        if (lblXGuide != null) lblXGuide.Text = "로그인 브라우저 폴더에 즐겨찾기를 추가했습니다.";
    }

    private async Task<string> SaveCurrentPageFaviconAsync(string url)
    {
        if (webViewX.CoreWebView2 == null || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return string.Empty;

        string iconFolder = Path.Combine(SettingsManager.UserDataFolder, "FavoriteIcons");
        string iconName = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uri.Host.ToLowerInvariant())))[..20] + "_raw_v2.png";
        string iconPath = Path.Combine(iconFolder, iconName);

        try
        {
            Directory.CreateDirectory(iconFolder);
            using Stream faviconStream = await webViewX.CoreWebView2.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
            using Image source = Image.FromStream(faviconStream);
            if (source.Width < 4 || source.Height < 4) return string.Empty;
            source.Save(iconPath, System.Drawing.Imaging.ImageFormat.Png);
            return iconPath;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void DeleteUnusedBookmarkIcon(string? iconPath, IEnumerable<LoginBrowserBookmark> remainingBookmarks)
    {
        if (string.IsNullOrWhiteSpace(iconPath) || !File.Exists(iconPath)) return;
        if (remainingBookmarks.Any(item => string.Equals(item.IconPath, iconPath, StringComparison.OrdinalIgnoreCase))) return;

        try { File.Delete(iconPath); } catch { }
    }

    private void UpdateLoginBrowserFavoriteButton()
    {
        if (btnLoginBrowserFavorite == null) return;

        string currentUrl = webViewX.CoreWebView2?.Source?.Trim() ?? string.Empty;
        SettingsManager.Settings.LoginBrowserBookmarks ??= new List<LoginBrowserBookmark>();
        bool isFavorite = SettingsManager.Settings.LoginBrowserBookmarks.Any(item =>
            string.Equals(item.Url?.Trim(), currentUrl, StringComparison.OrdinalIgnoreCase));

        btnLoginBrowserFavorite.Text = isFavorite ? "★" : "☆";
        btnLoginBrowserFavorite.ForeColor = isFavorite
            ? Color.FromArgb(234, 179, 8)
            : Color.FromArgb(71, 85, 105);
        _loginDownloadToolTip?.SetToolTip(btnLoginBrowserFavorite, isFavorite ? "즐겨찾기에서 삭제" : "현재 페이지 즐겨찾기");
    }

    private async Task OpenLoginSiteFromFolderAsync(string siteName)
    {
        HideLoginFolder(immediate: true);
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
        bool showAddressBar = ShouldShowLoginBrowserAddressBar(siteName);

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

        if (ShouldFocusLoginBrowserAddressBar(siteName) && txtLoginBrowserAddress != null)
        {
            BeginInvoke(new Action(() =>
            {
                txtLoginBrowserAddress.Focus();
                txtLoginBrowserAddress.SelectAll();
            }));
        }
    }

    private static bool ShouldShowLoginBrowserAddressBar(string siteName)
    {
        return siteName == "X" ||
               siteName == "\uC6F9 \uBE0C\uB77C\uC6B0\uC800" ||
               siteName == "\uAE30\uD0C0" ||
               siteName == "콰이쇼우" ||
               siteName == "\uCE58\uC9C0\uC9C1" ||
               siteName == "SOOP";
    }

    private static bool ShouldFocusLoginBrowserAddressBar(string siteName)
    {
        return siteName == "\uC6F9 \uBE0C\uB77C\uC6B0\uC800" ||
               siteName == "\uAE30\uD0C0";
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
        if (btnLoginBrowserFavorite != null) btnLoginBrowserFavorite.Visible = visible;
        if (visible) UpdateLoginBrowserFavoriteButton();
        LayoutLoginBrowserAddressBar();
    }

    private void LayoutLoginBrowserAddressBar()
    {
        if (panelXTopBar == null || txtLoginBrowserAddress == null || btnLoginBrowserGo == null) return;

        int left = 222;
        int gap = 8;
        int goWidth = 58;
        int favoriteWidth = btnLoginBrowserFavorite == null ? 0 : 36;
        int rightLimit = Math.Max(left + 260, panelXTopBar.ClientSize.Width - 110);
        int addressWidth = Math.Max(180, rightLimit - left - goWidth - favoriteWidth - (gap * 2));

        txtLoginBrowserAddress.Location = new Point(left, 16);
        txtLoginBrowserAddress.Size = new Size(addressWidth, 24);
        btnLoginBrowserGo.Location = new Point(left + addressWidth + gap, 12);
        btnLoginBrowserGo.Size = new Size(goWidth, 36);
        if (btnLoginBrowserFavorite != null)
        {
            btnLoginBrowserFavorite.Location = new Point(btnLoginBrowserGo.Right + gap, 12);
            btnLoginBrowserFavorite.Size = new Size(favoriteWidth, 36);
        }
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
            "X" => "https://x.com/",
            "Instagram" => "https://www.instagram.com/accounts/login/",
            "콰이쇼우" => "https://www.kuaishou.com/",
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
                Size = new Size(36, 36),
                Location = new Point(15, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                BorderRadius = 18,
                BackColor = Color.FromArgb(148, 163, 184),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                TextOffset = new Point(0, -1),
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

            btnLoginBrowserFavorite = new RoundButton
            {
                Parent = panelXTopBar,
                Text = "☆",
                Visible = false,
                BorderRadius = 18,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Symbol", 15F, FontStyle.Regular),
                TextOffset = new Point(0, -1),
                UseVisualStyleBackColor = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnLoginBrowserFavorite.FlatAppearance.BorderSize = 0;
            btnLoginBrowserFavorite.Click += async (s, e) => await ToggleCurrentPageBookmarkAsync();
            _loginDownloadToolTip ??= new ToolTip();
            _loginDownloadToolTip.SetToolTip(btnLoginBrowserFavorite, "현재 페이지 즐겨찾기");
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
            btnLoginBrowserFavorite.BringToFront();
            
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
        bool isXPage = tglXPrivateMode.Checked ||
            currentUrl.Contains("x.com/", StringComparison.OrdinalIgnoreCase) ||
            currentUrl.Contains("twitter.com/", StringComparison.OrdinalIgnoreCase);

        _loginBrowserDownloadTitle = (_isLoginBrowserMode || isXPage)
            ? await GetLoginBrowserDownloadTitleAsync()
            : "";

        if (isXPage)
        {
            XActiveMediaContext activeMedia = await GetActiveXMediaContextAsync();
            string capturedUrl = "";

            if (IsXDirectMediaUrl(activeMedia.DirectUrl))
            {
                capturedUrl = NormalizeCapturedMediaUrl(activeMedia.DirectUrl);
            }
            else if (!string.IsNullOrWhiteSpace(activeMedia.MediaId))
            {
                capturedUrl = GetBestCapturedMediaUrl(activeMedia.MediaId);
            }

            if (IsXDirectMediaUrl(capturedUrl))
            {
                txtYtDlpUrl.Text = capturedUrl;
            }
            else if (IsXStatusPageUrl(currentUrl))
            {
                txtYtDlpUrl.Text = currentUrl;
            }
            else if (IsXStatusPageUrl(activeMedia.StatusUrl))
            {
                txtYtDlpUrl.Text = activeMedia.StatusUrl;
            }
            else
            {
                txtYtDlpUrl.Text = currentUrl;
            }
        }
        else if (_isLoginBrowserMode && !string.IsNullOrWhiteSpace(_capturedM3u8Url) && LooksLikeCapturedMediaUrl(_capturedM3u8Url))
        {
            txtYtDlpUrl.Text = NormalizeCapturedMediaUrl(_capturedM3u8Url);
        }
        else
        {
            txtYtDlpUrl.Text = currentUrl;
        }
        

        BtnYtDlpRun_Click(this, EventArgs.Empty);
    }

    private sealed class XActiveMediaContext
    {
        public string DirectUrl { get; set; } = "";
        public string PosterUrl { get; set; } = "";
        public string StatusUrl { get; set; } = "";
        public string MediaId { get; set; } = "";
    }

    private async Task<XActiveMediaContext> GetActiveXMediaContextAsync()
    {
        var context = new XActiveMediaContext();
        try
        {
            if (webViewX.CoreWebView2 == null) return context;

            string result = await webViewX.CoreWebView2.ExecuteScriptAsync(@"
(() => {
    const viewportWidth = window.innerWidth || document.documentElement.clientWidth || 1;
    const viewportHeight = window.innerHeight || document.documentElement.clientHeight || 1;
    let best = null;
    let bestScore = -1;

    for (const video of document.querySelectorAll('video')) {
        const rect = video.getBoundingClientRect();
        const width = Math.max(0, Math.min(rect.right, viewportWidth) - Math.max(rect.left, 0));
        const height = Math.max(0, Math.min(rect.bottom, viewportHeight) - Math.max(rect.top, 0));
        const visibleArea = width * height;
        if (visibleArea <= 0) continue;

        const score = visibleArea + (!video.paused && !video.ended ? 1000000000 : 0);
        if (score > bestScore) {
            best = video;
            bestScore = score;
        }
    }

    if (!best) return { directUrl: '', posterUrl: '', statusUrl: location.href };

    const article = best.closest('article');
    const statusLink = article
        ? Array.from(article.querySelectorAll('a[href*=""/status/""]')).find(a => /\/status\/\d+/.test(a.href))
        : null;

    return {
        directUrl: best.currentSrc || best.src || '',
        posterUrl: best.poster || '',
        statusUrl: statusLink?.href || location.href
    };
})()");

            using JsonDocument document = JsonDocument.Parse(result);
            JsonElement root = document.RootElement;
            context.DirectUrl = root.TryGetProperty("directUrl", out JsonElement directUrl) ? directUrl.GetString() ?? "" : "";
            context.PosterUrl = root.TryGetProperty("posterUrl", out JsonElement posterUrl) ? posterUrl.GetString() ?? "" : "";
            context.StatusUrl = root.TryGetProperty("statusUrl", out JsonElement statusUrl) ? statusUrl.GetString() ?? "" : "";
            context.MediaId = ExtractXMediaId(context.PosterUrl);
            if (string.IsNullOrWhiteSpace(context.MediaId))
                context.MediaId = ExtractXMediaId(context.DirectUrl);
        }
        catch { }

        return context;
    }

    private static string ExtractXMediaId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(
            url,
            @"(?:amplify_video(?:_thumb)?|ext_tw_video(?:_thumb)?|tweet_video(?:_thumb)?)/(\d+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "";
    }

    private static bool IsXDirectMediaUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return false;
        return uri.Host.Equals("video.twimg.com", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.EndsWith(".video.twimg.com", StringComparison.OrdinalIgnoreCase);
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
        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out Uri? targetUri) ||
            (targetUri.Scheme != Uri.UriSchemeHttp && targetUri.Scheme != Uri.UriSchemeHttps))
        {
            return "";
        }
        
        var cookieManager = webViewX.CoreWebView2.CookieManager;
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

        await AddCookiesAsync($"{targetUri.Scheme}://{targetUri.Host}/");

        if (HostMatchesAnyDomain(targetUri.Host, "youtube.com", "youtube-nocookie.com", "youtu.be", "google.com", "googlevideo.com"))
        {
            await AddCookiesAsync("https://www.youtube.com/");
            await AddCookiesAsync("https://youtube.com/");
            await AddCookiesAsync("https://m.youtube.com/");
            await AddCookiesAsync("https://studio.youtube.com/");
            await AddCookiesAsync("https://accounts.google.com/");
            await AddCookiesAsync("https://myaccount.google.com/");
            await AddCookiesAsync("https://www.google.com/");
        }
        else if (HostMatchesAnyDomain(targetUri.Host, "x.com", "twitter.com", "twimg.com"))
        {
            await AddCookiesAsync("https://x.com/");
            await AddCookiesAsync("https://twitter.com/");
        }
        else if (HostMatchesAnyDomain(targetUri.Host, "instagram.com", "cdninstagram.com", "fbcdn.net"))
        {
            await AddCookiesAsync("https://www.instagram.com/");
        }
        else if (HostMatchesAnyDomain(targetUri.Host, "threads.com", "threads.net"))
        {
            await AddCookiesAsync("https://www.threads.com/");
            await AddCookiesAsync("https://www.instagram.com/");
        }
        else if (HostMatchesAnyDomain(targetUri.Host, "kuaishou.com", "kwimgs.com", "yximgs.com", "wskwai.com"))
        {
            await AddCookiesAsync("https://www.kuaishou.com/");
            await AddCookiesAsync("https://video.kuaishou.com/");
        }
        else if (HostMatchesAnyDomain(targetUri.Host, "naver.com", "pstatic.net"))
        {
            await AddCookiesAsync("https://chzzk.naver.com/");
            await AddCookiesAsync("https://www.naver.com/");
        }
        else if (HostMatchesAnyDomain(targetUri.Host, "sooplive.com", "sooplive.co.kr", "afreecatv.com"))
        {
            await AddCookiesAsync("https://www.sooplive.co.kr/");
            await AddCookiesAsync("https://www.afreecatv.com/");
        }
        
        if (cookies == null || cookies.Count == 0) return "";

        string cookiePath = Path.Combine(SettingsManager.UserDataFolder, "temp_x_cookies.txt");
        int count = 0;
        CleanupManager.RegisterFile(cookiePath);
        using (var sw = new StreamWriter(cookiePath, false, new System.Text.UTF8Encoding(false)))
        {
            sw.WriteLine("# Netscape HTTP Cookie File");
            sw.WriteLine("# This file is generated by YoutubeDownloader");
            
                foreach (var c in cookies)
                {

                    string cookieDomain = c.Domain.ToLowerInvariant();
                    if (!IsCookieDomainAllowedForTarget(cookieDomain, targetUri.Host)) continue;

                count++;
                string domain = c.Domain;
                string flag = domain.StartsWith(".") ? "TRUE" : "FALSE";
                string secure = c.IsSecure ? "TRUE" : "FALSE";
                
                long expires = 0;
                try {
                    if (c.IsSession || c.Expires == default(DateTime) || c.Expires.Year < 1970 || c.Expires.Year > 2037) {
                        expires = 2147483647; // Session or far future
                    } else {
                        expires = new DateTimeOffset(c.Expires).ToUnixTimeSeconds();
                    }
                } catch { expires = 2147483647; }
                
                sw.WriteLine($"{domain}\t{flag}\t{c.Path}\t{secure}\t{expires}\t{c.Name}\t{c.Value}");
            }
        }
        
        if (count > 0) return cookiePath;

        try { File.Delete(cookiePath); } catch { }
        CleanupManager.UnregisterFile(cookiePath);
        return "";
    }

    private static bool CookieFileContainsCookie(string cookieFile, string cookieName)
    {
        if (string.IsNullOrWhiteSpace(cookieFile) || !File.Exists(cookieFile)) return false;

        try
        {
            foreach (string line in File.ReadLines(cookieFile))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
                string[] fields = line.Split('\t');
                if (fields.Length >= 7 && fields[5].Equals(cookieName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch { }

        return false;
    }

    private static bool IsKnownLoginCookieHost(string host)
    {
        return LoginCookieDomainGroups.Any(group => HostMatchesAnyDomain(host, group));
    }

    private static bool IsCookieDomainAllowedForTarget(string cookieDomain, string targetHost)
    {
        string normalizedCookieDomain = NormalizeCookieDomain(cookieDomain);
        string normalizedTargetHost = NormalizeCookieDomain(targetHost);
        if (string.IsNullOrEmpty(normalizedCookieDomain) || string.IsNullOrEmpty(normalizedTargetHost)) return false;

        if (HostMatchesDomain(normalizedTargetHost, normalizedCookieDomain)) return true;

        foreach (string[] group in LoginCookieDomainGroups)
        {
            if (HostMatchesAnyDomain(normalizedTargetHost, group) &&
                HostMatchesAnyDomain(normalizedCookieDomain, group))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HostMatchesAnyDomain(string host, params string[] domains)
    {
        return domains.Any(domain => HostMatchesDomain(host, domain));
    }

    private static bool HostMatchesDomain(string host, string domain)
    {
        string normalizedHost = NormalizeCookieDomain(host);
        string normalizedDomain = NormalizeCookieDomain(domain);
        return normalizedHost.Equals(normalizedDomain, StringComparison.OrdinalIgnoreCase) ||
               normalizedHost.EndsWith("." + normalizedDomain, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeCookieDomain(string value)
    {
        return (value ?? string.Empty).Trim().TrimStart('.').TrimEnd('.').ToLowerInvariant();
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
        public bool EmbedMetadata { get; set; }
        public bool RemoveSponsorSegments { get; set; }
        public double SectionStartSeconds { get; set; }
        public double SectionEndSeconds { get; set; }
        public string SourceFeature { get; set; } = "유튜브 다운로더";
        public string TemporaryDownloadDirectory { get; set; } = "";
        public bool MediaDownloadCompleted { get; set; }
    }

    private class YtDlpDownloadJob
    {
        public string Id { get; set; } = "";
        public string Url { get; set; } = "";
        public string SavePath { get; set; } = "";
        public bool DownloadSubtitles { get; set; }
        public string SubtitleLanguagePreset { get; set; } = "Ko";
        public bool EmbedMetadata { get; set; }
        public bool RemoveSponsorSegments { get; set; }
        public double SectionStartSeconds { get; set; }
        public double SectionEndSeconds { get; set; }
        public string FormatSelector { get; set; } = "";
        public string OutputNameTemplate { get; set; } = "%(title)s";
        public string PreferredTitle { get; set; } = "";
        public int PlaylistItemIndex { get; set; }
        public bool UseXPrivateMode { get; set; }
        public bool UseInstaPrivateMode { get; set; }
        public bool UseLoginBrowserCookies { get; set; }
        public string SourceFeature { get; set; } = "웹사이트 영상 다운";
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
        public long EstimatedBytes { get; set; }

        public QualityOption(string title, string id, bool isVideo, long estimatedBytes = 0)
        {
            Title = title;
            Id = id;
            IsVideo = isVideo;
            EstimatedBytes = estimatedBytes;
        }
        public override string ToString() => EstimatedBytes > 0
            ? $"{Title}  ·  예상 {FormatFileSize(EstimatedBytes)}"
            : Title;
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

            if (isError) reportTitle = "\uC624\uB958 \uBCF4\uACE0";
            else reportTitle = "\uC790\uB3D9 \uC0C1\uD0DC \uBCF4\uACE0";

            string installId = string.IsNullOrWhiteSpace(SettingsManager.Settings.InstallId)
                ? "\uBBF8\uC0DD\uC131"
                : SettingsManager.Settings.InstallId.Trim();
            string safeErrorMsg = isError ? BuildErrorReportContent(errorMsg) : SanitizeReportText(errorMsg);
            var payload = new
            {
                username = "MMT \uB370\uC774\uD130 \uC9D1\uACC4\uAE30",
                allowed_mentions = new { parse = Array.Empty<string>() },
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
                Feature = NormalizeUsageFeatureName(SanitizeReportText(x.Key.Replace(todayStr + "_", ""))),
                Count = Math.Max(0, x.Value)
            })
            .Where(x => x.Count > 0 && !string.IsNullOrWhiteSpace(x.Feature))
            .GroupBy(x => x.Feature, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Feature = group.Key,
                Count = group.Sum(item => item.Count)
            })
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

    private static string NormalizeUsageFeatureName(string feature)
    {
        if (feature.Equals("video.twimg.com", StringComparison.OrdinalIgnoreCase) ||
            feature.Equals("twitter.com", StringComparison.OrdinalIgnoreCase))
        {
            return "X";
        }

        return feature;
    }

    private static string BuildErrorReportContent(string rawError)
    {
        string text = SanitizeReportText(rawError);
        string feature = ExtractReportValue(text, "기능:");
        string stage = ExtractReportValue(text, "세부 단계:");
        string site = ExtractReportValue(text, "사이트:");
        string target = ExtractReportValue(text, "대상:");
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
            $"**사용 기능**: {SanitizeReportText(string.IsNullOrWhiteSpace(feature) ? "확인 불가" : feature)}",
            $"**세부 단계**: {SanitizeReportText(string.IsNullOrWhiteSpace(stage) ? "확인 불가" : stage)}",
            $"**대상 사이트**: {SanitizeReportText(string.IsNullOrWhiteSpace(site) ? "확인 불가" : site)}",
            $"**대상 URL/파일**: {SanitizeReportText(string.IsNullOrWhiteSpace(target) ? (string.IsNullOrWhiteSpace(url) ? "확인 불가" : url) : target)}",
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

        return RedactSensitiveReportData(string.Join("\n", lines));
    }

    private static string RedactSensitiveReportData(string text)
    {
        string redacted = text;
        redacted = System.Text.RegularExpressions.Regex.Replace(
            redacted,
            @"(?is)(--add-header|-headers)\s+""[^""]*""",
            "$1 [REDACTED]");
        redacted = System.Text.RegularExpressions.Regex.Replace(
            redacted,
            @"(?i)--cookies(?:-from-browser)?\s+(?:""[^""]*""|\S+)",
            "--cookies [REDACTED]");
        redacted = System.Text.RegularExpressions.Regex.Replace(
            redacted,
            @"(?im)\b(Cookie|Authorization|Proxy-Authorization|x-csrf-token|x-device-token)\s*[:=]\s*[^\r\n]*",
            "$1: [REDACTED]");
        redacted = System.Text.RegularExpressions.Regex.Replace(
            redacted,
            @"https?://[^\s<>\r\n]+",
            match => SanitizeUrlMatchForReport(match.Value));

        redacted = ReplacePathForReport(
            redacted,
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "%USERPROFILE%");
        redacted = ReplacePathForReport(
            redacted,
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "%LOCALAPPDATA%");
        redacted = ReplacePathForReport(redacted, Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), "%TEMP%");
        return redacted;
    }

    private static string SanitizeUrlMatchForReport(string rawMatch)
    {
        string candidate = rawMatch;
        string suffix = "";
        while (candidate.Length > 0 && ").,;]}>'\"".Contains(candidate[^1]))
        {
            suffix = candidate[^1] + suffix;
            candidate = candidate[..^1];
        }

        return SanitizeUrlForReport(candidate) + suffix;
    }

    private static string SanitizeUrlForReport(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return url;
        }

        try
        {
            var builder = new UriBuilder(uri)
            {
                UserName = "",
                Password = "",
                Fragment = ""
            };

            string query = uri.Query.TrimStart('?');
            if (!string.IsNullOrEmpty(query))
            {
                var safeParts = new List<string>();
                foreach (string part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    int equalsIndex = part.IndexOf('=');
                    string rawKey = equalsIndex >= 0 ? part[..equalsIndex] : part;
                    string decodedKey;
                    try { decodedKey = Uri.UnescapeDataString(rawKey.Replace("+", " ")); }
                    catch { decodedKey = rawKey; }

                    safeParts.Add(IsSensitiveQueryKey(decodedKey)
                        ? rawKey + "=%5BREDACTED%5D"
                        : part);
                }
                builder.Query = string.Join("&", safeParts);
            }

            return builder.Uri.AbsoluteUri;
        }
        catch
        {
            return url;
        }
    }

    private static bool IsSensitiveQueryKey(string key)
    {
        string value = key.Trim().ToLowerInvariant();
        return value.Contains("token") ||
               value.Contains("auth") ||
               value.Contains("cookie") ||
               value.Contains("session") ||
               value.Contains("signature") ||
               value.Contains("credential") ||
               value.Contains("password") ||
               value.Contains("secret") ||
               value.Contains("hmac") ||
               value.Contains("hdnts") ||
               value.Equals("sig", StringComparison.Ordinal) ||
               value.Equals("key", StringComparison.Ordinal) ||
               value.Equals("policy", StringComparison.Ordinal) ||
               value.Equals("acl", StringComparison.Ordinal);
    }

    private static string ReplacePathForReport(string text, string path, string replacement)
    {
        if (string.IsNullOrWhiteSpace(path)) return text;
        return System.Text.RegularExpressions.Regex.Replace(
            text,
            System.Text.RegularExpressions.Regex.Escape(path),
            replacement.Replace("$", "$$"),
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    // 전역 오류 보고용 헬퍼
    private void ReportError(string feature, string stage, string msg, Exception? ex = null)
    {
        string safeMessage = SanitizeReportText(msg);
        string url = ExtractReportValue(safeMessage, "URL:");
        string input = ExtractReportValue(safeMessage, "Input:");
        string target = !string.IsNullOrWhiteSpace(url) ? url : input;
        string site = !string.IsNullOrWhiteSpace(url)
            ? GetSiteNameFromUrl(url)
            : !string.IsNullOrWhiteSpace(input) ? "로컬 파일" : "확인 불가";
        string detail = ex != null
            ? $"{safeMessage} (\uC6D0\uC778: {SanitizeReportText(ex.Message)})"
            : safeMessage;
        string fullMsg =
            $"기능: {SanitizeReportText(feature)}\n" +
            $"세부 단계: {SanitizeReportText(stage)}\n" +
            $"사이트: {SanitizeReportText(site)}\n" +
            $"대상: {SanitizeReportText(string.IsNullOrWhiteSpace(target) ? "확인 불가" : target)}\n" +
            detail;
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
