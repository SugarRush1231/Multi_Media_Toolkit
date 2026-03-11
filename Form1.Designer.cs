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
    private YoutubeDownloader.RoundButton btnAddQueue;
    private System.Windows.Forms.ListView lvQueue;
    private System.Windows.Forms.ColumnHeader colTitle;
    private System.Windows.Forms.ColumnHeader colQuality;
    private System.Windows.Forms.ColumnHeader colStatus;
    private YoutubeDownloader.RoundButton btnRemoveSelected;
    private System.Windows.Forms.Label lblStatus;
    private System.Windows.Forms.ProgressBar pbYoutube;
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

    // Tab 3: Codec Fixed
    private System.Windows.Forms.TabPage tabCodec;
    private System.Windows.Forms.Label lblCodecTitle;
    private System.Windows.Forms.Panel panelCodecInput;
    private System.Windows.Forms.TextBox txtCodecInput;
    private YoutubeDownloader.RoundButton btnBrowseCodec;
    private YoutubeDownloader.RoundButton btnConvertCodec;
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

    // Tab 4.2: YtDlp (External)
    private System.Windows.Forms.TabPage tabYtDlp;
    private System.Windows.Forms.Label lblYtDlpTitle;
    private System.Windows.Forms.Label lblYtDlpDesc;
    private System.Windows.Forms.Panel panelYtDlpUrl;
    private System.Windows.Forms.TextBox txtYtDlpUrl;
    private YoutubeDownloader.RoundButton btnYtDlpRun;
    private YoutubeDownloader.RoundButton btnYtDlpCancel;
    private System.Windows.Forms.Label lblYtDlpStatus;
    private System.Windows.Forms.ProgressBar pbYtDlp;
    private YoutubeDownloader.ToggleSwitch tglXPrivateMode;
    private System.Windows.Forms.Label lblXPrivateMode;
    private System.Windows.Forms.Panel panelXBrowser;
    private Microsoft.Web.WebView2.WinForms.WebView2 webViewX;
    private YoutubeDownloader.RoundButton btnXCapture;
    private YoutubeDownloader.RoundButton btnXDownload;
    private YoutubeDownloader.RoundButton btnXClose;
    private System.Windows.Forms.ProgressBar pbXDownload;
    private System.Windows.Forms.Label lblXStatus;
    private System.Windows.Forms.Label lblYtDlpSavePath;

    // Tab 4.5: Mini Edit
    private System.Windows.Forms.TabPage tabMiniEdit;
    private YoutubeDownloader.MiniEditor miniEditorControl;

    // Tab 5: Settings
    private System.Windows.Forms.TabPage tabSettings;
    private System.Windows.Forms.Label lblSettingsTitle;
    private System.Windows.Forms.CheckBox chkShowNotifications;
    private System.Windows.Forms.Label lblDownloadFolder;
    private System.Windows.Forms.Panel panelSettingsFolder;
    private System.Windows.Forms.TextBox txtDownloadFolder;
    private YoutubeDownloader.RoundButton btnBrowseFolder;
    private YoutubeDownloader.RoundButton btnSaveSettings;
    private YoutubeDownloader.RoundButton btnFullCleanup;
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
        this.components = new System.ComponentModel.Container();
        this.panelSidebar = new System.Windows.Forms.Panel();
        this.lblLogo = new System.Windows.Forms.Label();
        this.btnTabYoutube = new YoutubeDownloader.RoundButton();
        this.btnTabYtDlp = new YoutubeDownloader.RoundButton();
        this.btnTabWebM = new YoutubeDownloader.RoundButton();
        this.btnTabCodec = new YoutubeDownloader.RoundButton();
        this.btnTabAudio = new YoutubeDownloader.RoundButton();
        this.btnTabMiniEdit = new YoutubeDownloader.RoundButton();
        this.btnTabSettings = new YoutubeDownloader.RoundButton();
        this.lblSubLogo = new System.Windows.Forms.Label();
        
        this.panelMain = new System.Windows.Forms.Panel();
        this.tabControlMain = new System.Windows.Forms.TabControl();
        
        // YouTube Tab Elements
        this.tabYoutube = new System.Windows.Forms.TabPage();
        this.lblUrl = new System.Windows.Forms.Label();
        this.panelUrlContainer = new System.Windows.Forms.Panel();
        this.txtUrl = new System.Windows.Forms.TextBox();
        this.btnLoad = new YoutubeDownloader.RoundButton();
        this.panelInfo = new System.Windows.Forms.Panel();
        this.picThumbnail = new System.Windows.Forms.PictureBox();
        this.lblVideoTitle = new System.Windows.Forms.Label();
        this.txtEditTitle = new System.Windows.Forms.TextBox();
        this.lblQuality = new System.Windows.Forms.Label();
        this.cmbQuality = new System.Windows.Forms.ComboBox();
        this.btnAddQueue = new YoutubeDownloader.RoundButton();
        this.lvQueue = new System.Windows.Forms.ListView();
        this.colTitle = new System.Windows.Forms.ColumnHeader();
        this.colQuality = new System.Windows.Forms.ColumnHeader();
        this.colStatus = new System.Windows.Forms.ColumnHeader();
        this.btnRemoveSelected = new YoutubeDownloader.RoundButton();
        this.lblStatus = new System.Windows.Forms.Label();
        this.pbYoutube = new System.Windows.Forms.ProgressBar();
        this.contextMenuRemove = new System.Windows.Forms.ContextMenuStrip(this.components);

        this.menuRemoveSelected = new System.Windows.Forms.ToolStripMenuItem();

        // WebM Tab Elements
        this.tabWebM = new System.Windows.Forms.TabPage();
        this.lblWebMTitle = new System.Windows.Forms.Label();
        this.panelWebMInput = new System.Windows.Forms.Panel();
        this.txtWebMInput = new System.Windows.Forms.TextBox();
        this.btnBrowseWebM = new YoutubeDownloader.RoundButton();
        this.btnConvertWebM = new YoutubeDownloader.RoundButton();
        this.btnCancelWebM = new YoutubeDownloader.RoundButton();
        this.lblWebMStatus = new System.Windows.Forms.Label();
        this.pbWebM = new System.Windows.Forms.ProgressBar();
        this.cmbWebMFormat = new System.Windows.Forms.ComboBox();
        this.panelWebMOutput = new System.Windows.Forms.Panel();
        this.txtWebMOutput = new System.Windows.Forms.TextBox();
        this.btnBrowseWebMOutput = new YoutubeDownloader.RoundButton();

        // Codec Tab Elements
        this.lblXPrivateMode = new System.Windows.Forms.Label();
        this.tglXPrivateMode = new YoutubeDownloader.ToggleSwitch();
        this.panelXBrowser = new System.Windows.Forms.Panel();
        this.webViewX = new Microsoft.Web.WebView2.WinForms.WebView2();
        this.btnXCapture = new YoutubeDownloader.RoundButton();
        this.btnXDownload = new YoutubeDownloader.RoundButton();
        this.btnXClose = new YoutubeDownloader.RoundButton();
        this.pbXDownload = new System.Windows.Forms.ProgressBar();
        this.lblXStatus = new System.Windows.Forms.Label();
        this.lblYtDlpSavePath = new System.Windows.Forms.Label();
        this.tabCodec = new System.Windows.Forms.TabPage();
        this.lblCodecTitle = new System.Windows.Forms.Label();
        this.lblCodecDesc = new System.Windows.Forms.Label();
        this.panelCodecInput = new System.Windows.Forms.Panel();
        this.txtCodecInput = new System.Windows.Forms.TextBox();
        this.btnBrowseCodec = new YoutubeDownloader.RoundButton();
        this.btnConvertCodec = new YoutubeDownloader.RoundButton();
        this.btnCancelCodec = new YoutubeDownloader.RoundButton();
        this.lblCodecStatus = new System.Windows.Forms.Label();
        this.pbCodec = new System.Windows.Forms.ProgressBar();
        this.panelCodecOutput = new System.Windows.Forms.Panel();
        this.txtCodecOutput = new System.Windows.Forms.TextBox();
        this.btnBrowseCodecOutput = new YoutubeDownloader.RoundButton();

        // Audio Tab Elements
        this.tabAudio = new System.Windows.Forms.TabPage();
        this.lblAudioTitle = new System.Windows.Forms.Label();
        this.panelAudioInput = new System.Windows.Forms.Panel();
        this.txtAudioInput = new System.Windows.Forms.TextBox();
        this.btnBrowseAudio = new YoutubeDownloader.RoundButton();
        this.panelAudioOutput = new System.Windows.Forms.Panel();
        this.txtAudioOutput = new System.Windows.Forms.TextBox();
        this.btnBrowseAudioOutput = new YoutubeDownloader.RoundButton();
        this.cmbAudioFormat = new System.Windows.Forms.ComboBox();
        this.btnConvertAudio = new YoutubeDownloader.RoundButton();
        this.btnCancelAudio = new YoutubeDownloader.RoundButton();
        this.lblAudioStatus = new System.Windows.Forms.Label();
        this.pbAudio = new System.Windows.Forms.ProgressBar();

        // YtDlp Tab Elements
        this.tabYtDlp = new System.Windows.Forms.TabPage();
        this.lblYtDlpTitle = new System.Windows.Forms.Label();
        this.lblYtDlpDesc = new System.Windows.Forms.Label();
        this.panelYtDlpUrl = new System.Windows.Forms.Panel();
        this.txtYtDlpUrl = new System.Windows.Forms.TextBox();
        this.btnYtDlpRun = new YoutubeDownloader.RoundButton();
        this.btnYtDlpCancel = new YoutubeDownloader.RoundButton();
        this.lblYtDlpStatus = new System.Windows.Forms.Label();
        this.pbYtDlp = new System.Windows.Forms.ProgressBar();

        // Settings Tab Elements
        this.tabSettings = new System.Windows.Forms.TabPage();
        this.lblSettingsTitle = new System.Windows.Forms.Label();
        this.chkShowNotifications = new System.Windows.Forms.CheckBox();
        this.lblDownloadFolder = new System.Windows.Forms.Label();
        this.panelSettingsFolder = new System.Windows.Forms.Panel();
        this.txtDownloadFolder = new System.Windows.Forms.TextBox();
        this.btnBrowseFolder = new YoutubeDownloader.RoundButton();
        this.btnSaveSettings = new YoutubeDownloader.RoundButton();
        this.btnFullCleanup = new YoutubeDownloader.RoundButton();
        this.btnCheckUpdate = new YoutubeDownloader.RoundButton();
        this.lblAbout = new System.Windows.Forms.Label();


        this.notifyIconApp = new System.Windows.Forms.NotifyIcon(this.components);
        this.contextMenuTray = new System.Windows.Forms.ContextMenuStrip(this.components);
        this.menuTrayOpen = new System.Windows.Forms.ToolStripMenuItem();
        this.menuTrayExit = new System.Windows.Forms.ToolStripMenuItem();

        this.panelSidebar.SuspendLayout();
        this.panelMain.SuspendLayout();
        this.tabControlMain.SuspendLayout();
        this.tabYoutube.SuspendLayout();
        this.tabYtDlp.SuspendLayout();
        this.panelUrlContainer.SuspendLayout();
        this.panelInfo.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.picThumbnail)).BeginInit();
        this.contextMenuRemove.SuspendLayout();
        this.tabWebM.SuspendLayout();
        this.panelWebMInput.SuspendLayout();
        this.panelWebMOutput.SuspendLayout();
        this.tabCodec.SuspendLayout();
        this.panelCodecInput.SuspendLayout();
        this.panelCodecOutput.SuspendLayout();
        this.tabAudio.SuspendLayout();
        this.panelAudioInput.SuspendLayout();
        this.panelAudioOutput.SuspendLayout();
        this.tabYtDlp.SuspendLayout();
        this.panelYtDlpUrl.SuspendLayout();
        this.tabSettings.SuspendLayout();
        this.panelSettingsFolder.SuspendLayout();
        this.SuspendLayout();

        // panelSidebar
        this.panelSidebar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
        this.panelSidebar.Controls.Add(this.lblSubLogo);
        this.panelSidebar.Controls.Add(this.btnTabSettings);
        this.panelSidebar.Controls.Add(this.btnTabMiniEdit);
        this.panelSidebar.Controls.Add(this.btnTabAudio);
        this.panelSidebar.Controls.Add(this.btnTabWebM);
        this.panelSidebar.Controls.Add(this.btnTabCodec);
        this.panelSidebar.Controls.Add(this.btnTabYtDlp);
        this.panelSidebar.Controls.Add(this.btnTabYoutube);
        this.panelSidebar.Controls.Add(this.lblLogo);
        this.panelSidebar.Dock = System.Windows.Forms.DockStyle.Left;
        this.panelSidebar.Location = new System.Drawing.Point(0, 0);
        this.panelSidebar.Name = "panelSidebar";
        this.panelSidebar.Size = new System.Drawing.Size(180, 560);
        this.panelSidebar.TabIndex = 0;

        // lblLogo
        this.lblLogo.AutoSize = true;
        this.lblLogo.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        this.lblLogo.ForeColor = System.Drawing.Color.White;
        this.lblLogo.Location = new System.Drawing.Point(15, 20);
        this.lblLogo.Name = "lblLogo";
        this.lblLogo.Size = new System.Drawing.Size(150, 45);
        this.lblLogo.TabIndex = 0;
        this.lblLogo.Text = "Multi\nMedia Toolkit";
        this.lblLogo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
 
        // lblSubLogo
        this.lblSubLogo.AutoSize = true;
        this.lblSubLogo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.lblSubLogo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
        this.lblSubLogo.Location = new System.Drawing.Point(65, 68);
        this.lblSubLogo.Name = "lblSubLogo";
        this.lblSubLogo.Size = new System.Drawing.Size(46, 15);
        this.lblSubLogo.TabIndex = 1;
        this.lblSubLogo.Text = "Made by 김병석";

        // btnTabYoutube
        this.btnTabYoutube.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(71)))), ((int)(((byte)(87)))));
        this.btnTabYoutube.BorderRadius = 10;
        this.btnTabYoutube.FlatAppearance.BorderSize = 0;
        this.btnTabYoutube.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnTabYoutube.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        this.btnTabYoutube.ForeColor = System.Drawing.Color.White;
        this.btnTabYoutube.Location = new System.Drawing.Point(15, 100);
        this.btnTabYoutube.Name = "btnTabYoutube";
        this.btnTabYoutube.Size = new System.Drawing.Size(150, 40);
        this.btnTabYoutube.TabIndex = 1;
        this.btnTabYoutube.Text = "유튜브 다운로더";
        this.btnTabYoutube.UseVisualStyleBackColor = false;
        this.btnTabYoutube.Click += new System.EventHandler(this.BtnTab_Click);

        // btnTabYtDlp
        this.btnTabYtDlp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
        this.btnTabYtDlp.BorderRadius = 10;
        this.btnTabYtDlp.FlatAppearance.BorderSize = 0;
        this.btnTabYtDlp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnTabYtDlp.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.btnTabYtDlp.ForeColor = System.Drawing.Color.Silver;
        this.btnTabYtDlp.Location = new System.Drawing.Point(15, 150);
        this.btnTabYtDlp.Name = "btnTabYtDlp";
        this.btnTabYtDlp.Size = new System.Drawing.Size(150, 40);
        this.btnTabYtDlp.TabIndex = 2;
        this.btnTabYtDlp.Text = "웹 사이트 영상 다운";
        this.btnTabYtDlp.UseVisualStyleBackColor = false;
        this.btnTabYtDlp.Click += new System.EventHandler(this.BtnTab_Click);

        // btnTabCodec
        this.btnTabCodec.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
        this.btnTabCodec.BorderRadius = 10;
        this.btnTabCodec.FlatAppearance.BorderSize = 0;
        this.btnTabCodec.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnTabCodec.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.btnTabCodec.ForeColor = System.Drawing.Color.Silver;
        this.btnTabCodec.Location = new System.Drawing.Point(15, 200);
        this.btnTabCodec.Name = "btnTabCodec";
        this.btnTabCodec.Size = new System.Drawing.Size(150, 40);
        this.btnTabCodec.TabIndex = 3;
        this.btnTabCodec.Text = "Pr/AE 코덱 해결";
        this.btnTabCodec.UseVisualStyleBackColor = false;
        this.btnTabCodec.Click += new System.EventHandler(this.BtnTab_Click);

        // btnTabWebM
        this.btnTabWebM.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
        this.btnTabWebM.BorderRadius = 10;
        this.btnTabWebM.FlatAppearance.BorderSize = 0;
        this.btnTabWebM.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnTabWebM.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.btnTabWebM.ForeColor = System.Drawing.Color.Silver;
        this.btnTabWebM.Location = new System.Drawing.Point(15, 250);
        this.btnTabWebM.Name = "btnTabWebM";
        this.btnTabWebM.Size = new System.Drawing.Size(150, 40);
        this.btnTabWebM.TabIndex = 4;
        this.btnTabWebM.Text = "포맷 변환기";
        this.btnTabWebM.UseVisualStyleBackColor = false;
        this.btnTabWebM.Click += new System.EventHandler(this.BtnTab_Click);

        // btnTabAudio
        this.btnTabAudio.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
        this.btnTabAudio.BorderRadius = 10;
        this.btnTabAudio.FlatAppearance.BorderSize = 0;
        this.btnTabAudio.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnTabAudio.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.btnTabAudio.ForeColor = System.Drawing.Color.Silver;
        this.btnTabAudio.Location = new System.Drawing.Point(15, 300);
        this.btnTabAudio.Name = "btnTabAudio";
        this.btnTabAudio.Size = new System.Drawing.Size(150, 40);
        this.btnTabAudio.TabIndex = 5;
        this.btnTabAudio.Text = "오디오 변환기";
        this.btnTabAudio.UseVisualStyleBackColor = false;
        this.btnTabAudio.Click += new System.EventHandler(this.BtnTab_Click);

        // btnTabMiniEdit
        this.btnTabMiniEdit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
        this.btnTabMiniEdit.BorderRadius = 10;
        this.btnTabMiniEdit.FlatAppearance.BorderSize = 0;
        this.btnTabMiniEdit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnTabMiniEdit.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.btnTabMiniEdit.ForeColor = System.Drawing.Color.Silver;
        this.btnTabMiniEdit.Location = new System.Drawing.Point(15, 350);
        this.btnTabMiniEdit.Name = "btnTabMiniEdit";
        this.btnTabMiniEdit.Size = new System.Drawing.Size(150, 40);
        this.btnTabMiniEdit.TabIndex = 6;
        this.btnTabMiniEdit.Text = "미니 편집기";
        this.btnTabMiniEdit.UseVisualStyleBackColor = false;
        this.btnTabMiniEdit.Click += new System.EventHandler(this.BtnTab_Click);

        // btnTabSettings
        this.btnTabSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
        this.btnTabSettings.BorderRadius = 10;
        this.btnTabSettings.FlatAppearance.BorderSize = 0;
        this.btnTabSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnTabSettings.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.btnTabSettings.ForeColor = System.Drawing.Color.Silver;
        this.btnTabSettings.Location = new System.Drawing.Point(15, 500);
        this.btnTabSettings.Name = "btnTabSettings";
        this.btnTabSettings.Size = new System.Drawing.Size(150, 40);
        this.btnTabSettings.TabIndex = 5;
        this.btnTabSettings.Text = "설정";
        this.btnTabSettings.UseVisualStyleBackColor = false;
        this.btnTabSettings.Click += new System.EventHandler(this.BtnTab_Click);

        // panelMain
        this.panelMain.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
        this.panelMain.Controls.Add(this.tabControlMain);
        this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
        this.panelMain.Location = new System.Drawing.Point(180, 0);
        this.panelMain.Name = "panelMain";
        this.panelMain.Size = new System.Drawing.Size(620, 560);
        this.panelMain.TabIndex = 1;

        // tabControlMain
        this.tabControlMain.Controls.Add(this.tabYoutube);
        this.tabControlMain.Controls.Add(this.tabYtDlp);
        this.tabControlMain.Controls.Add(this.tabWebM);
        this.tabControlMain.Controls.Add(this.tabCodec);
        this.tabControlMain.Controls.Add(this.tabAudio);
        this.tabControlMain.Controls.Add(this.tabSettings);
        this.tabControlMain.Dock = System.Windows.Forms.DockStyle.Fill;
        this.tabControlMain.ItemSize = new System.Drawing.Size(0, 1);
        this.tabControlMain.Location = new System.Drawing.Point(0, 0);
        this.tabControlMain.Name = "tabControlMain";
        this.tabControlMain.SelectedIndex = 0;
        this.tabControlMain.Size = new System.Drawing.Size(620, 560);
        this.tabControlMain.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
        this.tabControlMain.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
        this.tabControlMain.TabIndex = 0;

        // =======================
        // tabYoutube
        // =======================
        this.tabYoutube.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
        this.tabYoutube.Controls.Add(this.lblUrl);
        this.tabYoutube.Controls.Add(this.panelUrlContainer);
        this.tabYoutube.Controls.Add(this.btnLoad);
        this.tabYoutube.Controls.Add(this.panelInfo);
        this.tabYoutube.Controls.Add(this.lblQuality);
        this.tabYoutube.Controls.Add(this.cmbQuality);
        this.tabYoutube.Controls.Add(this.btnAddQueue);
        this.tabYoutube.Controls.Add(this.lvQueue);
        this.tabYoutube.Controls.Add(this.btnRemoveSelected);
        this.tabYoutube.Controls.Add(this.lblStatus);
        this.tabYoutube.Controls.Add(this.pbYoutube);
        this.tabYoutube.Location = new System.Drawing.Point(4, 5);
        this.tabYoutube.Name = "tabYoutube";
        this.tabYoutube.Padding = new System.Windows.Forms.Padding(3);
        this.tabYoutube.Size = new System.Drawing.Size(612, 551);
        this.tabYoutube.TabIndex = 0;

        // lblUrl
        this.lblUrl.AutoSize = true;
        this.lblUrl.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        this.lblUrl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
        this.lblUrl.Location = new System.Drawing.Point(20, 20);
        this.lblUrl.Name = "lblUrl";
        this.lblUrl.Size = new System.Drawing.Size(95, 19);
        this.lblUrl.TabIndex = 1;
        this.lblUrl.Text = "유튜브 URL 입력";

        // panelUrlContainer
        this.panelUrlContainer.BackColor = System.Drawing.Color.White;
        this.panelUrlContainer.Controls.Add(this.txtUrl);
        this.panelUrlContainer.Location = new System.Drawing.Point(20, 45);
        this.panelUrlContainer.Name = "panelUrlContainer";
        this.panelUrlContainer.Size = new System.Drawing.Size(440, 45);
        this.panelUrlContainer.TabIndex = 2;
        this.panelUrlContainer.Paint += new System.Windows.Forms.PaintEventHandler(this.Panel_Paint);

        // txtUrl
        this.txtUrl.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this.txtUrl.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.txtUrl.Location = new System.Drawing.Point(15, 13);
        this.txtUrl.Name = "txtUrl";
        this.txtUrl.PlaceholderText = "https://www.youtube.com/watch?v=...";
        this.txtUrl.Size = new System.Drawing.Size(410, 20);
        this.txtUrl.TabIndex = 0;

        // btnLoad
        this.btnLoad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(144)))), ((int)(((byte)(255)))));
        this.btnLoad.BorderRadius = 22;
        this.btnLoad.FlatAppearance.BorderSize = 0;
        this.btnLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnLoad.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        this.btnLoad.ForeColor = System.Drawing.Color.White;
        this.btnLoad.Location = new System.Drawing.Point(470, 45);
        this.btnLoad.Name = "btnLoad";
        this.btnLoad.Size = new System.Drawing.Size(120, 45);
        this.btnLoad.TabIndex = 3;
        this.btnLoad.Text = "영상 확인";
        this.btnLoad.UseVisualStyleBackColor = false;
        this.btnLoad.Click += new System.EventHandler(this.BtnLoad_Click);

        // =======================
        // tabYtDlp
        // =======================
        this.tabYtDlp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
        this.tabYtDlp.Controls.Add(this.lblXPrivateMode);
        this.tabYtDlp.Controls.Add(this.tglXPrivateMode);
        this.tabYtDlp.Controls.Add(this.panelXBrowser);
        this.tabYtDlp.Controls.Add(this.lblYtDlpSavePath);
        this.tabYtDlp.Controls.Add(this.lblYtDlpTitle);
        this.tabYtDlp.Controls.Add(this.lblYtDlpDesc);
        this.tabYtDlp.Controls.Add(this.panelYtDlpUrl);
        this.tabYtDlp.Controls.Add(this.btnYtDlpRun);
        this.tabYtDlp.Controls.Add(this.btnYtDlpCancel);
        this.tabYtDlp.Controls.Add(this.lblYtDlpStatus);
        this.tabYtDlp.Controls.Add(this.pbYtDlp);
        this.tabYtDlp.Location = new System.Drawing.Point(4, 5);
        this.tabYtDlp.Name = "tabYtDlp";
        this.tabYtDlp.Size = new System.Drawing.Size(612, 551);
        this.tabYtDlp.TabIndex = 6;

        // lblYtDlpTitle
        this.lblYtDlpTitle.AutoSize = true;
        this.lblYtDlpTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
        this.lblYtDlpTitle.Location = new System.Drawing.Point(20, 20);
        this.lblYtDlpTitle.Name = "lblYtDlpTitle";
        this.lblYtDlpTitle.Size = new System.Drawing.Size(400, 30);
        this.lblYtDlpTitle.Text = "웹 영상 다운로드";

        // lblYtDlpDesc
        this.lblYtDlpDesc.Font = new System.Drawing.Font("Segoe UI", 9.5F);
        this.lblYtDlpDesc.Location = new System.Drawing.Point(20, 60);
        this.lblYtDlpDesc.Name = "lblYtDlpDesc";
        this.lblYtDlpDesc.Size = new System.Drawing.Size(570, 60);
        this.lblYtDlpDesc.Text = "치지직(video), 인스타 등 다양한 사이트를 지원합니다.\n지원 안내: 치지직은 'video', 'clips' 형태의 VOD 주소만 지원합니다. (live제외)\n※틱톡 영상은 우클릭으로 다운 가능하여 제외";


        // panelYtDlpUrl
        this.panelYtDlpUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
        this.panelYtDlpUrl.BackColor = System.Drawing.Color.White;
        this.panelYtDlpUrl.Controls.Add(this.txtYtDlpUrl);
        this.panelYtDlpUrl.Location = new System.Drawing.Point(20, 130);
        this.panelYtDlpUrl.Name = "panelYtDlpUrl";
        this.panelYtDlpUrl.Size = new System.Drawing.Size(570, 40);
        this.panelYtDlpUrl.Paint += new System.Windows.Forms.PaintEventHandler(this.Panel_Paint);

        // txtYtDlpUrl
        this.txtYtDlpUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
        this.txtYtDlpUrl.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this.txtYtDlpUrl.Font = new System.Drawing.Font("Segoe UI", 11F);
        this.txtYtDlpUrl.Location = new System.Drawing.Point(10, 10);
        this.txtYtDlpUrl.Name = "txtYtDlpUrl";
        this.txtYtDlpUrl.PlaceholderText = "다운로드할 영상의 URL을 입력하세요...";
        this.txtYtDlpUrl.Size = new System.Drawing.Size(550, 20);

        // lblXPrivateMode
        this.lblXPrivateMode.AutoSize = true;
        this.lblXPrivateMode.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
        this.lblXPrivateMode.Location = new System.Drawing.Point(345, 192);
        this.lblXPrivateMode.Name = "lblXPrivateMode";
        this.lblXPrivateMode.Size = new System.Drawing.Size(180, 15);
        this.lblXPrivateMode.Text = "X 비공개 영상 전용 모드";

        // tglXPrivateMode
        this.tglXPrivateMode.Location = new System.Drawing.Point(530, 188);
        this.tglXPrivateMode.Name = "tglXPrivateMode";
        this.tglXPrivateMode.Size = new System.Drawing.Size(60, 25);
        this.tglXPrivateMode.Text = "";
        this.tglXPrivateMode.CheckedChanged += new System.EventHandler(this.TglXPrivateMode_CheckedChanged);

        // panelXBrowser
        this.panelXBrowser.BackColor = System.Drawing.Color.White;
        this.panelXBrowser.Controls.Add(this.webViewX);
        this.panelXBrowser.Controls.Add(this.btnXCapture);
        this.panelXBrowser.Controls.Add(this.btnXDownload);
        this.panelXBrowser.Controls.Add(this.btnXClose);
        this.panelXBrowser.Controls.Add(this.pbXDownload);
        this.panelXBrowser.Controls.Add(this.lblXStatus);
        this.panelXBrowser.Location = new System.Drawing.Point(0, 0);
        this.panelXBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
        this.panelXBrowser.Name = "panelXBrowser";
        this.panelXBrowser.Visible = false;

        // webViewX (NuGet Package Reference Added in previous step)
        this.webViewX.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
        this.webViewX.Location = new System.Drawing.Point(0, 50);
        this.webViewX.Name = "webViewX";
        this.webViewX.Size = new System.Drawing.Size(570, 461);

        // btnXCapture
        this.btnXCapture.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
        this.btnXCapture.BorderRadius = 15;
        this.btnXCapture.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnXCapture.FlatAppearance.BorderSize = 0;
        this.btnXCapture.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
        this.btnXCapture.ForeColor = System.Drawing.Color.White;
        this.btnXCapture.Location = new System.Drawing.Point(10, 10);
        this.btnXCapture.Name = "btnXCapture";
        this.btnXCapture.Size = new System.Drawing.Size(120, 32);
        this.btnXCapture.Text = "1 주소 가져오기";
        this.btnXCapture.Click += new System.EventHandler(this.BtnXCapture_Click);

        // btnXDownload
        this.btnXDownload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(89)))), ((int)(((byte)(182)))));
        this.btnXDownload.BorderRadius = 15;
        this.btnXDownload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnXDownload.FlatAppearance.BorderSize = 0;
        this.btnXDownload.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
        this.btnXDownload.ForeColor = System.Drawing.Color.White;
        this.btnXDownload.Location = new System.Drawing.Point(140, 10);
        this.btnXDownload.Name = "btnXDownload";
        this.btnXDownload.Size = new System.Drawing.Size(140, 32);
        this.btnXDownload.Text = "2 바로 다운";
        this.btnXDownload.Click += new System.EventHandler(this.BtnXDownload_Click);

        // btnXClose
        this.btnXClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnXClose.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(149)))), ((int)(((byte)(165)))), ((int)(((byte)(166)))));
        this.btnXClose.BorderRadius = 15;
        this.btnXClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnXClose.FlatAppearance.BorderSize = 0;
        this.btnXClose.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
        this.btnXClose.ForeColor = System.Drawing.Color.White;
        this.btnXClose.Location = new System.Drawing.Point(490, 10);
        this.btnXClose.Name = "btnXClose";
        this.btnXClose.Size = new System.Drawing.Size(70, 32);
        this.btnXClose.Text = "닫기";
        this.btnXClose.Click += new System.EventHandler(this.BtnXClose_Click);

        // pbXDownload
        this.pbXDownload.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
        this.pbXDownload.Location = new System.Drawing.Point(10, 520);
        this.pbXDownload.Name = "pbXDownload";
        this.pbXDownload.Size = new System.Drawing.Size(550, 15);
        this.pbXDownload.Style = System.Windows.Forms.ProgressBarStyle.Continuous;

        // lblXStatus
        this.lblXStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
        this.lblXStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.lblXStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(89)))), ((int)(((byte)(182)))));
        this.lblXStatus.Location = new System.Drawing.Point(10, 540);
        this.lblXStatus.Name = "lblXStatus";
        this.lblXStatus.Size = new System.Drawing.Size(550, 20);
        this.lblXStatus.Text = "영상 페이지(status)로 이동해 주세요.";
        this.lblXStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // btnYtDlpRun
        this.btnYtDlpRun.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
        this.btnYtDlpRun.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(89)))), ((int)(((byte)(182)))));
        this.btnYtDlpRun.BorderRadius = 18;
        this.btnYtDlpRun.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnYtDlpRun.FlatAppearance.BorderSize = 0;
        this.btnYtDlpRun.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
        this.btnYtDlpRun.ForeColor = System.Drawing.Color.White;
        this.btnYtDlpRun.Location = new System.Drawing.Point(20, 182);
        this.btnYtDlpRun.Name = "btnYtDlpRun";
        this.btnYtDlpRun.Size = new System.Drawing.Size(150, 38);
        this.btnYtDlpRun.Text = "다운로드 시작";
        this.btnYtDlpRun.Click += new System.EventHandler(this.BtnYtDlpRun_Click);

        // btnYtDlpCancel
        this.btnYtDlpCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
        this.btnYtDlpCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(231)))), ((int)(((byte)(76)))), ((int)(((byte)(60)))));
        this.btnYtDlpCancel.BorderRadius = 18;
        this.btnYtDlpCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnYtDlpCancel.FlatAppearance.BorderSize = 0;
        this.btnYtDlpCancel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
        this.btnYtDlpCancel.ForeColor = System.Drawing.Color.White;
        this.btnYtDlpCancel.Location = new System.Drawing.Point(180, 182);
        this.btnYtDlpCancel.Name = "btnYtDlpCancel";
        this.btnYtDlpCancel.Size = new System.Drawing.Size(120, 38);
        this.btnYtDlpCancel.Text = "취소";
        this.btnYtDlpCancel.Visible = false;
        this.btnYtDlpCancel.Click += new System.EventHandler(this.BtnYtDlpCancel_Click);

        // lblYtDlpStatus
        this.lblYtDlpStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
        this.lblYtDlpStatus.Location = new System.Drawing.Point(20, 295);
        this.lblYtDlpStatus.Name = "lblYtDlpStatus";
        this.lblYtDlpStatus.Size = new System.Drawing.Size(570, 20);
        this.lblYtDlpStatus.Text = "대기 중...";

        // pbYtDlp
        this.pbYtDlp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
        this.pbYtDlp.Location = new System.Drawing.Point(20, 325);
        this.pbYtDlp.Name = "pbYtDlp";
        this.pbYtDlp.Size = new System.Drawing.Size(570, 20);
        this.pbYtDlp.Style = System.Windows.Forms.ProgressBarStyle.Continuous;

        // lblYtDlpSavePath
        this.lblYtDlpSavePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
        this.lblYtDlpSavePath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Underline);
        this.lblYtDlpSavePath.ForeColor = System.Drawing.Color.Blue;
        this.lblYtDlpSavePath.Cursor = System.Windows.Forms.Cursors.Hand;
        this.lblYtDlpSavePath.Location = new System.Drawing.Point(20, 355);
        this.lblYtDlpSavePath.Name = "lblYtDlpSavePath";
        this.lblYtDlpSavePath.Size = new System.Drawing.Size(570, 35);
        this.lblYtDlpSavePath.Text = "저장 위치: ";
        this.lblYtDlpSavePath.Click += new System.EventHandler(this.LblYtDlpSavePath_Click);

        // panelInfo
        this.panelInfo.BackColor = System.Drawing.Color.White;
        this.panelInfo.Controls.Add(this.lblVideoTitle);
        this.panelInfo.Controls.Add(this.txtEditTitle);
        this.panelInfo.Controls.Add(this.picThumbnail);
        this.panelInfo.Location = new System.Drawing.Point(20, 110);
        this.panelInfo.Name = "panelInfo";
        this.panelInfo.Size = new System.Drawing.Size(570, 100);
        this.panelInfo.TabIndex = 4;
        this.panelInfo.Paint += new System.Windows.Forms.PaintEventHandler(this.Panel_Paint);

        // lblVideoTitle
        this.lblVideoTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.lblVideoTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
        this.lblVideoTitle.Location = new System.Drawing.Point(170, 15);
        this.lblVideoTitle.Name = "lblVideoTitle";
        this.lblVideoTitle.Size = new System.Drawing.Size(390, 70);
        this.lblVideoTitle.TabIndex = 1;
        this.lblVideoTitle.Text = "위 입력칸에 유튜브 URL을 붙여넣고 영상 확인을 클릭하세요.";
        this.lblVideoTitle.DoubleClick += new System.EventHandler(this.LblVideoTitle_DoubleClick);

        // txtEditTitle
        this.txtEditTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.txtEditTitle.Location = new System.Drawing.Point(170, 15);
        this.txtEditTitle.Name = "txtEditTitle";
        this.txtEditTitle.Size = new System.Drawing.Size(380, 25);
        this.txtEditTitle.TabIndex = 2;
        this.txtEditTitle.Visible = false;
        this.txtEditTitle.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtEditTitle_KeyDown);
        this.txtEditTitle.LostFocus += new System.EventHandler(this.TxtEditTitle_LostFocus);

        // picThumbnail
        this.picThumbnail.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
        this.picThumbnail.Location = new System.Drawing.Point(10, 10);
        this.picThumbnail.Name = "picThumbnail";
        this.picThumbnail.Size = new System.Drawing.Size(142, 80);
        this.picThumbnail.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        this.picThumbnail.TabIndex = 0;
        this.picThumbnail.TabStop = false;

        // lblQuality
        this.lblQuality.AutoSize = true;
        this.lblQuality.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        this.lblQuality.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
        this.lblQuality.Location = new System.Drawing.Point(20, 225);
        this.lblQuality.Name = "lblQuality";
        this.lblQuality.Size = new System.Drawing.Size(73, 19);
        this.lblQuality.TabIndex = 5;
        this.lblQuality.Text = "화질 옵션";

        // cmbQuality
        this.cmbQuality.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cmbQuality.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.cmbQuality.FormattingEnabled = true;
        this.cmbQuality.Location = new System.Drawing.Point(100, 221);
        this.cmbQuality.Name = "cmbQuality";
        this.cmbQuality.Size = new System.Drawing.Size(360, 28);
        this.cmbQuality.TabIndex = 6;

        // btnAddQueue
        this.btnAddQueue.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(204)))), ((int)(((byte)(113)))));
        this.btnAddQueue.BorderRadius = 18;
        this.btnAddQueue.FlatAppearance.BorderSize = 0;
        this.btnAddQueue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnAddQueue.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        this.btnAddQueue.ForeColor = System.Drawing.Color.White;
        this.btnAddQueue.Location = new System.Drawing.Point(470, 218);
        this.btnAddQueue.Name = "btnAddQueue";
        this.btnAddQueue.Size = new System.Drawing.Size(120, 36);
        this.btnAddQueue.TabIndex = 7;
        this.btnAddQueue.Text = "다운로드/예약";
        this.btnAddQueue.UseVisualStyleBackColor = false;
        this.btnAddQueue.Click += new System.EventHandler(this.BtnAddQueue_Click);

        // contextMenuRemove
        this.contextMenuRemove.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.menuRemoveSelected });
        this.contextMenuRemove.Name = "contextMenuRemove";

        // menuRemoveSelected
        this.menuRemoveSelected.Name = "menuRemoveSelected";
        this.menuRemoveSelected.Size = new System.Drawing.Size(188, 22);
        this.menuRemoveSelected.Text = "선택 항목 취소 (지우기)";
        this.menuRemoveSelected.Click += new System.EventHandler(this.BtnRemoveSelected_Click);

        // lvQueue
        this.lvQueue.BackColor = System.Drawing.Color.White;
        this.lvQueue.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this.lvQueue.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { this.colTitle, this.colQuality, this.colStatus });
        this.lvQueue.ContextMenuStrip = this.contextMenuRemove;
        this.lvQueue.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.lvQueue.FullRowSelect = true;
        this.lvQueue.Location = new System.Drawing.Point(20, 275);
        this.lvQueue.Name = "lvQueue";
        this.lvQueue.Size = new System.Drawing.Size(570, 180);
        this.lvQueue.TabIndex = 8;
        this.lvQueue.UseCompatibleStateImageBehavior = false;
        this.lvQueue.View = System.Windows.Forms.View.Details;

        // colTitle
        this.colTitle.Text = "영상 제목";
        this.colTitle.Width = 320;
        // colQuality
        this.colQuality.Text = "화질/포맷";
        this.colQuality.Width = 120;
        // colStatus
        this.colStatus.Text = "상태";
        this.colStatus.Width = 110;

        // btnRemoveSelected
        this.btnRemoveSelected.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
        this.btnRemoveSelected.BorderRadius = 14;
        this.btnRemoveSelected.FlatAppearance.BorderSize = 0;
        this.btnRemoveSelected.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnRemoveSelected.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        this.btnRemoveSelected.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
        this.btnRemoveSelected.Location = new System.Drawing.Point(470, 465);
        this.btnRemoveSelected.Name = "btnRemoveSelected";
        this.btnRemoveSelected.Size = new System.Drawing.Size(120, 28);
        this.btnRemoveSelected.TabIndex = 10;
        this.btnRemoveSelected.Text = "선택 목록 취소";
        this.btnRemoveSelected.UseVisualStyleBackColor = false;
        this.btnRemoveSelected.Click += new System.EventHandler(this.BtnRemoveSelected_Click);

        // lblStatus
        this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.lblStatus.ForeColor = System.Drawing.Color.DarkGray;
        this.lblStatus.Location = new System.Drawing.Point(20, 470);
        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Size = new System.Drawing.Size(430, 20);
        this.lblStatus.TabIndex = 9;
        this.lblStatus.Text = "* 대기열 항목 우클릭으로도 취소가 가능합니다.";

        // pbYoutube
        this.pbYoutube.Location = new System.Drawing.Point(20, 500);
        this.pbYoutube.Name = "pbYoutube";
        this.pbYoutube.Size = new System.Drawing.Size(570, 20);
        this.pbYoutube.TabIndex = 11;
        this.pbYoutube.Style = System.Windows.Forms.ProgressBarStyle.Continuous;


        // =======================
        // tabWebM
        // =======================
        this.tabWebM.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
        this.tabWebM.Controls.Add(this.lblWebMTitle);
        this.tabWebM.Controls.Add(this.panelWebMInput);
        this.tabWebM.Controls.Add(this.btnBrowseWebM);
        this.tabWebM.Controls.Add(this.panelWebMOutput);
        this.tabWebM.Controls.Add(this.btnBrowseWebMOutput);
        this.tabWebM.Controls.Add(this.btnConvertWebM);
        this.tabWebM.Controls.Add(this.btnCancelWebM);
        this.tabWebM.Controls.Add(this.cmbWebMFormat);
        this.tabWebM.Controls.Add(this.lblWebMStatus);
        this.tabWebM.Controls.Add(this.pbWebM);
        this.tabWebM.Location = new System.Drawing.Point(4, 5);
        this.tabWebM.Name = "tabWebM";
        this.tabWebM.Size = new System.Drawing.Size(612, 551);
        this.tabWebM.TabIndex = 1;

        // lblWebMTitle
        this.lblWebMTitle.AutoSize = true;
        this.lblWebMTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        this.lblWebMTitle.Location = new System.Drawing.Point(20, 20);
        this.lblWebMTitle.Name = "lblWebMTitle";
        this.lblWebMTitle.Size = new System.Drawing.Size(400, 30);
        this.lblWebMTitle.Text = "영상 포맷 변환기";
        
        // panelWebMInput
        this.panelWebMInput.BackColor = System.Drawing.Color.White;
        this.panelWebMInput.Controls.Add(this.txtWebMInput);
        this.panelWebMInput.Location = new System.Drawing.Point(20, 80);
        this.panelWebMInput.Name = "panelWebMInput";
        this.panelWebMInput.Size = new System.Drawing.Size(450, 40);
        this.panelWebMInput.Paint += new System.Windows.Forms.PaintEventHandler(this.Panel_Paint);

        // txtWebMInput
        this.txtWebMInput.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this.txtWebMInput.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.txtWebMInput.Location = new System.Drawing.Point(10, 10);
        this.txtWebMInput.Name = "txtWebMInput";
        this.txtWebMInput.PlaceholderText = "MP4 파일 경로를 선택하세요...";
        this.txtWebMInput.Size = new System.Drawing.Size(430, 20);
        
        // btnBrowseWebM
        this.btnBrowseWebM.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
        this.btnBrowseWebM.BorderRadius = 20;
        this.btnBrowseWebM.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnBrowseWebM.FlatAppearance.BorderSize = 0;
        this.btnBrowseWebM.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.btnBrowseWebM.Location = new System.Drawing.Point(490, 80);
        this.btnBrowseWebM.Name = "btnBrowseWebM";
        this.btnBrowseWebM.Size = new System.Drawing.Size(100, 40);
        this.btnBrowseWebM.Text = "찾아보기";
        this.btnBrowseWebM.Click += new System.EventHandler(this.BtnBrowseWebM_Click);

        // btnCancelWebM
        this.btnCancelWebM.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(231)))), ((int)(((byte)(76)))), ((int)(((byte)(60)))));
        this.btnCancelWebM.BorderRadius = 25;
        this.btnCancelWebM.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnCancelWebM.FlatAppearance.BorderSize = 0;
        this.btnCancelWebM.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.btnCancelWebM.ForeColor = System.Drawing.Color.White;
        this.btnCancelWebM.Location = new System.Drawing.Point(470, 220);
        this.btnCancelWebM.Name = "btnCancelWebM";
        this.btnCancelWebM.Size = new System.Drawing.Size(120, 50);
        this.btnCancelWebM.Text = "취소";
        this.btnCancelWebM.Visible = false;
        this.btnCancelWebM.Click += new System.EventHandler(this.BtnCancelWebM_Click);

        // panelWebMOutput
        this.panelWebMOutput.BackColor = System.Drawing.Color.White;
        this.panelWebMOutput.Controls.Add(this.txtWebMOutput);
        this.panelWebMOutput.Location = new System.Drawing.Point(20, 130);
        this.panelWebMOutput.Name = "panelWebMOutput";
        this.panelWebMOutput.Size = new System.Drawing.Size(450, 40);
        this.panelWebMOutput.Paint += new System.Windows.Forms.PaintEventHandler(this.Panel_Paint);

        // txtWebMOutput
        this.txtWebMOutput.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this.txtWebMOutput.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.txtWebMOutput.Location = new System.Drawing.Point(10, 10);
        this.txtWebMOutput.Name = "txtWebMOutput";
        this.txtWebMOutput.PlaceholderText = "저장 위치 (기본: 원본 폴더)...";
        this.txtWebMOutput.Size = new System.Drawing.Size(430, 20);

        // cmbWebMFormat
        this.cmbWebMFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cmbWebMFormat.Font = new System.Drawing.Font("Segoe UI", 11F);
        this.cmbWebMFormat.FormattingEnabled = true;
        this.cmbWebMFormat.Items.AddRange(new object[] {
            "GIF (.gif)",
            "WebM (.webm)",
            "JPG Sequence (.jpg)",
            "PNG Sequence (.png)",
            "MOV (.mov)",
            "MKV (.mkv)",
            "AVI (.avi)",
            "WMV (.wmv)"
        });
        this.cmbWebMFormat.Location = new System.Drawing.Point(20, 180);
        this.cmbWebMFormat.Name = "cmbWebMFormat";
        this.cmbWebMFormat.Size = new System.Drawing.Size(570, 28);
        this.cmbWebMFormat.SelectedIndex = 0;

        // btnBrowseWebMOutput
        this.btnBrowseWebMOutput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
        this.btnBrowseWebMOutput.BorderRadius = 20;
        this.btnBrowseWebMOutput.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnBrowseWebMOutput.FlatAppearance.BorderSize = 0;
        this.btnBrowseWebMOutput.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.btnBrowseWebMOutput.Location = new System.Drawing.Point(490, 130);
        this.btnBrowseWebMOutput.Name = "btnBrowseWebMOutput";
        this.btnBrowseWebMOutput.Size = new System.Drawing.Size(100, 40);
        this.btnBrowseWebMOutput.Text = "위치 지정";
        this.btnBrowseWebMOutput.Click += new System.EventHandler(this.BtnBrowseWebMOutput_Click);

        // btnConvertWebM
        this.btnConvertWebM.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(204)))), ((int)(((byte)(113)))));
        this.btnConvertWebM.BorderRadius = 25;
        this.btnConvertWebM.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnConvertWebM.FlatAppearance.BorderSize = 0;
        this.btnConvertWebM.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.btnConvertWebM.ForeColor = System.Drawing.Color.White;
        this.btnConvertWebM.Location = new System.Drawing.Point(20, 220);
        this.btnConvertWebM.Name = "btnConvertWebM";
        this.btnConvertWebM.Size = new System.Drawing.Size(440, 50);
        this.btnConvertWebM.Text = "변환 시작";
        this.btnConvertWebM.Click += new System.EventHandler(this.BtnConvertWebM_Click);

        // lblWebMStatus
        this.lblWebMStatus.Location = new System.Drawing.Point(20, 285);
        this.lblWebMStatus.Name = "lblWebMStatus";
        this.lblWebMStatus.Size = new System.Drawing.Size(570, 20);
        this.lblWebMStatus.Text = "대기 중...";
 
        // pbWebM
        this.pbWebM.Location = new System.Drawing.Point(20, 315);
        this.pbWebM.Name = "pbWebM";
        this.pbWebM.Size = new System.Drawing.Size(570, 20);
        this.pbWebM.TabIndex = 10;
        this.pbWebM.Style = System.Windows.Forms.ProgressBarStyle.Continuous;

        // =======================
        // tabCodec
        // =======================
        this.tabCodec.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
        this.tabCodec.Controls.Add(this.lblCodecTitle);
        this.tabCodec.Controls.Add(this.lblCodecDesc);
        this.tabCodec.Controls.Add(this.panelCodecInput);
        this.tabCodec.Controls.Add(this.btnBrowseCodec);
        this.tabCodec.Controls.Add(this.panelCodecOutput);
        this.tabCodec.Controls.Add(this.btnBrowseCodecOutput);
        this.tabCodec.Controls.Add(this.btnConvertCodec);
        this.tabCodec.Controls.Add(this.btnCancelCodec);
        this.tabCodec.Controls.Add(this.lblCodecStatus);
        this.tabCodec.Controls.Add(this.pbCodec);
        this.tabCodec.Location = new System.Drawing.Point(4, 5);
        this.tabCodec.Name = "tabCodec";
        this.tabCodec.Size = new System.Drawing.Size(612, 551);
        this.tabCodec.TabIndex = 2;

        // lblCodecTitle
        this.lblCodecTitle.AutoSize = true;
        this.lblCodecTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        this.lblCodecTitle.Location = new System.Drawing.Point(20, 20);
        this.lblCodecTitle.Text = "프리미어 프로 / 에프터이펙트 코덱 해결";
        
        // lblCodecDesc
        this.lblCodecDesc.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
        this.lblCodecDesc.Location = new System.Drawing.Point(20, 55);
        this.lblCodecDesc.Size = new System.Drawing.Size(570, 40);
        this.lblCodecDesc.Text = "영상을 프리미어 프로나 에프터이펙트에 넣었을 때 화면이 안 나오고 소리만 나오는 현상을 해결합니다.\n(H.264 / AAC 코덱으로 재인코딩)";
        
        // panelCodecInput
        this.panelCodecInput.BackColor = System.Drawing.Color.White;
        this.panelCodecInput.Controls.Add(this.txtCodecInput);
        this.panelCodecInput.Location = new System.Drawing.Point(20, 100);
        this.panelCodecInput.Name = "panelCodecInput";
        this.panelCodecInput.Size = new System.Drawing.Size(450, 40);
        this.panelCodecInput.Paint += new System.Windows.Forms.PaintEventHandler(this.Panel_Paint);

        // txtCodecInput
        this.txtCodecInput.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this.txtCodecInput.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.txtCodecInput.Location = new System.Drawing.Point(10, 10);
        this.txtCodecInput.Name = "txtCodecInput";
        this.txtCodecInput.PlaceholderText = "원본 MP4 파일 경로를 선택하세요...";
        this.txtCodecInput.Size = new System.Drawing.Size(430, 20);
        
        // btnBrowseCodec
        this.btnBrowseCodec.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
        this.btnBrowseCodec.BorderRadius = 20;
        this.btnBrowseCodec.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnBrowseCodec.FlatAppearance.BorderSize = 0;
        this.btnBrowseCodec.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.btnBrowseCodec.Location = new System.Drawing.Point(490, 100);
        this.btnBrowseCodec.Name = "btnBrowseCodec";
        this.btnBrowseCodec.Size = new System.Drawing.Size(100, 40);
        this.btnBrowseCodec.Text = "찾아보기";
        this.btnBrowseCodec.Click += new System.EventHandler(this.BtnBrowseCodec_Click);

        // btnCancelCodec
        this.btnCancelCodec.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(231)))), ((int)(((byte)(76)))), ((int)(((byte)(60)))));
        this.btnCancelCodec.BorderRadius = 25;
        this.btnCancelCodec.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnCancelCodec.FlatAppearance.BorderSize = 0;
        this.btnCancelCodec.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.btnCancelCodec.ForeColor = System.Drawing.Color.White;
        this.btnCancelCodec.Location = new System.Drawing.Point(470, 210);
        this.btnCancelCodec.Name = "btnCancelCodec";
        this.btnCancelCodec.Size = new System.Drawing.Size(120, 50);
        this.btnCancelCodec.Text = "취소";
        this.btnCancelCodec.Visible = false;
        this.btnCancelCodec.Click += new System.EventHandler(this.BtnCancelCodec_Click);


        // panelCodecOutput
        this.panelCodecOutput.BackColor = System.Drawing.Color.White;
        this.panelCodecOutput.Controls.Add(this.txtCodecOutput);
        this.panelCodecOutput.Location = new System.Drawing.Point(20, 150);
        this.panelCodecOutput.Name = "panelCodecOutput";
        this.panelCodecOutput.Size = new System.Drawing.Size(450, 40);
        this.panelCodecOutput.Paint += new System.Windows.Forms.PaintEventHandler(this.Panel_Paint);

        // txtCodecOutput
        this.txtCodecOutput.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this.txtCodecOutput.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.txtCodecOutput.Location = new System.Drawing.Point(10, 10);
        this.txtCodecOutput.Name = "txtCodecOutput";
        this.txtCodecOutput.PlaceholderText = "저장 위치 (기본: 원본 폴더)...";
        this.txtCodecOutput.Size = new System.Drawing.Size(430, 20);

        // btnBrowseCodecOutput
        this.btnBrowseCodecOutput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
        this.btnBrowseCodecOutput.BorderRadius = 20;
        this.btnBrowseCodecOutput.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnBrowseCodecOutput.FlatAppearance.BorderSize = 0;
        this.btnBrowseCodecOutput.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.btnBrowseCodecOutput.Location = new System.Drawing.Point(490, 150);
        this.btnBrowseCodecOutput.Name = "btnBrowseCodecOutput";
        this.btnBrowseCodecOutput.Size = new System.Drawing.Size(100, 40);
        this.btnBrowseCodecOutput.Text = "위치 지정";
        this.btnBrowseCodecOutput.Click += new System.EventHandler(this.BtnBrowseCodecOutput_Click);

        // btnConvertCodec
        this.btnConvertCodec.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(89)))), ((int)(((byte)(182)))));
        this.btnConvertCodec.BorderRadius = 25;
        this.btnConvertCodec.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnConvertCodec.FlatAppearance.BorderSize = 0;
        this.btnConvertCodec.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.btnConvertCodec.ForeColor = System.Drawing.Color.White;
        this.btnConvertCodec.Location = new System.Drawing.Point(20, 210);
        this.btnConvertCodec.Name = "btnConvertCodec";
        this.btnConvertCodec.Size = new System.Drawing.Size(440, 50);
        this.btnConvertCodec.Text = "코덱 변환 시작";
        this.btnConvertCodec.Click += new System.EventHandler(this.BtnConvertCodec_Click);

        // lblCodecStatus
        this.lblCodecStatus.Location = new System.Drawing.Point(20, 275);
        this.lblCodecStatus.Name = "lblCodecStatus";
        this.lblCodecStatus.Size = new System.Drawing.Size(570, 20);
        this.lblCodecStatus.Text = "대기 중...";

        // pbCodec
        this.pbCodec.Location = new System.Drawing.Point(20, 305);
        this.pbCodec.Name = "pbCodec";
        this.pbCodec.Size = new System.Drawing.Size(570, 20);
        this.pbCodec.TabIndex = 12;
        this.pbCodec.Style = System.Windows.Forms.ProgressBarStyle.Continuous;


        // =======================
        // tabAudio
        // =======================
        this.tabAudio.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
        this.tabAudio.Controls.Add(this.lblAudioTitle);
        this.tabAudio.Controls.Add(this.panelAudioInput);
        this.tabAudio.Controls.Add(this.btnBrowseAudio);
        this.tabAudio.Controls.Add(this.panelAudioOutput);
        this.tabAudio.Controls.Add(this.btnBrowseAudioOutput);
        this.tabAudio.Controls.Add(this.cmbAudioFormat);
        this.tabAudio.Controls.Add(this.btnConvertAudio);
        this.tabAudio.Controls.Add(this.btnCancelAudio);
        this.tabAudio.Controls.Add(this.lblAudioStatus);
        this.tabAudio.Controls.Add(this.pbAudio);
        this.tabAudio.Location = new System.Drawing.Point(4, 5);
        this.tabAudio.Name = "tabAudio";
        this.tabAudio.Size = new System.Drawing.Size(612, 551);
        this.tabAudio.TabIndex = 3;

        // lblAudioTitle
        this.lblAudioTitle.AutoSize = true;
        this.lblAudioTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        this.lblAudioTitle.Location = new System.Drawing.Point(20, 20);
        this.lblAudioTitle.Name = "lblAudioTitle";
        this.lblAudioTitle.Size = new System.Drawing.Size(150, 30);
        this.lblAudioTitle.Text = "오디오 변환기";
        
        // panelAudioInput
        this.panelAudioInput.BackColor = System.Drawing.Color.White;
        this.panelAudioInput.Controls.Add(this.txtAudioInput);
        this.panelAudioInput.Location = new System.Drawing.Point(20, 80);
        this.panelAudioInput.Name = "panelAudioInput";
        this.panelAudioInput.Size = new System.Drawing.Size(450, 40);
        this.panelAudioInput.Paint += new System.Windows.Forms.PaintEventHandler(this.Panel_Paint);

        // txtAudioInput
        this.txtAudioInput.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this.txtAudioInput.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.txtAudioInput.Location = new System.Drawing.Point(10, 10);
        this.txtAudioInput.Name = "txtAudioInput";
        this.txtAudioInput.PlaceholderText = "오디오/비디오 파일 경로를 선택하세요...";
        this.txtAudioInput.Size = new System.Drawing.Size(430, 20);
        
        // btnBrowseAudio
        this.btnBrowseAudio.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
        this.btnBrowseAudio.BorderRadius = 20;
        this.btnBrowseAudio.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnBrowseAudio.FlatAppearance.BorderSize = 0;
        this.btnBrowseAudio.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.btnBrowseAudio.Location = new System.Drawing.Point(490, 80);
        this.btnBrowseAudio.Name = "btnBrowseAudio";
        this.btnBrowseAudio.Size = new System.Drawing.Size(100, 40);
        this.btnBrowseAudio.Text = "찾아보기";
        this.btnBrowseAudio.Click += new System.EventHandler(this.BtnBrowseAudio_Click);

        // panelAudioOutput
        this.panelAudioOutput.BackColor = System.Drawing.Color.White;
        this.panelAudioOutput.Controls.Add(this.txtAudioOutput);
        this.panelAudioOutput.Location = new System.Drawing.Point(20, 130);
        this.panelAudioOutput.Name = "panelAudioOutput";
        this.panelAudioOutput.Size = new System.Drawing.Size(450, 40);
        this.panelAudioOutput.Paint += new System.Windows.Forms.PaintEventHandler(this.Panel_Paint);

        // txtAudioOutput
        this.txtAudioOutput.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this.txtAudioOutput.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.txtAudioOutput.Location = new System.Drawing.Point(10, 10);
        this.txtAudioOutput.Name = "txtAudioOutput";
        this.txtAudioOutput.PlaceholderText = "저장 위치 (기본: 원본 폴더)...";
        this.txtAudioOutput.Size = new System.Drawing.Size(430, 20);

        // btnBrowseAudioOutput
        this.btnBrowseAudioOutput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
        this.btnBrowseAudioOutput.BorderRadius = 20;
        this.btnBrowseAudioOutput.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnBrowseAudioOutput.FlatAppearance.BorderSize = 0;
        this.btnBrowseAudioOutput.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.btnBrowseAudioOutput.Location = new System.Drawing.Point(490, 130);
        this.btnBrowseAudioOutput.Name = "btnBrowseAudioOutput";
        this.btnBrowseAudioOutput.Size = new System.Drawing.Size(100, 40);
        this.btnBrowseAudioOutput.Text = "위치 지정";
        this.btnBrowseAudioOutput.Click += new System.EventHandler(this.BtnBrowseAudioOutput_Click);

        // cmbAudioFormat
        this.cmbAudioFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cmbAudioFormat.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.cmbAudioFormat.FormattingEnabled = true;
        this.cmbAudioFormat.Location = new System.Drawing.Point(20, 180);
        this.cmbAudioFormat.Items.AddRange(new object[] {
            "MP3",
            "WAV",
            "M4A",
            "FLAC",
            "OGG"
        });
        this.cmbAudioFormat.SelectedIndex = 0;
        this.cmbAudioFormat.Name = "cmbAudioFormat";
        this.cmbAudioFormat.Size = new System.Drawing.Size(570, 28);

        // btnConvertAudio
        this.btnConvertAudio.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(204)))), ((int)(((byte)(113)))));
        this.btnConvertAudio.BorderRadius = 25;
        this.btnConvertAudio.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnConvertAudio.FlatAppearance.BorderSize = 0;
        this.btnConvertAudio.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.btnConvertAudio.ForeColor = System.Drawing.Color.White;
        this.btnConvertAudio.Location = new System.Drawing.Point(20, 230);
        this.btnConvertAudio.Name = "btnConvertAudio";
        this.btnConvertAudio.Size = new System.Drawing.Size(440, 50);
        this.btnConvertAudio.Text = "오디오 변환 시작";
        this.btnConvertAudio.Click += new System.EventHandler(this.BtnConvertAudio_Click);

        // btnCancelAudio
        this.btnCancelAudio.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(231)))), ((int)(((byte)(76)))), ((int)(((byte)(60)))));
        this.btnCancelAudio.BorderRadius = 25;
        this.btnCancelAudio.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnCancelAudio.FlatAppearance.BorderSize = 0;
        this.btnCancelAudio.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.btnCancelAudio.ForeColor = System.Drawing.Color.White;
        this.btnCancelAudio.Location = new System.Drawing.Point(470, 230);
        this.btnCancelAudio.Name = "btnCancelAudio";
        this.btnCancelAudio.Size = new System.Drawing.Size(120, 50);
        this.btnCancelAudio.Text = "취소";
        this.btnCancelAudio.Visible = false;
        this.btnCancelAudio.Click += new System.EventHandler(this.BtnCancelAudio_Click);

        // lblAudioStatus
        this.lblAudioStatus.Location = new System.Drawing.Point(20, 295);
        this.lblAudioStatus.Name = "lblAudioStatus";
        this.lblAudioStatus.Size = new System.Drawing.Size(570, 20);
        this.lblAudioStatus.Text = "대기 중...";

        this.tabMiniEdit = new System.Windows.Forms.TabPage();
        this.miniEditorControl = new YoutubeDownloader.MiniEditor();
        this.tabMiniEdit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
        this.tabMiniEdit.Controls.Add(this.miniEditorControl);
        this.tabMiniEdit.Location = new System.Drawing.Point(4, 5);
        this.tabMiniEdit.Name = "tabMiniEdit";
        this.tabMiniEdit.Size = new System.Drawing.Size(612, 551);
        this.tabMiniEdit.TabIndex = 6;

        this.miniEditorControl.Dock = System.Windows.Forms.DockStyle.Fill;
        this.miniEditorControl.Location = new System.Drawing.Point(0, 0);
        this.miniEditorControl.Name = "miniEditorControl";
        this.miniEditorControl.Size = new System.Drawing.Size(612, 551);
        this.miniEditorControl.TabIndex = 0;

        this.tabControlMain.Controls.Add(this.tabMiniEdit);

        // pbAudio
        this.pbAudio.Location = new System.Drawing.Point(20, 325);
        this.pbAudio.Name = "pbAudio";
        this.pbAudio.Size = new System.Drawing.Size(570, 20);
        this.pbAudio.TabIndex = 13;
        this.pbAudio.Style = System.Windows.Forms.ProgressBarStyle.Continuous;


        // =======================
        // tabSettings
        // =======================
        this.tabSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
        this.tabSettings.Controls.Add(this.lblAbout);
        this.tabSettings.Controls.Add(this.lblSettingsTitle);
        this.tabSettings.Controls.Add(this.chkShowNotifications);
        this.tabSettings.Controls.Add(this.lblDownloadFolder);
        this.tabSettings.Controls.Add(this.panelSettingsFolder);
        this.tabSettings.Controls.Add(this.btnBrowseFolder);
        this.tabSettings.Controls.Add(this.btnSaveSettings);
        this.tabSettings.Location = new System.Drawing.Point(4, 5);
        this.tabSettings.Name = "tabSettings";
        this.tabSettings.Size = new System.Drawing.Size(612, 551);
        this.tabSettings.TabIndex = 3;

        // lblSettingsTitle
        this.lblSettingsTitle.AutoSize = true;
        this.lblSettingsTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        this.lblSettingsTitle.Location = new System.Drawing.Point(20, 20);
        this.lblSettingsTitle.Text = "프로그램 설정";

        // chkShowNotifications
        this.chkShowNotifications.AutoSize = true;
        this.chkShowNotifications.Font = new System.Drawing.Font("Segoe UI", 11F);
        this.chkShowNotifications.Location = new System.Drawing.Point(20, 80);
        this.chkShowNotifications.Text = "알림 표시 (다운로드 완료/실패)";
        
        // lblDownloadFolder
        this.lblDownloadFolder.AutoSize = true;
        this.lblDownloadFolder.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
        this.lblDownloadFolder.Location = new System.Drawing.Point(20, 130);
        this.lblDownloadFolder.Text = "기본 저장 경로";

        // panelSettingsFolder
        this.panelSettingsFolder.BackColor = System.Drawing.Color.White;
        this.panelSettingsFolder.Controls.Add(this.txtDownloadFolder);
        this.panelSettingsFolder.Location = new System.Drawing.Point(20, 160);
        this.panelSettingsFolder.Name = "panelSettingsFolder";
        this.panelSettingsFolder.Size = new System.Drawing.Size(450, 40);
        this.panelSettingsFolder.Paint += new System.Windows.Forms.PaintEventHandler(this.Panel_Paint);

        // txtDownloadFolder
        this.txtDownloadFolder.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this.txtDownloadFolder.Font = new System.Drawing.Font("Segoe UI", 11F);
        this.txtDownloadFolder.Location = new System.Drawing.Point(10, 10);
        this.txtDownloadFolder.Name = "txtDownloadFolder";
        this.txtDownloadFolder.Size = new System.Drawing.Size(430, 20);
        
        // btnBrowseFolder
        this.btnBrowseFolder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
        this.btnBrowseFolder.BorderRadius = 20;
        this.btnBrowseFolder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnBrowseFolder.FlatAppearance.BorderSize = 0;
        this.btnBrowseFolder.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.btnBrowseFolder.Location = new System.Drawing.Point(490, 160);
        this.btnBrowseFolder.Name = "btnBrowseFolder";
        this.btnBrowseFolder.Size = new System.Drawing.Size(100, 40);
        this.btnBrowseFolder.Text = "경로 변경";
        this.btnBrowseFolder.Click += new System.EventHandler(this.BtnBrowseFolder_Click);

        // btnSaveSettings
        this.btnSaveSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(144)))), ((int)(((byte)(255)))));
        this.btnSaveSettings.BorderRadius = 25;
        this.btnSaveSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnSaveSettings.FlatAppearance.BorderSize = 0;
        this.btnSaveSettings.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.btnSaveSettings.ForeColor = System.Drawing.Color.White;
        this.btnSaveSettings.Location = new System.Drawing.Point(20, 240);
        this.btnSaveSettings.Name = "btnSaveSettings";
        this.btnSaveSettings.Size = new System.Drawing.Size(570, 50);
        this.btnSaveSettings.Text = "설정 저장";
        this.btnSaveSettings.Click += new System.EventHandler(this.BtnSaveSettings_Click);

        // btnFullCleanup
        this.btnFullCleanup.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
        this.btnFullCleanup.BorderRadius = 25;
        this.btnFullCleanup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnFullCleanup.FlatAppearance.BorderSize = 0;
        this.btnFullCleanup.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
        this.btnFullCleanup.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
        this.btnFullCleanup.Location = new System.Drawing.Point(20, 310);
        this.btnFullCleanup.Name = "btnFullCleanup";
        this.btnFullCleanup.Size = new System.Drawing.Size(570, 45);
        this.btnFullCleanup.TabIndex = 8;
        this.btnFullCleanup.Text = "임시 파일 및 시스템 정리 (캐시 제거)";
        this.btnFullCleanup.UseVisualStyleBackColor = false;
        this.btnFullCleanup.Click += new System.EventHandler(this.BtnFullCleanup_Click);

        this.tabSettings.Controls.Add(this.btnFullCleanup);

        // btnCheckUpdate
        this.btnCheckUpdate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(204)))), ((int)(((byte)(113)))));
        this.btnCheckUpdate.BorderRadius = 25;
        this.btnCheckUpdate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnCheckUpdate.FlatAppearance.BorderSize = 0;
        this.btnCheckUpdate.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
        this.btnCheckUpdate.ForeColor = System.Drawing.Color.White;
        this.btnCheckUpdate.Location = new System.Drawing.Point(20, 365);
        this.btnCheckUpdate.Name = "btnCheckUpdate";
        this.btnCheckUpdate.Size = new System.Drawing.Size(570, 45);
        this.btnCheckUpdate.TabIndex = 9;
        this.btnCheckUpdate.Text = "새 버전 업데이트 확인";
        this.btnCheckUpdate.UseVisualStyleBackColor = false;
        this.btnCheckUpdate.Click += new System.EventHandler(this.BtnCheckUpdate_Click);

        this.tabSettings.Controls.Add(this.btnCheckUpdate);

        // lblAbout
        this.lblAbout.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
        this.lblAbout.ForeColor = System.Drawing.Color.Gray;
        this.lblAbout.Location = new System.Drawing.Point(20, 465);
        this.lblAbout.Name = "lblAbout";
        this.lblAbout.Size = new System.Drawing.Size(570, 80);
        this.lblAbout.TabIndex = 7;
        this.lblAbout.Text = "Multi Media Toolkit\nCreated by 김병석\n© 2026 all rights reserved.\n(kbs318@naver.com)";
        this.lblAbout.TextAlign = System.Drawing.ContentAlignment.BottomRight;

        // notifyIconApp
        this.notifyIconApp.Visible = true;
        this.notifyIconApp.Text = "Multi Media Toolkit";
        this.notifyIconApp.Icon = System.Drawing.SystemIcons.Application;
        this.notifyIconApp.ContextMenuStrip = this.contextMenuTray;
        this.notifyIconApp.DoubleClick += new System.EventHandler(this.MenuTrayOpen_Click);

        // contextMenuTray
        this.contextMenuTray.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuTrayOpen,
            new System.Windows.Forms.ToolStripSeparator(),
            this.menuTrayExit
        });
        this.contextMenuTray.Name = "contextMenuTray";
        this.contextMenuTray.Size = new System.Drawing.Size(181, 70);

        // menuTrayOpen
        this.menuTrayOpen.Name = "menuTrayOpen";
        this.menuTrayOpen.Size = new System.Drawing.Size(180, 22);
        this.menuTrayOpen.Text = "열기";
        this.menuTrayOpen.Click += new System.EventHandler(this.MenuTrayOpen_Click);

        // menuTrayExit
        this.menuTrayExit.Name = "menuTrayExit";
        this.menuTrayExit.Size = new System.Drawing.Size(180, 22);
        this.menuTrayExit.Text = "종료";
        this.menuTrayExit.Click += new System.EventHandler(this.MenuTrayExit_Click);

        // Form1
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 600);
        this.MinimumSize = new System.Drawing.Size(800, 600);
        this.Controls.Add(this.panelMain);
        this.Controls.Add(this.panelSidebar);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.Name = "Form1";
        this.Text = "Multi Media Toolkit";
        this.Load += new System.EventHandler(this.Form1_Load);

        this.panelSidebar.ResumeLayout(false);
        this.panelSidebar.PerformLayout();
        this.panelMain.ResumeLayout(false);
        this.tabControlMain.ResumeLayout(false);
        this.tabYoutube.ResumeLayout(false);
        this.tabYoutube.PerformLayout();
        this.panelUrlContainer.ResumeLayout(false);
        this.panelUrlContainer.PerformLayout();
        this.panelInfo.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.picThumbnail)).EndInit();
        this.contextMenuRemove.ResumeLayout(false);
        this.tabWebM.ResumeLayout(false);
        this.tabWebM.PerformLayout();
        this.panelWebMInput.ResumeLayout(false);
        this.panelWebMInput.PerformLayout();
        this.tabCodec.ResumeLayout(false);
        this.tabCodec.PerformLayout();
        this.panelCodecInput.ResumeLayout(false);
        this.panelCodecInput.PerformLayout();
        this.tabSettings.ResumeLayout(false);
        this.tabSettings.PerformLayout();
        this.panelSettingsFolder.ResumeLayout(false);
        this.panelSettingsFolder.PerformLayout();
        this.ResumeLayout(false);
    }
}
