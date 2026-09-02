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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace ShareX
{
    /// <summary>
    /// Commands the main window runs against the tasks the user selected.
    /// </summary>
    public class TaskInfoManager
    {
        public TaskInfoStatus[] SelectedItems { get; private set; }

        public TaskInfoStatus SelectedItem
        {
            get
            {
                if (IsItemSelected)
                {
                    return SelectedItems[0];
                }

                return null;
            }
        }

        public bool IsItemSelected => SelectedItems != null && SelectedItems.Length > 0;

        public void UpdateSelectedItems(IEnumerable<WorkerTask> tasks)
        {
            if (tasks != null && tasks.Count() > 0)
            {
                SelectedItems = tasks.Where(x => x != null && x.Info != null).Select(x => new TaskInfoStatus(x)).ToArray();
            }
            else
            {
                SelectedItems = null;
            }
        }

        private void CopyTexts(IEnumerable<string> texts)
        {
            if (texts != null && texts.Count() > 0)
            {
                string joined = string.Join("\r\n", texts.ToArray());

                if (!string.IsNullOrEmpty(joined))
                {
                    if (ClipboardHelpers.CopyText(joined))
                    {
                        TaskHelpers.PlayNotificationSoundAsync(NotificationSound.ActionCompleted);
                    }
                }
            }
        }

        #region Open

        public void OpenFile()
        {
            if (IsItemSelected && SelectedItem.IsFileExist) FileHelpers.OpenFile(SelectedItem.Info.FilePath);
        }

        public void OpenThumbnailFile()
        {
            if (IsItemSelected && SelectedItem.IsThumbnailFileExist) FileHelpers.OpenFile(SelectedItem.Info.ThumbnailFilePath);
        }

        public void OpenFolder()
        {
            if (IsItemSelected && SelectedItem.IsFileExist) FileHelpers.OpenFolderWithFile(SelectedItem.Info.FilePath);
        }

        public void TryOpen()
        {
            if (IsItemSelected)
            {
                if (SelectedItem.IsFileExist)
                {
                    FileHelpers.OpenFile(SelectedItem.Info.FilePath);
                }
                else if (SelectedItem.IsThumbnailFileExist)
                {
                    FileHelpers.OpenFile(SelectedItem.Info.ThumbnailFilePath);
                }
            }
        }

        #endregion Open

        #region Copy

        public void CopyFile()
        {
            if (IsItemSelected && SelectedItem.IsFileExist)
            {
                if (ClipboardHelpers.CopyFile(SelectedItem.Info.FilePath))
                {
                    TaskHelpers.PlayNotificationSoundAsync(NotificationSound.ActionCompleted);
                }
            }
        }

        public void CopyImage()
        {
            if (IsItemSelected && SelectedItem.IsImageFile)
            {
                if (ClipboardHelpers.CopyImageFromFile(SelectedItem.Info.FilePath))
                {
                    TaskHelpers.PlayNotificationSoundAsync(NotificationSound.ActionCompleted);
                }
            }
        }

        public void CopyImageDimensions()
        {
            if (IsItemSelected && SelectedItem.IsImageFile)
            {
                Size size = ImageHelpers.GetImageFileDimensions(SelectedItem.Info.FilePath);

                if (!size.IsEmpty)
                {
                    if (ClipboardHelpers.CopyText($"{size.Width} x {size.Height}"))
                    {
                        TaskHelpers.PlayNotificationSoundAsync(NotificationSound.ActionCompleted);
                    }
                }
            }
        }

        public void CopyText()
        {
            if (IsItemSelected && SelectedItem.IsTextFile)
            {
                if (ClipboardHelpers.CopyTextFromFile(SelectedItem.Info.FilePath))
                {
                    TaskHelpers.PlayNotificationSoundAsync(NotificationSound.ActionCompleted);
                }
            }
        }

        public void CopyThumbnailFile()
        {
            if (IsItemSelected && SelectedItem.IsThumbnailFileExist)
            {
                if (ClipboardHelpers.CopyFile(SelectedItem.Info.ThumbnailFilePath))
                {
                    TaskHelpers.PlayNotificationSoundAsync(NotificationSound.ActionCompleted);
                }
            }
        }

        public void CopyThumbnailImage()
        {
            if (IsItemSelected && SelectedItem.IsThumbnailFileExist)
            {
                if (ClipboardHelpers.CopyImageFromFile(SelectedItem.Info.ThumbnailFilePath))
                {
                    TaskHelpers.PlayNotificationSoundAsync(NotificationSound.ActionCompleted);
                }
            }
        }

        public void CopyFilePath()
        {
            if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsFilePathValid).Select(x => x.Info.FilePath));
        }

        public void CopyFileName()
        {
            if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsFilePathValid).Select(x => Path.GetFileNameWithoutExtension(x.Info.FilePath)));
        }

        public void CopyFileNameWithExtension()
        {
            if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsFilePathValid).Select(x => Path.GetFileName(x.Info.FilePath)));
        }

        public void CopyFolder()
        {
            if (IsItemSelected) CopyTexts(SelectedItems.Where(x => x.IsFilePathValid).Select(x => Path.GetDirectoryName(x.Info.FilePath)));
        }

        public void TryCopy()
        {
            if (IsItemSelected)
            {
                if (SelectedItem.IsImageFile)
                {
                    CopyImage();
                }
                else if (SelectedItem.IsTextFile)
                {
                    CopyText();
                }
                else if (SelectedItem.IsFileExist)
                {
                    CopyFile();
                }
            }
        }

        #endregion Copy

        #region Other

        public void ShowImagePreview()
        {
            if (IsItemSelected && SelectedItem.IsImageFile) ImageViewer.ShowImage(SelectedItem.Info.FilePath);
        }

        public void ShowErrors()
        {
            if (IsItemSelected)
            {
                SelectedItem.Task.ShowErrorWindow();
            }
        }

        public void StopTask()
        {
            if (IsItemSelected)
            {
                foreach (WorkerTask task in SelectedItems.Select(x => x.Task))
                {
                    task?.Stop();
                }
            }
        }

        public void EditImage()
        {
            if (IsItemSelected && SelectedItem.IsImageFile) TaskHelpers.AnnotateImageFromFile(SelectedItem.Info.FilePath);
        }

        public void AddImageEffects()
        {
            if (IsItemSelected && SelectedItem.IsImageFile) TaskHelpers.OpenImageEffects(SelectedItem.Info.FilePath);
        }

        public void DeleteFiles()
        {
            if (IsItemSelected)
            {
                foreach (string filePath in SelectedItems.Select(x => x.Info.FilePath))
                {
                    FileHelpers.DeleteFile(filePath, true);
                }
            }
        }

        #endregion Other
    }
}
