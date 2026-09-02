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

using Newtonsoft.Json;
using ShareX.HelpersLib;
using ShareX.ImageEffectsLib;
using ShareX.ScreenCaptureLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace ShareX
{
    public class TaskSettings
    {
        [JsonIgnore]
        public TaskSettings TaskSettingsReference { get; private set; }

        [JsonIgnore]
        public bool IsSafeTaskSettings => TaskSettingsReference != null;

        public string Description = "";

        public HotkeyType Job = HotkeyType.None;

        public bool UseDefaultAfterCaptureJob = true;
        public AfterCaptureTasks AfterCaptureJob = AfterCaptureTasks.CopyImageToClipboard | AfterCaptureTasks.SaveImageToFile;

        public bool OverrideScreenshotsFolder = false;
        public string ScreenshotsFolder = "";

        public bool UseDefaultGeneralSettings = true;
        public TaskSettingsGeneral GeneralSettings = new TaskSettingsGeneral();

        public bool UseDefaultImageSettings = true;
        public TaskSettingsImage ImageSettings = new TaskSettingsImage();

        [JsonIgnore]
        public TaskSettingsImage ImageSettingsReference
        {
            get
            {
                if (UseDefaultImageSettings)
                {
                    return Program.DefaultTaskSettings.ImageSettings;
                }

                return TaskSettingsReference.ImageSettings;
            }
        }

        public bool UseDefaultCaptureSettings = true;
        public TaskSettingsCapture CaptureSettings = new TaskSettingsCapture();

        [JsonIgnore]
        public TaskSettingsCapture CaptureSettingsReference
        {
            get
            {
                if (UseDefaultCaptureSettings)
                {
                    return Program.DefaultTaskSettings.CaptureSettings;
                }

                return TaskSettingsReference.CaptureSettings;
            }
        }

        public bool UseDefaultFileNamingSettings = true;
        public TaskSettingsFileNaming FileNamingSettings = new TaskSettingsFileNaming();

        public bool UseDefaultActions = true;
        public List<ExternalProgram> ExternalPrograms = new List<ExternalProgram>();

        public bool UseDefaultAdvancedSettings = true;
        public TaskSettingsAdvanced AdvancedSettings = new TaskSettingsAdvanced();

        public override string ToString()
        {
            return !string.IsNullOrEmpty(Description) ? Description : Job.GetLocalizedDescription();
        }

        public bool IsUsingDefaultSettings
        {
            get
            {
                return UseDefaultAfterCaptureJob && !OverrideScreenshotsFolder && UseDefaultGeneralSettings && UseDefaultImageSettings &&
                    UseDefaultCaptureSettings && UseDefaultFileNamingSettings && UseDefaultActions && UseDefaultAdvancedSettings;
            }
        }

        public static TaskSettings GetDefaultTaskSettings()
        {
            TaskSettings taskSettings = new TaskSettings();
            taskSettings.SetDefaultSettings();
            taskSettings.TaskSettingsReference = Program.DefaultTaskSettings;
            return taskSettings;
        }

        public static TaskSettings GetSafeTaskSettings(TaskSettings taskSettings)
        {
            TaskSettings safeTaskSettings;

            if (taskSettings.IsUsingDefaultSettings && Program.DefaultTaskSettings != null)
            {
                safeTaskSettings = Program.DefaultTaskSettings.Copy();
                safeTaskSettings.Description = taskSettings.Description;
                safeTaskSettings.Job = taskSettings.Job;
            }
            else
            {
                safeTaskSettings = taskSettings.Copy();
                safeTaskSettings.SetDefaultSettings();
            }

            safeTaskSettings.TaskSettingsReference = taskSettings;
            return safeTaskSettings;
        }

        public void SetDefaultSettings()
        {
            if (Program.DefaultTaskSettings != null)
            {
                TaskSettings defaultTaskSettings = Program.DefaultTaskSettings.Copy();

                if (UseDefaultAfterCaptureJob)
                {
                    AfterCaptureJob = defaultTaskSettings.AfterCaptureJob;
                }

                if (UseDefaultGeneralSettings)
                {
                    GeneralSettings = defaultTaskSettings.GeneralSettings;
                }

                if (UseDefaultImageSettings)
                {
                    ImageSettings = defaultTaskSettings.ImageSettings;
                }

                if (UseDefaultCaptureSettings)
                {
                    CaptureSettings = defaultTaskSettings.CaptureSettings;
                }

                if (UseDefaultFileNamingSettings)
                {
                    FileNamingSettings = defaultTaskSettings.FileNamingSettings;
                }

                if (UseDefaultActions)
                {
                    ExternalPrograms = defaultTaskSettings.ExternalPrograms;
                }

                if (UseDefaultAdvancedSettings)
                {
                    AdvancedSettings = defaultTaskSettings.AdvancedSettings;
                }
            }
        }

        public void Cleanup()
        {
            if (UseDefaultGeneralSettings)
            {
                GeneralSettings = null;
            }

            if (UseDefaultImageSettings)
            {
                ImageSettings = null;
            }

            if (UseDefaultCaptureSettings)
            {
                CaptureSettings = null;
            }

            if (UseDefaultFileNamingSettings)
            {
                FileNamingSettings = null;
            }

            if (UseDefaultActions)
            {
                ExternalPrograms = null;
            }

            if (UseDefaultAdvancedSettings)
            {
                AdvancedSettings = null;
            }
        }
    }

    public class TaskSettingsGeneral
    {
        #region General / Notifications

        public bool PlaySoundAfterCapture = true;
        public bool PlaySoundAfterAction = true;
        public bool ShowToastNotificationAfterTaskCompleted = true;
        public float ToastWindowDuration = 3f;
        public float ToastWindowFadeDuration = 1f;
        public ContentAlignment ToastWindowPlacement = ContentAlignment.BottomRight;
        public Size ToastWindowSize = new Size(400, 300);
        public ToastClickAction ToastWindowLeftClickAction = ToastClickAction.OpenFile;
        public ToastClickAction ToastWindowRightClickAction = ToastClickAction.CloseNotification;
        public ToastClickAction ToastWindowMiddleClickAction = ToastClickAction.AnnotateImage;
        public List<NotificationActionButton> ToastWindowButtons = NotificationActionButton.CreateDefaultButtons();
        public bool ToastWindowAutoHide = true;
        public bool DisableNotificationsOnFullscreen = false;
        public bool UseCustomCaptureSound = false;
        public string CustomCaptureSoundPath = "";
        public bool UseCustomTaskCompletedSound = false;
        public string CustomTaskCompletedSoundPath = "";
        public bool UseCustomActionCompletedSound = false;
        public string CustomActionCompletedSoundPath = "";
        public bool UseCustomErrorSound = false;
        public string CustomErrorSoundPath = "";

        #endregion
    }

    public class TaskSettingsImage
    {
        #region Image / General

        public EImageFormat ImageFormat = EImageFormat.PNG;
        public PNGBitDepth ImagePNGBitDepth = PNGBitDepth.Default;
        public int ImageJPEGQuality = 90;
        public GIFQuality ImageGIFQuality = GIFQuality.Default;
        public bool ImageAutoUseJPEG = true;
        public int ImageAutoUseJPEGSize = 2048;
        public bool ImageAutoJPEGQuality = false;
        public FileExistAction FileExistAction = FileExistAction.Ask;

        #endregion Image / General

        #region Image / Effects

        public List<ImageEffectPreset> ImageEffectPresets = new List<ImageEffectPreset>() { ImageEffectPreset.GetDefaultPreset() };
        public int SelectedImageEffectPreset = 0;

        public bool ShowImageEffectsWindowAfterCapture = false;
        public bool ImageEffectOnlyRegionCapture = false;
        public bool UseRandomImageEffect = false;

        #endregion Image / Effects

        #region Image / Thumbnail

        public int ThumbnailWidth = 200;
        public int ThumbnailHeight = 0;
        public string ThumbnailName = "-thumbnail";
        public bool ThumbnailCheckSize = false;

        #endregion Image / Thumbnail
    }

    public class TaskSettingsCapture
    {
        #region Capture / General

        public bool ShowCursor = true;
        public decimal ScreenshotDelay = 0;
        public bool CaptureTransparent = false;
        public bool CaptureShadow = true;
        public int CaptureShadowOffset = 100;
        public bool CaptureClientArea = false;
        public bool CaptureAutoHideTaskbar = false;
        public bool CaptureAutoHideDesktopIcons = false;
        public bool HDRScreenshotColorCorrection = false;
        public Rectangle CaptureCustomRegion = new Rectangle(0, 0, 0, 0);
        public string CaptureCustomWindow = "";

        #endregion Capture / General

        #region Capture / Region capture

        public RegionCaptureOptions SurfaceOptions = new RegionCaptureOptions();

        #endregion Capture / Region capture

        #region Capture / Scrolling capture

        public ScrollingCaptureOptions ScrollingCaptureOptions = new ScrollingCaptureOptions();

        #endregion Capture / Scrolling capture
    }

    public class TaskSettingsFileNaming
    {
        #region File naming

        public bool UseCustomTimeZone = false;
        public TimeZoneInfo CustomTimeZone = TimeZoneInfo.Utc;
        public string NameFormatPattern = "%ra{10}";
        public string NameFormatPatternActiveWindow = "%pn_%ra{10}";

        #endregion File naming
    }

    public class TaskSettingsAdvanced
    {
        [Category("Capture"), DefaultValue(false), Description("Disable annotation support in region capture.")]
        public bool RegionCaptureDisableAnnotation { get; set; }

        [Category("Name pattern"), DefaultValue(100), Description("Maximum name pattern length for file name.")]
        public int NamePatternMaxLength { get; set; }

        [Category("Name pattern"), DefaultValue(50), Description("Maximum name pattern title (%t) length for file name.")]
        public int NamePatternMaxTitleLength { get; set; }

        public TaskSettingsAdvanced()
        {
            this.ApplyDefaultPropertyValues();
        }
    }
}
