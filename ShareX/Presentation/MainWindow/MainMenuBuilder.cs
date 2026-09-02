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

using ShareX.AvaloniaUI.Theming;
using ShareX.HelpersLib;
using ShareX.Properties;
using ShareX.ScreenCaptureLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShareX;

internal sealed class MainMenuBuilder
{
    private readonly MainForm _host;
    private readonly bool _trayMenu;

    public MainMenuBuilder(MainForm host, bool trayMenu = false)
    {
        _host = host;
        _trayMenu = trayMenu;
    }

    public IReadOnlyList<MainNavigationSection> BuildNavigation()
    {
        return new List<MainNavigationSection>
        {
            new("Capture", LucideIcons.camera, BuildCaptureMenu),
            new("Workflows", LucideIcons.list_checks, BuildWorkflowsMenu),
            new("After capture tasks", LucideIcons.image_up, BuildAfterCaptureMenu),
            new("Application settings", LucideIcons.settings, () => Run(MainFormCommand.ApplicationSettings)),
            new("Task settings", LucideIcons.sliders_horizontal, () => Run(MainFormCommand.TaskSettings)),
            new("Hotkey settings", LucideIcons.keyboard, () => Run(MainFormCommand.HotkeySettings)),
            new("Screenshots folder", LucideIcons.folder_open, () => Run(MainFormCommand.ScreenshotsFolder)),
            new("Debug", LucideIcons.bug, BuildDebugMenu),
            new("About", LucideIcons.info, () => Run(MainFormCommand.About))
        };
    }

    public IReadOnlyList<MainMenuEntry> BuildTrayMenu()
    {
        List<MainMenuEntry> items = new()
        {
            Parent("Capture", LucideIcons.camera, BuildCaptureMenu),
            Parent("Workflows", LucideIcons.list_checks, BuildWorkflowsMenu),
            MainMenuEntry.Separator(),
            Parent("After capture tasks", LucideIcons.image_up, BuildAfterCaptureMenu),
            MainMenuEntry.Separator(),
            Item("Application settings", LucideIcons.settings, () => Run(MainFormCommand.ApplicationSettings)),
            Item("Task settings", LucideIcons.sliders_horizontal, () => Run(MainFormCommand.TaskSettings)),
            Item("Hotkey settings", LucideIcons.keyboard, () => Run(MainFormCommand.HotkeySettings)),
            Item(Program.Settings.DisableHotkeys ? "Enable hotkeys" : "Disable hotkeys",
                Program.Settings.DisableHotkeys ? LucideIcons.keyboard : LucideIcons.keyboard_off,
                () => TaskHelpers.ToggleHotkeys()),
            MainMenuEntry.Separator(),
            Item("Screenshots folder", LucideIcons.folder_open, () => Run(MainFormCommand.ScreenshotsFolder)),
            MainMenuEntry.Separator(),
            Item("Restart as administrator", LucideIcons.shield, () => Program.Restart(true)),
            Parent("Recent items", LucideIcons.clipboard_list, BuildRecentItemsMenu,
                Program.Settings.RecentTasksSave && Program.Settings.RecentTasksShowInTrayMenu && TaskManager.RecentManager.Tasks.Count > 0),
            Item("Actions toolbar", LucideIcons.panel_top, () => TaskHelpers.ToggleActionsToolbar()),
            Item("Show " + Program.AppName, LucideIcons.maximize, MainWindowIntegration.Activate),
            Item("Exit", LucideIcons.log_out, _host.ForceClose)
        };

        return items;
    }

