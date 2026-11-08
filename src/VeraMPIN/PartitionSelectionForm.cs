namespace VeraMPIN;

internal sealed class PartitionSelectionForm : Form
{
    private static readonly Color Teal = Color.FromArgb(0, 105, 92);
    private static readonly Color SelectedRow = Color.FromArgb(220, 226, 231);
    private static readonly Color RowOutline = Color.FromArgb(0, 120, 215);
    private readonly ToolTip _toolTip = new() { ShowAlways = true };
    private int _pressedSelectedRow = -1;
    private Guid? _outlinedTargetId;
    private readonly DataGridView _partitionGrid = new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        AutoGenerateColumns = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None,
        ColumnHeadersHeight = 36,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        MultiSelect = false,
        ReadOnly = true,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect
    };
    private readonly TextBox _nameBox = new() { Dock = DockStyle.Fill };

    public PartitionTargetOption? SelectedTarget =>
        _partitionGrid.SelectedRows.Count == 1
            ? _partitionGrid.SelectedRows[0].Tag as PartitionTargetOption
            : null;
    public string DisplayName => _nameBox.Text.Trim();

    public PartitionSelectionForm(IReadOnlyCollection<PartitionTargetOption> targets)
    {
        Text = "Choose a drive or partition";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowInTaskbar = false;
        MinimumSize = new Size(760, 460);
        ClientSize = new Size(940, 560);
        ConfigureGrid();

        foreach (var target in targets)
        {
            var profile = target.Profile;
            var partition = profile.Partitions.FirstOrDefault(item => item.Number == profile.PartitionNumber);
            var rowIndex = _partitionGrid.Rows.Add(
                profile.IsSystemDisk ? "System drive" : FriendlyDiskType(profile.BusType),
                profile.DiskName,
                $"Partition {profile.PartitionNumber}",
                FormatSize(profile.PartitionSize),
                string.IsNullOrWhiteSpace(partition?.DriveLetter) ? "Not assigned" : $"{partition.DriveLetter}:",
                string.IsNullOrWhiteSpace(partition?.VolumeName) ? "No label" : partition.VolumeName);
            _partitionGrid.Rows[rowIndex].Tag = target;
            if (profile.IsSystemDisk)
            {
                _partitionGrid.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(120, 82, 34);
                _partitionGrid.Rows[rowIndex].Cells["disk"].ToolTipText =
                    "Windows identifies this as the system or boot disk. Verify carefully before selecting.";
            }
        }

        var description = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Choose a drive and partition. Internal system and boot disks are included but clearly marked.",
            TextAlign = ContentAlignment.MiddleLeft
        };
        var selectButton = new Button
        {
            Text = "Use selected partition",
            AutoSize = false,
            Width = 190,
            Height = 34,
            Enabled = false
        };
        var cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            AutoSize = false,
            Width = 92,
            Height = 34
        };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            AutoSize = false,
            Padding = new Padding(0, 3, 0, 0)
        };
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(selectButton);

        var bottomPanel = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 3,
            Padding = new Padding(0, 4, 0, 0)
        };
        bottomPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        bottomPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        bottomPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        bottomPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Name this mount (you can change it later):"
        }, 0, 0);
        bottomPanel.Controls.Add(_nameBox, 0, 1);
        bottomPanel.Controls.Add(buttons, 0, 2);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        layout.Controls.Add(description, 0, 0);
        layout.Controls.Add(_partitionGrid, 0, 1);
        layout.Controls.Add(bottomPanel, 0, 2);
        Controls.Add(layout);
        _toolTip.SetToolTip(_partitionGrid, "Select a partition to see or change its friendly mount name.");
        _toolTip.SetToolTip(_nameBox, "This name is shown in the saved mount list; it does not rename the disk or volume.");
        _toolTip.SetToolTip(selectButton, "Add the selected partition with the name shown below the table.");
        _toolTip.SetToolTip(cancelButton, "Close without adding a mount.");
        _toolTip.SetToolTip(description, "Partitions on internal, external, fixed, removable, and system disks can be listed here.");

        _partitionGrid.SelectionChanged += (_, _) =>
        {
            if (SelectedTarget is PartitionTargetOption selectedTarget)
            {
                _outlinedTargetId = selectedTarget.Profile.Id;
            }

            selectButton.Enabled = SelectedTarget is not null;
            if (SelectedTarget is not null)
            {
                _nameBox.Text = SelectedTarget.Profile.Name;
                _nameBox.SelectAll();
            }
        };
        _partitionGrid.CellMouseDown += (_, e) =>
        {
            _pressedSelectedRow = e.RowIndex >= 0 && _partitionGrid.Rows[e.RowIndex].Selected
                ? e.RowIndex
                : -1;
        };
        _partitionGrid.MouseDown += (_, e) =>
        {
            var hit = _partitionGrid.HitTest(e.X, e.Y);
            if (e.Button == MouseButtons.Left && hit.RowIndex < 0)
            {
                ClearPartitionSelection(selectButton);
            }
        };
        _partitionGrid.CellMouseClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && _pressedSelectedRow == e.RowIndex)
            {
                ClearPartitionSelection(selectButton);
            }

            _pressedSelectedRow = -1;
        };
        selectButton.Click += (_, e) =>
        {
            if (SelectedTarget is null || string.IsNullOrWhiteSpace(DisplayName))
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this, "Enter a name for this mount.", "Mount name required",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        };
        _partitionGrid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && _partitionGrid.Rows[e.RowIndex].Tag is PartitionTargetOption target &&
                !string.IsNullOrWhiteSpace(DisplayName))
            {
                _outlinedTargetId = target.Profile.Id;
                _partitionGrid.CurrentCell = _partitionGrid.Rows[e.RowIndex].Cells[0];
                _partitionGrid.Rows[e.RowIndex].Selected = true;
                DialogResult = DialogResult.OK;
                Close();
            }
        };
        _partitionGrid.RowPostPaint += (_, e) =>
        {
            var row = _partitionGrid.Rows[e.RowIndex];
            if (row.Selected || row.Tag is not PartitionTargetOption target ||
                target.Profile.Id != _outlinedTargetId)
            {
                return;
            }

            var bounds = new Rectangle(e.RowBounds.X + 1, e.RowBounds.Y + 1,
                Math.Max(0, e.RowBounds.Width - 3), Math.Max(0, e.RowBounds.Height - 3));
            using var pen = new Pen(RowOutline, 2);
            e.Graphics.DrawRectangle(pen, bounds);
        };
        AcceptButton = selectButton;
        CancelButton = cancelButton;
    }

    private void ConfigureGrid()
    {
        _partitionGrid.EnableHeadersVisualStyles = false;
        _partitionGrid.GridColor = Color.FromArgb(224, 234, 232);
        _partitionGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _partitionGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        _partitionGrid.ColumnHeadersDefaultCellStyle.BackColor = Teal;
        _partitionGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _partitionGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold);
        _partitionGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Teal;
        _partitionGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
        _partitionGrid.DefaultCellStyle.BackColor = Color.White;
        _partitionGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 250, 249);
        _partitionGrid.DefaultCellStyle.SelectionBackColor = SelectedRow;
        _partitionGrid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(25, 45, 50);
        _partitionGrid.DefaultCellStyle.Padding = new Padding(4, 2, 4, 2);
        _partitionGrid.RowTemplate.Height = 34;
        AddColumn("kind", "Drive type", 17);
        AddColumn("disk", "Drive name", 23);
        AddColumn("partition", "Partition", 14);
        AddColumn("size", "Size", 12);
        AddColumn("letter", "Drive letter", 13);
        AddColumn("label", "Volume name", 21);
    }

    private void ClearPartitionSelection(Button selectButton)
    {
        if (_partitionGrid.SelectedRows.Count > 0 &&
            _partitionGrid.SelectedRows[0].Tag is PartitionTargetOption target)
        {
            _outlinedTargetId = target.Profile.Id;
        }

        _partitionGrid.ClearSelection();
        _partitionGrid.CurrentCell = null;
        selectButton.Enabled = false;
        _partitionGrid.Invalidate();
    }

    private void AddColumn(string name, string header, float fillWeight)
    {
        _partitionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            FillWeight = fillWeight,
            SortMode = DataGridViewColumnSortMode.Automatic
        });
        var column = _partitionGrid.Columns[name]
            ?? throw new InvalidOperationException($"Could not create the '{name}' partition column.");
        column.HeaderCell.ToolTipText = header switch
        {
            "Drive type" => "Whether Windows identifies the drive as internal, external, removable, or a system drive.",
            "Drive name" => "The hardware name reported by Windows.",
            "Partition" => "The partition number on that drive.",
            "Size" => "The partition's capacity.",
            "Drive letter" => "A current Windows drive letter, if one is assigned.",
            "Volume name" => "The volume label reported by Windows, if present.",
            _ => header
        };
    }

    private static string FriendlyDiskType(string busType) =>
        busType.ToUpperInvariant() switch
        {
            "USB" => "External",
            "NVME" or "SATA" or "ATA" or "SAS" => "Internal",
            "SD" or "MMC" => "Memory card",
            "VIRTUAL" => "Virtual drive",
            _ => "Drive"
        };

    private static string FormatSize(long bytes) =>
        bytes >= 1_000_000_000_000L
            ? $"{bytes / 1_000_000_000_000d:0.##} TB"
            : bytes >= 1_000_000_000L
                ? $"{bytes / 1_000_000_000d:0.##} GB"
                : $"{bytes / 1_000_000d:0.##} MB";
}
