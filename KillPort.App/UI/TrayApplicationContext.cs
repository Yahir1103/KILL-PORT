using KillPort.App.Interfaces;
using KillPort.App.Localization;
using KillPort.App.Models;
using KillPort.App.Services;
using KillPort.App.Settings;

namespace KillPort.App.UI;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly IPortScanner _scanner;
    private readonly IPortCloser _closer;
    private readonly SettingsService _settings;
    private readonly LocalizationService _localization;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly List<ToolStripItem> _dynamicItems = [];
    private readonly ToolStripMenuItem _refreshItem;
    private readonly ToolStripMenuItem _portsHeader;
    private readonly ToolStripMenuItem _viewAllItem;
    private readonly ToolStripMenuItem _favoritesItem;
    private readonly ToolStripMenuItem _languageItem;
    private readonly ToolStripMenuItem _englishLanguageItem;
    private readonly ToolStripMenuItem _spanishLanguageItem;
    private readonly ToolStripMenuItem _exitItem;
    private PortListForm? _listForm;

    public TrayApplicationContext(
        IPortScanner scanner,
        IPortCloser closer,
        SettingsService settings,
        LocalizationService localization)
    {
        _scanner = scanner;
        _closer = closer;
        _settings = settings;
        _localization = localization;

        _menu = new ContextMenuStrip();

        _refreshItem = new ToolStripMenuItem();
        _refreshItem.Click += async (_, _) => await RefreshMenuAsync();

        _portsHeader = new ToolStripMenuItem { Enabled = false };

        _viewAllItem = new ToolStripMenuItem();
        _viewAllItem.Click += (_, _) => ShowListForm();

        _favoritesItem = new ToolStripMenuItem();
        _favoritesItem.Click += (_, _) => ShowFavoritesForm();

        _languageItem = new ToolStripMenuItem();
        _englishLanguageItem = new ToolStripMenuItem();
        _spanishLanguageItem = new ToolStripMenuItem();
        _englishLanguageItem.Click += (_, _) => _localization.SetLanguage("en");
        _spanishLanguageItem.Click += (_, _) => _localization.SetLanguage("es");
        _languageItem.DropDownItems.Add(_englishLanguageItem);
        _languageItem.DropDownItems.Add(_spanishLanguageItem);

        _exitItem = new ToolStripMenuItem();
        _exitItem.Click += (_, _) => ExitApp();

        _menu.Items.Add(_refreshItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_portsHeader);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_viewAllItem);
        _menu.Items.Add(_favoritesItem);
        _menu.Items.Add(_languageItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_exitItem);

        _menu.Opening += async (_, _) => await RefreshMenuAsync();

        _notifyIcon = new NotifyIcon
        {
            Icon = CreateTrayIcon(),
            ContextMenuStrip = _menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => ShowListForm();

        _localization.LanguageChanged += OnLanguageChanged;

        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        _refreshItem.Text = _localization.Get("MenuRefresh");
        _portsHeader.Text = _localization.Get("MenuOpenPorts");
        _viewAllItem.Text = _localization.Get("MenuViewAll");
        _favoritesItem.Text = _localization.Get("MenuFavorites");
        _languageItem.Text = _localization.Get("MenuLanguage");
        _englishLanguageItem.Text = _localization.Get("MenuEnglish");
        _spanishLanguageItem.Text = _localization.Get("MenuSpanish");
        _exitItem.Text = _localization.Get("MenuExit");
        _notifyIcon.Text = _localization.Get("AppName");
        _englishLanguageItem.Checked = _localization.IsLanguage("en");
        _spanishLanguageItem.Checked = _localization.IsLanguage("es");
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        ApplyLocalization();
        _ = RefreshMenuAsync();
    }

    private async Task RefreshMenuAsync()
    {
        var ports = await _scanner.GetListeningPortsAsync();
        UpdateDynamicItems(ports);
    }

    private void UpdateDynamicItems(IReadOnlyList<PortEntry> ports)
    {
        foreach (var item in _dynamicItems)
            _menu.Items.Remove(item);

        _dynamicItems.Clear();

        int insertIndex = _menu.Items.IndexOf(_portsHeader) + 1;

        var closable = ports
            .Where(port => port.IsClosable)
            .OrderBy(port => _settings.IsPinned(port.ProcessName) ? 0 : 1)
            .ThenBy(port => port.Port)
            .Take(10)
            .ToList();

        if (closable.Count == 0)
        {
            var empty = new ToolStripMenuItem(_localization.Get("TrayNone")) { Enabled = false };
            _menu.Items.Insert(insertIndex, empty);
            _dynamicItems.Add(empty);
            return;
        }

        bool lastWasPinned = true;

        foreach (var entry in closable)
        {
            bool pinned = _settings.IsPinned(entry.ProcessName);

            if (lastWasPinned && !pinned && _dynamicItems.Count > 0)
            {
                var separator = new ToolStripSeparator();
                _menu.Items.Insert(insertIndex++, separator);
                _dynamicItems.Add(separator);
            }
            lastWasPinned = pinned;

            string star = pinned ? $"{_localization.Get("ColumnPinned")} " : string.Empty;
            string label = _localization.Format(
                "TrayPortItemLabel",
                star,
                entry.Port,
                entry.ProcessName,
                entry.Pid);

            var item = new ToolStripMenuItem(label);

            if (pinned)
            {
                Font baseFont = item.Font ?? SystemFonts.MenuFont ?? Control.DefaultFont;
                item.Font = new Font(baseFont, FontStyle.Bold);
            }

            var captured = entry;
            item.Click += async (_, _) => await ConfirmAndCloseAsync(captured);
            _menu.Items.Insert(insertIndex++, item);
            _dynamicItems.Add(item);
        }
    }

    private async Task ConfirmAndCloseAsync(PortEntry entry)
    {
        var confirm = MessageBox.Show(
            _localization.Format("DialogConfirmCloseMessage", entry.Port, entry.ProcessName, entry.Pid),
            _localization.Get("DialogConfirmCloseTitle"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        var result = await _closer.CloseAsync(entry);

        _notifyIcon.ShowBalloonTip(
            3000,
            result.Success ? _localization.Get("AppName") : _localization.Get("NotifyErrorTitle"),
            result.Message,
            result.Success ? ToolTipIcon.Info : ToolTipIcon.Error);

        if (result.RequiresElevation)
        {
            MessageBox.Show(
                _localization.Get("DialogPrivilegesMessage"),
                _localization.Get("DialogPrivilegesTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void ShowListForm()
    {
        if (_listForm is null || _listForm.IsDisposed)
        {
            _listForm = new PortListForm(_scanner, _closer, _settings, _localization);
            _listForm.Show();
        }
        else
        {
            _listForm.BringToFront();
            _listForm.Activate();
        }
    }

    private void ShowFavoritesForm()
    {
        using var form = new FavoritesForm(_settings, _localization);
        form.ShowDialog();
    }

    private void ExitApp()
    {
        _notifyIcon.Visible = false;
        _listForm?.Close();
        Application.Exit();
    }

    private static Icon CreateTrayIcon()
    {
        using var bitmap = new Bitmap(16, 16);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Transparent);
            graphics.FillEllipse(Brushes.DodgerBlue, 1, 1, 14, 14);
            using var font = new Font("Arial", 7f, FontStyle.Bold);
            graphics.DrawString("K", font, Brushes.White, 2.5f, 2.5f);
        }

        IntPtr iconHandle = bitmap.GetHicon();
        try
        {
            using var temporaryIcon = Icon.FromHandle(iconHandle);
            return (Icon)temporaryIcon.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(iconHandle);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _localization.LanguageChanged -= OnLanguageChanged;
            _notifyIcon.Dispose();
            _menu.Dispose();
        }

        base.Dispose(disposing);
    }
}
