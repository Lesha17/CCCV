using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Disk.SDK;
using Disk.SDK.Provider;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;

namespace CCCV
{
    public partial class MainWindow
    {

        private void Process_to_download_FL(String name, Item item)
        {
            CCCV_FileList info = item.Data as CCCV_FileList;
            client = new DiskSdkClient(access_token);
            long size = info.Size;

            if (size < settings.CurrentSizeOfData)
            {
                Prepare_to_download(name, item);
            }
            else if (size < settings.MaxSizeOfData && settings.How == Settings.HowUpload.AfterClick)
            {
                notifyIcon.BalloonTipClicked += delegate
                {
                    Prepare_to_download(name, item);
                };
                notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена изменилось на другом компьютере.\n" +
                    "Нажмите, чтобы загрузить", ToolTipIcon.Info);
            }
            else if (settings.ShowBalloon)
            {
                notifyIcon.BalloonTipClicked += delegate
                {
                    Prepare_to_download(name, item);
                };
                notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена изменилось на другом компьютере.\n" +
                    "Похоже, её размер больше максимально возможного"
                    + "Нажмите, чтобы загрузить", ToolTipIcon.Info);
            }
        }

        private void Prepare_to_download(string name, Item item)
        {
            CCCV_FileList fl = item.Data as CCCV_FileList;
            fl.LocalPath = local_files_path + name + "\\";

            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                InitProgress(fl.Size);
                try
                {
                    Create_tmp_dirs(item);
                }
                catch (Exception e)
                {
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        notifyIcon.ShowBalloonTip(0, "CCCV", "Не удалось создать папку:" + "\n" + e.Message, ToolTipIcon.Error);
                    }));
                    return;
                }
                Download_Files(item, fl.Size);
            }));

        }

        private void Create_tmp_dirs(Item item)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                progress.Status.Text = "Создание временных папок...";
            }));
            CCCV_FileList fl = item.Data as CCCV_FileList;
            Console.WriteLine("Creating dir: " + fl.LocalPath);
            Directory.CreateDirectory(fl.LocalPath);
            foreach (string dir in fl.AllDirs)
            {
                Console.WriteLine("Creating dir: " + fl.LocalPath + dir);
                Directory.CreateDirectory(fl.LocalPath + dir);
            }
        }

        private void Download_Files(Item item, long size)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                progress.Status.Text = "Скачивание файлов...";
            }));
            CCCV_FileList fl = item.Data as CCCV_FileList;
            client = new DiskSdkClient(access_token);
            IEnumerator<string> enumerator = fl.AllFiles.GetEnumerator();

            EventHandler<SdkEventArgs> completed = null;

            if (!enumerator.MoveNext())
            {
                SetData(item);
                return;
            }
            FileStream fs;
            try
            {
                fs = new FileStream(fl.LocalPath + enumerator.Current, FileMode.Create, FileAccess.Write);
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    progress.DecrementCountOfTasks(size);
                    notifyIcon.ShowBalloonTip(0, "CCCV", "Не удалось создать файл:" + enumerator.Current + "\n" + ex.Message, ToolTipIcon.Error);
                }));
                return;
            }

            completed = new EventHandler<SdkEventArgs>(delegate (object o, SdkEventArgs e)
                {
                    try
                    {
                        fs.Close();
                    }
                    catch
                    {

                    }
                    if (e.Error != null)
                    {
                        Console.WriteLine(e.Error);
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            progress.DecrementCountOfTasks(size);
                            notifyIcon.ShowBalloonTip(0, "CCCV", "Ошибка загрузки: \n" + e.Error, ToolTipIcon.Error);
                        }));
                        return;
                    }
                    if (!enumerator.MoveNext())
                    {
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            progress.DecrementCountOfTasks(size);
                        }));
                        SetData(item);
                        return;
                    }
                    try
                    {
                        fs = new FileStream(fl.LocalPath + enumerator.Current, FileMode.Create, FileAccess.Write);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            progress.DecrementCountOfTasks(size);
                            notifyIcon.ShowBalloonTip(0, "CCCV", "Не удалось создать файл:" + enumerator.Current + "\n" + ex.Message, ToolTipIcon.Error);
                        }));
                    }
                    Console.WriteLine("Downloading: " + fl.LocalPath + enumerator.Current);
                    client.DownloadFileAsync(fl.DiskPath + enumerator.Current, fs, progress, completed);
                });

            Console.WriteLine("Downloading: " + fl.LocalPath + enumerator.Current);

            client.DownloadFileAsync(fl.DiskPath + enumerator.Current, fs, progress, completed);

        }

        private void UpdateClipbordContent_withFiles(CCCV_FileList fl)
        {
            StringCollection toSet = new StringCollection();
            string[] sc = Directory.GetDirectories(fl.LocalPath);
            foreach (string dir in Directory.GetDirectories(fl.LocalPath))
            {
                toSet.Add(dir);
            }
            sc = Directory.GetDirectories(fl.LocalPath);
            foreach (string file in Directory.GetFiles(fl.LocalPath))
            {
                toSet.Add(file);
            }
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                System.Windows.Clipboard.SetFileDropList(toSet);
            }));
        }

        private void StartUploadFiles(CCCV_FileList fl, string local_path, string disk_path, long info_size, long fl_size)
        {
            client = new DiskSdkClient(access_token);

            IEnumerator<string> dirs = fl.AllDirs.GetEnumerator();

            EventHandler<SdkEventArgs> handler = null;

            handler = new EventHandler<SdkEventArgs>(delegate (Object o, SdkEventArgs e)
            {
                if (e.Error != null)
                {
                    Console.WriteLine("Error: " + e.Error);
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        progress.DecrementCountOfTasks(fl_size);
                        notifyIcon.ShowBalloonTip(0, "CCCV", "Ошибка создания папок на Диске: \n" + e.Error, ToolTipIcon.Error);
                    }));
                    return;

                }
                if (!dirs.MoveNext())
                {
                    UploadFiles(fl, local_path, disk_path, info_size, fl_size, progress);
                    return;
                }
                Console.WriteLine("Making directory: " + fl.DiskPath + dirs.Current);
                client.MakeDirectoryAsync(fl.DiskPath + dirs.Current);
            });

            client.MakeFolderCompleted += handler;

            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                InitProgress(fl_size);
                progress.Status.Text = "Создание папок на Диске..";
                Console.WriteLine("Making directory: " + fl.DiskPath);
                client.MakeDirectoryAsync(fl.DiskPath);
            }));
        }

        private void UploadFiles(CCCV_FileList fl, string local_path, string disk_path, long info_size, long fl_size, Progress pr)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                pr.Status.Text = "Загрузка файлов на Диск...";
            }));

            client = new DiskSdkClient(access_token);

            IEnumerator<string> files = fl.AllFiles.GetEnumerator();

            EventHandler<SdkEventArgs> handler = null;

            if (!files.MoveNext())
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    UploadInfo(local_path, disk_path, info_size);
                }));
                return;
            }


            FileStream fs = null;
            try
            {
                fs = new FileStream(fl.LocalPath + files.Current, FileMode.Open, FileAccess.Read);
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    notifyIcon.ShowBalloonTip(0, "CCCV", "Не удалось открыть файл:" + files.Current + "\n" + ex.Message, ToolTipIcon.Error);
                }));
                return;
            }
            handler = new EventHandler<SdkEventArgs>(delegate (Object o, SdkEventArgs e)
            {
                try
                {
                    fs.Close();
                }
                catch
                {

                }

                if (!files.MoveNext())
                {
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        progress.DecrementCountOfTasks(fl_size);
                        UploadInfo(local_path, disk_path, info_size);
                    }));

                    return;
                }

                if (e.Error != null)
                {
                    Console.WriteLine(e.Error);
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        progress.DecrementCountOfTasks(fl_size);
                        notifyIcon.ShowBalloonTip(0, "CCCV", "Ошибка загрузки файлов на Диск: \n" + e.Error, ToolTipIcon.Error);
                    }));
                    return;
                }


                Console.WriteLine("Uploading file: " + fl.DiskPath + files.Current);

                try
                {
                    fs = new FileStream(fl.LocalPath + files.Current, FileMode.Open, FileAccess.Read);
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        progress.DecrementCountOfTasks(fl_size);
                        notifyIcon.ShowBalloonTip(0, "CCCV", "Не удалось открыть файл:" + files.Current + "\n" + ex.Message, ToolTipIcon.Error);
                    }));
                    return;
                }
                client.UploadFileAsync(fl.DiskPath + files.Current, fs, progress, handler);
            });

            string s = fl.DiskPath + files.Current;
            Console.WriteLine("Uploading file: " + fl.DiskPath + files.Current);

            client.UploadFileAsync(s, fs, progress, handler);
        }
    }
}