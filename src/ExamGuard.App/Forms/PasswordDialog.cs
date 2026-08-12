using ExamGuard.App.Services;
using ExamGuard.Core.Configuration;

namespace ExamGuard.App.Forms;

public enum PasswordAction
{
    None,
    Unlock,
    Exit
}

/// <summary>
/// Password-protected control dialog. All privileged actions (unlock, change
/// password, exit) require the teacher password.
/// </summary>
public sealed class PasswordDialog : Form
{
    private readonly AppConfig _config;
    private readonly ConfigStore _store;
    private readonly LockoutGuard _lockout;
    private TextBox _txtPassword = null!;
    private Label _lblStatus = null!;

    public PasswordAction Result { get; private set; } = PasswordAction.None;

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
        Width = 380;
        Height = 210;
        TopMost = true;

        var lbl = new Label
        {
            Text = "Nhập mật khẩu giáo viên:",
            AutoSize = true,
            Location = new Point(24, 20)
        };

        _txtPassword = new TextBox
        {
            Location = new Point(24, 48),
            Width = 320,
            UseSystemPasswordChar = true
        };
        _txtPassword.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) TryVerify(PasswordAction.Unlock);
        };

        _btnUnlock = CreateButton("Mở khóa", 24, 92, 100);
        _btnUnlock.Click += (_, _) => TryVerify(PasswordAction.Unlock);
        _btnExit = CreateButton("Thoát", 138, 92, 100);
        _btnExit.Click += (_, _) => TryVerify(PasswordAction.Exit);
        _btnChange = CreateButton("Đổi mật khẩu", 252, 92, 100);
        _btnChange.Click += (_, _) => ChangePasswordFlow();

        _lblStatus = new Label
        {
            Text = string.Empty,
            AutoSize = true,
            ForeColor = Color.Firebrick,
            Location = new Point(24, 138)
        };

        Controls.Add(lbl);
        Controls.Add(_txtPassword);
        Controls.Add(_btnUnlock);
        Controls.Add(_btnExit);
        Controls.Add(_btnChange);
        Controls.Add(_lblStatus);

        Shown += (_, _) => _txtPassword.Focus();
    }

    private Button _btnUnlock = null!;
    private Button _btnExit = null!;
    private Button _btnChange = null!;

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

        _lockout.Reset();
        _txtPassword.Clear();
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
