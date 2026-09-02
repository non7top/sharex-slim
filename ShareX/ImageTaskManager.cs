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
using System.Drawing;

namespace ShareX
{
    /// <summary>
    /// Entry point for the capture pipeline: takes a freshly captured image and
    /// runs it through the after-capture tasks (annotate, save, clipboard, ...).
    /// </summary>
    public static class ImageTaskManager
    {
        public static void RunImageTask(Bitmap bmp, TaskSettings taskSettings, bool skipQuickTaskMenu = false, bool skipAfterCaptureWindow = false)
        {
            TaskMetadata metadata = new TaskMetadata(bmp);
            RunImageTask(metadata, taskSettings, skipQuickTaskMenu, skipAfterCaptureWindow);
        }

        public static void RunImageTask(TaskMetadata metadata, TaskSettings taskSettings, bool skipQuickTaskMenu = false, bool skipAfterCaptureWindow = false)
        {
            if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

            if (metadata != null && metadata.Image != null && taskSettings != null)
            {
                if (!skipQuickTaskMenu && taskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.ShowQuickTaskMenu))
                {
                    QuickTaskMenu quickTaskMenu = new QuickTaskMenu();

                    quickTaskMenu.TaskInfoSelected += taskInfo =>
                    {
                        if (taskInfo == null)
                        {
                            RunImageTask(metadata, taskSettings, true);
                        }
                        else if (taskInfo.IsValid)
                        {
                            taskSettings.AfterCaptureJob = taskInfo.AfterCaptureTasks;
                            RunImageTask(metadata, taskSettings, true);
                        }
                    };

                    quickTaskMenu.ShowMenu();

                    return;
                }

                void StartImageTask(string customFileName)
                {
                    WorkerTask task = WorkerTask.CreateImageTask(metadata, taskSettings, customFileName);
                    TaskManager.Start(task);
                }

                if (!skipAfterCaptureWindow)
                {
                    TaskHelpers.ShowAfterCaptureWindow(taskSettings, result =>
                    {
                        if (result.Accepted)
                        {
                            StartImageTask(result.FileName);
                        }
                    }, metadata);

                    return;
                }

                StartImageTask(null);
            }
        }
    }
}
