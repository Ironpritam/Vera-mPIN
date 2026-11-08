namespace VeraMPIN;

internal sealed class CredentialSetupForm : Form
{
    private readonly TextBox _passwordBox = new() { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
    private readonly TextBox _confirmationBox = new() { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
    private readonly ToolTip _toolTip = new() { ShowAlways = true };
    private readonly CheckBox _riskAcknowledgement = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 9),
        Text = "I understand: VeraCrypt's command-line password option can expose it to local process-inspection tools.",
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly Label _matchStatus = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(120, 82, 34),
        Text = "Enter the password twice to continue."
    };
    private readonly Button _saveButton = new()
    {
        BackColor = Color.FromArgb(0, 105, 92),
        Enabled = false,
        FlatStyle = FlatStyle.Flat,
        ForeColor = Color.White,
        Height = 42,
        Text = "Enable mPIN",
        Width = 170
    };

    public string Password => _passwordBox.Text;

    public CredentialSetupForm(string volumeName)
    {
        Text = "Set up mPIN";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(247, 249, 247);
        Font = new Font("Segoe UI", 9);
        var workingArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        var clientWidth = Math.Max(360, Math.Min(640, workingArea.Width - 80));
        var clientHeight = Math.Max(440, Math.Min(560, workingArea.Height - 80));
        ClientSize = new Size(clientWidth, clientHeight);
        _saveButton.FlatAppearance.BorderSize = 0;

        var layout = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 18, 22, 16),
            RowCount = 10
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var eyebrow = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 105, 92),
            Text = "OPTIONAL CONVENIENCE"
        };
        var title = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 19, FontStyle.Bold),
            MaximumSize = new Size(clientWidth - 56, 0),
            ForeColor = Color.FromArgb(28, 36, 38),
            Text = $"Save mPIN approval for {volumeName}"
        };
        var description = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(clientWidth - 56, 0),
            ForeColor = Color.FromArgb(76, 88, 89),
            Text = "Enter and confirm this volume's VeraCrypt password to save it for this Windows account. Windows Hello must approve each use. VeraCrypt checks the password at the next mount. mPIN is not a separate PIN."
        };
        var passwordLabel = new Label { AutoSize = true, Text = "This volume's VeraCrypt password" };
        var confirmLabel = new Label { AutoSize = true, Text = "Confirm password" };

        layout.Controls.Add(eyebrow, 0, 0);
        layout.Controls.Add(title, 0, 1);
        layout.Controls.Add(description, 0, 2);
        layout.Controls.Add(passwordLabel, 0, 3);
        layout.Controls.Add(_passwordBox, 0, 4);
        layout.Controls.Add(confirmLabel, 0, 5);
        layout.Controls.Add(_confirmationBox, 0, 6);
        layout.Controls.Add(_matchStatus, 0, 7);
        layout.Controls.Add(_riskAcknowledgement, 0, 8);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        var cancelButton = new Button
        {
            BackColor = Color.White,
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            Height = 42,
            Text = "Cancel",
            Width = 100
        };
        cancelButton.FlatAppearance.BorderColor = Color.FromArgb(210, 220, 216);
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(_saveButton);
        layout.Controls.Add(buttons, 0, 9);
        Controls.Add(layout);
        _toolTip.SetToolTip(_riskAcknowledgement,
            "This is a security warning: VeraCrypt's command-line password option may be visible to local process-inspection tools.");
        _toolTip.SetToolTip(_saveButton, "Save this password for Windows Hello approval after confirming both entries match.");
        _toolTip.SetToolTip(cancelButton, "Close without saving the password.");

        _passwordBox.TextChanged += (_, _) => UpdateSaveState();
        _confirmationBox.TextChanged += (_, _) => UpdateSaveState();
        _riskAcknowledgement.CheckedChanged += (_, _) => UpdateSaveState();
        _saveButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };
        AcceptButton = _saveButton;
        CancelButton = cancelButton;
    }

    private void UpdateSaveState()
    {
        var passwordsMatch = _passwordBox.TextLength > 0 &&
            string.Equals(_passwordBox.Text, _confirmationBox.Text, StringComparison.Ordinal);

        _matchStatus.Text = passwordsMatch
            ? "Passwords match."
            : _confirmationBox.TextLength == 0
                ? "Enter the password twice to confirm."
                : "The passwords do not match yet.";
        _matchStatus.ForeColor = passwordsMatch
            ? Color.FromArgb(0, 105, 92)
            : Color.FromArgb(120, 82, 34);
        _saveButton.Enabled = passwordsMatch && _riskAcknowledgement.Checked;
    }
}