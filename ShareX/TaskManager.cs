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
using ShareX.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ShareX
{
    public static class TaskManager
    {
        public static event Action<WorkerTask> TaskAdded;
        public static event Action<WorkerTask> TaskRemoved;
        public static event Action<WorkerTask> TaskChanged;
        public static event Action<WorkerTask, Bitmap> TaskImageReady;
        public static event Action TaskCollectionChanged;

        public static List<WorkerTask> Tasks { get; } = new List<WorkerTask>();
        public static RecentTaskManager RecentManager { get; } = new RecentTaskManager();
        public static bool IsBusy => Tasks.Count > 0 && Tasks.Any(task => task.IsBusy);

        private static int lastIconStatus = -1;

        public static void Start(WorkerTask task)
        {
            if (task != null)
            {
                Tasks.Add(task);
                UpdateMainFormTip();

                task.StatusChanged += Task_StatusChanged;
                task.ImageReady += Task_ImageReady;
                task.TaskCompleted += Task_TaskCompleted;

                TaskAdded?.Invoke(task);
                TaskCollectionChanged?.Invoke();

                StartTasks();
            }
        }

        public static void Remove(WorkerTask task)
        {
            if (task != null)
            {
                task.Stop();
                Tasks.Remove(task);
                UpdateMainFormTip();

                TaskRemoved?.Invoke(task);
                TaskCollectionChanged?.Invoke();

                task.Dispose();
            }
        }

        private static void StartTasks()
        {
            int workingTasksCount = Tasks.Count(x => x.IsWorking);
            WorkerTask[] inQueueTasks = Tasks.Where(x => x.Status == TaskStatus.InQueue).ToArray();

            if (inQueueTasks.Length > 0)
            {
                int len;

                if (Program.Settings.ConcurrentTaskLimit == 0)
                {
                    len = inQueueTasks.Length;
                }
                else
                {
                    len = (Program.Settings.ConcurrentTaskLimit - workingTasksCount).Clamp(0, inQueueTasks.Length);
                }

                for (int i = 0; i < len; i++)
                {
                    inQueueTasks[i].Start();
                }
            }
        }

        public static void StopAllTasks()
        {
            foreach (WorkerTask task in Tasks)
            {
                if (task != null) task.Stop();
            }
        }

        public static void UpdateMainFormTip()
        {
            TaskCollectionChanged?.Invoke();
        }

        private static void Task_StatusChanged(WorkerTask task)
        {
            DebugHelper.WriteLine("Task status: " + task.Status);

            UpdateProgressUI();
            TaskChanged?.Invoke(task);
        }

        private static void Task_ImageReady(WorkerTask task, Bitmap image)
        {
            TaskChanged?.Invoke(task);
            TaskImageReady?.Invoke(task, image);
        }

        private static void Task_TaskCompleted(WorkerTask task)
        {
            try
            {
                if (task != null)
                {
                    task.KeepImage = false;

                    TaskInfo info = task.Info;

                    if (info != null)
                    {
                        string result = info.ToString();

                        if (!string.IsNullOrEmpty(result))
                        {
                            RecentManager.Add(task);
                        }

                        if (task.Status == TaskStatus.Stopped)
                        {
                            DebugHelper.WriteLine($"Task stopped. File name: {info.FileName}");
                        }
                        else if (task.Status == TaskStatus.Failed)
                        {
                            string errors = info.ErrorsToString();

                            DebugHelper.WriteLine($"Task failed. File name: {info.FileName}, Errors:\r\n{errors}");

                            TaskHelpers.PlayNotificationSoundAsync(NotificationSound.Error, info.TaskSettings);

                            if (info.TaskSettings.GeneralSettings.ShowToastNotificationAfterTaskCompleted && !string.IsNullOrEmpty(errors) &&
                                (!info.TaskSettings.GeneralSettings.DisableNotificationsOnFullscreen || !CaptureHelpers.IsActiveWindowFullscreen()))
                            {
                                TaskHelpers.ShowNotificationTip(errors, Program.Title + " - " + Resources.TaskManager_task_UploadCompleted_Error, 5000);
                            }
                        }
                        else
                        {
                            DebugHelper.WriteLine($"Task completed. File name: {info.FileName}, Duration: {(long)info.TaskDuration.TotalMilliseconds} ms");

                            if (!task.StopRequested && !string.IsNullOrEmpty(result))
                            {
                                TaskHelpers.PlayNotificationSoundAsync(NotificationSound.TaskCompleted, info.TaskSettings);

                                if (info.TaskSettings.GeneralSettings.ShowToastNotificationAfterTaskCompleted &&
                                    (!info.TaskSettings.GeneralSettings.DisableNotificationsOnFullscreen || !CaptureHelpers.IsActiveWindowFullscreen()))
                                {
                                    task.KeepImage = true;

                                    NotificationWindowConfig toastConfig = new NotificationWindowConfig()
                                    {
                                        Duration = (int)(info.TaskSettings.GeneralSettings.ToastWindowDuration * 1000),
                                        FadeDuration = (int)(info.TaskSettings.GeneralSettings.ToastWindowFadeDuration * 1000),
                                        Placement = info.TaskSettings.GeneralSettings.ToastWindowPlacement,
                                        Size = info.TaskSettings.GeneralSettings.ToastWindowSize,
                                        LeftClickAction = info.TaskSettings.GeneralSettings.ToastWindowLeftClickAction,
                                        RightClickAction = info.TaskSettings.GeneralSettings.ToastWindowRightClickAction,
                                        MiddleClickAction = info.TaskSettings.GeneralSettings.ToastWindowMiddleClickAction,
                                        ActionButtons = NotificationActionButton.CloneButtons(info.TaskSettings.GeneralSettings.ToastWindowButtons),
                                        FilePath = info.FilePath,
                                        Image = task.Image,
                                        Title = Program.Title + " - " + Resources.TaskManager_task_UploadCompleted_ShareX___Task_completed,
                                        Text = result
                                    };

                                    NotificationWindow.Show(toastConfig);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                TaskChanged?.Invoke(task);

                if (!IsBusy && Program.CLI.IsCommandExist("AutoClose"))
                {
                    Application.Exit();
                }
                else
                {
                    StartTasks();
                    UpdateProgressUI();

                    if (Program.Settings.SaveSettingsAfterTaskCompleted && !IsBusy)
                    {
                        SettingManager.SaveAllSettingsAsync();
                    }
                }
            }
        }

        public static void UpdateProgressUI()
        {
            MainWindowIntegration.SetTitle(Program.Title);
            UpdateTrayIcon();
        }

        public static void UpdateTrayIcon()
        {
            if (Program.Settings.ShowTray && lastIconStatus != 0)
            {
                Icon icon = ShareXResources.Icon;

                MainWindowIntegration.SetTrayIcon(icon);
                icon.Dispose();

                lastIconStatus = 0;
            }
        }
    }
}
