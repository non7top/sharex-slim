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

using ShareX.AvaloniaUI.Theming;
using ShareX.AvaloniaUI.Windows;
using ShareX.HelpersLib;
using ShareX.ImageEffectsLib;
using ShareX.Properties;
using ShareX.ScreenCaptureLib;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShareX
{
    public static class TaskHelpers
    {
        public static async Task ExecuteJob(HotkeyType job, string filePath = null)
        {
            await ExecuteJob(Program.DefaultTaskSettings, job, filePath);
        }

        public static async Task ExecuteJob(TaskSettings taskSettings)
        {
            await ExecuteJob(taskSettings, taskSettings.Job);
        }

        public static async Task ExecuteJob(TaskSettings taskSettings, HotkeyType job, string filePath = null)
        {
            if (job == HotkeyType.None) return;

            DebugHelper.WriteLine("Executing: " + job.GetLocalizedDescription());

            TaskSettings safeTaskSettings = TaskSettings.GetSafeTaskSettings(taskSettings);

            switch (job)
            {
                // Screen capture
                case HotkeyType.PrintScreen:
                    new CaptureFullscreen().Capture(safeTaskSettings);
                    break;
                case HotkeyType.ActiveWindow:
                    new CaptureActiveWindow().Capture(safeTaskSettings);
                    break;
                case HotkeyType.ActiveMonitor:
                    new CaptureActiveMonitor().Capture(safeTaskSettings);
                    break;
                case HotkeyType.RectangleRegion:
                    new CaptureRegion().Capture(safeTaskSettings);
                    break;
                case HotkeyType.RectangleLight:
                    new CaptureRegion(RegionCaptureType.Light).Capture(safeTaskSettings);
                    break;
                case HotkeyType.RectangleTransparent:
                    new CaptureRegion(RegionCaptureType.Transparent).Capture(safeTaskSettings);
                    break;
                case HotkeyType.CustomRegion:
                    new CaptureCustomRegion().Capture(safeTaskSettings);
                    break;
                case HotkeyType.CustomWindow:
                    new CaptureCustomWindow().Capture(safeTaskSettings);
                    break;
                case HotkeyType.LastRegion:
                    new CaptureLastRegion().Capture(safeTaskSettings);
                    break;
                case HotkeyType.ScrollingCapture:
                    await OpenScrollingCapture(safeTaskSettings);
                    break;
                case HotkeyType.AutoCapture:
                    OpenAutoCapture(safeTaskSettings);
                    break;
                case HotkeyType.StartAutoCapture:
                    StartAutoCapture(safeTaskSettings);
                    break;
                case HotkeyType.StopAutoCapture:
                    StopAutoCapture();
                    break;
                // Other
                case HotkeyType.DisableHotkeys:
                    ToggleHotkeys(safeTaskSettings);
                    break;
                case HotkeyType.OpenMainWindow:
                    Program.MainForm.ForceActivate();
                    break;
                case HotkeyType.OpenScreenshotsFolder:
                    OpenScreenshotsFolder();
                    break;
                case HotkeyType.ToggleActionsToolbar:
                    ToggleActionsToolbar();
                    break;
                case HotkeyType.ToggleTrayMenu:
                    ToggleTrayMenu();
                    break;
                case HotkeyType.ExitShareX:
                    Program.MainForm.ForceClose();
                    break;
            }
        }

        public static ImageData PrepareImage(Image img, TaskSettings taskSettings)
        {
            ImageData imageData = new ImageData();
            imageData.ImageStream = SaveImageAsStream(img, taskSettings.ImageSettings.ImageFormat, taskSettings);
            imageData.ImageFormat = taskSettings.ImageSettings.ImageFormat;

            if (taskSettings.ImageSettings.ImageAutoUseJPEG && taskSettings.ImageSettings.ImageFormat != EImageFormat.JPEG &&
                imageData.ImageStream.Length > taskSettings.ImageSettings.ImageAutoUseJPEGSize * 1000)
            {
                imageData.ImageStream.Dispose();

                using (Bitmap newImage = ImageHelpers.FillBackground(img, Color.White))
                {
                    if (taskSettings.ImageSettings.ImageAutoJPEGQuality)
                    {
                        imageData.ImageStream = ImageHelpers.SaveJPEGAutoQuality(newImage, taskSettings.ImageSettings.ImageAutoUseJPEGSize * 1000, 2, 70, 100);
                    }
                    else
                    {
                        imageData.ImageStream = ImageHelpers.SaveJPEG(newImage, taskSettings.ImageSettings.ImageJPEGQuality);
                    }
                }

                imageData.ImageFormat = EImageFormat.JPEG;
            }

            return imageData;
        }

        public static string CreateThumbnail(Bitmap bmp, string folder, string fileName, TaskSettings taskSettings)
        {
            if ((taskSettings.ImageSettings.ThumbnailWidth > 0 || taskSettings.ImageSettings.ThumbnailHeight > 0) && (!taskSettings.ImageSettings.ThumbnailCheckSize ||
                (bmp.Width > taskSettings.ImageSettings.ThumbnailWidth && bmp.Height > taskSettings.ImageSettings.ThumbnailHeight)))
            {
                string thumbnailFileName = Path.GetFileNameWithoutExtension(fileName) + taskSettings.ImageSettings.ThumbnailName + ".jpg";
                string thumbnailFilePath = HandleExistsFile(folder, thumbnailFileName, taskSettings);

                if (!string.IsNullOrEmpty(thumbnailFilePath))
                {
                    using (Bitmap thumbnail = (Bitmap)bmp.Clone())
                    using (Bitmap resizedImage = new Resize(taskSettings.ImageSettings.ThumbnailWidth, taskSettings.ImageSettings.ThumbnailHeight).Apply(thumbnail))
                    using (Bitmap newImage = ImageHelpers.FillBackground(resizedImage, Color.White))
                    {
                        ImageHelpers.SaveJPEG(newImage, thumbnailFilePath, 90);
                        return thumbnailFilePath;
                    }
                }
            }

            return null;
        }

        public static MemoryStream SaveImageAsStream(Image img, EImageFormat imageFormat, TaskSettings taskSettings)
        {
            return SaveImageAsStream(img, imageFormat, taskSettings.ImageSettings.ImagePNGBitDepth,
                taskSettings.ImageSettings.ImageJPEGQuality, taskSettings.ImageSettings.ImageGIFQuality);
        }

        public static MemoryStream SaveImageAsStream(Image img, EImageFormat imageFormat, PNGBitDepth pngBitDepth = PNGBitDepth.Automatic,
            int jpegQuality = 90, GIFQuality gifQuality = GIFQuality.Default)
        {
            MemoryStream ms = new MemoryStream();

            try
            {
                switch (imageFormat)
                {
                    case EImageFormat.PNG:
                        ImageHelpers.SavePNG(img, ms, pngBitDepth);

                        if (Program.Settings.PNGStripColorSpaceInformation)
                        {
                            using (ms)
                            {
                                return ImageHelpers.PNGStripColorSpaceInformation(ms);
                            }
                        }
                        break;
                    case EImageFormat.JPEG:
                        using (Bitmap newImage = ImageHelpers.FillBackground(img, Color.White))
                        {
                            ImageHelpers.SaveJPEG(newImage, ms, jpegQuality);
                        }
                        break;
                    case EImageFormat.GIF:
                        ImageHelpers.SaveGIF(img, ms, gifQuality);
                        break;
                    case EImageFormat.BMP:
                        img.Save(ms, ImageFormat.Bmp);
                        break;
                    case EImageFormat.TIFF:
                        img.Save(ms, ImageFormat.Tiff);
                        break;
                }
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e);
                e.ShowError();
            }

            return ms;
        }

        public static void SaveImageAsFile(Bitmap bmp, TaskSettings taskSettings, bool overwriteFile = false)
        {
            using (ImageData imageData = PrepareImage(bmp, taskSettings))
            {
                string screenshotsFolder = GetScreenshotsFolder(taskSettings);
                string fileName = GetFileName(taskSettings, imageData.ImageFormat.GetDescription(), bmp);
                string filePath = Path.Combine(screenshotsFolder, fileName);

                if (!overwriteFile)
                {
                    filePath = HandleExistsFile(filePath, taskSettings);
                }

                if (!string.IsNullOrEmpty(filePath))
                {
                    imageData.Write(filePath);
                    DebugHelper.WriteLine("Image saved to file: " + filePath);
                }
            }
        }

        public static string GetFileName(TaskSettings taskSettings, string extension, Bitmap bmp)
        {
            TaskMetadata metadata = new TaskMetadata(bmp);
            return GetFileName(taskSettings, extension, metadata);
        }

        public static string GetFileName(TaskSettings taskSettings, string extension = null, TaskMetadata metadata = null)
        {
            string fileName;

            NameParser nameParser = new NameParser(NameParserType.FileName)
            {
                AutoIncrementNumber = Program.Settings.NameParserAutoIncrementNumber,
                MaxNameLength = taskSettings.AdvancedSettings.NamePatternMaxLength,
                MaxTitleLength = taskSettings.AdvancedSettings.NamePatternMaxTitleLength,
                CustomTimeZone = taskSettings.FileNamingSettings.UseCustomTimeZone ? taskSettings.FileNamingSettings.CustomTimeZone : null
            };

            if (metadata != null)
            {
                if (metadata.Image != null)
                {
                    nameParser.ImageWidth = metadata.Image.Width;
                    nameParser.ImageHeight = metadata.Image.Height;
                }

                nameParser.WindowText = metadata.WindowTitle;
                nameParser.ProcessName = metadata.ProcessName;
            }

            if (!string.IsNullOrEmpty(taskSettings.FileNamingSettings.NameFormatPatternActiveWindow) && !string.IsNullOrEmpty(nameParser.WindowText))
            {
                fileName = nameParser.Parse(taskSettings.FileNamingSettings.NameFormatPatternActiveWindow);
            }
            else
            {
                fileName = nameParser.Parse(taskSettings.FileNamingSettings.NameFormatPattern);
            }

            Program.Settings.NameParserAutoIncrementNumber = nameParser.AutoIncrementNumber;

            if (!string.IsNullOrEmpty(extension))
            {
                fileName += "." + extension.TrimStart('.');
            }

            return fileName;
        }

        public static string GetScreenshotsFolder(TaskSettings taskSettings = null, TaskMetadata metadata = null)
        {
            string screenshotsFolder;

            NameParser nameParser = new NameParser(NameParserType.FilePath);

            if (metadata != null)
            {
                if (metadata.Image != null)
                {
                    nameParser.ImageWidth = metadata.Image.Width;
                    nameParser.ImageHeight = metadata.Image.Height;
                }

                nameParser.WindowText = metadata.WindowTitle;
                nameParser.ProcessName = metadata.ProcessName;
            }

            if (taskSettings != null && taskSettings.OverrideScreenshotsFolder && !string.IsNullOrEmpty(taskSettings.ScreenshotsFolder))
            {
                screenshotsFolder = nameParser.Parse(taskSettings.ScreenshotsFolder);
            }
            else
            {
                string subFolderPattern;

                if (!string.IsNullOrEmpty(Program.Settings.SaveImageSubFolderPatternWindow) && !string.IsNullOrEmpty(nameParser.WindowText))
                {
                    subFolderPattern = Program.Settings.SaveImageSubFolderPatternWindow;
                }
                else
                {
                    subFolderPattern = Program.Settings.SaveImageSubFolderPattern;
                }

                string subFolderPath = nameParser.Parse(subFolderPattern);
                screenshotsFolder = Path.Combine(Program.ScreenshotsParentFolder, subFolderPath);
            }

            return FileHelpers.GetAbsolutePath(screenshotsFolder);
        }

        public static void ShowAfterCaptureWindow(TaskSettings taskSettings, Action<AfterCaptureWindowResult> completed,
            TaskMetadata metadata = null, string filePath = null)
        {
            if (!taskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.ShowAfterCaptureWindow))
            {
                completed(new AfterCaptureWindowResult(true, null));
                return;
            }

            AfterCaptureWindowIntegration.Show(taskSettings, metadata, filePath, result =>
            {
                if (!result.Accepted)
                {
                    metadata?.Dispose();
                }

                completed(result);
            });
        }

        public static void PrintImage(Image img)
        {
            if (Program.Settings.DontShowPrintSettingsDialog)
            {
                using (PrintHelper printHelper = new PrintHelper(img))
                {
                    printHelper.Settings = Program.Settings.PrintSettings;
                    printHelper.Print();
                }
            }
            else
            {
                PrintWindowIntegration.Show(
                    img,
                    Program.Settings.PrintSettings,
                    owner: MainWindowIntegration.Instance);
            }
        }

        public static Bitmap ApplyImageEffects(Bitmap bmp, TaskSettingsImage taskSettingsImage)
        {
            if (bmp != null)
            {
                bmp = ImageHelpers.NonIndexedBitmap(bmp);

                if (taskSettingsImage.ShowImageEffectsWindowAfterCapture)
                {
                    ImageEffectsDialogResult result = ImageEffectsIntegration.ShowDialog(bmp,
                        taskSettingsImage.ImageEffectPresets, taskSettingsImage.SelectedImageEffectPreset,
                        ImageEffectsWindowMode.Editor);
                    taskSettingsImage.SelectedImageEffectPreset = result.SelectedPresetIndex;
                }

                ImageEffectPreset imageEffect = null;

                if (taskSettingsImage.UseRandomImageEffect)
                {
                    imageEffect = RandomFast.Pick(taskSettingsImage.ImageEffectPresets);
                }
                else if (taskSettingsImage.ImageEffectPresets.IsValidIndex(taskSettingsImage.SelectedImageEffectPreset))
                {
                    imageEffect = taskSettingsImage.ImageEffectPresets[taskSettingsImage.SelectedImageEffectPreset];
                }

                if (imageEffect != null)
                {
                    using (bmp)
                    {
                        return imageEffect.ApplyEffects(bmp);
                    }
                }
            }

            return bmp;
        }

        public static void AddDefaultExternalPrograms(TaskSettings taskSettings)
        {
            if (taskSettings.ExternalPrograms == null)
            {
                taskSettings.ExternalPrograms = new List<ExternalProgram>();
            }

            AddExternalProgramFromRegistry(taskSettings, "Paint", "mspaint.exe");
            AddExternalProgramFromRegistry(taskSettings, "Paint.NET", "PaintDotNet.exe");
            AddExternalProgramFromRegistry(taskSettings, "Adobe Photoshop", "Photoshop.exe");
            AddExternalProgramFromRegistry(taskSettings, "IrfanView", "i_view32.exe");
            AddExternalProgramFromRegistry(taskSettings, "XnView", "xnview.exe");
        }

        private static void AddExternalProgramFromRegistry(TaskSettings taskSettings, string name, string fileName)
        {
            if (!taskSettings.ExternalPrograms.Exists(x => x.Name == name))
            {
                try
                {
                    string filePath = RegistryHelpers.SearchProgramPath(fileName);

                    if (!string.IsNullOrEmpty(filePath))
                    {
                        ExternalProgram externalProgram = new ExternalProgram(name, filePath);
                        taskSettings.ExternalPrograms.Add(externalProgram);
                    }
                }
                catch (Exception e)
                {
                    DebugHelper.WriteException(e);
                }
            }
        }

        public static string HandleExistsFile(string folder, string fileName, TaskSettings taskSettings)
        {
            string filePath = Path.Combine(folder, fileName);
            return HandleExistsFile(filePath, taskSettings);
        }

        public static string HandleExistsFile(string filePath, TaskSettings taskSettings)
        {
            if (File.Exists(filePath))
            {
                switch (taskSettings.ImageSettings.FileExistAction)
                {
                    case FileExistAction.Ask:
                        filePath = FileExistWindowIntegration.Show(filePath);
                        break;
                    case FileExistAction.UniqueName:
                        filePath = FileHelpers.GetUniqueFilePath(filePath);
                        break;
                    case FileExistAction.Cancel:
                        filePath = "";
                        break;
                }
            }

            return filePath;
        }

        public static async Task OpenScrollingCapture(TaskSettings taskSettings = null)
        {
            if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

            await ScrollingCaptureWindowIntegration.StartStopAsync(taskSettings.CaptureSettingsReference.ScrollingCaptureOptions,
                img => ImageTaskManager.RunImageTask(img, taskSettings),
                () => PlayNotificationSoundAsync(NotificationSound.ActionCompleted, taskSettings));
        }

        public static void OpenAutoCapture(TaskSettings taskSettings = null)
        {
            if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

            AutoCaptureWindowIntegration.Show(taskSettings);
        }

        public static void StartAutoCapture(TaskSettings taskSettings = null)
        {
            if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

            AutoCaptureWindowIntegration.Start(taskSettings);
        }

        public static void StopAutoCapture()
        {
            AutoCaptureWindowIntegration.Stop();
        }

        public static void OpenScreenshotsFolder()
        {
            string screenshotsFolder = GetScreenshotsFolder();

            if (Directory.Exists(screenshotsFolder))
            {
                FileHelpers.OpenFolder(screenshotsFolder);
            }
            else
            {
                FileHelpers.OpenFolder(Program.ScreenshotsParentFolder);
            }
        }

        public static void OpenDebugLog()
        {
            DebugLogWindowIntegration.Show(DebugHelper.Logger);
        }

        public static void AnnotateImageFromFile(string filePath, TaskSettings taskSettings = null)
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

                Bitmap bmp = ImageHelpers.LoadImage(filePath);

                AnnotateImageAsync(bmp, filePath, taskSettings);
            }
            else
            {
                MessageBox.Show("File does not exist:" + Environment.NewLine + filePath, "ShareX", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public static void AnnotateImageAsync(Bitmap bmp, string filePath, TaskSettings taskSettings)
        {
            ThreadWorker worker = new ThreadWorker();

            worker.DoWork += () =>
            {
                bmp = AnnotateImage(bmp, filePath, taskSettings);
            };

            worker.Completed += () =>
            {
                if (bmp != null)
                {
                    ImageTaskManager.RunImageTask(bmp, taskSettings);
                }
            };

            worker.Start(ApartmentState.STA);
        }

        public static Bitmap AnnotateImage(Bitmap bmp, string filePath, TaskSettings taskSettings, bool taskMode = false)
        {
            if (bmp != null)
            {
                bmp = ImageHelpers.NonIndexedBitmap(bmp);

                using (bmp)
                {
                    RegionCaptureMode mode = taskMode ? RegionCaptureMode.TaskEditor : RegionCaptureMode.Editor;
                    RegionCaptureOptions options = taskSettings.CaptureSettingsReference.SurfaceOptions;

                    using (RegionCaptureForm form = new RegionCaptureForm(mode, options, bmp))
                    {
                        form.ImageFilePath = filePath;

                        form.SaveImageRequested += (output, newFilePath) =>
                        {
                            using (output)
                            {
                                if (string.IsNullOrEmpty(newFilePath))
                                {
                                    string screenshotsFolder = GetScreenshotsFolder(taskSettings);
                                    string fileName = GetFileName(taskSettings, taskSettings.ImageSettings.ImageFormat.GetDescription(), output);
                                    newFilePath = Path.Combine(screenshotsFolder, fileName);
                                }

                                ImageHelpers.SaveImage(output, newFilePath);
                            }

                            return newFilePath;
                        };

                        form.SaveImageAsRequested += (output, newFilePath) =>
                        {
                            using (output)
                            {
                                if (string.IsNullOrEmpty(newFilePath))
                                {
                                    string screenshotsFolder = GetScreenshotsFolder(taskSettings);
                                    string fileName = GetFileName(taskSettings, taskSettings.ImageSettings.ImageFormat.GetDescription(), output);
                                    newFilePath = Path.Combine(screenshotsFolder, fileName);
                                }

                                newFilePath = ImageHelpers.SaveImageFileDialog(output, newFilePath);
                            }

                            return newFilePath;
                        };

                        form.CopyImageRequested += MainFormCopyImage;
                        form.PrintImageRequested += MainFormPrintImage;
                        form.ShowDialog();

                        switch (form.Result)
                        {
                            case RegionResult.Close: // Esc
                            case RegionResult.AnnotateCancelTask:
                                return null;
                            case RegionResult.Region: // Enter
                            case RegionResult.AnnotateRunAfterCaptureTasks:
                                return form.GetResultImage();
                            case RegionResult.Fullscreen: // Space or right click
                            case RegionResult.AnnotateContinueTask:
                                return (Bitmap)form.Canvas.Clone();
                        }
                    }
                }
            }

            return null;
        }

        public static void MainFormCopyImage(Bitmap bmp)
        {
            Program.MainForm.InvokeSafe(() =>
            {
                ClipboardHelpers.CopyImage(bmp);
            });
        }

        public static void MainFormPrintImage(Bitmap bmp)
        {
            Program.MainForm.InvokeSafe(() =>
            {
                using (bmp)
                {
                    PrintImage(bmp);
                }
            });
        }

        public static void OpenImageEffects(TaskSettings taskSettings = null)
        {
            string filePath = ImageHelpers.OpenImageFileDialog();

            OpenImageEffects(filePath, taskSettings);
        }

        public static void OpenImageEffects(string filePath, TaskSettings taskSettings = null)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                Bitmap bmp = ImageHelpers.LoadImage(filePath);

                if (bmp != null)
                {
                    bmp = ImageHelpers.NonIndexedBitmap(bmp);

                    if (taskSettings == null) taskSettings = Program.DefaultTaskSettings;

                    using (bmp)
                    {
                        ImageEffectsIntegration.ShowToolWindow(bmp,
                            taskSettings.ImageSettingsReference.ImageEffectPresets,
                            taskSettings.ImageSettings.SelectedImageEffectPreset,
                            CreateImageEffectsCallbacks(taskSettings), filePath,
                            selectedIndex => taskSettings.ImageSettingsReference.SelectedImageEffectPreset = selectedIndex);
                    }
                }
            }
        }

        public static void OpenImageEffectsSingleton(TaskSettings taskSettings = null, string importJson = null)
        {
            if (taskSettings == null) taskSettings = Program.DefaultTaskSettings;

            ImageEffectsIntegration.ShowPresetWindow(taskSettings.ImageSettings.ImageEffectPresets,
                taskSettings.ImageSettings.SelectedImageEffectPreset,
                selectedIndex => taskSettings.ImageSettings.SelectedImageEffectPreset = selectedIndex,
                importJson, CreateImageEffectsCallbacks(taskSettings));
        }

        private static ImageEffectsCallbacks CreateImageEffectsCallbacks(TaskSettings taskSettings)
        {
            return new ImageEffectsCallbacks
            {
                LoadImageFromFile = () =>
                {
                    string path = ImageHelpers.OpenImageFileDialog();
                    Bitmap image = !string.IsNullOrWhiteSpace(path) ? ImageHelpers.LoadImage(path) : null;
                    return image != null ? new ImageEffectsSource(image, path) : null;
                },
                LoadImageFromClipboard = () =>
                {
                    Bitmap image = ClipboardHelpers.GetImage();
                    return image != null ? new ImageEffectsSource(image) : null;
                },
                SaveImage = (image, path) => ImageHelpers.SaveImageFileDialog(image, path),
                UploadImage = image => ImageTaskManager.RunImageTask(image, taskSettings),
                OpenImageEffectsPage = () => URLHelpers.OpenURL(Links.ImageEffects)
            };
        }

        public static void RunShareXAsAdmin(string arguments = null)
        {
            try
            {
                string exePath = Application.ExecutablePath;

                string cmdArgs = $"/c timeout /t 1 & powershell -Command \"Start-Process '{exePath}' -Verb runAs";

                if (!string.IsNullOrEmpty(arguments))
                {
                    cmdArgs += $" -ArgumentList '{arguments}'";
                }

                cmdArgs += "\"";

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = cmdArgs,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch
            {
            }
        }

        public static bool ToggleHotkeys(TaskSettings taskSettings = null)
        {
            bool disableHotkeys = !Program.Settings.DisableHotkeys;
            ToggleHotkeys(disableHotkeys, taskSettings);
            return disableHotkeys;
        }

        public static void ToggleHotkeys(bool disableHotkeys, TaskSettings taskSettings = null)
        {
            if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

            Program.Settings.DisableHotkeys = disableHotkeys;
            Program.HotkeyManager.ToggleHotkeys(disableHotkeys);
            MainWindowIntegration.RefreshMenus();

            PlayNotificationSoundAsync(NotificationSound.ActionCompleted, taskSettings);

            if (taskSettings.GeneralSettings.ShowToastNotificationAfterTaskCompleted)
            {
                ShowNotificationTip(disableHotkeys ? Resources.TaskHelpers_ToggleHotkeys_Hotkeys_disabled_ : Resources.TaskHelpers_ToggleHotkeys_Hotkeys_enabled_);
            }
        }

        public static void PlayNotificationSoundAsync(NotificationSound notificationSound, TaskSettings taskSettings = null)
        {
            if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

            switch (notificationSound)
            {
                case NotificationSound.Capture:
                    if (taskSettings.GeneralSettings.PlaySoundAfterCapture)
                    {
                        if (taskSettings.GeneralSettings.UseCustomCaptureSound && !string.IsNullOrEmpty(taskSettings.GeneralSettings.CustomCaptureSoundPath))
                        {
                            Helpers.PlaySoundAsync(taskSettings.GeneralSettings.CustomCaptureSoundPath);
                        }
                        else
                        {
                            Helpers.PlaySoundAsync(Resources.CaptureSound);
                        }
                    }
                    break;
                case NotificationSound.TaskCompleted:
                    if (taskSettings.GeneralSettings.PlaySoundAfterCapture)
                    {
                        if (taskSettings.GeneralSettings.UseCustomTaskCompletedSound && !string.IsNullOrEmpty(taskSettings.GeneralSettings.CustomTaskCompletedSoundPath))
                        {
                            Helpers.PlaySoundAsync(taskSettings.GeneralSettings.CustomTaskCompletedSoundPath);
                        }
                        else
                        {
                            Helpers.PlaySoundAsync(Resources.TaskCompletedSound);
                        }
                    }
                    break;
                case NotificationSound.ActionCompleted:
                    if (taskSettings.GeneralSettings.PlaySoundAfterAction)
                    {
                        if (taskSettings.GeneralSettings.UseCustomActionCompletedSound && !string.IsNullOrEmpty(taskSettings.GeneralSettings.CustomActionCompletedSoundPath))
                        {
                            Helpers.PlaySoundAsync(taskSettings.GeneralSettings.CustomActionCompletedSoundPath);
                        }
                        else
                        {
                            Helpers.PlaySoundAsync(Resources.ActionCompletedSound);
                        }
                    }
                    break;
                case NotificationSound.Error:
                    if (taskSettings.GeneralSettings.PlaySoundAfterCapture)
                    {
                        if (taskSettings.GeneralSettings.UseCustomErrorSound && !string.IsNullOrEmpty(taskSettings.GeneralSettings.CustomErrorSoundPath))
                        {
                            Helpers.PlaySoundAsync(taskSettings.GeneralSettings.CustomErrorSoundPath);
                        }
                        else
                        {
                            Helpers.PlaySoundAsync(Resources.ErrorSound);
                        }
                    }
                    break;
            }
        }

        public static string FindMenuLucideIcon(HotkeyType hotkeyType)
        {
            return hotkeyType switch
            {
                HotkeyType.None => LucideIcons.circle_dashed,

                // Upload

                // Screen capture
                HotkeyType.PrintScreen => LucideIcons.monitor,
                HotkeyType.ActiveWindow => LucideIcons.app_window,
                HotkeyType.CustomWindow => LucideIcons.scan,
                HotkeyType.ActiveMonitor => LucideIcons.monitor,
                HotkeyType.RectangleRegion => LucideIcons.scan,
                HotkeyType.RectangleLight => LucideIcons.square,
                HotkeyType.RectangleTransparent => LucideIcons.square_dashed,
                HotkeyType.CustomRegion => LucideIcons.scan_line,
                HotkeyType.LastRegion => LucideIcons.layers,
                HotkeyType.ScrollingCapture => LucideIcons.scroll_text,
                HotkeyType.AutoCapture => LucideIcons.clock,
                HotkeyType.StartAutoCapture => LucideIcons.circle_play,
                HotkeyType.StopAutoCapture => LucideIcons.timer_off,

                // Screen record

                // Tools

                // Other
                HotkeyType.DisableHotkeys => LucideIcons.keyboard_off,
                HotkeyType.OpenMainWindow => LucideIcons.panel_top_open,
                HotkeyType.OpenScreenshotsFolder => LucideIcons.folder_open,
                HotkeyType.ToggleActionsToolbar => LucideIcons.panel_top,
                HotkeyType.ToggleTrayMenu => LucideIcons.menu,
                HotkeyType.ExitShareX => LucideIcons.log_out,
                _ => LucideIcons.circle
            };
        }

        public static Image FindMenuIcon<T>(T value) where T : Enum
        {
            if (value is AfterCaptureTasks afterCaptureTask)
            {
                switch (afterCaptureTask)
                {
                    default: throw new Exception("Icon missing for after capture task: " + afterCaptureTask);
                    case AfterCaptureTasks.ShowQuickTaskMenu: return Resources.ui_menu_blue;
                    case AfterCaptureTasks.ShowAfterCaptureWindow: return Resources.application_text_image;
                    case AfterCaptureTasks.AddImageEffects: return Resources.image_saturation;
                    case AfterCaptureTasks.AnnotateImage: return Resources.image_pencil;
                    case AfterCaptureTasks.CopyImageToClipboard: return Resources.clipboard_paste_image;
                    case AfterCaptureTasks.SendImageToPrinter: return Resources.printer;
                    case AfterCaptureTasks.SaveImageToFile: return Resources.disk;
                    case AfterCaptureTasks.SaveImageToFileWithDialog: return Resources.disk_rename;
                    case AfterCaptureTasks.SaveThumbnailImageToFile: return Resources.disk_small;
                    case AfterCaptureTasks.PerformActions: return Resources.application_terminal;
                    case AfterCaptureTasks.CopyFileToClipboard: return Resources.clipboard_block;
                    case AfterCaptureTasks.CopyFilePathToClipboard: return Resources.clipboard_list;
                    case AfterCaptureTasks.CopyFolderPathToClipboard: return Resources.folder_bookmark;
                    case AfterCaptureTasks.ShowInExplorer: return Resources.folder_stand;
                    case AfterCaptureTasks.DeleteFile: return Resources.bin;
                }
            }
            else if (value is HotkeyType hotkeyType)
            {
                switch (hotkeyType)
                {
                    default: throw new Exception("Icon missing for hotkey type: " + hotkeyType);
                    case HotkeyType.None: return null;
                    // Upload
                    // Screen capture
                    case HotkeyType.PrintScreen: return Resources.layer_fullscreen;
                    case HotkeyType.ActiveWindow: return Resources.application_blue;
                    case HotkeyType.ActiveMonitor: return Resources.monitor;
                    case HotkeyType.RectangleRegion: return Resources.layer_shape;
                    case HotkeyType.RectangleLight: return Resources.Rectangle;
                    case HotkeyType.RectangleTransparent: return Resources.layer_transparent;
                    case HotkeyType.CustomRegion: return Resources.layer__arrow;
                    case HotkeyType.CustomWindow: return Resources.application__arrow;
                    case HotkeyType.LastRegion: return Resources.layers;
                    case HotkeyType.ScrollingCapture: return Resources.ui_scroll_pane_image;
                    case HotkeyType.AutoCapture: return Resources.clock;
                    case HotkeyType.StartAutoCapture: return Resources.clock__arrow;
                    case HotkeyType.StopAutoCapture: return Resources.clock__minus;
                    // Screen record
                    // Tools
                    // Other
                    case HotkeyType.DisableHotkeys: return Resources.keyboard__minus;
                    case HotkeyType.OpenMainWindow: return Resources.application_home;
                    case HotkeyType.OpenScreenshotsFolder: return Resources.folder_open_image;
                    case HotkeyType.ToggleActionsToolbar: return Resources.ui_toolbar__arrow;
                    case HotkeyType.ToggleTrayMenu: return Resources.ui_menu_blue;
                    case HotkeyType.ExitShareX: return Resources.cross;
                }
            }

            return null;
        }

        public static Image FindMenuIcon<T>(int index) where T : Enum
        {
            T value = Helpers.GetEnumFromIndex<T>(index);
            return FindMenuIcon(value);
        }

        public static Screenshot GetScreenshot(TaskSettings taskSettings = null)
        {
            if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

            Screenshot screenshot = new Screenshot()
            {
                CaptureCursor = taskSettings.CaptureSettings.ShowCursor,
                CaptureClientArea = taskSettings.CaptureSettings.CaptureClientArea,
                RemoveOutsideScreenArea = true,
                CaptureShadow = taskSettings.CaptureSettings.CaptureShadow,
                ShadowOffset = taskSettings.CaptureSettings.CaptureShadowOffset,
                AutoHideTaskbar = taskSettings.CaptureSettings.CaptureAutoHideTaskbar,
                HDRScreenshotColorCorrection = taskSettings.CaptureSettings.HDRScreenshotColorCorrection
            };

            return screenshot;
        }

        public static void ImportImageEffect(string filePath)
        {
            string configJson = null;

            try
            {
                configJson = ImageEffectPackager.ExtractPackage(filePath, Program.ImageEffectsFolder);
            }
            catch (Exception ex)
            {
                ex.ShowError(false);
            }

            if (!string.IsNullOrEmpty(configJson))
            {
                OpenImageEffectsSingleton(Program.DefaultTaskSettings, configJson);

                if (!Program.DefaultTaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.AddImageEffects) &&
                    MessageBox.Show(Resources.WouldYouLikeToEnableImageEffects,
                    "ShareX", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Program.DefaultTaskSettings.AfterCaptureJob = Program.DefaultTaskSettings.AfterCaptureJob.Add(AfterCaptureTasks.AddImageEffects);
                    MainWindowIntegration.RefreshMenus();
                }
            }
        }

        public static void OpenActionsToolbar()
        {
            ActionsToolbarWindowIntegration.Show();
        }

        public static void ToggleActionsToolbar()
        {
            ActionsToolbarWindowIntegration.Toggle();
        }

        public static void ShowNotificationTip(string text, string title = "ShareX", int duration = -1)
        {
            if (duration < 0)
            {
                duration = (int)(Program.DefaultTaskSettings.GeneralSettings.ToastWindowDuration * 1000);
            }

            NotificationWindowConfig toastConfig = new NotificationWindowConfig()
            {
                Duration = duration,
                FadeDuration = (int)(Program.DefaultTaskSettings.GeneralSettings.ToastWindowFadeDuration * 1000),
                Placement = Program.DefaultTaskSettings.GeneralSettings.ToastWindowPlacement,
                Size = Program.DefaultTaskSettings.GeneralSettings.ToastWindowSize,
                ActionButtons = NotificationActionButton.CloneButtons(Program.DefaultTaskSettings.GeneralSettings.ToastWindowButtons),
                Title = title,
                Text = text
            };

            Program.MainForm.InvokeSafe(() =>
            {
                NotificationWindow.Show(toastConfig);
            });
        }

        public static void ToggleTrayMenu()
        {
            MainWindowIntegration.ShowTrayMenu();
        }

    }
}
