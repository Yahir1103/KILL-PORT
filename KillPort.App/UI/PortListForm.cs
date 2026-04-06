using KillPort.App.Interfaces;
using KillPort.App.Localization;
using KillPort.App.Models;
using KillPort.App.Settings;

namespace KillPort.App.UI;

public sealed class PortListForm : Form
{
    private static readonly Color PinnedBackground = Color.FromArgb(235, 245, 255);

    private readonly IPortScanner _scanner;
    private readonly IPortCloser _closer;
    private readonly SettingsService _settings;
    private readonly LocalizationService _localization;
    private readonly DataGridView _grid;
    private readonly Button _refreshButton;
    private readonly Button _closePortButton;
    private readonly System.Windows.Forms.Timer _autoRefreshTimer;
    private bool _isRefreshing;

    public PortListForm(
        IPortScanner scanner,
        IPortCloser closer,
        SettingsService settings,
        LocalizationService localization)
    {
        _scanner = scanner;
        _closer = closer;
        _settings = settings;
        _localization = localization;

        Size = new Size(680, 440);
        MinimumSize = new Size(520, 320);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.SizableToolWindow;

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.None,
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Pinned", FillWeight = 5 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Port", FillWeight = 11 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Address", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Process", FillWeight = 35 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Pid", FillWeight = 11 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", FillWeight = 20 });

        _refreshButton = new Button { Width = 110, Height = 28 };
        _closePortButton = new Button { Width = 150, Height = 28, Enabled = false };

        _grid.SelectionChanged += (_, _) =>
            _closePortButton.Enabled = _grid.SelectedRows.Count > 0 && SelectedEntry()?.IsClosable == true;

        _refreshButton.Click += async (_, _) => await RefreshAsync();
        _closePortButton.Click += async (_, _) => await CloseSelectedAsync();

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(4),
            WrapContents = false,
        };
        buttonPanel.Controls.Add(_closePortButton);
        buttonPanel.Controls.Add(_refreshButton);

        Controls.Add(_grid);
        Controls.Add(buttonPanel);

        _autoRefreshTimer = new System.Windows.Forms.Timer { Interval = 5000 };
        _autoRefreshTimer.Tick += async (_, _) => await RefreshAsync();

        _localization.LanguageChanged += OnLanguageChanged;

        ApplyLocalization();

        Load += async (_, _) =>
        {
            await RefreshAsync();
            _autoRefreshTimer.Start();
        };
        FormClosing += (_, _) => _autoRefreshTimer.Stop();
    }

    public void ApplyLocalization()
    {
        Text = _localization.Get("PortListTitle");
        _grid.Columns["Pinned"]!.HeaderText = _localization.Get("ColumnPinned");
        _grid.Columns["Port"]!.HeaderText = _localization.Get("ColumnPort");
        _grid.Columns["Address"]!.HeaderText = _localization.Get("ColumnAddress");
        _grid.Columns["Process"]!.HeaderText = _localization.Get("ColumnProcess");
        _grid.Columns["Pid"]!.HeaderText = _localization.Get("ColumnPid");
        _grid.Columns["Status"]!.HeaderText = _localization.Get("ColumnStatus");
        _refreshButton.Text = _localization.Get("ButtonRefresh");
        _closePortButton.Text = _localization.Get("ButtonCloseSelected");
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _localization.LanguageChanged -= OnLanguageChanged;
        base.OnFormClosed(e);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(new Action(HandleLanguageChanged));
            return;
        }

        HandleLanguageChanged();
    }

    private void HandleLanguageChanged()
    {
        ApplyLocalization();
        _ = RefreshAsync();
    }

    private PortEntry? SelectedEntry()
    {
        if (_grid.SelectedRows.Count == 0)
            return null;

        return _grid.SelectedRows[0].Tag as PortEntry;
    }

    private int GetFirstDisplayedScrollingRowIndex()
    {
        try
        {
            return _grid.FirstDisplayedScrollingRowIndex;
        }
        catch
        {
            return -1;
        }
    }

    private PortEntry? FirstVisibleEntry()
    {
        int rowIndex = GetFirstDisplayedScrollingRowIndex();
        if (rowIndex < 0 || rowIndex >= _grid.Rows.Count)
            return null;

        return _grid.Rows[rowIndex].Tag as PortEntry;
    }

    private void RestoreScrollPosition(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _grid.Rows.Count)
            return;

        try
        {
            _grid.FirstDisplayedScrollingRowIndex = rowIndex;
        }
        catch
        {
            // Ignore transient DataGridView scroll restore errors.
        }
    }

    private static (int Port, string Address, int Pid)? EntryKey(PortEntry? entry) =>
        entry is null ? null : (entry.Port, entry.Address, entry.Pid);

    private static bool MatchesEntry(PortEntry entry, (int Port, string Address, int Pid) key) =>
        entry.Port == key.Port &&
        entry.Pid == key.Pid &&
        string.Equals(entry.Address, key.Address, StringComparison.OrdinalIgnoreCase);

    private static bool IsWindowsSvchost(PortEntry entry) =>
        string.Equals(entry.ProcessName, "svchost", StringComparison.OrdinalIgnoreCase);

    private string GetDisplayProcessName(PortEntry entry) =>
        IsWindowsSvchost(entry)
            ? $"{entry.ProcessName} ({_localization.Get("ProcessWindowsTag")})"
            : entry.ProcessName;

    private async Task RefreshAsync()
    {
        if (_isRefreshing)
            return;

        try
        {
            _isRefreshing = true;
            _refreshButton.Enabled = false;

            var ports = await _scanner.GetListeningPortsAsync();
            PopulateGrid(ports);
        }
        finally
        {
            _isRefreshing = false;
            _refreshButton.Enabled = true;
        }
    }

    private void PopulateGrid(IReadOnlyList<PortEntry> ports)
    {
        int previousScrollIndex = GetFirstDisplayedScrollingRowIndex();
        var previousVisibleKey = EntryKey(FirstVisibleEntry());
        var previousSelectionKey = EntryKey(SelectedEntry());

        var sorted = ports
            .OrderBy(port => _settings.IsPinned(port.ProcessName) ? 0 : 1)
            .ThenBy(port => port.Port)
            .ToList();

        _grid.SuspendLayout();
        _grid.Rows.Clear();

        DataGridViewRow? rowToSelect = null;
        int? rowIndexToScroll = null;

        foreach (var port in sorted)
        {
            bool pinned = _settings.IsPinned(port.ProcessName);
            string star = pinned ? _localization.Get("ColumnPinned") : string.Empty;
            string status = port.IsClosable ? _localization.Get("StatusOk") : port.BlockReason;

            int rowIndex = _grid.Rows.Add(
                star,
                port.Port,
                port.Address,
                GetDisplayProcessName(port),
                port.Pid,
                status);

            var row = _grid.Rows[rowIndex];
            row.Tag = port;

            if (previousSelectionKey is { } selectionKey && MatchesEntry(port, selectionKey))
                rowToSelect = row;

            if (previousVisibleKey is { } visibleKey && MatchesEntry(port, visibleKey))
                rowIndexToScroll = rowIndex;

            if (pinned)
            {
                row.DefaultCellStyle.BackColor = PinnedBackground;
                row.DefaultCellStyle.Font = new Font(_grid.Font, FontStyle.Bold);
            }
            else if (!port.IsClosable)
            {
                row.DefaultCellStyle.ForeColor = SystemColors.GrayText;
            }
        }

        _grid.ClearSelection();

        if (rowToSelect is not null)
            rowToSelect.Selected = true;

        _grid.ResumeLayout();

        if (rowIndexToScroll is int visibleRowIndex)
        {
            RestoreScrollPosition(visibleRowIndex);
        }
        else if (previousScrollIndex >= 0 && _grid.Rows.Count > 0)
        {
            RestoreScrollPosition(Math.Min(previousScrollIndex, _grid.Rows.Count - 1));
        }

        _closePortButton.Enabled = _grid.SelectedRows.Count > 0 && SelectedEntry()?.IsClosable == true;
    }

    private async Task CloseSelectedAsync()
    {
        var entry = SelectedEntry();
        if (entry is null)
            return;

        if (!entry.IsClosable)
        {
            MessageBox.Show(
                _localization.Format("DialogCloseBlockedMessage", entry.BlockReason),
                _localization.Get("DialogCloseBlockedTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            _localization.Format("DialogConfirmCloseMessage", entry.Port, entry.ProcessName, entry.Pid),
            _localization.Get("DialogConfirmCloseTitle"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        var result = await _closer.CloseAsync(entry);

        MessageBox.Show(
            result.Message,
            result.Success ? _localization.Get("TitleSuccess") : _localization.Get("TitleError"),
            MessageBoxButtons.OK,
            result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);

        if (result.RequiresElevation)
        {
            MessageBox.Show(
                _localization.Get("DialogPrivilegesMessage"),
                _localization.Get("DialogPrivilegesTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        await RefreshAsync();
    }
}
