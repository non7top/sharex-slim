#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.
*/

#endregion License Information (GPL v3)

#nullable enable

using Avalonia.Threading;
using ShareX.AvaloniaUI.Controls;
using ShareX.AvaloniaUI.Theming;
using ShareX.HelpersLib;
using ShareX.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShareX;

public sealed class ApplicationSettingsViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly DispatcherTimer _saveTimer;
    private SettingsNavigationItem? _selectedNavigationItem;
    private string _personalFolderPath = string.Empty;
    private string _personalFolderPreview = string.Empty;
    private string _screenshotsFolderPreview = string.Empty;
    private bool _startWithWindows;
    private bool _startWithWindowsEnabled;
    private string _startWithWindowsText = string.Empty;
    private bool _shellContextMenu;
    private bool _editWithShareX;
    private bool _sendToMenu;
    private bool _exportSettings = true;
    private bool _personalPathDirty;
    private bool _isBusy;
    private bool _restartRequired;
    private string _statusMessage = string.Empty;
    private bool _disposed;

    private ApplicationConfig Settings => Program.Settings;

    public ObservableCollection<SettingsNavigationItem> NavigationItems { get; private set; } = [];
    public ObservableCollection<AdvancedSettingItem> AdvancedSettings { get; private set; } = [];
    public ObservableCollection<AdvancedSettingCategory> AdvancedSettingCategories { get; private set; } = [];

    public IReadOnlyList<EnumOption<SupportedLanguage>> LanguageOptions { get; } = CreateEnumOptions<SupportedLanguage>();
    public IReadOnlyList<EnumOption<HotkeyType>> HotkeyTypeOptions { get; } = CreateEnumOptions<HotkeyType>();
    public IReadOnlyList<EnumOption<ThumbnailTitleLocation>> ThumbnailTitleLocationOptions { get; } = CreateEnumOptions<ThumbnailTitleLocation>();
    public IReadOnlyList<EnumOption<ThumbnailViewClickAction>> ThumbnailClickActionOptions { get; } = CreateEnumOptions<ThumbnailViewClickAction>();

    public SettingsNavigationItem? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (!SetField(ref _selectedNavigationItem, value))
            {
                return;
            }

            OnPageChanged();

            if (value?.Id == "integration")
            {
                RefreshStartWithWindows();
            }
        }
    }

    public bool IsGeneralPage => IsPage("general");
    public bool IsIntegrationPage => IsPage("integration");
    public bool IsPathsPage => IsPage("paths");
    public bool IsSettingsPage => IsPage("settings");
    public bool IsMainWindowPage => IsPage("main-window");
    public bool IsHistoryPage => IsPage("history");
    public bool IsPrintPage => IsPage("print");
    public bool IsAdvancedPage => IsPage("advanced");

    public bool WindowsIntegrationVisible
    {
        get
        {
#if MicrosoftStore
            return false;
#else
            return true;
#endif
        }
    }

    public EnumOption<SupportedLanguage>? SelectedLanguage
    {
        get => Find(LanguageOptions, Settings.Language);
        set
        {
            if (value == null || Settings.Language == value.Value)
            {
                return;
            }

            Settings.Language = value.Value;
            MarkChanged();

            if (LanguageHelper.ChangeLanguage(value.Value))
            {
                RestartRequired = true;
            }
        }
    }

    public bool ShowTray
    {
        get => Settings.ShowTray;
        set
        {
            if (SetSetting(Settings.ShowTray, value, x => Settings.ShowTray = x))
            {
                MainWindowIntegration.SetTrayVisible(value);
                OnPropertyChanged(nameof(SilentRunEnabled));
            }
        }
    }

    public bool SilentRunEnabled => ShowTray;
    public bool SilentRun { get => Settings.SilentRun; set => SetSetting(Settings.SilentRun, value, x => Settings.SilentRun = x); }
    public bool TaskbarProgressSupported => TaskbarManager.IsPlatformSupported;

    public bool UseWhiteShareXIcon
    {
        get => Settings.UseWhiteShareXIcon;
        set
        {
            if (SetSetting(Settings.UseWhiteShareXIcon, value, x => Settings.UseWhiteShareXIcon = x))
            {
                InvokeOnMainThread(Program.MainForm.UpdateTheme);
            }
        }
    }

    public bool RememberMainFormPosition { get => Settings.RememberMainFormPosition; set => SetSetting(Settings.RememberMainFormPosition, value, x => Settings.RememberMainFormPosition = x); }
    public bool RememberMainFormSize { get => Settings.RememberMainFormSize; set => SetSetting(Settings.RememberMainFormSize, value, x => Settings.RememberMainFormSize = x); }

    public EnumOption<HotkeyType>? SelectedTrayLeftDoubleClickAction
    {
        get => Find(HotkeyTypeOptions, Settings.TrayLeftDoubleClickAction);
        set { if (value != null) SetSetting(Settings.TrayLeftDoubleClickAction, value.Value, x => Settings.TrayLeftDoubleClickAction = x); }
    }

    public EnumOption<HotkeyType>? SelectedTrayLeftClickAction
    {
        get => Find(HotkeyTypeOptions, Settings.TrayLeftClickAction);
        set { if (value != null) SetSetting(Settings.TrayLeftClickAction, value.Value, x => Settings.TrayLeftClickAction = x); }
    }

    public EnumOption<HotkeyType>? SelectedTrayMiddleClickAction
    {
        get => Find(HotkeyTypeOptions, Settings.TrayMiddleClickAction);
        set { if (value != null) SetSetting(Settings.TrayMiddleClickAction, value.Value, x => Settings.TrayMiddleClickAction = x); }
    }


    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (!_startWithWindowsEnabled || _startWithWindows == value)
            {
                return;
            }

            try
            {
                InvokeOnMainThread(() => StartupManager.State = value ? StartupState.Enabled : StartupState.Disabled);
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e);
                StatusMessage = e.Message;
            }

            RefreshStartWithWindows();
        }
    }

    public bool StartWithWindowsEnabled { get => _startWithWindowsEnabled; private set => SetField(ref _startWithWindowsEnabled, value); }
    public string StartWithWindowsText { get => _startWithWindowsText; private set => SetField(ref _startWithWindowsText, value); }

    public bool ShellContextMenu
    {
        get => _shellContextMenu;
        set
        {
            if (SetField(ref _shellContextMenu, value))
            {
                InvokeOnMainThread(() => IntegrationHelpers.CreateShellContextMenuButton(value));
            }
        }
    }

    public bool EditWithShareX
    {
        get => _editWithShareX;
        set
        {
            if (SetField(ref _editWithShareX, value))
            {
                InvokeOnMainThread(() => IntegrationHelpers.CreateEditShellContextMenuButton(value));
            }
        }
    }

    public bool SendToMenu
    {
        get => _sendToMenu;
        set
        {
            if (SetField(ref _sendToMenu, value))
            {
                InvokeOnMainThread(() => IntegrationHelpers.CreateSendToMenuButton(value));
            }
        }
    }

    public string PersonalFolderPath
    {
        get => _personalFolderPath;
        set
        {
            if (!SetField(ref _personalFolderPath, value))
            {
                return;
            }

            UpdatePersonalFolderPreview();
            _personalPathDirty = true;
            MarkChanged();
        }
    }

    public string PersonalFolderPreview { get => _personalFolderPreview; private set => SetField(ref _personalFolderPreview, value); }

    public bool UseCustomScreenshotsPath
    {
        get => Settings.UseCustomScreenshotsPath;
        set
        {
            if (SetSetting(Settings.UseCustomScreenshotsPath, value, x => Settings.UseCustomScreenshotsPath = x))
            {
                UpdateScreenshotsFolderPreview();
            }
        }
    }

    public string CustomScreenshotsPath
    {
        get => Settings.CustomScreenshotsPath;
        set
        {
            string sanitized = FileHelpers.SanitizePath(value);
            if (SetSetting(Settings.CustomScreenshotsPath, sanitized, x => Settings.CustomScreenshotsPath = x))
            {
                UpdateScreenshotsFolderPreview();
            }
        }
    }

    public string SaveImageSubFolderPattern
    {
        get => Settings.SaveImageSubFolderPattern;
        set
        {
            string sanitized = FileHelpers.SanitizePath(value);
            if (SetSetting(Settings.SaveImageSubFolderPattern, sanitized, x => Settings.SaveImageSubFolderPattern = x))
            {
                UpdateScreenshotsFolderPreview();
            }
        }
    }

    public string SaveImageSubFolderPatternWindow
    {
        get => Settings.SaveImageSubFolderPatternWindow;
        set => SetSetting(Settings.SaveImageSubFolderPatternWindow, FileHelpers.SanitizePath(value), x => Settings.SaveImageSubFolderPatternWindow = x);
    }

    public string ScreenshotsFolderPreview { get => _screenshotsFolderPreview; private set => SetField(ref _screenshotsFolderPreview, value); }

    public bool ExportSettings { get => _exportSettings; set { if (SetField(ref _exportSettings, value)) OnPropertyChanged(nameof(CanExport)); } }
    public bool CanExport => !IsBusy && ExportSettings;

    public bool AutoCleanupBackupFiles { get => Settings.AutoCleanupBackupFiles; set => SetSetting(Settings.AutoCleanupBackupFiles, value, x => Settings.AutoCleanupBackupFiles = x); }
    public bool AutoCleanupLogFiles { get => Settings.AutoCleanupLogFiles; set => SetSetting(Settings.AutoCleanupLogFiles, value, x => Settings.AutoCleanupLogFiles = x); }
    public decimal CleanupKeepFileCount { get => Settings.CleanupKeepFileCount; set => SetSetting(Settings.CleanupKeepFileCount, decimal.ToInt32(value), x => Settings.CleanupKeepFileCount = x); }

    public bool ShowThumbnailTitle { get => Settings.ShowThumbnailTitle; set => SetSetting(Settings.ShowThumbnailTitle, value, x => Settings.ShowThumbnailTitle = x); }

    public EnumOption<ThumbnailTitleLocation>? SelectedThumbnailTitleLocation
    {
        get => Find(ThumbnailTitleLocationOptions, Settings.ThumbnailTitleLocation);
        set { if (value != null) SetSetting(Settings.ThumbnailTitleLocation, value.Value, x => Settings.ThumbnailTitleLocation = x); }
    }

    public decimal ThumbnailWidth
    {
        get => Settings.ThumbnailSize.Width;
        set => SetSetting(Settings.ThumbnailSize.Width, decimal.ToInt32(value), x => Settings.ThumbnailSize = new Size(x, Settings.ThumbnailSize.Height));
    }

    public decimal ThumbnailHeight
    {
        get => Settings.ThumbnailSize.Height;
        set => SetSetting(Settings.ThumbnailSize.Height, decimal.ToInt32(value), x => Settings.ThumbnailSize = new Size(Settings.ThumbnailSize.Width, x));
    }

    public EnumOption<ThumbnailViewClickAction>? SelectedThumbnailClickAction
    {
        get => Find(ThumbnailClickActionOptions, Settings.ThumbnailClickAction);
        set { if (value != null) SetSetting(Settings.ThumbnailClickAction, value.Value, x => Settings.ThumbnailClickAction = x); }
    }


    public decimal ConcurrentTaskLimit { get => Settings.ConcurrentTaskLimit; set => SetSetting(Settings.ConcurrentTaskLimit, decimal.ToInt32(value), x => Settings.ConcurrentTaskLimit = x); }



    public bool RecentTasksSave { get => Settings.RecentTasksSave; set => SetSetting(Settings.RecentTasksSave, value, x => Settings.RecentTasksSave = x); }
    public decimal RecentTasksMaxCount { get => Settings.RecentTasksMaxCount; set => SetSetting(Settings.RecentTasksMaxCount, decimal.ToInt32(value), x => Settings.RecentTasksMaxCount = x); }
    public bool RecentTasksShowInTrayMenu { get => Settings.RecentTasksShowInTrayMenu; set => SetSetting(Settings.RecentTasksShowInTrayMenu, value, x => Settings.RecentTasksShowInTrayMenu = x); }
    public bool RecentTasksTrayMenuMostRecentFirst { get => Settings.RecentTasksTrayMenuMostRecentFirst; set => SetSetting(Settings.RecentTasksTrayMenuMostRecentFirst, value, x => Settings.RecentTasksTrayMenuMostRecentFirst = x); }

    public bool DontShowPrintSettingsDialog { get => Settings.DontShowPrintSettingsDialog; set => SetSetting(Settings.DontShowPrintSettingsDialog, value, x => Settings.DontShowPrintSettingsDialog = x); }

    public bool DontShowWindowsPrintDialog
    {
        get => !Settings.PrintSettings.ShowPrintDialog;
        set
        {
            if (SetSetting(Settings.PrintSettings.ShowPrintDialog, !value, x => Settings.PrintSettings.ShowPrintDialog = x))
            {
                OnPropertyChanged(nameof(DefaultPrinterOverrideVisible));
            }
        }
    }

    public bool DefaultPrinterOverrideVisible => !Settings.PrintSettings.ShowPrintDialog;
    public string DefaultPrinterOverride { get => Settings.PrintSettings.DefaultPrinterOverride; set => SetSetting(Settings.PrintSettings.DefaultPrinterOverride, value, x => Settings.PrintSettings.DefaultPrinterOverride = x); }


    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanExport));
            }
        }
    }

    public bool RestartRequired { get => _restartRequired; private set => SetField(ref _restartRequired, value); }
    public string StatusMessage { get => _statusMessage; private set { if (SetField(ref _statusMessage, value)) OnPropertyChanged(nameof(HasStatusMessage)); } }
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public ApplicationSettingsViewModel()
    {
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(650) };
        _saveTimer.Tick += OnSaveTimerTick;

        Reload();
    }

    public void ResetThumbnailSize()
    {
        Settings.ThumbnailSize = new Size(200, 150);
        OnPropertyChanged(nameof(ThumbnailWidth));
        OnPropertyChanged(nameof(ThumbnailHeight));
        MarkChanged();
    }


    public void EditQuickTaskMenu() => QuickTaskMenuEditorIntegration.Show();

    public void OpenPersonalFolder() => FileHelpers.OpenFolder(PersonalFolderPreview);
    public void OpenScreenshotsFolder() => FileHelpers.OpenFolder(ScreenshotsFolderPreview);

    public void ShowImagePrintSettings(Avalonia.Controls.Window owner)
    {
        InvokeOnMainThread(() =>
        {
            using Image image = TaskHelpers.GetScreenshot().CaptureActiveMonitor();
            PrintWindowIntegration.Show(image, Settings.PrintSettings, true, owner);
        });
        MarkChanged();
    }

    public async Task ExportAsync(string path)
    {
        IsBusy = true;
        StatusMessage = "Exporting backup...";

        try
        {
            bool exportSettings = ExportSettings;

            bool result = await Task.Run(() =>
            {
                SettingManager.SaveAllSettings();
                return SettingManager.Export(path, exportSettings);
            });
            StatusMessage = result ? $"Backup exported to {path}" : "Backup export failed.";
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
            StatusMessage = e.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ImportAsync(string path)
    {
        IsBusy = true;
        StatusMessage = "Importing backup...";
        _saveTimer.Stop();

        try
        {
            bool result = await Task.Run(() =>
            {
                if (!SettingManager.Import(path))
                {
                    return false;
                }

                SettingManager.LoadAllSettings();
                return true;
            });

            if (result)
            {
                LanguageHelper.ChangeLanguage(Settings.Language);
                Reload();
                await UpdateMainFormAsync();
                StatusMessage = $"Backup imported from {path}";
            }
            else
            {
                StatusMessage = "Backup import failed.";
            }
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
            StatusMessage = e.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ResetAsync()
    {
        bool confirmed = InvokeOnMainThread(() => MessageBox.Show(
            Resources.ApplicationSettingsForm_btnResetSettings_Click_WouldYouLikeToResetShareXSettings,
            "ShareX - " + Resources.Confirmation,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Exclamation) == DialogResult.Yes);

        if (!confirmed)
        {
            return;
        }

        IsBusy = true;
        _saveTimer.Stop();

        try
        {
            InvokeOnMainThread(() =>
            {
                SettingManager.ResetSettings();
                SettingManager.SaveAllSettings();
            });
            LanguageHelper.ChangeLanguage(Settings.Language);
            Reload();
            await UpdateMainFormAsync();
            StatusMessage = "Settings reset.";
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
            StatusMessage = e.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Restart() => InvokeOnMainThread(() => Program.Restart());

    public void Reload()
    {
        _personalFolderPath = Program.ReadPersonalPathConfig();
        UpdatePersonalFolderPreview();
        UpdateScreenshotsFolderPreview();
        RefreshIntegrations();
        RefreshStartWithWindows();


        AdvancedSettings = new ObservableCollection<AdvancedSettingItem>(
            TypeDescriptor.GetProperties(Settings).Cast<PropertyDescriptor>()
                .Where(x => x.IsBrowsable && (x.PropertyType == typeof(bool) || x.PropertyType == typeof(int) || x.PropertyType == typeof(string) || x.PropertyType.IsEnum))
                .OrderBy(x => x.Category)
                .ThenBy(x => x.DisplayName)
                .Select(x => new AdvancedSettingItem(Settings, x, OnAdvancedSettingChanged)));

        AdvancedSettingCategories = new ObservableCollection<AdvancedSettingCategory>(AdvancedSettings
            .GroupBy(x => x.Category)
            .Select(group => new AdvancedSettingCategory(group.Key, new ObservableCollection<AdvancedSettingItem>(group))));


        NavigationItems = CreateNavigationItems();
        SelectedNavigationItem = NavigationItems.FirstOrDefault();

        OnPropertyChanged(string.Empty);
    }

    private ObservableCollection<SettingsNavigationItem> CreateNavigationItems()
    {
        return
        [
            Nav("general", "General", LucideIcons.settings),
            Nav("integration", "Integration", LucideIcons.plug),
            Nav("paths", "Paths", LucideIcons.folder),
            Nav("settings", "Settings", LucideIcons.database_backup),
            Nav("main-window", "Main window", LucideIcons.monitor),
            Nav("history", "Recent tasks", LucideIcons.history),
            Nav("print", "Print", LucideIcons.printer),
            Nav("advanced", "Advanced", LucideIcons.sliders_horizontal)
        ];
    }

    private static SettingsNavigationItem Nav(string id, string title, string icon) => new(id, title, icon);

    private void RefreshIntegrations()
    {
#if !MicrosoftStore
        _shellContextMenu = IntegrationHelpers.CheckShellContextMenuButton();
        _editWithShareX = IntegrationHelpers.CheckEditShellContextMenuButton();
        _sendToMenu = IntegrationHelpers.CheckSendToMenuButton();
#endif
#if STEAM
#endif
    }

    private void RefreshStartWithWindows()
    {
        StartWithWindowsText = Resources.ApplicationSettingsForm_cbStartWithWindows_Text;
        StartWithWindowsEnabled = false;

        try
        {
            StartupState state = InvokeOnMainThread(() => StartupManager.State);
            _startWithWindows = state == StartupState.Enabled || state == StartupState.EnabledByPolicy;
            OnPropertyChanged(nameof(StartWithWindows));

            if (state == StartupState.DisabledByUser)
            {
                StartWithWindowsText = Resources.ApplicationSettingsForm_cbStartWithWindows_DisabledByUser_Text;
            }
            else if (state == StartupState.DisabledByPolicy)
            {
                StartWithWindowsText = Resources.ApplicationSettingsForm_cbStartWithWindows_DisabledByPolicy_Text;
            }
            else if (state == StartupState.EnabledByPolicy)
            {
                StartWithWindowsText = Resources.ApplicationSettingsForm_cbStartWithWindows_EnabledByPolicy_Text;
            }
            else
            {
                StartWithWindowsEnabled = true;
            }
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
            StatusMessage = e.Message;
        }
    }

    private void UpdatePersonalFolderPreview()
    {
        try
        {
            string path = FileHelpers.SanitizePath(_personalFolderPath);
            if (string.IsNullOrEmpty(path))
            {
                path = Program.Portable ? Program.PortablePersonalFolder : Program.DefaultPersonalFolder;
            }
            else
            {
                path = FileHelpers.GetAbsolutePath(path);
            }

            PersonalFolderPreview = path;
        }
        catch (Exception e)
        {
            PersonalFolderPreview = "Error: " + e.Message;
        }
    }

    private void UpdateScreenshotsFolderPreview()
    {
        try
        {
            ScreenshotsFolderPreview = TaskHelpers.GetScreenshotsFolder();
        }
        catch (Exception e)
        {
            ScreenshotsFolderPreview = "Error: " + e.Message;
        }
    }

    private bool IsPage(string id) => SelectedNavigationItem?.Id == id;

    private void OnPageChanged()
    {
        OnPropertyChanged(nameof(IsGeneralPage));
        OnPropertyChanged(nameof(IsIntegrationPage));
        OnPropertyChanged(nameof(IsPathsPage));
        OnPropertyChanged(nameof(IsSettingsPage));
        OnPropertyChanged(nameof(IsMainWindowPage));
        OnPropertyChanged(nameof(IsHistoryPage));
        OnPropertyChanged(nameof(IsPrintPage));
        OnPropertyChanged(nameof(IsAdvancedPage));
    }

    private bool SetSetting<T>(T current, T value, Action<T> setter, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(current, value))
        {
            return false;
        }

        setter(value);
        OnPropertyChanged(propertyName);
        MarkChanged();
        return true;
    }

    private void MarkChanged()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void OnAdvancedSettingChanged()
    {
        MarkChanged();
    }

    private void OnSaveTimerTick(object? sender, EventArgs e)
    {
        _saveTimer.Stop();
        FlushPersonalPath();
        InvokeOnMainThread(Program.MainForm.ApplyApplicationSettings);
        SettingManager.SaveApplicationConfigAsync();
    }

    private void FlushPersonalPath()
    {
        if (!_personalPathDirty)
        {
            return;
        }

        _personalPathDirty = false;

        try
        {
            bool changed = InvokeOnMainThread(() => Program.WritePersonalPathConfig(FileHelpers.SanitizePath(_personalFolderPath)));
            if (changed)
            {
                RestartRequired = true;
            }
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
            StatusMessage = e.Message;
        }
    }

    private async Task UpdateMainFormAsync()
    {
        Task updateTask = InvokeOnMainThread(() => Program.MainForm.UpdateControls());
        await updateTask;
    }

    private static IReadOnlyList<EnumOption<T>> CreateEnumOptions<T>() where T : struct, Enum =>
        Helpers.GetEnums<T>().Select(x => new EnumOption<T>(x, x.GetLocalizedDescription())).ToArray();

    private static EnumOption<T>? Find<T>(IReadOnlyList<EnumOption<T>> options, T value) =>
        options.FirstOrDefault(x => EqualityComparer<T>.Default.Equals(x.Value, value));

    private static void InvokeOnMainThread(Action action)
    {
        if (Program.MainForm == null || Program.MainForm.IsDisposed)
        {
            return;
        }

        if (Program.MainForm.InvokeRequired)
        {
            Program.MainForm.Invoke(action);
        }
        else
        {
            action();
        }
    }

    private static T InvokeOnMainThread<T>(Func<T> action)
    {
        if (Program.MainForm == null || Program.MainForm.IsDisposed)
        {
            return action();
        }

        if (Program.MainForm.InvokeRequired)
        {
            return (T)Program.MainForm.Invoke(action);
        }

        return action();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _saveTimer.Stop();
        FlushPersonalPath();
        InvokeOnMainThread(Program.MainForm.ApplyApplicationSettings);
        SettingManager.SaveApplicationConfigAsync();
    }
}