    private IReadOnlyList<MainMenuEntry> BuildCaptureMenu()
    {
        bool autoHide = !_trayMenu;
        return new List<MainMenuEntry>
        {
            Item("Fullscreen", LucideIcons.maximize, () => new CaptureFullscreen().Capture(autoHide)),
            Parent("Window", LucideIcons.app_window, BuildWindowMenu),
            Parent("Monitor", LucideIcons.monitor, BuildMonitorMenu),
            Item("Region", LucideIcons.scan, () => new CaptureRegion().Capture(autoHide)),
            Item("Region (light)", LucideIcons.square, () => new CaptureRegion(RegionCaptureType.Light).Capture(autoHide)),
            Item("Region (transparent)", LucideIcons.square_dashed, () => new CaptureRegion(RegionCaptureType.Transparent).Capture(autoHide)),
            Item("Last region", LucideIcons.layers, () => new CaptureLastRegion().Capture(autoHide)),
            Item("Scrolling capture", LucideIcons.scroll_text, async () => await TaskHelpers.OpenScrollingCapture()),
            Item("Auto capture", LucideIcons.clock, () => TaskHelpers.OpenAutoCapture()),
            MainMenuEntry.Separator(),
            new MainMenuEntry("Show cursor", LucideIcons.mouse_pointer_2,
                () => Program.DefaultTaskSettings.CaptureSettings.ShowCursor = !Program.DefaultTaskSettings.CaptureSettings.ShowCursor,
                isChecked: Program.DefaultTaskSettings.CaptureSettings.ShowCursor,
                toggleType: MainMenuToggleType.CheckBox),
            Parent(string.Format(Resources.ScreenshotDelay0S, Program.DefaultTaskSettings.CaptureSettings.ScreenshotDelay.ToString("0.#")),
                LucideIcons.timer, BuildScreenshotDelayMenu)
        };
    }

    private IReadOnlyList<MainMenuEntry> BuildWindowMenu()
    {
        List<MainMenuEntry> items = new();

        try
        {
            foreach (WindowInfo window in new WindowsList().GetVisibleWindowsList())
            {
                WindowInfo selectedWindow = window;
                string title = selectedWindow.Text.Truncate(50, "...");
                items.Add(Item(title, IconForName(title),
                    () => new CaptureWindow(selectedWindow.Handle).Capture(!_trayMenu)));
            }
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }

        if (items.Count == 0)
        {
            items.Add(new MainMenuEntry("No windows found", LucideIcons.app_window, isEnabled: false));
        }

        return items;
    }

    private IReadOnlyList<MainMenuEntry> BuildMonitorMenu()
    {
        List<MainMenuEntry> items = new();
        Screen[] screens = Screen.AllScreens;

        for (int i = 0; i < screens.Length; i++)
        {
            Rectangle bounds = screens[i].Bounds;
            string label = $"{i + 1}. {bounds.Width}x{bounds.Height}";
            items.Add(Item(label, i == 0 ? LucideIcons.monitor : LucideIcons.monitor_up,
                () => new CaptureMonitor(bounds).Capture(!_trayMenu)));
        }

        return items;
    }

    private IReadOnlyList<MainMenuEntry> BuildScreenshotDelayMenu()
    {
        decimal current = Program.DefaultTaskSettings.CaptureSettings.ScreenshotDelay;
        return Enumerable.Range(0, 6)
            .Select(delay => new MainMenuEntry(
                string.Format(Resources.ScreenshotDelay0S, delay),
                delay == 0 ? LucideIcons.timer_off : LucideIcons.timer,
                () => _host.SetAvaloniaScreenshotDelay(delay),
                isChecked: Math.Abs(current - delay) < 0.01m,
                toggleType: MainMenuToggleType.Radio))
            .ToArray();
    }

    private IReadOnlyList<MainMenuEntry> BuildWorkflowsMenu()
    {
        List<MainMenuEntry> items = new();

        if (Program.HotkeysConfig?.Hotkeys != null)
        {
            foreach (HotkeySettings hotkey in Program.HotkeysConfig.Hotkeys)
            {
                if (hotkey.TaskSettings.Job == HotkeyType.None ||
                    (Program.Settings.WorkflowsOnlyShowEdited && hotkey.TaskSettings.IsUsingDefaultSettings))
                {
                    continue;
                }

                HotkeySettings workflow = hotkey;
                string title = workflow.TaskSettings + (workflow.TaskSettings.IsUsingDefaultSettings ? string.Empty : "*");
                if (workflow.HotkeyInfo.IsValidHotkey)
                {
                    title += $"    {workflow.HotkeyInfo}";
                }

                items.Add(Item(title, TaskHelpers.FindMenuLucideIcon(workflow.TaskSettings.Job),
                    async () => await TaskHelpers.ExecuteJob(workflow.TaskSettings)));
            }
        }

        if (!_trayMenu)
        {
            if (items.Count > 0)
            {
                items.Add(MainMenuEntry.Separator());
            }

            items.Add(Item("Add workflows from Hotkey settings...", LucideIcons.keyboard,
                () => Run(MainFormCommand.HotkeySettings)));
        }

        return items;
    }

