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

        private void process_to_download_FL(String name, CCCV_FileList info)
        {
            client = new DiskSdkClient(access_token);
            long size = info.Size;

            if (size < settings.CurrentSizeOfData)
            {
                prepare_to_download(name, info);
            }
            else if (size < settings.MaxSizeOfData && settings.How == Settings.HowUpload.AfterClick)
            {
                notifyIcon.BalloonTipClicked += delegate
                {
                    prepare_to_download(name, info);
                };
                notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена изменилось на другом компьютере.\n" +
                    "Нажмите, чтобы загрузить", ToolTipIcon.Info);
            }
            else if (settings.ShowBalloon)
            {
                notifyIcon.BalloonTipClicked += delegate
                {
                    prepare_to_download(name, info);
                };
                notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена изменилось на другом компьютере.\n" +
                    "Похоже, её размер больше максимально возможного"
                    + "Нажмите, чтобы загрузить", ToolTipIcon.Info);
            }
        }

        private void prepare_to_download(string name, CCCV_FileList fl)
        {
            fl.LocalPath = local_files_path + name;
            create_tmp_dirs(fl);
            download_Files(fl);
        }

        private void create_tmp_dirs(CCCV_FileList fl)
        {
            Directory.CreateDirectory(fl.LocalPath);
            foreach (string dir in fl.AllDirs)
            {
                Directory.CreateDirectory(fl.LocalPath + dir);
            }
        }

        private void download_Files(CCCV_FileList fl)
        {
            client = new DiskSdkClient(access_token);
            IEnumerator<string> enumerator = fl.AllFiles.GetEnumerator();

            EventHandler<SdkEventArgs> completed = null;

            completed = new EventHandler<SdkEventArgs>(delegate
                {
                    if (!enumerator.MoveNext())
                    {
                        UpdateClipbordContent_withFiles(fl);
                        return;
                    }
                    FileStream fis = new FileStream(fl.LocalPath + enumerator.Current, FileMode.Create, FileAccess.Write);
                    client.DownloadFileAsync(fl.DiskPath + enumerator.Current, fis, this, completed);
                });

            FileStream fs = new FileStream(fl.LocalPath + enumerator.Current, FileMode.Create, FileAccess.Write);
            client.DownloadFileAsync(fl.DiskPath + enumerator.Current, fs, this, completed);
        }

        private void UpdateClipbordContent_withFiles(CCCV_FileList fl)
        {
            StringCollection toSet = new StringCollection();
            foreach (string dir in Directory.GetDirectories(fl.LocalPath))
            {
                toSet.Add(dir);
            }
            foreach (string file in Directory.GetDirectories(fl.LocalPath))
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

            handler = new EventHandler<SdkEventArgs>(delegate(Object o, SdkEventArgs e)
            {
                if (e.Error != null)
                {
                    Console.WriteLine("Error: " + e.Error);
                }
                if (!dirs.MoveNext())
                {
                    UploadFiles(fl, local_path, disk_path);
                    return;
                }
                Console.WriteLine("Making directory: " + fl.DiskPath + dirs.Current);
                client.MakeDirectoryAsync(fl.DiskPath + dirs.Current);
            });

            client.MakeFolderCompleted += handler;

            Console.WriteLine("Making directory: " + fl.DiskPath);
            client.MakeDirectoryAsync(fl.DiskPath);
        }

        private void UploadFiles(CCCV_FileList fl, string local_path, string disk_path)
        {
            client = new DiskSdkClient(access_token);

            IEnumerator<string> files = fl.AllFiles.GetEnumerator();

            EventHandler<SdkEventArgs> handler = null;

            handler = new EventHandler<SdkEventArgs>(delegate(Object o, SdkEventArgs e)
            {
                if (e.Error != null)
                {
                    Console.WriteLine(e.Error);
                }
                Console.WriteLine(e.Error);
                if (!files.MoveNext())
                {
                    UploadInfo(local_path, disk_path);
                    return;
                }
                Console.WriteLine("Uploading file: " + fl.DiskPath + files.Current);
                FileStream fs = new FileStream(fl.LocalPath + files.Current, FileMode.Open, FileAccess.Read);
                client.UploadFileAsync(fl.DiskPath + files.Current, fs, this, handler);
            });

            if (!files.MoveNext())
            {
                return;
            }

            FileStream _fs = new FileStream(fl.LocalPath + files.Current, FileMode.Open, FileAccess.Read);
            string s = fl.DiskPath + files.Current;
            Console.WriteLine("Uploading file: " + fl.DiskPath + files.Current);
            client.UploadFileAsync(s, _fs, this, handler);

        }
    }
}