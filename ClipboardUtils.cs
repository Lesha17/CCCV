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
            client.GetListAsync(Data_Folder_path);
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
                    Item info = new Item(null, in_Result.DisplayName, in_Result.CreationDate, "", (long)(in_Result.ContentLength));
                    process_info(info);
                }
            }
        }

        void process_info(Item info)
        {
            Thread t = new Thread(ThreadStart =>
            {
                UpdateContent_in_Clipboard(info);
            });
            t.SetApartmentState(ApartmentState.STA);
            if (info.Size < settings.CurrentSizeOfData)
            {
                t.Start();
            }
            else if (info.Size < settings.MaxSizeOfData && settings.How == Settings.HowUpload.AfterClick)
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

        private void UpdateContent_in_Clipboard(Item item)
        {
            notify_message = "Загрузка: ";
            ready.WaitOne();
            client = new DiskSdkClient(access_token);
            FileStream fs;
            try
            {
                fs = File.Open(item.Path, FileMode.Create);
            }
            catch (Exception)
            {
                return;
            }

            client.DownloadFileAsync(Data_Folder_path + item.Path, fs,
                this,
                delegate
                {
                    fs.Close();

                    fs = File.Open(item.Path, FileMode.Open);
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
                               System.Windows.Clipboard.SetData(item.Type, item.Data);
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
            Get_and_Upload();
        }

        private void Get_and_Upload()
        {
            Thread t = new Thread(new ThreadStart(delegate
            {
                DateTime now = DateTime.Now;
                string content_path = now.ToBinary().ToString();
                object content = null;
                long size = 0;
                string thisFormat = "";
                string[] formats = System.Windows.Clipboard.GetDataObject().GetFormats();
                foreach (string format in formats)
                {
                    content = System.Windows.Clipboard.GetData(format);
                    if (content != null && content.GetType().IsSerializable)
                    {
                        thisFormat = format;
                        break;
                    }
                }
                Console.WriteLine("Serializing content");

                Item content_info = new Item(content, content_path, now, thisFormat, size);

                JsonSerializer serializer = new JsonSerializer();
                using (StreamWriter sw = new StreamWriter(content_path))
                {
                    using (JsonTextWriter writer = new JsonTextWriter(sw))
                    {
                        serializer.Serialize(writer, content_info);
                    }
                }
                size = new FileInfo(content_path).Length;

                Console.WriteLine("Serialized. Size = " + size);

                if (size < settings.CurrentSizeOfData)
                {
                    Console.WriteLine("size<current size");
                    OkayUpload(content_path, now, thisFormat, size);
                }
                else if (settings.How == Settings.HowUpload.AfterClick && size < settings.MaxSizeOfData)
                {
                    notifyIcon.BalloonTipClicked += delegate
                    {
                        Console.WriteLine("Notify clicked");
                        OkayUpload(content_path, now, thisFormat, size);
                    };
                    notifyIcon.ShowBalloonTip(0, "CCCV", "Содержимое буфера обмена изменилось.\n" +
                        "Нажмите, чтобы загрузить", ToolTipIcon.Info);
                }
                else if (settings.ShowBalloon)
                {
                    notifyIcon.BalloonTipClicked += delegate
                    {
                        OkayUpload(content_path, now, thisFormat, size);
                    };
                    notifyIcon.ShowBalloonTip(0, "CCCV", "Похоже, размер объекта больше максимального.\n" +
                        "Нажмите, если всё равно хотите загрузить", ToolTipIcon.Info);
                }
            }));
            t.SetApartmentState(ApartmentState.STA);
            Console.WriteLine("starting thread in get_and_upload");
            t.Start();
        }

        private void OkayUpload(string content_path, DateTime now, string thisFormat, long size)
        {
            ready.WaitOne();
            Console.WriteLine("Uploading content in OkayUpload");
            FileStream fs = File.Open(content_path, FileMode.Open);
            client = new DiskSdkClient(access_token);

            client.UploadFileAsync(Data_Folder_path + content_path, fs,
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