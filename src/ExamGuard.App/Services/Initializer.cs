using ExamGuard.Core.Configuration;

namespace ExamGuard.App.Services;

/// <summary>
/// One-shot setup used with the --init switch to create or reset the teacher
/// password before deploying on lab machines.
/// </summary>
public static class Initializer
{
    public static void InitializePassword()
    {
        var store = new ConfigStore();
        var config = store.Load();
        var setup = new SetupPasswordForm();
        if (setup.ShowDialog() != DialogResult.OK)
            return;
        config.SetPassword(setup.NewPassword);
        store.Save(config);
        MessageBox.Show("Đã lưu mật khẩu mới.", "ExamGuard",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

internal sealed class SetupPasswordForm : Form
{
    private readonly TextBox _txtNew = new();
    private readonly TextBox _txtConfirm = new();

    public string NewPassword { get; private set; } = string.Empty;

    public SetupPasswordForm()
    {
        Text = "ExamGuard - Cài đặt mật khẩu";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Width = 380;
        Height = 210;

        Controls.Add(new Label { Text = "Mật khẩu giáo viên mới:", AutoSize = true, Location = new Point(24, 20) });
        _txtNew.Location = new Point(24, 44);
        _txtNew.Width = 320;
        _txtNew.UseSystemPasswordChar = true;
        Controls.Add(_txtNew);

        Controls.Add(new Label { Text = "Xác nhận mật khẩu:", AutoSize = true, Location = new Point(24, 76) });
        _txtConfirm.Location = new Point(24, 100);
        _txtConfirm.Width = 320;
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
