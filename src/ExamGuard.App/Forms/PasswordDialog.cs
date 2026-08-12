using ExamGuard.App.Services;
using ExamGuard.Core.Configuration;

namespace ExamGuard.App.Forms;

public enum PasswordAction
{
    None,
    Unlock,
    Exit,
    DeleteAll
}

/// <summary>
/// Password-protected control dialog. All privileged actions (unlock, change
/// password, exit, delete everything) require the teacher password.
/// </summary>
public sealed class PasswordDialog : Form
{
    private static readonly string[] DurationPresets = { "5", "10", "15", "30", "60", "120" };
    private static readonly Color Accent = Color.FromArgb(37, 99, 235);
    private static readonly Color Danger = Color.FromArgb(220, 38, 38);
    private static readonly Color Neutral = Color.FromArgb(71, 85, 105);

    private readonly AppConfig _config;
    private readonly ConfigStore _store;
    private readonly LockoutGuard _lockout;
    private TextBox _txtPassword = null!;
    private ComboBox _cboMinutes = null!;
    private Label _lblStatus = null!;

    public PasswordAction Result { get; private set; } = PasswordAction.None;

    /// <summary>Minutes the temporary unlock should last (when Result == Unlock).</summary>
    public int UnlockDurationMinutes { get; private set; }

    public PasswordDialog(AppConfig config, ConfigStore store, LockoutGuard lockout)
    {
        _config = config;
        _store = store;
        _lockout = lockout;

        Text = "ExamGuard";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ShowIcon = false;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Font = new Font("Segoe UI", 9F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(24, 20, 24, 18),
            ColumnCount = 1,
            RowCount = 7,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (int i = 0; i < root.RowCount; i++)
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Text = "ExamGuard",
            AutoSize = true,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Accent,
            Margin = new Padding(0, 0, 0, 12),
        }, 0, 0);

        root.Controls.Add(new Label
        {
            Text = "Mật khẩu giáo viên",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6),
        }, 0, 1);

        _txtPassword = new TextBox
        {
            Height = 30,
            Dock = DockStyle.Fill,
            UseSystemPasswordChar = true,
            Margin = new Padding(0, 0, 0, 14),
            BorderStyle = BorderStyle.FixedSingle,
        };
        _txtPassword.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) TryVerify(PasswordAction.Unlock);
        };
        root.Controls.Add(_txtPassword, 0, 2);

        var durationRow = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 16),
        };
        durationRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        durationRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        durationRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        durationRow.Controls.Add(new Label
        {
            Text = "Thời gian mở khóa:",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
        }, 0, 0);
        _cboMinutes = new ComboBox
        {
            Width = 68,
            Height = 30,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(12, 0, 8, 0),
        };
        _cboMinutes.Items.AddRange(DurationPresets);
        int def = Array.IndexOf(DurationPresets, Math.Max(1, _config.UnlockMinutes).ToString());
        _cboMinutes.SelectedIndex = def >= 0 ? def : Array.IndexOf(DurationPresets, "60");
        durationRow.Controls.Add(_cboMinutes, 1, 0);
        durationRow.Controls.Add(new Label
        {
            Text = "phút",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 2, 0, 0),
        }, 2, 0);
        root.Controls.Add(durationRow, 0, 3);

        var buttonRow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 12),
        };
        _btnUnlock = CreateButton("Mở khóa", Accent);
        _btnUnlock.Click += (_, _) => TryVerify(PasswordAction.Unlock);
        _btnExit = CreateButton("Thoát hẳn", Neutral);
        _btnExit.Click += (_, _) => TryVerify(PasswordAction.Exit);
        _btnChange = CreateButton("Đổi mật khẩu", null);
        _btnChange.Click += (_, _) => ChangePasswordFlow();
        buttonRow.Controls.Add(_btnUnlock);
        buttonRow.Controls.Add(_btnExit);
        buttonRow.Controls.Add(_btnChange);
        root.Controls.Add(buttonRow, 0, 4);

        _btnDelete = CreateButton("Xóa toàn bộ ExamGuard", Danger, wide: true);
        _btnDelete.Click += (_, _) => TryVerify(PasswordAction.DeleteAll);
        root.Controls.Add(_btnDelete, 0, 5);

        _lblStatus = new Label
        {
            Text = string.Empty,
            AutoSize = true,
            ForeColor = Danger,
            Margin = new Padding(0, 0, 0, 0),
        };
        root.Controls.Add(_lblStatus, 0, 6);

        Shown += (_, _) => _txtPassword.Focus();
    }

    private Button _btnUnlock = null!;
    private Button _btnExit = null!;
    private Button _btnChange = null!;
    private Button _btnDelete = null!;

    private static Button CreateButton(string text, Color? back, bool wide = false)
    {
        var btn = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 34,
            Margin = new Padding(0, 0, 10, 0),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
        };
        btn.FlatAppearance.BorderSize = 0;
        if (wide)
        {
            btn.Dock = DockStyle.Fill;
            btn.Height = 36;
            btn.Margin = new Padding(0, 0, 0, 16);
            btn.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        }
        if (back.HasValue)
        {
            btn.BackColor = back.Value;
            btn.ForeColor = Color.White;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(back.Value);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(back.Value);
        }
        else
        {
            btn.UseVisualStyleBackColor = true;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(229, 231, 235);
        }
        return btn;
    }

    private void TryVerify(PasswordAction action)
    {
        if (_lockout.IsLocked)
        {
            ShowStatus($"Thử lại sau {_lockout.RemainingSeconds} giây.");
            return;
        }

        if (!_config.HasPassword)
        {
            ShowStatus("Chưa cấu hình mật khẩu.");
            return;
        }

        if (!_config.VerifyPassword(_txtPassword.Text))
        {
            _lockout.RegisterFailure();
            ShowStatus("Sai mật khẩu!");
            _txtPassword.SelectAll();
            _txtPassword.Focus();
            return;
        }

        if (action == PasswordAction.DeleteAll)
        {
            var confirm = MessageBox.Show(this,
                "Xóa toàn bộ ExamGuard?\n\nTất cả file, cấu hình, autostart và watchdog trên máy này sẽ bị xóa.\nKhông thể hoàn tác!",
                "ExamGuard", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (confirm != DialogResult.OK)
                return;
        }

        _lockout.Reset();
        _txtPassword.Clear();
        UnlockDurationMinutes =
            int.TryParse((string?)_cboMinutes.SelectedItem, out int minutes) ? Math.Max(1, minutes) : 60;
        Result = action;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ChangePasswordFlow()
    {
        if (_lockout.IsLocked)
        {
            ShowStatus($"Thử lại sau {_lockout.RemainingSeconds} giây.");
            return;
        }

        if (!_config.HasPassword || !_config.VerifyPassword(_txtPassword.Text))
        {
            _lockout.RegisterFailure();
            ShowStatus("Nhập mật khẩu hiện tại trước khi đổi.");
            return;
        }

        _lockout.Reset();

        var dlg = new ChangePasswordForm();
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _config.SetPassword(dlg.NewPassword);
            _store.Save(_config);
            ShowStatus("Đã đổi mật khẩu thành công.", Color.ForestGreen);
        }
    }

    private void ShowStatus(string message, Color? color = null)
    {
        _lblStatus.Text = message;
        _lblStatus.ForeColor = color ?? Danger;
    }
}

