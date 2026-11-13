using System.Globalization;

namespace VeraMPIN;

public partial class MainForm : Form
{
    private static readonly Color Ink = Color.FromArgb(18, 35, 42);
    private static readonly Color PanelColor = Color.FromArgb(229, 238, 236);
    private static readonly Color Muted = Color.FromArgb(76, 88, 89);
    private static readonly Color Teal = Color.FromArgb(0, 105, 92);
    private static readonly Color Red = Color.FromArgb(174, 48, 54);
    private static readonly Color SelectedRow = Color.FromArgb(220, 226, 231);
    private static readonly Color RowOutline = Color.FromArgb(0, 120, 215);

    private readonly Label _eyebrowLabel = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(56, 184, 159),
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        Text = "VERACRYPT  /  OPTIONAL WINDOWS HELLO APPROVAL"
    };
    private readonly Label _titleLabel = new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 25, FontStyle.Bold),
        ForeColor = Color.FromArgb(240, 247, 246),
        Text = "Manage your VeraCrypt drives"
    };
    private readonly DataGridView _profilesGrid = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        BackColor = Color.White,
        ForeColor = Ink,
        Font = new Font("Segoe UI", 10),
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        AutoGenerateColumns = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BackgroundColor = Color.White,
        ColumnHeadersHeight = 34,
        EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
        MultiSelect = false,
        ReadOnly = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect
    };
    private readonly Label _profileDetails = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        ForeColor = Muted,
        Font = new Font("Segoe UI", 9),
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly Label _mountStateLabel = new()
    {
        AutoSize = true,
        ForeColor = Muted,
        Margin = new Padding(0, 8, 12, 0)
    };
    private readonly Label _statusLabel = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        ForeColor = Muted,
        Font = new Font("Segoe UI", 9),
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly Label _busyLabel = new()
    {
        AutoSize = true,
        ForeColor = Teal,
        Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold),
        Text = "Working…",
        Visible = false,
        Anchor = AnchorStyles.Right
    };
    private readonly ProgressBar _busyProgress = new()
    {
        Style = ProgressBarStyle.Marquee,
        MarqueeAnimationSpeed = 28,
        Size = new Size(108, 14),
        Visible = false,
        Anchor = AnchorStyles.Right
    };
    private readonly ToolTip _statusToolTip = new();
    private readonly ToolTip _profileToolTip = new();
    private readonly ToolTip _controlToolTip = new() { ShowAlways = true };
    private readonly ComboBox _mountLetterComboBox = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 104,
        Font = new Font("Segoe UI", 9)
    };
    private readonly Button _addPartitionButton;
    private readonly Button _addContainerButton;
    private readonly Button _identifyButton;
    private readonly Button _refreshMountStatusButton;
    private readonly Button _mountButton;
    private readonly Button _lockButton;
    private readonly Button _dismountAllButton;
    private readonly Button _removeButton;
    private readonly Button _credentialButton;
    private readonly Button _removeMpinButton;
    private readonly Button _externalDismountButton;
    private readonly System.Windows.Forms.Timer _mountStatusTimer = new() { Interval = 30_000 };
    private bool _refreshingMountLetters;
    private bool _busy;
    private string? _lastAutomaticRefreshWarning;
    private int _pressedSelectedProfileRow = -1;
    private Guid? _outlinedProfileId;

    public MainForm()
    {
        Text = "Vera mPIN";
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? System.Drawing.SystemIcons.Application;
        StartPosition = FormStartPosition.CenterScreen;
        var workingArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        MinimumSize = new Size(
            Math.Min(760, Math.Max(400, workingArea.Width - 32)),
            Math.Min(620, Math.Max(450, workingArea.Height - 32)));
        ClientSize = new Size(
            Math.Min(940, Math.Max(400, workingArea.Width - 48)),
            Math.Min(920, Math.Max(600, workingArea.Height - 48)));
        if (workingArea.Height < 900)
        {
            WindowState = FormWindowState.Maximized;
        }
        BackColor = Ink;
        Font = new Font("Segoe UI", 9);
        AutoScaleMode = AutoScaleMode.Dpi;

        _addPartitionButton = CreateButton("Add a partition", Color.White, Teal, 170);
        _addContainerButton = CreateButton("Add a VeraCrypt file", Color.White, Teal, 184);
        _identifyButton = CreateButton("Identify this SSD", Color.White, Teal, 158);
        _refreshMountStatusButton = CreateButton("Refresh status", Color.White, Teal, 142);
        _mountButton = CreateButton("Mount selected", Teal, Color.White, 164);
        _lockButton = CreateButton("Dismount selected", Red, Color.White, 178);
        _dismountAllButton = CreateButton("Dismount all", Red, Color.White, 150);
        _removeButton = CreateButton("Remove", Color.White, Red, 108);
        _credentialButton = CreateButton("Set up mPIN for this volume", Color.Transparent, Teal, 228);
        _removeMpinButton = CreateButton("Remove mPIN...", Color.White, Teal, 148);
        _externalDismountButton = CreateButton("Dismount a VeraCrypt volume...", Color.White, Teal, 240);
        _credentialButton.FlatAppearance.BorderSize = 0;
        ConfigureControlToolTips();
        ConfigureProfilesGrid();

        var root = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 18, 24, 16),
            RowCount = 4,
            BackColor = Ink
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 164));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var heading = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        _eyebrowLabel.Location = new Point(0, 0);
        _titleLabel.Location = new Point(0, 22);
        heading.Controls.Add(_eyebrowLabel);
        heading.Controls.Add(_titleLabel);

        var mountsGroup = CreateGroup("SAVED DRIVES & FILES");
        var mountsLayout = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 4,
            BackColor = PanelColor
        };
        mountsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        mountsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mountsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        mountsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));

        var sourceActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 2, 0, 0)
        };
        sourceActions.Controls.Add(_addPartitionButton);
        sourceActions.Controls.Add(_addContainerButton);
        sourceActions.Controls.Add(_identifyButton);
        sourceActions.Controls.Add(_refreshMountStatusButton);
        sourceActions.Controls.Add(_externalDismountButton);

        var listHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(8)
        };
        listHost.Controls.Add(_profilesGrid);

        var detailsPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(242, 247, 246),
            Padding = new Padding(12, 4, 12, 4)
        };
        detailsPanel.Controls.Add(_profileDetails);

        var controls = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            BackColor = PanelColor,
            Padding = new Padding(0, 4, 0, 0),
            RowCount = 2
        };
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        controls.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var mountControls = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = Padding.Empty,
            Padding = new Padding(0, 0, 0, 2)
        };
        mountControls.Controls.Add(_mountStateLabel);
        mountControls.Controls.Add(new Label
        {
            AutoSize = true,
            ForeColor = Muted,
            Margin = new Padding(0, 8, 7, 0),
            Text = "Preferred drive letter"
        });
        mountControls.Controls.Add(_mountLetterComboBox);

        var operationButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            Margin = Padding.Empty,
            Padding = new Padding(0, 2, 0, 0)
        };
        operationButtons.Controls.Add(_removeButton);
        operationButtons.Controls.Add(_dismountAllButton);
        operationButtons.Controls.Add(_lockButton);
        operationButtons.Controls.Add(_mountButton);
        controls.Controls.Add(mountControls, 0, 0);
        controls.Controls.Add(operationButtons, 0, 1);

        mountsLayout.Controls.Add(sourceActions, 0, 0);
        mountsLayout.Controls.Add(listHost, 0, 1);
        mountsLayout.Controls.Add(detailsPanel, 0, 2);
        mountsLayout.Controls.Add(controls, 0, 3);
        mountsGroup.Controls.Add(mountsLayout);

        var securityGroup = CreateGroup("OPTIONAL mPIN");
        var securityLayout = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = PanelColor,
            Padding = new Padding(4, 2, 4, 0)
        };
        securityLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        securityLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        var securityDescription = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Muted,
            Font = new Font("Segoe UI", 9),
            Text = "Optional: save this volume's VeraCrypt password for Windows Hello approval when mounting.\r\nWithout mPIN, VeraCrypt asks for the password each time. mPIN is not a separate PIN.",
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(4, 4, 4, 0)
        };
        securityLayout.Controls.Add(securityDescription, 0, 0);
        var mPinActions = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        mPinActions.Controls.Add(_credentialButton);
        mPinActions.Controls.Add(_removeMpinButton);
        securityLayout.Controls.Add(mPinActions, 0, 1);
        securityGroup.Controls.Add(securityLayout);

        var footer = new Panel
        {
            BackColor = PanelColor,
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 0, 12, 0)
        };
        var footerLayout = new TableLayoutPanel
        {
            ColumnCount = 3,
            Dock = DockStyle.Fill,
            RowCount = 1,
            BackColor = PanelColor
        };
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        footerLayout.Controls.Add(_statusLabel, 0, 0);
        footerLayout.Controls.Add(_busyLabel, 1, 0);
        footerLayout.Controls.Add(_busyProgress, 2, 0);
        footer.Controls.Add(footerLayout);

        root.Controls.Add(heading, 0, 0);
        root.Controls.Add(mountsGroup, 0, 1);
        root.Controls.Add(securityGroup, 0, 2);
        root.Controls.Add(footer, 0, 3);
        Controls.Add(root);

        _profilesGrid.SelectionChanged += (_, _) =>
        {
            if (_profilesGrid.SelectedRows.Count > 0 &&
                _profilesGrid.SelectedRows[0].Tag is MountProfile selectedProfile)
            {
                _outlinedProfileId = selectedProfile.Id;
            }

            UpdateSelectedProfile();
        };
        _profilesGrid.RowPostPaint += ProfilesGrid_RowPostPaint;
        _profilesGrid.MouseDown += ProfilesGrid_MouseDown;
        _profilesGrid.CellMouseDown += (_, e) =>
        {
            _pressedSelectedProfileRow = e.RowIndex >= 0 && _profilesGrid.Rows[e.RowIndex].Selected
                ? e.RowIndex
                : -1;
        };
        _profilesGrid.CellMouseClick += ProfilesGrid_CellMouseClick;
        _profilesGrid.CellEndEdit += ProfilesGrid_CellEndEdit;
        _addPartitionButton.Click += AddPartitionButton_Click;
        _addContainerButton.Click += AddContainerButton_Click;
        _identifyButton.Click += IdentifyButton_Click;
        _refreshMountStatusButton.Click += RefreshMountStatusButton_Click;
        _mountButton.Click += MountButton_Click;
        _lockButton.Click += LockButton_Click;
        _dismountAllButton.Click += DismountAllButton_Click;
        _removeButton.Click += RemoveButton_Click;
        _credentialButton.Click += CredentialButton_Click;
        _removeMpinButton.Click += RemoveMpinButton_Click;
        _externalDismountButton.Click += ExternalDismountButton_Click;
        _mountLetterComboBox.SelectedIndexChanged += MountLetterComboBox_SelectedIndexChanged;
        _mountStatusTimer.Tick += MountStatusTimer_Tick;
        FormClosed += (_, _) => _mountStatusTimer.Dispose();
        _mountLetterComboBox.DropDown += (_, _) =>
        {
            try
            {
                RefreshMountLetterChoices();
            }
            catch (Exception exception)
            {
                SetStatus($"Could not scan available mount letters: {exception.Message}", Red);
            }
        };

        foreach (Control control in Controls)
        {
            WireSelectionClearing(control);
        }

        var mountStateStatus = ReconcileMountedState(out var staleMountsCleared);
        RefreshProfileList(null);
        SetStatus(
            mountStateStatus ?? "Add one or more VeraCrypt sources to get started.",
            mountStateStatus is null ? Muted : Color.FromArgb(151, 91, 32));
        try
        {
            RefreshMountLetterChoices();
        }
        catch (Exception exception)
        {
            SetStatus($"Could not scan available mount letters: {exception.Message}", Red);
        }

        if (staleMountsCleared)
        {
            MessageBox.Show(
                this,
                mountStateStatus,
                "Mount status refreshed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        _mountStatusTimer.Start();
    }

    private static Button CreateButton(string text, Color backColor, Color foreColor, int width) => new()
    {
        AutoSize = false,
        BackColor = backColor,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold),
        ForeColor = foreColor,
        Height = 38,
        Margin = new Padding(0, 0, 8, 0),
        Text = text,
        Width = width
    };

    private static GroupBox CreateGroup(string title) => new()
    {
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold),
        ForeColor = Color.FromArgb(38, 54, 53),
        BackColor = PanelColor,
        Padding = new Padding(14),
        Text = title
    };

    private void ConfigureProfilesGrid()
    {
        _profilesGrid.EnableHeadersVisualStyles = false;
        _profilesGrid.GridColor = Color.FromArgb(224, 234, 232);
        _profilesGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _profilesGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        _profilesGrid.ColumnHeadersDefaultCellStyle.BackColor = Teal;
        _profilesGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _profilesGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold);
        _profilesGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Teal;
        _profilesGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
        _profilesGrid.DefaultCellStyle.BackColor = Color.White;
        _profilesGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 250, 249);
        _profilesGrid.DefaultCellStyle.SelectionBackColor = SelectedRow;
        _profilesGrid.DefaultCellStyle.SelectionForeColor = Ink;
        _profilesGrid.DefaultCellStyle.Padding = new Padding(6, 2, 6, 2);
        _profilesGrid.RowTemplate.Height = 36;
        _profilesGrid.Columns.Clear();
        _profilesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "name",
            HeaderText = "Name (double-click to edit)",
            FillWeight = 52,
            ReadOnly = false,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _profilesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "source",
            HeaderText = "Source",
            FillWeight = 24,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _profilesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "status",
            HeaderText = "Status",
            FillWeight = 24,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _profilesGrid.Columns["name"]!.HeaderCell.ToolTipText = "The friendly name for this saved mount. Double-click to edit.";
        _profilesGrid.Columns["source"]!.HeaderCell.ToolTipText = "The saved item is either a VeraCrypt container file or a disk partition.";
        _profilesGrid.Columns["status"]!.HeaderCell.ToolTipText = "Shows whether this app has recorded the entry as mounted.";
    }

    private void ProfilesGrid_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.RowIndex < 0 || _profilesGrid.IsCurrentCellInEditMode ||
            _pressedSelectedProfileRow != e.RowIndex)
        {
            _pressedSelectedProfileRow = -1;
            return;
        }

        _profilesGrid.ClearSelection();
        _profilesGrid.CurrentCell = null;
        _pressedSelectedProfileRow = -1;
        UpdateSelectedProfile();
        _profilesGrid.Invalidate();
    }

    private void ProfilesGrid_MouseDown(object? sender, MouseEventArgs e)
    {
        var hit = _profilesGrid.HitTest(e.X, e.Y);
        if (e.Button == MouseButtons.Left && hit.RowIndex < 0)
        {
            ClearProfileSelection();
        }
    }

    private void ProfilesGrid_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
    {
        var row = _profilesGrid.Rows[e.RowIndex];
        if (row.Selected || row.Tag is not MountProfile profile || profile.Id != _outlinedProfileId)
        {
            return;
        }

        var bounds = new Rectangle(e.RowBounds.X + 1, e.RowBounds.Y + 1,
            Math.Max(0, e.RowBounds.Width - 3), Math.Max(0, e.RowBounds.Height - 3));
        using var pen = new Pen(RowOutline, 2);
        e.Graphics.DrawRectangle(pen, bounds);
    }

    private void WireSelectionClearing(Control control)
    {
        if (control == _profilesGrid)
        {
            return;
        }

        if (control != _mountButton && control != _lockButton && control != _removeButton &&
            control != _dismountAllButton && control != _credentialButton &&
            control != _removeMpinButton && control != _mountLetterComboBox)
        {
            control.MouseDown += (_, _) => ClearProfileSelection();
        }

        foreach (Control child in control.Controls)
        {
            WireSelectionClearing(child);
        }
    }

    private void ClearProfileSelection()
    {
        if (_profilesGrid.SelectedRows.Count == 0 && _profilesGrid.CurrentCell is null)
        {
            return;
        }

        if (_profilesGrid.SelectedRows.Count > 0 &&
            _profilesGrid.SelectedRows[0].Tag is MountProfile selectedProfile)
        {
            _outlinedProfileId = selectedProfile.Id;
        }

        _profilesGrid.ClearSelection();
        _profilesGrid.CurrentCell = null;
        UpdateSelectedProfile();
        _profilesGrid.Invalidate();
    }

    private void ConfigureControlToolTips()
    {
        _controlToolTip.SetToolTip(_addPartitionButton, "Browse partitions on all connected disks and save one as a mount entry.");
        _controlToolTip.SetToolTip(_addContainerButton, "Choose and save a VeraCrypt container file.");
        _controlToolTip.SetToolTip(_identifyButton, "Find the largest other partition on the USB drive containing this app.");
        _controlToolTip.SetToolTip(_refreshMountStatusButton,
            "Check saved mounted entries against the current drive letters and volume identities. Vera mPIN also checks automatically every 30 seconds.");
        _controlToolTip.SetToolTip(_externalDismountButton,
            "Choose a drive letter and ask VeraCrypt to dismount it, including volumes mounted outside this app. Verify the letter in VeraCrypt first.");
        _controlToolTip.SetToolTip(_mountButton, "Mount the selected saved entry, using its preferred drive letter when available.");
        _controlToolTip.SetToolTip(_lockButton,
            "Dismount only the selected entry that Vera mPIN mounted and recorded. Use the other dismount button for volumes mounted directly in VeraCrypt.");
        _controlToolTip.SetToolTip(_dismountAllButton,
            "Dismount every volume mounted and tracked by Vera mPIN. This does not include volumes mounted directly in VeraCrypt. Each volume is checked and dismounted normally, without force.");
        _controlToolTip.SetToolTip(_removeButton, "Remove this saved entry from the app. This does not delete its data or container.");
        _controlToolTip.SetToolTip(_mountLetterComboBox, "Choose the preferred letter for this entry. If it is busy during mounting, another free letter is used.");
        _controlToolTip.SetToolTip(_credentialButton, "Save or update this entry's VeraCrypt password for Windows Hello approval.");
        _controlToolTip.SetToolTip(_removeMpinButton,
            "Choose whether to remove the saved mPIN password for the selected entry or for all protected entries.");
        _controlToolTip.SetToolTip(_mountStateLabel, "Shows whether the selected saved entry is currently mounted by this app.");
        _controlToolTip.SetToolTip(_profileDetails, "Details about the selected source. Names shown in the list can be edited directly.");
        _controlToolTip.SetToolTip(_eyebrowLabel, "Windows Hello approval is optional. VeraCrypt remains responsible for encryption and mounting.");
        _controlToolTip.SetToolTip(_titleLabel, "Choose a saved partition or container, then mount or dismount it.");
        _controlToolTip.SetToolTip(_statusLabel, "Operation results and errors are reported here.");
        _controlToolTip.SetToolTip(_busyLabel, "The app is processing your request. Please wait.");
        _controlToolTip.SetToolTip(_busyProgress, "The app is processing your request. Please wait.");
    }

    private void ProfilesGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _profilesGrid.Rows[e.RowIndex].Tag is not MountProfile profile ||
            e.ColumnIndex != 0)
        {
            return;
        }

        var updatedName = Convert.ToString(_profilesGrid.Rows[e.RowIndex].Cells["name"].Value)?.Trim();
        if (string.IsNullOrWhiteSpace(updatedName))
        {
            _profilesGrid.Rows[e.RowIndex].Cells["name"].Value = profile.Name;
            SetStatus("A mount name cannot be empty.", Color.FromArgb(151, 91, 32));
            return;
        }

        profile.Name = updatedName;
        try
        {
            SaveProfile(profile);
            RefreshProfileList(profile.Id);
            SetStatus($"Mount renamed to “{profile.Name}”.", Teal);
        }
        catch (Exception exception)
        {
            _profilesGrid.Rows[e.RowIndex].Cells["name"].Value = profile.Name;
            SetStatus($"Could not save the mount name: {exception.Message}", Red);
        }
    }

    private SsdConfiguration LoadSettings() => SsdConfiguration.Load() ?? new SsdConfiguration();

    private void SaveSettings(SsdConfiguration settings) => settings.Save();

    private MountProfile? SelectedProfile =>
        _profilesGrid.SelectedRows.Count == 1
            ? _profilesGrid.SelectedRows[0].Tag as MountProfile
            : null;

    private async void AddPartitionButton_Click(object? sender, EventArgs e)
    {
        SetBusy(true);
        SetStatus("Scanning connected drives and partitions…", Muted);
        try
        {
            var targets = await DiskInventory.GetPartitionTargetsAsync();
            if (targets.Count == 0)
            {
                throw new InvalidOperationException("Windows did not report any disk partitions.");
            }

            using var dialog = new PartitionSelectionForm(targets);
            if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SelectedTarget is null)
            {
                SetStatus("No partition was added.", Muted);
                return;
            }

            var profile = dialog.SelectedTarget.Profile;
            profile.Name = dialog.DisplayName;
            var warning = profile.IsSystemDisk
                ? "\n\nWARNING: Windows identifies this as the system or boot disk. Only continue if you have positively verified that this is the intended encrypted partition."
                : "\n\nThe target can be an internal, external, fixed, or removable disk. Confirm the disk and partition carefully.";
            var confirmation = $"Add this partition as a VeraCrypt mount?\n\n" +
                               $"{dialog.SelectedTarget.Title}\n{dialog.SelectedTarget.Details}\n\n" +
                               $"Disk: {profile.DiskName} ({profile.BusType})\n" +
                               $"Disk size: {FormatSize(profile.DiskSize)}\n" +
                               $"Partition: {profile.PartitionNumber}\n" +
                               $"Partition size: {FormatSize(profile.PartitionSize)}{warning}";
            if (MessageBox.Show(confirmation, "Confirm partition source", MessageBoxButtons.YesNo,
                    profile.IsSystemDisk ? MessageBoxIcon.Warning : MessageBoxIcon.Question) != DialogResult.Yes)
            {
                SetStatus("No partition was added.", Muted);
                return;
            }

            AddProfile(profile);
        }
        catch (Exception exception)
        {
            SetStatus(exception.Message, Red);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void AddContainerButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            Filter = "VeraCrypt containers|*.hc;*.tc;*.vc|All files|*.*",
            Title = "Add VeraCrypt container"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var path = Path.GetFullPath(dialog.FileName);
        AddProfile(new MountProfile
        {
            Name = Path.GetFileName(path),
            SourceType = MountSourceType.ContainerFile,
            ContainerPath = path
        });
    }

    private async void IdentifyButton_Click(object? sender, EventArgs e)
    {
        SetBusy(true);
        SetStatus("Looking for the app's USB drive…", Muted);
        try
        {
            var profile = await DiskInventory.FindTargetBesideLauncherAsync();
            var confirmation = $"Add the largest other partition on the USB disk containing this app?\n\n" +
                               $"Disk: {profile.DiskName}\nPartition: {profile.PartitionNumber}\n" +
                               $"Size: {FormatSize(profile.PartitionSize)}";
            if (MessageBox.Show(confirmation, "Identify this SSD", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                AddProfile(profile);
            }
            else
            {
                SetStatus("No partition was added.", Muted);
            }
        }
        catch (Exception exception)
        {
            SetStatus(exception.Message, Red);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void AddProfile(MountProfile profile)
    {
        var settings = LoadSettings();
        var duplicate = settings.Profiles.FirstOrDefault(existing => IsSameSource(existing, profile));
        if (duplicate is not null)
        {
            RefreshProfileList(duplicate.Id);
            SetStatus($"That source is already in the mount list: {duplicate.Name}", Color.FromArgb(151, 91, 32));
            return;
        }

        settings.Profiles.Add(profile);
        SaveSettings(settings);
        RefreshProfileList(profile.Id);
        SetStatus($"Added “{profile.Name}” to the mount list.", Teal);
    }

    private static bool IsSameSource(MountProfile first, MountProfile second) =>
        first.SourceType == second.SourceType &&
        (first.SourceType == MountSourceType.ContainerFile
            ? string.Equals(first.ContainerPath, second.ContainerPath, StringComparison.OrdinalIgnoreCase)
            : string.Equals(first.DiskIdentity, second.DiskIdentity, StringComparison.OrdinalIgnoreCase) &&
              first.PartitionNumber == second.PartitionNumber &&
              first.PartitionSize == second.PartitionSize &&
              (first.PartitionOffset == 0 || second.PartitionOffset == 0 ||
               first.PartitionOffset == second.PartitionOffset));

    private void RefreshProfileList(Guid? selectedId)
    {
        var profiles = LoadSettings().Profiles;
        _profilesGrid.SuspendLayout();
        try
        {
            _profilesGrid.Rows.Clear();
            foreach (var profile in profiles)
            {
                var source = profile.SourceType == MountSourceType.ContainerFile
                    ? "VeraCrypt file"
                    : string.Equals(profile.BusType, "USB", StringComparison.OrdinalIgnoreCase)
                        ? "External drive"
                        : "Drive partition";
                var status = profile.MountedLetter is char mountedLetter
                    ? $"Mounted at {mountedLetter}:"
                    : profile.HasMpin ? "Ready · mPIN enabled" : "Ready";
                var rowIndex = _profilesGrid.Rows.Add(profile.Name, source, status);
                _profilesGrid.Rows[rowIndex].Tag = profile;
                _profilesGrid.Rows[rowIndex].Cells["name"].ToolTipText =
                    "Double-click to give this mount a name that is easy to recognize.";
            }

            var selectedIndex = profiles.FindIndex(profile => profile.Id == selectedId);
            if (selectedIndex < 0 && _profilesGrid.Rows.Count > 0)
            {
                selectedIndex = 0;
            }

            if (selectedIndex >= 0)
            {
                _outlinedProfileId = profiles[selectedIndex].Id;
                _profilesGrid.CurrentCell = _profilesGrid.Rows[selectedIndex].Cells["name"];
                _profilesGrid.Rows[selectedIndex].Selected = true;
            }
            else
            {
                _outlinedProfileId = null;
            }
        }
        finally
        {
            _profilesGrid.ResumeLayout();
        }

        UpdateSelectedProfile();
    }

    private string? ReconcileMountedState(out bool staleStateCleared)
    {
        staleStateCleared = false;
        var settings = LoadSettings();
        var staleMounts = new List<(MountProfile Profile, MountedVolume? CurrentVolume)>();
        var warnings = new List<string>();

        foreach (var profile in settings.Profiles.Where(item => item.MountedLetter is not null))
        {
            try
            {
                var currentVolume = VeraCryptService.GetMountedVolume(profile.MountedLetter!.Value);
                if (currentVolume is null || !IsConfiguredVolume(profile, currentVolume))
                {
                    staleMounts.Add((profile, currentVolume));
                    profile.ClearMountedState();
                }
            }
            catch (Exception exception) when (
                exception is IOException or InvalidOperationException or UnauthorizedAccessException)
            {
                warnings.Add(
                    $"Could not verify “{profile.Name}” at {profile.MountedLetter}: {exception.Message}");
            }
        }

        if (staleMounts.Count > 0)
        {
            try
            {
                SaveSettings(settings);
                staleStateCleared = true;
                warnings.AddRange(staleMounts.Select(item => item.CurrentVolume is null
                    ? $"Cleared the stale mounted status for “{item.Profile.Name}”; its drive is no longer mounted."
                    : $"Cleared the stale mounted status for “{item.Profile.Name}”; its recorded drive letter now belongs to a different volume."));
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
                warnings.Add($"Could not save refreshed mount status: {exception.Message}");
            }
        }

        return warnings.Count == 0 ? null : string.Join(Environment.NewLine, warnings);
    }

    private void RefreshMountStatusButton_Click(object? sender, EventArgs e)
    {
        if (_busy)
        {
            return;
        }

        try
        {
            var selectedId = SelectedProfile?.Id;
            var warning = ReconcileMountedState(out var staleStateCleared);
            if (staleStateCleared)
            {
                RefreshProfileList(selectedId);
            }

            var status = warning ?? "Mount status checked. No stale mount entries were found.";
            SetStatus(status, warning is null ? Teal : Color.FromArgb(151, 91, 32));
            if (warning is not null)
            {
                MessageBox.Show(
                    this,
                    warning,
                    "Mount status refresh",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        catch (Exception exception)
        {
            var message = $"Could not refresh mount status: {exception.Message}";
            SetStatus(message, Red);
            MessageBox.Show(this, message, "Mount status refresh failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void MountStatusTimer_Tick(object? sender, EventArgs e)
    {
        if (_busy || IsDisposed || !Visible)
        {
            return;
        }

        try
        {
            var selectedId = SelectedProfile?.Id;
            var warning = ReconcileMountedState(out var staleStateCleared);
            if (staleStateCleared)
            {
                RefreshProfileList(selectedId);
            }

            if (warning is null)
            {
                _lastAutomaticRefreshWarning = null;
                return;
            }

            SetStatus(warning, Color.FromArgb(151, 91, 32));
            if (string.Equals(warning, _lastAutomaticRefreshWarning, StringComparison.Ordinal))
            {
                return;
            }

            _lastAutomaticRefreshWarning = warning;
            MessageBox.Show(
                this,
                warning,
                "Mount status changed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (Exception exception)
        {
            var warning = $"Automatic mount-status check failed: {exception.Message}";
            SetStatus(warning, Red);
            if (!string.Equals(warning, _lastAutomaticRefreshWarning, StringComparison.Ordinal))
            {
                _lastAutomaticRefreshWarning = warning;
                MessageBox.Show(
                    this,
                    warning,
                    "Mount status check failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }

    private void UpdateSelectedProfile()
    {
        var profile = SelectedProfile;
        var enabled = profile is not null && !_busy;
        _mountButton.Enabled = enabled && profile?.MountedLetter is null;
        _lockButton.Enabled = enabled && profile?.MountedLetter is not null;
        _dismountAllButton.Enabled = !_busy && LoadSettings().Profiles.Any(item => item.MountedLetter is not null);
        _removeButton.Enabled = enabled;
        _credentialButton.Enabled = enabled;
        _credentialButton.Text = profile?.HasMpin == true
            ? "Update mPIN password"
            : "Set up mPIN for this volume";
        _removeMpinButton.Enabled = !_busy && SecretStore.AnyExists(LoadSettings().Profiles);
        _mountLetterComboBox.Enabled = enabled;

        if (profile is null)
        {
            _profileDetails.Text = "No source selected. Add a partition or a container file above.";
            _mountStateLabel.Text = "Select a mount";
            _mountLetterComboBox.Items.Clear();
            _mountLetterComboBox.Enabled = false;
            return;
        }

        var targetPartition = profile.Partitions.FirstOrDefault(partition => partition.Number == profile.PartitionNumber);
        _profileDetails.Text = profile.SourceType == MountSourceType.ContainerFile
            ? $"Saved file: {Path.GetFileName(profile.ContainerPath)}{Environment.NewLine}{profile.ContainerPath}"
            : $"{(profile.IsSystemDisk ? "Windows system drive" : string.Equals(profile.BusType, "USB", StringComparison.OrdinalIgnoreCase) ? "External drive" : "Internal drive")}: {profile.DiskName}  ·  " +
              $"Part {profile.PartitionNumber}  ·  {FormatSize(profile.PartitionSize)}{(profile.IsSystemDisk ? "  ·  Please double-check this is the encrypted partition." : "")}" +
              $"{Environment.NewLine}{(targetPartition is null ? "Volume name unavailable" : $"{(string.IsNullOrWhiteSpace(targetPartition.DriveLetter) ? "No drive letter assigned" : $"Opens as {targetPartition.DriveLetter}:")}  ·  {(string.IsNullOrWhiteSpace(targetPartition.VolumeName) ? "No volume name" : targetPartition.VolumeName)}")}  ·  The app checks this drive before opening it.";
        _profileToolTip.SetToolTip(_profileDetails, _profileDetails.Text);
        _mountStateLabel.Text = profile.MountedLetter is char mountedLetter
            ? $"Mounted: {mountedLetter}:"
            : "Not mounted";
        RefreshMountLetterChoices();
    }

    private async void MountButton_Click(object? sender, EventArgs e)
    {
        var profile = SelectedProfile;
        if (profile is null)
        {
            return;
        }

        SetBusy(true);
        SetStatus($"Preparing to mount “{profile.Name}”…", Muted);
        try
        {
            if (profile.MountedLetter is char currentLetter)
            {
                var existing = VeraCryptService.GetMountedVolume(currentLetter);
                if (existing is not null && IsConfiguredVolume(profile, existing))
                {
                    SetStatus($"“{profile.Name}” is already mounted at {currentLetter}:", Teal);
                    return;
                }

                profile.ClearMountedState();
                SaveProfile(profile);
                RefreshProfileList(profile.Id);
                if (existing is not null)
                {
                    SetStatus(
                        $"Cleared stale mount tracking for “{profile.Name}”. Drive {currentLetter}: belongs to a different volume; another available letter will be used.",
                        Color.FromArgb(151, 91, 32));
                }
            }

            var sourcePath = profile.SourceType switch
            {
                MountSourceType.Partition => (await DiskInventory.ResolveConfiguredTargetAsync(profile)).DevicePath,
                MountSourceType.ContainerFile when !string.IsNullOrWhiteSpace(profile.ContainerPath) && File.Exists(profile.ContainerPath) =>
                    Path.GetFullPath(profile.ContainerPath),
                MountSourceType.ContainerFile =>
                    throw new FileNotFoundException("The saved container is missing. Remove this entry and add the file from its current location.", profile.ContainerPath),
                _ => throw new InvalidOperationException("The saved mount source type is not supported.")
            };

            var mountLetter = VeraCryptService.FindAvailableMountLetter(profile.PreferredMountLetter);
            string? password = null;
            if (profile.HasMpin)
            {
                await HelloAuthentication.VerifyAsync();
                password = SecretStore.Read(profile.Id);
            }

            SetStatus($"Mounting “{profile.Name}” as {mountLetter}: …", Muted);
            using var process = VeraCryptService.StartMount(sourcePath, mountLetter, password);
            password = null;
            var volume = await VeraCryptService.WaitForMountedVolumeAsync(mountLetter, process, TimeSpan.FromSeconds(90));

            profile.MountedLetter = mountLetter;
            profile.MountedVolumeSerial = volume.SerialNumber;
            profile.MountedVolumeSize = volume.Size;
            profile.MountedVolumeLabel = volume.Label;
            SaveProfile(profile);
            RefreshProfileList(profile.Id);
            SetStatus($"Mounted “{profile.Name}” at {mountLetter}:.", Teal);
        }
        catch (OperationCanceledException)
        {
            SetStatus("Verification cancelled; the volume was not mounted.", Color.FromArgb(151, 91, 32));
        }
        catch (Exception exception)
        {
            SetStatus(exception.Message, Red);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void LockButton_Click(object? sender, EventArgs e)
    {
        var profile = SelectedProfile;
        if (profile?.MountedLetter is not char mountLetter)
        {
            SetStatus("This entry has no app-managed mounted volume to dismount.", Color.FromArgb(151, 91, 32));
            return;
        }

        SetBusy(true);
        SetStatus($"Checking “{profile.Name}” before dismount…", Muted);
        try
        {
            var volume = VeraCryptService.GetMountedVolume(mountLetter);
            if (volume is null)
            {
                profile.ClearMountedState();
                SaveProfile(profile);
                RefreshProfileList(profile.Id);
                SetStatus($"The volume was already unmounted. Cleared its stale status for “{profile.Name}”.", Color.FromArgb(151, 91, 32));
                return;
            }

            if (!IsConfiguredVolume(profile, volume))
            {
                profile.ClearMountedState();
                SaveProfile(profile);
                RefreshProfileList(profile.Id);
                SetStatus(
                    $"Drive {mountLetter}: is a different volume. Cleared stale tracking for “{profile.Name}”; nothing was dismounted.",
                    Color.FromArgb(151, 91, 32));
                return;
            }

            if (profile.SourceType == MountSourceType.Partition)
            {
                await DiskInventory.ResolveConfiguredTargetAsync(profile);
            }

            var answer = MessageBox.Show(
                $"Dismount “{profile.Name}” at {mountLetter}:? Files using it may prevent a normal dismount.",
                "Dismount VeraCrypt volume",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes)
            {
                return;
            }

            using var process = VeraCryptService.StartDismount(mountLetter);
            await VeraCryptService.WaitForDismountAsync(mountLetter, volume, process, TimeSpan.FromSeconds(30));
            profile.ClearMountedState();
            SaveProfile(profile);
            RefreshProfileList(profile.Id);
            SetStatus($"Dismounted “{profile.Name}”.", Teal);
        }
        catch (Exception exception)
        {
            SetStatus(exception.Message, Red);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void DismountAllButton_Click(object? sender, EventArgs e)
    {
        if (_busy)
        {
            return;
        }

        var mountedProfiles = LoadSettings().Profiles
            .Where(profile => profile.MountedLetter is not null)
            .ToList();
        if (mountedProfiles.Count == 0)
        {
            SetStatus("There are no Vera mPIN-managed volumes to dismount.", Color.FromArgb(151, 91, 32));
            return;
        }

        var confirmation = string.Join(Environment.NewLine,
            mountedProfiles.Select(profile => $"- {profile.Name} ({profile.MountedLetter}:)"));
        var answer = MessageBox.Show(
            $"Dismount all {mountedProfiles.Count} volumes mounted by Vera mPIN?\n\n{confirmation}\n\n" +
            "Each volume will be dismounted normally. Files using a volume may prevent its dismount; no forced dismount will be attempted.",
            "Dismount all Vera mPIN volumes",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (answer != DialogResult.Yes)
        {
            return;
        }

        SetBusy(true);
        var dismounted = new List<string>();
        var staleMountsCleared = new List<string>();
        var failures = new List<string>();
        var stoppedAfterCancellation = false;
        try
        {
            for (var index = 0; index < mountedProfiles.Count; index++)
            {
                var profile = mountedProfiles[index];
                var mountLetter = profile.MountedLetter!.Value;
                try
                {
                    var volume = VeraCryptService.GetMountedVolume(mountLetter);
                    if (volume is null)
                    {
                        profile.ClearMountedState();
                        SaveProfile(profile);
                        staleMountsCleared.Add($"{profile.Name} (already unmounted)");
                        continue;
                    }

                    if (!IsConfiguredVolume(profile, volume))
                    {
                        profile.ClearMountedState();
                        SaveProfile(profile);
                        staleMountsCleared.Add($"{profile.Name} (drive letter belongs to another volume; left it untouched)");
                        continue;
                    }

                    if (profile.SourceType == MountSourceType.Partition)
                    {
                        await DiskInventory.ResolveConfiguredTargetAsync(profile);
                    }

                    SetStatus($"Dismounting “{profile.Name}” at {mountLetter}: …", Muted);
                    using var process = VeraCryptService.StartDismount(mountLetter);
                    await VeraCryptService.WaitForDismountAsync(
                        mountLetter, volume, process, TimeSpan.FromSeconds(30));

                    profile.ClearMountedState();
                    SaveProfile(profile);
                    dismounted.Add(profile.Name);
                }
                catch (OperationCanceledException exception)
                {
                    failures.Add($"- {profile.Name} ({mountLetter}:): {exception.Message}");
                    failures.Add($"- {mountedProfiles.Count - index - 1} remaining volume(s) were not attempted.");
                    stoppedAfterCancellation = true;
                    break;
                }
                catch (Exception exception)
                {
                    failures.Add($"- {profile.Name} ({mountLetter}:): {exception.Message}");
                }
            }

            RefreshProfileList(null);
            if (failures.Count == 0 && staleMountsCleared.Count == 0)
            {
                SetStatus($"Dismounted all {dismounted.Count} Vera mPIN-managed volumes.", Teal);
            }
            else if (failures.Count == 0)
            {
                SetStatus(
                    $"Dismounted {dismounted.Count} volume(s) and cleared stale status for {staleMountsCleared.Count} entry/entries.",
                    Color.FromArgb(151, 91, 32));
                MessageBox.Show(
                    this,
                    $"Dismounted: {dismounted.Count}\nStale mount status cleared: {staleMountsCleared.Count}\n\n{string.Join(Environment.NewLine, staleMountsCleared)}",
                    "Mount status refreshed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                var resultSummary = stoppedAfterCancellation
                    ? $"Dismounted {dismounted.Count} volume(s); the operation stopped after cancellation."
                    : $"Dismounted {dismounted.Count} volume(s); {failures.Count} could not be dismounted.";
                if (staleMountsCleared.Count > 0)
                {
                    resultSummary += $" Cleared stale status for {staleMountsCleared.Count} entry/entries.";
                }
                SetStatus(resultSummary, Red);
                MessageBox.Show(
                    this,
                    $"Dismounted: {dismounted.Count}\nStale mount status cleared: {staleMountsCleared.Count}\nCould not dismount: {failures.Count}\n\n" +
                    (staleMountsCleared.Count == 0 ? "" : $"{string.Join(Environment.NewLine, staleMountsCleared)}{Environment.NewLine}{Environment.NewLine}") +
                    string.Join(Environment.NewLine, failures),
                    "Some volumes were not dismounted",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        catch (Exception exception)
        {
            SetStatus($"Could not complete the dismount-all operation: {exception.Message}", Red);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void RemoveButton_Click(object? sender, EventArgs e)
    {
        var profile = SelectedProfile;
        if (profile is null)
        {
            return;
        }

        try
        {
            if (profile.MountedLetter is char letter && VeraCryptService.GetMountedVolume(letter) is not null)
            {
                throw new InvalidOperationException($"Dismount the volume at {letter}: before removing this entry.");
            }

            var settings = LoadSettings();
            settings.Profiles.RemoveAll(item => item.Id == profile.Id);
            SaveSettings(settings);
            SecretStore.Delete(profile.Id);
            RefreshProfileList(settings.Profiles.FirstOrDefault()?.Id);
            SetStatus($"Removed “{profile.Name}” from the list. This does not delete its container or data.", Teal);
        }
        catch (Exception exception)
        {
            SetStatus(exception.Message, Red);
        }
    }

    private void SaveProfile(MountProfile profile)
    {
        var settings = LoadSettings();
        var index = settings.Profiles.FindIndex(item => item.Id == profile.Id);
        if (index < 0)
        {
            throw new InvalidOperationException("The mount entry was removed while an operation was in progress.");
        }

        settings.Profiles[index] = profile;
        SaveSettings(settings);
    }

    private void MountLetterComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_refreshingMountLetters || _mountLetterComboBox.SelectedItem is not string selection)
        {
            return;
        }

        var profile = SelectedProfile;
        if (profile is null)
        {
            return;
        }

        profile.PreferredMountLetter = selection[0];
        SaveProfile(profile);
        UpdateSelectedProfile();
    }

    private void RefreshMountLetterChoices()
    {
        var profile = SelectedProfile;
        var availableLetters = VeraCryptService.GetAvailableMountLetters();
        _refreshingMountLetters = true;
        try
        {
            _mountLetterComboBox.Items.Clear();
            foreach (var letter in availableLetters)
            {
                _mountLetterComboBox.Items.Add($"{letter}:");
            }

            if (profile is not null)
            {
                var preferredItem = $"{profile.PreferredMountLetter}:";
                if (!availableLetters.Contains(profile.PreferredMountLetter))
                {
                    preferredItem += profile.MountedLetter == profile.PreferredMountLetter
                        ? " (currently mounted)"
                        : " (in use; fallback will apply)";
                    _mountLetterComboBox.Items.Insert(0, preferredItem);
                }

                _mountLetterComboBox.SelectedItem = _mountLetterComboBox.Items
                    .Cast<string>()
                    .FirstOrDefault(item => item.StartsWith($"{profile.PreferredMountLetter}:", StringComparison.Ordinal));
            }
        }
        finally
        {
            _refreshingMountLetters = false;
        }
    }

    private void SetSourceButtonsEnabled(bool enabled)
    {
        _addPartitionButton.Enabled = enabled;
        _addContainerButton.Enabled = enabled;
        _identifyButton.Enabled = enabled;
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        UseWaitCursor = busy;
        _busyLabel.Visible = busy;
        _busyProgress.Visible = busy;
        _busyProgress.MarqueeAnimationSpeed = busy ? 28 : 0;
        SetSourceButtonsEnabled(!busy);
        _externalDismountButton.Enabled = !busy;
        _refreshMountStatusButton.Enabled = !busy;
        _dismountAllButton.Enabled = !busy && LoadSettings().Profiles.Any(item => item.MountedLetter is not null);
        _profilesGrid.Enabled = !busy;
        _credentialButton.Enabled = !busy;
        _removeMpinButton.Enabled = !busy && SecretStore.AnyExists(LoadSettings().Profiles);
        UpdateSelectedProfile();
    }

    private void CredentialButton_Click(object? sender, EventArgs e)
    {
        var profile = SelectedProfile;
        if (profile is null || _busy)
        {
            return;
        }

        EnableMpin(profile);
    }

    private void EnableMpin(MountProfile profile)
    {
        using var dialog = new CredentialSetupForm(profile.Name);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            SecretStore.Save(profile.Id, dialog.Password);
            RefreshProfileList(profile.Id);
            SetStatus($"mPIN enabled for “{profile.Name}”. Windows Hello approval will be requested each time.", Teal);
        }
        catch (Exception exception)
        {
            SetStatus($"Could not enable mPIN: {exception.Message}", Red);
        }
    }

    private void RemoveMpinButton_Click(object? sender, EventArgs e)
    {
        if (_busy)
        {
            return;
        }

        var settings = LoadSettings();
        var menu = new ContextMenuStrip();
        menu.Closed += (_, _) => menu.Dispose();
        if (SelectedProfile is MountProfile selected && selected.HasMpin)
        {
            menu.Items.Add($"Remove mPIN for “{selected.Name}”", null, (_, _) => RemoveSelectedMpin(selected));
        }

        if (SecretStore.AnyExists(settings.Profiles))
        {
            menu.Items.Add("Remove mPIN for all volumes...", null, (_, _) => RemoveAllMpin(settings.Profiles));
        }

        if (menu.Items.Count == 0)
        {
            menu.Dispose();
            SetStatus("There are no saved mPIN credentials to remove.", Muted);
            return;
        }

        menu.Show(_removeMpinButton, new Point(0, _removeMpinButton.Height));
    }

    private async void ExternalDismountButton_Click(object? sender, EventArgs e)
    {
        if (_busy)
        {
            return;
        }

        try
        {
            SetBusy(true);
            SetStatus("Checking available drives…", Muted);
            var mountedDrives = new List<MountedDriveOption>();
            foreach (var drive in DriveInfo.GetDrives().Where(drive => drive.IsReady))
            {
                var letter = char.ToUpperInvariant(drive.Name[0]);
                var volume = VeraCryptService.GetMountedVolume(letter);
                if (volume is not null)
                {
                    mountedDrives.Add(new MountedDriveOption(letter,
                        string.IsNullOrWhiteSpace(volume.Label) ? "No volume label" : volume.Label,
                        volume.Size,
                        drive.DriveType.ToString()));
                }
            }

            if (mountedDrives.Count == 0)
            {
                SetStatus("No ready drives were found to check.", Color.FromArgb(151, 91, 32));
                return;
            }

            using var dialog = new MountedDriveSelectionForm(mountedDrives);
            if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SelectedDrive is not MountedDriveOption selected)
            {
                SetStatus("No drive was dismounted.", Muted);
                return;
            }

            var answer = MessageBox.Show(
                $"Ask VeraCrypt to dismount {selected.Letter}: ({selected.Label}, {FormatSize(selected.Size)})?\n\n" +
                "Only proceed if you recognize this as a VeraCrypt volume. The app cannot verify whether another application mounted it.",
                "Dismount existing VeraCrypt volume",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes)
            {
                SetStatus("No drive was dismounted.", Muted);
                return;
            }

            var currentVolume = VeraCryptService.GetMountedVolume(selected.Letter)
                ?? throw new InvalidOperationException($"Drive {selected.Letter}: is no longer mounted.");
            using var process = VeraCryptService.StartDismount(selected.Letter);
            await VeraCryptService.WaitForDismountAsync(selected.Letter, currentVolume, process, TimeSpan.FromSeconds(30));
            SetStatus($"Dismounted the VeraCrypt volume at {selected.Letter}:.", Teal);
        }
        catch (Exception exception)
        {
            SetStatus(exception.Message, Red);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void RemoveSelectedMpin(MountProfile profile)
    {
        var answer = MessageBox.Show(
            $"Remove the saved mPIN password for “{profile.Name}” only? Other mount entries will not be changed.",
            "Remove mPIN for selected volume",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (answer != DialogResult.Yes)
        {
            return;
        }

        try
        {
            SecretStore.Delete(profile.Id);
            RefreshProfileList(profile.Id);
            SetStatus($"mPIN removed for “{profile.Name}”. It will use VeraCrypt's password prompt.", Teal);
        }
        catch (Exception exception)
        {
            SetStatus($"Could not remove mPIN: {exception.Message}", Red);
        }
    }

    private void RemoveAllMpin(IReadOnlyCollection<MountProfile> profiles)
    {
        var count = profiles.Count(profile => profile.HasMpin);
        var answer = MessageBox.Show(
            $"Remove mPIN-saved passwords for all {count} protected volume entries? Every entry will use VeraCrypt's password prompt afterward.",
            "Remove mPIN for all volumes",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (answer != DialogResult.Yes)
        {
            return;
        }

        try
        {
            SecretStore.DeleteAll(profiles);
            RefreshProfileList(SelectedProfile?.Id);
            SetStatus("mPIN passwords removed for all volumes.", Teal);
        }
        catch (Exception exception)
        {
            SetStatus($"Could not remove all mPIN passwords: {exception.Message}", Red);
        }
    }

    private static bool IsConfiguredVolume(MountProfile profile, MountedVolume volume) =>
        profile.MountedVolumeSerial is uint serial &&
        volume.SerialNumber == serial &&
        profile.MountedVolumeSize == volume.Size &&
        string.Equals(profile.MountedVolumeLabel, volume.Label, StringComparison.Ordinal);

    private void SetStatus(string message, Color color)
    {
        _statusLabel.Text = message;
        _statusLabel.ForeColor = color;
        _statusToolTip.SetToolTip(_statusLabel, message);
    }

    private static string FormatSize(long bytes) =>
        bytes >= 1_000_000_000_000L
            ? $"{(bytes / 1_000_000_000_000d).ToString("0.##", CultureInfo.InvariantCulture)} TB"
            : bytes >= 1_000_000_000L
                ? $"{(bytes / 1_000_000_000d).ToString("0.##", CultureInfo.InvariantCulture)} GB"
                : $"{(bytes / 1_000_000d).ToString("0.##", CultureInfo.InvariantCulture)} MB";
}
