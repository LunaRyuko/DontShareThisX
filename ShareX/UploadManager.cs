#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2022 ShareX Team

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
using ShareX.IndexerLib;
using ShareX.Properties;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace ShareX
{
    public static class UploadManager
    {
        public static void UploadFile(string filePath, TaskSettings taskSettings = null)
        {
        }

        public static void UploadFile(string[] files, TaskSettings taskSettings = null)
        {
        }

        private static bool IsUploadConfirmed(int length)
        {
            return false;
        }

        public static void UploadFile(TaskSettings taskSettings = null)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "ShareX - " + Resources.UploadManager_UploadFile_File_upload;

                if (!string.IsNullOrEmpty(Program.Settings.FileUploadDefaultDirectory) && Directory.Exists(Program.Settings.FileUploadDefaultDirectory))
                {
                    ofd.InitialDirectory = Program.Settings.FileUploadDefaultDirectory;
                }
                else
                {
                    ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }

                ofd.Multiselect = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (!string.IsNullOrEmpty(ofd.FileName))
                    {
                        Program.Settings.FileUploadDefaultDirectory = Path.GetDirectoryName(ofd.FileName);
                    }

                    UploadFile(ofd.FileNames, taskSettings);
                }
            }
        }

        public static void UploadFolder(TaskSettings taskSettings = null)
        {
            using (FolderSelectDialog folderDialog = new FolderSelectDialog())
            {
                folderDialog.Title = "ShareX - " + Resources.UploadManager_UploadFolder_Folder_upload;

                if (!string.IsNullOrEmpty(Program.Settings.FileUploadDefaultDirectory) && Directory.Exists(Program.Settings.FileUploadDefaultDirectory))
                {
                    folderDialog.InitialDirectory = Program.Settings.FileUploadDefaultDirectory;
                }
                else
                {
                    folderDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }

                if (folderDialog.ShowDialog() && !string.IsNullOrEmpty(folderDialog.FileName))
                {
                    Program.Settings.FileUploadDefaultDirectory = folderDialog.FileName;
                    UploadFile(folderDialog.FileName, taskSettings);
                }
            }
        }

        public static void ProcessImageUpload(Bitmap bmp, TaskSettings taskSettings)
        {
            if (bmp != null)
            {
                if (!taskSettings.AdvancedSettings.ProcessImagesDuringClipboardUpload)
                {
                    taskSettings.AfterCaptureJob = AfterCaptureTasks.SaveImageToFile;
                }

                RunImageTask(bmp, taskSettings);
            }
        }

        public static void ProcessTextUpload(string text, TaskSettings taskSettings)
        {

        }

        public static void ProcessFilesUpload(string[] files, TaskSettings taskSettings)
        {
            if (files != null && files.Length > 0)
            {
                UploadFile(files, taskSettings);
            }
        }

        public static void ClipboardUpload(TaskSettings taskSettings = null)
        {
            if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

            if (ClipboardHelpers.ContainsImage())
            {
                Bitmap bmp = ClipboardHelpers.GetImage();

                ProcessImageUpload(bmp, taskSettings);
            }
            else if (ClipboardHelpers.ContainsText())
            {
                string text = ClipboardHelpers.GetText();

                ProcessTextUpload(text, taskSettings);
            }
            else if (ClipboardHelpers.ContainsFileDropList())
            {
                string[] files = ClipboardHelpers.GetFileDropList();

                ProcessFilesUpload(files, taskSettings);
            }
        }

        public static void ClipboardUploadWithContentViewer(TaskSettings taskSettings = null)
        {
            if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

            using (ClipboardUploadForm clipboardUploadForm = new ClipboardUploadForm(taskSettings))
            {
                clipboardUploadForm.ShowDialog();
            }
        }

        public static void ClipboardUploadMainWindow(TaskSettings taskSettings = null)
        {
            if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

            if (Program.Settings.ShowClipboardContentViewer)
            {
                using (ClipboardUploadForm clipboardUploadForm = new ClipboardUploadForm(taskSettings, true))
                {
                    clipboardUploadForm.ShowDialog();
                    Program.Settings.ShowClipboardContentViewer = !clipboardUploadForm.DontShowThisWindow;
                }
            }
            else
            {
                ClipboardUpload(taskSettings);
            }
        }

        public static void ShowTextUploadDialog(TaskSettings taskSettings = null)
        {
            if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();
        }

        public static void DragDropUpload(IDataObject data, TaskSettings taskSettings = null)
        {
            if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

            if (data.GetDataPresent(DataFormats.FileDrop, false))
            {
                string[] files = data.GetData(DataFormats.FileDrop, false) as string[];
                UploadFile(files, taskSettings);
            }
            else if (data.GetDataPresent(DataFormats.Bitmap, false))
            {
                Bitmap bmp = data.GetData(DataFormats.Bitmap, false) as Bitmap;
                RunImageTask(bmp, taskSettings);
            }
            else if (data.GetDataPresent(DataFormats.Text, false))
            {
                string text = data.GetData(DataFormats.Text, false) as string;
                UploadText(text, taskSettings, true);
            }
        }

        public static void UploadURL(TaskSettings taskSettings = null)
        {
            if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

            string inputText = null;

            string text = ClipboardHelpers.GetText(true);

            if (URLHelpers.IsValidURL(text))
            {
                inputText = text;
            }

            string url = InputBox.GetInputText("ShareX - " + Resources.UploadManager_UploadURL_URL_to_download_from_and_upload, inputText);

            if (!string.IsNullOrEmpty(url))
            {
                DownloadAndUploadFile(url, taskSettings);
            }
        }

        public static void ShowShortenURLDialog(TaskSettings taskSettings = null)
        {
            if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

            string inputText = null;

            string text = ClipboardHelpers.GetText(true);

            if (URLHelpers.IsValidURL(text))
            {
                inputText = text;
            }

            string url = InputBox.GetInputText("ShareX - " + Resources.UploadManager_ShowShortenURLDialog_ShortenURL, inputText,
                Resources.UploadManager_ShowShortenURLDialog_Shorten);

            if (!string.IsNullOrEmpty(url))
            {
                ShortenURL(url, taskSettings);
            }
        }

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
                            taskSettings.AfterUploadJob = taskInfo.AfterUploadTasks;
                            RunImageTask(metadata, taskSettings, true);
                        }
                    };

                    quickTaskMenu.ShowMenu();

                    return;
                }

                string customFileName = null;

                if (!skipAfterCaptureWindow && !TaskHelpers.ShowAfterCaptureForm(taskSettings, out customFileName, metadata))
                {
                    return;
                }

                WorkerTask task = WorkerTask.CreateImageUploaderTask(metadata, taskSettings, customFileName);
                TaskManager.Start(task);
            }
        }

        public static void UploadImage(Bitmap bmp, TaskSettings taskSettings = null)
        {
            if (bmp != null)
            {
                if (taskSettings == null)
                {
                    taskSettings = TaskSettings.GetDefaultTaskSettings();
                }

                if (taskSettings.IsSafeTaskSettings)
                {
                    taskSettings.UseDefaultAfterCaptureJob = false;
                    taskSettings.AfterCaptureJob = AfterCaptureTasks.SaveImageToFile;
                }

                RunImageTask(bmp, taskSettings);
            }
        }

        public static void UploadText(string text, TaskSettings taskSettings = null, bool allowCustomText = false)
        {
            if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

            if (!string.IsNullOrEmpty(text))
            {
                if (allowCustomText)
                {
                    string input = taskSettings.AdvancedSettings.TextCustom;

                    if (!string.IsNullOrEmpty(input))
                    {
                        if (taskSettings.AdvancedSettings.TextCustomEncodeInput)
                        {
                            text = HttpUtility.HtmlEncode(text);
                        }

                        text = input.Replace("%input", text);
                    }
                }

                WorkerTask task = WorkerTask.CreateTextUploaderTask(text, taskSettings);
                TaskManager.Start(task);
            }
        }

        public static void UploadImageStream(Stream stream, string fileName, TaskSettings taskSettings = null)
        {
            if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

            if (stream != null && stream.Length > 0 && !string.IsNullOrEmpty(fileName))
            {
                WorkerTask task = WorkerTask.CreateDataUploaderTask(EDataType.Image, stream, fileName, taskSettings);
                TaskManager.Start(task);
            }
        }

        public static void ShortenURL(string url, TaskSettings taskSettings = null)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

                WorkerTask task = WorkerTask.CreateURLShortenerTask(url, taskSettings);
                TaskManager.Start(task);
            }
        }

        public static void ShareURL(string url, TaskSettings taskSettings = null)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

                WorkerTask task = WorkerTask.CreateShareURLTask(url, taskSettings);
                TaskManager.Start(task);
            }
        }

        public static void DownloadFile(string url, TaskSettings taskSettings = null)
        {
            DownloadFile(url, false, taskSettings);
        }

        public static void DownloadAndUploadFile(string url, TaskSettings taskSettings = null)
        {
            DownloadFile(url, true, taskSettings);
        }

        private static void DownloadFile(string url, bool upload, TaskSettings taskSettings = null)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

                WorkerTask task = WorkerTask.CreateDownloadTask(url, upload, taskSettings);

                if (task != null)
                {
                    TaskManager.Start(task);
                }
            }
        }

        public static void IndexFolder(TaskSettings taskSettings = null)
        {
            using (FolderSelectDialog dlg = new FolderSelectDialog())
            {
                if (dlg.ShowDialog())
                {
                    IndexFolder(dlg.FileName, taskSettings);
                }
            }
        }

        public static void IndexFolder(string folderPath, TaskSettings taskSettings = null)
        {
            if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
            {
                if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

                taskSettings.ToolsSettings.IndexerSettings.BinaryUnits = Program.Settings.BinaryUnits;

                string source = null;

                Task.Run(() =>
                {
                    source = Indexer.Index(folderPath, taskSettings.ToolsSettings.IndexerSettings);
                }).ContinueInCurrentContext(() =>
                {
                    if (!string.IsNullOrEmpty(source))
                    {
                        WorkerTask task = WorkerTask.CreateTextUploaderTask(source, taskSettings);
                        task.Info.FileName = Path.ChangeExtension(task.Info.FileName, taskSettings.ToolsSettings.IndexerSettings.Output.ToString().ToLower());
                        TaskManager.Start(task);
                    }
                });
            }
        }
    }
}