internal sealed class ChangePasswordForm : Form
{
    private readonly TextBox _txtNew = new();
    private readonly TextBox _txtConfirm = new();

    public string NewPassword { get; private set; } = string.Empty;

    public ChangePasswordForm()
    {
        Text = "Đổi mật khẩu";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Font = new Font("Segoe UI", 9F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(24, 18, 24, 16),
            ColumnCount = 1,
            RowCount = 5,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (int i = 0; i < root.RowCount; i++)
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Text = "Mật khẩu mới",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6),
        }, 0, 0);
        _txtNew.Location = new Point(0, 0);
        _txtNew.Width = 300;
        _txtNew.Height = 30;
        _txtNew.UseSystemPasswordChar = true;
        _txtNew.Margin = new Padding(0, 0, 0, 12);
        root.Controls.Add(_txtNew, 0, 1);

        root.Controls.Add(new Label
        {
            Text = "Xác nhận mật khẩu",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6),
        }, 0, 2);
        _txtConfirm.Width = 300;
        _txtConfirm.Height = 30;
        _txtConfirm.UseSystemPasswordChar = true;
        _txtConfirm.Margin = new Padding(0, 0, 0, 16);
        root.Controls.Add(_txtConfirm, 0, 3);

        var buttonRow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
        };
        var cancel = new Button
        {
            Text = "Hủy",
            Width = 90,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(10, 0, 0, 0),
        };
        cancel.FlatAppearance.BorderSize = 0;
        cancel.UseVisualStyleBackColor = true;
        cancel.DialogResult = DialogResult.Cancel;
        var ok = new Button
        {
            Text = "Lưu",
            Width = 90,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
        };
        ok.FlatAppearance.BorderSize = 0;
        ok.FlatAppearance.MouseOverBackColor = ControlPaint.Light(ok.BackColor);
        ok.DialogResult = DialogResult.OK;
        buttonRow.Controls.Add(cancel);
        buttonRow.Controls.Add(ok);
        root.Controls.Add(buttonRow, 0, 4);

        FormClosing += (_, e) =>
        {
            if (DialogResult == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(_txtNew.Text) || _txtNew.Text != _txtConfirm.Text)
                {
                    MessageBox.Show(this, "Mật khẩu không khớp hoặc trống.", "ExamGuard",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }
                NewPassword = _txtNew.Text;
            }
        };
    }
}
