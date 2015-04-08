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

namespace CCCV
{
    public partial class MainWindow : Window
    {
        [DllImport("User32.dll")]
        protected static extern int SetClipboardViewer(int hWndNewViewer);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        private const int WM_CHANGECBCHAIN = 0x30D;
        private const int WM_DRAWCLIPBOARD = 0x308;

        private const string List_in_PC_updated = "_list.cbd";
        private const string DownloadThread = "DownloadThread";
        private const int TIMER_INTERVAL = 600000;

        private IntPtr nextClipboardViewer;
        private HwndSource hwndSource;

        private SettingsPage settingsPage;
        private System.Timers.Timer timer;

        private StreamWriter log;

        private bool changedByUser;

        public void CreateClipboardListener()
        {
            hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            nextClipboardViewer = (IntPtr)SetClipboardViewer((int)hwndSource.Handle);
            hwndSource.AddHook(WndProc);

            Dispatcher.BeginInvoke(new System.Threading.ThreadStart(delegate
            {
                Console.WriteLine("Creating listener");
                settingsPage = new SettingsPage(settings);
                settingsPage.RB1.Checked += RB1_Checked;
                settingsPage.RB2.Checked += RB2_Checked;
                settingsPage.RB3.Checked += RB3_Checked;
                settingsPage.SizeOfData.TextChanged += SizeOfData_TextChanged;
                settingsPage.IfSizeBigger.Checked += IfSizeBigger_Checked;
                settingsPage.IfSizeBigger.Unchecked += IfSizeBigger_Unchecked;
                settingsPage.Update_Content.Click += Update_Content_Click;

                Console.WriteLine("Preprocessing completed");

                if (Preprocessing_completed != null)
                    Preprocessing_completed(this, new EventArgs());
            }));
        }

        public void Update_Content_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Update click");
            UpdateClipbordContent();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_DRAWCLIPBOARD:
                    if (changedByUser)
                        ClipboardChanged();
                    break;
                case WM_CHANGECBCHAIN:
                    if (wParam == nextClipboardViewer)
                    {
                        nextClipboardViewer = lParam;
                    }
                    else
                    {
                        SendMessage(nextClipboardViewer, WM_CHANGECBCHAIN, wParam, lParam);
                    }
                    break;
                default:
                    break;
            }

