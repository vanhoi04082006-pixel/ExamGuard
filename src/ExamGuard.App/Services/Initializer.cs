using System.Security.Principal;
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

        // When a password already exists, resetting it via --init lets anyone
        // with access to the exe take over the machine. Require an elevated
        // (administrator) token so only the teacher can reset it.
        if (config.HasPassword && !IsElevated())
        {
            MessageBox.Show(
                "Đã có mật khẩu. Để đặt lại mật khẩu cần chạy ExamGuard với quyền quản trị viên\n" +
                "(nhấp phải file -> Chạy với tư cách quản trị viên).",
                "ExamGuard - Từ chối", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var setup = new SetupPasswordForm();
        if (setup.ShowDialog() != DialogResult.OK)
            return;
        config.SetPassword(setup.NewPassword);
        if (store.Save(config))
        {
            MessageBox.Show("Đã lưu mật khẩu mới.", "ExamGuard",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show("Không thể lưu mật khẩu! Thư mục cài đặt không cho ghi.\n" +
                            "Hãy chạy với quyền quản trị viên hoặc đổi nơi cài đặt.",
                "ExamGuard", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool IsElevated()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity)
                .IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}

internal sealed class SetupPasswordForm : Form
{
    private static readonly Color Accent = Color.FromArgb(37, 99, 235);
    private readonly TextBox _txtNew = new();
    private readonly TextBox _txtConfirm = new();

    public string NewPassword { get; private set; } = string.Empty;

    public SetupPasswordForm()
    {
        Text = "ExamGuard - Cài đặt mật khẩu";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        StartPosition = FormStartPosition.CenterScreen;
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
            Text = "Mật khẩu giáo viên mới",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6),
        }, 0, 0);
        _txtNew.Width = 320;
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
        _txtConfirm.Width = 320;
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
            Width = 100,
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
            Width = 100,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = Accent,
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
