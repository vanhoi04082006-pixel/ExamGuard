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
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        Width = 400;
        Height = 258;
        TopMost = true;

        Controls.Add(new Label
        {
            Text = "Nhập mật khẩu giáo viên:",
            AutoSize = true,
            Location = new Point(24, 20)
        });

        _txtPassword = new TextBox
        {
            Location = new Point(24, 44),
            Width = 340,
            UseSystemPasswordChar = true
        };
        _txtPassword.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) TryVerify(PasswordAction.Unlock);
        };
        Controls.Add(_txtPassword);

        Controls.Add(new Label
        {
            Text = "Thời gian mở khóa:",
            AutoSize = true,
            Location = new Point(24, 76)
        });

        _cboMinutes = new ComboBox
        {
            Location = new Point(24, 96),
            Width = 150,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboMinutes.Items.AddRange(DurationPresets);
        int def = Array.IndexOf(DurationPresets, Math.Max(1, _config.UnlockMinutes).ToString());
        _cboMinutes.SelectedIndex = def >= 0 ? def : Array.IndexOf(DurationPresets, "60");
        Controls.Add(_cboMinutes);

        _btnUnlock = CreateButton("Mở khóa", 24, 134, 104);
        _btnUnlock.Click += (_, _) => TryVerify(PasswordAction.Unlock);
        _btnExit = CreateButton("Thoát hẳn", 138, 134, 104);
        _btnExit.Click += (_, _) => TryVerify(PasswordAction.Exit);
        _btnChange = CreateButton("Đổi mật khẩu", 252, 134, 104);
        _btnChange.Click += (_, _) => ChangePasswordFlow();
        _btnDelete = CreateButton("Xóa toàn bộ", 24, 168, 332);
        _btnDelete.ForeColor = Color.Firebrick;
        _btnDelete.Click += (_, _) => TryVerify(PasswordAction.DeleteAll);

        _lblStatus = new Label
        {
            Text = string.Empty,
            AutoSize = true,
            ForeColor = Color.Firebrick,
            Location = new Point(24, 206)
        };

        Controls.Add(_btnUnlock);
        Controls.Add(_btnExit);
        Controls.Add(_btnChange);
        Controls.Add(_btnDelete);
        Controls.Add(_lblStatus);

        Shown += (_, _) => _txtPassword.Focus();
    }

    private Button _btnUnlock = null!;
    private Button _btnExit = null!;
    private Button _btnChange = null!;
    private Button _btnDelete = null!;

    private Button CreateButton(string text, int x, int y, int w)
        => new() { Text = text, Location = new Point(x, y), Width = w };

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
        _lblStatus.ForeColor = color ?? Color.Firebrick;
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
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Width = 360;
        Height = 200;

        Controls.Add(new Label { Text = "Mật khẩu mới:", AutoSize = true, Location = new Point(24, 20) });
        _txtNew.Location = new Point(24, 44);
        _txtNew.Width = 300;
        _txtNew.UseSystemPasswordChar = true;
        Controls.Add(_txtNew);

        Controls.Add(new Label { Text = "Xác nhận:", AutoSize = true, Location = new Point(24, 76) });
        _txtConfirm.Location = new Point(24, 100);
        _txtConfirm.Width = 300;
        _txtConfirm.UseSystemPasswordChar = true;
        Controls.Add(_txtConfirm);

        var ok = new Button { Text = "Lưu", Location = new Point(24, 140), Width = 120, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Hủy", Location = new Point(204, 140), Width = 120, DialogResult = DialogResult.Cancel };
        Controls.Add(ok);
        Controls.Add(cancel);

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