            return IntPtr.Zero;
        }

        void MainWindow_Preprocessing_completed(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Threading.ThreadStart(delegate
            {
                settingsPage = settingsPage ?? new SettingsPage(settings);
                MainFrame.NavigationService.Navigate(settingsPage);
                addLogOutItem();
                changedByUser = true;
                timer = new System.Timers.Timer(TIMER_INTERVAL);
                timer.AutoReset = true;
                timer.Elapsed += timer_Elapsed;
                timer.Start();
            }));
        }

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            UpdateClipbordContent();
        }

        private void UpdateClipbordContent()
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                timer.Stop();
            }));

            client = new DiskSdkClient(access_token);
            client.GetListCompleted += client_GetListCompleted_To_Update;
            client.GetListAsync(Disk_Data_Folder_path);
        }

        void client_GetListCompleted_To_Update(object sender, GenericSdkEventArgs<IEnumerable<DiskItemInfo>> e)
        {
            Console.WriteLine("Get list completed");
            if (e.Result != null && e.Result.Count() > elements_in_DataFolder.Count())
            {
                DiskItemInfo in_elements = Item.LastInList(elements_in_DataFolder);
                DiskItemInfo in_Result = Item.LastInList(e.Result);
                Console.WriteLine("Elements in data folder size: " + elements_in_DataFolder.Count());
                Console.WriteLine("Result size: " + e.Result.Count());
                if (!in_elements.Equals(in_Result))
                {
                    Console.WriteLine("New item: " + in_Result.DisplayName);
                    process_info(in_Result.DisplayName, in_Result.ContentLength);
                }
            }
        }

        void process_info(string name, int size)
        {
            Thread t = new Thread(ThreadStart =>
            {
                UpdateContent_in_Clipboard(name);
            });
            t.SetApartmentState(ApartmentState.STA);
            if (size < settings.CurrentSizeOfData)
            {
                t.Start();
            }
            else if (size < settings.MaxSizeOfData && settings.How == Settings.HowUpload.AfterClick)
            {
                notifyIcon.BalloonTipClicked += delegate
                {
                    t.Start();
                };
                notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена изменилось на другом компьютере.\n" +
                    "Нажмите, чтобы загрузить", ToolTipIcon.Info);
            }
            else if (settings.ShowBalloon)
            {
                notifyIcon.BalloonTipClicked += delegate
                {
                    t.Start();
                };
                notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена изменилось на другом компьютере.\n" +
                    "Похоже, её размер больше максимально возможного"
                    + "Нажмите, чтобы загрузить", ToolTipIcon.Info);
            }

        }

        private void UpdateContent_in_Clipboard(string name)
        {
            Item item = null;
            notify_message = "Загрузка: ";
            ready.WaitOne();
            client = new DiskSdkClient(access_token);
            FileStream fs;
            string downloaded_data;
            try
            {
                fs = File.Open(downloaded_data = local_data_path + name, FileMode.Create);
            }
            catch (Exception)
            {
                return;
            }

            client.DownloadFileAsync(Disk_Data_Folder_path + name, fs,
                this,
                delegate
                {
                    fs.Close();

                    fs = File.Open(downloaded_data, FileMode.Open);
                    JsonSerializer serializer = new JsonSerializer();

                    using (StreamReader sr = new StreamReader(fs))
                    {
                        using (JsonReader reader = new JsonTextReader(sr))
                        {
                            item = serializer.Deserialize<Item>(reader);
                        }
                    }
                    fs.Close();
                    if (item.Data != null)
                    {
                        Thread t = new Thread(ThreadStart =>
                           {
                               changedByUser = false;
                               switch (item.Type)
                               {
                                   case Item.ContentType.Text:
                                       System.Windows.Clipboard.SetText((string)item.Data);
                                       break;
                                   case Item.ContentType.FileList:

                                       break;
                                   default:
                                       break;
                               }
                               notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена изменено.", ToolTipIcon.Info);
                           });
                        t.SetApartmentState(ApartmentState.STA);
                        t.Start();
                        t.Join();
                        changedByUser = true;
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                            {
                                timer.Start();
                            }));
                    }

                    notify_message = "Ничего не происходит";

                    ready.Set();
                });
        }

        private void ClipboardChanged()
        {
            Console.WriteLine("Clipboard changed");
            client = new DiskSdkClient(access_token);
            new Thread(ThreadStart => { Get_and_Upload(); }).Start();
        }

        private void Get_and_Upload()
        {
            Thread t = new Thread(new ThreadStart(delegate
            {
                DateTime now = DateTime.Now;
                string content_name = now.ToBinary().ToString();
                object content = null;
                long size = 0;
                Item.ContentType thisType = Item.ContentType.Text;

                if (System.Windows.Clipboard.ContainsText())
                {
                    thisType = Item.ContentType.Text;
                    content = System.Windows.Clipboard.GetText();
                }
                else if (System.Windows.Clipboard.ContainsFileDropList())
                {
                    thisType = Item.ContentType.FileList;
                    content = new CCCV_FileList(System.Windows.Clipboard.GetFileDropList(), Disk_Files_Folder_path + content_name + "/");
                }
                else if (System.Windows.Clipboard.ContainsImage())
                {
                    thisType = Item.ContentType.Image;
                }
                else if (System.Windows.Clipboard.ContainsAudio())
                {
                    thisType = Item.ContentType.Audio;
                }
                else
                {
                    return;
                }

                Console.WriteLine("Serializing content");

                Item content_info = new Item(content, thisType);

                JsonSerializer serializer = new JsonSerializer();
                string serialized_data;
                using (StreamWriter sw = new StreamWriter(serialized_data = local_data_path + content_name))
                {
                    using (JsonTextWriter writer = new JsonTextWriter(sw))
                    {
                        serializer.Serialize(writer, content_info);
                    }
                }
                size = new FileInfo(serialized_data).Length + (thisType == Item.ContentType.FileList ? ((CCCV_FileList)content).Size : 0);

                Console.WriteLine("Serialized. Size = " + size);

                if (size < settings.CurrentSizeOfData)
                {
                    Console.WriteLine("size<current size");
                    OkayUpload(content_info, serialized_data, Disk_Data_Folder_path + content_name);
                }
                else if (settings.How == Settings.HowUpload.AfterClick && size < settings.MaxSizeOfData)
                {
                    notifyIcon.BalloonTipClicked += delegate
                    {
                        Console.WriteLine("Notify clicked");
                        OkayUpload(content_info, serialized_data, Disk_Data_Folder_path + content_name);
                    };
                    notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена изменилось.\n" +
                        "Нажмите, чтобы загрузить", ToolTipIcon.Info);
                }
                else if (settings.ShowBalloon)
                {
                    notifyIcon.BalloonTipClicked += delegate
                    {
                        OkayUpload(content_info, serialized_data, Disk_Data_Folder_path + content_name);
                    };
                    notifyIcon.ShowBalloonTip(0, "CCCV", "Похоже, размер объекта больше максимального.\n" +
                        "Нажмите, если всё равно хотите загрузить", ToolTipIcon.Info);
                }
            }));
            t.SetApartmentState(ApartmentState.STA);
            Console.WriteLine("starting thread in get_and_upload");
            t.Start();
        }

        private void OkayUpload(Item item, string local_path, string disk_path)
        {
            //ready.WaitOne();
            Console.WriteLine("Uploading content in OkayUpload");

            if (item.Type == Item.ContentType.FileList)
            {
                CreateDirs(item.Data as CCCV_FileList, local_path, disk_path);
            }
            else
            {
                UploadInfo(local_path, disk_path);  
            }
        }

        private void UploadInfo(string local_path, string disk_path)
        {
            FileStream fs = File.Open(local_path, FileMode.Open);
            client = new DiskSdkClient(access_token);
            client.UploadFileAsync(disk_path, fs,
                this, Completed);
        }


        private void Completed(object o, SdkEventArgs e)
        {
            notify_message = "Содержимое вашего буфера загружается на Диск\n";
            ready.Set();
            notifyIcon.BalloonTipClicked += delegate
            {

            };
            notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена загружено на сервер", ToolTipIcon.Info);

        }

    }
}