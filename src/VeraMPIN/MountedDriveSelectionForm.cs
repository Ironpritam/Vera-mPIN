namespace VeraMPIN;

internal sealed class MountedDriveSelectionForm : Form
{
    private readonly ToolTip _toolTip = new() { ShowAlways = true };
    private readonly ListBox _drives = new()
    {
        Dock = DockStyle.Fill,
        IntegralHeight = false
    };

    public MountedDriveOption? SelectedDrive => _drives.SelectedItem as MountedDriveOption;

    public MountedDriveSelectionForm(IReadOnlyCollection<MountedDriveOption> drives)
    {
        Text = "Choose a drive to dismount";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(560, 340);
        _drives.Items.AddRange(drives.Cast<object>().ToArray());

        var description = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Choose a drive letter to ask VeraCrypt to dismount. This includes volumes mounted outside Vera mPIN. Other Windows drives may also appear; verify the letter in VeraCrypt before continuing.",
            TextAlign = ContentAlignment.MiddleLeft
        };
        var useButton = new Button
        {
            Text = "Dismount with VeraCrypt",
            DialogResult = DialogResult.OK,
            AutoSize = true,
            Enabled = false
        };
        var cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            AutoSize = true
        };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(useButton);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.Controls.Add(description, 0, 0);
        layout.Controls.Add(_drives, 0, 1);
        layout.Controls.Add(buttons, 0, 2);
        Controls.Add(layout);
        _toolTip.SetToolTip(_drives,
            "Drive letters that are currently ready in Windows. Confirm the selected letter in VeraCrypt; the list can include non-VeraCrypt drives.");
        _toolTip.SetToolTip(useButton, "Ask VeraCrypt to dismount the selected drive letter.");
        _toolTip.SetToolTip(cancelButton, "Close without asking VeraCrypt to dismount anything.");
        _toolTip.SetToolTip(description, "This flow is for a VeraCrypt volume mounted outside Vera mPIN as well as one mounted by the app.");

        _drives.SelectedIndexChanged += (_, _) => useButton.Enabled = SelectedDrive is not null;
        AcceptButton = useButton;
        CancelButton = cancelButton;
    }
}
