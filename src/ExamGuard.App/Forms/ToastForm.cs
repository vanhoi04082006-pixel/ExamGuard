namespace ExamGuard.App.Forms;

/// <summary>
/// Small, auto-closing notification shown when the service is started by hand
/// (double-click) so the teacher gets feedback that it is running in the
/// background. Never shown for watchdog/autostart launches.
/// </summary>
internal sealed class ToastForm : Form
{
    private readonly System.Windows.Forms.Timer _closeTimer;
    private readonly System.Windows.Forms.Timer _fadeTimer;
    private int _fadeStep;

    public ToastForm(string message)
    {
        Text = "ExamGuard";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        ShowIcon = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = Color.FromArgb(15, 23, 42);
        Font = new Font("Segoe UI", 9.5F);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Opacity = 0;

        var root = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(20, 14, 20, 14),
            ColumnCount = 1,
            RowCount = 2,
            BackColor = BackColor,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(new Label
        {
            Text = "ExamGuard",
            AutoSize = true,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(96, 165, 250),
            Margin = new Padding(0, 0, 0, 6),
        }, 0, 0);

        root.Controls.Add(new Label
        {
            Text = message,
            AutoSize = true,
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 0, 0),
        }, 0, 1);

        Controls.Add(root);

        _fadeTimer = new System.Windows.Forms.Timer { Interval = 25 };
        _fadeTimer.Tick += (_, _) =>
        {
            _fadeStep++;
            Opacity = Math.Max(0, 1 - (_fadeStep * 0.06));
            if (Opacity <= 0)
            {
                _fadeTimer.Stop();
                Close();
            }
        };

        _closeTimer = new System.Windows.Forms.Timer { Interval = 4000 };
        _closeTimer.Tick += (_, _) =>
        {
            _closeTimer.Stop();
            _fadeTimer.Start();
        };

        Shown += (_, _) => _closeTimer.Start();
    }

    /// <summary>Positions the toast in the bottom-right corner, above the taskbar.</summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        var screen = Screen.PrimaryScreen;
        var area = screen?.WorkingArea ?? Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(area.Right - Width - 16, area.Bottom - Height - 16);
    }
}