    private IReadOnlyList<MainMenuEntry> BuildAfterCaptureMenu()
    {
        AfterCaptureTasks value = Program.DefaultTaskSettings.AfterCaptureJob;
        IEnumerable<AfterCaptureTasks> values = Helpers.GetEnums<AfterCaptureTasks>().Skip(1);

        return values.Select(task => new MainMenuEntry(
            task.GetLocalizedDescription(),
            IconForName(task.ToString()),
            () => Program.DefaultTaskSettings.AfterCaptureJob = Program.DefaultTaskSettings.AfterCaptureJob.Swap(task),
            createChildren: task == AfterCaptureTasks.AddImageEffects ? BuildImageEffectPresetMenu : null,
            isChecked: value.HasFlag(task),
            toggleType: MainMenuToggleType.CheckBox)).ToArray();
    }

    private IReadOnlyList<MainMenuEntry> BuildImageEffectPresetMenu()
    {
        List<MainMenuEntry> items = new()
        {
            new MainMenuEntry("Enable add image effects", LucideIcons.wand_sparkles,
                () => Program.DefaultTaskSettings.AfterCaptureJob =
                    Program.DefaultTaskSettings.AfterCaptureJob.Swap(AfterCaptureTasks.AddImageEffects),
                isChecked: Program.DefaultTaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.AddImageEffects),
                toggleType: MainMenuToggleType.CheckBox),
            MainMenuEntry.Separator()
        };
        List<ImageEffectsLib.ImageEffectPreset>? presets = Program.DefaultTaskSettings.ImageSettings.ImageEffectPresets;

        if (presets != null)
        {
            for (int i = 0; i < presets.Count; i++)
            {
                int index = i;
                ImageEffectsLib.ImageEffectPreset? preset = presets[i];
                if (preset != null)
                {
                    items.Add(new MainMenuEntry(preset.ToString(), LucideIcons.wand_sparkles,
                        () => Program.DefaultTaskSettings.ImageSettings.SelectedImageEffectPreset = index,
                        isChecked: index == Program.DefaultTaskSettings.ImageSettings.SelectedImageEffectPreset,
                        toggleType: MainMenuToggleType.Radio));
                }
            }
        }

        if (items.Count == 2)
        {
            items.Add(new MainMenuEntry("No image effect presets", LucideIcons.wand_sparkles, isEnabled: false));
        }

        return items;
    }

    private IReadOnlyList<MainMenuEntry> BuildDebugMenu()
    {
        return new List<MainMenuEntry>
        {
            Item("Show debug log", LucideIcons.file_text, () => Run(MainFormCommand.DebugLog))
        };
    }

    private static IReadOnlyList<MainMenuEntry> BuildRecentItemsMenu()
    {
        IEnumerable<RecentTask> tasks = TaskManager.RecentManager.Tasks;
        if (Program.Settings.RecentTasksTrayMenuMostRecentFirst)
        {
            tasks = tasks.Reverse();
        }

        return tasks.Select(task => Parent(task.TrayMenuText, IconForName(task.TrayMenuText), () => new List<MainMenuEntry>
        {
            Item("Copy", LucideIcons.copy, task.Copy),
            Item("Open", LucideIcons.external_link, task.Open)
        })).ToArray();
    }

    private void Run(MainFormCommand command) => _host.ExecuteAvaloniaMainFormCommand(command);

    private static MainMenuEntry Item(string header, string icon, Action execute, bool isVisible = true) =>
        new(header, icon, execute, isVisible: isVisible);

    private static MainMenuEntry Item(string header, string icon, Func<Task> execute, bool isVisible = true) =>
        new(header, icon, execute, isVisible: isVisible);

    private static MainMenuEntry Parent(string header, string icon, Func<IReadOnlyList<MainMenuEntry>> children, bool isVisible = true) =>
        new(header, icon, createChildren: children, isVisible: isVisible);

    private static string IconForName(string name)
    {
        int hash = StringComparer.Ordinal.GetHashCode(name) & int.MaxValue;
        string[] icons =
        {
            LucideIcons.circle_check, LucideIcons.copy, LucideIcons.file, LucideIcons.folder,
            LucideIcons.image, LucideIcons.link, LucideIcons.cloud, LucideIcons.sparkles,
            LucideIcons.clipboard, LucideIcons.database, LucideIcons.external_link, LucideIcons.settings_2
        };
        return icons[hash % icons.Length];
    }
}
