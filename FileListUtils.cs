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

        private void process_to_download_FL(String name, Item item)
        {
            CCCV_FileList info = item.Data as CCCV_FileList;
            client = new DiskSdkClient(access_token);
            long size = info.Size;

            if (size < settings.CurrentSizeOfData)
            {
                prepare_to_download(name, item);
            }
            else if (size < settings.MaxSizeOfData && settings.How == Settings.HowUpload.AfterClick)
            {
                notifyIcon.BalloonTipClicked += delegate
                {
                    prepare_to_download(name, item);
                };
                notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена изменилось на другом компьютере.\n" +
                    "Нажмите, чтобы загрузить", ToolTipIcon.Info);
            }
            else if (settings.ShowBalloon)
            {
                notifyIcon.BalloonTipClicked += delegate
                {
                    prepare_to_download(name, item);
                };
                notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена изменилось на другом компьютере.\n" +
                    "Похоже, её размер больше максимально возможного"
                    + "Нажмите, чтобы загрузить", ToolTipIcon.Info);
            }
        }

        private void prepare_to_download(string name, Item item)
        {
            CCCV_FileList fl = item.Data as CCCV_FileList;
            fl.LocalPath = local_files_path + name + "\\";
            
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                progress = new Progress(this, fl.Size);
                MainFrame.Navigate(progress);
                create_tmp_dirs(item);
                download_Files(item);
            }));
            
        }

        private void create_tmp_dirs(Item item)
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

        private void download_Files(Item item)
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
                setData(item);
                return;
            }
            FileStream fs = new FileStream(fl.LocalPath + enumerator.Current, FileMode.Create, FileAccess.Write);

            completed = new EventHandler<SdkEventArgs>(delegate(object o, SdkEventArgs e)
                {
                    fs.Close();
                    if (e.Error != null)
                    {
                        Console.WriteLine(e.Error);
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            MainFrame.Navigate(settingsPage);
                            notifyIcon.ShowBalloonTip(0, "CCCV", "Ошибка загрузки: \n" + e.Error, ToolTipIcon.Error);
                        }));
                        return;
                    }
                    if (!enumerator.MoveNext())
                    {
                        setData(item);
                        return;
                    }
                    fs = new FileStream(fl.LocalPath + enumerator.Current, FileMode.Create, FileAccess.Write);
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

        private void CreateDirs(CCCV_FileList fl, string local_path, string disk_path)
        {
            client = new DiskSdkClient(access_token);

            IEnumerator<string> dirs = fl.AllDirs.GetEnumerator();

            EventHandler<SdkEventArgs> handler = null;

            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                progress.Status.Text = "Создание папок на Диске..";
            }));

            handler = new EventHandler<SdkEventArgs>(delegate(Object o, SdkEventArgs e)
            {
                if (e.Error != null)
                {
                    Console.WriteLine("Error: " + e.Error);
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        MainFrame.Navigate(settingsPage);
                        notifyIcon.ShowBalloonTip(0, "CCCV", "Ошибка создания папок на Диске: \n" + e.Error, ToolTipIcon.Error);
                    }));
                    return;

                }
                if (!dirs.MoveNext())
                {
                    UploadFiles(fl, local_path, disk_path, progress);
                    return;
                }
                Console.WriteLine("Making directory: " + fl.DiskPath + dirs.Current);
                client.MakeDirectoryAsync(fl.DiskPath + dirs.Current);
            });

            client.MakeFolderCompleted += handler;

            Console.WriteLine("Making directory: " + fl.DiskPath);
            client.MakeDirectoryAsync(fl.DiskPath);
        }

        private void UploadFiles(CCCV_FileList fl, string local_path, string disk_path, Progress pr)
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
                UploadInfo(local_path, disk_path);
                return;
            }

            //FileInfo fi = new FileInfo(fl.LocalPath + files.Current);

            FileStream fs = new FileStream(fl.LocalPath + files.Current, FileMode.Open, FileAccess.Read);

            handler = new EventHandler<SdkEventArgs>(delegate(Object o, SdkEventArgs e)
            {
                fs.Close();
                //progress.Done += fi.Length;
                if (e.Error != null)
                {
                    Console.WriteLine(e.Error);
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        MainFrame.Navigate(settingsPage);
                        notifyIcon.ShowBalloonTip(0, "CCCV", "Ошибка загрузки файлов на Диск: \n" + e.Error, ToolTipIcon.Error);
                    }));
                    return;
                }
                
                if (!files.MoveNext())
                {
                    UploadInfo(local_path, disk_path);
                    return;
                }
                Console.WriteLine("Uploading file: " + fl.DiskPath + files.Current);

                //fi = new FileInfo(fl.LocalPath + files.Current);
                fs = new FileStream(fl.LocalPath + files.Current, FileMode.Open, FileAccess.Read);
                client.UploadFileAsync(fl.DiskPath + files.Current, fs, pr, handler);
            });

            string s = fl.DiskPath + files.Current;
            Console.WriteLine("Uploading file: " + fl.DiskPath + files.Current);
            client.UploadFileAsync(s, fs, pr, handler);

        }
    }
}