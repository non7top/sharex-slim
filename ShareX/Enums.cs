#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.HelpersLib;
using System;
using System.ComponentModel;

#if MicrosoftStore
using Windows.ApplicationModel;
#endif

namespace ShareX
{
    public enum ShareXBuild
    {
        Debug,
        Release,
        Unknown
    }

    public enum SupportedLanguage
    {
        Automatic, // Localized
        [Description("العربية (Arabic)")]
        Arabic,
        [Description("Nederlands (Dutch)")]
        Dutch,
        [Description("English")]
        English,
        [Description("Français (French)")]
        French,
        [Description("Deutsch (German)")]
        German,
        [Description("עִברִית (Hebrew)")]
        Hebrew,
        [Description("Magyar (Hungarian)")]
        Hungarian,
        [Description("Bahasa Indonesia (Indonesian)")]
        Indonesian,
        [Description("Italiano (Italian)")]
        Italian,
        [Description("日本語 (Japanese)")]
        Japanese,
        [Description("한국어 (Korean)")]
        Korean,
        [Description("Español mexicano (Mexican Spanish)")]
        MexicanSpanish,
        [Description("فارسی (Persian)")]
        Persian,
        [Description("Polski (Polish)")]
        Polish,
        [Description("Português (Portuguese)")]
        Portuguese,
        [Description("Português-Brasil (Portuguese-Brazil)")]
        PortugueseBrazil,
        [Description("Română (Romanian)")]
        Romanian,
        [Description("Русский (Russian)")]
        Russian,
        [Description("简体中文 (Simplified Chinese)")]
        SimplifiedChinese,
        [Description("Español (Spanish)")]
        Spanish,
        [Description("繁體中文 (Traditional Chinese)")]
        TraditionalChinese,
        [Description("Türkçe (Turkish)")]
        Turkish,
        [Description("Українська (Ukrainian)")]
        Ukrainian,
        [Description("Tiếng Việt (Vietnamese)")]
        Vietnamese
    }

    public enum TaskJob
    {
        Job
    }

    public enum TaskStatus
    {
        InQueue,
        Preparing,
        Working,
        Stopping,
        Stopped,
        Failed,
        Completed
    }

    [Flags]
    public enum AfterCaptureTasks // Localized
    {
        None = 0,
        ShowQuickTaskMenu = 1,
        ShowAfterCaptureWindow = 1 << 1,
        AddImageEffects = 1 << 2,
        AnnotateImage = 1 << 3,
        CopyImageToClipboard = 1 << 4,
        SendImageToPrinter = 1 << 5,
        SaveImageToFile = 1 << 6,
        SaveImageToFileWithDialog = 1 << 7,
        SaveThumbnailImageToFile = 1 << 8,
        PerformActions = 1 << 9,
        CopyFileToClipboard = 1 << 10,
        CopyFilePathToClipboard = 1 << 11,
        CopyFolderPathToClipboard = 1 << 12,
        ShowInExplorer = 1 << 13,
        DeleteFile = 1 << 14
    }

    public enum CaptureType
    {
        Fullscreen,
        Monitor,
        ActiveMonitor,
        Window,
        ActiveWindow,
        Region,
        CustomRegion,
        LastRegion
    }

    public enum HotkeyType // Localized
    {
        None,
        // Screen capture
        [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
        PrintScreen,
        [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
        ActiveWindow,
        [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
        CustomWindow,
        [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
        ActiveMonitor,
        [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
        RectangleRegion,
        [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
        RectangleLight,
        [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
        RectangleTransparent,
        [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
        CustomRegion,
        [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
        LastRegion,
        [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
        ScrollingCapture,
        [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
        AutoCapture,
        [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
        StartAutoCapture,
        [Category(EnumExtensions.HotkeyType_Category_ScreenCapture)]
        StopAutoCapture,
        // Other
        [Category(EnumExtensions.HotkeyType_Category_Other)]
        DisableHotkeys,
        [Category(EnumExtensions.HotkeyType_Category_Other)]
        OpenMainWindow,
        [Category(EnumExtensions.HotkeyType_Category_Other)]
        OpenScreenshotsFolder,
        [Category(EnumExtensions.HotkeyType_Category_Other)]
        ToggleActionsToolbar,
        [Category(EnumExtensions.HotkeyType_Category_Other)]
        ToggleTrayMenu,
        [Category(EnumExtensions.HotkeyType_Category_Other)]
        ExitShareX
    }

    public enum ToastClickAction // Localized
    {
        CloseNotification,
        AnnotateImage,
        CopyImageToClipboard,
        CopyFile,
        CopyFilePath,
        OpenFile,
        OpenFolder,
        DeleteFile
    }

    public enum ThumbnailViewClickAction // Localized
    {
        Default,
        Select,
        OpenFile,
        OpenFolder,
        EditImage
    }

    public enum FileExistAction // Localized
    {
        Ask,
        Overwrite,
        UniqueName,
        Cancel
    }

    public enum ThumbnailTitleLocation // Localized
    {
        Top, Bottom
    }

    public enum RegionCaptureType
    {
        Default, Light, Transparent
    }

#if !MicrosoftStore
    public enum StartupState
    {
        Disabled,
        DisabledByUser,
        Enabled,
        DisabledByPolicy,
        EnabledByPolicy
    }
#else
    public enum StartupState
    {
        Disabled = StartupTaskState.Disabled,
        DisabledByUser = StartupTaskState.DisabledByUser,
        Enabled = StartupTaskState.Enabled,
        DisabledByPolicy = StartupTaskState.DisabledByPolicy,
        EnabledByPolicy = StartupTaskState.EnabledByPolicy
    }
#endif

    public enum NotificationSound
    {
        Capture,
        TaskCompleted,
        ActionCompleted,
        Error
    }
}
