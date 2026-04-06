using KillPort.App.Localization;
using KillPort.App.Settings;

namespace KillPort.App.UI;

public sealed class FavoritesForm : Form
{
    private readonly SettingsService _settings;
    private readonly LocalizationService _localization;
    private readonly ListBox _list;
    private readonly TextBox _input;
    private readonly Button _addButton;
    private readonly Button _removeButton;
    private readonly Button _saveButton;
    private readonly Label _hintLabel;

    public FavoritesForm(SettingsService settings, LocalizationService localization)
    {
        _settings = settings;
        _localization = localization;

        Size = new Size(340, 320);
        MinimumSize = new Size(300, 280);
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;

        _list = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
        };

        _input = new TextBox
        {
            Dock = DockStyle.Fill,
        };
        _input.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                AddCurrent();
            }
        };

        _addButton = new Button { Width = 72, Height = 24 };
        _removeButton = new Button { Width = 72, Height = 24 };
        _saveButton = new Button { Height = 28, Dock = DockStyle.Fill };

        _addButton.Click += (_, _) => AddCurrent();
        _removeButton.Click += (_, _) => RemoveSelected();
        _saveButton.Click += (_, _) => SaveAndClose();

        _list.SelectedIndexChanged += (_, _) =>
            _removeButton.Enabled = _list.SelectedIndex >= 0;

        var inputRow = new TableLayoutPanel
        {
            Height = 30,
            Dock = DockStyle.Bottom,
            ColumnCount = 2,
            RowCount = 1,
        };
        inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        inputRow.Controls.Add(_input, 0, 0);
        inputRow.Controls.Add(_addButton, 1, 0);

        var bottomPanel = new TableLayoutPanel
        {
            Height = 34,
            Dock = DockStyle.Bottom,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(4, 2, 4, 2),
        };
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottomPanel.Controls.Add(_removeButton, 0, 0);
        bottomPanel.Controls.Add(_saveButton, 1, 0);

        _hintLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            ForeColor = SystemColors.GrayText,
            Padding = new Padding(4, 4, 4, 0),
        };

        Controls.Add(_list);
        Controls.Add(inputRow);
        Controls.Add(bottomPanel);
        Controls.Add(_hintLabel);

        _localization.LanguageChanged += OnLanguageChanged;

        ApplyLocalization();

        Load += (_, _) => PopulateList();
    }

    public void ApplyLocalization()
    {
        Text = _localization.Get("FavoritesTitle");
        _input.PlaceholderText = _localization.Get("FavoritesPlaceholder");
        _addButton.Text = _localization.Get("FavoritesAdd");
        _removeButton.Text = _localization.Get("FavoritesRemove");
        _saveButton.Text = _localization.Get("FavoritesSaveClose");
        _hintLabel.Text = _localization.Get("FavoritesHint");
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
            BeginInvoke(ApplyLocalization);
            return;
        }

        ApplyLocalization();
    }

    private void PopulateList()
    {
        _list.Items.Clear();

        foreach (var name in _settings.Current.PinnedProcesses)
            _list.Items.Add(name);

        _removeButton.Enabled = false;
    }

    private void AddCurrent()
    {
        var name = _input.Text.Trim();
        if (string.IsNullOrEmpty(name))
            return;

        bool exists = _list.Items.Cast<string>()
            .Any(item => item.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (!exists)
            _list.Items.Add(name);

        _input.Clear();
        _input.Focus();
    }

    private void RemoveSelected()
    {
        if (_list.SelectedIndex >= 0)
            _list.Items.RemoveAt(_list.SelectedIndex);
    }

    private void SaveAndClose()
    {
        _settings.Current.PinnedProcesses = _list.Items.Cast<string>().ToList();
        _settings.Save();
        Close();
    }
}
