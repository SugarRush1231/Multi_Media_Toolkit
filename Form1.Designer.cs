namespace YoutubeDownloader;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    // Layout
    private System.Windows.Forms.Panel panelSidebar;
    private System.Windows.Forms.Panel panelMain;
    private System.Windows.Forms.Label lblLogo;
    private YoutubeDownloader.RoundButton btnTabYoutube;
    private YoutubeDownloader.RoundButton btnTabYtDlp;
    private YoutubeDownloader.RoundButton btnTabWebM;
    private YoutubeDownloader.RoundButton btnTabCodec;
    private YoutubeDownloader.RoundButton btnTabAudio;
    private YoutubeDownloader.RoundButton btnTabMiniEdit;
    private YoutubeDownloader.RoundButton btnTabSettings;
    private System.Windows.Forms.Label lblSubLogo;

    // Hidden TabControl
    private System.Windows.Forms.TabControl tabControlMain;
    
    // Tab 1: YouTube
    private System.Windows.Forms.TabPage tabYoutube;
    private System.Windows.Forms.Panel panelUrlContainer;
    private System.Windows.Forms.TextBox txtUrl;
    private System.Windows.Forms.Label lblUrl;
    private YoutubeDownloader.RoundButton btnLoad;
    private System.Windows.Forms.Panel panelInfo;
    private System.Windows.Forms.PictureBox picThumbnail;
    private System.Windows.Forms.Label lblVideoTitle;
    private System.Windows.Forms.Label lblQuality;
    private System.Windows.Forms.ComboBox cmbQuality;
    private System.Windows.Forms.CheckBox chkYoutubeDownloadSubtitles;
    private YoutubeDownloader.RoundButton btnAddQueue;
    private System.Windows.Forms.ListView lvQueue;
    private System.Windows.Forms.ColumnHeader colTitle;
    private System.Windows.Forms.ColumnHeader colQuality;
    private System.Windows.Forms.ColumnHeader colStatus;
    private YoutubeDownloader.RoundButton btnRemoveSelected;
    private System.Windows.Forms.Label lblStatus;
    private System.Windows.Forms.ProgressBar pbYoutube;
    private System.Windows.Forms.Label lblYoutubeSavePath;
    private YoutubeDownloader.RoundButton btnOpenYoutubeFolder;
    private System.Windows.Forms.TextBox txtEditTitle;
    private System.Windows.Forms.ContextMenuStrip contextMenuRemove;
    private System.Windows.Forms.ToolStripMenuItem menuRemoveSelected;

    // Tab 2: WebM
    private System.Windows.Forms.TabPage tabWebM;
    private System.Windows.Forms.Label lblWebMTitle;
    private System.Windows.Forms.Panel panelWebMInput;
    private System.Windows.Forms.TextBox txtWebMInput;
    private YoutubeDownloader.RoundButton btnBrowseWebM;
    private YoutubeDownloader.RoundButton btnConvertWebM;
    private YoutubeDownloader.RoundButton btnCancelWebM;
    private System.Windows.Forms.Label lblWebMStatus;
    private System.Windows.Forms.ProgressBar pbWebM;
    private System.Windows.Forms.ComboBox cmbWebMFormat;
    private System.Windows.Forms.Panel panelWebMOutput;
    private System.Windows.Forms.TextBox txtWebMOutput;
    private YoutubeDownloader.RoundButton btnBrowseWebMOutput;
    private System.Windows.Forms.Label lblWebMSavePath;
    private YoutubeDownloader.RoundButton btnOpenWebMFolder;

    // Tab 3: Codec Fixed
    private System.Windows.Forms.TabPage tabCodec;
    private System.Windows.Forms.Label lblCodecTitle;
    private System.Windows.Forms.Panel panelCodecInput;
    private System.Windows.Forms.TextBox txtCodecInput;
    private YoutubeDownloader.RoundButton btnBrowseCodec;
    private YoutubeDownloader.RoundButton btnConvertCodec;
    private System.Windows.Forms.Label lblCodecSavePath;
    private YoutubeDownloader.RoundButton btnOpenCodecFolder;
    private YoutubeDownloader.RoundButton btnCancelCodec;
    private System.Windows.Forms.Label lblCodecStatus;
    private System.Windows.Forms.Label lblCodecDesc;
    private System.Windows.Forms.ProgressBar pbCodec;
    private System.Windows.Forms.Panel panelCodecOutput;
    private System.Windows.Forms.TextBox txtCodecOutput;
    private YoutubeDownloader.RoundButton btnBrowseCodecOutput;

    // Tab 4: Audio Converter
    private System.Windows.Forms.TabPage tabAudio;
    private System.Windows.Forms.Label lblAudioTitle;
    private System.Windows.Forms.Panel panelAudioInput;
    private System.Windows.Forms.TextBox txtAudioInput;
    private YoutubeDownloader.RoundButton btnBrowseAudio;
    private System.Windows.Forms.Panel panelAudioOutput;
    private System.Windows.Forms.TextBox txtAudioOutput;
    private YoutubeDownloader.RoundButton btnBrowseAudioOutput;
    private System.Windows.Forms.ComboBox cmbAudioFormat;
    private YoutubeDownloader.RoundButton btnConvertAudio;
    private YoutubeDownloader.RoundButton btnCancelAudio;
    private System.Windows.Forms.Label lblAudioStatus;
    private System.Windows.Forms.ProgressBar pbAudio;
    private System.Windows.Forms.Label lblAudioSavePath;
    private YoutubeDownloader.RoundButton btnOpenAudioFolder;

    // Tab 4.2: YtDlp (External)
    private System.Windows.Forms.TabPage tabYtDlp;
    private System.Windows.Forms.Label lblYtDlpTitle;
    private System.Windows.Forms.Label lblYtDlpDesc;
    private System.Windows.Forms.Panel panelYtDlpUrl;
    private System.Windows.Forms.TextBox txtYtDlpUrl;
    private System.Windows.Forms.CheckBox chkYtDlpDownloadSubtitles;
    private YoutubeDownloader.RoundButton btnYtDlpRun;
    private YoutubeDownloader.RoundButton btnYtDlpCancel;
    private System.Windows.Forms.Label lblYtDlpStatus;
    private System.Windows.Forms.ProgressBar pbYtDlp;
    private System.Windows.Forms.ListView lvYtDlpQueue;
    private System.Windows.Forms.ColumnHeader colYtDlpUrl;
    private System.Windows.Forms.ColumnHeader colYtDlpSubtitles;
    private System.Windows.Forms.ColumnHeader colYtDlpStatus;
    private YoutubeDownloader.RoundButton btnRemoveSelectedYtDlp;
    private System.Windows.Forms.ContextMenuStrip contextMenuYtDlpRemove;
    private System.Windows.Forms.ToolStripMenuItem menuYtDlpRemoveSelected;
    private YoutubeDownloader.RoundButton btnYtDlpLoginBrowser;
    private YoutubeDownloader.ToggleSwitch tglXPrivateMode;
    private System.Windows.Forms.Label lblInstaPrivateMode;
    private YoutubeDownloader.ToggleSwitch tglInstaPrivateMode;
    private System.Windows.Forms.Label lblXPrivateMode;
    private System.Windows.Forms.Panel panelXBrowser;
    private Microsoft.Web.WebView2.WinForms.WebView2 webViewX;
    private YoutubeDownloader.RoundButton btnXCapture;
    private YoutubeDownloader.RoundButton btnXDownload;
    private YoutubeDownloader.RoundButton btnXClose;
    private System.Windows.Forms.ProgressBar pbXDownload;
    private System.Windows.Forms.Label lblXStatus;
    private System.Windows.Forms.Label lblYtDlpSavePath;
    private YoutubeDownloader.RoundButton btnOpenYtDlpFolder;

    // Tab 4.5: Mini Edit
    private System.Windows.Forms.TabPage tabMiniEdit;
    private YoutubeDownloader.MiniEditor miniEditorControl;

    // Tab 5: Settings
    private System.Windows.Forms.TabPage tabSettings;
    private System.Windows.Forms.Label lblSettingsTitle;
    private System.Windows.Forms.CheckBox chkShowNotifications;
    private System.Windows.Forms.CheckBox chkAutoOpenFolder;
    private System.Windows.Forms.CheckBox chkAutoUpdateCheck;
    private System.Windows.Forms.Label lblDownloadFolder;
    private System.Windows.Forms.Panel panelSettingsFolder;
    private System.Windows.Forms.TextBox txtDownloadFolder;
    private YoutubeDownloader.RoundButton btnBrowseFolder;
    private YoutubeDownloader.RoundButton btnSaveSettings;
    private YoutubeDownloader.RoundButton btnCheckUpdate;
    private System.Windows.Forms.Label lblAbout;


    // NotifyIcon
    private System.Windows.Forms.NotifyIcon notifyIconApp;
    private System.Windows.Forms.ContextMenuStrip contextMenuTray;
    private System.Windows.Forms.ToolStripMenuItem menuTrayOpen;
    private System.Windows.Forms.ToolStripMenuItem menuTrayExit;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
        panelSidebar = new Panel();
        lblSubLogo = new Label();
        btnTabSettings = new RoundButton();
        btnTabMiniEdit = new RoundButton();
        btnTabAudio = new RoundButton();
        btnTabWebM = new RoundButton();
        btnTabCodec = new RoundButton();
        btnTabYtDlp = new RoundButton();
        btnTabYoutube = new RoundButton();
        lblLogo = new Label();
        panelMain = new Panel();
        tabControlMain = new TabControl();
        tabYoutube = new TabPage();
        lblUrl = new Label();
        panelUrlContainer = new Panel();
        txtUrl = new TextBox();
        btnLoad = new RoundButton();
        panelInfo = new Panel();
        lblVideoTitle = new Label();
        txtEditTitle = new TextBox();
        picThumbnail = new PictureBox();
        lblQuality = new Label();
        cmbQuality = new ComboBox();
        chkYoutubeDownloadSubtitles = new CheckBox();
        btnAddQueue = new RoundButton();
        lvQueue = new ListView();
        colTitle = new ColumnHeader();
        colQuality = new ColumnHeader();
        colStatus = new ColumnHeader();
        contextMenuRemove = new ContextMenuStrip(components);
        menuRemoveSelected = new ToolStripMenuItem();
        btnRemoveSelected = new RoundButton();
        lblStatus = new Label();
        pbYoutube = new ProgressBar();
        lblYoutubeSavePath = new Label();
        btnOpenYoutubeFolder = new RoundButton();
        tabYtDlp = new TabPage();
        lblXPrivateMode = new Label();
        tglXPrivateMode = new ToggleSwitch();
        lblInstaPrivateMode = new Label();
        tglInstaPrivateMode = new ToggleSwitch();
        panelXBrowser = new Panel();
        webViewX = new Microsoft.Web.WebView2.WinForms.WebView2();
        btnXCapture = new RoundButton();
        btnXDownload = new RoundButton();
        btnXClose = new RoundButton();
        pbXDownload = new ProgressBar();
        lblXStatus = new Label();
        lblYtDlpSavePath = new Label();
        btnOpenYtDlpFolder = new RoundButton();
        lblYtDlpTitle = new Label();
        lblYtDlpDesc = new Label();
        panelYtDlpUrl = new Panel();
        txtYtDlpUrl = new TextBox();
        chkYtDlpDownloadSubtitles = new CheckBox();
        btnYtDlpRun = new RoundButton();
        btnYtDlpCancel = new RoundButton();
        lblYtDlpStatus = new Label();
        pbYtDlp = new ProgressBar();
        lvYtDlpQueue = new ListView();
        colYtDlpUrl = new ColumnHeader();
        colYtDlpSubtitles = new ColumnHeader();
        colYtDlpStatus = new ColumnHeader();
        btnRemoveSelectedYtDlp = new RoundButton();
        contextMenuYtDlpRemove = new ContextMenuStrip(components);
        menuYtDlpRemoveSelected = new ToolStripMenuItem();
        btnYtDlpLoginBrowser = new RoundButton();
        tabWebM = new TabPage();
        lblWebMTitle = new Label();
        panelWebMInput = new Panel();
        txtWebMInput = new TextBox();
        btnBrowseWebM = new RoundButton();
        panelWebMOutput = new Panel();
        txtWebMOutput = new TextBox();
        btnBrowseWebMOutput = new RoundButton();
        btnConvertWebM = new RoundButton();
        btnCancelWebM = new RoundButton();
        cmbWebMFormat = new ComboBox();
        lblWebMStatus = new Label();
        pbWebM = new ProgressBar();
        lblWebMSavePath = new Label();
        btnOpenWebMFolder = new RoundButton();
        tabCodec = new TabPage();
        lblCodecTitle = new Label();
        lblCodecDesc = new Label();
        panelCodecInput = new Panel();
        txtCodecInput = new TextBox();
        btnBrowseCodec = new RoundButton();
        panelCodecOutput = new Panel();
        txtCodecOutput = new TextBox();
        btnBrowseCodecOutput = new RoundButton();
        btnConvertCodec = new RoundButton();
        btnCancelCodec = new RoundButton();
        lblCodecStatus = new Label();
        pbCodec = new ProgressBar();
        lblCodecSavePath = new Label();
        btnOpenCodecFolder = new RoundButton();
        tabAudio = new TabPage();
        lblAudioTitle = new Label();
        panelAudioInput = new Panel();
        txtAudioInput = new TextBox();
        btnBrowseAudio = new RoundButton();
        panelAudioOutput = new Panel();
        txtAudioOutput = new TextBox();
        btnBrowseAudioOutput = new RoundButton();
        cmbAudioFormat = new ComboBox();
        btnConvertAudio = new RoundButton();
        btnCancelAudio = new RoundButton();
        lblAudioStatus = new Label();
        pbAudio = new ProgressBar();
        lblAudioSavePath = new Label();
        btnOpenAudioFolder = new RoundButton();
        tabSettings = new TabPage();
        lblAbout = new Label();
        lblSettingsTitle = new Label();
        chkShowNotifications = new CheckBox();
        chkAutoOpenFolder = new CheckBox();
        chkAutoUpdateCheck = new CheckBox();
        lblDownloadFolder = new Label();
        panelSettingsFolder = new Panel();
        txtDownloadFolder = new TextBox();
        btnBrowseFolder = new RoundButton();
        btnSaveSettings = new RoundButton();
        btnCheckUpdate = new RoundButton();
        tabMiniEdit = new TabPage();
        miniEditorControl = new MiniEditor();
        notifyIconApp = new NotifyIcon(components);
        contextMenuTray = new ContextMenuStrip(components);
        menuTrayOpen = new ToolStripMenuItem();
        menuTrayExit = new ToolStripMenuItem();
        panelSidebar.SuspendLayout();
        panelMain.SuspendLayout();
        tabControlMain.SuspendLayout();
        tabYoutube.SuspendLayout();
        panelUrlContainer.SuspendLayout();
        panelInfo.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)picThumbnail).BeginInit();
        contextMenuRemove.SuspendLayout();
        tabYtDlp.SuspendLayout();
        contextMenuYtDlpRemove.SuspendLayout();
        panelXBrowser.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)webViewX).BeginInit();
        panelYtDlpUrl.SuspendLayout();
        tabWebM.SuspendLayout();
        panelWebMInput.SuspendLayout();
        panelWebMOutput.SuspendLayout();
        tabCodec.SuspendLayout();
        panelCodecInput.SuspendLayout();
        panelCodecOutput.SuspendLayout();
        tabAudio.SuspendLayout();
        panelAudioInput.SuspendLayout();
        panelAudioOutput.SuspendLayout();
        tabSettings.SuspendLayout();
        panelSettingsFolder.SuspendLayout();
        tabMiniEdit.SuspendLayout();
        contextMenuTray.SuspendLayout();
        SuspendLayout();
        // 
        // panelSidebar
        // 
        panelSidebar.BackColor = Color.FromArgb(30, 30, 30);
        panelSidebar.Controls.Add(lblSubLogo);
        panelSidebar.Controls.Add(btnTabSettings);
        panelSidebar.Controls.Add(btnTabMiniEdit);
        panelSidebar.Controls.Add(btnTabAudio);
        panelSidebar.Controls.Add(btnTabWebM);
        panelSidebar.Controls.Add(btnTabCodec);
        panelSidebar.Controls.Add(btnTabYtDlp);
        panelSidebar.Controls.Add(btnTabYoutube);
        panelSidebar.Controls.Add(lblLogo);
        panelSidebar.Dock = DockStyle.Left;
        panelSidebar.Location = new Point(0, 0);
        panelSidebar.Name = "panelSidebar";
        panelSidebar.Size = new Size(180, 600);
        panelSidebar.TabIndex = 0;
        // 
        // lblSubLogo
        // 
        lblSubLogo.AutoSize = true;
        lblSubLogo.Font = new Font("Segoe UI", 9F);
        lblSubLogo.ForeColor = Color.FromArgb(200, 200, 200);
        lblSubLogo.Location = new Point(65, 68);
        lblSubLogo.Name = "lblSubLogo";
        lblSubLogo.Size = new Size(92, 15);
        lblSubLogo.TabIndex = 1;
        lblSubLogo.Text = "";
        lblSubLogo.Visible = false;
        // 
        // btnTabSettings
        // 
        btnTabSettings.BackColor = Color.FromArgb(50, 50, 50);
        btnTabSettings.FlatAppearance.BorderSize = 0;
        btnTabSettings.FlatStyle = FlatStyle.Flat;
        btnTabSettings.Font = new Font("Segoe UI", 10F);
        btnTabSettings.ForeColor = Color.Silver;
        btnTabSettings.Location = new Point(15, 500);
        btnTabSettings.Name = "btnTabSettings";
        btnTabSettings.Size = new Size(150, 40);
        btnTabSettings.TabIndex = 5;
        btnTabSettings.Text = "설정";
        btnTabSettings.UseVisualStyleBackColor = false;
        btnTabSettings.Click += BtnTab_Click;
        // 
        // btnTabMiniEdit
        // 
        btnTabMiniEdit.BackColor = Color.FromArgb(50, 50, 50);
        btnTabMiniEdit.FlatAppearance.BorderSize = 0;
        btnTabMiniEdit.FlatStyle = FlatStyle.Flat;
        btnTabMiniEdit.Font = new Font("Segoe UI", 10F);
        btnTabMiniEdit.ForeColor = Color.Silver;
        btnTabMiniEdit.Location = new Point(15, 350);
        btnTabMiniEdit.Name = "btnTabMiniEdit";
        btnTabMiniEdit.Size = new Size(150, 40);
        btnTabMiniEdit.TabIndex = 6;
        btnTabMiniEdit.Text = "미니 편집기";
        btnTabMiniEdit.UseVisualStyleBackColor = false;
        btnTabMiniEdit.Click += BtnTab_Click;
        // 
        // btnTabAudio
        // 
        btnTabAudio.BackColor = Color.FromArgb(50, 50, 50);
        btnTabAudio.FlatAppearance.BorderSize = 0;
        btnTabAudio.FlatStyle = FlatStyle.Flat;
        btnTabAudio.Font = new Font("Segoe UI", 10F);
        btnTabAudio.ForeColor = Color.Silver;
        btnTabAudio.Location = new Point(15, 300);
        btnTabAudio.Name = "btnTabAudio";
        btnTabAudio.Size = new Size(150, 40);
        btnTabAudio.TabIndex = 5;
        btnTabAudio.Text = "오디오 변환기";
        btnTabAudio.UseVisualStyleBackColor = false;
        btnTabAudio.Click += BtnTab_Click;
        // 
        // btnTabWebM
        // 
        btnTabWebM.BackColor = Color.FromArgb(50, 50, 50);
        btnTabWebM.FlatAppearance.BorderSize = 0;
        btnTabWebM.FlatStyle = FlatStyle.Flat;
        btnTabWebM.Font = new Font("Segoe UI", 10F);
        btnTabWebM.ForeColor = Color.Silver;
        btnTabWebM.Location = new Point(15, 250);
        btnTabWebM.Name = "btnTabWebM";
        btnTabWebM.Size = new Size(150, 40);
        btnTabWebM.TabIndex = 4;
        btnTabWebM.Text = "포맷 변환기";
        btnTabWebM.UseVisualStyleBackColor = false;
        btnTabWebM.Click += BtnTab_Click;
        // 
        // btnTabCodec
        // 
        btnTabCodec.BackColor = Color.FromArgb(50, 50, 50);
        btnTabCodec.FlatAppearance.BorderSize = 0;
        btnTabCodec.FlatStyle = FlatStyle.Flat;
        btnTabCodec.Font = new Font("Segoe UI", 10F);
        btnTabCodec.ForeColor = Color.Silver;
        btnTabCodec.Location = new Point(15, 200);
        btnTabCodec.Name = "btnTabCodec";
        btnTabCodec.Size = new Size(150, 40);
        btnTabCodec.TabIndex = 3;
        btnTabCodec.Text = "Pr/AE 코덱 해결";
        btnTabCodec.UseVisualStyleBackColor = false;
        btnTabCodec.Click += BtnTab_Click;
        // 
        // btnTabYtDlp
        // 
        btnTabYtDlp.BackColor = Color.FromArgb(50, 50, 50);
        btnTabYtDlp.FlatAppearance.BorderSize = 0;
        btnTabYtDlp.FlatStyle = FlatStyle.Flat;
        btnTabYtDlp.Font = new Font("Segoe UI", 10F);
        btnTabYtDlp.ForeColor = Color.Silver;
        btnTabYtDlp.Location = new Point(15, 150);
        btnTabYtDlp.Name = "btnTabYtDlp";
        btnTabYtDlp.Size = new Size(150, 40);
        btnTabYtDlp.TabIndex = 2;
        btnTabYtDlp.Text = "웹 사이트 영상 다운";
        btnTabYtDlp.UseVisualStyleBackColor = false;
        btnTabYtDlp.Click += BtnTab_Click;
        // 
        // btnTabYoutube
        // 
        btnTabYoutube.BackColor = Color.FromArgb(255, 71, 87);
        btnTabYoutube.FlatAppearance.BorderSize = 0;
        btnTabYoutube.FlatStyle = FlatStyle.Flat;
        btnTabYoutube.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnTabYoutube.ForeColor = Color.White;
        btnTabYoutube.Location = new Point(15, 100);
        btnTabYoutube.Name = "btnTabYoutube";
        btnTabYoutube.Size = new Size(150, 40);
        btnTabYoutube.TabIndex = 1;
        btnTabYoutube.Text = "유튜브 다운로더";
        btnTabYoutube.UseVisualStyleBackColor = false;
        btnTabYoutube.Click += BtnTab_Click;
        // 
        // lblLogo
        // 
        lblLogo.AutoSize = true;
        lblLogo.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblLogo.ForeColor = Color.White;
        lblLogo.Location = new Point(15, 20);
        lblLogo.Name = "lblLogo";
        lblLogo.Size = new Size(133, 50);
        lblLogo.TabIndex = 0;
        lblLogo.Text = "Multi\nMedia Toolkit";
        lblLogo.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // panelMain
        // 
        panelMain.BackColor = Color.FromArgb(250, 250, 250);
        panelMain.Controls.Add(tabControlMain);
        panelMain.Dock = DockStyle.Fill;
        panelMain.Location = new Point(180, 0);
        panelMain.Name = "panelMain";
        panelMain.Size = new Size(620, 600);
        panelMain.TabIndex = 1;
        // 
        // tabControlMain
        // 
        tabControlMain.Appearance = TabAppearance.FlatButtons;
        tabControlMain.Controls.Add(tabYoutube);
        tabControlMain.Controls.Add(tabYtDlp);
        tabControlMain.Controls.Add(tabWebM);
        tabControlMain.Controls.Add(tabCodec);
        tabControlMain.Controls.Add(tabAudio);
        tabControlMain.Controls.Add(tabSettings);
        tabControlMain.Controls.Add(tabMiniEdit);
        tabControlMain.Dock = DockStyle.Fill;
        tabControlMain.ItemSize = new Size(0, 1);
        tabControlMain.Location = new Point(0, 0);
        tabControlMain.Name = "tabControlMain";
        tabControlMain.SelectedIndex = 0;
        tabControlMain.Size = new Size(620, 600);
        tabControlMain.SizeMode = TabSizeMode.Fixed;
        tabControlMain.TabIndex = 0;
        // 
        // tabYoutube
        // 
        tabYoutube.BackColor = Color.FromArgb(250, 250, 250);
        tabYoutube.Controls.Add(lblUrl);
        tabYoutube.Controls.Add(panelUrlContainer);
        tabYoutube.Controls.Add(btnLoad);
        tabYoutube.Controls.Add(panelInfo);
        tabYoutube.Controls.Add(lblQuality);
        tabYoutube.Controls.Add(cmbQuality);
        tabYoutube.Controls.Add(chkYoutubeDownloadSubtitles);
        tabYoutube.Controls.Add(btnAddQueue);
        tabYoutube.Controls.Add(lvQueue);
        tabYoutube.Controls.Add(btnRemoveSelected);
        tabYoutube.Controls.Add(lblStatus);
        tabYoutube.Controls.Add(pbYoutube);
        tabYoutube.Controls.Add(lblYoutubeSavePath);
        tabYoutube.Controls.Add(btnOpenYoutubeFolder);
        tabYoutube.Location = new Point(4, 5);
        tabYoutube.Name = "tabYoutube";
        tabYoutube.Padding = new Padding(3);
        tabYoutube.Size = new Size(612, 591);
        tabYoutube.TabIndex = 0;
        // 
        // lblUrl
        // 
        lblUrl.AutoSize = true;
        lblUrl.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblUrl.ForeColor = Color.FromArgb(80, 80, 80);
        lblUrl.Location = new Point(20, 20);
        lblUrl.Name = "lblUrl";
        lblUrl.Size = new Size(113, 19);
        lblUrl.TabIndex = 1;
        lblUrl.Text = "유튜브 URL 입력";
        // 
        // panelUrlContainer
        // 
        panelUrlContainer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        panelUrlContainer.BorderStyle = BorderStyle.FixedSingle;
        panelUrlContainer.Controls.Add(txtUrl);
        panelUrlContainer.Location = new Point(20, 45);
        panelUrlContainer.Name = "panelUrlContainer";
        panelUrlContainer.Size = new Size(440, 45);
        panelUrlContainer.TabIndex = 2;
        // 
        // txtUrl
        // 
        txtUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtUrl.BorderStyle = BorderStyle.None;
        txtUrl.Font = new Font("Segoe UI", 11F);
        txtUrl.Location = new Point(15, 13);
        txtUrl.Name = "txtUrl";
        txtUrl.PlaceholderText = "https://www.youtube.com/watch?v=...";
        txtUrl.Size = new Size(410, 20);
        txtUrl.TabIndex = 0;
        // 
        // btnLoad
        // 
        btnLoad.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnLoad.BackColor = Color.FromArgb(2, 132, 199);
        btnLoad.BorderRadius = 18;
        btnLoad.FlatAppearance.BorderSize = 0;
        btnLoad.FlatStyle = FlatStyle.Flat;
        btnLoad.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnLoad.ForeColor = Color.White;
        btnLoad.Location = new Point(470, 45);
        btnLoad.Name = "btnLoad";
        btnLoad.Size = new Size(120, 45);
        btnLoad.TabIndex = 3;
        btnLoad.Text = "영상 확인 🔍";
        btnLoad.UseVisualStyleBackColor = false;
        btnLoad.Click += BtnLoad_Click;
        // 
        // panelInfo
        // 
        panelInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        panelInfo.BackColor = Color.White;
        panelInfo.Controls.Add(lblVideoTitle);
        panelInfo.Controls.Add(txtEditTitle);
        panelInfo.Controls.Add(picThumbnail);
        panelInfo.Location = new Point(20, 110);
        panelInfo.Name = "panelInfo";
        panelInfo.Size = new Size(570, 100);
        panelInfo.TabIndex = 4;
        panelInfo.Paint += Panel_Paint;
        // 
        // lblVideoTitle
        // 
        lblVideoTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblVideoTitle.Font = new Font("Segoe UI", 10F);
        lblVideoTitle.ForeColor = Color.FromArgb(40, 40, 40);
        lblVideoTitle.Location = new Point(170, 15);
        lblVideoTitle.Name = "lblVideoTitle";
        lblVideoTitle.Size = new Size(390, 70);
        lblVideoTitle.TabIndex = 1;
        lblVideoTitle.Text = "위 입력칸에 유튜브 URL을 붙여넣고 영상 확인을 클릭하세요.";
        lblVideoTitle.DoubleClick += LblVideoTitle_DoubleClick;
        // 
        // txtEditTitle
        // 
        txtEditTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtEditTitle.Font = new Font("Segoe UI", 10F);
        txtEditTitle.Location = new Point(170, 15);
        txtEditTitle.Name = "txtEditTitle";
        txtEditTitle.Size = new Size(380, 25);
        txtEditTitle.TabIndex = 2;
        txtEditTitle.Visible = false;
        txtEditTitle.KeyDown += TxtEditTitle_KeyDown;
        txtEditTitle.LostFocus += TxtEditTitle_LostFocus;
        // 
        // picThumbnail
        // 
        picThumbnail.BackColor = Color.FromArgb(240, 240, 240);
        picThumbnail.Location = new Point(10, 10);
        picThumbnail.Name = "picThumbnail";
        picThumbnail.Size = new Size(142, 80);
        picThumbnail.SizeMode = PictureBoxSizeMode.Zoom;
        picThumbnail.TabIndex = 0;
        picThumbnail.TabStop = false;
        // 
        // lblQuality
        // 
        lblQuality.AutoSize = true;
        lblQuality.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblQuality.ForeColor = Color.FromArgb(80, 80, 80);
        lblQuality.Location = new Point(20, 225);
        lblQuality.Name = "lblQuality";
        lblQuality.Size = new Size(69, 19);
        lblQuality.TabIndex = 5;
        lblQuality.Text = "화질 옵션";
        // 
        // cmbQuality
        // 
        cmbQuality.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        cmbQuality.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbQuality.Enabled = false;
        cmbQuality.Font = new Font("Segoe UI", 11F);
        cmbQuality.FormattingEnabled = true;
        cmbQuality.Location = new Point(100, 221);
        cmbQuality.Name = "cmbQuality";
        cmbQuality.Size = new Size(360, 28);
        cmbQuality.TabIndex = 6;
        // 
        // chkYoutubeDownloadSubtitles
        // 
        chkYoutubeDownloadSubtitles.AutoSize = true;
        chkYoutubeDownloadSubtitles.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        chkYoutubeDownloadSubtitles.ForeColor = Color.FromArgb(80, 80, 80);
        chkYoutubeDownloadSubtitles.Location = new Point(100, 252);
        chkYoutubeDownloadSubtitles.Name = "chkYoutubeDownloadSubtitles";
        chkYoutubeDownloadSubtitles.Size = new Size(150, 19);
        chkYoutubeDownloadSubtitles.TabIndex = 14;
        chkYoutubeDownloadSubtitles.Text = "\uC790\uB9C9\uB3C4 \uD568\uAED8 \uB2E4\uC6B4\uB85C\uB4DC";
        chkYoutubeDownloadSubtitles.UseVisualStyleBackColor = true;
        // 
        // btnAddQueue
        // 
        btnAddQueue.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnAddQueue.BackColor = Color.FromArgb(2, 132, 199);
        btnAddQueue.BorderRadius = 15;
        btnAddQueue.FlatAppearance.BorderSize = 0;
        btnAddQueue.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnAddQueue.ForeColor = Color.White;
        btnAddQueue.Location = new Point(470, 216);
        btnAddQueue.Name = "btnAddQueue";
        btnAddQueue.Size = new Size(120, 38);
        btnAddQueue.TabIndex = 7;
        btnAddQueue.Text = "다운로드 📥";
        btnAddQueue.UseVisualStyleBackColor = false;
        btnAddQueue.Click += BtnAddQueue_Click;
        // 
        // lvQueue
        // 
        lvQueue.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        lvQueue.BackColor = Color.White;
        lvQueue.BorderStyle = BorderStyle.None;
        lvQueue.Columns.AddRange(new ColumnHeader[] { colTitle, colQuality, colStatus });
        lvQueue.ContextMenuStrip = contextMenuRemove;
        lvQueue.Font = new Font("Segoe UI", 9.5F);
        lvQueue.FullRowSelect = true;
        lvQueue.Location = new Point(20, 275);
        lvQueue.Name = "lvQueue";
        lvQueue.Size = new Size(570, 180);
        lvQueue.TabIndex = 8;
        lvQueue.UseCompatibleStateImageBehavior = false;
        lvQueue.View = View.Details;
        // 
        // colTitle
        // 
        colTitle.Text = "영상 제목";
        colTitle.Width = 320;
        // 
        // colQuality
        // 
        colQuality.Text = "화질/포맷";
        colQuality.Width = 120;
        // 
        // colStatus
        // 
        colStatus.Text = "상태";
        colStatus.Width = 110;
        // 
        // contextMenuRemove
        // 
        contextMenuRemove.Items.AddRange(new ToolStripItem[] { menuRemoveSelected });
        contextMenuRemove.Name = "contextMenuRemove";
        contextMenuRemove.Size = new Size(203, 26);
        // 
        // menuRemoveSelected
        // 
        menuRemoveSelected.Name = "menuRemoveSelected";
        menuRemoveSelected.Size = new Size(202, 22);
        menuRemoveSelected.Text = "선택 항목 취소 (지우기)";
        menuRemoveSelected.Click += BtnRemoveSelected_Click;
        // 
        // btnRemoveSelected
        // 
        btnRemoveSelected.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnRemoveSelected.BackColor = Color.FromArgb(255, 241, 242);
        btnRemoveSelected.BorderRadius = 14;
        btnRemoveSelected.FlatAppearance.BorderSize = 0;
        btnRemoveSelected.FlatStyle = FlatStyle.Flat;
        btnRemoveSelected.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnRemoveSelected.ForeColor = Color.FromArgb(225, 29, 72);
        btnRemoveSelected.Location = new Point(470, 465);
        btnRemoveSelected.Name = "btnRemoveSelected";
        btnRemoveSelected.Size = new Size(120, 28);
        btnRemoveSelected.TabIndex = 9;
        btnRemoveSelected.Text = "항목 삭제 🗑️";
        btnRemoveSelected.UseVisualStyleBackColor = false;
        btnRemoveSelected.Click += BtnRemoveSelected_Click;
        // 
        // lblStatus
        // 
        lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lblStatus.Font = new Font("Segoe UI", 9F);
        lblStatus.ForeColor = Color.DarkGray;
        lblStatus.Location = new Point(20, 470);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(430, 20);
        lblStatus.TabIndex = 9;
        lblStatus.Text = "* 대기열 항목 우클릭으로도 취소가 가능합니다.";
        // 
        // pbYoutube
        // 
        pbYoutube.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        pbYoutube.Location = new Point(20, 500);
        pbYoutube.Name = "pbYoutube";
        pbYoutube.Size = new Size(570, 20);
        pbYoutube.Style = ProgressBarStyle.Continuous;
        pbYoutube.TabIndex = 11;
        // 
        // lblYoutubeSavePath
        // 
        lblYoutubeSavePath.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        lblYoutubeSavePath.AutoSize = true;
        lblYoutubeSavePath.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblYoutubeSavePath.ForeColor = Color.FromArgb(80, 80, 80);
        lblYoutubeSavePath.Location = new Point(20, 532);
        lblYoutubeSavePath.Name = "lblYoutubeSavePath";
        lblYoutubeSavePath.TabIndex = 12;
        lblYoutubeSavePath.Text = "현재 저장 위치: ";
        // 
        // btnOpenYoutubeFolder
        // 
        btnOpenYoutubeFolder.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        btnOpenYoutubeFolder.BackColor = Color.FromArgb(226, 232, 240);
        btnOpenYoutubeFolder.BorderRadius = 13;
        btnOpenYoutubeFolder.FlatAppearance.BorderSize = 0;
        btnOpenYoutubeFolder.FlatStyle = FlatStyle.Flat;
        btnOpenYoutubeFolder.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        btnOpenYoutubeFolder.ForeColor = Color.FromArgb(51, 65, 85);
        btnOpenYoutubeFolder.Location = new Point(20, 550);
        btnOpenYoutubeFolder.Name = "btnOpenYoutubeFolder";
        btnOpenYoutubeFolder.Size = new Size(80, 26);
        btnOpenYoutubeFolder.TabIndex = 13;
        btnOpenYoutubeFolder.Text = "폴더 열기 📂";
        btnOpenYoutubeFolder.UseVisualStyleBackColor = false;
        btnOpenYoutubeFolder.Click += BtnOpenYoutubeFolder_Click;
        // 
        // tabYtDlp
        // 
        tabYtDlp.BackColor = Color.FromArgb(250, 250, 250);
        tabYtDlp.Controls.Add(lblXPrivateMode);
        tabYtDlp.Controls.Add(tglXPrivateMode);
        tabYtDlp.Controls.Add(lblInstaPrivateMode);
        tabYtDlp.Controls.Add(tglInstaPrivateMode);
        tabYtDlp.Controls.Add(panelXBrowser);
        tabYtDlp.Controls.Add(lblYtDlpSavePath);
        tabYtDlp.Controls.Add(btnOpenYtDlpFolder);
        tabYtDlp.Controls.Add(lblYtDlpTitle);
        tabYtDlp.Controls.Add(lblYtDlpDesc);
        tabYtDlp.Controls.Add(panelYtDlpUrl);
        tabYtDlp.Controls.Add(chkYtDlpDownloadSubtitles);
        tabYtDlp.Controls.Add(btnYtDlpRun);
        tabYtDlp.Controls.Add(btnYtDlpCancel);
        tabYtDlp.Controls.Add(lblYtDlpStatus);
        tabYtDlp.Controls.Add(pbYtDlp);
        tabYtDlp.Controls.Add(lvYtDlpQueue);
        tabYtDlp.Controls.Add(btnRemoveSelectedYtDlp);
        tabYtDlp.Controls.Add(btnYtDlpLoginBrowser);
        tabYtDlp.Location = new Point(4, 5);
        tabYtDlp.Name = "tabYtDlp";
        tabYtDlp.Size = new Size(612, 551);
        tabYtDlp.TabIndex = 6;
        // 
        // lblXPrivateMode
        // 
        lblXPrivateMode.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lblXPrivateMode.AutoSize = true;
        lblXPrivateMode.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblXPrivateMode.Location = new Point(345, 192);
        lblXPrivateMode.Name = "lblXPrivateMode";
        lblXPrivateMode.Size = new Size(150, 17);
        lblXPrivateMode.TabIndex = 0;
        lblXPrivateMode.Text = "X 비공개 영상 전용 모드";
        lblXPrivateMode.Visible = false;
        // 
        // tglXPrivateMode
        // 
        tglXPrivateMode.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        tglXPrivateMode.Location = new Point(530, 188);
        tglXPrivateMode.Name = "tglXPrivateMode";
        tglXPrivateMode.Padding = new Padding(6);
        tglXPrivateMode.Size = new Size(60, 25);
        tglXPrivateMode.TabIndex = 1;
        tglXPrivateMode.Visible = false;
        tglXPrivateMode.CheckedChanged += TglXPrivateMode_CheckedChanged;
        // 
        // lblInstaPrivateMode
        // 
        lblInstaPrivateMode.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lblInstaPrivateMode.AutoSize = true;
        lblInstaPrivateMode.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblInstaPrivateMode.Location = new Point(345, 222);
        lblInstaPrivateMode.Name = "lblInstaPrivateMode";
        lblInstaPrivateMode.Size = new Size(160, 17);
        lblInstaPrivateMode.TabIndex = 2;
        lblInstaPrivateMode.Text = "Instagram 실패 시 로그인";
        lblInstaPrivateMode.Visible = false;
        // 
        // tglInstaPrivateMode
        // 
        tglInstaPrivateMode.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        tglInstaPrivateMode.Location = new Point(530, 218);
        tglInstaPrivateMode.Name = "tglInstaPrivateMode";
        tglInstaPrivateMode.Padding = new Padding(6);
        tglInstaPrivateMode.Size = new Size(60, 25);
        tglInstaPrivateMode.TabIndex = 3;
        tglInstaPrivateMode.Visible = false;
        tglInstaPrivateMode.CheckedChanged += TglInstaPrivateMode_CheckedChanged;
        // 
        // panelXBrowser
        // 
        panelXBrowser.BackColor = Color.White;
        panelXBrowser.Controls.Add(webViewX);
        panelXBrowser.Controls.Add(btnXCapture);
        panelXBrowser.Controls.Add(btnXDownload);
        panelXBrowser.Controls.Add(btnXClose);
        panelXBrowser.Controls.Add(pbXDownload);
        panelXBrowser.Controls.Add(lblXStatus);
        panelXBrowser.Dock = DockStyle.Fill;
        panelXBrowser.Location = new Point(0, 0);
        panelXBrowser.Name = "panelXBrowser";
        panelXBrowser.Size = new Size(200, 100);
        panelXBrowser.TabIndex = 2;
        panelXBrowser.Visible = false;
        // 
        // webViewX
        // 
        webViewX.AllowExternalDrop = true;
        webViewX.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        webViewX.CreationProperties = null;
        webViewX.DefaultBackgroundColor = Color.White;
        webViewX.Location = new Point(0, 50);
        webViewX.Name = "webViewX";
        webViewX.Size = new Size(570, 461);
        webViewX.TabIndex = 0;
        webViewX.ZoomFactor = 1D;
        // 
        // btnXCapture
        // 
        btnXCapture.BackColor = Color.FromArgb(52, 152, 219);
        btnXCapture.FlatAppearance.BorderSize = 0;
        btnXCapture.FlatStyle = FlatStyle.Flat;
        btnXCapture.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnXCapture.ForeColor = Color.White;
        btnXCapture.Location = new Point(10, 10);
        btnXCapture.Name = "btnXCapture";
        btnXCapture.Size = new Size(120, 32);
        btnXCapture.TabIndex = 1;
        btnXCapture.Text = "1 주소 가져오기";
        btnXCapture.UseVisualStyleBackColor = false;
        btnXCapture.Click += BtnXCapture_Click;
        // 
        // btnXDownload
        // 
        btnXDownload.BackColor = Color.FromArgb(155, 89, 182);
        btnXDownload.FlatAppearance.BorderSize = 0;
        btnXDownload.FlatStyle = FlatStyle.Flat;
        btnXDownload.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnXDownload.ForeColor = Color.White;
        btnXDownload.Location = new Point(140, 10);
        btnXDownload.Name = "btnXDownload";
        btnXDownload.Size = new Size(140, 32);
        btnXDownload.TabIndex = 2;
        btnXDownload.Text = "2 바로 다운";
        btnXDownload.UseVisualStyleBackColor = false;
        btnXDownload.Click += BtnXDownload_Click;
        // 
        // btnXClose
        // 
        btnXClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnXClose.BackColor = Color.FromArgb(149, 165, 166);
        btnXClose.FlatAppearance.BorderSize = 0;
        btnXClose.FlatStyle = FlatStyle.Flat;
        btnXClose.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnXClose.ForeColor = Color.White;
        btnXClose.Location = new Point(490, 10);
        btnXClose.Name = "btnXClose";
        btnXClose.Size = new Size(70, 32);
        btnXClose.TabIndex = 3;
        btnXClose.Text = "닫기";
        btnXClose.UseVisualStyleBackColor = false;
        btnXClose.Click += BtnXClose_Click;
        // 
        // pbXDownload
        // 
        pbXDownload.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        pbXDownload.Location = new Point(10, 520);
        pbXDownload.Name = "pbXDownload";
        pbXDownload.Size = new Size(550, 15);
        pbXDownload.Style = ProgressBarStyle.Continuous;
        pbXDownload.TabIndex = 4;
        // 
        // lblXStatus
        // 
        lblXStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lblXStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblXStatus.ForeColor = Color.FromArgb(155, 89, 182);
        lblXStatus.Location = new Point(10, 540);
        lblXStatus.Name = "lblXStatus";
        lblXStatus.Size = new Size(550, 20);
        lblXStatus.TabIndex = 5;
        lblXStatus.Text = "영상 페이지(status)로 이동해 주세요.";
        lblXStatus.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // lblYtDlpSavePath
        // 
        lblYtDlpSavePath.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        lblYtDlpSavePath.AutoSize = true;
        lblYtDlpSavePath.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblYtDlpSavePath.ForeColor = Color.FromArgb(80, 80, 80);
        lblYtDlpSavePath.Location = new Point(20, 505);
        lblYtDlpSavePath.Name = "lblYtDlpSavePath";
        lblYtDlpSavePath.Size = new Size(61, 15);
        lblYtDlpSavePath.TabIndex = 3;
        lblYtDlpSavePath.Text = "저장 위치: ";
        // 
        // btnOpenYtDlpFolder
        // 
        btnOpenYtDlpFolder.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        btnOpenYtDlpFolder.BackColor = Color.FromArgb(226, 232, 240);
        btnOpenYtDlpFolder.BorderRadius = 13;
        btnOpenYtDlpFolder.FlatAppearance.BorderSize = 0;
        btnOpenYtDlpFolder.FlatStyle = FlatStyle.Flat;
        btnOpenYtDlpFolder.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        btnOpenYtDlpFolder.ForeColor = Color.FromArgb(51, 65, 85);
        btnOpenYtDlpFolder.Location = new Point(20, 525);
        btnOpenYtDlpFolder.Name = "btnOpenYtDlpFolder";
        btnOpenYtDlpFolder.Size = new Size(80, 26);
        btnOpenYtDlpFolder.TabIndex = 13;
        btnOpenYtDlpFolder.Text = "폴더 열기 📂";
        btnOpenYtDlpFolder.UseVisualStyleBackColor = false;
        btnOpenYtDlpFolder.Click += BtnOpenYtDlpFolder_Click;
        // 
        // lblYtDlpTitle
        // 
        lblYtDlpTitle.AutoSize = true;
        lblYtDlpTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        lblYtDlpTitle.Location = new Point(20, 20);
        lblYtDlpTitle.Name = "lblYtDlpTitle";
        lblYtDlpTitle.Size = new Size(179, 30);
        lblYtDlpTitle.TabIndex = 4;
        lblYtDlpTitle.Text = "웹 사이트 영상 다운로드";
        // 
        // lblYtDlpDesc
        // 
        lblYtDlpDesc.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblYtDlpDesc.Font = new Font("Segoe UI", 9.5F);
        lblYtDlpDesc.Location = new Point(20, 60);
        lblYtDlpDesc.Name = "lblYtDlpDesc";
        lblYtDlpDesc.Size = new Size(570, 60);
        lblYtDlpDesc.TabIndex = 5;
        lblYtDlpDesc.Text = "치지직, Instagram, TikTok, SOOP, Pinterest, X, Vimeo 등 다양한 사이트를 지원합니다.\n비공개/일부공개/나이제한 영상은 [로그인하고 받기]에서 로그인 후 즉시 다운로드하거나 URL로 받을 수 있습니다.";
        // 
        // panelYtDlpUrl
        // 
        panelYtDlpUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        panelYtDlpUrl.BackColor = Color.White;
        panelYtDlpUrl.BorderStyle = BorderStyle.FixedSingle;
        panelYtDlpUrl.Controls.Add(txtYtDlpUrl);
        panelYtDlpUrl.Location = new Point(20, 130);
        panelYtDlpUrl.Name = "panelYtDlpUrl";
        panelYtDlpUrl.Size = new Size(570, 40);
        panelYtDlpUrl.TabIndex = 6;
        panelYtDlpUrl.Paint += Panel_Paint;
        // 
        // txtYtDlpUrl
        // 
        txtYtDlpUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtYtDlpUrl.BorderStyle = BorderStyle.None;
        txtYtDlpUrl.Font = new Font("Segoe UI", 11F);
        txtYtDlpUrl.Location = new Point(10, 10);
        txtYtDlpUrl.Name = "txtYtDlpUrl";
        txtYtDlpUrl.PlaceholderText = "다운로드할 영상의 URL을 입력하세요...";
        txtYtDlpUrl.Size = new Size(550, 20);
        txtYtDlpUrl.TabIndex = 0;
        // 
        // chkYtDlpDownloadSubtitles
        // 
        chkYtDlpDownloadSubtitles.AutoSize = true;
        chkYtDlpDownloadSubtitles.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        chkYtDlpDownloadSubtitles.ForeColor = Color.FromArgb(80, 80, 80);
        chkYtDlpDownloadSubtitles.Location = new Point(20, 230);
        chkYtDlpDownloadSubtitles.Name = "chkYtDlpDownloadSubtitles";
        chkYtDlpDownloadSubtitles.Size = new Size(150, 19);
        chkYtDlpDownloadSubtitles.TabIndex = 14;
        chkYtDlpDownloadSubtitles.Text = "\uC790\uB9C9\uB3C4 \uD568\uAED8 \uB2E4\uC6B4\uB85C\uB4DC";
        chkYtDlpDownloadSubtitles.UseVisualStyleBackColor = true;
        // 
        // btnYtDlpRun
        // 
        btnYtDlpRun.BackColor = Color.FromArgb(2, 132, 199);
        btnYtDlpRun.BorderRadius = 15;
        btnYtDlpRun.FlatAppearance.BorderSize = 0;
        btnYtDlpRun.FlatStyle = FlatStyle.Flat;
        btnYtDlpRun.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnYtDlpRun.ForeColor = Color.White;
        btnYtDlpRun.Location = new Point(20, 182);
        btnYtDlpRun.Name = "btnYtDlpRun";
        btnYtDlpRun.Size = new Size(150, 38);
        btnYtDlpRun.TabIndex = 7;
        btnYtDlpRun.Text = "\uB2E4\uC6B4\uB85C\uB4DC";
        btnYtDlpRun.UseVisualStyleBackColor = false;
        btnYtDlpRun.Click += BtnYtDlpRun_Click;
        // 
        // btnYtDlpCancel
        // 
        btnYtDlpCancel.BackColor = Color.FromArgb(255, 241, 242);
        btnYtDlpCancel.BorderRadius = 15;
        btnYtDlpCancel.FlatAppearance.BorderSize = 0;
        btnYtDlpCancel.FlatStyle = FlatStyle.Flat;
        btnYtDlpCancel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnYtDlpCancel.ForeColor = Color.FromArgb(225, 29, 72);
        btnYtDlpCancel.Location = new Point(398, 182);
        btnYtDlpCancel.Name = "btnYtDlpCancel";
        btnYtDlpCancel.Size = new Size(120, 38);
        btnYtDlpCancel.TabIndex = 8;
        btnYtDlpCancel.Text = "중단 ✖";
        btnYtDlpCancel.UseVisualStyleBackColor = false;
        btnYtDlpCancel.Visible = false;
        btnYtDlpCancel.Click += BtnYtDlpCancel_Click;
        // 
        // lblYtDlpStatus
        // 
        lblYtDlpStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblYtDlpStatus.Location = new Point(20, 295);
        lblYtDlpStatus.Name = "lblYtDlpStatus";
        lblYtDlpStatus.Size = new Size(570, 45);
        lblYtDlpStatus.TabIndex = 9;
        lblYtDlpStatus.Text = "대기 중...";
        // 
        // pbYtDlp
        // 
        pbYtDlp.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        pbYtDlp.Location = new Point(20, 350);
        pbYtDlp.Name = "pbYtDlp";
        pbYtDlp.Size = new Size(570, 20);
        pbYtDlp.Style = ProgressBarStyle.Continuous;
        pbYtDlp.TabIndex = 10;
        // 
        // lvYtDlpQueue
        // 
        lvYtDlpQueue.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        lvYtDlpQueue.BackColor = Color.White;
        lvYtDlpQueue.BorderStyle = BorderStyle.None;
        lvYtDlpQueue.Columns.AddRange(new ColumnHeader[] { colYtDlpUrl, colYtDlpSubtitles, colYtDlpStatus });
        lvYtDlpQueue.ContextMenuStrip = contextMenuYtDlpRemove;
        lvYtDlpQueue.Font = new Font("Segoe UI", 9.5F);
        lvYtDlpQueue.FullRowSelect = true;
        lvYtDlpQueue.GridLines = false;
        lvYtDlpQueue.Location = new Point(20, 380);
        lvYtDlpQueue.Name = "lvYtDlpQueue";
        lvYtDlpQueue.Size = new Size(570, 110);
        lvYtDlpQueue.TabIndex = 15;
        lvYtDlpQueue.UseCompatibleStateImageBehavior = false;
        lvYtDlpQueue.View = View.Details;
        // 
        // colYtDlpUrl
        // 
        colYtDlpUrl.Text = "URL";
        colYtDlpUrl.Width = 285;
        // 
        // colYtDlpSubtitles
        // 
        colYtDlpSubtitles.Text = "\uC790\uB9C9";
        colYtDlpSubtitles.Width = 55;
        // 
        // colYtDlpStatus
        // 
        colYtDlpStatus.Text = "\uC0C1\uD0DC";
        colYtDlpStatus.Width = 220;
        // 
        // btnRemoveSelectedYtDlp
        // 
        btnRemoveSelectedYtDlp.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnRemoveSelectedYtDlp.BackColor = Color.FromArgb(255, 241, 242);
        btnRemoveSelectedYtDlp.BorderRadius = 14;
        btnRemoveSelectedYtDlp.FlatAppearance.BorderSize = 0;
        btnRemoveSelectedYtDlp.FlatStyle = FlatStyle.Flat;
        btnRemoveSelectedYtDlp.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnRemoveSelectedYtDlp.ForeColor = Color.FromArgb(225, 29, 72);
        btnRemoveSelectedYtDlp.Location = new Point(470, 500);
        btnRemoveSelectedYtDlp.Name = "btnRemoveSelectedYtDlp";
        btnRemoveSelectedYtDlp.Size = new Size(120, 28);
        btnRemoveSelectedYtDlp.TabIndex = 16;
        btnRemoveSelectedYtDlp.Text = "\uC120\uD0DD \uCDE8\uC18C";
        btnRemoveSelectedYtDlp.UseVisualStyleBackColor = false;
        btnRemoveSelectedYtDlp.Click += BtnRemoveSelectedYtDlp_Click;
        // 
        // contextMenuYtDlpRemove
        // 
        contextMenuYtDlpRemove.Items.AddRange(new ToolStripItem[] { menuYtDlpRemoveSelected });
        contextMenuYtDlpRemove.Name = "contextMenuYtDlpRemove";
        contextMenuYtDlpRemove.Size = new Size(179, 26);
        // 
        // menuYtDlpRemoveSelected
        // 
        menuYtDlpRemoveSelected.Name = "menuYtDlpRemoveSelected";
        menuYtDlpRemoveSelected.Size = new Size(178, 22);
        menuYtDlpRemoveSelected.Text = "\uC120\uD0DD \uD56D\uBAA9 \uCDE8\uC18C";
        menuYtDlpRemoveSelected.Click += BtnRemoveSelectedYtDlp_Click;
        // 
        // btnYtDlpLoginBrowser
        // 
        btnYtDlpLoginBrowser.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnYtDlpLoginBrowser.BackColor = Color.FromArgb(15, 118, 110);
        btnYtDlpLoginBrowser.BorderRadius = 15;
        btnYtDlpLoginBrowser.FlatAppearance.BorderSize = 0;
        btnYtDlpLoginBrowser.FlatStyle = FlatStyle.Flat;
        btnYtDlpLoginBrowser.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnYtDlpLoginBrowser.ForeColor = Color.White;
        btnYtDlpLoginBrowser.Location = new Point(180, 182);
        btnYtDlpLoginBrowser.Name = "btnYtDlpLoginBrowser";
        btnYtDlpLoginBrowser.Size = new Size(180, 38);
        btnYtDlpLoginBrowser.TabIndex = 17;
        btnYtDlpLoginBrowser.Text = "\uB85C\uADF8\uC778\uD558\uACE0 \uBC1B\uAE30";
        btnYtDlpLoginBrowser.UseVisualStyleBackColor = false;
        btnYtDlpLoginBrowser.Click += BtnYtDlpLoginBrowser_Click;
        // 
        // tabWebM
        // 
        tabWebM.BackColor = Color.FromArgb(250, 250, 250);
        tabWebM.Controls.Add(lblWebMTitle);
        tabWebM.Controls.Add(panelWebMInput);
        tabWebM.Controls.Add(btnBrowseWebM);
        tabWebM.Controls.Add(panelWebMOutput);
        tabWebM.Controls.Add(btnBrowseWebMOutput);
        tabWebM.Controls.Add(btnConvertWebM);
        tabWebM.Controls.Add(btnCancelWebM);
        tabWebM.Controls.Add(cmbWebMFormat);
        tabWebM.Controls.Add(lblWebMStatus);
        tabWebM.Controls.Add(pbWebM);
        tabWebM.Controls.Add(lblWebMSavePath);
        tabWebM.Controls.Add(btnOpenWebMFolder);
        tabWebM.Location = new Point(4, 5);
        tabWebM.Name = "tabWebM";
        tabWebM.Size = new Size(612, 551);
        tabWebM.TabIndex = 1;
        // 
        // lblWebMTitle
        // 
        lblWebMTitle.AutoSize = true;
        lblWebMTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        lblWebMTitle.Location = new Point(20, 20);
        lblWebMTitle.Name = "lblWebMTitle";
        lblWebMTitle.Size = new Size(179, 30);
        lblWebMTitle.TabIndex = 0;
        lblWebMTitle.Text = "영상 포맷 변환기";
        // 
        // panelWebMInput
        // 
        panelWebMInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        panelWebMInput.BackColor = Color.White;
        panelWebMInput.Controls.Add(txtWebMInput);
        panelWebMInput.Location = new Point(20, 80);
        panelWebMInput.Name = "panelWebMInput";
        panelWebMInput.Size = new Size(450, 40);
        panelWebMInput.TabIndex = 1;
        panelWebMInput.Paint += Panel_Paint;
        // 
        // txtWebMInput
        // 
        txtWebMInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtWebMInput.BorderStyle = BorderStyle.None;
        txtWebMInput.Font = new Font("Segoe UI", 11F);
        txtWebMInput.Location = new Point(10, 10);
        txtWebMInput.Name = "txtWebMInput";
        txtWebMInput.PlaceholderText = "MP4 파일 경로를 선택하세요...";
        txtWebMInput.Size = new Size(430, 20);
        txtWebMInput.TabIndex = 0;
        // 
        // btnBrowseWebM
        // 
        btnBrowseWebM.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseWebM.BackColor = Color.FromArgb(226, 232, 240);
        btnBrowseWebM.BorderRadius = 20;
        btnBrowseWebM.FlatAppearance.BorderSize = 0;
        btnBrowseWebM.FlatStyle = FlatStyle.Flat;
        btnBrowseWebM.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnBrowseWebM.ForeColor = Color.FromArgb(71, 85, 105);
        btnBrowseWebM.Location = new Point(490, 80);
        btnBrowseWebM.Name = "btnBrowseWebM";
        btnBrowseWebM.Size = new Size(100, 40);
        btnBrowseWebM.TabIndex = 2;
        btnBrowseWebM.Text = "파일 선택";
        btnBrowseWebM.UseVisualStyleBackColor = false;
        btnBrowseWebM.Click += BtnBrowseWebM_Click;
        // 
        // panelWebMOutput
        // 
        panelWebMOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        panelWebMOutput.BackColor = Color.White;
        panelWebMOutput.Controls.Add(txtWebMOutput);
        panelWebMOutput.Location = new Point(20, 130);
        panelWebMOutput.Name = "panelWebMOutput";
        panelWebMOutput.Size = new Size(450, 40);
        panelWebMOutput.TabIndex = 3;
        panelWebMOutput.Paint += Panel_Paint;
        // 
        // txtWebMOutput
        // 
        txtWebMOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtWebMOutput.BorderStyle = BorderStyle.None;
        txtWebMOutput.Font = new Font("Segoe UI", 11F);
        txtWebMOutput.Location = new Point(10, 10);
        txtWebMOutput.Name = "txtWebMOutput";
        txtWebMOutput.PlaceholderText = "저장 위치 (기본: 원본 폴더)...";
        txtWebMOutput.Size = new Size(430, 20);
        txtWebMOutput.TabIndex = 0;
        // 
        // btnBrowseWebMOutput
        // 
        btnBrowseWebMOutput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseWebMOutput.BackColor = Color.FromArgb(226, 232, 240);
        btnBrowseWebMOutput.BorderRadius = 20;
        btnBrowseWebMOutput.FlatAppearance.BorderSize = 0;
        btnBrowseWebMOutput.FlatStyle = FlatStyle.Flat;
        btnBrowseWebMOutput.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnBrowseWebMOutput.ForeColor = Color.FromArgb(71, 85, 105);
        btnBrowseWebMOutput.Location = new Point(490, 130);
        btnBrowseWebMOutput.Name = "btnBrowseWebMOutput";
        btnBrowseWebMOutput.Size = new Size(100, 40);
        btnBrowseWebMOutput.TabIndex = 4;
        btnBrowseWebMOutput.Text = "위치 설정";
        btnBrowseWebMOutput.UseVisualStyleBackColor = false;
        btnBrowseWebMOutput.Click += BtnBrowseWebMOutput_Click;
        // 
        // btnConvertWebM
        // 
        btnConvertWebM.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        btnConvertWebM.BackColor = Color.FromArgb(16, 185, 129);
        btnConvertWebM.BorderRadius = 15;
        btnConvertWebM.FlatAppearance.BorderSize = 0;
        btnConvertWebM.FlatStyle = FlatStyle.Flat;
        btnConvertWebM.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        btnConvertWebM.ForeColor = Color.White;
        btnConvertWebM.Location = new Point(20, 230);
        btnConvertWebM.Name = "btnConvertWebM";
        btnConvertWebM.Size = new Size(440, 50);
        btnConvertWebM.TabIndex = 5;
        btnConvertWebM.Text = "포맷 변환 시작 🔄";
        btnConvertWebM.UseVisualStyleBackColor = false;
        btnConvertWebM.Click += BtnConvertWebM_Click;
        // 
        // btnCancelWebM
        // 
        btnCancelWebM.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCancelWebM.BackColor = Color.FromArgb(255, 241, 242);
        btnCancelWebM.BorderRadius = 15;
        btnCancelWebM.FlatAppearance.BorderSize = 0;
        btnCancelWebM.FlatStyle = FlatStyle.Flat;
        btnCancelWebM.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        btnCancelWebM.ForeColor = Color.FromArgb(225, 29, 72);
        btnCancelWebM.Location = new Point(470, 230);
        btnCancelWebM.Name = "btnCancelWebM";
        btnCancelWebM.Size = new Size(120, 50);
        btnCancelWebM.TabIndex = 6;
        btnCancelWebM.Text = "취소 ✖";
        btnCancelWebM.UseVisualStyleBackColor = false;
        btnCancelWebM.Visible = false;
        btnCancelWebM.Click += BtnCancelWebM_Click;
        // 
        // cmbWebMFormat
        // 
        cmbWebMFormat.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        cmbWebMFormat.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbWebMFormat.Font = new Font("Segoe UI", 11F);
        cmbWebMFormat.FormattingEnabled = true;
        cmbWebMFormat.Items.AddRange(new object[] { "GIF (.gif)", "WebM (.webm)", "JPG Sequence (.jpg)", "PNG Sequence (.png)", "MOV (.mov)", "MKV (.mkv)", "AVI (.avi)", "WMV (.wmv)" });
        cmbWebMFormat.Location = new Point(20, 180);
        cmbWebMFormat.Name = "cmbWebMFormat";
        cmbWebMFormat.Size = new Size(570, 28);
        cmbWebMFormat.TabIndex = 7;
        // 
        // lblWebMStatus
        // 
        lblWebMStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblWebMStatus.Location = new Point(20, 295);
        lblWebMStatus.Name = "lblWebMStatus";
        lblWebMStatus.Size = new Size(570, 45);
        lblWebMStatus.TabIndex = 8;
        lblWebMStatus.Text = "대기 중...";
        // 
        // pbWebM
        // 
        pbWebM.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        pbWebM.Location = new Point(20, 350);
        pbWebM.Name = "pbWebM";
        pbWebM.Size = new Size(570, 20);
        pbWebM.Style = ProgressBarStyle.Continuous;
        pbWebM.TabIndex = 10;
        // 
        // lblWebMSavePath
        // 
        lblWebMSavePath.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        lblWebMSavePath.AutoSize = true;
        lblWebMSavePath.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblWebMSavePath.ForeColor = Color.FromArgb(80, 80, 80);
        lblWebMSavePath.Location = new Point(20, 385);
        lblWebMSavePath.Name = "lblWebMSavePath";
        lblWebMSavePath.TabIndex = 11;
        lblWebMSavePath.Text = "현재 저장 위치: ";
        // 
        // btnOpenWebMFolder
        // 
        btnOpenWebMFolder.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnOpenWebMFolder.BackColor = Color.FromArgb(226, 232, 240);
        btnOpenWebMFolder.BorderRadius = 13;
        btnOpenWebMFolder.FlatAppearance.BorderSize = 0;
        btnOpenWebMFolder.FlatStyle = FlatStyle.Flat;
        btnOpenWebMFolder.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        btnOpenWebMFolder.ForeColor = Color.FromArgb(51, 65, 85);
        btnOpenWebMFolder.Location = new Point(20, 410);
        btnOpenWebMFolder.Name = "btnOpenWebMFolder";
        btnOpenWebMFolder.Size = new Size(80, 26);
        btnOpenWebMFolder.TabIndex = 12;
        btnOpenWebMFolder.Text = "폴더 열기 📂";
        btnOpenWebMFolder.UseVisualStyleBackColor = false;
        btnOpenWebMFolder.Click += BtnOpenWebMFolder_Click;
        // 
        // tabCodec
        // 
        tabCodec.BackColor = Color.FromArgb(250, 250, 250);
        tabCodec.Controls.Add(lblCodecTitle);
        tabCodec.Controls.Add(lblCodecDesc);
        tabCodec.Controls.Add(panelCodecInput);
        tabCodec.Controls.Add(btnBrowseCodec);
        tabCodec.Controls.Add(panelCodecOutput);
        tabCodec.Controls.Add(btnBrowseCodecOutput);
        tabCodec.Controls.Add(btnConvertCodec);
        tabCodec.Controls.Add(btnCancelCodec);
        tabCodec.Controls.Add(lblCodecStatus);
        tabCodec.Controls.Add(pbCodec);
        tabCodec.Controls.Add(lblCodecSavePath);
        tabCodec.Controls.Add(btnOpenCodecFolder);
        tabCodec.Location = new Point(4, 5);
        tabCodec.Name = "tabCodec";
        tabCodec.Size = new Size(612, 551);
        tabCodec.TabIndex = 2;
        // 
        // lblCodecTitle
        // 
        lblCodecTitle.AutoSize = true;
        lblCodecTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        lblCodecTitle.Location = new Point(20, 20);
        lblCodecTitle.Name = "lblCodecTitle";
        lblCodecTitle.Size = new Size(405, 30);
        lblCodecTitle.TabIndex = 0;
        lblCodecTitle.Text = "프리미어 프로 / 에프터이펙트 코덱 해결";
        // 
        // lblCodecDesc
        // 
        lblCodecDesc.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblCodecDesc.Font = new Font("Segoe UI", 9.5F);
        lblCodecDesc.Location = new Point(20, 55);
        lblCodecDesc.Name = "lblCodecDesc";
        lblCodecDesc.Size = new Size(570, 40);
        lblCodecDesc.TabIndex = 1;
        lblCodecDesc.Text = "영상을 프리미어 프로나 에프터이펙트에 넣었을 때 화면이 안 나오고 소리만 나오는 현상을 해결합니다.\n(H.264 / AAC 코덱으로 재인코딩)";
        // 
        // panelCodecInput
        // 
        panelCodecInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        panelCodecInput.BackColor = Color.White;
        panelCodecInput.Controls.Add(txtCodecInput);
        panelCodecInput.Location = new Point(20, 135);
        panelCodecInput.Name = "panelCodecInput";
        panelCodecInput.Size = new Size(450, 40);
        panelCodecInput.TabIndex = 2;
        panelCodecInput.Paint += Panel_Paint;
        // 
        // txtCodecInput
        // 
        txtCodecInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtCodecInput.BorderStyle = BorderStyle.None;
        txtCodecInput.Font = new Font("Segoe UI", 11F);
        txtCodecInput.Location = new Point(10, 10);
        txtCodecInput.Name = "txtCodecInput";
        txtCodecInput.PlaceholderText = "원원 MP4 파일 경로를 선택하세요...";
        txtCodecInput.Size = new Size(430, 20);
        txtCodecInput.TabIndex = 0;
        // 
        // btnBrowseCodec
        // 
        btnBrowseCodec.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseCodec.BackColor = Color.FromArgb(226, 232, 240);
        btnBrowseCodec.BorderRadius = 20;
        btnBrowseCodec.FlatAppearance.BorderSize = 0;
        btnBrowseCodec.FlatStyle = FlatStyle.Flat;
        btnBrowseCodec.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnBrowseCodec.ForeColor = Color.FromArgb(71, 85, 105);
        btnBrowseCodec.Location = new Point(490, 135);
        btnBrowseCodec.Name = "btnBrowseCodec";
        btnBrowseCodec.Size = new Size(100, 40);
        btnBrowseCodec.TabIndex = 3;
        btnBrowseCodec.Text = "파일 선택";
        btnBrowseCodec.UseVisualStyleBackColor = false;
        btnBrowseCodec.Click += BtnBrowseCodec_Click;
        // 
        // panelCodecOutput
        // 
        panelCodecOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        panelCodecOutput.BackColor = Color.White;
        panelCodecOutput.Controls.Add(txtCodecOutput);
        panelCodecOutput.Location = new Point(20, 185);
        panelCodecOutput.Name = "panelCodecOutput";
        panelCodecOutput.Size = new Size(450, 40);
        panelCodecOutput.TabIndex = 4;
        panelCodecOutput.Paint += Panel_Paint;
        // 
        // txtCodecOutput
        // 
        txtCodecOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtCodecOutput.BorderStyle = BorderStyle.None;
        txtCodecOutput.Font = new Font("Segoe UI", 11F);
        txtCodecOutput.Location = new Point(10, 10);
        txtCodecOutput.Name = "txtCodecOutput";
        txtCodecOutput.PlaceholderText = "저장 위치 (기본: 원본 폴더)...";
        txtCodecOutput.Size = new Size(430, 20);
        txtCodecOutput.TabIndex = 0;
        // 
        // btnBrowseCodecOutput
        // 
        btnBrowseCodecOutput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseCodecOutput.BackColor = Color.FromArgb(226, 232, 240);
        btnBrowseCodecOutput.BorderRadius = 20;
        btnBrowseCodecOutput.FlatAppearance.BorderSize = 0;
        btnBrowseCodecOutput.FlatStyle = FlatStyle.Flat;
        btnBrowseCodecOutput.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnBrowseCodecOutput.ForeColor = Color.FromArgb(71, 85, 105);
        btnBrowseCodecOutput.Location = new Point(490, 185);
        btnBrowseCodecOutput.Name = "btnBrowseCodecOutput";
        btnBrowseCodecOutput.Size = new Size(100, 40);
        btnBrowseCodecOutput.TabIndex = 5;
        btnBrowseCodecOutput.Text = "위치 설정";
        btnBrowseCodecOutput.UseVisualStyleBackColor = false;
        btnBrowseCodecOutput.Click += BtnBrowseCodecOutput_Click;
        // 
        // btnConvertCodec
        // 
        btnConvertCodec.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        btnConvertCodec.BackColor = Color.FromArgb(16, 185, 129);
        btnConvertCodec.BorderRadius = 15;
        btnConvertCodec.FlatAppearance.BorderSize = 0;
        btnConvertCodec.FlatStyle = FlatStyle.Flat;
        btnConvertCodec.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        btnConvertCodec.ForeColor = Color.White;
        btnConvertCodec.Location = new Point(20, 270);
        btnConvertCodec.Name = "btnConvertCodec";
        btnConvertCodec.Size = new Size(440, 50);
        btnConvertCodec.TabIndex = 6;
        btnConvertCodec.Text = "코덱 문제 해결 시작 ✨";
        btnConvertCodec.UseVisualStyleBackColor = false;
        btnConvertCodec.Click += BtnConvertCodec_Click;
        // 
        // btnCancelCodec
        // 
        btnCancelCodec.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCancelCodec.BackColor = Color.FromArgb(255, 241, 242);
        btnCancelCodec.BorderRadius = 15;
        btnCancelCodec.FlatAppearance.BorderSize = 0;
        btnCancelCodec.FlatStyle = FlatStyle.Flat;
        btnCancelCodec.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        btnCancelCodec.ForeColor = Color.FromArgb(225, 29, 72);
        btnCancelCodec.Location = new Point(470, 270);
        btnCancelCodec.Name = "btnCancelCodec";
        btnCancelCodec.Size = new Size(120, 50);
        btnCancelCodec.TabIndex = 7;
        btnCancelCodec.Text = "취소 ✖";
        btnCancelCodec.UseVisualStyleBackColor = false;
        btnCancelCodec.Visible = false;
        btnCancelCodec.Click += BtnCancelCodec_Click;
        // 
        // lblCodecStatus
        // 
        lblCodecStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblCodecStatus.Location = new Point(20, 335);
        lblCodecStatus.Name = "lblCodecStatus";
        lblCodecStatus.Size = new Size(570, 45);
        lblCodecStatus.TabIndex = 8;
        lblCodecStatus.Text = "대기 중...";
        // 
        // pbCodec
        // 
        pbCodec.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        pbCodec.Location = new Point(20, 390);
        pbCodec.Name = "pbCodec";
        pbCodec.Size = new Size(570, 20);
        pbCodec.Style = ProgressBarStyle.Continuous;
        pbCodec.TabIndex = 12;
        // 
        // lblCodecSavePath
        // 
        lblCodecSavePath.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        lblCodecSavePath.AutoSize = true;
        lblCodecSavePath.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblCodecSavePath.ForeColor = Color.FromArgb(80, 80, 80);
        lblCodecSavePath.Location = new Point(20, 425);
        lblCodecSavePath.Name = "lblCodecSavePath";
        lblCodecSavePath.TabIndex = 13;
        lblCodecSavePath.Text = "현재 저장 위치: ";
        // 
        // btnOpenCodecFolder
        // 
        btnOpenCodecFolder.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnOpenCodecFolder.BackColor = Color.FromArgb(226, 232, 240);
        btnOpenCodecFolder.BorderRadius = 13;
        btnOpenCodecFolder.FlatAppearance.BorderSize = 0;
        btnOpenCodecFolder.FlatStyle = FlatStyle.Flat;
        btnOpenCodecFolder.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        btnOpenCodecFolder.ForeColor = Color.FromArgb(51, 65, 85);
        btnOpenCodecFolder.Location = new Point(20, 450);
        btnOpenCodecFolder.Name = "btnOpenCodecFolder";
        btnOpenCodecFolder.Size = new Size(80, 26);
        btnOpenCodecFolder.TabIndex = 14;
        btnOpenCodecFolder.Text = "폴더 열기 📂";
        btnOpenCodecFolder.UseVisualStyleBackColor = false;
        btnOpenCodecFolder.Click += BtnOpenCodecFolder_Click;
        // 
        // tabAudio
        // 
        tabAudio.BackColor = Color.FromArgb(250, 250, 250);
        tabAudio.Controls.Add(lblAudioTitle);
        tabAudio.Controls.Add(panelAudioInput);
        tabAudio.Controls.Add(btnBrowseAudio);
        tabAudio.Controls.Add(panelAudioOutput);
        tabAudio.Controls.Add(btnBrowseAudioOutput);
        tabAudio.Controls.Add(cmbAudioFormat);
        tabAudio.Controls.Add(btnConvertAudio);
        tabAudio.Controls.Add(btnCancelAudio);
        tabAudio.Controls.Add(lblAudioStatus);
        tabAudio.Controls.Add(pbAudio);
        tabAudio.Controls.Add(lblAudioSavePath);
        tabAudio.Controls.Add(btnOpenAudioFolder);
        tabAudio.Location = new Point(4, 5);
        tabAudio.Name = "tabAudio";
        tabAudio.Size = new Size(612, 551);
        tabAudio.TabIndex = 3;
        // 
        // lblAudioTitle
        // 
        lblAudioTitle.AutoSize = true;
        lblAudioTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        lblAudioTitle.Location = new Point(20, 20);
        lblAudioTitle.Name = "lblAudioTitle";
        lblAudioTitle.Size = new Size(151, 30);
        lblAudioTitle.TabIndex = 0;
        lblAudioTitle.Text = "오디오 변환기";
        // 
        // panelAudioInput
        // 
        panelAudioInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        panelAudioInput.BackColor = Color.White;
        panelAudioInput.Controls.Add(txtAudioInput);
        panelAudioInput.Location = new Point(20, 80);
        panelAudioInput.Name = "panelAudioInput";
        panelAudioInput.Size = new Size(450, 40);
        panelAudioInput.TabIndex = 1;
        panelAudioInput.Paint += Panel_Paint;
        // 
        // txtAudioInput
        // 
        txtAudioInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtAudioInput.BorderStyle = BorderStyle.None;
        txtAudioInput.Font = new Font("Segoe UI", 11F);
        txtAudioInput.Location = new Point(10, 10);
        txtAudioInput.Name = "txtAudioInput";
        txtAudioInput.PlaceholderText = "오디오/비디오 파일 경로를 선택하세요...";
        txtAudioInput.Size = new Size(430, 20);
        txtAudioInput.TabIndex = 0;
        // 
        // btnBrowseAudio
        // 
        btnBrowseAudio.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseAudio.BackColor = Color.FromArgb(226, 232, 240);
        btnBrowseAudio.BorderRadius = 20;
        btnBrowseAudio.FlatAppearance.BorderSize = 0;
        btnBrowseAudio.FlatStyle = FlatStyle.Flat;
        btnBrowseAudio.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnBrowseAudio.ForeColor = Color.FromArgb(71, 85, 105);
        btnBrowseAudio.Location = new Point(490, 80);
        btnBrowseAudio.Name = "btnBrowseAudio";
        btnBrowseAudio.Size = new Size(100, 40);
        btnBrowseAudio.TabIndex = 2;
        btnBrowseAudio.Text = "파일 선택";
        btnBrowseAudio.UseVisualStyleBackColor = false;
        btnBrowseAudio.Click += BtnBrowseAudio_Click;
        // 
        // panelAudioOutput
        // 
        panelAudioOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        panelAudioOutput.BackColor = Color.White;
        panelAudioOutput.Controls.Add(txtAudioOutput);
        panelAudioOutput.Location = new Point(20, 130);
        panelAudioOutput.Name = "panelAudioOutput";
        panelAudioOutput.Size = new Size(450, 40);
        panelAudioOutput.TabIndex = 3;
        panelAudioOutput.Paint += Panel_Paint;
        // 
        // txtAudioOutput
        // 
        txtAudioOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtAudioOutput.BorderStyle = BorderStyle.None;
        txtAudioOutput.Font = new Font("Segoe UI", 11F);
        txtAudioOutput.Location = new Point(10, 10);
        txtAudioOutput.Name = "txtAudioOutput";
        txtAudioOutput.PlaceholderText = "저장 위치 (기본: 원본 폴더)...";
        txtAudioOutput.Size = new Size(430, 20);
        txtAudioOutput.TabIndex = 0;
        // 
        // btnBrowseAudioOutput
        // 
        btnBrowseAudioOutput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseAudioOutput.BackColor = Color.FromArgb(226, 232, 240);
        btnBrowseAudioOutput.BorderRadius = 20;
        btnBrowseAudioOutput.FlatAppearance.BorderSize = 0;
        btnBrowseAudioOutput.FlatStyle = FlatStyle.Flat;
        btnBrowseAudioOutput.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnBrowseAudioOutput.ForeColor = Color.FromArgb(71, 85, 105);
        btnBrowseAudioOutput.Location = new Point(490, 130);
        btnBrowseAudioOutput.Name = "btnBrowseAudioOutput";
        btnBrowseAudioOutput.Size = new Size(100, 40);
        btnBrowseAudioOutput.TabIndex = 4;
        btnBrowseAudioOutput.Text = "위치 설정";
        btnBrowseAudioOutput.UseVisualStyleBackColor = false;
        btnBrowseAudioOutput.Click += BtnBrowseAudioOutput_Click;
        // 
        // cmbAudioFormat
        // 
        cmbAudioFormat.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        cmbAudioFormat.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbAudioFormat.Font = new Font("Segoe UI", 11F);
        cmbAudioFormat.FormattingEnabled = true;
        cmbAudioFormat.Items.AddRange(new object[] { "MP3", "WAV", "M4A", "FLAC", "OGG" });
        cmbAudioFormat.Location = new Point(20, 180);
        cmbAudioFormat.Name = "cmbAudioFormat";
        cmbAudioFormat.Size = new Size(570, 28);
        cmbAudioFormat.TabIndex = 5;
        // 
        // btnConvertAudio
        // 
        btnConvertAudio.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        btnConvertAudio.BackColor = Color.FromArgb(16, 185, 129);
        btnConvertAudio.BorderRadius = 15;
        btnConvertAudio.FlatAppearance.BorderSize = 0;
        btnConvertAudio.FlatStyle = FlatStyle.Flat;
        btnConvertAudio.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        btnConvertAudio.ForeColor = Color.White;
        btnConvertAudio.Location = new Point(20, 230);
        btnConvertAudio.Name = "btnConvertAudio";
        btnConvertAudio.Size = new Size(440, 50);
        btnConvertAudio.TabIndex = 6;
        btnConvertAudio.Text = "오디오 추출/변환 시작 🎵";
        btnConvertAudio.UseVisualStyleBackColor = false;
        btnConvertAudio.Click += BtnConvertAudio_Click;
        // 
        // btnCancelAudio
        // 
        btnCancelAudio.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCancelAudio.BackColor = Color.FromArgb(255, 241, 242);
        btnCancelAudio.BorderRadius = 15;
        btnCancelAudio.FlatAppearance.BorderSize = 0;
        btnCancelAudio.FlatStyle = FlatStyle.Flat;
        btnCancelAudio.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        btnCancelAudio.ForeColor = Color.FromArgb(225, 29, 72);
        btnCancelAudio.Location = new Point(470, 230);
        btnCancelAudio.Name = "btnCancelAudio";
        btnCancelAudio.Size = new Size(120, 50);
        btnCancelAudio.TabIndex = 7;
        btnCancelAudio.Text = "취소 ✖";
        btnCancelAudio.UseVisualStyleBackColor = false;
        btnCancelAudio.Visible = false;
        btnCancelAudio.Click += BtnCancelAudio_Click;
        // 
        // lblAudioStatus
        // 
        lblAudioStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblAudioStatus.Location = new Point(20, 295);
        lblAudioStatus.Name = "lblAudioStatus";
        lblAudioStatus.Size = new Size(570, 45);
        lblAudioStatus.TabIndex = 8;
        lblAudioStatus.Text = "대기 중...";
        // 
        // pbAudio
        // 
        pbAudio.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        pbAudio.Location = new Point(20, 350);
        pbAudio.Name = "pbAudio";
        pbAudio.Size = new Size(570, 20);
        pbAudio.Style = ProgressBarStyle.Continuous;
        pbAudio.TabIndex = 13;
        // 
        // lblAudioSavePath
        // 
        lblAudioSavePath.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        lblAudioSavePath.AutoSize = true;
        lblAudioSavePath.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblAudioSavePath.ForeColor = Color.FromArgb(80, 80, 80);
        lblAudioSavePath.Location = new Point(20, 385);
        lblAudioSavePath.Name = "lblAudioSavePath";
        lblAudioSavePath.TabIndex = 14;
        lblAudioSavePath.Text = "현재 저장 위치: ";
        // 
        // btnOpenAudioFolder
        // 
        btnOpenAudioFolder.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnOpenAudioFolder.BackColor = Color.FromArgb(226, 232, 240);
        btnOpenAudioFolder.BorderRadius = 13;
        btnOpenAudioFolder.FlatAppearance.BorderSize = 0;
        btnOpenAudioFolder.FlatStyle = FlatStyle.Flat;
        btnOpenAudioFolder.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        btnOpenAudioFolder.ForeColor = Color.FromArgb(51, 65, 85);
        btnOpenAudioFolder.Location = new Point(20, 410);
        btnOpenAudioFolder.Name = "btnOpenAudioFolder";
        btnOpenAudioFolder.Size = new Size(80, 26);
        btnOpenAudioFolder.TabIndex = 15;
        btnOpenAudioFolder.Text = "폴더 열기 📂";
        btnOpenAudioFolder.UseVisualStyleBackColor = false;
        btnOpenAudioFolder.Click += BtnOpenAudioFolder_Click;
        // 
        // tabSettings
        // 
        tabSettings.BackColor = Color.FromArgb(250, 250, 250);
        tabSettings.Controls.Add(lblAbout);
        tabSettings.Controls.Add(lblSettingsTitle);
        tabSettings.Controls.Add(chkShowNotifications);
        tabSettings.Controls.Add(chkAutoOpenFolder);
        tabSettings.Controls.Add(chkAutoUpdateCheck);
        tabSettings.Controls.Add(lblDownloadFolder);
        tabSettings.Controls.Add(panelSettingsFolder);
        tabSettings.Controls.Add(btnBrowseFolder);
        tabSettings.Controls.Add(btnSaveSettings);
        tabSettings.Controls.Add(btnCheckUpdate);
        tabSettings.Location = new Point(4, 5);
        tabSettings.Name = "tabSettings";
        tabSettings.Size = new Size(612, 551);
        tabSettings.TabIndex = 3;
        // 
        // lblAbout
        // 
        lblAbout.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lblAbout.Font = new Font("Segoe UI", 9F);
        lblAbout.ForeColor = Color.Gray;
        lblAbout.Location = new Point(20, 465);
        lblAbout.Name = "lblAbout";
        lblAbout.Size = new Size(570, 80);
        lblAbout.TabIndex = 17;
        lblAbout.Text = "Multi Media Toolkit v1.3.2\nCreated by 김병석\n© 2026 all rights reserved.\n(kbs318@naver.com)";
        lblAbout.TextAlign = ContentAlignment.BottomRight;
        // 
        // lblSettingsTitle
        // 
        lblSettingsTitle.AutoSize = true;
        lblSettingsTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        lblSettingsTitle.Location = new Point(20, 20);
        lblSettingsTitle.Name = "lblSettingsTitle";
        lblSettingsTitle.Size = new Size(151, 30);
        lblSettingsTitle.TabIndex = 8;
        lblSettingsTitle.Text = "프로그램 설정";
        // 
        // chkShowNotifications
        // 
        chkShowNotifications.AutoSize = true;
        chkShowNotifications.Font = new Font("Segoe UI", 11F);
        chkShowNotifications.Location = new Point(20, 80);
        chkShowNotifications.Name = "chkShowNotifications";
        chkShowNotifications.Size = new Size(236, 24);
        chkShowNotifications.TabIndex = 9;
        chkShowNotifications.Text = "알림 표시 (다운로드 완료/실패)";
        // 
        // chkAutoOpenFolder
        // 
        chkAutoOpenFolder.AutoSize = true;
        chkAutoOpenFolder.Font = new Font("Segoe UI", 11F);
        chkAutoOpenFolder.Location = new Point(20, 115);
        chkAutoOpenFolder.Name = "chkAutoOpenFolder";
        chkAutoOpenFolder.Size = new Size(236, 24);
        chkAutoOpenFolder.TabIndex = 10;
        chkAutoOpenFolder.Text = "다운로드 완료 시 저장 폴더 열기";
        // 
        // chkAutoUpdateCheck
        // 
        chkAutoUpdateCheck.AutoSize = true;
        chkAutoUpdateCheck.Font = new Font("Segoe UI", 11F);
        chkAutoUpdateCheck.Location = new Point(20, 150);
        chkAutoUpdateCheck.Name = "chkAutoUpdateCheck";
        chkAutoUpdateCheck.Size = new Size(236, 24);
        chkAutoUpdateCheck.TabIndex = 11;
        chkAutoUpdateCheck.Text = "시작 시 자동으로 업데이트 확인";
        // 
        // lblDownloadFolder
        // 
        lblDownloadFolder.AutoSize = true;
        lblDownloadFolder.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblDownloadFolder.Location = new Point(20, 200);
        lblDownloadFolder.Name = "lblDownloadFolder";
        lblDownloadFolder.Size = new Size(107, 20);
        lblDownloadFolder.TabIndex = 12;
        lblDownloadFolder.Text = "기본 저장 경로";
        // 
        // panelSettingsFolder
        // 
        panelSettingsFolder.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        panelSettingsFolder.BackColor = Color.White;
        panelSettingsFolder.BorderStyle = BorderStyle.FixedSingle;
        panelSettingsFolder.Controls.Add(txtDownloadFolder);
        panelSettingsFolder.Location = new Point(20, 230);
        panelSettingsFolder.Name = "panelSettingsFolder";
        panelSettingsFolder.Size = new Size(450, 40);
        panelSettingsFolder.TabIndex = 13;
        panelSettingsFolder.Paint += Panel_Paint;
        // 
        // txtDownloadFolder
        // 
        txtDownloadFolder.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtDownloadFolder.BorderStyle = BorderStyle.None;
        txtDownloadFolder.Font = new Font("Segoe UI", 11F);
        txtDownloadFolder.Location = new Point(10, 10);
        txtDownloadFolder.Name = "txtDownloadFolder";
        txtDownloadFolder.Size = new Size(430, 20);
        txtDownloadFolder.TabIndex = 0;
        // 
        // btnBrowseFolder
        // 
        btnBrowseFolder.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseFolder.BackColor = Color.FromArgb(226, 232, 240);
        btnBrowseFolder.BorderRadius = 20;
        btnBrowseFolder.FlatAppearance.BorderSize = 0;
        btnBrowseFolder.FlatStyle = FlatStyle.Flat;
        btnBrowseFolder.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnBrowseFolder.ForeColor = Color.FromArgb(71, 85, 105);
        btnBrowseFolder.Location = new Point(490, 230);
        btnBrowseFolder.Name = "btnBrowseFolder";
        btnBrowseFolder.Size = new Size(100, 40);
        btnBrowseFolder.TabIndex = 14;
        btnBrowseFolder.Text = "경로 변경";
        btnBrowseFolder.UseVisualStyleBackColor = false;
        btnBrowseFolder.Click += BtnBrowseFolder_Click;
        // 
        // btnSaveSettings
        // 
        btnSaveSettings.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        btnSaveSettings.BackColor = Color.FromArgb(2, 132, 199);
        btnSaveSettings.BorderRadius = 15;
        btnSaveSettings.FlatAppearance.BorderSize = 0;
        btnSaveSettings.FlatStyle = FlatStyle.Flat;
        btnSaveSettings.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        btnSaveSettings.ForeColor = Color.White;
        btnSaveSettings.Location = new Point(20, 290);
        btnSaveSettings.Name = "btnSaveSettings";
        btnSaveSettings.Size = new Size(570, 50);
        btnSaveSettings.TabIndex = 15;
        btnSaveSettings.Text = "설정 저장 ✨";
        btnSaveSettings.UseVisualStyleBackColor = false;
        btnSaveSettings.Click += BtnSaveSettings_Click;
        // 
        // btnCheckUpdate
        // 
        btnCheckUpdate.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        btnCheckUpdate.BackColor = Color.FromArgb(16, 185, 129);
        btnCheckUpdate.BorderRadius = 15;
        btnCheckUpdate.FlatAppearance.BorderSize = 0;
        btnCheckUpdate.FlatStyle = FlatStyle.Flat;
        btnCheckUpdate.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnCheckUpdate.ForeColor = Color.White;
        btnCheckUpdate.Location = new Point(20, 360);
        btnCheckUpdate.Name = "btnCheckUpdate";
        btnCheckUpdate.Size = new Size(570, 45);
        btnCheckUpdate.TabIndex = 16;
        btnCheckUpdate.Text = "새 버전 업데이트 확인 🔄";
        btnCheckUpdate.UseVisualStyleBackColor = false;
        btnCheckUpdate.Click += BtnCheckUpdate_Click;
        // 
        // tabMiniEdit
        // 
        tabMiniEdit.BackColor = Color.FromArgb(250, 250, 250);
        tabMiniEdit.Controls.Add(miniEditorControl);
        tabMiniEdit.Location = new Point(4, 5);
        tabMiniEdit.Name = "tabMiniEdit";
        tabMiniEdit.Size = new Size(612, 551);
        tabMiniEdit.TabIndex = 6;
        // 
        // miniEditorControl
        // 
        miniEditorControl.BackColor = Color.FromArgb(250, 250, 250);
        miniEditorControl.Dock = DockStyle.Fill;
        miniEditorControl.Location = new Point(0, 0);
        miniEditorControl.Name = "miniEditorControl";
        miniEditorControl.Size = new Size(612, 551);
        miniEditorControl.TabIndex = 0;
        // 
        // notifyIconApp
        // 
        notifyIconApp.ContextMenuStrip = contextMenuTray;
        notifyIconApp.Icon = (Icon)resources.GetObject("notifyIconApp.Icon");
        notifyIconApp.Text = "Multi Media Toolkit";
        notifyIconApp.Visible = true;
        notifyIconApp.DoubleClick += MenuTrayOpen_Click;
        // 
        // contextMenuTray
        // 
        contextMenuTray.Items.AddRange(new ToolStripItem[] { menuTrayOpen, menuTrayExit });
        contextMenuTray.Name = "contextMenuTray";
        contextMenuTray.Size = new Size(99, 54);
        // 
        // menuTrayOpen
        // 
        menuTrayOpen.Name = "menuTrayOpen";
        menuTrayOpen.Size = new Size(98, 22);
        menuTrayOpen.Text = "열기";
        menuTrayOpen.Click += MenuTrayOpen_Click;
        // 
        // menuTrayExit
        // 
        menuTrayExit.Name = "menuTrayExit";
        menuTrayExit.Size = new Size(98, 22);
        menuTrayExit.Text = "종료";
        menuTrayExit.Click += MenuTrayExit_Click;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1100, 750);
        Controls.Add(panelMain);
        Controls.Add(panelSidebar);
        MinimumSize = new Size(1000, 700);
        Name = "Form1";
        Text = "Multi Media Toolkit";
        Load += Form1_Load;
        panelSidebar.ResumeLayout(false);
        panelSidebar.PerformLayout();
        panelMain.ResumeLayout(false);
        tabControlMain.ResumeLayout(false);
        tabYoutube.ResumeLayout(false);
        tabYoutube.PerformLayout();
        panelUrlContainer.ResumeLayout(false);
        panelUrlContainer.PerformLayout();
        panelInfo.ResumeLayout(false);
        panelInfo.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)picThumbnail).EndInit();
        contextMenuRemove.ResumeLayout(false);
        tabYtDlp.ResumeLayout(false);
        tabYtDlp.PerformLayout();
        contextMenuYtDlpRemove.ResumeLayout(false);
        panelXBrowser.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)webViewX).EndInit();
        panelYtDlpUrl.ResumeLayout(false);
        panelYtDlpUrl.PerformLayout();
        tabWebM.ResumeLayout(false);
        tabWebM.PerformLayout();
        panelWebMInput.ResumeLayout(false);
        panelWebMInput.PerformLayout();
        panelWebMOutput.ResumeLayout(false);
        panelWebMOutput.PerformLayout();
        tabCodec.ResumeLayout(false);
        tabCodec.PerformLayout();
        panelCodecInput.ResumeLayout(false);
        panelCodecInput.PerformLayout();
        panelCodecOutput.ResumeLayout(false);
        panelCodecOutput.PerformLayout();
        tabAudio.ResumeLayout(false);
        tabAudio.PerformLayout();
        panelAudioInput.ResumeLayout(false);
        panelAudioInput.PerformLayout();
        panelAudioOutput.ResumeLayout(false);
        panelAudioOutput.PerformLayout();
        tabSettings.ResumeLayout(false);
        tabSettings.PerformLayout();
        panelSettingsFolder.ResumeLayout(false);
        panelSettingsFolder.PerformLayout();
        tabMiniEdit.ResumeLayout(false);
        contextMenuTray.ResumeLayout(false);
        ResumeLayout(false);
    }
}
