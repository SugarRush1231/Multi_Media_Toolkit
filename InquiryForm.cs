using System;
using System.Drawing;
using System.Windows.Forms;

namespace YoutubeDownloader;

internal sealed class InquiryForm : Form
{
    private readonly TextBox _titleTextBox;
    private readonly TextBox _messageTextBox;
    private readonly Label _countLabel;
    private readonly Label _validationLabel;

    public InquiryForm(string initialTitle = "", string initialMessage = "")
    {
        Text = "문의하기";
        ClientSize = new Size(520, 396);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        BackColor = Color.FromArgb(250, 250, 250);
        Font = new Font("Segoe UI", 9F);

        Controls.Add(new Label
        {
            Text = "문의하기",
            Location = new Point(24, 18),
            AutoSize = true,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42)
        });
        Controls.Add(new Label
        {
            Text = "문제 상황이나 의견을 남겨주시면 제작자에게 바로 전달됩니다.",
            Location = new Point(25, 52),
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 116, 139)
        });

        Controls.Add(CreateFieldLabel("제목", 86));
        _titleTextBox = new TextBox
        {
            Location = new Point(24, 108),
            Size = new Size(472, 28),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "문의 제목을 입력하세요.",
            MaxLength = 100
        };
        _titleTextBox.Text = initialTitle;
        Controls.Add(_titleTextBox);

        Controls.Add(CreateFieldLabel("문의 내용", 154));
        _messageTextBox = new TextBox
        {
            Location = new Point(24, 176),
            Size = new Size(472, 142),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            MaxLength = 1000,
            BorderStyle = BorderStyle.FixedSingle,
            AcceptsReturn = true,
            PlaceholderText = "문의 내용을 입력하세요."
        };
        _messageTextBox.Text = initialMessage;
        _messageTextBox.TextChanged += (s, e) => UpdateCount();
        Controls.Add(_messageTextBox);

        _validationLabel = new Label
        {
            Location = new Point(24, 322),
            Size = new Size(330, 20),
            ForeColor = Color.FromArgb(220, 38, 38)
        };
        Controls.Add(_validationLabel);
        _countLabel = new Label
        {
            Location = new Point(354, 322),
            Size = new Size(142, 20),
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(100, 116, 139)
        };
        Controls.Add(_countLabel);
        UpdateCount();

        var cancelButton = CreateActionButton("취소", new Point(238, 350), Color.FromArgb(255, 241, 242), Color.FromArgb(225, 29, 72));
        cancelButton.DialogResult = DialogResult.Cancel;
        Controls.Add(cancelButton);

        var sendButton = CreateActionButton("전송", new Point(368, 350), Color.FromArgb(2, 132, 199), Color.White);
        sendButton.Click += (s, e) => Submit();
        Controls.Add(sendButton);

        AcceptButton = sendButton;
        CancelButton = cancelButton;
        Shown += (s, e) =>
        {
            TextBox target = string.IsNullOrWhiteSpace(_titleTextBox.Text) ? _titleTextBox : _messageTextBox;
            target.Focus();
            target.SelectionStart = target.TextLength;
        };
    }

    public string InquiryTitle => _titleTextBox.Text.Trim();
    public string Message => _messageTextBox.Text.Trim();

    private void Submit()
    {
        if (string.IsNullOrWhiteSpace(_titleTextBox.Text))
        {
            _validationLabel.Text = "제목을 입력해 주세요.";
            _titleTextBox.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(_messageTextBox.Text))
        {
            _validationLabel.Text = "문의 내용을 입력해 주세요.";
            _messageTextBox.Focus();
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private static Label CreateFieldLabel(string text, int top)
    {
        return new Label
        {
            Text = text,
            Location = new Point(24, top),
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 65, 85)
        };
    }

    private static RoundButton CreateActionButton(string text, Point location, Color backColor, Color foreColor)
    {
        var button = new RoundButton
        {
            Text = text,
            Location = location,
            Size = new Size(128, 36),
            BorderRadius = 12,
            BackColor = backColor,
            ForeColor = foreColor,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private void UpdateCount()
    {
        _countLabel.Text = $"{_messageTextBox.TextLength} / 1000";
        if (_messageTextBox.TextLength > 0) _validationLabel.Text = string.Empty;
    }
}
